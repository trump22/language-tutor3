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

function New-SafeFileName([string] $value) {
    $safe = $value -replace "[^a-zA-Z0-9_.-]", "_"
    if ($safe.Length -gt 80) {
        $safe = $safe.Substring(0, 80)
    }

    return $safe.Trim("_")
}

function Save-FailedTestLog(
    [string] $OutputDirectory,
    [string] $TestCaseId,
    [string] $TestName,
    [string] $Outcome,
    [string] $Duration,
    [string] $RunLink,
    [string] $Message,
    [string] $StackTrace
) {
    if (-not (Test-Path -LiteralPath $OutputDirectory)) {
        New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
    }

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $safeTestCaseId = New-SafeFileName $TestCaseId
    $safeTestName = New-SafeFileName $TestName
    $fileName = "${safeTestCaseId}_${timestamp}_${safeTestName}.log"
    $path = Join-Path $OutputDirectory $fileName

    $content = @"
Language Tutor automated test failure
=====================================

Test case: $TestCaseId
Test name: $TestName
Outcome: $Outcome
Duration: $Duration
Run: $RunLink
Created at UTC: $([DateTimeOffset]::UtcNow.ToString("u"))

Error message
-------------
$Message

Stack trace
-----------
$StackTrace
"@

    Set-Content -LiteralPath $path -Value $content -Encoding utf8
    return $path
}

function Add-JiraAttachment(
    [string] $IssueKey,
    [string] $FilePath,
    [string] $JiraRoot,
    [hashtable] $BaseHeaders
) {
    if (-not (Test-Path -LiteralPath $FilePath)) {
        Write-Host "[jira-sync] Attachment file not found: $FilePath"
        return
    }

    $attachmentHeaders = @{
        Authorization = $BaseHeaders.Authorization
        Accept = "application/json"
        "X-Atlassian-Token" = "no-check"
    }

    try {
        Invoke-RestMethod `
            -Method Post `
            -Uri "$JiraRoot/rest/api/3/issue/$IssueKey/attachments" `
            -Headers $attachmentHeaders `
            -Form @{
                file = Get-Item -LiteralPath $FilePath
            } | Out-Null

        Write-Host "[jira-sync] Attached $(Split-Path -Leaf $FilePath) to $IssueKey"
    }
    catch {
        Write-Host "[jira-sync] Could not attach $(Split-Path -Leaf $FilePath) to ${IssueKey}: $($_.Exception.Message)"
    }
}

function Get-AllureFailureAttachments(
    [string] $ResultsDirectory,
    [string] $TestName
) {
    $allureResultsDirectory = Join-Path $ResultsDirectory "allure-results"
    if (-not (Test-Path -LiteralPath $allureResultsDirectory)) {
        return @()
    }

    $attachments = New-Object System.Collections.Generic.List[string]
    $resultFiles = Get-ChildItem -Path $allureResultsDirectory -Filter "*-result.json" -File -ErrorAction SilentlyContinue

    foreach ($file in $resultFiles) {
        try {
            $result = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
        }
        catch {
            continue
        }

        $matchesTest = $false
        if ($result.fullName -and ([string] $result.fullName).Contains($TestName)) {
            $matchesTest = $true
        }
        elseif ($result.name -and ([string] $TestName).Contains([string] $result.name)) {
            $matchesTest = $true
        }

        if (-not $matchesTest) {
            continue
        }

        foreach ($attachment in @($result.attachments)) {
            if (-not $attachment.source) {
                continue
            }

            $attachmentPath = Join-Path $allureResultsDirectory ([string] $attachment.source)
            if (Test-Path -LiteralPath $attachmentPath) {
                $attachments.Add($attachmentPath)
            }
        }
    }

    return $attachments.ToArray()
}

function Get-RecentFailureMedia(
    [string] $ResultsDirectory
) {
    $patterns = @("*.mp4", "*.webm", "*.png")
    $media = New-Object System.Collections.Generic.List[string]

    foreach ($pattern in $patterns) {
        Get-ChildItem -Path $ResultsDirectory -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 2 |
            ForEach-Object { $media.Add($_.FullName) }
    }

    return $media.ToArray()
}

function Add-FailureAttachments(
    [string] $IssueKey,
    [string] $LogPath,
    [string] $ResultsDirectory,
    [string] $TestName,
    [string] $JiraRoot,
    [hashtable] $BaseHeaders
) {
    $paths = New-Object System.Collections.Generic.List[string]
    $paths.Add($LogPath)

    foreach ($path in (Get-AllureFailureAttachments -ResultsDirectory $ResultsDirectory -TestName $TestName)) {
        $paths.Add($path)
    }

    if ($paths.Count -eq 1) {
        foreach ($path in (Get-RecentFailureMedia -ResultsDirectory $ResultsDirectory)) {
            $paths.Add($path)
        }
    }

    $uniquePaths = $paths |
        Where-Object { Test-Path -LiteralPath $_ } |
        Select-Object -Unique |
        Select-Object -First 4

    foreach ($path in $uniquePaths) {
        Add-JiraAttachment `
            -IssueKey $IssueKey `
            -FilePath $path `
            -JiraRoot $JiraRoot `
            -BaseHeaders $BaseHeaders
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
$attachmentDirectory = Join-Path $ResultsDirectory "jira-attachments"

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

    $attachmentPath = Save-FailedTestLog `
        -OutputDirectory $attachmentDirectory `
        -TestCaseId $tcId `
        -TestName $testName `
        -Outcome $outcome `
        -Duration $duration `
        -RunLink $runLink `
        -Message $message `
        -StackTrace $stackTrace

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
        Add-FailureAttachments `
            -IssueKey $issueKey `
            -LogPath $attachmentPath `
            -ResultsDirectory $ResultsDirectory `
            -TestName $testName `
            -JiraRoot $jiraRoot `
            -BaseHeaders $headers
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
    Add-FailureAttachments `
        -IssueKey $created.key `
        -LogPath $attachmentPath `
        -ResultsDirectory $ResultsDirectory `
        -TestName $testName `
        -JiraRoot $jiraRoot `
        -BaseHeaders $headers
}
