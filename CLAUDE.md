# Blue Heeler: project contract

Volunteer scheduling and membership tracking for a volunteer-run animal shelter. Start
with the wiki index, `docs/wiki/README.md`: product context, roadmap, decisions and
conventions. Read `docs/adr/` before changing architecture and the spec in
`docs/specs/` for the item at hand.

## Settings

- **Standards:** engineering-standards, dotnet-standards, azure-standards
- **Line:** `PAS` (features `PAS-F<n>`, cards `PAS-F<n>-<NN>`, milestones `PAS-M<n>`)
- **Base branch:** `main`
- **Card review:** required
- **Domain-test review:** agent
- **Domain-logic paths:** `src/BlueHeeler.Domain/`
- **Board header:** no
- **Specs:** `docs/specs/`; **ADRs:** `docs/adr/`
- **Docs (wiki):** `docs/wiki/` (index `docs/wiki/README.md`)

## Gate

Each command must pass before a PR:

```bash
dotnet build -warnaserror
dotnet format --verify-no-changes
dotnet test                      # API tests need Docker (Testcontainers PostgreSQL)
(cd web && npm ci && npm test && npm run build)
```

Mutation testing for domain changes (break threshold 80%):
`cd tests/BlueHeeler.Domain.Tests && dotnet tool restore && dotnet stryker`.

## Rules specific to this project

- The repository is public. Member data (dumps, exports, logs with names or emails) never
  goes into the repository, issues or CI artifacts unless encrypted per ADR 0002.
- Endpoints require a `Permission`, never a role name; roles map to permissions in
  `RolePermissions`.
- Every SPA HTTP call goes through `web/src/api/`.
- Stay on free tiers. Anything that costs money needs the maintainer's approval first.
