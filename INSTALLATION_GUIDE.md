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

Right-click the **Start** button → **Windows PowerShell (Admin)** (or search "PowerShell",
right-click, "Run as administrator").

Paste each line below one at a time, pressing Enter after each. **Replace the placeholders**
(shown in `<angle brackets>`) with your real values before pasting:

```powershell
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
[Environment]::SetEnvironmentVariable("ASPNETCORE_URLS", "http://0.0.0.0:5012", "Machine")
[Environment]::SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Port=<your-postgres-port>;Database=retailcommerce;Username=retailcommerce_app;Password=<the-password-from-step-2>", "Machine")
```

Then generate and set a random security key (paste this whole block as one command):

```powershell
$key = [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
[Environment]::SetEnvironmentVariable("Jwt__Key", $key, "Machine")
```

**Close this PowerShell window** now — this matters. Environment variables only take effect in
windows opened *after* they're set.

---

## Step 4 — Install it as a permanent background service

Open a **new** PowerShell window as Administrator (same as Step 3) and run:

```powershell
sc.exe create RetailCommercePos binPath= "C:\RetailCommerce\RetailCommerce.Api.exe" start= auto
sc.exe start RetailCommercePos
```

(The space right after each `=` matters — don't remove it.)

This registers the app to start automatically every time the computer turns on, running quietly
in the background — no window, nothing to keep open.

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

- **Service won't start** — double-check Step 3's environment variables were set *before*
  installing the service (Step 4), and in a PowerShell window opened after Step 3. If unsure,
  redo Step 3, then remove and recreate the service:
  ```powershell
  sc.exe stop RetailCommercePos
  sc.exe delete RetailCommercePos
  ```
  then repeat Step 4.
- **Blank/white page at `http://localhost:5012`** — the `wwwroot` folder is missing or in the
  wrong place; re-check Step 1 (`RetailCommerce.Api.exe` and `wwwroot` must be in the same
  folder).
- **Can't log in / errors after logging in** — usually a wrong database password or port in
  Step 3's `ConnectionStrings__Default`. Re-verify against what you set in Step 2's pgAdmin.
- **Can't reach it from other computers in the shop** — Windows Firewall may be blocking it.
  Open **Windows Defender Firewall with Advanced Security** → **Inbound Rules** → **New Rule…**
  → **Port** → TCP → Specific local port `5012` → **Allow the connection** → apply to all
  profiles.
