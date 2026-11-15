# 0003 Cross-site request protection with antiforgery tokens

- **Status:** Accepted
- **Date:** 2026-11-14
- **Amends:** ADR 0001, which left the mechanism open

## Context

The session is a same-origin cookie (ADR 0001), so a malicious page could try to make
the browser send a state-changing request with that cookie attached (CSRF). The SPA
calls the API with `fetch` and JSON bodies; nothing in the app posts HTML forms.

Two OWASP primary defences fit a JSON SPA:

- **A required custom header.** A cross-origin page can't add one without a CORS
  preflight. It holds only while the API grants no cross-origin access.
- **Synchronizer tokens**, which ASP.NET Core ships as `IAntiforgery`. They hold even
  if CORS is later opened to another origin, because that origin still can't read
  the victim's token.

## Decision

Use ASP.NET Core antiforgery, in the pattern Microsoft documents for SPAs:

- `GET /api/antiforgery/token` returns `{ token }` and sets the HttpOnly cookie half of
  the pair (`__Host-pas-xsrf`, Secure, `SameSite=Strict`). Responses are `no-store`.
  It is anonymous, so calls made before signing in can carry a token too.
- Every `POST`, `PUT`, `PATCH` and `DELETE` under `/api` must send the token in the
  `X-XSRF-TOKEN` header. `AntiforgeryValidationMiddleware` validates it with
  `IAntiforgery.IsRequestValidAsync` and answers 400 otherwise. It runs after
  authorization, so a signed-out caller gets 401 and the SPA sends them to sign in.
- Tokens are bound to the member id (`NameIdentifier`), so a role change keeps them
  valid but signing in or out does not. The SPA client fetches a token on its first
  state-changing call, reuses it, and drops it after signing out or any refused call.

The `SameSite=Lax` session cookie stays as a second layer. CORS stays off: the SPA is
same-origin, and CORS only loosens the browser's rules.

The built-in `UseAntiforgery` middleware is not relied on: it only rejects requests for
endpoints that bind forms, and these endpoints bind JSON.

## Consequences

- New state-changing endpoints are covered without per-endpoint work.
- Each browser session makes one extra `GET` before its first change.
- Enabling CORS later needs an explicit origin allow-list (never any origin with
  credentials); the token check keeps protecting the session either way.
- Non-browser clients (a future kiosk or phone app) would use their own scheme, such as
  a bearer token, rather than the cookie, and so need no antiforgery token.
