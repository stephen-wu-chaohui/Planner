# ============================
# Build stage
# ============================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ARG TARGETARCH=amd64

# Copy project files required by Worker restore graph
COPY src/Planner.Contracts/Planner.Contracts.csproj \
     src/Planner.Contracts/

COPY src/Planner.Messaging/Planner.Messaging.csproj \
     src/Planner.Messaging/

COPY src/Planner.Optimization/Planner.Optimization.csproj \
     src/Planner.Optimization/

COPY src/Planner.Optimization.Contracts/Planner.Optimization.Contracts.csproj \
     src/Planner.Optimization.Contracts/

COPY src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj \
     src/Planner.Optimization.Worker/

# Restore ONLY the Worker project
RUN case "$TARGETARCH" in \
        amd64) TARGETRID=linux-x64 ;; \
        arm64) TARGETRID=linux-arm64 ;; \
        *) echo "Unsupported TARGETARCH: $TARGETARCH" && exit 1 ;; \
    esac \
    && dotnet restore \
        src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj \
        -r "$TARGETRID"

# Copy remaining source
COPY src/ src/

# Publish Worker
RUN case "$TARGETARCH" in \
        amd64) TARGETRID=linux-x64 ;; \
        arm64) TARGETRID=linux-arm64 ;; \
        *) echo "Unsupported TARGETARCH: $TARGETARCH" && exit 1 ;; \
    esac \
    && dotnet publish \
        src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj \
        -c Release \
        -r "$TARGETRID" \
        --self-contained false \
        -o /app/publish \
        --no-restore

# ============================
# Runtime stage
# ============================
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
# Rely on the built-in Microsoft user
USER app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Planner.Optimization.Worker.dll"]
