$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$zapResults = Join-Path $root 'TestResults\zap'

if (Test-Path -LiteralPath $zapResults) {
    Remove-Item -LiteralPath $zapResults -Recurse -Force
}
New-Item -ItemType Directory -Path $zapResults | Out-Null

Push-Location $root
try {
    docker compose `
        -f docker-compose.selenium.yml `
        -f docker-compose.zap.yml `
        up `
        --build `
        --abort-on-container-exit `
        --exit-code-from zap `
        zap

    if ($LASTEXITCODE -ne 0) {
        throw "OWASP ZAP scan failed with exit code $LASTEXITCODE."
    }
}
finally {
    docker compose `
        -f docker-compose.selenium.yml `
        -f docker-compose.zap.yml `
        down -v --remove-orphans
    Pop-Location
}
