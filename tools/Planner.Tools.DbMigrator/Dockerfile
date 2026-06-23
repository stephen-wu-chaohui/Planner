# -------- Build stage --------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY tools/Planner.Tools.DbMigrator/Planner.Tools.DbMigrator.csproj tools/Planner.Tools.DbMigrator/
COPY src/Planner.Application/Planner.Application.csproj src/Planner.Application/
COPY src/Planner.Domain/Planner.Domain.csproj src/Planner.Domain/
COPY src/Planner.Infrastructure/Planner.Infrastructure.csproj src/Planner.Infrastructure/

RUN dotnet restore tools/Planner.Tools.DbMigrator/Planner.Tools.DbMigrator.csproj

COPY src/ src/
COPY tools/Planner.Tools.DbMigrator/ tools/Planner.Tools.DbMigrator/

RUN dotnet publish tools/Planner.Tools.DbMigrator/Planner.Tools.DbMigrator.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# -------- Runtime stage --------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
USER app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Planner.Tools.DbMigrator.dll"]
CMD ["migrate"]
