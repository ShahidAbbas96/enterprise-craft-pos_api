# Deploying to a new machine

Target: a Windows machine that already has **PostgreSQL** installed. This guide gets the
backend + frontend running there as a single process on a single port, so PostgreSQL and the
.NET runtime are the only things you install on that machine.

## How this works

- The API now applies EF Core migrations and seeds roles + a default admin login on **every
  startup, in every environment** (`DbSeeder.BootstrapAsync`, called unconditionally in
  `Program.cs`) — point it at an empty database and the schema + a login are created
  automatically. It seeds no sample/demo data outside local development.
- The API also serves the Angular production build directly (`app.UseStaticFiles()` +
  `app.MapFallbackToFile("index.html")` in `Program.cs`), if you copy the Angular build's files
  into the API's `wwwroot` folder before publishing. One process, one port, no CORS to configure,
  no second web server to install.

## 1. On this (build) machine

You need Node.js + the .NET SDK here — not on the target machine.

```bash
# Frontend: production build
cd "enterprise-craft-pos"
npm install
npx ng build --configuration production
# Output: dist/enterprise-craft-pos/browser/

# Backend: publish
cd "../enterprise-craft-pos_api/src/RetailCommerce.Api"
dotnet publish -c Release -o ../../publish
```

Copy the Angular output into the publish folder's `wwwroot`:

```bash
mkdir -p ../../publish/wwwroot
cp -r ../../../enterprise-craft-pos/dist/enterprise-craft-pos/browser/* ../../publish/wwwroot/
```

Zip the `publish` folder and copy it to the target machine (e.g. `C:\RetailCommerce\`).

> If you'd rather build a self-contained executable that doesn't need the .NET runtime installed
> on the target machine at all, add `-r win-x64 --self-contained true` to the `dotnet publish`
> command above (produces a larger output, but the target machine then only needs PostgreSQL).

## 2. On the target machine

### a) PostgreSQL — create the database

In `psql` or pgAdmin:

```sql
CREATE DATABASE retailcommerce;
CREATE USER retailcommerce_app WITH PASSWORD 'choose-a-strong-password';
GRANT ALL PRIVILEGES ON DATABASE retailcommerce TO retailcommerce_app;
```

### b) Install the ASP.NET Core 8.0 Runtime

Only needed if you did **not** publish self-contained in step 1. Download "ASP.NET Core Runtime
8.0.x — Windows Hosting Bundle" (if fronting with IIS) or plain "ASP.NET Core Runtime 8.0.x —
Windows x64" (if just running the .exe directly) from Microsoft's .NET download page.

### c) Configure secrets via environment variables

Nothing sensitive is committed to source control — `ConnectionStrings:Default` and `Jwt:Key`
ship empty in `appsettings.json` and **must** be supplied on the target machine. Set these as
system/user environment variables (double-underscore `__` = nested JSON key), e.g. in an admin
PowerShell:

```powershell
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
[Environment]::SetEnvironmentVariable("ASPNETCORE_URLS", "http://0.0.0.0:5012", "Machine")
[Environment]::SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Port=5432;Database=retailcommerce;Username=retailcommerce_app;Password=choose-a-strong-password", "Machine")
[Environment]::SetEnvironmentVariable("Jwt__Key", "<generate a random 32+ character string>", "Machine")
```

Generate a `Jwt__Key` value with e.g. `[Convert]::ToBase64String((1..32|%{Get-Random -Max 256}))`
in PowerShell — any long random string works, it just needs to be secret and stable (changing it
invalidates every issued login token).

You do **not** need to set `Cors__AllowedOrigins` for this same-origin, single-process setup —
CORS only matters if the Angular files are served from a different origin than the API (see
"Alternative: separate hosting" below).

### d) Run it

```powershell
cd C:\RetailCommerce\publish
dotnet RetailCommerce.Api.dll
```

(Or `.\RetailCommerce.Api.exe` if you published self-contained.) First run against the empty
database logs migration + seeding activity and creates the schema — this takes a few seconds
longer than subsequent starts.

To keep it running as a background service instead of a console window, either:
- Register it as a Windows Service (`sc create RetailCommercePos binPath= "C:\RetailCommerce\publish\RetailCommerce.Api.exe"` for a self-contained publish, or wrap `dotnet RetailCommerce.Api.dll` with a tool like NSSM), or
- Host it behind IIS using the ASP.NET Core Module (requires the Hosting Bundle from step b).

### e) First login

Browse to `http://<machine-ip-or-hostname>:5012`. Log in with:

- Email: `admin@retailcommerce.local`
- Password: `ChangeMe!123`

**Change this password immediately** (Settings → Profile & Security) — it's a well-known value
baked into the source code, not a secret generated for this deployment. Then, as that admin:

1. Settings → Configuration → Users: create real user accounts (assign each a Store + role).
2. Settings → Configuration → (Departments/Categories/etc.), or Data Management → Import
   Products/Import Inventory: bring in your real catalog — nothing is pre-seeded.
3. Add your real Store(s)/Warehouse(s) before assigning users to them (there is no default store
   on a fresh deploy).

## Alternative: separate hosting (API and frontend on different origins/ports)

If you'd rather serve the Angular build from IIS/nginx on its own site instead of the API's
`wwwroot`, two things change:

1. Edit `src/environments/environment.ts`'s `apiBaseUrl` to the API's full URL (e.g.
   `https://pos-api.yourdomain.local/api`) before running `ng build --configuration production`.
2. Set `Cors__AllowedOrigins__0` (and `__1`, `__2`, ... for more) to the exact origin(s) the
   Angular site is served from, e.g. `Cors__AllowedOrigins__0=https://pos.yourdomain.local`.

## Updating later

Repeat step 1 (rebuild + publish + copy `wwwroot`) and replace the files on the target machine;
restart the process. Any new EF Core migrations apply automatically on the next startup — no
manual `dotnet ef database update` step required.
