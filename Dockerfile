# ============================================================
# BUILD
# ============================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy toàn bộ source code vào container
COPY . .

# Restore API và các project mà API sử dụng
RUN dotnet restore "src/ClinicManagementSystem.Api/ClinicManagementSystem.Api.csproj"

# Build + publish API
RUN dotnet publish \
    "src/ClinicManagementSystem.Api/ClinicManagementSystem.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false


# ============================================================
# RUNTIME
# ============================================================

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ClinicManagementSystem.Api.dll"]