#!/bin/bash
# ============================================================
# 🔐 Planner Key Vault RBAC Access Setup (Final Version)
# Grants admin access to your user + read access to App Services
# ============================================================

# --- Configuration ---
RG="PlannerRG"
KV="planner-kv-sw"
VAULT_ID=$(az keyvault show -n $KV -g $RG --query id -o tsv)

echo "🔎 Checking Key Vault existence..."
if [ -z "$VAULT_ID" ]; then
  echo "❌ Key Vault '$KV' not found in resource group '$RG'."
  exit 1
fi
echo "✅ Found Key Vault: $KV"

# --- Confirm RBAC is enabled ---
IS_RBAC=$(az keyvault show -n $KV --query properties.enableRbacAuthorization -o tsv)
if [ "$IS_RBAC" != "true" ]; then
  echo "⚠️ This Key Vault does not have RBAC authorization enabled."
  echo "Please enable it in the Azure Portal or via:"
  echo "az keyvault update -n $KV -g $RG --enable-rbac-authorization true"
  exit 1
fi
echo "🔐 RBAC authorization confirmed."

# --- Detect signed-in user ---
USER_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null)
USER_UPN=$(az ad signed-in-user show --query userPrincipalName -o tsv 2>/dev/null)

if [ -z "$USER_ID" ]; then
  echo "⚠️ Unable to detect signed-in user. Are you logged in with Azure CLI?"
  echo "Run 'az login' first."
  exit 1
fi

echo "👤 Detected user: $USER_UPN"
echo "🪄 Assigning Key Vault Administrator role..."
az role assignment create \
  --role "Key Vault Administrator" \
  --assignee-object-id "$USER_ID" \
  --assignee-principal-type User \
  --scope "$VAULT_ID" \
  --only-show-errors || echo "ℹ️ Role may already exist, continuing..."

# --- Grant read-only access to App Service managed identities ---
echo
echo "🔗 Granting read-only access to App Services (if available)..."
for APP in planner-api planner-blazorapp planner-optimization-worker
do
  APP_ID=$(az webapp identity show -g $RG -n $APP --query principalId -o tsv 2>/dev/null)
  if [ -n "$APP_ID" ]; then
    echo "✅ Found identity for: $APP"
    az role assignment create \
      --role "Key Vault Secrets User" \
      --assignee-object-id "$APP_ID" \
      --assignee-principal-type ServicePrincipal \
      --scope "$VAULT_ID" \
      --only-show-errors || echo "ℹ️ Role may already exist for $APP"
  else
    echo "⚠️ App Service '$APP' not found or identity not enabled yet."
  fi
done

# --- Summary ---
echo
echo "✅ Key Vault RBAC access configuration complete!"
echo "-------------------------------------------"
echo "Vault:         $KV"
echo "ResourceGroup: $RG"
echo "Admin User:    $USER_UPN"
echo "Apps Checked:  planner-api, planner-blazorapp, planner-optimization-worker"
echo "-------------------------------------------"
echo "💡 You can verify role assignments via:"
echo "az role assignment list --scope $VAULT_ID -o table"
