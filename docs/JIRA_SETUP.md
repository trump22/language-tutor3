# Jira Integration

This project uses Jira as the work-management layer for automated test cases.
Docker still runs the application and Selenium; Jira tracks tasks, bugs, and test-case ownership.

## What Is Connected

- Allure test cases are named `TC1` to `TC18`.
- `TC1` to `TC6` cover the original smoke and student journey tests.
- `TC7` to `TC18` are automated from the report test cases:
  - Sign-up validation
  - Login validation and admin role routing
  - Admin add-user flow
  - AI exercise creator form flow
- Each automated test has a Jira issue link placeholder such as `LT-7`, `LT-8`, and so on.
- Allure uses `JIRA_BASE_URL` to turn those issue keys into clickable Jira links.
- CI can also create or update Jira `Bug` issues automatically when a Selenium testcase fails.

## Required Jira Values

Create or open a Jira project, then collect:

- `JIRA_BASE_URL`: for example `https://your-domain.atlassian.net`
- `JIRA_PROJECT_KEY`: for example `LT`
- `JIRA_EMAIL`: your Atlassian account email
- `JIRA_API_TOKEN`: an Atlassian API token

Do not commit the email or token to git.

## Local PowerShell Setup

```powershell
$env:JIRA_BASE_URL="https://your-domain.atlassian.net"
$env:JIRA_PROJECT_KEY="LT"
$env:JIRA_EMAIL="you@example.com"
$env:JIRA_API_TOKEN="your-atlassian-api-token"
```

Create Jira issues for the current automated test cases:

```powershell
.\scripts\jira-create-testcases.ps1
```

Run Selenium and generate Allure links to Jira:

```powershell
docker compose -f docker-compose.selenium.yml up --build --abort-on-container-exit --exit-code-from tests
npx --yes allure-commandline generate TestResults/allure-results --clean --output TestResults/allure-report
```

Open the report:

```powershell
python -m http.server 8089 --directory TestResults\allure-report
```

Then open:

```text
http://localhost:8089
```

## GitHub Actions Secrets

Add these repository secrets if you want Jira links in CI reports:

- `JIRA_BASE_URL`
- `JIRA_PROJECT_KEY`
- `JIRA_EMAIL`
- `JIRA_API_TOKEN`

`JIRA_BASE_URL` is enough for clickable Allure links. `JIRA_PROJECT_KEY`, `JIRA_EMAIL`, and `JIRA_API_TOKEN` are required when CI creates or updates Jira bugs for failed tests.

## Sync Failed Tests To Jira

GitHub Actions runs this automatically when the Selenium job fails:

```powershell
./scripts/jira-sync-failed-tests.ps1 -ResultsDirectory TestResults
```

Local usage:

```powershell
$env:JIRA_BASE_URL="https://your-domain.atlassian.net"
$env:JIRA_PROJECT_KEY="LT"
$env:JIRA_EMAIL="you@example.com"
$env:JIRA_API_TOKEN="your-atlassian-api-token"
.\scripts\jira-sync-failed-tests.ps1 -ResultsDirectory TestResults
```

Behavior:

- If all tests pass, no Jira bug is created.
- If a test fails, the script reads `selenium.trx`.
- It extracts the testcase ID, for example `TC15`.
- It searches for an open Jira bug with labels `automated-test-failure` and `tc-15`.
- If found, it adds a new comment with the latest run details.
- If not found, it creates a new `Bug`.
