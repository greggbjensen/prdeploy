FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Base image modifications...
#
RUN apt-get update && apt-get install -y \
    curl

# Download and install the Tracer
RUN mkdir -p /opt/datadog \
    && mkdir -p /var/log/datadog \
    && TRACER_VERSION=$(curl -s https://api.github.com/repos/DataDog/dd-trace-dotnet/releases/latest | grep tag_name | cut -d '"' -f 4 | cut -c2-) \
    && curl -LO https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb \
    && dpkg -i ./datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb \
    && rm ./datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb \
    && /opt/datadog/createLogPath.sh

# Enable the tracer
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/opt/datadog
ENV DD_INTEGRATIONS=/opt/datadog/integrations.json

# Enable profiling
ENV LD_PRELOAD=/opt/datadog/continuousprofiler/Datadog.Linux.ApiWrapper.x64.so
ENV DD_PROFILING_ENABLED=1
ENV DD_ENV=dev
ENV DD_VERSION=1.0.0

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG NUGET_PAT
ARG VERSION=1.0.0
WORKDIR /src
COPY ["prdeploy-api/nuget.config", "prdeploy-api/"]
COPY ["prdeploy-api/src/PrDeploy.Api/PrDeploy.Api.csproj", "prdeploy-api/src/PrDeploy.Api/"]
COPY ["prdeploy-api/src/PrDeploy.Api.Business/PrDeploy.Api.Business.csproj", "prdeploy-api/src/PrDeploy.Api.Business/"]
COPY ["prdeploy-api/src/PrDeploy.Api.Models/PrDeploy.Api.Models.csproj", "prdeploy-api/src/PrDeploy.Api.Models/"]
COPY [".nuget/packages", ".nuget/packages"]

RUN dotnet restore "prdeploy-api/src/PrDeploy.Api/PrDeploy.Api.csproj" -r linux-x64 -p:Version=$VERSION -p:PublishReadyToRun=true

COPY "prdeploy-api" "prdeploy-api"
WORKDIR "/src/prdeploy-api/src/PrDeploy.Api"
RUN dotnet build "PrDeploy.Api.csproj" --no-restore -c Release -r linux-x64 --no-self-contained -p:Version=$VERSION -p:PublishReadyToRun=true

FROM build AS publish
RUN dotnet publish "PrDeploy.Api.csproj" --no-build --no-restore -c Release -o /app/publish -r linux-x64 --no-self-contained -p:Version=$VERSION -p:PublishReadyToRun=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PrDeploy.Api.dll"]
