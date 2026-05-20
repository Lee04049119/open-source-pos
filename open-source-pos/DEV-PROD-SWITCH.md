# Dev vs production (API + CORS)

## When your PC does not have a static LAN IP

Use the **PosNetworkSetup** Windows app (one **Save** updates everything):

1. Build/run the `PosNetworkSetup` project from this solution.
2. **Browse** to your repo root (folder that contains `open-source-pos` and `open-source-pos-frontend`).
3. Enter **LAN host**: current DHCP address (e.g. `192.168.0.15`) or the PC **computer name**.
4. Adjust ports if needed (defaults: Angular `4200`, API HTTP `5000`, HTTPS `5001`, images `9096`).
5. Click **Save all** — writes:
   - `open-source-pos/appsettings.Local.json` → `Lan:Host` + ports (API merges extra CORS origins automatically).
   - `open-source-pos-frontend/src/assets/app-runtime-config.json` → Angular API/image URLs.

Restart the API and `ng serve` after saving. Both generated files are **gitignored** (see `.gitignore`).

See `open-source-pos/appsettings.Local.example.json` and `open-source-pos-frontend/src/assets/app-runtime-config.sample.json` for shape.

## Backend (ASP.NET)

| Goal | What to use |
|------|----------------|
| **Dev — HTTP on port 5000** (LAN + localhost) | Visual Studio / VS Code profile **`open_source_pos`** (`http://0.0.0.0:5000`, `Development`). CORS: `appsettings.Development.json` → `Cors:AllowedOrigins`. |
| **Production — HTTPS on port 5001** | Profile **`open_source_pos_https`** (`https://0.0.0.0:5001`, `Production`). CORS: `appsettings.Production.json` → `Cors:AllowedOrigins`. Trust dev cert: `dotnet dev-certs https --trust`. |

Copy `appsettings.default.json` → `appsettings.json` for secrets and DB strings (see `.gitignore`).

If `Cors:AllowedOrigins` is **empty or missing**, the API falls back to allowing **any** origin (convenient for local experiments; prefer explicit lists for real deployments).

Add any extra browser origins (scheme + host + port) your Angular app uses to the right `appsettings.*.json` file.

## Frontend (Angular)

| Goal | Edit |
|------|------|
| **Dev** (`ng serve`) | Prefer **`app-runtime-config.json`** from PosNetworkSetup. Fallback: `src/environments/environment.ts` → `apiBaseUrl`, `imageServerUrl`. |
| **Production build** | Same runtime file before/after `ng build`, or `environment.prod.ts` fallback, then `ng build`. |

Set `apiBaseUrl` to `null` in either environment file to use the old automatic `Configuration` URLs by hostname (only if `app-runtime-config.json` is absent).
