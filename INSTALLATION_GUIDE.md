# Retail Commerce POS — Installation Guide

This guide sets up the POS system to run permanently on this computer — it starts automatically
with Windows and keeps running in the background, so staff never need to open or manage any
technical windows day-to-day.

You need:
- This computer has **PostgreSQL** already installed.
- The file **`RetailCommercePos-SelfContained-win-x64.zip`** that was shared with you.
- About 15 minutes, and an account with **Administrator** access on this computer.

---

## Step 1 — Extract the files

1. Copy `RetailCommercePos-SelfContained-win-x64.zip` to this computer (e.g. Desktop or Downloads).
2. Right-click it → **Extract All…**
3. Extract it to `C:\RetailCommerce` (type that exact path in the extract dialog, or extract
   anywhere and then move the folder so it ends up at `C:\RetailCommerce`).
4. Open `C:\RetailCommerce` and confirm you see `RetailCommerce.Api.exe` and a `wwwroot` folder
   inside it. If you don't, the extraction went into a subfolder — move the contents up one level
   so `RetailCommerce.Api.exe` sits directly inside `C:\RetailCommerce`.

---

## Step 2 — Create the database

Open **pgAdmin** (installed alongside PostgreSQL — search for it in the Start menu).

1. Connect to your PostgreSQL server (it will ask for the password you set when installing
   PostgreSQL).
2. Right-click **Databases** → **Create** → **Database…**
   - Name: `retailcommerce`
   - Click **Save**.
3. Right-click **Login/Group Roles** → **Create** → **Login/Group Role…**
   - General tab → Name: `retailcommerce_app`
   - Definition tab → Password: choose a strong password and **write it down** — you need it in
     the next step.
   - Privileges tab → turn on **Can login?**
   - Click **Save**.
4. Right-click the `retailcommerce` database → **Properties** → **Security** tab → grant `ALL`
   privileges to `retailcommerce_app`, then **Save**.
   (If that Security tab looks unfamiliar, it's simpler to instead open pgAdmin's **Query Tool**
   against the `retailcommerce` database and run:
   `GRANT ALL PRIVILEGES ON DATABASE retailcommerce TO retailcommerce_app;`)

Note the PostgreSQL **port** you're using — check pgAdmin's connection properties for your
server (Properties → Connection tab). It's `5432` unless you changed it during install.

---

## Step 3 — Configure the app

Everything the app needs — which port to listen on, the database password, and the security
key — goes in one small JSON file next to the exe, **not** environment variables. This matters
specifically for Step 4's Windows Service: `sc.exe`'s parent process only reads machine-level
environment variables once, at boot, so a variable set *after* that (as an `[Environment]::
SetEnvironmentVariable(..., "Machine")` command run on an already-running machine would be)
never reaches a service — even a freshly-created one, and even a plain `.exe` run directly from
a new terminal — until the next reboot. Until then, the app silently falls back to defaults
(an empty database connection, and port 5000 instead of 5012), which is exactly what causes
"network error" on every screen and `ERR_CONNECTION_REFUSED` in the browser. A file the app
reads directly has no such timing gotcha, so this guide doesn't use environment variables at all.

Generate a random security key first — paste this in PowerShell and copy the value it prints:

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

Create `C:\RetailCommerce\appsettings.Production.json` (same folder as `RetailCommerce.Api.exe`)
with this content, filling in your real Postgres port/password from Step 2 and the key you just
generated:

```json
{
  "Urls": "http://0.0.0.0:5012",
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=<your-postgres-port>;Database=retailcommerce;Username=retailcommerce_app;Password=<the-password-from-step-2>"
  },
  "Jwt": {
    "Key": "<the-key-you-just-generated>"
  }
}
```

Save it as plain text (Notepad works fine — just make sure it's not saved as `.json.txt`; in
Notepad's Save dialog, set "Save as type" to **All Files** and type the filename with `.json` on
the end). ASP.NET Core loads this automatically on startup — the app defaults to the
`Production` environment whenever `ASPNETCORE_ENVIRONMENT` isn't set, which is exactly the case
here, so `appsettings.Production.json` is picked up with no environment variable needed at all.

If you already ran the environment-variable commands from an earlier version of this guide,
clear them so they can't reappear and silently override this file after a future reboot:

```powershell
[Environment]::SetEnvironmentVariable("ASPNETCORE_URLS", $null, "Machine")
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", $null, "Machine")
[Environment]::SetEnvironmentVariable("ConnectionStrings__Default", $null, "Machine")
[Environment]::SetEnvironmentVariable("Jwt__Key", $null, "Machine")
```

---

## Step 4 — Install it as a permanent background service

Open a **new** PowerShell window as Administrator (same as Step 3) and run:

```powershell
sc.exe create RetailCommercePos binPath= "C:\RetailCommerce\RetailCommerce.Api.exe" start= auto
sc.exe start RetailCommercePos
```

(The space right after each `=` matters — don't remove it.)

This registers the app to start automatically every time the computer turns on, running quietly
in the background — no window, nothing to keep open. `start= auto` is what makes this happen: it
tells Windows to launch the service on every boot, with no manual action needed — including after
a shutdown, restart, or power outage. You never need to start it by hand; just turn the computer
on and it's running within a few seconds.

Check it's running:

```powershell
sc.exe query RetailCommercePos
```

You should see `STATE: 4 RUNNING`. If instead it shows `STOPPED`, see Troubleshooting below.

---

## Step 5 — First-time login

Open a web browser (Chrome, Edge) on this computer and go to:

```
http://localhost:5012
```

(To reach it from other computers/POS terminals on the same shop network instead, use this
computer's IP address, e.g. `http://192.168.1.50:5012` — ask whoever set up the network for that
address, or run `ipconfig` in PowerShell and look for "IPv4 Address".)

Log in with:

- **Email:** `admin@retailcommerce.local`
- **Password:** `ChangeMe!123`

**Change this password immediately**: Settings → Profile & Security. This default password is
public (it's in the source code), so don't leave it active.

Then, before the first real sale:

1. **Settings → Configuration → Users** — create a login for each staff member, assigning their
   role and store.
2. Add your real **Store(s)** and **Warehouse(s)**.
3. Bring in your product catalog: **Settings → Configuration** to set up categories etc., or
   **Data Management → Import Products / Import Inventory** to bulk-load from Excel.

The app is now ready for daily use — nothing further needs to be started or managed. It will
keep running even after Windows restarts.

---

## Day-to-day: is it working?

If the POS screen won't load one day, check the service status (Administrator PowerShell):

```powershell
sc.exe query RetailCommercePos
```

- **RUNNING** but the page won't load → check this computer's network/Wi-Fi, or that PostgreSQL
  itself is running (search "Services" in the Start menu, look for a PostgreSQL entry).
- **STOPPED** → restart it: `sc.exe start RetailCommercePos`

---

## Troubleshooting

- **Service won't start** — check `C:\RetailCommerce\logs\app-<date>.log` for the actual reason
  (the app logs a `[Critical]` entry with the full exception if startup fails). The most common
  cause: `System.ArgumentException: Host can't be null` means `appsettings.Production.json`
  (Step 3b) is missing, misnamed (e.g. saved as `.json.txt`), or has a typo in `ConnectionStrings`
  — re-check it exists right next to `RetailCommerce.Api.exe` and is valid JSON. Also double-check
  Step 3a's environment variables were set in a PowerShell window opened *after* they were set
  (`ASPNETCORE_URLS`/`ASPNETCORE_ENVIRONMENT` only — the database password and security key live
  in the JSON file now, not environment variables, precisely to avoid this class of problem). If
  you change the JSON file, just restart the service — no need to delete and recreate it:
  ```powershell
  sc.exe stop RetailCommercePos
  sc.exe start RetailCommercePos
  ```
- **"The service did not respond to the start or control request in a timely fashion"** (Win32
  error 1053) — this means the build predates the app's Windows Service integration: older builds
  ran as a plain console app under `sc.exe`, so the process actually started fine (check
  `logs\app-<date>.log` next to the exe — you'll usually see it did) but never told Windows'
  Service Control Manager it was alive, so `sc.exe start`/`Start-Service` always timed out waiting
  for that acknowledgement. Two things had to be fixed in the app itself, not the service
  registration: (1) calling `UseWindowsService()` so the app performs the SCM handshake at all,
  and (2) starting that handshake *before* running database migrations, since migrations can take
  a few seconds and nothing acknowledges SCM until the app actually starts serving requests — get
  a newer build, stop the service, replace the files in `C:\RetailCommerce` with it, then start it
  again; no need to delete and recreate the service, the same `RetailCommercePos` registration
  still works.
- **Blank/white page at `http://localhost:5012`** — the `wwwroot` folder is missing or in the
  wrong place; re-check Step 1 (`RetailCommerce.Api.exe` and `wwwroot` must be in the same
  folder).
- **Can't log in / errors after logging in** — usually a wrong database password or port in
  `appsettings.Production.json`'s `ConnectionStrings:Default` (Step 3b). Re-verify against what
  you set in Step 2's pgAdmin.
- **Can't reach it from other computers in the shop** — Windows Firewall may be blocking it.
  Open **Windows Defender Firewall with Advanced Security** → **Inbound Rules** → **New Rule…**
  → **Port** → TCP → Specific local port `5012` → **Allow the connection** → apply to all
  profiles.
