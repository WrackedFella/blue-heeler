# PAS-F1-02 Admin adds and edits members, who can then sign in

- **Feature:** PAS-F1 Phase 1, first usable release
- **Status:** In review (spec, tests and code in one PR)
- **Gate class:** `domain`
- **Depends on:** PAS-F1-01

## 1 Behaviour

Approved acceptance criteria, unchanged:

```gherkin
Scenario: A new member signs in
  Given an admin adds "Sam" with email "sam@example.org", role Walker, Tier 1
  When Sam signs in with Google as "sam@example.org"
  Then Sam is signed in as an Active Walker, Tier 1

Scenario: An unknown email is turned away
  Given no member has the email "stranger@example.org"
  When someone signs in with Google as "stranger@example.org"
  Then they are not signed in
  And they are told to contact the volunteer coordinator

Scenario: An inactive member is turned away
  Given an admin sets Sam to Inactive
  When Sam signs in
  Then Sam is not signed in

Scenario: Only admins manage members
  Given Sam is a Walker
  When Sam opens the member list
  Then Sam is refused

Scenario Outline: An admin changes a member's role or tier
  Given Sam is a Walker, Tier 1
  When an admin sets Sam's <field> to <value>
  Then the member list shows Sam with <field> <value>

  Examples:
    | field | value      |
    | role  | Shift Lead |
    | tier  | 2          |
```

The refusals already work from PAS-F1-01; this item adds the screens and endpoints
that put members on the roster and change them, and the "contact the volunteer
coordinator" wording.

One rule is added that no scenario names: **the last Active Admin cannot be demoted or
set Inactive.** The first-admin bootstrap only runs while no Admin exists, so without
this rule one mis-click could lock everyone out until someone edits the database.

## 2 Design

### 2.1 Domain

- `MemberDetails` (record): name, email, role, tier, status, the editable part of a
  member. `MemberDetails.Create(name, email, role, tier, status)` validates raw input
  and returns `MemberDetailsResult`: `Valid(details)` or `Invalid(errors)`, where errors
  map a field name to messages. Rules: name not blank and at most 200 characters;
  email per `EmailAddress`; role and status present; tier absent or 1 to 3. A result
  type, not exceptions, because bad form input is expected.
- `Member.Add(id, details, now)` builds a member; `member.Apply(details)` changes one.
- `RosterPolicy.CheckEdit(current, proposed, activeAdminCount)` returns `null` or
  `EditRefusal.LastActiveAdmin` when the change would leave no Active Admin.
- `Permission.ManageMembers`, granted to `Admin` only.

### 2.2 API

| Endpoint | Permission | Success | Failures |
|---|---|---|---|
| `GET /api/members/{id}` | `ViewMembers` | 200 member | 404 |
| `POST /api/members` | `ManageMembers` | 201 with `Location` and the member | 400 validation, 409 email in use |
| `PUT /api/members/{id}` | `ManageMembers` | 200 member | 400, 404, 409 email in use or last Active Admin |

- The request body is `{ name, email, role, tier, status }`, enums as their names.
  Role and status are nullable in the request so a missing value is a validation error
  rather than silently becoming the enum's first value (`Admin`).
- Duplicate email is caught from the unique index (PostgreSQL `23505`), not pre-checked,
  so two admins adding the same person at once still get one row and one 409.
- Failures are RFC 9457 problem details; 400 carries `errors` keyed by field.

### 2.3 Cross-site request protection

Every `POST`, `PUT`, `PATCH` and `DELETE` under `/api` must carry the header
`X-PAS-Request: 1`, or it is refused with 400 before authentication runs. The SPA's
client adds it to every call.

Why a header rather than ASP.NET Core antiforgery tokens: the SPA sends JSON from the
same origin. A cross-site page cannot add a custom header without a CORS preflight,
and the API allows no cross-origin requests, so the header is a complete defence (the
OWASP "custom request header" pattern) with no token endpoint or token storage. The
`SameSite=Lax` cookie remains a second layer. Form posts, which antiforgery exists for,
are not used.

### 2.4 SPA

- A small session store (`web/src/session.ts`) keeps the `Me` the router guard
  fetched, so pages can check permissions without another request.
- Members page: "Add member" button and an "Edit" link per row, shown only with
  `ManageMembers`.
- `/members/new` and `/members/:id/edit` share `MemberForm.vue`: name, email, role,
  tier (blank, 1, 2, 3) and status. Server errors are shown next to their fields; a 409
  shows its message above the form.
- Sign-in page: every refusal tells the person to contact the volunteer coordinator
  (already the case; now covered by a test).

## 3 Out of scope

- Deleting members: Inactive is the roster's "removed", and history will hang off
  members from Phase 2.
- Spreadsheet import, emergency contact, training records (later phases).
- An audit log of changes (Phase 3).
- A concurrency token on edits. Two admins editing one member at once is unlikely at
  this size; the last write wins.
- Search, sorting or paging of the list (100 members fit on one page).

## 4 Footprint

| Part | Files |
|---|---|
| A, domain | `src/BlueHeeler.Domain/Members/{MemberDetails,Member}.cs`, `src/BlueHeeler.Domain/Access/{Permission,RolePermissions,RosterPolicy}.cs`, matching tests |
| B, API | `src/BlueHeeler.Api/Members/*`, `src/BlueHeeler.Api/Hosting/{HostingSetup,CsrfHeaderMiddleware}.cs`, `src/BlueHeeler.Api/Data/MemberConfiguration.cs`, `src/BlueHeeler.Api/Auth/ExternalSignIn.cs` (read order fix), `Program.cs`, `tests/BlueHeeler.Api.Tests/**` |
| C, SPA | `web/src/**` (including `session.ts`, `test-support.ts`, `pages/MemberFormPage.vue`) |
| D, docs | this spec, `docs/wiki/{roadmap,architecture,development,domain}.md` |

B depends on A. C and D are independent.

## 5 Test map

| Scenario or rule | Test | Part |
|---|---|---|
| New member signs in as Active Walker, Tier 1 | `MemberManagementTests.AddMember_ThenSignIn_SignsInAsActiveWalkerTier1` | B |
| Unknown email turned away (API) | `SignInTests.GoogleSignIn_UnknownEmail_RedirectsWithErrorAndNoCookie` (existing) | B |
| Unknown email told to contact the coordinator | `SignInPage.spec.ts`: "tells a refused visitor to contact the volunteer coordinator" | C |
| Admin sets Sam Inactive, Sam can't sign in | `MemberManagementTests.EditMember_SetInactive_SignInRefused` | B |
| Walker refused the member list | `MembersEndpointTests.GetMembers_WalkerSignedIn_Returns403` (existing) | B |
| Walker can't add or edit | `MemberManagementTests.AddMember_AsWalker_Returns403`, `EditMember_AsWalker_Returns403` | B |
| Admin changes role or tier, list shows it | `MemberManagementTests.EditMember_ChangeRoleOrTier_ListShowsChange` (theory over both rows) | B |
| Validation: blank name, bad email, tier out of range, missing role or status | `MemberDetailsTests.Create_*` | A |
| Validation surfaces as 400 with field errors | `MemberManagementTests.AddMember_InvalidInput_Returns400WithFieldErrors` | B |
| Duplicate email | `MemberManagementTests.AddMember_EmailInUse_Returns409`, `EditMember_EmailOfAnotherMember_Returns409` | B |
| Last Active Admin can't be demoted or deactivated | `RosterPolicyTests.*`, `MemberManagementTests.EditMember_LastActiveAdminDemoted_Returns409` | A, B |
| A second admin can demote the first | `RosterPolicyTests.CheckEdit_DemoteAdmin_OtherActiveAdminExists_Allows` | A |
| Unknown member id | `MemberManagementTests.EditMember_UnknownId_Returns404`, `GetMember_UnknownId_Returns404` | B |
| Mutating call without the header is refused | `CsrfHeaderTests.Post_WithoutHeader_Returns400`, `SignOut_WithoutHeader_Returns400` | B |
| `ManageMembers` granted to Admin only | `RolePermissionsTests` | A |
| Form: add, edit, field errors, hidden without permission | `MemberForm.spec.ts`, `MembersPage.spec.ts` | C |

## 6 Risks

- **Locking everyone out.** Covered by the last-Active-Admin rule. Two admins demoting
  each other at the same instant could still both succeed; acceptable at this size.
- **Email changes break sign-in** if an admin mistypes. The member list shows the email,
  and the person is refused with the coordinator message, so it surfaces quickly.
