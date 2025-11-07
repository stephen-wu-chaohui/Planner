#!/bin/bash
# ============================================================
# Register all required Azure resource providers for Planner
# ============================================================

echo "🔧 Registering required Azure resource providers..."
echo

# Core data + messaging + runtime services
az provider register --namespace Microsoft.Sql
az provider register --namespace Microsoft.ContainerInstance
az provider register --namespace Microsoft.SignalRService
az provider register --namespace Microsoft.AppConfiguration
az provider register --namespace Microsoft.KeyVault
az provider register --namespace Microsoft.Web
az provider register --namespace Microsoft.OperationalInsights
az provider register --namespace Microsoft.Insights

echo
echo "⏳ Checking registration status..."
for ns in Microsoft.Sql Microsoft.ContainerInstance Microsoft.SignalRService Microsoft.AppConfiguration Microsoft.KeyVault Microsoft.Web Microsoft.OperationalInsights Microsoft.Insights
do
  state=$(az provider show --namespace $ns --query "registrationState" -o tsv)
  echo "$ns : $state"
done

echo
echo "✅ All required Azure namespaces registered (or pending registration)."
echo "   If any show 'Registering', wait 1–2 minutes and re-run this check:"
echo "   az provider show --namespace Microsoft.Sql --query registrationState"
