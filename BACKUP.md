# Backups & restore

## How it works

The `backup` service in `docker-compose.yml` runs continuously alongside the
rest of the stack. On start (and then every 24 hours) it:

1. Runs `mysqldump --single-transaction` against the `db` service and gzips
   the result to `./backups/db_<timestamp>.sql.gz`.
2. Tars the `api-images` volume (the same volume the `api` service stores
   uploaded/downloaded photos in) to `./backups/images_<timestamp>.tar.gz`.
3. Deletes backup files older than `BACKUP_RETENTION_DAYS` (default 14,
   set in `.env`).

Both files share the same `<timestamp>` (`YYYYMMDD_HHMMSS`), so a matching
pair is always a consistent snapshot.

**This is local-only today.** `./backups` is a directory on the same host as
the rest of the stack — losing the host loses the backups too. That's not a
real backup strategy on its own; it's the first half of one. Point something
at `./backups` to copy it off-host on whatever schedule makes sense for you,
for example:

```sh
# rsync to a NAS reachable over SSH
rsync -av --delete ./backups/ user@nas:/volume1/photoalbum-backups/

# or rclone to any cloud/remote target rclone supports
rclone sync ./backups/ remote:photoalbum-backups/
```

Neither is wired up here since the actual destination is specific to your
setup — pick one and put it in your own host-level cron/scheduled task.

## Restoring

Restore is deliberately manual — it's a rare, destructive operation and
should never run unattended. List available backups and restore one:

```sh
# See what's available
docker compose run --rm backup /scripts/restore.sh

# Restore a specific one (overwrites the live DB and images!)
docker compose run --rm backup /scripts/restore.sh 20260713_120000
```

This restores into the **live** `db` and `api-images` volumes — make sure
that's actually what you want before running it. There's no confirmation
prompt.

### Tested procedure

The restore commands were verified against a throwaway, isolated MySQL
container (not the live stack) before this was written: a backup was taken,
restored into a scratch database, and the admin user + test albums/photos
were confirmed present via direct `SELECT` queries; the images tarball was
extracted to a scratch directory and the files confirmed intact byte-for-byte.

## Disk encryption

Not applicable in this deployment — this runs on a stationary home server,
not a portable device. If that ever changes (e.g. moved to a laptop), enable
full-disk encryption (LUKS on Linux, BitLocker on Windows) on the host disk
so the DB and images are protected if the machine is lost or stolen.
Application-layer encryption of individual images is unnecessary overhead
for a home album on top of that.

## Secrets at rest

- All secrets (DB credentials, JWT key, admin bootstrap password) live in
  `.env`, which is gitignored and read via `env_file:` in
  `docker-compose.yml` — never inlined into the compose file itself.
- Lock down `.env`'s permissions on the host: `chmod 600 .env`.

### Rotating the JWT signing key

1. Generate a new key: `openssl rand -base64 48`.
2. Update `Jwt__Key` in `.env`.
3. `docker compose up -d --force-recreate api`.

Every previously-issued access token stops validating immediately (they're
short-lived by design, so this is expected and fine — everyone just logs in
again).

### Rotating the DB password

This one has a gotcha: MySQL's `MYSQL_PASSWORD`/`MYSQL_ROOT_PASSWORD`
environment variables are **only applied when the data volume is first
initialized**. Changing them in `.env` on an already-running database does
nothing on their own — you have to actually change the live password too:

1. Update `MYSQL_PASSWORD`/`MYSQL_ROOT_PASSWORD` in `.env`.
2. Change the password inside the running database:
   ```sh
   docker compose exec db mysql -u root -p"$OLD_ROOT_PASSWORD" -e \
     "ALTER USER 'photoalbum'@'%' IDENTIFIED BY 'NEW_PASSWORD'; FLUSH PRIVILEGES;"
   ```
   (and the same for `root`@`localhost` if you're rotating the root password too)
3. `docker compose up -d --force-recreate api backup` so both pick up the
   new `ConnectionStrings__DefaultConnection`/`MYSQL_ROOT_PASSWORD`.
