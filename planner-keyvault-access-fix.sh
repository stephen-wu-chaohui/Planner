#!/bin/bash
# ============================================================
# 🧰 Fix Key Vault Access for Current User or Managed Identity
# ============================================================

RG="PlannerRG"
KV="planner-kv-sw"

echo "🔍 Checking Key Vault access model..."
RBAC_ENABLED=$(az keyvault show -g $RG -n $KV --query "properties.enableRbacAuthorization" -o tsv 2>/dev/null)

if [ "$RBAC_ENABLED" = "true" ]; then
  echo "✅ Key Vault $KV uses RBAC authorization."
else
  echo "ℹ️ Key Vault $KV uses Access Policy authorization."
fi

# Detect current identity
echo "👤 Detecting current Azure CLI identity..."
CURRENT_USER_UPN=$(az ad signed-in-user show --query userPrincipalName -o tsv 2>/dev/null)
CURRENT_USER_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null)

if [ -n "$CURRENT_USER_UPN" ]; then
  echo "   Logged in as user: $CURRENT_USER_UPN"
  echo "   Object ID: $CURRENT_USER_ID"
else
  # Fall back to managed identity case (like when running inside App Service)
  MSI_CLIENT_ID=$(az account show --query user.name -o tsv 2>/dev/null)
  echo "   Logged in as managed identity: $MSI_CLIENT_ID"
fi

# Apply permissions depending on model
if [ "$RBAC_ENABLED" = "true" ]; then
  echo "🔑 Assigning 'Key Vault Administrator' role (RBAC)..."
  az role assignment create \
    --role "Key Vault Administrator" \
    --assignee-object-id "$CURRENT_USER_ID" \
    --scope $(az keyvault show -n $KV -g $RG --query id -o tsv) \
    >/dev/null 2>&1

  echo "✅ RBAC role assignment applied for $CURRENT_USER_UPN"
else
  echo "🔑 Adding access policy for secrets..."
  az keyvault set-policy \
    --name $KV \
    --upn "$CURRENT_USER_UPN" \
    --secret-permissions get list set delete \
    >/dev/null 2>&1

  echo "✅ Access policy added for $CURRENT_USER_UPN"
fi

# Verify result
echo
echo "🧾 Current access check:"
az keyvault show -n $KV -g $RG --query "properties.accessPolicies[].objectId" -o tsv | grep "$CURRENT_USER_ID" >/dev/null \
  && echo "✅ Confirmed: you have access to $KV" \
  || echo "⚠️ Please wait a minute for Azure RBAC propagation or verify manually in portal."

echo
echo "💡 Next:"
echo " - Re-run your Key Vault init script:"
echo "     bash planner-keyvault-init.sh"
echo " - If still forbidden, run again in 1–2 minutes (RBAC can take a bit to propagate)."
