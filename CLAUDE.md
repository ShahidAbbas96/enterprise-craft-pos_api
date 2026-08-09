# Retail Commerce POS — Production Rebuild (.NET 8 API)

> **This repo is the backend** for a two-repo system. The frontend lives at the sibling path:
> `F:\Product 2026\Enterprize craft pos Production Ready\enterprise-craft-pos` (Angular 19+).
> This same CLAUDE.md (word-for-word) is also placed in that repo so either session has full context.
> **Read this entire file before writing any code.**

## 0. What this project is

We are rebuilding a **Lovable-generated prototype** ("Retail Commerce" — an enterprise POS/retail-management app, originally React + TanStack Start + Supabase) as a **real, production-ready system**:

- **Backend:** ASP.NET Core Web API, **.NET 8**, Clean Architecture, EF Core, **PostgreSQL**, JWT + refresh tokens, Swagger/OpenAPI, SignalR where real-time sync is needed.
- **Frontend:** **Angular 19+**, reproducing the prototype's UI/UX as closely as possible (same layout, same OKLCH color system, same screens), but wired to this real API instead of mock data.
- **Client domain:** the client is a **fashion accessories retailer** ("Desire") selling **Footwear, Bags, and Jewellery** across Women's/Kids'/Men's lines — see §5 for the real Item Master hierarchy supplied by the client. This is materially different from the prototype's generic grocery/pharmacy mock data (`Basmati Rice`, `Paracetamol`, etc.) — **the mock catalog data must not be ported forward**; the real Product/Item data model must be built around the hierarchy in §5.

This CLAUDE.md is the source of truth for scope. It was produced by a full reverse-engineering pass over the original Lovable prototype's source code (React/TanStack Start app, one Supabase migration, ~83 files) plus the client's real item-master spreadsheet. Nothing in here was invented without basis — sections are marked **UNKNOWN** / **CONFIRM WITH CLIENT** where the original prototype or the supplied data didn't resolve a question. Do not silently invent business rules to fill those gaps — surface the question instead.

---

## 1. Hard rules for whoever (human or Claude) works in this repo

1. **No mock/fake data in the delivered product.** The prototype faked almost everything (see §3). Every screen must be backed by a real endpoint in this API, backed by real PostgreSQL tables.
2. **No fake auth.** The prototype's login was `setTimeout` + navigate, with zero session, zero guard, on any route. This rebuild requires real JWT + refresh-token auth from day one, enforced via `[Authorize]`/policies on every endpoint except `/auth/*`.
3. **No open-access data policy.** The prototype's DB had `USING (true) WITH CHECK (true)` RLS — anyone with the anon key had full CRUD on everything. This rebuild must enforce role- and store-scoped authorization at the API layer on every query/command.
4. **Preserve business logic that was actually correct in the prototype**, reimplemented properly:
   - Sales must validate stock availability before committing and decrement inventory atomically (was PL/pgSQL `create_sales_order`, must become a C# transactional command handler).
   - Purchase orders must **not** increase inventory on creation — only on **receiving** (was `receive_purchase_order` RPC).
   - Transfers must **not** move stock on creation — only on **completion**, and must fail loudly if source stock is insufficient (was `complete_transfer` RPC).
   - Every stock-affecting operation must write an audit row to a stock-movement ledger (was `stock_movements` table).
   - Document numbers (invoice/PO/transfer) must be generated server-side, sequential per type, never client-side `Math.random()` (the prototype's POS literally faked invoice numbers with `Math.random()` — do not repeat that).
5. **Full CRUD, not create-only.** The prototype only ever implemented Create for Products/Customers and never shipped Edit/Delete for anything. Build full CRUD for every entity in §5/§7.
6. **Real transactions for multi-table writes.** The prototype's "insert header, then insert lines, delete header if lines fail" pattern is a broken compensating-transaction hack — use real `DbContext` transactions (or a single stored procedure/function) instead.
7. **Server-side pagination and filtering** on every list endpoint. The prototype loaded entire tables client-side — fine for a demo, not for "100,000+ SKUs."
8. Ask before guessing on anything marked **UNKNOWN** / **CONFIRM WITH CLIENT** below rather than inventing a rule.

---

## 2. Repos & paths

| Repo | Path | Stack |
|---|---|---|
| API (this repo) | `F:\Product 2026\Enterprize craft pos Production Ready\enterprise-craft-pos_api` | ASP.NET Core 8 Web API (currently a fresh, unmodified `dotnet new webapi` scaffold — `Controllers/`, `Program.cs`, `WeatherForecast.cs` are template boilerplate to be replaced) |
| Frontend | `F:\Product 2026\Enterprize craft pos Production Ready\enterprise-craft-pos` | Angular 19+ (currently a fresh, unmodified `ng new` scaffold) |
| Reference prototype (read-only, do not edit) | `F:\Product 2026\Enterprize craft pos\enterprise-craft-pos` | The original Lovable app — React 19 + TanStack Start + Supabase. Use it only to check exact copy/layout/behavior when replicating a screen. It is a **separate git repo**, not part of this system. |

These are two independent repos (polyrepo), not an Nx/Turborepo monorepo. CORS must be configured on the API for the Angular dev origin.

---

## 3. What was real vs. fake in the reference prototype (do not carry forward the fake parts)

| Area | Prototype reality | Required in this rebuild |
|---|---|---|
| Products list + Add Product | ✅ Real Supabase CRUD (create + list only) | Full CRUD, real DB, real validation, real image storage |
| Customers list + Add Customer | ✅ Real Supabase CRUD (create + list only) | Full CRUD |
| Purchase Order creation dialog | ✅ Fully built and functional **but never wired into any page** — dead/orphaned component | Must be a real, reachable screen |
| POS terminal | ❌ 100% client-side mock — cart math is real, but "Pay" invents an invoice number with `Math.random()` and never writes to the DB | Must call a real create-sale endpoint that validates stock, deducts inventory, records payment, awards loyalty points |
| Dashboard, Inventory, Suppliers, Sales & Orders, Reports, Multi-Store, Settings | ❌ 100% hardcoded arrays / literals in the route files | Must be real, queried, aggregated data |
| Login | ❌ 100% fake — `setTimeout` then navigate, no real session, no guard on any route | Real JWT login/refresh/logout/forgot-password + route guards on every page |
| Database security | ❌ RLS policy `USING (true) WITH CHECK (true)` for `anon` + `authenticated` — effectively public read/write | Real role/store-scoped authorization |
| Warehouse/store distinction | ❌ Only `warehouses` existed in the DB; "Multi-Store" screen was 100% mock with no backing table at all | Needs both `stores` (sell) and `warehouses` (hold stock) as first-class, properly related entities — confirm with client whether they're 1:1 or store can have multiple warehouses |

Full findings from the original reverse-engineering pass (page-by-page button/dialog audit, exact dead code, exact SQL schema) are preserved in this conversation's history — ask if you need the granular detail restated; the summary above and the schema in §6 are what matters for building.

---

## 4. Tech stack (decided)

- **.NET 8** (LTS), ASP.NET Core Web API, C#.
- **Clean Architecture**: `Domain` / `Application` / `Infrastructure` / `Api` projects.
- **EF Core 8** targeting **PostgreSQL** (`Npgsql.EntityFrameworkCore.PostgreSQL`) — chosen for continuity with the prototype's existing Postgres schema (§6), which is a solid starting skeleton.
- **Auth:** ASP.NET Core Identity or a custom user store (decide during Phase 3) issuing **JWT access tokens + rotating refresh tokens**. Role-based + store-scoped authorization policies.
- **Validation:** FluentValidation in the Application layer.
- **API docs:** Swagger/Swashbuckle, kept in sync with every endpoint.
- **Real-time:** SignalR hub(s) for POS-to-POS / terminal-to-dashboard live stock and sales updates — **CONFIRM WITH CLIENT** how critical true real-time sync is for v1 vs. polling/refetch-on-focus being acceptable initially.
- **Background jobs:** built-in `IHostedService`/`BackgroundService` (or Hangfire if the job surface grows) for things like auto-reorder draft PO generation — not present in the prototype at all, net-new per the original spec's ambitions.
- **Testing:** xUnit for unit tests (Application handlers) + integration tests (EF Core + Testcontainers-Postgres) for the transactional flows in §1.4 — the prototype shipped with **zero tests**; this rebuild should not repeat that.

---

## 5. The real Item Master hierarchy (client data — replaces the prototype's mock catalog)

The client supplied `DESIRE ITEM MASTER HIERARCHY.xlsx` — a real sample of their product catalog. This is **the actual domain model for `Product`/`Item`**, not the prototype's flat `category: "grocery"` string. It must be modeled as a proper hierarchy + attribute set, not copied as free text.

### 5.1 Observed structure (62 sample rows, 14 columns)

| Field | Role | Observed values in the sample |
|---|---|---|
| `ItemCode` | Structured SKU/item code, encodes department+other facets (see §5.3) | e.g. `F24625001`, `B24124002`, `J23124001`, `K24624002` |
| `Department` | Top-level line of business | `FOOTWEAR`, `BAGS`, `JEWELLERY` (sample has no `APPAREL`/others — **CONFIRM full department list with client**) |
| `Gender` | Target segment | `WOMEN`, `KIDS` in sample — **CONFIRM whether `MEN` and/or `UNISEX` exist as segments; they did not appear in this sample** |
| `Event` | Occasion/usage tier | `BASIC`, `CASUAL`, `FORMAL`, `COTURE` *(client's spelling — keep as "COTURE" unless they confirm "COUTURE" is intended)* |
| `Category` | Product type within department | Footwear: `KHUSSA`, `SANDAL`, `SLIPPER`; Bags: `HAND CARRY`, `SHOULDER`, `CROSS BODY`; Jewellery: `EARRINGS`, `NECKLACE`, `RING`, `BANGLES`, `HEAD PEACE` |
| `Subcategory` | Refinement within category | e.g. KHUSSA → `REGULAR`, `PESHAWARI KHUSSA`; SANDAL → `SHELL TOE`, `NARROW BAND`; SLIPPER → `BROAD BAND`, `D3-BAND`; HAND CARRY → `CLUTCHES`, `POTLI`; SHOULDER → `TOTE BAG`, `HOBO`; RING → `SLEEK FINGER RING`, `THICK FINGER RING`; NECKLACE → `PENDENT`. Value `Default` used when no meaningful subcategory applies (e.g., most jewellery/earrings rows) |
| `Uppar Material` *(sic — "Upper Material")* | Primary material of the visible/upper part | `METAL`, `SOFA VELVET`, `MIRCO VELVET` *(sic — "Micro Velvet")*, `RAW SILK`, `JUTE`/`Jute` (inconsistent casing in source data — normalize on ingest), `RAXINE`, `NET`, `CANE` |
| `Uppar Type` | Finish/treatment of the upper | `Default`, `EMBELLISHED`, `PRINTED`, `EMBELLISHED+PRINTED`, `EMBELLISHED+NET`, `METAL ORNAMENT`, `Embroidered Motif`, `Criss-Cross`, `PLAIN`, `Thread Embroidery` |
| `Heel Hight` *(sic — "Heel Height")* | Footwear-only attribute | `Default`, `FLAT (0-5 mm)`, `SMALL (6-51 mm)`, `MEDIUM (52-76 mm)` — banded ranges, not free numeric entry |
| `Heel Type` | Footwear-only attribute | `Default`, `FLAT`, `KITTEN`, `BLOCK` |
| `Sole Material` | Footwear-only attribute | `Default`, `LEATHER`, `SHEET` |
| `Sole Type` | Footwear-only attribute | `Default`, `KHUSSA SOLE`, `PEEP TOE`, `NARROW TOE`, `SEMI-BROAD TOE` |
| `Collection` | Named drop/collection, encodes a version and a 2-digit year suffix | e.g. `Zebtan-V2-24`, `Masal-V2-25`, `Dazzle Jewellery-V1-24`, `Old collection-V1-19` — free text but consistently pattern `<Name>-V<n>-<YY>` |
| `Year` | 4-digit year, generally matches the `Collection` suffix (mismatch seen once: `Dazzle Saughat-V2-26` tagged `Year=2024` — **treat `Year` as the authoritative field and `Collection` as descriptive**, flag mismatches during data cleanup) | `2019`, `2023`, `2024`, `2025` |

### 5.2 Data modeling implications for `Product`/`Item`

- Model **Department, Gender, Event, Category, Subcategory** as a real hierarchical reference structure (either a self-referencing `ProductAttribute`/`Taxonomy` table with a `Type` discriminator, or dedicated lookup tables per level) — **not** the prototype's single flat `category: text` column with a hardcoded 9-item frontend array.
- Model **Uppar Material, Uppar Type, Heel Height, Heel Type, Sole Material, Sole Type** as a configurable, **department-aware attribute set** — Heel/Sole attributes only apply to `FOOTWEAR`, Uppar Material/Type apply across all three observed departments. Design this so a future new department (e.g. `APPAREL`) can define its own attribute set without a schema migration — an EAV-style `ProductAttributeDefinition` + `ProductAttributeValue` pair, or a `jsonb` attributes column with server-side schema validation per department, are both reasonable; pick one and be consistent. **CONFIRM WITH CLIENT** whether more departments/attributes exist beyond this 62-row sample before finalizing the schema.
- `Collection` is effectively a **named merchandising batch/season** — model it as its own entity (`Collections`: name, version, year, department) rather than a free-text field, so Reports can group by collection.
- Normalize inconsistent casing/spelling found in the raw sample (`Jute` vs `JUTE`, "Uppar" vs "Upper", "Hight" vs "Height") at the **ingestion/mapping layer**, not by preserving the typos into the production schema — the production column/enum names should use correct English (`UpperMaterial`, `HeelHeight`), while the *display labels* can still say whatever the client is used to seeing if requested.
- Keep the prototype's already-correct generic product fields (SKU, barcode, cost, price, wholesale price, tax, discount, unit, min/max/reorder stock, supplier, warehouse, location, status, image) — those are sound and apply on top of this richer taxonomy.

### 5.3 ItemCode structure — partially decoded, do not hard-code a guess

Pattern observed across the 62-row sample: `<DeptLetter><EventDigit><...more digits...><Sequence>`, e.g.:
- First letter: `J`=Jewellery, `B`=Bags, `F`=Footwear(Women in sample), `K`=Footwear(Kids in sample — same trailing digits as the `F` equivalent for the same product, e.g. `F24624002` / `K24624002`), so the letter appears to encode **Department**, and possibly **Department+Gender** for Footwear specifically since Kids only appears there in this sample.
- Second character consistently correlates with `Event` in the sample: `1`=BASIC, `2`=CASUAL, `3`=FORMAL, `4`=COTURE.
- The remaining digits (category/material/sequence encoding) were **not fully decodable with confidence from 62 rows** and should not be guessed into production logic.

**Action required, do not code around this:** ask the client for their actual SKU/item-code generation specification (or enough sample data to fully reverse it) before building an auto-code-generator. Until confirmed, treat `ItemCode` as client-supplied/importable data, and give the system its own internal surrogate key (UUID) as primary key regardless — mirroring the prototype's pattern of UUID PKs with a separate human-readable business code column.

---

## 6. Data model — starting schema (from the prototype's real Postgres migration, to be extended per §5)

The prototype's single migration (`supabase/migrations/20260808050944_*.sql`) is a reasonable **skeleton** to evolve, not a finished production schema. Reuse its good parts, fix its gaps:

**Tables to carry forward (with fixes):**
`warehouses`, `suppliers`, `products` *(rebuild per §5 taxonomy)*, `inventory`, `stock_movements`, `customers`, `purchase_orders` + `purchase_order_lines`, `transfers` + `transfer_lines`, `orders` + `order_lines`, `payments`.

**Known gaps to fix (do not reproduce as-is):**
- `warehouses.code` was a bare `text` primary key referenced only loosely (as plain text, not a real FK) from `products.warehouse` / `inventory.warehouse`. Use real FK constraints.
- **No `stores` table existed at all** — despite a whole "Multi-Store" screen in the UI. Add `stores` as a first-class entity; **CONFIRM WITH CLIENT** the relationship between `stores` (sell to customers) and `warehouses` (hold stock) — same thing, 1:1, or 1-store-to-many-warehouses.
- **No `categories` table** — category was a free-text column with a hardcoded frontend list. Replaced entirely by the real taxonomy in §5.
- **No users/roles/permissions tables at all.** Add `Users`, `Roles`, `StoreAssignments` (or equivalent) — needed for real auth (§1.2) and for the role set below.
- **No multi-tenant boundary** (`tenant_id`/`company_id`) anywhere. **CONFIRM WITH CLIENT**: is this single-tenant (one deployment per retailer) or must it support multiple company/head-office tenants in one deployment? This materially changes the schema — resolve before finalizing migrations.
- Business logic that lived in Postgres `PL/pgSQL` functions (`create_sales_order`, `receive_purchase_order`, `complete_transfer`, `next_doc_number`, and a `sync_product_stock` trigger keeping `products.stock` in sync with `inventory`) must be reimplemented as **C# Application-layer command handlers running inside EF Core transactions** — do not try to port raw PL/pgSQL into this codebase; the SQL logic is documented in §1.4 well enough to reimplement correctly in C#.

**Roles implied by the original spec** (not enforced anywhere in the prototype, must be enforced for real here): **Super Admin, Head Office, Store Manager, Cashier, Inventory Manager, Purchase Manager**. **CONFIRM WITH CLIENT** the final role list and per-role permissions before finalizing the authorization policy design — this list is a reasonable starting point, not a confirmed requirement.

---

## 7. Screens / API surface to build (parity target)

Reproduce these 10 screens (from the reference prototype's routes) plus a real login, each now backed by real endpoints — not the mocked versions:

`Dashboard`, `Login`, `POS Terminal`, `Products`, `Inventory`, `Customers`, `Suppliers & Purchasing`, `Sales & Orders`, `Reports & Analytics`, `Multi-Store Management`, `Settings`.

Minimum endpoint set (expand as each module is built):

```
Auth:              POST /api/auth/login | /refresh | /logout | /forgot-password
Products:          GET/POST/PUT/DELETE /api/products  (+ server-side paging/filter/search)
Categories/Taxonomy: GET/POST/PUT/DELETE /api/taxonomy/{department|gender|event|category|subcategory}
Customers:         GET/POST/PUT/DELETE /api/customers
Suppliers:         GET/POST/PUT/DELETE /api/suppliers
Warehouses/Stores:  GET/POST/PUT/DELETE /api/warehouses, /api/stores
Inventory:         GET /api/inventory  |  POST /api/inventory/adjust
Purchase Orders:   GET/POST /api/purchase-orders | PATCH .../status | POST .../receive
Transfers:         GET/POST /api/transfers | PATCH .../status | POST .../complete
Sales/Orders:      GET /api/orders | POST /api/orders  (the real POS "create sale" endpoint —
                     validate stock, deduct inventory, record payment, award loyalty points, generate invoice number server-side)
Reports:           GET /api/reports/dashboard | /sales | /inventory | ... (aggregation endpoints; the prototype had zero of these — every KPI was hardcoded)
```

---

## 8. Business rules to enforce (carried forward from the working parts of the prototype's SQL)

**Confirmed rules to reimplement faithfully:**
- SKU and barcode unique per product; customer phone unique.
- Cost/price/quantities never negative; transfer source ≠ destination warehouse.
- Creating a PO or Transfer does **not** move stock; only **receiving** a PO or **completing** a Transfer does.
- A sale must validate total available stock (summed across warehouses, or store-scoped — **confirm** scoping rule) before committing, and fail the whole transaction if insufficient.
- Every inventory change writes an immutable `stock_movements` row (kind: `opening_stock`/`sale`/`transfer_in`/`transfer_out`/`purchase_receipt`/`adjustment`).
- `products.stock` (if kept as a denormalized read-optimization) must always be derived from `inventory`, never written directly.
- Loyalty points accrue on completed sales tied to a customer (prototype used `floor(total)` — **confirm** the real point formula with the client rather than assuming this placeholder is final).

**Inferred, not enforced in the prototype — must be designed for real here:**
- Low/out-of-stock thresholds using each product's own `reorder`/`min_stock` (the prototype's POS hardcoded a flat 5% tax ignoring the per-product tax field — **do not repeat that inconsistency**; always use the product's own tax rate).
- Refund/return manager-approval-above-threshold workflow (prototype only had decorative Settings copy for this).

**Missing entirely — net new:** RBAC enforcement, multi-tenant/store data scoping, stock reservation at cart-add time (prevent overselling across concurrent terminals), audit trail beyond stock movements (login history, record-change history), auto-reorder job.

---

## 9. UI/UX parity (Angular side, for reference — see the frontend repo's copy of this file for the Angular-specific structure)

Match the reference prototype's design system: OKLCH color tokens (primary blue `oklch(0.52 0.19 258)`, success/accent green, warning orange, destructive red), a persistent dark-navy sidebar even in light mode, `shadcn "new-york"`-equivalent component density, card-based surfaces with subtle elevation on hover, and the POS terminal's dedicated full-height 3-column layout (distinct from the rest of the app's sidebar+header shell). Full token values and per-screen responsive behavior are documented in the frontend repo's CLAUDE.md (§"UI/UX Replication" there) — keep both files in sync if either is updated.

---

## 10. Open questions to resolve with the client before/while building (do not guess)

1. Multi-tenant SaaS (many companies, one deployment) vs. single-tenant (one deployment per retailer)?
2. Full `Department` / `Gender` list beyond `{FOOTWEAR, BAGS, JEWELLERY}` / `{WOMEN, KIDS}` seen in the 62-row sample — is `MEN`/`UNISEX`/`APPAREL` etc. coming?
3. The real `ItemCode` generation algorithm (§5.3) — needed before building auto-numbering.
4. Final role list and per-role permissions (§6).
5. Store vs. warehouse relationship (§6).
6. Loyalty point formula (§8).
7. How real-time does POS/inventory sync need to be for v1 (SignalR push vs. periodic refetch)?
8. `Collection`/`Year` mismatch handling and whether historical prototype-style mock data should seed dev/staging at all (recommendation: no — seed with a cleaned subset of the real item-master data instead).

---

## 11. Roadmap

1. **Setup** — solution structure (Domain/Application/Infrastructure/Api), Angular workspace, Postgres instance, Swagger, CI skeleton.
2. **Database** — full schema per §6/§5 (taxonomy tables, stores, users/roles), migrations, seed script using cleaned real item-master data (not prototype mock data).
3. **Auth** — real JWT + refresh, roles, Angular guards + interceptors.
4. **Core modules** — Products (with full taxonomy), Customers, Suppliers, Warehouses/Stores — full CRUD.
5. **Business modules** — Purchase Orders (create→approve→receive), Transfers (create→complete), Inventory adjustments/cycle counts.
6. **POS** — real sale-creation endpoint wired to the terminal UI, real receipts, real held-sale persistence, real customer lookup, per-product tax.
7. **Reports** — real aggregation endpoints backing Dashboard/Reports.
8. **Testing** — unit + integration tests, especially for the transactional stock/sale/transfer logic.
9. **Deployment** — IIS or Linux/Nginx, environment-based config.
