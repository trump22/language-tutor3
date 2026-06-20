FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY languagetutor.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
RUN mkdir -p /app/uploads

COPY --from=build /app/publish ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "languagetutor.dll"]
