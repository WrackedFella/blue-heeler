# 0001 Stack and hosting

- **Status:** Accepted
- **Date:** 2026-11-13

## Context

The app serves one volunteer-run shelter: at most about 100 users, no paid staff, and
no budget until the board backs it. Whoever maintains it after the original author
must be able to pick it up from the repository alone, so the stack favours mainstream,
well-documented parts over the cheapest-possible or most novel ones.

## Decision

- **One deployable.** A Vue 3 + TypeScript single-page app, built by Vite, is served as
  static files by one ASP.NET Core API on .NET 10 (LTS, supported to November 2028).
  The SPA and API share an origin.
- **Auth is a same-origin cookie** issued by ASP.NET Core cookie authentication after an
  external sign-in. Google sign-in (any Google account, not Workspace) comes first; an
  emailed magic link is the fallback for members without Google. Microsoft sign-in is
  deferred. The `members` table, not the identity provider, decides who may sign in.
- **Data in PostgreSQL** on Neon's free tier, through EF Core with Npgsql. Schema
  changes are EF Core migrations, applied at startup (EF Core takes a migration lock, so
  overlapping starts are safe).
- **One container on Azure Container Apps** (consumption plan, scale to zero, at most
  one replica), built and deployed by GitHub Actions, with the image in GitHub
  Container Registry.
- **Permissions are granted per capability.** Endpoints require a `Permission`; a role
  is only a named set of permissions (see the item 1 spec, §2.3).

## Consequences

- Same-origin cookies avoid CORS and token storage in the browser. State-changing
  endpoints must add antiforgery protection when the first one lands.
- One image means one release unit: the SPA and API cannot drift apart.
- Scale to zero costs nothing idle but adds a cold start (Container Apps plus Neon
  resuming), felt by the first visitor after a quiet spell. Setting a minimum of one
  replica removes it once there is a budget.
- Neon's free tier keeps only 6 hours of restore history, so the app runs its own
  encrypted nightly backups (ADR 0002).
- .NET 10 rather than the newer .NET 11 because 11 is a standard-term release; the
  next move is to .NET 12 LTS.
- Swapping Neon for Azure Database for PostgreSQL, or Container Apps for App Service,
  changes configuration and the deploy workflow, not application code.
