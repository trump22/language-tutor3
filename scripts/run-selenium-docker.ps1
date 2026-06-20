$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$results = Join-Path $root 'TestResults'

if (Test-Path -LiteralPath $results) {
    Remove-Item -LiteralPath $results -Recurse -Force
}
New-Item -ItemType Directory -Path $results | Out-Null

Push-Location $root
try {
    docker compose -f docker-compose.selenium.yml up `
        --build `
        --abort-on-container-exit `
        --exit-code-from tests

    if ($LASTEXITCODE -ne 0) {
        throw "Selenium tests failed with exit code $LASTEXITCODE."
    }
}
finally {
    docker compose -f docker-compose.selenium.yml down -v --remove-orphans
    Pop-Location
}
