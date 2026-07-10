# 📋 PROJECT CONTEXT — NHÀ THUỐC LONG CHÂU

> **Mục đích file này**: Đưa cho AI assistant ở session mới để nhanh chóng nắm context dự án mà không cần rà soát lại toàn bộ.
> **Cập nhật lần cuối**: 2026-06-17

---

## 1. TỔNG QUAN DỰ ÁN

| Thuộc tính | Giá trị |
|---|---|
| **Tên** | Nhà Thuốc Long Châu — Hệ thống bán thuốc trực tuyến |
| **Framework** | ASP.NET Core 8.0 MVC (`net8.0`) |
| **Database** | SQL Server + Entity Framework Core 9.x (Code-First) |
| **Root namespace** | `web_ban_thuoc` |
| **Solution file** | `doAnWebNC.sln` / `web-ban-thuoc.sln` |
| **Thư mục chính** | `web-ban-thuoc/` |
| **Tác giả** | Nguyễn Trung Hiếu (22DH111077) |
| **Mục đích** | Đồ án Web nâng cao — e-commerce dược phẩm |

---

## 2. CÔNG NGHỆ & PACKAGES

### Backend
- **ASP.NET Core 8.0** (MVC pattern)
- **EF Core 9.x** (`Microsoft.EntityFrameworkCore.SqlServer 9.0.7`, `Tools 9.0.6`, `Design 9.0.6`)
- **ASP.NET Core Identity 8.0** (`Identity.EntityFrameworkCore`, `Identity.UI`)
- **SignalR** (`Microsoft.AspNetCore.SignalR 1.2.0`) — Real-time chat
- **Newtonsoft.Json 13.0.3** — JSON serialization (PayOS)
- **ClosedXML 0.104.2** — Export Excel
- **System.Security.Cryptography.Algorithms 4.3.1** — Checksum PayOS

### Frontend
- **Razor Views** (`.cshtml`)
- **Bootstrap 5** + **jQuery** + **Font Awesome**
- CSS tùy chỉnh: `site.css`, `admin.css`, `payos.css`, `reset.css`
- JS: `site.js`
- Static assets: `wwwroot/` (css, js, images, lib)

### Thanh toán
- **PayOS** (api-merchant.payos.vn) — Thanh toán trực tuyến QR/banking

### Email
- **SMTP (Gmail)** — Gửi mail xác nhận, quên mật khẩu, thông báo đơn hàng
- Strategy pattern: Dev → `NullEmailSender`, Prod → `SmtpEmailSender`
- Decorator: `LoggingEmailSender` wrap sender thực

### Docker
- `docker-compose.yml`: SQL Server 2022 (port 14330) + Web app (port 5000)
- DB init script: `docker/sql/LongChauDB.sql`

---

## 3. CẤU TRÚC THƯ MỤC CHI TIẾT

```
doAnWebNC/                          (root)
├── web-ban-thuoc/                  (main project)
│   ├── Program.cs                  ★ Entry point, DI, middleware, seed data
│   ├── GlobalUsings.cs             Global using directives
│   ├── ChatHub.cs                  SignalR hub cho chat real-time
│   ├── web-ban-thuoc.csproj        Project file (net8.0)
│   ├── appsettings.json            Config chung (ConnectionString, Email, PayOS)
│   ├── appsettings.Development.json Config dev (Gemini API key, PayOS)
│   ├── Dockerfile                  Docker build
│   ├── LongChauDB_New.sql          SQL script khởi tạo DB
│   │
│   ├── Controllers/
│   │   ├── HomeController.cs           Trang chủ
│   │   ├── AuthController.cs           Đăng nhập/ký/quên MK/profile (25KB)
│   │   ├── ProductController.cs        Danh sách & chi tiết SP
│   │   ├── CategoriesController.cs     Duyệt danh mục
│   │   ├── CartController.cs           Giỏ hàng
│   │   ├── PayOSController.cs          Thanh toán PayOS (27KB - lớn nhất)
│   │   ├── ChatApiController.cs        API chat
│   │   ├── AiBotController.cs          AI chatbot (Gemini)
│   │   ├── LoyaltyController.cs        Điểm thưởng khách hàng
│   │   └── Admin/
│   │       ├── AdminHomeController.cs      Dashboard (15KB)
│   │       ├── AdminProductController.cs   CRUD sản phẩm (12KB)
│   │       ├── AdminCategoryController.cs  CRUD danh mục (10KB)
│   │       ├── AdminOrderController.cs     Quản lý đơn hàng
│   │       ├── AdminBannerController.cs    CRUD banner (14KB)
│   │       ├── AdminUserController.cs      Quản lý user (10KB)
│   │       ├── AdminChatController.cs      Chat với khách
│   │       ├── AdminInventoryController.cs Kho hàng
│   │       ├── AdminPurchaseController.cs  Đơn mua hàng NCC (14KB)
│   │       ├── AdminReportController.cs    Báo cáo thống kê (34KB - lớn nhất)
│   │       ├── AdminLoyaltyController.cs   Quản lý loyalty
│   │       ├── AdminStaffController.cs     Quản lý nhân viên (10KB)
│   │       └── AdminSupplierController.cs  CRUD nhà cung cấp
│   │
│   ├── Models/                     (46 files)
│   │   ├── LongChauDbContext.cs    ★ DbContext (307 lines, 29 DbSets)
│   │   ├── Product.cs              Sản phẩm
│   │   ├── Category.cs             Danh mục (tự tham chiếu 3 cấp)
│   │   ├── Order.cs                Đơn hàng
│   │   ├── OrderItem.cs            Chi tiết đơn hàng
│   │   ├── OrderStatuses.cs        ★ Enum trạng thái đơn (chuỗi tiếng Việt)
│   │   ├── OrderStatusHistory.cs   Lịch sử trạng thái
│   │   ├── Cart.cs                 Giỏ hàng + CartItem
│   │   ├── Payment.cs              Thanh toán
│   │   ├── PaymentStatuses.cs      Trạng thái thanh toán
│   │   ├── Review.cs               Đánh giá SP
│   │   ├── Voucher.cs              Voucher + UserVoucher
│   │   ├── VoucherRedemption.cs    Lịch sử dùng voucher
│   │   ├── Banner.cs               Banner quảng cáo
│   │   ├── ChatMessage.cs          Tin nhắn chat
│   │   ├── UserRankInfo.cs         Thông tin hạng thành viên
│   │   ├── LoyaltyTiers.cs         ★ Định nghĩa hạng (Bạc/Vàng/Bạch kim)
│   │   ├── LoyaltyReward.cs        Quà đổi điểm
│   │   ├── LoyaltyPointTransaction.cs Giao dịch điểm
│   │   ├── Warehouse.cs            Kho hàng
│   │   ├── WarehouseStock.cs       Tồn kho theo kho
│   │   ├── InventoryTransaction.cs Phiếu nhập/xuất kho
│   │   ├── ProductBatch.cs         Lô hàng
│   │   ├── ProductImage.cs         Ảnh sản phẩm
│   │   ├── Supplier.cs             Nhà cung cấp
│   │   ├── PurchaseOrder.cs        Đơn mua hàng + PurchaseOrderLine
│   │   ├── GoodsReceipt.cs         Phiếu nhập kho + GoodsReceiptLine
│   │   ├── Shipment.cs             Vận chuyển + ShippingCarriers
│   │   ├── PayOSModels.cs          Models PayOS (Request/Response/Webhook)
│   │   ├── PayOSWebhookEvent.cs    Lưu webhook event (idempotency)
│   │   ├── StaffRoles.cs           ★ Roles: Admin, WarehouseStaff, CustomerSupport
│   │   ├── ProfileViewModel.cs     VM profile
│   │   ├── LoginViewModel.cs       VM đăng nhập
│   │   ├── RegisterViewModel.cs    VM đăng ký
│   │   ├── HomeViewModel.cs        VM trang chủ
│   │   ├── CategoryMenuViewModel.cs VM menu danh mục
│   │   ├── CartLineViewModel.cs    VM dòng giỏ hàng
│   │   ├── CheckoutPopupViewModel.cs VM popup checkout
│   │   ├── UserAdminViewModel.cs   VM user cho admin
│   │   ├── UserDetailViewModel.cs  VM chi tiết user
│   │   ├── VoucherAdminViewModel.cs VM voucher admin
│   │   ├── VoucherCreateModel.cs   VM tạo voucher
│   │   ├── StaffViewModels.cs      VM nhân viên
│   │   ├── InventoryViewModels.cs  VM kho hàng
│   │   ├── ProductImportModels.cs  VM import SP Excel
│   │   └── ErrorViewModel.cs       VM lỗi
│   │
│   ├── Services/                   (19 files)
│   │   ├── CartService.cs              ICartService — Giỏ hàng
│   │   ├── OrderService.cs             IOrderService — Đơn hàng
│   │   ├── InventoryService.cs         IInventoryService — Kho hàng (27KB)
│   │   ├── PayOSService.cs             IPayOSService — Thanh toán PayOS
│   │   ├── PayOSWebhookProcessor.cs    IPayOSWebhookProcessor
│   │   ├── UserRankService.cs          UserRankService — Xếp hạng (16KB)
│   │   ├── LoyaltyService.cs           ILoyaltyService — Điểm thưởng
│   │   ├── VoucherHelper.cs            VoucherHelper — Logic voucher (hosted service)
│   │   ├── RecommendationService.cs    IRecommendationService — Gợi ý SP
│   │   ├── OrderEmailService.cs        IOrderEmailService — Email đơn hàng (15KB)
│   │   ├── OrderNotificationService.cs IOrderNotificationService
│   │   ├── ProductExcelImportService.cs IProductExcelImportService (15KB)
│   │   ├── IEmailSender.cs             Interface gửi mail
│   │   ├── SmtpEmailSender.cs          SMTP implementation
│   │   ├── NullEmailSender.cs          Null implementation (dev)
│   │   ├── LoggingEmailSender.cs       Decorator logging
│   │   ├── EmailSenderFactory.cs       Factory
│   │   ├── EmailSettings.cs            Config class
│   │   └── CustomIdentityErrorDescriber.cs  Lỗi Identity tiếng Việt
│   │
│   ├── Filters/
│   │   └── NavbarFilter.cs         IActionFilter — Load categories cho navbar
│   │
│   ├── ViewComponents/
│   │   ├── NavbarViewComponent.cs          Navbar component
│   │   ├── AdminSidebarViewComponent.cs    Admin sidebar (5KB)
│   │   └── AdminNotificationViewComponent.cs  Thông báo admin
│   │
│   ├── Views/
│   │   ├── _ViewImports.cshtml     Tag helpers
│   │   ├── _ViewStart.cshtml       Layout mặc định
│   │   ├── _ProductList.cshtml     Partial danh sách SP
│   │   ├── Home/Index.cshtml       ★ Trang chủ (13KB)
│   │   ├── Auth/
│   │   │   ├── Index.cshtml        Đăng nhập/Đăng ký (10KB)
│   │   │   ├── Profile.cshtml      Hồ sơ cá nhân (41KB - lớn nhất)
│   │   │   ├── ForgotPassword.cshtml
│   │   │   ├── VerifyResetCode.cshtml
│   │   │   └── AccessDenied.cshtml
│   │   ├── Product/Details.cshtml  Chi tiết SP (39KB)
│   │   ├── Categories/
│   │   │   ├── Index.cshtml        Danh sách theo danh mục (12KB)
│   │   │   └── _ProductList.cshtml
│   │   ├── Cart/
│   │   │   ├── Index.cshtml        Giỏ hàng (23KB)
│   │   │   └── ThankYou.cshtml
│   │   ├── PayOS/
│   │   │   ├── CreatePayment.cshtml Trang thanh toán (10KB)
│   │   │   ├── Success.cshtml
│   │   │   ├── Failed.cshtml
│   │   │   └── Cancel.cshtml
│   │   ├── Shared/
│   │   │   ├── _Layout.cshtml      ★ Layout chính (11KB)
│   │   │   ├── _Header.cshtml      Header (14KB)
│   │   │   ├── _Footer.cshtml      Footer (4KB)
│   │   │   ├── _FilterSidebar.cshtml Sidebar lọc SP (15KB)
│   │   │   ├── _ChatPopup.cshtml   Popup chat (29KB)
│   │   │   ├── _AiChatPopup.cshtml Popup AI chat (18KB)
│   │   │   ├── _ToastPartial.cshtml Toast notifications
│   │   │   ├── _AdminPagination.cshtml Phân trang admin
│   │   │   ├── Error.cshtml
│   │   │   ├── _Layout.cshtml.css
│   │   │   ├── _ValidationScriptsPartial.cshtml
│   │   │   └── Components/
│   │   │       ├── Navbar/         (view component)
│   │   │       ├── AdminSidebar/   (view component)
│   │   │       └── AdminNotification/ (view component)
│   │   └── Admin/
│   │       ├── _Layout.cshtml      Layout admin (7KB)
│   │       ├── Index.cshtml        Dashboard (4KB)
│   │       ├── Product/            Create, Edit, Delete, Import, ImportResult, Index, _ProductDetailPartial
│   │       ├── Category/           Create, Edit, Delete, Index
│   │       ├── Order/Index.cshtml  (16KB)
│   │       ├── Banner/             Create, Edit, Delete, Details, Index
│   │       ├── Voucher/            Index (19KB), Redemptions
│   │       ├── User/               Index (10KB), Details (14KB)
│   │       ├── Chat/Index.cshtml   (41KB — lớn nhất)
│   │       ├── Report/Index.cshtml (36KB)
│   │       ├── Inventory/Index.cshtml (12KB)
│   │       ├── Purchase/           Create, Details, Index, Receive, Replenishment
│   │       ├── Staff/              Create, Edit, Index
│   │       ├── Supplier/           Create, Edit, Index
│   │       └── Loyalty/            Index, Rewards
│   │
│   ├── Migrations/                 (55 files — ~28 migration pairs)
│   └── wwwroot/
│       ├── css/ (admin.css, payos.css, reset.css, site.css)
│       ├── js/ (site.js)
│       ├── images/
│       └── lib/
│
├── docker-compose.yml
├── docker/sql/                     SQL init scripts
├── docs/screenshots/               Ảnh chụp màn hình
├── tools/
├── bao_cao_extract/
├── run-dev.ps1                     Script chạy dev
└── stop-dev.ps1                    Script dừng dev
```

---

## 4. DATABASE SCHEMA (29 DbSets)

### DbContext: `LongChauDbContext` (kế thừa `IdentityDbContext`)

```
DbSet<Category>                Categories
DbSet<Product>                 Products
DbSet<ProductImage>            ProductImages
DbSet<Order>                   Orders
DbSet<OrderItem>               OrderItems
DbSet<OrderStatusHistory>      OrderStatusHistories
DbSet<Cart>                    Carts
DbSet<CartItem>                CartItems
DbSet<Review>                  Reviews
DbSet<Payment>                 Payments
DbSet<Voucher>                 Vouchers
DbSet<UserVoucher>             UserVouchers
DbSet<VoucherRedemption>       VoucherRedemptions
DbSet<Banner>                  Banners
DbSet<ChatMessage>             ChatMessages
DbSet<UserRankInfo>            UserRankInfos
DbSet<LoyaltyPointTransaction> LoyaltyPointTransactions
DbSet<LoyaltyReward>           LoyaltyRewards
DbSet<Warehouse>               Warehouses
DbSet<WarehouseStock>          WarehouseStocks
DbSet<InventoryTransaction>    InventoryTransactions
DbSet<ProductBatch>            ProductBatches
DbSet<Supplier>                Suppliers
DbSet<PurchaseOrder>           PurchaseOrders
DbSet<PurchaseOrderLine>       PurchaseOrderLines
DbSet<GoodsReceipt>            GoodsReceipts
DbSet<GoodsReceiptLine>        GoodsReceiptLines
DbSet<Shipment>                Shipments
DbSet<PayOSWebhookEvent>       PayOSWebhookEvents
+ Các bảng Identity mặc định (AspNetUsers, AspNetRoles, ...)
```

### Sơ đồ Entity quan trọng

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Category   │◄────│   Product    │────►│  Supplier    │
│ (3 cấp self) │     │              │     │              │
└──────────────┘     └──────┬───────┘     └──────────────┘
                            │
          ┌─────────────────┼─────────────────┐
          │                 │                 │
   ┌──────┴──────┐  ┌──────┴──────┐  ┌──────┴──────┐
   │ ProductImage│  │   Review    │  │  OrderItem  │
   └─────────────┘  └─────────────┘  └──────┬──────┘
                                            │
                                     ┌──────┴──────┐
                                     │    Order     │
                                     │              │
                                     └──────┬───────┘
                                            │
               ┌────────────┬───────┬───────┼───────┐
               │            │       │       │       │
        ┌──────┴──┐  ┌──────┴──┐ ┌──┴───┐ ┌─┴────┐ ┌┴───────────┐
        │ Payment │  │Shipment │ │Status│ │Vouch │ │IdentityUser│
        └─────────┘  └─────────┘ │History│ │Redemp│ └────────────┘
                                 └───────┘ └──────┘

┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
│  Warehouse   │◄────│ WarehouseStock   │────►│   Product    │
│              │     │ (Unique: WH+Prod)│     │              │
└──────┬───────┘     └──────────────────┘     └──────────────┘
       │
┌──────┴──────────────┐     ┌──────────────────┐
│InventoryTransaction │     │   ProductBatch   │
│(Import/Sale/Return/ │     │(BatchNo, Expiry) │
│ Adjustment)         │     └──────────────────┘
└─────────────────────┘

┌───────────────┐     ┌──────────────────┐     ┌──────────────────┐
│ PurchaseOrder │────►│PurchaseOrderLine │     │  GoodsReceipt    │
│ (Supplier+WH) │     │                  │     │  (Supplier+WH)   │
└───────────────┘     └──────────────────┘     └────────┬─────────┘
                                                        │
                                                ┌───────┴──────────┐
                                                │GoodsReceiptLine  │
                                                │→ creates Batch   │
                                                └──────────────────┘
```

### Chi tiết Entity chính

#### Product
| Field | Type | Note |
|---|---|---|
| ProductId | int (PK) | |
| ProductName | string (required) | |
| Sku | string? | Unique (filtered) |
| Barcode | string? | |
| RegistrationNumber | string? | Số đăng ký BYT |
| RequiresPrescription | bool | Cần kê đơn |
| CostPrice | decimal? | Giá vốn |
| Price | decimal | Giá bán |
| Brand | string? | Thương hiệu |
| Package | string? | Quy cách đóng gói |
| CategoryId | int? (FK) | → Category |
| SupplierId | int? (FK) | → Supplier |
| Ingredients, Uses, Dosage, TargetUsers, Contraindications | string? | Thông tin dược |
| IsFeature | bool | Sản phẩm nổi bật |
| Origin | string? | Xuất xứ |
| StockQuantity | int | Tổng tồn kho (sync từ WarehouseStocks) |
| IsActive | bool | Đang kinh doanh |
| Slug | string? | URL-friendly name |
| SoldQuantity | int? | Đã bán |
| MinStockLevel | int | Ngưỡng cảnh báo hết hàng |
| **Nav** | ProductImages, Reviews, OrderItems, InventoryTransactions, WarehouseStocks, ProductBatches | |

#### Order
| Field | Type | Note |
|---|---|---|
| OrderId | int (PK) | |
| UserId | string? (FK) | → IdentityUser (SetNull) |
| OrderDate | DateTime? | |
| TotalAmount | decimal? | |
| Status | string? | Chuỗi tiếng Việt (xem OrderStatuses) |
| ShippingAddress | string? | |
| PaymentStatus | string? | |
| FullName, Phone | string? | |
| VoucherCode | string? | Mã voucher đã dùng |
| VoucherDiscount | decimal? | Số tiền giảm |
| PrescriptionNote | string? | Ghi chú kê đơn |
| **Nav** | OrderItems, Payments, StatusHistories, User, Shipment | |

#### Category (Tự tham chiếu 3 cấp)
| Field | Type | Note |
|---|---|---|
| CategoryId | int (PK) | |
| CategoryName | string (required) | |
| Description, ImageUrl | string? | |
| ParentCategoryId | int? (FK) | → Category (self) |
| IsFeature | bool | |
| CategoryLevel | string? | "Level 1" / "Level 2" / "Level 3" |
| ProductCount | int | |
| **Nav** | InverseParentCategory, ParentCategory, Products | |

#### Voucher
| Field | Type | Note |
|---|---|---|
| VoucherId | int (PK) | |
| Code | string (required) | |
| Description | string | |
| ExpiryDate | DateTime | |
| DiscountAmount | decimal? | Giảm cố định |
| PercentValue | decimal? | Giảm % |
| DiscountType | string | "FullOrder" (default) |
| IsPublic | bool | true = mọi user dùng được |
| IsActive | bool | |
| CategoryId | int? | Voucher theo danh mục |
| MinOrderAmount | decimal? | Đơn tối thiểu |
| RequiredRank | string? | Hạng tối thiểu |
| MaxUsage | int? | Tổng lượt dùng tối đa |
| UsedCount | int | Đã dùng |
| **Nav** | UserVouchers, Redemptions | |

---

## 5. BUSINESS LOGIC QUAN TRỌNG

### 5.1 Trạng thái đơn hàng (`OrderStatuses`)
```
Chờ thanh toán → Đã xác nhận
Chờ xác nhận → Đã xác nhận → Đang đóng gói → Đang giao → Đã giao
Bất kỳ (chưa terminal) → Đã hủy

Terminal: Đã giao, Đã hủy
Customer có thể hủy khi: Chờ xác nhận, Đã xác nhận
Xuất kho khi: Đã xác nhận (RequiresStockExport)
```

### 5.2 Hệ thống hạng thành viên (`LoyaltyTiers`)
```
Bạc: TotalSpent6Months >= 0
Vàng: TotalSpent6Months >= 5,000,000đ
Bạch kim: TotalSpent6Months >= 10,000,000đ

Tích điểm: 1 điểm / 1.000đ (mọi hạng)
Reset: hàng tháng (MonthlyVoucherHostedService)
```

### 5.3 Staff Roles
```
Admin:            /admin              (toàn quyền)
WarehouseStaff:   /AdminInventory     (quản lý kho)
CustomerSupport:  /admin/chat         (chat hỗ trợ)
```

### 5.4 Inventory Flow
```
PurchaseOrder (NCC) → GoodsReceipt (nhập kho) → ProductBatch (lô hàng)
                                                → InventoryTransaction (Import)
                                                → WarehouseStock (cập nhật tồn)
                                                → Product.StockQuantity (sync)

Order confirmed → InventoryTransaction (Sale) → giảm tồn kho
Order cancelled → InventoryTransaction (Return) → hoàn tồn kho
```

### 5.5 Voucher System
```
Public voucher: Mọi user nhập mã dùng (MaxUsage giới hạn tổng)
Private voucher: Gán cho user cụ thể qua UserVoucher
VoucherRedemption: Ghi nhận lịch sử dùng (unique: VoucherId + OrderId)
```

### 5.6 Chat System (SignalR)
```
Hub: /chathub
Groups: chat_{customerUserId}
Admin gọi JoinConversation(customerUserId) để join group
Messages lưu DB: ChatMessages (SenderId, ReceiverId, Message, IsRead)
```

### 5.7 PayOS Integration
```
Flow: CreatePaymentLink → Redirect checkoutUrl → Webhook/Return → Update order
Checksum: HMAC-SHA256 với ChecksumKey
Webhook: Idempotency qua PayOSWebhookEvent.IdempotencyKey (unique)
```

---

## 6. DEPENDENCY INJECTION (Program.cs)

```csharp
// DbContext
AddDbContext<LongChauDbContext>(SqlServer)

// Identity
AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<LongChauDbContext>()
    .AddErrorDescriber<CustomIdentityErrorDescriber>()  // Lỗi tiếng Việt
    .AddDefaultTokenProviders()
    .AddDefaultUI()

// Email (Strategy + Decorator pattern)
Dev:  IEmailSender → LoggingEmailSender(NullEmailSender)
Prod: IEmailSender → LoggingEmailSender(SmtpEmailSender)

// Services (Scoped)
NavbarFilter, UserRankService, IInventoryService, ICartService,
IOrderService, ILoyaltyService, IOrderNotificationService,
IPayOSWebhookProcessor, IProductExcelImportService,
IRecommendationService, IPayOSService, IOrderEmailService

// Background
AddHostedService<MonthlyVoucherHostedService>()  // (trong VoucherHelper.cs)

// Infra
AddSession(), AddSignalR(), AddHttpClient()
```

---

## 7. ROUTING

```csharp
// Admin route
pattern: "admin/{action=Index}/{id?}"
defaults: controller = "AdminHome"

// Default route
pattern: "{controller=Home}/{action=Index}/{id?}"

// SignalR
MapHub<ChatHub>("/chathub")

// Identity
MapRazorPages()
```

**Cookie Auth:**
- LoginPath: `/Auth/Index`
- AccessDeniedPath: `/Auth/AccessDenied`

---

## 8. SEED DATA (Program.cs)

### Roles
- `Admin`, `WarehouseStaff`, `CustomerSupport`

### Accounts
| Email | Password | Role |
|---|---|---|
| admin@gmail.com | Admin123. | Admin |
| warehouse@longchau.local | Kho123456. | WarehouseStaff |
| support@longchau.local | Support123. | CustomerSupport |

### Default Data
- Supplier mặc định: `NCC-MAC-DINH`
- 3 LoyaltyRewards (Voucher 30K, 5%, 100K)

---

## 9. CẤU HÌNH (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=LongChauDB_New;..."
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "nowrgan@gmail.com",
    "SenderName": "Nhà Thuốc Long Châu"
  },
  "PayOS": {
    "ClientId": "...",
    "ApiKey": "...",
    "ChecksumKey": "...",
    "BaseUrl": "https://api-merchant.payos.vn"
  },
  "AppSettings": {
    "BaseUrl": "https://localhost:5226"
  }
}
```

**Dev-only** (appsettings.Development.json):
- `Gemini.ApiKey` — Cho AI chatbot

---

## 10. QUY ƯỚC & PATTERNS

### Naming
- **Namespace**: `web_ban_thuoc` (root)
- **Models namespace**: `web_ban_thuoc.Models`
- **Services namespace**: `web_ban_thuoc.Services`
- **Admin Controllers**: Prefix `Admin` (e.g., `AdminHomeController`, `AdminProductController`)
- **Trạng thái DB**: Chuỗi tiếng Việt (e.g., "Chờ xác nhận", "Đã giao")

### Architecture Patterns
- **MVC**: Controller → Service → DbContext → Database
- **Scoped DI**: Tất cả services đều Scoped
- **Strategy + Decorator**: Email sending
- **Global Filter**: `NavbarFilter` (load categories mọi request)
- **ViewComponents**: `NavbarViewComponent`, `AdminSidebarViewComponent`, `AdminNotificationViewComponent`
- **Hosted Service**: `MonthlyVoucherHostedService` (background job)

### Frontend Patterns
- **Shared Layout**: `_Layout.cshtml` (user) + `Admin/_Layout.cshtml` (admin)
- **Partial Views**: `_Header`, `_Footer`, `_FilterSidebar`, `_ChatPopup`, `_AiChatPopup`
- **Admin Pagination**: `_AdminPagination.cshtml`
- **Toast**: `_ToastPartial.cshtml`
- **2 layout hệ thống**: User layout (Bootstrap 5) và Admin layout (custom admin.css)

### Database Patterns
- **Soft reference**: Hầu hết FK dùng `OnDelete(SetNull)` hoặc `Restrict`
- **Cascade**: Cart→CartItem, Order→StatusHistory, PurchaseOrder→Lines, GoodsReceipt→Lines
- **Unique indexes**: UserVoucher(UserId+VoucherId), WarehouseStock(WarehouseId+ProductId), VoucherRedemption(VoucherId+OrderId), Supplier.Code, PurchaseOrder.OrderCode, GoodsReceipt.ReceiptCode, Product.Sku (filtered)
- **Code-first migrations**: 28 migrations (07/2025 → 06/2026)

---

## 11. FILES LỚN NHẤT (cần chú ý khi sửa)

| File | Size | Ghi chú |
|---|---|---|
| `Views/Admin/Chat/Index.cshtml` | 41KB | Chat admin phức tạp |
| `Views/Auth/Profile.cshtml` | 41KB | Profile có nhiều tab |
| `Views/Product/Details.cshtml` | 39KB | Chi tiết SP + review |
| `Views/Admin/Report/Index.cshtml` | 36KB | Báo cáo đầy đủ |
| `Controllers/Admin/AdminReportController.cs` | 34KB | Logic báo cáo |
| `Views/Shared/_ChatPopup.cshtml` | 29KB | Chat popup |
| `Controllers/PayOSController.cs` | 27KB | Thanh toán |
| `Services/InventoryService.cs` | 27KB | Logic kho hàng |
| `Controllers/AuthController.cs` | 25KB | Auth flow |
| `Views/Cart/Index.cshtml` | 23KB | Giỏ hàng |

---

## 12. CHẠY DỰ ÁN

### Local (không Docker)
```bash
# Yêu cầu: .NET 8.0 SDK, SQL Server (SQLEXPRESS)
cd web-ban-thuoc
dotnet ef database update
dotnet run
# → https://localhost:5226
```

### Docker
```bash
docker-compose up --build
# Web: http://localhost:5000
# SQL: localhost:14330 (sa / MyStrongPassword123!)
```

### Dev Scripts
```powershell
./run-dev.ps1   # Chạy dev
./stop-dev.ps1  # Dừng dev
```

---

## 13. MIGRATION TIMELINE

| Ngày | Migration | Nội dung |
|---|---|---|
| 2025-07-12 | InitialCreate | Schema ban đầu |
| 2025-07-12 | UpdateReviewUserToIdentity | Review → IdentityUser |
| 2025-07-19 | AddFullNameAndPhoneToOrder | FullName, Phone vào Order |
| 2025-07-21 | AddChatMessageTable | Bảng ChatMessage |
| 2025-07-23 | AddUserRankAndMailStatus | UserRankInfo |
| 2025-07-23 | SyncUserRankInfo | Sync rank |
| 2025-07-23 | AddVoucherAndUserVoucher | Voucher system |
| 2025-07-24 | AddTotalSpent6Months | Tính hạng 6 tháng |
| 2025-07-24 | UpdateVoucherForPercentAndCategory | Voucher % + danh mục |
| 2025-07-24 | MakeDiscountAmountNullable | Nullable discount |
| 2025-07-24 | AddVoucherToOrder | Voucher info vào Order |
| 2025-07-25 | AddIsNewToUserVoucher | Flag voucher mới |
| 2025-07-25 | AddMaxUsageToVoucher | Giới hạn dùng |
| 2025-07-25 | AddUsedCountToVoucher | Đếm lượt dùng |
| 2025-07-31 | AddBannerTable | Banner |
| 2025-07-31 | AddBannerTypeToBanner | Loại banner |
| 2025-08-04 | AddTransactionIdToPayment | TransactionId |
| 2026-06-05 | MergeVoucherAndInventoryWarehouse | **Major** - Merge voucher + kho |
| 2026-06-05 | RestoreUserVouchersTable | Khôi phục UserVouchers |
| 2026-06-05 | Phase1_OrderWorkflowAndCart | Cart DB, Order workflow |
| 2026-06-06 | Phase2_WarehouseAndCatalog | **Major** - Warehouse, Supplier, Batch |
| 2026-06-06 | FixProductImagesTableName | Sửa tên bảng |
| 2026-06-07 | AddProductSupplier | Product → Supplier FK |
| 2026-06-08 | Phase3_MarketingAndLoyalty | Loyalty points, VoucherRedemption |
| 2026-06-08 | AddLoyaltyRewards | Bảng LoyaltyReward |
| 2026-06-08 | Phase4_OperationsAndShipping | Shipment, PayOSWebhookEvent |
| 2026-06-09 | PendingChanges | Sửa nhỏ cuối |

---

> **Lưu ý khi sử dụng file này**: Copy toàn bộ nội dung này vào message đầu tiên của session mới với AI assistant, kèm theo yêu cầu cụ thể của bạn. AI sẽ nắm được context dự án ngay lập tức.
