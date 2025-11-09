# -------------------- Dockerfile (auto-regenerated) --------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Planner.Optimization.Worker/Planner.Optimization.Worker.csproj", "Planner.Optimization.Worker/"]
RUN dotnet restore "Planner.Optimization.Worker/Planner.Optimization.Worker.csproj"
COPY . .
WORKDIR "/src/Planner.Optimization.Worker"
RUN dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishTrimmed=true -o /app/publish

FROM debian:bookworm-slim AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["./Planner.Optimization.Worker"]
# -----------------------------------------------------------------------
