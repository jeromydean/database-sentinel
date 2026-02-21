# Keycloak + PostgreSQL (Docker / Podman)

Runs Keycloak and a single PostgreSQL instance used for:

- **Keycloak** – database `keycloak`, user `keycloak`
- **API metrics** – database `sentinel`, user `sentinel` (BlueFence.DatabaseSentinel.Api)

The same `docker-compose.yml` works with **Docker** or **Podman**. Keycloak is configured for **HTTPS** in development using a self-signed certificate you generate once.

## Quick start (development)

### 1. Generate the Keycloak certificate (one-time, Windows)

Keycloak requires a PKCS12 keystore for HTTPS. For local development, generate a self-signed cert:

From the repo root, in PowerShell **as Administrator** (or run the script and approve the elevation prompt):

```powershell
.\docker\generate-certificate.ps1
```

This creates `docker/certs/keycloak.pfx` (password: `password`), adds it to the machine’s Trusted Root store so browsers don’t complain, and is ignored by git. If the cert expires (default 365 days), run the script again.

### 2. Start the stack

From the repository root:

```bash
# Docker
docker compose up -d

# Podman
podman compose up -d
```

### 3. First-run Keycloak setup (realm, clients, admin role)

After Keycloak is running, create the application realm, clients, and admin role. From the repo root, in PowerShell:

```powershell
.\docker\keycloak-setup.ps1
```

This creates:

- **Realm**: `database-sentinel`
- **API client**: `database-sentinel-api` (confidential) – for the API to validate tokens; get its secret in Keycloak Admin → Clients → database-sentinel-api → Credentials
- **UI client**: `database-sentinel-ui` (public) – for the Avalonia app (login flows)
- **Realm role**: `sentinel-admin`
- **Initial user**: `dsadmin` / `dsadmin` – created in the realm with the `sentinel-admin` role so you can sign in from the app immediately

The script is idempotent: safe to run again (skips what already exists). To use a different initial username or password:

```powershell
.\docker\keycloak-setup.ps1 -InitialUserName "myuser" -InitialUserPassword "mypass"
```

Defaults assume Keycloak at https://localhost:8443 and admin/admin; override with parameters if needed, e.g.:

```powershell
.\docker\keycloak-setup.ps1 -KeycloakUrl "https://localhost:8443" -AdminPassword "YourAdminPassword"
```

### 4. Access

- **Keycloak Admin**: https://localhost:8443/admin (admin / admin)
- **PostgreSQL**: localhost:5432
  - Keycloak DB: `Host=localhost;Port=5432;Database=keycloak;Username=keycloak;Password=keycloak_db_password`
  - API (sentinel): `Host=localhost;Port=5432;Database=sentinel;Username=sentinel;Password=sentinel_db_password`

---

## Podman: "Cannot connect to Podman" / connection refused

On Windows or macOS, Podman runs inside a small Linux VM. If you see:

```text
Cannot connect to Podman. Please verify your connection...
unable to connect to Podman socket: ... connection refused
```

the Podman machine is not running. Start it:

```bash
podman machine start
```

If you don’t have a machine yet:

```bash
podman machine init
podman machine start
```

Then run `podman compose up -d` again. Check status with `podman machine list`.

---

## Init scripts

Scripts in `postgres-init/` run only on first start (empty data volume). To re-run them, remove the volume and start again:

```bash
docker compose down -v
docker compose up -d
```

---

## Production deployment

**Do not use the development certificate or this compose file as-is in production.**

- **Keycloak**
  - Use a proper TLS certificate (e.g. from your PKI or a public CA), not a self-signed dev cert.
  - Configure `KC_HOSTNAME` and proxy/HTTPS according to your environment (reverse proxy, load balancer).
  - Use strong, secret-managed passwords for `KC_BOOTSTRAP_ADMIN_*` and database credentials; do not commit them.
- **PostgreSQL**
  - Run with restricted network access, strong passwords, and backups. Prefer a managed database or a dedicated Postgres instance, not the same container as other apps.
  - Create the `sentinel` database and user via your own automation or migration; the `postgres-init` scripts in this repo are for local dev only.
- **Secrets**
  - Store passwords and certs in a secret manager or environment supplied at deploy time; never hardcode production secrets in repo or compose.

Use this compose as a reference for **local development only**. For production, deploy Keycloak and PostgreSQL according to your platform (Kubernetes, cloud services, etc.) and security standards.
