# Decisions and open questions

Product decisions that aren't architecture (those are ADRs in [`docs/adr/`](../adr/)).
Before building something that touches one of these, read its row; before changing
one, ask the maintainer and update the row in the same PR.

## 1 Decided

| Topic | Decision | Why | What the design must do |
|---|---|---|---|
| Users | Volunteers only, ~100 at most | No paid staff | No payroll or labour-law records |
| Membership | No dues; status Active/Inactive, with a roster review | Tracking is loose today and improving it is a goal | No payments integration |
| Sign-in | Google (personal accounts) first, emailed magic link as fallback; Microsoft deferred | No Workspace; works for everyone with an email | The roster, not the identity provider, decides who gets in |
| Clock-in | Replaced by lead-marked attendance; clock-in/kiosk moved to Phase 4 | Halves Phase 2; leads already know who came | Attendance entry has an optional note and optional actual hours |
| Hours | Actual hours when given, else shift length | Leads correct only the exceptions | Derive, don't double-store |
| Tiers | Do **not** limit sign-up; each shift has a Tier 2+ target and is flagged understaffed below it | Any walker can help; experience matters per shift, not per person | Tier on the member, target on the shift; the flag is derived, never stored |
| Sensitive records | Admins see training, waivers and disciplinary records | Least exposure | Grant permissions per capability (`Permission`), never by role name, so a manager role is configuration |
| Communication | Email through a transactional provider's free tier; SMS later | SMS costs per message | Magic-link email (PAS-F1-03) is the first email; later notices reuse that path |
| Mailing lists | Target undecided: Mailchimp free, app-sent email, or free Workspace for Nonprofits | Shelter isn't on Workspace | Name the port after the capability (e.g. `IMailingListSync`), not the vendor |
| Pilot before board | The maintainer's group uses Phases 1–2; the board sees pilot results | Real evidence beats a demo | Phase 1 must be live, not a local demo |
| Hosting budget | Free tiers until the board approves a budget | Prototype stage | See [product.md §3](product.md#3-constraints) |
| Missed features | Admin role, recurring shifts, Tier 2 target, reminders, mass cancel, emergency contact, hours totals, expiry reminders, audit log, privacy rules, onboarding, iCal, drop cutoff, spreadsheet import: all accepted | Standard in volunteer scheduling tools | Placed in [roadmap.md §2](roadmap.md#2-features-by-phase) |

## 2 Assumed defaults

Built on these unless the maintainer says otherwise. Each is cheap to change now.

| # | Question | Default | Cost of the other answer |
|---|---|---|---|
| A1 | Sign-up confirmed at once, or approved by a lead? | At once; leads remove people after | A pending state and a lead task on every sign-up |
| A2 | Shift Lead: a role a person holds, or assigned per shift? | Held role; each shift gets one lead slot only leads can fill (Phase 2) | An assign step per shift |
| A3 | Does a missing required waiver block sign-up? | Yes, for required waiver types (Phase 2) | Without a block, waivers are reports only |

## 3 Open

| # | Question | Blocks | Notes |
|---|---|---|---|
| O1 | Is there a manager role between Admin and Shift Lead? | Nothing yet | Per-capability permissions keep it a table row |
| O2 | Are there volunteers under 18? | Phase 2 data shape | If yes: a guardian waiver type and an adult-present rule join Phase 2 |
| O3 | When is the board meeting? | Pilot timing | Sets when Phase 2 must have results |
| O4 | Kiosk device: shared tablet or volunteers' phones? Location check for phone punches? | Phase 4 | A `/kiosk` web route covers both; decide during the pilot |
| O5 | PetPoint link: read-only (available dogs) or write walk logs back? Does PAS use it, and who administers it? | Phase 4 | Access needs a PetPoint admin to request an API key; spike first |
| O6 | Hosting budget and the Azure nonprofit grant | Post-board | Revisit after the board sees the pilot; an always-on replica removes the cold start |
