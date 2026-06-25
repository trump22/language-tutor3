param(
    [string]$BaseUrl = 'http://localhost:5174',
    [string]$TestFilter = 'Category=Report53'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $root 'tests\LanguageTutor.E2E\LanguageTutor.E2E.csproj'
$results = Join-Path $root 'TestResults'

$env:E2E_BASE_URL = $BaseUrl.TrimEnd('/')
$env:E2E_SCREENSHOT_DIR = Join-Path $results 'screenshots'
Remove-Item Env:SELENIUM_REMOTE_URL -ErrorAction SilentlyContinue

dotnet test $testProject `
    --filter $TestFilter `
    --logger 'trx;LogFileName=selenium-local.trx' `
    --results-directory $results

exit $LASTEXITCODE
