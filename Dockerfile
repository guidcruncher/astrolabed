# Build stage

# Website build
FROM node:22-alpine AS client-build
WORKDIR /src

COPY ./src/ClientUI/package*.json ./
RUN npm ci

COPY ./src/ClientUI/ ./
RUN npm run build

# Dotnet build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
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

RUN mkdir -p /app/ClientUI
COPY --from=dotnet-build /out .
COPY --from=client-build ./src/dist /app/ClientUI

# Run DNS forwarder
RUN mkdir -p /var/lib/astrolabed
RUN mkdir -p /etc/astrolabed/rules
RUN mkdir -p /etc/astrolabed/hosts

ENTRYPOINT ["dotnet", "Astrolabed.dll"]
