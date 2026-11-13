# Backup and restore runbook

Design and reasoning: ADR 0002. Backups run nightly from `.github/workflows/backup.yml`
and are kept 35 days as encrypted workflow artifacts. Neon's own instant restore covers
the last 6 hours.

## 1 One-time setup

### 1.1 Read-only database role

In the Neon SQL editor, connected to `pas` as the owner:

```sql
CREATE ROLE pas_backup WITH LOGIN PASSWORD '<generated password>';
GRANT CONNECT ON DATABASE pas TO pas_backup;
GRANT USAGE ON SCHEMA public TO pas_backup;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO pas_backup;
GRANT SELECT ON ALL SEQUENCES IN SCHEMA public TO pas_backup;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO pas_backup;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON SEQUENCES TO pas_backup;
```

`ALTER DEFAULT PRIVILEGES` applies to tables the owner creates later, so new migrations
are backed up without another grant. Run it as the same role the app migrates with.

### 1.2 Key pair (each key holder)

Install [age](https://github.com/FiloSottile/age) and run `age-keygen -o pas-backup.key`.
Store `pas-backup.key` in your password manager, then delete the file. Share only the
public key it prints (`age1...`). There should be at least two key holders before
go-live.

### 1.3 GitHub settings

- Repository settings → Environments → create `backup`, restricted to `main`, and add
  the environment secret `BACKUP_DATABASE_URL`: the `pas_backup` connection string in
  URL form, on the **direct** (non-pooler) host,
  `postgresql://pas_backup:<password>@<host>/pas?sslmode=require`.
- Add the **repository** variable (Settings → Secrets and variables → Actions →
  Variables) `BACKUP_AGE_RECIPIENTS`: every key holder's public key, separated by
  spaces. It must be repository-level: the job checks it before binding to the
  environment, so an environment-level value would make every run skip silently.

Until the variable is set, the workflow skips itself. Run it once by hand (Actions →
Backup → Run workflow) to confirm it succeeds.

GitHub disables scheduled workflows in a public repository after 60 days without
commits. If the Backup workflow shows as disabled, re-enable it (Actions → Backup →
Enable workflow). The quarterly drill below includes checking this.

## 2 Restore drill

Required once before members other than the admin are entered, then each quarter.

1. Actions → Backup → the latest successful run → download the artifact and unzip it.
2. Decrypt: `age --decrypt -i <(cat pas-backup.key) -o pas.dump pas-<stamp>.dump.age`
   (paste the key from your password manager into `pas-backup.key` first, and delete it
   afterwards).
3. Restore into a scratch database, never production. The tools run inside the
   container, so no local PostgreSQL client is needed:
   ```bash
   docker run -d --name pas-drill -e POSTGRES_PASSWORD=drill -e POSTGRES_DB=pas postgres:17-alpine
   until docker exec pas-drill pg_isready -h 127.0.0.1 -U postgres -d pas; do sleep 1; done
   docker cp pas.dump pas-drill:/tmp/pas.dump
   docker exec pas-drill pg_restore -U postgres -d pas --no-owner /tmp/pas.dump
   docker exec pas-drill psql -U postgres -d pas -c "select count(*), max(created_at) from members"
   ```
4. Check the count matches the app's member list. Then delete `pas.dump`, the key file
   and the container (`docker rm -f pas-drill`).
5. Check the Backup workflow is still enabled and its last nightly run succeeded.
6. Note the date and result in the drill log below.

## 3 Restoring production

- **A mistake within the last 6 hours:** Neon console → the branch → Restore to a point
  in time. Neon keeps the pre-restore state as a branch, so this is reversible.
- **Anything older, or Neon is gone:** create a fresh Neon database, restore the latest
  decrypted dump into it with `pg_restore --no-owner`, recreate the `pas_backup` role,
  update the Container App's `db` secret, and restart the revision.

## 4 Drill log

| Date | Who | Backup used | Members restored | Result |
|---|---|---|---|---|
