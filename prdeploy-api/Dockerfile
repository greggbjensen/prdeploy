FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG VERSION=1.0.0
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["src/PrDeploy.Api/PrDeploy.Api.csproj", "src/PrDeploy.Api/"]
COPY ["src/PrDeploy.Api.Business/PrDeploy.Api.Business.csproj", "src/PrDeploy.Api.Business/"]
COPY ["src/PrDeploy.Api.Models/PrDeploy.Api.Models.csproj", "src/PrDeploy.Api.Models/"]

RUN dotnet restore "src/PrDeploy.Api/PrDeploy.Api.csproj" -r linux-x64 -p:Version=$VERSION -p:PublishReadyToRun=true

COPY . .
WORKDIR "/src/src/PrDeploy.Api"
RUN dotnet build "PrDeploy.Api.csproj" --no-restore -c Release -r linux-x64 --no-self-contained -p:Version=$VERSION -p:PublishReadyToRun=true

FROM build AS publish
RUN dotnet publish "PrDeploy.Api.csproj" --no-build --no-restore -c Release -o /app/publish -r linux-x64 --no-self-contained -p:Version=$VERSION -p:PublishReadyToRun=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PrDeploy.Api.dll"]
