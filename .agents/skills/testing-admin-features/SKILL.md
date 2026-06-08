---
name: testing-admin-features
description: Run and test the Long Châu pharmacy admin app (ASP.NET Core 8 + SQL Server) end-to-end. Use when verifying admin UI features such as inventory, purchase orders, product pickers, or SweetAlert2 popups.
---

# Testing Long Châu admin features

ASP.NET Core 8 MVC + EF Core + SQL Server e-commerce/pharmacy admin. Customer site and `/admin` dashboard share Bootstrap 5 + SweetAlert2 v11.

## Run the app locally

1. Start SQL Server (Docker): `docker compose up -d` (container `sqlserver`, host port `14330`, creds `sa` / `MyStrongPassword123!`). DB: `LongChauDB_New`.
2. .NET SDK lives at `~/.dotnet`. Build/run from `web-ban-thuoc/`: `dotnet run` → app on `http://localhost:5226`.
3. Connection string (appsettings): `Server=localhost,14330;Database=LongChauDB_New;User Id=sa;Password=MyStrongPassword123!;TrustServerCertificate=true;MultipleActiveResultSets=true`.

## Admin login

- The login form GET route is `/` or `/Auth/Index` (NOT `/Auth/Login` — that returns 405, POST-only). Easiest: open `http://localhost:5226/` while logged out → login form is on the page.
- Admin account: `admin@gmail.com` / `Admin123.` → redirects to `/admin`.

## Seeding test data (often required)

The seed DB on a fresh VM may have only 1 product, no images, no supplier link. To test product pickers meaningfully, seed via `sqlcmd`:

- Run sqlcmd inside the container: `docker exec -i sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'MyStrongPassword123!' -C -d LongChauDB_New ...`
- IMPORTANT: prepend `SET QUOTED_IDENTIFIER ON;` + `GO` before INSERT/UPDATE, otherwise tables with computed columns/indexed views fail with Msg 1934.
- Key tables: `Products` (required cols: ProductName, Price, IsFeature, StockQuantity, IsActive, RequiresPrescription; SupplierId/Sku/CategoryId nullable), `ProductImages` (ImageUrl is just the filename e.g. `canxi_d3k2_1.png`, set IsMain=1), `WarehouseStocks` (WarehouseId, ProductId, QuantityOnHand, QuantityReserved, UpdatedAt). Available stock = max(0, OnHand - Reserved).
- Real product image files matching demo data exist in `web-ban-thuoc/wwwroot/images/products/` (e.g. `canxi_d3k2_*.png`, `immuvita_easylife_*.png`, `active_liver_plus_*.png`, `default.png`). Reference these filenames in ProductImages so thumbnails render.
- For a product to appear in the AdminPurchase/Create picker it MUST have `SupplierId` set; the picker is filtered by the selected supplier.

## Feature: product-picker popups (PR #22)

- **Inventory** `/AdminInventory` tab "Nhập kho nhanh": button "Chọn sản phẩm..." opens a Bootstrap modal grid of cards (image + name + SKU + "Tồn khả dụng" badge, red ≤10 / green >10), real-time search by name/SKU, click sets hidden ProductId + updates the button. Submitting without a product shows SweetAlert2 "Vui lòng chọn sản phẩm trước khi nhập kho." Successful import shows "Nhập kho thành công!" and updates the stat cards.
- **Purchase order** `/AdminPurchase/Create`: per-line picker is DISABLED until a supplier is chosen ("Chọn Nhà cung cấp..."). After choosing supplier it shows "N sản phẩm thuộc NCC đã chọn." Picker lists supplier products with image/stock; selecting a product auto-fills the "Giá nhập" (UnitCost) from CostPrice. Submit creates a draft PO; verify on `/AdminPurchase/Details/{id}`.

## General tips

- SweetAlert2 helpers (`showConfirm`, `showAlert`, `confirmSubmit`) are in BOTH `Views/Shared/_Layout.cshtml` (customer) and `Views/Admin/_Layout.cshtml` (admin). When adding popups to admin pages, confirm the admin layout includes SweetAlert2 — it was historically missing and is a common bug source.
- When typing URLs into the Chrome address bar via computer-use, the first character is sometimes dropped — verify the URL and retype with `ctrl+a` first if it 404s.
- No CI is configured on this repo.

## Devin Secrets Needed

None for local testing — DB password is a local-dev docker credential (`MyStrongPassword123!`) and the admin account is a seeded local account.
