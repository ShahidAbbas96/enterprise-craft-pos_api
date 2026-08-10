# Running Retail Commerce POS on this machine

You should have two folders copied over from the build machine:

1. **Backend publish output** — contains `RetailCommerce.Api.dll` (or `RetailCommerce.Api.exe`
   if published self-contained), `appsettings.json`, etc.
2. **Frontend build output** — the Angular production build (`dist/enterprise-craft-pos/browser`
   contents: `index.html`, `main-*.js`, etc.)

Follow these steps in order.

---

## Step 1 — Combine the two builds

The API serves the Angular app itself, so the frontend files must sit inside the backend
folder's `wwwroot` subfolder.

1. Open the backend publish folder (wherever you copied it, e.g. `C:\RetailCommerce\`).
2. If there is no `wwwroot` folder inside it, create one.
3. Copy **everything inside** the frontend build folder (`index.html` and all the `.js`/`.css`
   files alongside it — not the folder itself) into that `wwwroot` folder.

You should end up with a structure like:

```
C:\RetailCommerce\
    RetailCommerce.Api.dll
    appsettings.json
    ... (other backend files)
    wwwroot\
        index.html
        main-XXXXXXXX.js
        polyfills-XXXXXXXX.js
        styles-XXXXXXXX.css
        ... (other frontend files)
```

If you already copied the frontend files into `wwwroot` before transferring, skip this step.

---

## Step 2 — Install prerequisites on this machine

- **PostgreSQL** — already installed, per your setup.
- **.NET runtime** — only needed if the backend was published *without* `--self-contained`. Check:
  open the backend folder — if you see a `RetailCommerce.Api.exe` that's tens of MB and a `runtimes`
  folder next to it, it's self-contained and you can skip this. Otherwise, install **ASP.NET Core
  Runtime 8.0.x (Windows x64)** from Microsoft's .NET download page (search "download .NET 8.0
  runtime", pick "ASP.NET Core Runtime" — not just ".NET Runtime").

Nothing else is required — no Node.js, no IIS, no nginx.

---

## Step 3 — Create the database

Open `psql` (or pgAdmin) on this machine and run:

```sql
CREATE DATABASE retailcommerce;
CREATE USER retailcommerce_app WITH PASSWORD 'choose-a-strong-password';
GRANT ALL PRIVILEGES ON DATABASE retailcommerce TO retailcommerce_app;
```

(Reuse the existing PostgreSQL superuser account instead if you'd rather not create a dedicated
app login — either works, just note whichever connection string you end up with for Step 4.)

---

## Step 4 — Configure the app

The app needs a database connection string and a JWT signing key — neither ships with a real
value, for security. Set them as **machine-level environment variables** (open an
**Administrator** PowerShell window):

```powershell
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
[Environment]::SetEnvironmentVariable("ASPNETCORE_URLS", "http://0.0.0.0:5012", "Machine")
[Environment]::SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Port=5432;Database=retailcommerce;Username=retailcommerce_app;Password=choose-a-strong-password", "Machine")
[Environment]::SetEnvironmentVariable("Jwt__Key", "REPLACE-WITH-A-LONG-RANDOM-SECRET", "Machine")
```

Generate a random value for `Jwt__Key` (32+ characters) instead of typing your own, e.g. right
in that same PowerShell window:

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

Copy the output and use it as the `Jwt__Key` value above.

> `ASPNETCORE_URLS=http://0.0.0.0:5012` makes the app listen on **all** network interfaces on
> port 5012 (not just `localhost`) — that's what lets other POS terminals/computers on the same
> network reach it via this machine's IP address. If this machine is the *only* place the app
> will ever be opened from, `http://localhost:5012` works too.

**Close and reopen** any PowerShell/terminal window after setting these — machine-level
environment variables only apply to processes started *after* they're set.

---

## Step 5 — Run it

Open a terminal in the backend publish folder and run:

```powershell
cd C:\RetailCommerce
dotnet RetailCommerce.Api.dll
```

(If published self-contained, run `.\RetailCommerce.Api.exe` instead — no `dotnet` prefix needed.)

The **first** time you run this against the empty database, it will take a few extra seconds —
it's creating every table and the default login. You should see log lines mentioning migrations
and a seeded admin user. Leave this window open; closing it stops the app.

---

## Step 6 — Open it and log in

From a browser on this machine (or any other machine on the same network, using this machine's
IP address instead of `localhost`):

```
http://localhost:5012
```

Log in with:

- **Email:** `admin@retailcommerce.local`
- **Password:** `ChangeMe!123`

**Immediately go to Settings → Profile & Security and change this password** — it's a fixed
value baked into the source code, not something generated for you, so anyone who's seen this
repo knows it.

Then, before using the POS for real:

1. **Settings → Configuration → Users** — create real user accounts (cashiers, managers), each
   assigned to a Store and role.
2. Add your real **Store(s)** and **Warehouse(s)** — nothing is pre-created.
3. **Settings → Configuration** (Departments/Categories/etc.), or **Data Management → Import
   Products / Import Inventory** — bring in your real product catalog. No demo products are
   seeded on this deployment.

---

## Keeping it running (optional but recommended)

Running `dotnet RetailCommerce.Api.dll` in a terminal window stops the app the moment that
window closes or the machine restarts. To keep it running in the background permanently, install
it as a Windows Service:

```powershell
# Framework-dependent publish:
sc.exe create RetailCommercePos binPath= "dotnet C:\RetailCommerce\RetailCommerce.Api.dll" start= auto

# Self-contained publish:
sc.exe create RetailCommercePos binPath= "C:\RetailCommerce\RetailCommerce.Api.exe" start= auto
```

Then start it with `sc.exe start RetailCommercePos` (or via `services.msc`). It will now also
start automatically on reboot. Note the space after each `=` in the command above — `sc.exe` is
picky about that syntax.

---

## Troubleshooting

- **Blank page / 404 at `http://localhost:5012`** — the frontend files weren't found in
  `wwwroot`. Re-check Step 1: `wwwroot\index.html` must exist next to `RetailCommerce.Api.dll`.
- **"Unable to configure HTTPS endpoint..."** — something is trying to launch with an `https`
  profile. This app is meant to run over plain HTTP; make sure `ASPNETCORE_URLS` starts with
  `http://`, not `https://`.
- **Can't reach it from another computer** — check this machine's firewall allows inbound
  connections on port 5012, and confirm `ASPNETCORE_URLS` uses `0.0.0.0` (not `localhost`).
- **Login fails / 500 errors** — double check `ConnectionStrings__Default` (correct password,
  PostgreSQL actually running, database name matches) and that `Jwt__Key` was actually set
  before the app was started (environment variables set *after* the app starts have no effect).
