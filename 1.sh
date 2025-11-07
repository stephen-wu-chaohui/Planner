# az keyvault set-policy   --name planner-kv-sw   --resource-group PlannerRG   --object-id c2d61914-5eb3-4746-bc7e-ed6bbf4308e1  --secret-permissions get list set delete



USER_ID=$(az ad signed-in-user show --query id -o tsv)

az role assignment create \
  --role "Key Vault Administrator" \
   --assignee-principal-type  \
  --assignee-object-id $USER_ID \
  --scope $(az keyvault show -n planner-kv-sw -g PlannerRG --query id -o tsv)
