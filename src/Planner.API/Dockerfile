# ==============================================================
# Planner.API Dockerfile  (kept for Option B)
# ==============================================================

# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["Planner.API/Planner.API.csproj", "Planner.API/"]
RUN dotnet restore "Planner.API/Planner.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR /src/Planner.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Planner.API.dll"]
