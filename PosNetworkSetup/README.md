# PosNetworkSetup (Windows Forms)

Small utility to update **LAN IP or PC name** in one click when DHCP changes your address.

**Save all** writes:

1. `open-source-pos/appsettings.Local.json` — `Lan:Host` and ports (API adds matching CORS origins automatically).
2. `open-source-pos-frontend/src/assets/app-runtime-config.json` — Angular `apiBaseUrl` / `apiBaseUrlHttps` and image URLs.

Then restart the ASP.NET API and `ng serve` (or rebuild the Angular app).

Repository root = folder that contains both `open-source-pos` and `open-source-pos-frontend`.

Last used repo path is stored under `%AppData%\OpenSourcePosSetup\last_repo_root.txt`.
