# syntax=docker/dockerfile:1

# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything (simple + reliable for small repos)
COPY . .

RUN dotnet restore src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj
RUN dotnet publish src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj -c Release -o /app/publish

# Run
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Worker reads shared.appsettings.json from AppContext.BaseDirectory
ENTRYPOINT ["dotnet", "Planner.Optimization.Worker.dll"]
