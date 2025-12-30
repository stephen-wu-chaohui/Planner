# ============================
# Build stage
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution (for reference only, not restore)
COPY Planner.sln .

# Copy project files required by Worker restore graph
COPY src/Planner.Contracts/Planner.Contracts.csproj \
     src/Planner.Contracts/

COPY src/Planner.Messaging/Planner.Messaging.csproj \
     src/Planner.Messaging/

COPY src/Planner.Optimization/Planner.Optimization.csproj \
     src/Planner.Optimization/

COPY src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj \
     src/Planner.Optimization.Worker/

# Restore ONLY the Worker project
RUN dotnet restore \
    src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj

# Copy remaining source
COPY src/ src/

# Publish Worker
RUN dotnet publish \
    src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ============================
# Runtime stage
# ============================
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --home /app appuser
USER appuser

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Planner.Optimization.Worker.dll"]
