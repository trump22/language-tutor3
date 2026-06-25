FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /tests

COPY LanguageTutor.E2E.csproj ./
RUN dotnet restore

COPY . ./

ENTRYPOINT ["/bin/sh", "-c", "mkdir -p /test-results/allure-results && printf '{\"allure\":{\"directory\":\"/test-results/allure-results\"}}' > /test-results/allureConfig.json && ALLURE_CONFIG=/test-results/allureConfig.json dotnet test --no-restore --logger 'trx;LogFileName=selenium.trx' --results-directory /test-results -- xUnit.ReporterSwitch=allure"]
