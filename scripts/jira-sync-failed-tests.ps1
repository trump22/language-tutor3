param(
    [string] $ResultsDirectory = "TestResults",
    [string] $JiraBaseUrl = $env:JIRA_BASE_URL,
    [string] $JiraEmail = $env:JIRA_EMAIL,
    [string] $JiraApiToken = $env:JIRA_API_TOKEN,
    [string] $ProjectKey = $env:JIRA_PROJECT_KEY,
    [string] $RunUrl = $env:GITHUB_SERVER_URL,
    [string] $IssueType = "Bug"
)

$ErrorActionPreference = "Stop"

function Write-Skip($message) {
    Write-Host "[jira-sync] $message"
}

function New-AdfParagraph([string] $text) {
    return @{
        type = "paragraph"
        content = @(
            @{
                type = "text"
                text = $text
            }
        )
    }
}

function New-AdfCodeBlock([string] $text) {
    if ([string]::IsNullOrWhiteSpace($text)) {
        $text = "(empty)"
    }

    if ($text.Length -gt 6000) {
        $text = $text.Substring(0, 6000) + "`n... truncated ..."
    }

    return @{
        type = "codeBlock"
        attrs = @{
            language = "text"
        }
        content = @(
            @{
                type = "text"
                text = $text
            }
        )
    }
}

if ([string]::IsNullOrWhiteSpace($JiraBaseUrl) -or
    [string]::IsNullOrWhiteSpace($JiraEmail) -or
    [string]::IsNullOrWhiteSpace($JiraApiToken) -or
    [string]::IsNullOrWhiteSpace($ProjectKey)) {
    Write-Skip "Jira secrets are not configured. Skipping failed-test issue sync."
    exit 0
}

$trxPath = Get-ChildItem -Path $ResultsDirectory -Filter "*.trx" -Recurse |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $trxPath) {
    Write-Skip "No TRX file found under $ResultsDirectory."
    exit 0
}

[xml] $trx = Get-Content $trxPath.FullName -Raw
$ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
$ns.AddNamespace("trx", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

$failedResults = $trx.SelectNodes("//trx:UnitTestResult[@outcome!='Passed']", $ns)
if (-not $failedResults -or $failedResults.Count -eq 0) {
    Write-Host "[jira-sync] No failed automated tests found."
    exit 0
}

$basicAuth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${JiraEmail}:${JiraApiToken}"))
$headers = @{
    Authorization = "Basic $basicAuth"
    Accept = "application/json"
    "Content-Type" = "application/json"
}

$jiraRoot = $JiraBaseUrl.TrimEnd("/")

foreach ($result in $failedResults) {
    $testName = [string] $result.testName
    $outcome = [string] $result.outcome
    $duration = [string] $result.duration
    $tcId = if ($testName -match "(TC\d+)") { $Matches[1] } else { "UNKNOWN-TC" }
    $tcLabel = $tcId.ToLowerInvariant().Replace("tc", "tc-")

    $messageNode = $result.SelectSingleNode("trx:Output/trx:ErrorInfo/trx:Message", $ns)
    $stackNode = $result.SelectSingleNode("trx:Output/trx:ErrorInfo/trx:StackTrace", $ns)
    $message = if ($messageNode) { [string] $messageNode.InnerText } else { "No error message captured." }
    $stackTrace = if ($stackNode) { [string] $stackNode.InnerText } else { "No stack trace captured." }

    $summary = "BUG: $tcId failed - $testName"
    $runLink = if ($env:GITHUB_SERVER_URL -and $env:GITHUB_REPOSITORY -and $env:GITHUB_RUN_ID) {
        "$($env:GITHUB_SERVER_URL)/$($env:GITHUB_REPOSITORY)/actions/runs/$($env:GITHUB_RUN_ID)"
    } elseif ($RunUrl) {
        $RunUrl
    } else {
        "Local or unknown run"
    }

    $description = @{
        type = "doc"
        version = 1
        content = @(
            (New-AdfParagraph "Automated Selenium/xUnit test failed in the Language Tutor CI/CD pipeline."),
            (New-AdfParagraph "Test case: $tcId"),
            (New-AdfParagraph "Test name: $testName"),
            (New-AdfParagraph "Outcome: $outcome"),
            (New-AdfParagraph "Duration: $duration"),
            (New-AdfParagraph "Run: $runLink"),
            (New-AdfParagraph "Error message:"),
            (New-AdfCodeBlock $message),
            (New-AdfParagraph "Stack trace:"),
            (New-AdfCodeBlock $stackTrace)
        )
    }

    $jql = "project = $ProjectKey AND labels = automated-test-failure AND labels = $tcLabel AND statusCategory != Done ORDER BY created DESC"
    $encodedJql = [Uri]::EscapeDataString($jql)
    $searchUrl = "$jiraRoot/rest/api/3/search/jql?jql=$encodedJql&maxResults=1&fields=key,summary"
    $existing = Invoke-RestMethod -Method Get -Uri $searchUrl -Headers $headers

    if ($existing.issues.Count -gt 0) {
        $issueKey = $existing.issues[0].key
        $commentBody = @{
            body = $description
        } | ConvertTo-Json -Depth 20

        Invoke-RestMethod `
            -Method Post `
            -Uri "$jiraRoot/rest/api/3/issue/$issueKey/comment" `
            -Headers $headers `
            -Body $commentBody | Out-Null

        Write-Host "[jira-sync] Updated $issueKey for $tcId"
        continue
    }

    $createBody = @{
        fields = @{
            project = @{
                key = $ProjectKey
            }
            summary = $summary
            description = $description
            issuetype = @{
                name = $IssueType
            }
            labels = @("automated-test-failure", "selenium", "ci", $tcLabel)
        }
    } | ConvertTo-Json -Depth 20

    $created = Invoke-RestMethod `
        -Method Post `
        -Uri "$jiraRoot/rest/api/3/issue" `
        -Headers $headers `
        -Body $createBody

    Write-Host "[jira-sync] Created $($created.key) for $tcId"
}
