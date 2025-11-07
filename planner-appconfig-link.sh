#!/bin/bash
# ============================================================
# 🗂️ Planner App Configuration Setup & Key Vault Linking
# ============================================================

RG="PlannerRG"
LOC="australiaeast"
APP_CONF="planner-appconfig"
KV="planner-kv-sw"
SQL_SERVER="planner-sqlserver"
SQL_DB="PlannerDb"
RABBIT_CONTAINER="planner-rabbitmq"

echo "🔍 Checking if App Configuration store exists..."
APP_CONF_ID=$(az appconfig show -n $APP_CONF -g $RG --query id -o tsv 2>/dev/null)

if [ -z "$APP_CONF_ID" ]; then
  echo "⚠️ App Configuration not found. Creating $APP_CONF..."
  az appconfig create --name $APP_CONF --resource-group $RG --location $LOC --sku Standard
else
  echo "✅ Found App Configuration store: $APP_CONF"
fi

# --- Get Rabbit host ---
RABBIT_HOST=$(az container show -g $RG -n $RABBIT_CONTAINER --query ipAddress.fqdn -o tsv)
KV_URI="https://${KV}.vault.azure.net"

echo
echo "📦 Adding non-secret configuration keys..."
az appconfig kv set --name $APP_CONF --key "RabbitMq:Host" --value "$RABBIT_HOST"
az appconfig kv set --name $APP_CONF --key "RabbitMq:Username" --value "planner"
az appconfig kv set --name $APP_CONF --key "Sql:Server" --value "${SQL_SERVER}.database.windows.net"
az appconfig kv set --name $APP_CONF --key "Sql:Database" --value "$SQL_DB"

echo
echo "🔗 Linking Key Vault secrets as Key Vault references..."
az appconfig kv set-keyvault --name $APP_CONF --key "RabbitMq:Password" \
  --secret-identifier $(az keyvault secret show --vault-name $KV --name RabbitPassword --query id -o tsv)

az appconfig kv set-keyvault --name $APP_CONF --key "ConnectionStrings:DefaultConnection" \
  --secret-identifier $(az keyvault secret show --vault-name $KV --name SqlConnectionString --query id -o tsv)

az appconfig kv set-keyvault --name $APP_CONF --key "SignalR:ConnectionString" \
  --secret-identifier $(az keyvault secret show --vault-name $KV --name SignalRConnection --query id -o tsv)

echo
echo "✅ App Configuration and Key Vault integration complete!"
echo "-------------------------------------------"
APP_CONF_ENDPOINT=$(az appconfig show -n $APP_CONF -g $RG --query endpoint -o tsv)
echo "App Configuration: $APP_CONF_ENDPOINT"
echo "Key Vault:         $KV_URI"
echo "-------------------------------------------"
echo "💡 Next:"
echo " - Add AppConfig__Endpoint=$APP_CONF_ENDPOINT to your App Service environment settings"
echo " - Ensure AzureServicesAuthConnectionString=RunAs=ManagedIdentity"
echo " - Enable System Assigned Managed Identity on each app"
echo " - Your apps will now auto-load all settings at runtime 🎯"
