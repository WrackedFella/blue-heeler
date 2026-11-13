# PAS-F1-01 Admin signs in with Google and sees the member list

- **Feature:** PAS-F1 Phase 1, first usable release
- **Status:** In review (spec, tests and code in one PR)
- **Gate class:** `domain`
- **Depends on:** nothing (walking skeleton)

## 1 Behaviour

Approved acceptance criteria, unchanged:

```gherkin
Scenario: The configured first admin signs in
  Given the app is configured with "admin@example.org" as the first admin
  When that person signs in with Google as "admin@example.org"
  Then they see the member list, containing themselves as an Active Admin

Scenario: A signed-out visitor is sent to sign in
  Given a visitor who is not signed in
  When they open the member list
  Then they see the sign-in page
```

Item 1 must also refuse anyone who is not a member. Item 2 owns the scenario and the
"contact the volunteer coordinator" message, but a walking skeleton that lets any
Google account in would expose real member data on day one, so the refusal itself is
built here (§2.2) and item 2 only adds its wording.

## 2 Design

### 2.1 Shape

```
web/ (Vue 3 + TS, Vite)  ── same-origin fetch ──▶  src/BlueHeeler.Api  ──▶  PostgreSQL
                                                      │
                                                      └─ src/BlueHeeler.Domain (no infrastructure references)
```

- **`BlueHeeler.Domain`** holds the rules: `Member`, `EmailAddress`, `Role`,
  `WalkerTier`, `MemberStatus`, `Permission`, `RolePermissions`, and `SignInPolicy`,
  a pure function that decides what an external sign-in means. It references nothing
  but the BCL, so the rules are tested without a database or a web host.
- **`BlueHeeler.Api`** is the composition root and every adapter: EF Core
  (`AppDbContext`, migrations), authentication wiring, endpoints, health checks, and
  static hosting of the built SPA from `wwwroot`. One project because there is one
  deployable and one adapter per concern so far; a separate infrastructure project
  pays off only when a second host (a job, a kiosk API) needs the same adapters.

### 2.2 Sign-in flow

1. The SPA's sign-in page links to `GET /api/auth/google`, which challenges the Google
   scheme with return URL `/members`.
2. Google redirects back to `/signin-google`. The Google handler exchanges the code and
   reads the user's `email`, `email_verified` and `name`.
3. In `OnTicketReceived`, `ExternalSignIn` loads the member by email and whether any
   Admin exists, then asks `SignInPolicy.Decide` for one of:
   - `SignIn(member)`: an Active member with that email.
   - `BootstrapFirstAdmin(member)`: no member has the email, it equals the configured
     `Bootstrap:FirstAdminEmail`, and no Admin exists yet. The new Active Admin is
     saved, named from Google.
   - `Refuse(reason)`: unverified email, unknown email, or an Inactive member.
4. On `SignIn`/`BootstrapFirstAdmin` the ticket's principal is replaced by an app
   principal carrying only the member id, and the cookie scheme signs it in. On
   `Refuse` the handler redirects to `/sign-in?error=<reason>` and no cookie is set.

Why match on email rather than Google's subject id: admins add members by email before
those members ever sign in (item 2), and magic-link sign-in (item 3) only knows the
email. Google asserts `email_verified`, and the policy refuses an unverified email.

Why bootstrap at sign-in rather than seed at startup: it takes the admin's real name
from Google, and the "no Admin exists" condition means the setting cannot be used to
take over a running system later.

### 2.3 Permissions per capability

- `Permission` is an enum of actions (`ViewMembers` in this item).
- `RolePermissions` is a table from `Role` to its permission set. Adding a Manager role
  later is one new enum value and one table row; no endpoint changes.
- Each endpoint declares `.RequirePermission(Permission.X)`. At startup one
  authorization policy is registered per `Permission` value, requiring the claim
  `permission = X`.
- The cookie holds only the member id. The cookie's `OnValidatePrincipal` reloads the
  member on every request and rebuilds the principal's permission claims from its
  current role, rejecting the cookie if the member is gone or Inactive. So a role
  change or deactivation takes effect on the next request, at the cost of one indexed
  lookup per request, which is nothing at 100 users.
- For `/api/*` paths, a missing cookie answers `401` and a missing permission `403`
  instead of redirecting, so the SPA decides where to send the user.

### 2.4 Data

Table `members`: `id uuid pk`, `name varchar(200) not null`, `email varchar(254) not
null unique` (stored normalised: trimmed, lower-case), `role varchar(32) not null`,
`walker_tier int null`, `status varchar(32) not null`, `created_at timestamptz not
null`. Enums are stored as their names so the database stays readable in a backup and
in Neon's console. A Google display name longer than 200 characters is truncated at
bootstrap.

ASP.NET Core Data Protection keys (which encrypt the auth cookie) are stored in the
same database (`data_protection_keys`), so cookies survive container restarts and
scale-to-zero.

### 2.5 Hosting concerns built in this item

- Health: `/healthz` (liveness, no dependencies) and `/readyz` (database reachable).
- Forwarded headers are honoured so the OAuth redirect URI uses `https` behind the
  Container Apps ingress.
- Options are bound and validated at startup, before migrations run:
  `ConnectionStrings:Default` and `Authentication:Google:ClientId`/`ClientSecret` are
  required; `Bootstrap:FirstAdminEmail` is optional (it is only needed until the first
  Admin exists) but must be an email address when set. Secrets come from Container Apps
  secrets, never from files in the repository.
- Deploys run only on pushes to `main` in this repository, after the CI gate, and wait
  for the new revision's readiness probe; pull requests never reach production
  credentials.
- Unhandled exceptions become RFC 9457 problem details.
- `Dockerfile`: Node stage builds the SPA, SDK stage publishes the API, runtime stage
  runs as the non-root `app` user on a pinned `aspnet` image.

### 2.6 Backups (before the group starts)

Per ADR 0002: `.github/workflows/backup.yml` runs nightly, dumps through a read-only
role, verifies the dump, encrypts it with age and keeps only the ciphertext as a
35-day artifact. `docs/runbooks/backup-and-restore.md` holds the one-time setup
(read-only role, key pair, repository secret and variable) and the restore drill.
**Release gate:** one restore drill is done before any member other than the admin is
entered.

### 2.7 SPA

- Routes: `/sign-in` (public) and `/members` (requires sign-in); `/` redirects to
  `/members`.
- A router guard calls `GET /api/me`; a `401` routes to `/sign-in`. All HTTP calls go
  through `web/src/api/`, so swapping in a generated client later touches one folder.
- The member list sends a `401` (session ended mid-visit) to `/sign-in`, says "no
  access" on a `403`, and has a Sign out button.
- No component library: one table and one button do not justify the dependency.

## 3 Out of scope

- Adding, editing or deactivating members, and the refusal wording (item 2).
- Magic-link sign-in and any email sending (item 3).
- Antiforgery tokens: this item adds no state-changing endpoint except sign-out, which
  is protected by `SameSite=Lax` on a `POST`. Item 2 adds antiforgery with its first
  form.
- An OpenAPI-generated TypeScript client: two response types do not repay the build
  step. Item 2 adds it when the API grows.
- Infrastructure as code. The one-time Azure, Neon and Google setup is a short
  runbook (`docs/runbooks/deploy.md`); a template earns its keep when there is a
  second environment.
- A manager role, audit log, or any member field beyond those in §2.4.

## 4 Footprint

| Part | Files |
|---|---|
| A, domain | `src/BlueHeeler.Domain/**`, `tests/BlueHeeler.Domain.Tests/**` |
| B, API | `src/BlueHeeler.Api/**`, `tests/BlueHeeler.Api.Tests/**` |
| C, SPA | `web/**` |
| D, delivery | `Dockerfile`, `.dockerignore`, `.github/workflows/*.yml`, `docs/runbooks/*.md` |

Shared once: `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`,
`BlueHeeler.slnx`, `CLAUDE.md`, ADRs. B depends on A's types; C and D are independent.

## 5 Test map

| Scenario or rule | Test | Part |
|---|---|---|
| First admin signs in, sees self as Active Admin | `SignInTests.GoogleSignIn_ConfiguredFirstAdmin_SeesSelfAsActiveAdmin` | B |
| Signed-out visitor sent to sign in (API side) | `MembersEndpointTests.GetMembers_SignedOut_Returns401` | B |
| Signed-out visitor sent to sign in (SPA side) | `router.spec.ts`: "sends a signed-out visitor to the sign-in page" | C |
| Unknown, unverified or Inactive email refused, no cookie | `SignInTests.GoogleSignIn_UnknownEmail_…`, `…_UnverifiedEmail_…`, `…_InactiveMember_…` | B |
| Bootstrap is case-insensitive and happens once | `SignInTests.GoogleSignIn_FirstAdminEmailInOtherCase_…`, `…_SecondSignInOfFirstAdmin_DoesNotDuplicate`, `…_FirstAdminAfterConcurrentBootstrap_…` | B |
| Session cookie is `__Host-`, Secure, HttpOnly, SameSite=Lax, no Domain | `SignInTests.GoogleSignIn_Callback_SetsHardenedSessionCookie` | B |
| `https` redirect URI behind the TLS-terminating proxy | `SignInTests.GoogleChallenge_BehindTlsTerminatingProxy_UsesHttpsRedirectUri` | B |
| Policy: each decision branch | `SignInPolicyTests.Decide_*` (Active, Inactive, unknown, unverified, bootstrap only while no Admin, no setting, existing member, blank and overlong name) | A |
| Email normalisation and validation | `EmailAddressTests` | A |
| Member invariants (id, name blank or over 200, email) | `MemberTests` | A |
| Admin has ViewMembers, others do not; every role has a row | `RolePermissionsTests` | A |
| Role change, deactivation or deletion applies on the next request | `MembersEndpointTests.GetMembers_MemberDemotedAfterSignIn_Returns403`, `…Deactivated…_Returns401`, `…Deleted…_Returns401` | B |
| A Walker cannot see the member list | `MembersEndpointTests.GetMembers_WalkerSignedIn_Returns403` | B |
| Liveness and readiness | `HealthTests.HealthEndpoint_WithDatabase_Returns200` | B |
| Member page: rows, load failure, 403, 401, sign out | `MembersPage.spec.ts` | C |
| Domain references no infrastructure | `ArchitectureTests.DomainAssembly_ReferencesOnlyTheBcl` | A |

API tests run the real pipeline with `WebApplicationFactory` against PostgreSQL in
Testcontainers. Google itself is replaced only at the network edge: the Google
handler's backchannel `HttpClient` gets a fake handler that answers the token and
userinfo calls, so the challenge, state, correlation cookie and callback all run for
real.

## 6 Risks

- **Real data on day one.** Mitigated by the refusal in §2.2, the backups in §2.6, and
  the restore-drill release gate.
- **Cold start** after idle (Container Apps scale to zero plus Neon suspend), a few
  seconds on the first request. Accepted for Phase 1; ADR 0001 names the fix.
- **Email as identity.** A member who changes their Google address can no longer sign
  in until an admin updates their email. Acceptable at this size; storing Google's
  subject id alongside is a later, additive change.
- **Data Protection keys unencrypted at rest** in the database. Anyone who can read the
  database can already read the member data the cookie protects; Key Vault wrapping
  arrives with the first integration (ADR 0001's scope cut).
