# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first for layer caching
COPY src/TodoApi/TodoApi.csproj src/TodoApi/
COPY tests/TodoApi.Tests/TodoApi.Tests.csproj tests/TodoApi.Tests/
COPY TodoApi.sln .

RUN dotnet restore TodoApi.sln

# Copy remaining source
COPY src/ src/
COPY tests/ tests/

# Build
RUN dotnet build TodoApi.sln --configuration Release --no-restore

# Run tests inside the build stage
RUN dotnet test tests/TodoApi.Tests/TodoApi.Tests.csproj \
    --configuration Release \
    --no-build \
    --verbosity normal

# Publish
RUN dotnet publish src/TodoApi/TodoApi.csproj \
    --configuration Release \
    --no-build \
    --output /app/publish

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

ENTRYPOINT ["dotnet", "TodoApi.dll"]
