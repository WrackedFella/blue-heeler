# 0003 Cross-site request protection by custom header

- **Status:** Accepted
- **Date:** 2026-11-14
- **Amends:** ADR 0001, which said state-changing endpoints would add antiforgery protection

## Context

The session is a same-origin cookie (ADR 0001), so a malicious page could try to make
the browser send a state-changing request with that cookie attached (CSRF). The SPA
calls the API with `fetch` and JSON bodies; nothing in the app posts HTML forms.

## Decision

Every `POST`, `PUT`, `PATCH` and `DELETE` under `/api` must carry the header
`X-PAS-Request` (the SPA sends `1`; any value passes). `CsrfHeaderMiddleware` answers
400 before authentication runs when it is missing. The SPA's API client adds it to
every non-GET call.

This is OWASP's "custom request header" defence. A page on another origin cannot add a
custom header without a CORS preflight, and the API grants no cross-origin access, so
the browser never sends such a request. The `SameSite=Lax` session cookie stays as a
second layer.

ASP.NET Core antiforgery tokens were not used: they defend form posts, and for a JSON
SPA they add a token endpoint, token storage and per-request plumbing for no extra
protection here.

## Consequences

- New state-changing endpoints are covered without per-endpoint work.
- Enabling CORS for any origin, or adding a state-changing `GET`, would undo the
  defence. Both need a new decision.
- Clients other than the SPA (scripts, a future kiosk) must send the header too.
