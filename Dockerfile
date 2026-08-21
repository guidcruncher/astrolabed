# Multi-stage Dockerfile for Astrolabed DNS Engine (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Astrolabed.Dns/Astrolabed.Dns.csproj", "Astrolabed.Dns/"]
RUN dotnet restore "Astrolabed.Dns/Astrolabed.Dns.csproj"

COPY src/ .
WORKDIR "/src/Astrolabed.Dns"

# Build with ReadyToRun (RTR) to reduce startup JIT delay & enable dynamic PGO
RUN dotnet publish "Astrolabed.Dns.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=true

# Runtime Stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Ensure directories exist for hosts and blocklist volumes
USER root
RUN mkdir -p /etc/astrolabed/dns-hosts /etc/astrolabed/dns-lists && \
    chown -R $APP_UID:$APP_UID /etc/astrolabed

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
