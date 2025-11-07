#!/bin/bash
# ============================================================
# 🔐 Planner Key Vault Access Setup (Extended)
# Grants admin access to your user + read access to App Service identities
# ============================================================

RG="PlannerRG"
KV="planner-kv-sw"
VAULT_ID=$(az keyvault show -n $KV -g $RG --query id -o tsv)

# --- Check existence ---
if [ -z "$VAULT_ID" ]; then
  echo "❌ Key Vault '$KV' not found in Resource Group '$RG'."
  exit 1
fi

echo "🔎 Checking Key Vault authorization model..."
IS_RBAC=$(az keyvault show -n $KV --query properties.enableRbacAuthorization -o tsv)
echo "   -> RBAC authorization: $IS_RBAC"

# --- Detect signed-in user ---
echo "👤 Detecting current Azure identity..."
USER_UPN=$(az ad signed-in-user show --query userPrincipalName -o tsv 2>/dev/null)
USER_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null)

if [ -n "$USER_UPN" ]; then
  echo "✅ Signed in as user: $USER_UPN"
else
  echo "⚠️ No signed-in user detected (CLI service principal or managed identity)."
fi

# --- Apply permissions for user ---
if [ "$IS_RBAC" == "true" ]; then
  echo "🪄 Using RBAC: Assigning 'Key Vault Administrator' role to you..."
  az role assignment create \
    --role "Key Vault Administrator" \
    --assignee "$USER_UPN" \
    --scope "$VAULT_ID" \
    --only-show-errors
else
  echo "🧾 Using Access Policies: granting secret admin permissions to you..."
  az keyvault set-policy \
    --name $KV \
    --resource-group $RG \
    --upn "$USER_UPN" \
    --secret-permissions get list set delete \
    --only-show-errors
fi

# --- Grant read-only access to App Service identities ---
echo
echo "🔗 Checking for App Services to grant read access..."
for APP in planner-api planner-blazorapp planner-optimization-worker
do
  APP_ID=$(az webapp identity show -g $RG -n $APP --query principalId -o tsv 2>/dev/null)
  if [ -n "$APP_ID" ]; then
    echo "✅ Found managed identity for: $APP"
    if [ "$IS_RBAC" == "true" ]; then
      az role assignment create \
        --role "Key Vault Secrets User" \
        --assignee-object-id "$APP_ID" \
        --scope "$VAULT_ID" \
        --only-show-errors
      echo "   → Assigned 'Key Vault Secrets User' (RBAC)"
    else
      az keyvault set-policy \
        --name $KV \
        --object-id "$APP_ID" \
        --secret-permissions get list \
        --only-show-errors
      echo "   → Granted 'get,list' access (Access Policy)"
    fi
  else
    echo "⚠️ App Service '$APP' not found or identity not enabled."
  fi
done

echo
echo "✅ Key Vault permission setup complete!"
echo "-------------------------------------------"
echo "Vault:       $KV"
echo "Authorization Model: $([ "$IS_RBAC" == "true" ] && echo "RBAC" || echo "Access Policy")"
echo "User Admin:  $USER_UPN"
echo "Apps:        planner-api, planner-blazorapp, planner-optimization-worker"
echo "-------------------------------------------"
echo "💡 Next step:  bash planner-keyvault-init.sh"
