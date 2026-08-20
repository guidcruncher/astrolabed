# Multi-stage Dockerfile for Astrolabed DNS Engine (.NET 10)
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Astrolabed.Dns/Astrolabed.Dns.csproj", "Astrolabed.Dns/"]
RUN dotnet restore "Astrolabed.Dns/Astrolabed.Dns.csproj"

COPY src/ .
WORKDIR "/src/Astrolabed.Dns"
RUN dotnet publish "Astrolabed.Dns.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

RUN mkdir -p /etc/astrolabed/dns-hosts
RUN mkdir -p /etc/astrolabed/dns-lists

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    LC_ALL=en_US.UTF-8

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Astrolabed.Dns.dll"]
