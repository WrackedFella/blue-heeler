# Deploy runbook

One-time setup of the free-tier production environment, then how releases flow. Do the
steps in order; each names the console or command. Nothing here costs money on the free
tiers at this app's size, but Azure needs a subscription with a payment method on file.

## 1 Neon database

1. Create a Neon project (region close to the Azure region you pick), database `pas`.
2. Copy the pooled connection string, converted to the .NET form:
   `Host=<host>;Database=pas;Username=<owner>;Password=<password>;SSL Mode=Require`.
   This is the app's `ConnectionStrings__Default`. Keep it out of the repository.
3. Create the read-only backup role (section 1 of `backup-and-restore.md`).

## 2 Google sign-in client

1. Google Cloud console → APIs & Services → OAuth consent screen: External, app name
   "PAS Volunteers", scopes `openid`, `email`, `profile` only. Publish it (no verification
   is needed for these scopes).
2. Credentials → Create OAuth client ID → Web application. Authorised redirect URI:
   `https://<container-app-fqdn>/signin-google` (add `http://localhost:5173/signin-google`
   for local development).
3. Keep the client ID and secret for step 3.

## 3 Azure Container App

```bash
az group create -n rg-pas -l <region>
az containerapp env create -n cae-pas -g rg-pas -l <region>
az containerapp create -n ca-pas -g rg-pas --environment cae-pas \
  --image mcr.microsoft.com/dotnet/samples:aspnetapp \
  --ingress external --target-port 8080 \
  --min-replicas 0 --max-replicas 1 \
  --secrets db="<connection string>" google-secret="<client secret>" \
  --env-vars ConnectionStrings__Default=secretref:db \
             Authentication__Google__ClientId=<client id> \
             Authentication__Google__ClientSecret=secretref:google-secret \
             Bootstrap__FirstAdminEmail=<your gmail address>
```

The placeholder image is replaced by the first deploy. `--max-replicas 1` keeps the
free grant predictable; the app is stateless, so raising it later is safe.

If the GHCR package is private, give the Container App a registry credential
(`az containerapp registry set` with a GitHub token that has `read:packages`); making the
package public avoids that, and the image holds no secrets.

## 4 GitHub → Azure without secrets

```bash
az ad app create --display-name gh-blue-heeler-deploy   # note appId
az ad sp create --id <appId>
az role assignment create --assignee <appId> --role Contributor \
  --scope $(az group show -n rg-pas --query id -o tsv)
az ad app federated-credential create --id <appId> --parameters '{
  "name": "github-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:WrackedFella/blue-heeler:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

In the repository settings, create the environment `production` and these environment
variables: `AZURE_CLIENT_ID` (the appId), `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`,
`AZURE_RESOURCE_GROUP` (`rg-pas`), `AZURE_CONTAINERAPP_NAME` (`ca-pas`).

## 5 Releases

Every push to `main` that passes CI builds an image tagged with the commit, pushes it to
GitHub Container Registry and rolls the Container App onto it, then waits for
`/readyz`. Until `AZURE_CONTAINERAPP_NAME` is set, the deploy job skips itself.
Migrations run when the new revision starts.

To roll back, re-run the Deploy workflow for an earlier commit, or point the app at an
earlier image: `az containerapp update -n ca-pas -g rg-pas --image ghcr.io/wrackedfella/blue-heeler:<sha>`.
A migration that already ran is not undone by rolling back the image.

## 6 First sign-in

Open the app's URL and sign in with the Google account named in
`Bootstrap__FirstAdminEmail`. It becomes the first Admin. While no Admin exists, that
setting is the only way in; once one does, the setting is ignored and can be removed.
