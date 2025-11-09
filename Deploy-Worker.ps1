$ErrorActionPreference = "Stop"

$tag = "v$(Get-Date -Format 'yyyy.MM.dd.HHmm')"
$image = "plannerregistry.azurecr.io/planner-worker:$tag"

Write-Host "Building image $image..."

docker build -f dockerfile.worker -t $image .

Write-Host "Pushing image..."
docker push $image

Write-Host "Updating Container App..."
az containerapp update `
    --name planner-worker `
    --resource-group PlannerRG `
    --image $image `
    --set-env-vars `
        "RabbitMq__Host=$(az containerapp show -g PlannerRG -n planner-rabbitmq --query properties.configuration.ingress.fqdn -o tsv)" `
        "RabbitMq__User=guest" `
        "RabbitMq__Pass=guest" `
        "RabbitMq__Port=5672"

Write-Host "Restarting worker..."
$rev = az containerapp revision list `
          --name planner-worker `
          --resource-group PlannerRG `
          --query "[?active==true].name | [0]" -o tsv

az containerapp revision restart `
          --name planner-worker `
          --resource-group PlannerRG `
          --revision $rev

Write-Host "✅ Deployment completed"
