# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ./src ./src
COPY ./tests ./tests
COPY Astrolabed.sln .

RUN dotnet restore
RUN dotnet publish src/Astrolabed/Astrolabed.csproj -c Release -o /out
RUN rm /out/appsettings.* 

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /out .

# Run DNS forwarder
RUN mkdir -p /var/lib/astrolabed
RUN mkdir -p /etc/astrolabed/rules
RUN mkdir -p /etc/astrolabed/hosts

ENTRYPOINT ["dotnet", "Astrolabed.dll"]
