param(
  [string]$Tag = "planner-optimization-worker:dev",
  [string]$ContainerName = "planner-optimization-worker"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Write-Host "Building image $Tag..."
docker build -f src/Planner.Optimization.Worker/Dockerfile -t $Tag .

Write-Host "Restarting container $ContainerName..."
docker rm -f $ContainerName 2>$null | Out-Null
docker run -d --name $ContainerName --restart unless-stopped `
  -e RabbitMq__Host=host.docker.internal `
  -e RabbitMq__Port=5672 `
  -e RabbitMq__User=guest `
  -e RabbitMq__Pass=guest `
  $Tag | Out-Null

Write-Host "Done. Recent logs:"
docker logs --tail 30 $ContainerName
