# syntax=docker/dockerfile:1

FROM node:22.23.5-alpine3.24 AS web
WORKDIR /web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0.404-noble AS api
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/BlueHeeler.Domain/BlueHeeler.Domain.csproj src/BlueHeeler.Domain/
COPY src/BlueHeeler.Api/BlueHeeler.Api.csproj src/BlueHeeler.Api/
RUN dotnet restore src/BlueHeeler.Api/BlueHeeler.Api.csproj
COPY src/ src/
RUN dotnet publish src/BlueHeeler.Api/BlueHeeler.Api.csproj -c Release --no-restore -o /app

# Chiseled: no shell or package manager, and it runs as the non-root "app" user by default.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble-chiseled
WORKDIR /app
COPY --from=api /app ./
COPY --from=web /web/dist ./wwwroot
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "BlueHeeler.Api.dll"]
