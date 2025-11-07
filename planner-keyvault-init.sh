#!/bin/bash
# ============================================================
# 🔐 Initialize existing Planner Key Vault
# ============================================================

RG="PlannerRG"
KV="planner-kv-sw"
SQL_SERVER="planner-sqlserver"
SQL_DB="PlannerDb"
SQL_ADMIN="planneradmin"
SQL_PASS="StrongPassword!123"     # same as before
SIGNALR="planner-signalr"

echo "🔎 Verifying Key Vault..."
az keyvault show -g $RG -n $KV --query properties.vaultUri -o tsv || {
  echo "❌ Key Vault $KV not found. Please double-check the name."
  exit 1
}

# --- Build connection strings ---
SQL_CONN="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Initial Catalog=${SQL_DB};User ID=${SQL_ADMIN};Password=${SQL_PASS};Encrypt=True;Connection Timeout=30;"

RABBIT_PASS="planner123"
SIGNALR_CONN=$(az signalr key list --name $SIGNALR --resource-group $RG --query primaryConnectionString -o tsv)

# --- Upload secrets ---
echo "🔐 Uploading secrets to Key Vault: $KV ..."
az keyvault secret set --vault-name $KV --name SqlConnectionString --value "$SQL_CONN"
az keyvault secret set --vault-name $KV --name RabbitPassword --value "$RABBIT_PASS"
az keyvault secret set --vault-name $KV --name SignalRConnection --value "$SIGNALR_CONN"

echo
echo "✅ Secrets stored successfully:"
echo "-------------------------------------------"
echo "SQL Connection:      $(az keyvault secret show --vault-name $KV --name SqlConnectionString --query id -o tsv)"
echo "RabbitMQ Password:   $(az keyvault secret show --vault-name $KV --name RabbitPassword --query id -o tsv)"
echo "SignalR Connection:  $(az keyvault secret show --vault-name $KV --name SignalRConnection --query id -o tsv)"
echo "-------------------------------------------"
echo "💡 Next:"
echo " - Link these secret URIs into Azure App Configuration"
echo " - Or, if using Program.cs, let DefaultAzureCredential resolve them automatically."
