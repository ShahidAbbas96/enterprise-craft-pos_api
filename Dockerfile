# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, isolated by csproj-only copy, so a source change doesn't invalidate this layer.
COPY src/RetailCommerce.Domain/RetailCommerce.Domain.csproj src/RetailCommerce.Domain/
COPY src/RetailCommerce.Application/RetailCommerce.Application.csproj src/RetailCommerce.Application/
COPY src/RetailCommerce.Infrastructure/RetailCommerce.Infrastructure.csproj src/RetailCommerce.Infrastructure/
COPY src/RetailCommerce.Api/RetailCommerce.Api.csproj src/RetailCommerce.Api/
RUN dotnet restore src/RetailCommerce.Api/RetailCommerce.Api.csproj

COPY src/ src/
RUN dotnet publish src/RetailCommerce.Api/RetailCommerce.Api.csproj -c Release -o /app --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user — the image otherwise runs as root by default.
RUN adduser --disabled-password --gecos "" appuser
COPY --from=build /app .
RUN mkdir -p /app/logs && chown -R appuser:appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "RetailCommerce.Api.dll"]
