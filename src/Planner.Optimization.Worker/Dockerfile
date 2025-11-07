# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj", "src/Planner.Optimization.Worker/"]
COPY ["src/Planner.Messaging/Planner.Messaging.csproj", "src/Planner.Messaging/"]
COPY ["src/Planner.Contracts/Planner.Contracts.csproj", "src/Planner.Contracts/"]
RUN dotnet restore "src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj"
COPY . .
WORKDIR "/src/src/Planner.Optimization.Worker"
RUN dotnet publish -c Release -o /app/publish
