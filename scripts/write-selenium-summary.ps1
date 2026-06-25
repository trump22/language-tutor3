param(
    [string] $ResultsDirectory = "TestResults"
)

$ErrorActionPreference = "Stop"

function Write-Line([string] $text) {
    Write-Host $text
    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $text
    }
}

$trxPath = Get-ChildItem -Path $ResultsDirectory -Filter "*.trx" -Recurse -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

Write-Line "## Selenium E2E Summary"
Write-Line ""

$videoCount = @(Get-ChildItem -Path $ResultsDirectory -Filter "*.mp4" -Recurse -File -ErrorAction SilentlyContinue).Count +
    @(Get-ChildItem -Path $ResultsDirectory -Filter "*.webm" -Recurse -File -ErrorAction SilentlyContinue).Count
$screenshotCount = @(Get-ChildItem -Path $ResultsDirectory -Filter "*.png" -Recurse -File -ErrorAction SilentlyContinue).Count
$jiraAttachmentCount = if (Test-Path -LiteralPath (Join-Path $ResultsDirectory "jira-attachments")) {
    @(Get-ChildItem -Path (Join-Path $ResultsDirectory "jira-attachments") -File -ErrorAction SilentlyContinue).Count
} else {
    0
}

if (-not $trxPath) {
    $allureResultsDirectory = Join-Path $ResultsDirectory "allure-results"
    $allureFiles = @()
    if (Test-Path -LiteralPath $allureResultsDirectory) {
        $allureFiles = @(Get-ChildItem -Path $allureResultsDirectory -Filter "*-result.json" -File -ErrorAction SilentlyContinue)
    }

    if ($allureFiles.Count -eq 0) {
        Write-Line "No TRX or Allure result files were found under ``$ResultsDirectory``."
        exit 0
    }

    $allureResults = @(
        foreach ($file in $allureFiles) {
            try {
                Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
            }
            catch {
                $null
            }
        }
    )

    $passed = @($allureResults | Where-Object { $_.status -eq "passed" })
    $failed = @($allureResults | Where-Object { $_.status -eq "failed" })
    $broken = @($allureResults | Where-Object { $_.status -and $_.status -ne "passed" -and $_.status -ne "failed" })

    Write-Line "| Metric | Count |"
    Write-Line "|---|---:|"
    Write-Line "| Total | $($allureResults.Count) |"
    Write-Line "| Passed | $($passed.Count) |"
    Write-Line "| Failed | $($failed.Count) |"
    Write-Line "| Other | $($broken.Count) |"
    Write-Line "| Videos | $videoCount |"
    Write-Line "| Screenshots | $screenshotCount |"
    Write-Line "| Jira log attachments | $jiraAttachmentCount |"
    Write-Line ""

    if ($failed.Count -eq 0) {
        Write-Line "All Selenium tests passed."
        exit 0
    }

    Write-Line "### Failed Test Cases"
    Write-Line ""
    Write-Line "| Test | Error |"
    Write-Line "|---|---|"

    foreach ($result in $failed | Select-Object -First 20) {
        $testName = if ($result.name) { [string] $result.name } else { [string] $result.fullName }
        $message = if ($result.statusDetails.message) { [string] $result.statusDetails.message } else { "No error message captured." }
        $shortMessage = ($message -replace "\r?\n", " ").Trim()
        if ($shortMessage.Length -gt 160) {
            $shortMessage = $shortMessage.Substring(0, 160) + "..."
        }

        Write-Line "| ``$testName`` | $shortMessage |"
    }

    exit 0
}

[xml] $trx = Get-Content $trxPath.FullName -Raw
$ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
$ns.AddNamespace("trx", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

$results = @($trx.SelectNodes("//trx:UnitTestResult", $ns))
$passed = @($results | Where-Object { $_.outcome -eq "Passed" })
$failed = @($results | Where-Object { $_.outcome -eq "Failed" })
$broken = @($results | Where-Object { $_.outcome -and $_.outcome -ne "Passed" -and $_.outcome -ne "Failed" })

Write-Line "| Metric | Count |"
Write-Line "|---|---:|"
Write-Line "| Total | $($results.Count) |"
Write-Line "| Passed | $($passed.Count) |"
Write-Line "| Failed | $($failed.Count) |"
Write-Line "| Other | $($broken.Count) |"
Write-Line "| Videos | $videoCount |"
Write-Line "| Screenshots | $screenshotCount |"
Write-Line "| Jira log attachments | $jiraAttachmentCount |"
Write-Line ""

if ($failed.Count -eq 0) {
    Write-Line "All Selenium tests passed."
    exit 0
}

Write-Line "### Failed Test Cases"
Write-Line ""
Write-Line "| Test | Duration | Error |"
Write-Line "|---|---:|---|"

foreach ($result in $failed | Select-Object -First 20) {
    $testName = [string] $result.testName
    $duration = [string] $result.duration
    $messageNode = $result.SelectSingleNode("trx:Output/trx:ErrorInfo/trx:Message", $ns)
    $message = if ($messageNode) { [string] $messageNode.InnerText } else { "No error message captured." }
    $shortMessage = ($message -replace "\r?\n", " ").Trim()
    if ($shortMessage.Length -gt 160) {
        $shortMessage = $shortMessage.Substring(0, 160) + "..."
    }

    Write-Line "| ``$testName`` | ``$duration`` | $shortMessage |"
}

if ($failed.Count -gt 20) {
    Write-Line ""
    Write-Line "Showing first 20 failed tests. Download ``selenium-results`` for full logs."
}
