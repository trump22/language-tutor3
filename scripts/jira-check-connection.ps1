param(
    [string] $JiraBaseUrl = $env:JIRA_BASE_URL,
    [string] $JiraEmail = $env:JIRA_EMAIL,
    [string] $JiraApiToken = $env:JIRA_API_TOKEN,
    [string] $ProjectKey = $env:JIRA_PROJECT_KEY
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($JiraBaseUrl) -or
    [string]::IsNullOrWhiteSpace($JiraEmail) -or
    [string]::IsNullOrWhiteSpace($JiraApiToken) -or
    [string]::IsNullOrWhiteSpace($ProjectKey)) {
    throw "Missing Jira configuration. Check JIRA_BASE_URL, JIRA_EMAIL, JIRA_API_TOKEN, and JIRA_PROJECT_KEY secrets."
}

$basicAuth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${JiraEmail}:${JiraApiToken}"))
$headers = @{
    Authorization = "Basic $basicAuth"
    Accept = "application/json"
}

$jiraRoot = $JiraBaseUrl.TrimEnd("/")

Write-Host "Checking Jira account..."
$me = Invoke-RestMethod `
    -Method Get `
    -Uri "$jiraRoot/rest/api/3/myself" `
    -Headers $headers

Write-Host "Authenticated as: $($me.displayName) <$($me.emailAddress)>"

Write-Host "Checking Jira project: $ProjectKey"
$project = Invoke-RestMethod `
    -Method Get `
    -Uri "$jiraRoot/rest/api/3/project/$ProjectKey" `
    -Headers $headers

Write-Host "Project found: $($project.key) - $($project.name)"
Write-Host "Jira connection OK."
