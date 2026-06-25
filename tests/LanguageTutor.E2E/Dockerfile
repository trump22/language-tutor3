FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /tests

COPY LanguageTutor.E2E.csproj ./
RUN dotnet restore

COPY . ./

ENTRYPOINT ["/bin/sh", "-c", "mkdir -p /test-results/allure-results && E2E_TEST_FILTER=${E2E_TEST_FILTER:-Category=Report53} && JIRA_BASE_URL=${JIRA_BASE_URL:-https://your-jira-domain.atlassian.net} && printf '{\"allure\":{\"directory\":\"/test-results/allure-results\",\"links\":[\"%s/browse/{issue}\"]}}' \"$JIRA_BASE_URL\" > /test-results/allureConfig.json && echo \"Running Selenium tests with filter: $E2E_TEST_FILTER\" && ALLURE_CONFIG=/test-results/allureConfig.json dotnet test --no-restore --filter \"$E2E_TEST_FILTER\" --logger 'trx;LogFileName=selenium.trx' --results-directory /test-results -- xUnit.ReporterSwitch=allure; code=$?; echo 'Waiting for Selenium video finalization...'; sleep 10; exit $code"]
