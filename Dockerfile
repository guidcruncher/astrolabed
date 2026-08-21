# Multi-stage Dockerfile for Astrolabed DNS Engine (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Astrolabed.EventBus/Astrolabed.EventBus.csproj", "Astrolabed.EventBus/"]
RUN dotnet restore "Astrolabed.EventBus/Astrolabed.EventBus.csproj"
COPY ["src/Astrolabed.Data/Astrolabed.Data.csproj", "Astrolabed.Data/"]
RUN dotnet restore "Astrolabed.Data/Astrolabed.Data.csproj"
COPY ["src/Astrolabed.Dns/Astrolabed.Dns.csproj", "Astrolabed.Dns/"]
RUN dotnet restore "Astrolabed.Dns/Astrolabed.Dns.csproj"

COPY ["src/Astrolabed.Core/Astrolabed.Core.csproj", "Astrolabed.Core/"]
RUN dotnet restore "Astrolabed.Core/Astrolabed.Core.csproj"

COPY src/ .
WORKDIR "/src/Astrolabed.Core"

# Build with ReadyToRun (RTR) to reduce startup JIT delay & enable dynamic PGO
RUN dotnet publish "Astrolabed.Core.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=true

# Runtime Stage
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
    # GC & Thread Optimizations for UDP Processing
    DOTNET_gcServer=1 \
    DOTNET_GCDynamicAdaptationForMinMem=0 \
    DOTNET_SYSTEM_NET_SOCKETS_PERTHREAD_COMPLETION_PORT=1

COPY --from=build /app/publish .
RUN rm /app/publish/appsettings*.* -rf

# Run as non-root app user
# USER $APP_UID

ENTRYPOINT ["dotnet", "Astrolabed.Core.dll"]
