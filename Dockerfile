# ============================================================
# Dockerfile — GasStation MS-ի համար (Render.com deploy)
# Multi-stage build՝ փոքր վերջնական image-ի համար
# ============================================================

# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj և restore (cache-ի օպտիմալացման համար)
COPY GasStationMS.csproj ./
RUN dotnet restore

# Copy մնացած ֆայլերը և publish
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Render-ը տրամադրում է PORT environment variable
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "GasStationMS.dll"]
