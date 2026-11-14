# Imagen única de producción: la API de ASP.NET Core sirve también el frontend compilado (wwwroot).
# Uso y variables de entorno: docs/proceso/despliegue.md

# 1) Frontend
FROM node:24-alpine AS frontend
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# 2) Backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/SistemaTaller.Api/SistemaTaller.Api.csproj SistemaTaller.Api/
RUN dotnet restore SistemaTaller.Api/SistemaTaller.Api.csproj
COPY backend/SistemaTaller.Api/ SistemaTaller.Api/
RUN dotnet publish SistemaTaller.Api/SistemaTaller.Api.csproj -c Release -o /app --no-restore

# 3) Imagen final
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend /app ./
COPY --from=frontend /src/dist ./wwwroot
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "SistemaTaller.Api.dll"]
