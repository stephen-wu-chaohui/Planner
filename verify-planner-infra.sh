#!/bin/bash
# ============================================================
# 🔍 Planner Infrastructure Verification Script
# ============================================================

RG="PlannerRG"
SQL_SERVER="planner-sqlserver"
SQL_DB="PlannerDb"
RABBIT_CONTAINER="planner-rabbitmq"
SIGNALR="planner-signalr"
KV="planner-kv-sw"
APP_CONF="planner-appconfig"

echo "🔎 Checking Azure resources in Resource Group: $RG"
echo "----------------------------------------------"

# 1️⃣ Resource Group
if az group show --name $RG &>/dev/null; then
    echo "✅ Resource Group: $RG exists."
else
    echo "❌ Resource Group: $RG not found."
fi

# 2️⃣ SQL Server
if az sql server show -g $RG -n $SQL_SERVER &>/dev/null; then
    echo "✅ SQL Server: $SQL_SERVER found."
    SQL_FQDN=$(az sql server show -g $RG -n $SQL_SERVER --query fullyQualifiedDomainName -o tsv)
    echo "   FQDN: $SQL_FQDN"
else
    echo "❌ SQL Server: $SQL_SERVER not found."
fi

# 3️⃣ SQL Database
if az sql db show -g $RG -s $SQL_SERVER -n $SQL_DB &>/dev/null; then
    echo "✅ SQL Database: $SQL_DB exists."
else
    echo "❌ SQL Database: $SQL_DB missing."
fi

# 4️⃣ RabbitMQ Container
if az container show -g $RG -n $RABBIT_CONTAINER &>/dev/null; then
    echo "✅ RabbitMQ Container: $RABBIT_CONTAINER found."
    RABBIT_FQDN=$(az container show -g $RG -n $RABBIT_CONTAINER --query ipAddress.fqdn -o tsv)
    echo "   Host: $RABBIT_FQDN"
else
    echo "❌ RabbitMQ Container: $RABBIT_CONTAINER not found."
fi

# 5️⃣ SignalR Service
if az signalr show -g $RG -n $SIGNALR &>/dev/null; then
    echo "✅ SignalR Service: $SIGNALR exists."
    SIGNALR_HOST=$(az signalr show -g $RG -n $SIGNALR --query hostName -o tsv)
    echo "   Endpoint: $SIGNALR_HOST"
else
    echo "❌ SignalR Service: $SIGNALR missing."
fi

# 6️⃣ Key Vault
if az keyvault show -g $RG -n $KV &>/dev/null; then
    echo "✅ Key Vault: $KV found."
    KV_URI=$(az keyvault show -g $RG -n $KV --query properties.vaultUri -o tsv)
    echo "   URI: $KV_URI"
else
    echo "❌ Key Vault: $KV not found."
fi

# 7️⃣ App Configuration
if az appconfig show -g $RG -n $APP_CONF &>/dev/null; then
    echo "✅ App Configuration: $APP_CONF found."
    APP_CONF_EP=$(az appconfig show -g $RG -n $APP_CONF --query endpoint -o tsv)
    echo "   Endpoint: $APP_CONF_EP"
else
    echo "❌ App Configuration: $APP_CONF missing."
fi

echo "----------------------------------------------"
echo "🧾 Verification Complete."
echo
echo "💡 Next steps:"
echo " - If any resources show ❌, re-run the setup script for that component."
echo " - Use the endpoints above in Azure App Config & your application settings."
