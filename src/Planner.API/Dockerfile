# -------- Build stage --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution
COPY . .

# Restore
RUN dotnet restore src/Planner.API/Planner.API.csproj

# Publish
RUN dotnet publish src/Planner.API/Planner.API.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# -------- Runtime stage --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Azure Container Apps listens on 8080 by convention
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Planner.API.dll"]
