FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /tests

COPY LanguageTutor.E2E.csproj ./
RUN dotnet restore

COPY . ./

ENTRYPOINT ["dotnet", "test", "--no-restore", "--logger", "trx;LogFileName=selenium.trx", "--results-directory", "/test-results"]
