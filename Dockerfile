# Multi-stage Dockerfile for Astrolabed DNS Engine (.NET 10)
# Force SDK stage to run natively on the build host platform to cross-compile fast
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Copy project files
COPY ["src/Astrolabed.EventBus/Astrolabed.EventBus.csproj", "Astrolabed.EventBus/"]
COPY ["src/Astrolabed.Data/Astrolabed.Data.csproj", "Astrolabed.Data/"]
COPY ["src/Astrolabed.Dns/Astrolabed.Dns.csproj", "Astrolabed.Dns/"]
COPY ["src/Astrolabed.Core/Astrolabed.Core.csproj", "Astrolabed.Core/"]
COPY ["src/Astrolabed.Main/Astrolabed.Main.csproj", "Astrolabed.Main/"]

COPY src/ .
WORKDIR "/src/Astrolabed.Main"

# Cross-compile for the targeted platform (x64 or arm64) using native host SDK
# ReadyToRun (RTR) generates platform-specific machine code during publish
RUN case "${TARGETARCH}" in \
        "amd64") DOTNET_ARCH="x64" ;; \
        "arm64") DOTNET_ARCH="arm64" ;; \
        *) DOTNET_ARCH="${TARGETARCH}" ;; \
    esac && \
    dotnet publish "Astrolabed.Main.csproj" \
        -c Release \
        -a "${DOTNET_ARCH}" \
        -o /app/publish \
        /p:UseAppHost=false \
        /p:PublishReadyToRun=true

# Runtime Stage automatically pulls the platform matching TARGETPLATFORM
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Ensure directories exist for hosts and blocklist volumes
USER root
RUN mkdir -p /etc/astrolabed/dns-hosts /etc/astrolabed/dns-lists /var/lib/astrolabed && \
    chown -R $APP_UID:$APP_UID /etc/astrolabed && chown -R $APP_UID:$APP_UID /var/lib/astrolabed

# High-Performance .NET DNS Environment Configuration
ENV DOCKER=true \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    LC_ALL=en_US.UTF-8 \
    DOTNET_gcServer=1 \
    DOTNET_GCDynamicAdaptationForMinMem=0 \
    DOTNET_SYSTEM_NET_SOCKETS_PERTHREAD_COMPLETION_PORT=1

COPY --from=build /app/publish .
RUN rm /app/publish/appsettings*.* -rf

ENTRYPOINT ["dotnet", "Astrolabed.Main.dll"]
