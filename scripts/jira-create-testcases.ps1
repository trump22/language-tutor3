param(
    [string] $JiraBaseUrl = $env:JIRA_BASE_URL,
    [string] $JiraEmail = $env:JIRA_EMAIL,
    [string] $JiraApiToken = $env:JIRA_API_TOKEN,
    [string] $ProjectKey = $env:JIRA_PROJECT_KEY,
    [string] $IssueType = "Task"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($JiraBaseUrl) -or
    [string]::IsNullOrWhiteSpace($JiraEmail) -or
    [string]::IsNullOrWhiteSpace($JiraApiToken) -or
    [string]::IsNullOrWhiteSpace($ProjectKey)) {
    throw "Set JIRA_BASE_URL, JIRA_EMAIL, JIRA_API_TOKEN, and JIRA_PROJECT_KEY before running this script."
}

$root = Split-Path -Parent $PSScriptRoot
$testCasesPath = Join-Path $root "tests\LanguageTutor.E2E\TestCases\jira-testcases.json"
$testCases = Get-Content $testCasesPath -Raw | ConvertFrom-Json

$basicAuth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${JiraEmail}:${JiraApiToken}"))
$headers = @{
    Authorization = "Basic $basicAuth"
    Accept = "application/json"
    "Content-Type" = "application/json"
}

foreach ($testCase in $testCases) {
    $body = @{
        fields = @{
            project = @{
                key = $ProjectKey
            }
            summary = $testCase.summary
            description = @{
                type = "doc"
                version = 1
                content = @(
                    @{
                        type = "paragraph"
                        content = @(
                            @{
                                type = "text"
                                text = $testCase.description
                            }
                        )
                    }
                )
            }
            issuetype = @{
                name = $IssueType
            }
            labels = @("automated-test", "selenium", "allure")
        }
    } | ConvertTo-Json -Depth 12

    $created = Invoke-RestMethod `
        -Method Post `
        -Uri "$($JiraBaseUrl.TrimEnd('/'))/rest/api/3/issue" `
        -Headers $headers `
        -Body $body

    Write-Host "$($testCase.key) -> $($created.key): $($testCase.summary)"
}
