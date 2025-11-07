#!/bin/bash
# ============================================================
# 🚀 Planner Infrastructure Setup
# Region: australiaeast
# Components: Azure SQL, RabbitMQ (ACI), SignalR
# ============================================================

# --- Configurable variables ---
RG="PlannerRG"
LOC="australiaeast"
SQL_SERVER="planner-sqlserver"
SQL_ADMIN="planneradmin"
SQL_PASS="StrongPassword!123"   # Change before running
SQL_DB="PlannerDb"
RABBIT_CONTAINER="planner-rabbitmq"
SIGNALR="planner-signalr"

# --- Create resource group ---
echo "📦 Creating Resource Group..."
az group create --name $RG --location $LOC

# --- Create Azure SQL Server + Database ---
echo "🧱 Creating Azure SQL Server..."
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RG \
  --location $LOC \
  --admin-user $SQL_ADMIN \
  --admin-password "$SQL_PASS"

echo "🗄️ Creating Azure SQL Database..."
az sql db create \
  --resource-group $RG \
  --server $SQL_SERVER \
  --name $SQL_DB \
  --service-objective S0

# --- Configure firewall to allow Azure services ---
echo "🌐 Enabling access for Azure services..."
az sql server firewall-rule create \
  --resource-group $RG \
  --server $SQL_SERVER \
  --name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0


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

# --- Deploy Azure SignalR Service ---
echo "🔔 Creating Azure SignalR Service..."
az signalr create \
  --name $SIGNALR \
  --resource-group $RG \
  --location $LOC \
  --sku Standard_S1 \
  --unit-count 1

# --- Output connection info ---
echo
echo "✅ Setup complete!"
echo "-------------------------------------------"
echo "SQL Server:    $SQL_SERVER.database.windows.net"
echo "SQL Database:  $SQL_DB"
echo "RabbitMQ UI:   http://$(az container show -g $RG -n $RABBIT_CONTAINER --query ipAddress.fqdn -o tsv):15672"
echo "RabbitMQ AMQP: amqp://planner:planner123@$(az container show -g $RG -n $RABBIT_CONTAINER --query ipAddress.fqdn -o tsv):5672"
echo "SignalR:       $(az signalr show -n $SIGNALR -g $RG --query hostName -o tsv)"
echo "-------------------------------------------"
echo "ℹ️ Next Steps:"
echo " - Store the SQL connection string & RabbitMQ password in Azure Key Vault"
echo " - Store non-secret settings (Rabbit host, SignalR endpoint) in Azure App Configuration"
echo " - Update your App Service app settings or environment variables accordingly"
