# PhotoAlbum

A self-hosted, Dockerized photo album for a home server. Family accounts, admin-managed, no open registration, HTTPS by default on the LAN.

## Stack

- **API**: ASP.NET Core 8 Web API (`PhotoAlbumApi`, assembly `Backend_dev`), EF Core against MySQL (`MySql.EntityFrameworkCore`, the Oracle provider)
- **Frontend**: Blazor WebAssembly (`PhotoAlbumBlazor`), published as static files and served by nginx
- **Reverse proxy**: Caddy, automatic HTTPS (self-signed via `tls internal` for LAN use; drop-in Let's Encrypt if exposed to the internet)
- **Database**: MySQL 8.0
- **Tests**: `UnitTests` (xUnit), `k6_Tests` (k6 load/smoke tests)
- **CI**: GitHub Actions — build, unit tests, Docker image builds on every push

## Architecture

```
                      ┌─────────┐
  LAN clients ──────► │  Caddy  │  automatic HTTPS, only service with published ports
                      └────┬────┘
                 ┌─────────┴─────────┐
                 ▼                   ▼
           ┌───────────┐       ┌───────────┐
           │  api:8080 │       │  web:8080 │  nginx serving the Blazor WASM build
           │ ASP.NET 8 │       └───────────┘
           └─────┬─────┘
                 ▼
           ┌───────────┐       ┌────────────┐
           │  db (MySQL)│◄─────│   backup   │  nightly mysqldump + image copy
           └───────────┘       └────────────┘
```

All services sit on an internal Docker network; only `proxy` publishes host ports (80/443).

## Security model

- **Passwords**: BCrypt (`BCrypt.Net-Next`), never plaintext.
- **Users**: a handful of family accounts, admin-created only. No self-registration. On first startup, if the `Users` table is empty, one admin account is bootstrapped from `ADMIN_USERNAME`/`ADMIN_PASSWORD`.
- **Tokens**: short-lived JWT access tokens (no refresh tokens yet — re-login when they expire).
- **Uploads**: random server-side (GUID) filenames, a size cap, and file-type validation by magic bytes rather than trusting the client's extension/Content-Type.
- **Thumbnails**: generation is bounded against decompression-bomb-style files (small on-disk size, maliciously huge declared pixel dimensions) via an ImageSharp memory allocation cap; a file that would exceed it falls back to serving the original instead of ballooning API memory.
- **Image-by-URL**: SSRF-guarded — blocks private/loopback/link-local/reserved IP ranges (including on every redirect hop, not just the initial request) before making any outbound call.
- **Rate limiting**: per-IP fixed-window limits on `/api/users/login` and the public share endpoints.
- **Secrets**: JWT signing key, DB connection string, and admin bootstrap credentials all come from environment variables (`.env`, gitignored). The API fails fast at startup if the JWT key is missing or under 32 bytes.
- **Containers**: run as non-root; DataProtection keys persist in a named volume so they survive container recreation.
- **2FA**: none yet — acceptable for a LAN-only deployment. TOTP for the admin account is a prerequisite before ever exposing this to the internet.

## Features

- Albums and photos: create, upload, edit, soft-delete with undo, download
- Upload by file or by URL (SSRF-guarded)
- Lightbox viewer, search, pagination, bulk delete, drag-and-drop upload
- Global "all photos" gallery across albums
- Shareable, revocable album links (public, unauthenticated access via an opaque token — deliberately not behind `[Authorize]`)
- Thumbnails: grid/preview views use a small generated JPEG instead of the full-resolution original; the lightbox and downloads still use the original. Generated on first request and cached to disk.
- Trash auto-purge: soft-deleted albums/photos (and their thumbnails) are permanently deleted after a retention window (default 30 days, `Trash__RetentionDays`)
- Storage-usage dashboard (admin-only): shows total size and file count for both uploaded-photo storage and backup storage
- Admin user management (create/update/delete family accounts)
- Structured request logging with per-request correlation IDs (`X-Correlation-Id` response header, tied together in the logs)
- `/health` endpoint for container healthchecks

## API overview

All routes are under `/api`. Endpoints without a note below require a `Bearer` JWT; admin-only routes additionally require the `Admin` role.

| Area | Routes |
|---|---|
| Auth | `POST /api/users/login` *(no auth required)* |
| Users | `GET /api/users`, `GET /api/users/{id}`, `POST /api/users`, `PUT /api/users/{id}`, `DELETE /api/users/{id}` — all admin-only |
| Albums | `GET/POST /api/v{version}/albums`, `GET/PUT /api/v{version}/albums/{id}`, `DELETE /api/v{version}/albums/{id}`, `PUT /api/v{version}/albums/undo-delete/{id}` (v1 and v2 both supported) |
| Photos | `GET/POST /api/photos`, `POST /api/photos/upload`, `GET /api/photos/{id}`, `PUT /api/photos/{id}`, `GET /api/photos/download/{id}`, `GET /api/photos/thumbnail/{id}`, `DELETE /api/photos/{id}`, `PUT /api/photos/undo-delete/{id}` |
| Sharing | `POST /api/shares`, `GET /api/shares` (your own links), `DELETE /api/shares/{token}` — authenticated. `GET /api/shares/{token}`, `GET /api/shares/{token}/photos/{photoId}`, `GET /api/shares/{token}/photos/{photoId}/thumbnail` — public, token-gated |
| Storage | `GET /api/storage` — admin-only, photo/backup storage usage |
| Ops | `GET /health` |

Full request/response shapes are documented via Swagger UI at `/swagger` when the API is running.

## Running it

### With Docker (recommended)

```bash
cp .env.example .env
# fill in MYSQL_ROOT_PASSWORD, MYSQL_PASSWORD, Jwt__Key (openssl rand -base64 48),
# ADMIN_USERNAME, ADMIN_PASSWORD (12+ chars) in .env

docker compose up --build
```

The site is reachable at `https://localhost/` (or whatever `SITE_ADDRESS` you set in `.env`) via Caddy's self-signed certificate. Log in with the admin credentials from `.env`.

### Locally, without Docker

Needs a running MySQL instance and the same environment variables (see `.env.example`) set via `dotnet user-secrets`, environment variables, or `appsettings.Development.json`.

```bash
dotnet restore Backend_dev.generated.sln
dotnet build Backend_dev.generated.sln
dotnet run --project PhotoAlbumApi
```

## Backups

The `backup` service runs nightly `mysqldump` plus a copy of the uploaded-images volume to `./backups` (gitignored — contains real data). Retention is controlled by `BACKUP_RETENTION_DAYS`. The `api` container also mounts `./backups` read-only so the storage-usage dashboard can report its size. See [BACKUP.md](BACKUP.md) for restore instructions.

## Testing

```bash
dotnet build Backend_dev.generated.sln
dotnet test UnitTests/UnitTests.csproj
```

`k6_Tests/` has a login smoke test and a combined list/upload load test, meant to run against a live deployment:

```bash
k6 run -e BASE_URL=https://localhost -e ADMIN_USERNAME=... -e ADMIN_PASSWORD=... k6_Tests/login.js
k6 run -e BASE_URL=https://localhost -e ADMIN_USERNAME=... -e ADMIN_PASSWORD=... k6_Tests/load_tests.js
```

CI (`.github/workflows/ci.yml`) runs the build, unit tests, and both Docker image builds on every push.

## Project layout

```
PhotoAlbumApi/        ASP.NET Core 8 Web API
PhotoAlbumBlazor/      Blazor WebAssembly frontend
UnitTests/             xUnit tests for the API
k6_Tests/               k6 load/smoke tests
scripts/                backup.sh / restore.sh, run inside the backup container
docker-compose.yml      full stack: db, api, web, proxy, backup
Caddyfile               reverse proxy config
BACKUP.md               backup/restore runbook
```

## Environment variables

See [.env.example](.env.example) for the full list with explanations. At minimum you need `MYSQL_ROOT_PASSWORD`, `MYSQL_PASSWORD`, `Jwt__Key`, `ADMIN_USERNAME`, and `ADMIN_PASSWORD`.
