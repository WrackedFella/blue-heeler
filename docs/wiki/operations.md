# Operations

Procedures live in the runbooks; this page is the map.

- [Deploy runbook](../runbooks/deploy.md): one-time production setup and how releases flow.
- [Backup and restore runbook](../runbooks/backup-and-restore.md): backup setup, restore drill, drill log.

## 1 Environments

| Environment | Where | Database | Deploys |
|---|---|---|---|
| Local | Developer machine | PostgreSQL 17 in Docker | `dotnet run` + `npm run dev` |
| Tests | CI runner or local | Throwaway PostgreSQL via Testcontainers | Per test run |
| Production | Azure Container Apps, consumption plan, 0–1 replica | Neon free tier, PostgreSQL 17, direct (non-pooler) host | Every push to `main`, after the CI gate |

There is no staging environment; at this size the PR gate plus a health-gated rollout
is enough, and a second environment would cost free-tier headroom.

## 2 Configuration

Values are environment variables (`__` for `:`), held as Container Apps secrets where
secret. Names only here; values never go in the repository.

| Setting | Secret | Purpose |
|---|---|---|
| `ConnectionStrings__Default` | Yes | App database (owner role) |
| `Authentication__Google__ClientId` | No | Google OAuth client |
| `Authentication__Google__ClientSecret` | Yes | Google OAuth client |
| `Bootstrap__FirstAdminEmail` | No | First admin; ignored once any Admin exists |
| `AllowedHosts` | No | The app's host name |

GitHub repository variables: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`,
`AZURE_SUBSCRIPTION_ID`, `AZURE_RESOURCE_GROUP`, `AZURE_CONTAINERAPP_NAME`,
`BACKUP_AGE_RECIPIENTS`. The `backup` environment holds the one secret,
`BACKUP_DATABASE_URL` (read-only role). Deploys use OIDC federation, so no Azure
credential is stored in GitHub. Each workflow skips itself until its variable is set.

## 3 Workflows

| Workflow | Trigger | Does |
|---|---|---|
| `ci.yml` | Pull requests; called by deploy | The gate: build, format, test, SPA test and build, image build |
| `deploy.yml` | Push to `main` | CI → image to GHCR tagged by commit → roll the Container App → wait for `/readyz` |
| `backup.yml` | Nightly 09:17 UTC, manual | `pg_dump` via read-only role → verify → encrypt with age → 35-day artifact ([ADR 0002](../adr/0002-backups.md)) |

Health: `/healthz` (process alive) and `/readyz` (database reachable). A revision that
never becomes ready doesn't take traffic, and the previous one keeps serving.

## 4 Owed before real member data

The production environment isn't stood up yet. In order (each step is in the deploy or
backup runbook):

- [ ] Neon project on PostgreSQL 17, database `pas`, and the read-only backup role
- [ ] Google OAuth client with the production redirect URI
- [ ] Azure resource group, Container Apps environment and app, with probes
- [ ] OIDC app registration, `production` and `backup` GitHub environments, repository variables
- [ ] Backup key pairs for at least two key holders (the maintainer and one board member before go-live)
- [ ] A passed restore drill, logged, before any member other than the admin is entered

Tick these here as they're done, so the next maintainer knows what exists.

## 5 Known operational traps

- **Cold start.** After idle, the first visitor waits for Container Apps and Neon to
  resume. A minimum of one replica fixes it once there's a budget.
- **Scheduled workflows pause.** GitHub disables schedules in a public repository after
  60 days without commits. The quarterly restore drill checks the Backup workflow is
  still enabled.
- **Neon keeps 6 hours of history** on the free tier. Beyond that, the nightly backup
  is the only copy.
- **Lost private keys make backups useless.** Keep at least two key holders.
- **Migrations aren't undone by a rollback.** See [architecture.md §5](architecture.md#5-data).
