param(
    [switch] $SkipTests
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$composeFiles = @(
    "-f", "docker-compose.selenium.yml",
    "-f", "docker-compose.selenium.ui.yml"
)

Write-Host "Starting Selenium UI environment..." -ForegroundColor Cyan
docker compose @composeFiles up --build -d postgres backend frontend selenium

Write-Host "Opening Selenium Grid UI and noVNC browser view..." -ForegroundColor Cyan
Start-Process "http://localhost:4444/ui"
Start-Process "http://localhost:7900/?autoconnect=1&resize=scale&password=secret"

if ($SkipTests) {
    Write-Host "Environment is running. Run tests later with:" -ForegroundColor Yellow
    Write-Host "docker compose -f docker-compose.selenium.yml -f docker-compose.selenium.ui.yml run --rm tests"
    exit 0
}

Write-Host "Running Selenium tests. Watch the browser in the noVNC tab." -ForegroundColor Cyan
docker compose @composeFiles run --rm tests

Write-Host ""
Write-Host "Selenium UI remains available:" -ForegroundColor Green
Write-Host "Grid:  http://localhost:4444/ui"
Write-Host "noVNC: http://localhost:7900/?autoconnect=1&resize=scale&password=secret"
Write-Host ""
Write-Host "Stop everything with:" -ForegroundColor Yellow
Write-Host "docker compose -f docker-compose.selenium.yml -f docker-compose.selenium.ui.yml down -v --remove-orphans"
