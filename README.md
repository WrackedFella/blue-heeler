# Blue Heeler

Volunteer scheduling and membership tracking for a volunteer-run animal shelter: shift
sign-up, a member roster with roles and training tiers, and coverage reports. It is
built to run free at about 100 users and to be easy to hand over.

## Stack

Vue 3 + TypeScript SPA served by one ASP.NET Core (.NET 10) API, PostgreSQL through EF
Core, Google sign-in with a same-origin cookie, deployed as one container on Azure
Container Apps. The reasoning is in [ADR 0001](docs/adr/0001-stack-and-hosting.md).

## Layout

| Path | What it is |
|---|---|
| `src/BlueHeeler.Domain` | Rules (members, roles, permissions, sign-in policy); no infrastructure |
| `src/BlueHeeler.Api` | Composition root, endpoints, auth, EF Core, hosting |
| `web` | The SPA |
| `tests` | xUnit tests; API tests run the real pipeline against PostgreSQL in Docker |
| `docs` | ADRs, specs, runbooks |

## Run locally

Prerequisites: .NET 10 SDK, Node 22, Docker.

```bash
docker run -d --name pas-db -e POSTGRES_PASSWORD=dev -p 5432:5432 postgres:17-alpine
cd src/BlueHeeler.Api
dotnet user-secrets init
dotnet user-secrets set ConnectionStrings:Default "Host=localhost;Database=pas;Username=postgres;Password=dev"
dotnet user-secrets set Authentication:Google:ClientId "<dev client id>"
dotnet user-secrets set Authentication:Google:ClientSecret "<dev client secret>"
dotnet user-secrets set Bootstrap:FirstAdminEmail "<your gmail>"
dotnet run --launch-profile http
# second terminal
cd web && npm ci && npm run dev    # http://localhost:5173
```

The Google client needs `http://localhost:5173/signin-google` as a redirect URI.

## Tests

```bash
dotnet test
cd web && npm test
```

## Operations

- [Deploy runbook](docs/runbooks/deploy.md)
- [Backup and restore runbook](docs/runbooks/backup-and-restore.md)
