# --- Configurable variables ---
RG="PlannerRG"
LOC="australiaeast"
SQL_SERVER="planner-sqlserver"
SQL_ADMIN="planneradmin"
SQL_PASS="StrongPassword!123"   # Change before running
SQL_DB="PlannerDb"
RABBIT_CONTAINER="planner-rabbitmq"
SIGNALR="planner-signalr"

# --- Deploy RabbitMQ in Azure Container Instance ---
echo "🐇 Deploying RabbitMQ container..."
az container create \
  --name $RABBIT_CONTAINER \
  --resource-group $RG \
  --image rabbitmq:3-management \
  --cpu 1 --memory 1.5 \
  --os-type Linux \
  --ports 5672 15672 \
  --dns-name-label planner-rabbit-$RANDOM \
  --location $LOC \
  --environment-variables \
    RABBITMQ_DEFAULT_USER=planner \
    RABBITMQ_DEFAULT_PASS=planner123
