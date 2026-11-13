# Product

## 1 Who it's for

PAS is an animal shelter run entirely by volunteers: no paid staff, about 100 people at
most (the rolls may shrink after a roster review), and no membership dues. Today shift
sign-up and membership are tracked loosely by hand. Blue Heeler replaces that with one
web app.

| User | Role in the app | Main jobs |
|---|---|---|
| Walker (Tier 1–3) | `Walker` | Find shifts, sign up, cancel, see "my shifts" |
| Shift Lead | `ShiftLead` | See who's coming, ask for more people, mark attendance and no-shows |
| Volunteer coordinator | `Admin` | Manage members, roles and tiers; post shifts; see sensitive records; pull reports |
| The board | none | Sees coverage and membership reports and pilot results |

A manager role between Admin and Shift Lead may come later; see
[decisions.md §1](decisions.md#1-decided).

## 2 End state

Volunteers sign up for shifts that suit their training level from a phone or browser.
Shift leads see who is coming, ask for more people when a shift is short, and record
who showed up. Coordinators keep each volunteer's training, signed waivers and conduct
record in one place, and pull coverage and membership reports for the board. Messages
go out through a third-party email service: the app sends notices but doesn't run a
mail system.

## 3 Constraints

These shape every design choice. Check a proposal against them before anything else.

1. **$0 to run until the board approves a budget.** Free tiers only (Azure Container
   Apps grant, Neon free Postgres, GitHub Actions). Anything that costs money needs the
   maintainer's approval first.
2. **Easy to hand over.** The original maintainer may leave the shelter. Mainstream,
   well-documented parts beat clever or cheap-but-obscure ones, and the repository must
   be enough to pick it up (hence this wiki).
3. **Small scale.** ~100 users, a handful at a time. Don't add caching, queues or
   services for load that won't come.
4. **Public repository.** Member data never goes into the repository, issues or CI
   artifacts unless encrypted ([ADR 0002](../adr/0002-backups.md)).
5. **Volunteers, not employees.** Labour-law record keeping (e.g. FLSA) doesn't apply,
   so attendance and hours are for reporting and grants, not payroll.
6. **No Google Workspace.** The shelter has a Google Business Profile only, which has
   no member or mailing-list API. Volunteers sign in with personal Google accounts, or
   an emailed link.

## 4 Scope

**In:** shift scheduling and sign-up, member roster with roles, tiers and status,
role-based access, attendance by shift leads, training and waiver records, conduct
(disciplinary) records, coverage and membership reports, email notices.

**Later (Phase 4):** mailing-list sync, SMS, kiosk or phone clock-in, shift swaps,
waitlists, stored waiver scans and e-signature, a PetPoint (shelter software) link.

**Out:** payroll, donor CRM. Tools such as Bloomerang already do donor CRM; export to
them rather than rebuild them.

## 5 How success is judged

The maintainer's own volunteer group uses the app first (Phases 1 and 2). The board
then decides on go-live from the pilot's real results, not from a demo. See
[roadmap.md](roadmap.md).
