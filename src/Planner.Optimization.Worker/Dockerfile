# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj for restore optimization
COPY ["src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj", "src/Planner.Optimization.Worker/"]
COPY ["src/Planner.Contracts/Planner.Contracts.csproj", "src/Planner.Contracts/"]
COPY ["src/Planner.Domain/Planner.Domain.csproj", "src/Planner.Domain/"]
COPY ["src/Planner.Infrastructure/Planner.Infrastructure.csproj", "src/Planner.Infrastructure/"]
COPY ["src/Planner.Application/Planner.Application.csproj", "src/Planner.Application/"]

RUN dotnet restore "src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj"

# Copy everything
COPY . .

RUN dotnet publish "src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj" \
    -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Planner.Optimization.Worker.dll"]
