# Dev vs production (API + CORS)

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
| **Dev** (`ng serve`) | `src/environments/environment.ts` → `apiBaseUrl`, `imageServerUrl`. |
| **Production build** | `src/environments/environment.prod.ts` → same fields, then `ng build`. |

Set `apiBaseUrl` to `null` in either file to use the old automatic `Configuration` URLs by hostname.
