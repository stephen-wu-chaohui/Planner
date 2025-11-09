cd deploy

# Pre: Environment & placeholder setup
.\deploy-shared-pre.ps1

# Deploy individual projects
.\deploy-api.ps1
.\deploy-blazor.ps1
.\deploy-worker.ps1

# Post: Push shared secrets to all
.\deploy-shared-post.ps1
.\restart-all.ps1
