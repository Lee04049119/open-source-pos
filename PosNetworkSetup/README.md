# PosNetworkSetup (Windows Forms)

Updates **LAN IP / PC name** in one click (writes `appsettings.Local.json` + `app-runtime-config.json`).

## If build or run fails — read this first

### 1. Install requirements

| Requirement | Why |
|-------------|-----|
| **.NET 8 SDK** | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **“.NET desktop development”** workload in Visual Studio | WinForms needs **Windows Desktop**, not only “ASP.NET” |

In **Visual Studio Installer** → **Modify** your VS edition → check **.NET desktop development** → Apply.

### 2. Build **this project only** (not the big solution)

Building `open-source-pos.sln` can fail on the **SQL database project** (`OpenSourcePosDB`). That does **not** mean PosNetworkSetup is broken.

Use the **small solution** or the script:

```powershell
# Option A — standalone solution (recommended)
cd C:\GITHUB\open-source-pos
dotnet build PosNetworkSetup.sln
dotnet run --project PosNetworkSetup\PosNetworkSetup.csproj
```

```powershell
# Option B — double-click or run
cd C:\GITHUB\open-source-pos\PosNetworkSetup
.\build-and-run.bat
```

```powershell
# Option C — Visual Studio
# Open PosNetworkSetup.sln (repo root), set PosNetworkSetup as startup, F5
```

### 3. No build at all (PowerShell fallback)

From repo root (replace IP and path):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Set-PosLanConfig.ps1 `
  -RepoRoot "C:\GITHUB\open-source-pos" `
  -HostName "192.168.0.15"
```

Then restart the API and `ng serve`.

### 4. Typical errors

| Error | Fix |
|-------|-----|
| `'dotnet' is not recognized` | Install .NET 8 SDK; restart terminal |
| `NETSDK1100` / WindowsDesktop / `UseWindowsForms` | Install **.NET desktop development** workload |
| `The target framework 'net8.0-windows' was not found` | Install .NET 8 SDK (full SDK, not runtime only) |
| Build fails on `OpenSourcePosDB` | Use `PosNetworkSetup.sln` or `dotnet build PosNetworkSetup\PosNetworkSetup.csproj` |
| App starts then nothing | Check `bin\Debug\net8.0-windows\PosNetworkSetup.exe` exists after build |

## Using the WinForms app

1. **Browse** → repo root (folder with `open-source-pos` and `open-source-pos-frontend`).
2. **LAN host** → current DHCP IP or PC name.
3. **Save all** → restart API + Angular.

Files written (gitignored):

- `open-source-pos/appsettings.Local.json`
- `open-source-pos-frontend/src/assets/app-runtime-config.json`
