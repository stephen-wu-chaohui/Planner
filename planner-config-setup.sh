#!/bin/bash
# ============================================================
# 🔐 Planner Configuration Setup
# Creates Azure Key Vault + App Configuration
# Adds secure secrets and references for Planner project
# ============================================================

RG="PlannerRG"
LOC="australiaeast"
KV="planner-kv"
APP_CONF="planner-appconfig"

SQL_SERVER="planner-sqlserver"
SQL_ADMIN="planneradmin"
SQL_PASS="StrongPassword!123"   # same as your setup
SQL_DB="PlannerDb"

# We'll detect RabbitMQ + SignalR values dynamically if possible
RABBIT_CONTAINER="planner-rabbitmq"
SIGNALR="planner-signalr"

echo "🔒 Creating Key Vault..."
az keyvault create --name $KV --resource-group $RG --location $LOC

# --- Add secrets to Key Vault ---
echo "🗝️ Adding secrets to Key Vault..."
# SQL connection string
SQL_CONN="Server=tcp:$SQL_SERVER.database.windows.net,1433;Initial Catalog=$SQL_DB;User ID=$SQL_ADMIN;Password=$SQL_PASS;Encrypt=True;Connection Timeout=30;"
az keyvault secret set --vault-name $KV --name SqlConnectionString --value "$SQL_CONN"

# RabbitMQ password
az keyvault secret set --vault-name $KV --name RabbitPassword --value "planner123"

# SignalR connection string
SIGNALR_CONN=$(az signalr key list --name $SIGNALR --resource-group $RG --query primaryConnectionString -o tsv)
az keyvault secret set --vault-name $KV --name SignalRConnection --value "$SIGNALR_CONN"

# --- Create App Configuration store ---
echo "🗂️ Creating App Configuration..."
az appconfig create --name $APP_CONF --resource-group $RG --location $LOC --sku Standard

# --- Get Rabbit host FQDN ---
RABBIT_HOST=$(az container show -g $RG -n $RABBIT_CONTAINER --query ipAddress.fqdn -o tsv)

echo "📦 Adding non-secret settings to App Configuration..."
az appconfig kv set --name $APP_CONF --key "RabbitMq:Host" --value "$RABBIT_HOST"
az appconfig kv set --name $APP_CONF --key "RabbitMq:Username" --value "planner"
az appconfig kv set --name $APP_CONF --key "Sql:Server" --value "$SQL_SERVER.database.windows.net"
az appconfig kv set --name $APP_CONF --key "Sql:Database" --value "$SQL_DB"

echo "🔗 Linking Key Vault secrets as Key Vault references..."
az appconfig kv set-keyvault --name $APP_CONF --key "RabbitMq:Password" --secret-identifier $(az keyvault secret show --vault-name $KV --name RabbitPassword --query id -o tsv)
az appconfig kv set-keyvault --name $APP_CONF --key "ConnectionStrings:DefaultConnection" --secret-identifier $(az keyvault secret show --vault-name $KV --name SqlConnectionString --query id -o tsv)
az appconfig kv set-keyvault --name $APP_CONF --key "SignalR:ConnectionString" --secret-identifier $(az keyvault secret show --vault-name $KV --name SignalRConnection --query id -o tsv)

echo
echo "✅ Planner configuration setup complete!"
echo "-------------------------------------------"
echo "App Configuration: https://portal.azure.com/#resource/subscriptions/<YOUR_SUB_ID>/resourceGroups/$RG/providers/Microsoft.AppConfiguration/configurationStores/$APP_CONF"
echo "Key Vault:         https://portal.azure.com/#resource/subscriptions/<YOUR_SUB_ID>/resourceGroups/$RG/providers/Microsoft.KeyVault/vaults/$KV"
echo "-------------------------------------------"
echo "ℹ️ Next Steps:"
echo " - In each App Service (Planner.API, BlazorApp, Worker):"
echo "     AppConfig__Endpoint=https://$APP_CONF.azconfig.io"
echo "     AzureServicesAuthConnectionString=RunAs=ManagedIdentity"
echo " - Enable System Assigned Managed Identity for those apps"
echo " - Verify Key Vault access policy includes those identities"
