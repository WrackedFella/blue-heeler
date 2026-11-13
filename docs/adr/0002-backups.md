# 0002 Backups of member data

- **Status:** Accepted
- **Date:** 2026-11-13

## Context

The app holds real volunteer contact details from its first release. Neon's free tier
keeps 6 hours of history for instant restore, which covers a mistake noticed the same
morning but not one found days later, nor loss of the Neon project or account. The
repository is public, so anything CI stores is visible to anyone who can read the
repository. The budget is $0.

## Decision

Two layers, both free:

1. **Neon instant restore** for the last 6 hours: the first resort for a bad edit.
2. **A nightly logical backup** by a scheduled GitHub Actions workflow
   (`.github/workflows/backup.yml`):
   - `pg_dump --format=custom` through a **read-only** database role whose connection
     string is the only secret the workflow holds.
   - The dump is checked before it leaves the runner (`pg_restore --list` succeeds and
     the `members` table is present), then encrypted with
     [age](https://age-encryption.org) to one or more **public** keys held in a
     repository variable. The private keys never touch GitHub: the key holders keep
     them offline (a password manager entry is enough).
   - Only the `.age` file is uploaded, as a workflow artifact kept 35 days. The plain
     dump exists only on the ephemeral runner.
   - A failed run fails the workflow, and GitHub emails the repository owner.

A backup is trusted only after a **restore drill**: a key holder downloads the latest
artifact, decrypts it and restores it into a scratch database, following
`docs/runbooks/backup-and-restore.md`. One drill is required before volunteers other
than the admin are entered, and one each quarter after.

## Consequences

- Recovery point: up to 24 hours from backups, near zero within Neon's 6-hour window.
- Losing every private key makes the backups useless, so there are at least two key
  holders (the maintainer and one board member) before go-live.
- The encrypted files are downloadable by anyone signed in to GitHub while the
  repository is public. age's X25519 encryption keeps them unreadable, but if the
  board prefers the ciphertext not be public, the workflow can push to a private
  repository instead, at the cost of one more token to manage.
- When there is a budget, Neon's paid plan (7 days of history) or an Azure Storage
  container with immutability can replace or supplement the artifacts.
