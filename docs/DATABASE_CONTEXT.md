# CƠ SỞ DỮ LIỆU — NHÀ THUỐC LONG CHÂU

> **Mục đích**: Tài liệu tập trung toàn bộ thông tin liên quan đến CSDL — schema, entity, quan hệ, migration, quy ước lưu trữ. Dùng cho báo cáo CSDL, ERD, thiết kế bảng, hoặc đưa context cho AI.
> **Cập nhật lần cuối**: 2026-06-17
> **Liên quan**: [PROJECT_CONTEXT.md](./PROJECT_CONTEXT.md) · [BUSINESS_CONTEXT.md](./BUSINESS_CONTEXT.md)

---

## 1. TỔNG QUAN CSDL

| Thuộc tính | Giá trị |
|---|---|
| **Hệ quản trị** | Microsoft SQL Server |
| **ORM** | Entity Framework Core 9.x (Code-First) |
| **DbContext** | `LongChauDbContext` — kế thừa `IdentityDbContext` |
| **File DbContext** | `web-ban-thuoc/Models/LongChauDbContext.cs` (~307 dòng) |
| **Số DbSet nghiệp vụ** | 29 |
| **Bảng Identity** | AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims |
| **Tên database (local)** | `LongChauDB_New` |
| **Migration folder** | `web-ban-thuoc/Migrations/` (~28 cặp migration, 55 file) |
| **Snapshot** | `Migrations/LongChauDbContextModelSnapshot.cs` |

### Packages EF Core

- `Microsoft.EntityFrameworkCore.SqlServer` 9.0.7
- `Microsoft.EntityFrameworkCore.Tools` 9.0.6
- `Microsoft.EntityFrameworkCore.Design` 9.0.6
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0

---

## 2. KẾT NỐI & TRIỂN KHAI

### Connection string (appsettings.json)

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=LongChauDB_New;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### Chạy migration

```bash
cd web-ban-thuoc
dotnet ef database update
```

### Docker

| Thành phần | Giá trị |
|---|---|
| SQL Server image | 2022 |
| Port host | `14330` |
| Web app port | `5000` |
| SA password | `MyStrongPassword123!` |
| Init script | `docker/sql/LongChauDB.sql` |
| Compose file | `docker-compose.yml` |

### SQL script thủ công

- `web-ban-thuoc/LongChauDB_New.sql` — script khởi tạo DB
- `docker/sql/LongChauDB.sql` — script Docker init

### ERD (tài liệu đồ họa)

Thư mục `docs/ERD/`:

| File | Mô tả |
|---|---|
| `chen-erd.png` | ERD đầy đủ |
| `chen-erd-core.png` | ERD lõi |
| `chen-erd-core.svg` | ERD lõi (vector) |
| `chen-erd.dot` / `chen-erd-core.dot` | Nguồn Graphviz |

---

## 3. DANH SÁCH DbSet (29 BẢNG NGHIỆP VỤ)

| # | Entity | DbSet property | Bảng (mặc định EF) |
|---|---|---|---|
| 1 | Category | Categories | Categories |
| 2 | Product | Products | Products |
| 3 | ProductImage | ProductImages | ProductImages |
| 4 | Order | Orders | Orders |
| 5 | OrderItem | OrderItems | OrderItems |
| 6 | OrderStatusHistory | OrderStatusHistories | OrderStatusHistories |
| 7 | Cart | Carts | Carts |
| 8 | CartItem | CartItems | CartItems |
| 9 | Review | Reviews | Reviews |
| 10 | Payment | Payments | Payments |
| 11 | Voucher | Vouchers | Vouchers |
| 12 | UserVoucher | UserVouchers | UserVouchers |
| 13 | VoucherRedemption | VoucherRedemptions | VoucherRedemptions |
| 14 | Banner | Banners | Banners |
| 15 | ChatMessage | ChatMessages | ChatMessages |
| 16 | UserRankInfo | UserRankInfos | UserRankInfos |
| 17 | LoyaltyPointTransaction | LoyaltyPointTransactions | LoyaltyPointTransactions |
| 18 | LoyaltyReward | LoyaltyRewards | LoyaltyRewards |
| 19 | Warehouse | Warehouses | Warehouses |
| 20 | WarehouseStock | WarehouseStocks | WarehouseStocks |
| 21 | InventoryTransaction | InventoryTransactions | InventoryTransactions |
| 22 | ProductBatch | ProductBatches | ProductBatches |
| 23 | Supplier | Suppliers | Suppliers |
| 24 | PurchaseOrder | PurchaseOrders | PurchaseOrders |
| 25 | PurchaseOrderLine | PurchaseOrderLines | PurchaseOrderLines |
| 26 | GoodsReceipt | GoodsReceipts | GoodsReceipts |
| 27 | GoodsReceiptLine | GoodsReceiptLines | GoodsReceiptLines |
| 28 | Shipment | Shipments | Shipments |
| 29 | PayOSWebhookEvent | PayOSWebhookEvents | PayOSWebhookEvents |

---

## 4. SƠ ĐỒ QUAN HỆ (ERD TEXT)

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
│ Import/Sale/Return/ │     │ (BatchNo, Expiry)│
│ BatchSale/Adjustment│     └──────────────────┘
└─────────────────────┘

┌───────────────┐     ┌──────────────────┐     ┌──────────────────┐
│ PurchaseOrder │────►│PurchaseOrderLine │     │  GoodsReceipt    │
│ (Supplier+WH) │     │                  │     │  (Supplier+WH)   │
└───────────────┘     └──────────────────┘     └────────┬─────────┘
                                                        │
                                                ┌───────┴──────────┐
                                                │GoodsReceiptLine  │
                                                │→ tạo ProductBatch│
                                                └──────────────────┘

Cart (1 user = 1 cart) ──► CartItem ──► Product
Voucher ──► UserVoucher, VoucherRedemption
UserRankInfo (PK = UserId) ──► AspNetUsers
LoyaltyPointTransaction ──► Order?, LoyaltyReward?
PayOSWebhookEvent (IdempotencyKey unique)
```

---

## 5. CHI TIẾT TỪNG BẢNG

### 5.1 Danh mục & Sản phẩm

#### Category (tự tham chiếu 3 cấp)

| Cột | Kiểu | Ghi chú |
|---|---|---|
| CategoryId | int PK | |
| CategoryName | string (required) | |
| Description | string? | |
| ImageUrl | string? | |
| ParentCategoryId | int? FK | → Category (self) |
| IsFeature | bool | Danh mục nổi bật |
| CategoryLevel | string? | `"Level 1"` / `"Level 2"` / `"Level 3"` |
| ProductCount | int | Đếm SP (cache) |

**Nav**: InverseParentCategory, ParentCategory, Products

#### Product

| Cột | Kiểu | Ghi chú |
|---|---|---|
| ProductId | int PK | |
| ProductName | string (required) | |
| Sku | string? | **Unique** (filtered: NOT NULL và <> '') |
| Barcode | string? | |
| RegistrationNumber | string? | Số đăng ký lưu hành BYT |
| RequiresPrescription | bool | Thuốc kê đơn |
| CostPrice | decimal? | Giá vốn |
| Price | decimal | Giá bán |
| Brand | string? | Thương hiệu |
| Package | string? | Quy cách đóng gói |
| CategoryId | int? FK | → Category (SetNull) |
| SupplierId | int? FK | → Supplier (SetNull) |
| Ingredients | string? | Thành phần |
| Uses | string? | Công dụng |
| Dosage | string? | Liều dùng |
| TargetUsers | string? | Đối tượng |
| Contraindications | string? | Chống chỉ định |
| IngredientUnit | string? | Đơn vị thành phần |
| IsFeature | bool | SP nổi bật |
| Origin | string? | Xuất xứ |
| StockQuantity | int | Tổng tồn (sync từ WarehouseStocks) |
| MinStockLevel | int | Ngưỡng cảnh báo hết hàng (default 0) |
| IsActive | bool | Đang kinh doanh |
| Slug | string? | URL-friendly |
| SoldQuantity | int? | Đã bán |

**Nav**: Category, Supplier, ProductImages, Reviews, OrderItems, InventoryTransactions, WarehouseStocks, ProductBatches

#### ProductImage

| Cột | Kiểu | Ghi chú |
|---|---|---|
| ProductImageId | int PK | |
| ProductId | int FK | → Product |
| ImageUrl | string | |
| SortOrder | int? | |
| IsMain | bool? | Ảnh chính |

---

### 5.2 Đơn hàng & Giỏ hàng

#### Order

| Cột | Kiểu | Ghi chú |
|---|---|---|
| OrderId | int PK | |
| UserId | string? FK | → AspNetUsers (SetNull) |
| OrderDate | DateTime? | |
| TotalAmount | decimal? | Tổng sau giảm giá |
| Status | string? | Chuỗi tiếng Việt — xem `OrderStatuses` |
| ShippingAddress | string? | |
| PaymentStatus | string? | Xem `PaymentStatuses` |
| FullName | string? | Snapshot khách |
| Phone | string? | |
| VoucherCode | string? | Mã đã dùng |
| VoucherDiscount | decimal? | Số tiền giảm |
| PrescriptionNote | string? | Ghi chú kê đơn |

**Nav**: OrderItems, Payments, StatusHistories, User, Shipment (1-1)

#### OrderItem

| Cột | Kiểu | Ghi chú |
|---|---|---|
| OrderItemId | int PK | |
| OrderId | int? FK | → Order |
| ProductId | int? FK | → Product |
| Quantity | int | |
| Price | decimal | Giá tại thời điểm đặt |

#### OrderStatusHistory

| Cột | Kiểu | Ghi chú |
|---|---|---|
| OrderStatusHistoryId | int PK | |
| OrderId | int FK | → Order (**Cascade**) |
| FromStatus | string? | |
| ToStatus | string (required) | |
| ChangedByUserId | string? | |
| Note | string? | |
| ChangedAt | DateTime | |

#### Cart

| Cột | Kiểu | Ghi chú |
|---|---|---|
| CartId | int PK | |
| UserId | string | **Unique index** — mỗi user 1 giỏ |
| VoucherCode | string? | |
| VoucherDiscount | decimal | |
| UpdatedAt | DateTime | |

#### CartItem

| Cột | Kiểu | Ghi chú |
|---|---|---|
| CartItemId | int PK | |
| CartId | int FK | → Cart (**Cascade**) |
| ProductId | int FK | → Product (**Restrict**) |
| Quantity | int | |
| UnitPrice | decimal | |

#### Payment

| Cột | Kiểu | Ghi chú |
|---|---|---|
| PaymentId | int PK | |
| OrderId | int? FK | → Order |
| PaymentMethod | string? | PayOS, COD, ... |
| Amount | decimal? | |
| PaymentDate | DateTime? | |
| PaymentStatus | string? | Xem `PaymentStatuses` |
| TransactionId | string? | Mã giao dịch PayOS |

#### Shipment (1-1 với Order)

| Cột | Kiểu | Ghi chú |
|---|---|---|
| ShipmentId | int PK | |
| OrderId | int FK | **Unique** — 1 đơn 1 vận đơn (**Cascade**) |
| Carrier | string | GHN, GHTK, ViettelPost, Other |
| TrackingCode | string | |
| ShippingFee | decimal? | |
| ShippedAt | DateTime | |
| EstimatedDelivery | DateTime? | |
| Note | string? | |
| CreatedByUserId | string? | |
| CreatedAt | DateTime | |

---

### 5.3 Khuyến mãi & Loyalty

#### Voucher

| Cột | Kiểu | Ghi chú |
|---|---|---|
| VoucherId | int PK | |
| Code | string (required) | |
| Description | string | |
| ExpiryDate | DateTime | |
| DiscountAmount | decimal? | Giảm cố định (VNĐ) |
| PercentValue | decimal? | Giảm % |
| DiscountType | string | Default `"FullOrder"` |
| IsPublic | bool | true = mã dùng chung |
| IsActive | bool | |
| CategoryId | int? FK | → Category (SetNull) — voucher theo danh mục |
| CategoryName | string? | Cache tên danh mục |
| MinOrderAmount | decimal? | Đơn tối thiểu |
| RequiredRank | string? | Bạc / Vàng / Bạch kim |
| Detail | string? | |
| MaxUsage | int? | Tổng lượt dùng tối đa (null = không giới hạn) |
| UsedCount | int | Đã dùng |

#### UserVoucher

| Cột | Kiểu | Ghi chú |
|---|---|---|
| UserVoucherId | int PK | |
| UserId | string | |
| VoucherId | int FK | → Voucher (**Cascade**) |
| IsUsed | bool | |
| UsedDate | DateTime? | |
| IsNew | bool | Badge "mới" trên UI |

**Unique**: (UserId, VoucherId)

#### VoucherRedemption

| Cột | Kiểu | Ghi chú |
|---|---|---|
| VoucherRedemptionId | int PK | |
| VoucherId | int FK | → Voucher (**Restrict**) |
| UserId | string | |
| OrderId | int FK | → Order (**Restrict**) |
| DiscountAmount | decimal | |
| RedeemedAt | DateTime | |
| IsReverted | bool | Hoàn voucher khi hủy đơn |

**Unique**: (VoucherId, OrderId)

#### UserRankInfo

| Cột | Kiểu | Ghi chú |
|---|---|---|
| UserId | string PK | → AspNetUsers.Id |
| TotalSpent | decimal | Tổng chi tiêu all-time |
| TotalSpent6Months | decimal | Chi tiêu 6 tháng — tính hạng |
| Rank | string | Bạc / Vàng / Bạch kim |
| LoyaltyPoints | int | Điểm hiện có |
| LastRankMailSent | DateTime? | |
| LastNotiMailSent | DateTime? | |
| LastRankReset | DateTime? | |

#### LoyaltyReward

| Cột | Kiểu | Ghi chú |
|---|---|---|
| LoyaltyRewardId | int PK | |
| Title | string | |
| Description | string? | |
| PointsCost | int | |
| RewardType | string | `VoucherPercent` \| `VoucherFixed` |
| PercentValue | decimal? | |
| DiscountAmount | decimal? | |
| ExpiryDays | int | Default 30 |
| MinOrderAmount | decimal? | |
| RequiredRank | string? | |
| StockRemaining | int? | null = không giới hạn |
| MaxPerUser | int? | |
| IsActive | bool | |
| SortOrder | int | |
| CreatedAt | DateTime | |

#### LoyaltyPointTransaction

| Cột | Kiểu | Ghi chú |
|---|---|---|
| LoyaltyPointTransactionId | int PK | |
| UserId | string | |
| Points | int | Dương = cộng, âm = trừ |
| TransactionType | string | `Earn` \| `Adjust` \| `Redeem` |
| OrderId | int? FK | → Order (SetNull) |
| LoyaltyRewardId | int? FK | → LoyaltyReward (SetNull) |
| Description | string? | |
| CreatedAt | DateTime | |

**Index (filtered)**: (UserId, OrderId, TransactionType) WHERE OrderId IS NOT NULL

---

### 5.4 Kho hàng & Mua hàng NCC

#### Warehouse

| Cột | Kiểu | Ghi chú |
|---|---|---|
| WarehouseId | int PK | |
| Name | string | |
| Address | string? | |
| IsDefault | bool | Kho mặc định |
| IsActive | bool | |
| CreatedAt | DateTime | |

#### WarehouseStock

| Cột | Kiểu | Ghi chú |
|---|---|---|
| WarehouseStockId | int PK | |
| WarehouseId | int FK | → Warehouse (**Restrict**) |
| ProductId | int FK | → Product (**Restrict**) |
| QuantityOnHand | int | Tồn thực tế |
| QuantityReserved | int | Đã giữ chỗ |
| UpdatedAt | DateTime | |

**Unique**: (WarehouseId, ProductId)  
**Computed**: `AvailableQuantity = QuantityOnHand - QuantityReserved`

#### ProductBatch (FEFO — hết hạn sớm trước)

| Cột | Kiểu | Ghi chú |
|---|---|---|
| ProductBatchId | int PK | |
| ProductId | int FK | → Product (**Restrict**) |
| WarehouseId | int FK | → Warehouse (**Restrict**) |
| BatchNo | string | Số lô |
| ExpiryDate | DateTime? | HSD |
| QuantityOnHand | int | |
| UnitCost | decimal? | Giá vốn lô |
| SupplierId | int? FK | → Supplier (SetNull) |
| GoodsReceiptLineId | int? FK | → GoodsReceiptLine (SetNull, 1-1) |
| CreatedAt | DateTime | |

#### InventoryTransaction

| Cột | Kiểu | Ghi chú |
|---|---|---|
| TransactionId | int PK | |
| ProductId | int FK | → Product (**Restrict**) |
| WarehouseId | int FK | → Warehouse (**Restrict**) |
| TransactionType | string | Xem mục 6 |
| Quantity | int | Luôn dương |
| QuantityBefore | int | |
| QuantityAfter | int | |
| OrderId | int? FK | → Order (SetNull) |
| SupplierId | int? FK | → Supplier (SetNull) |
| ProductBatchId | int? FK | → ProductBatch (SetNull) |
| GoodsReceiptId | int? FK | → GoodsReceipt (SetNull) |
| SupplierName | string? | Snapshot |
| UnitCost | decimal? | |
| Note | string? | |
| CreatedByUserId | string? FK | → AspNetUsers (SetNull) |
| TransactionDate | DateTime | |

#### Supplier

| Cột | Kiểu | Ghi chú |
|---|---|---|
| SupplierId | int PK | |
| Code | string | **Unique** |
| Name | string | |
| Phone, Email, Address | string? | |
| TaxCode | string? | MST |
| IsActive | bool | |
| CreatedAt | DateTime | |

#### PurchaseOrder

| Cột | Kiểu | Ghi chú |
|---|---|---|
| PurchaseOrderId | int PK | |
| OrderCode | string | **Unique** |
| SupplierId | int FK | → Supplier (**Restrict**) |
| WarehouseId | int FK | → Warehouse (**Restrict**) |
| Status | string | Xem `PurchaseOrderStatuses` |
| OrderDate | DateTime | |
| ExpectedDate | DateTime? | |
| Note | string? | |
| CreatedByUserId | string? | |

#### PurchaseOrderLine

| Cột | Kiểu | Ghi chú |
|---|---|---|
| PurchaseOrderLineId | int PK | |
| PurchaseOrderId | int FK | → PurchaseOrder (**Cascade**) |
| ProductId | int FK | → Product (**Restrict**) |
| QuantityOrdered | int | |
| QuantityReceived | int | |
| UnitCost | decimal | |

**Computed**: `RemainingQuantity = max(0, QuantityOrdered - QuantityReceived)`

#### GoodsReceipt

| Cột | Kiểu | Ghi chú |
|---|---|---|
| GoodsReceiptId | int PK | |
| ReceiptCode | string | **Unique** |
| PurchaseOrderId | int? FK | → PurchaseOrder (SetNull) |
| SupplierId | int FK | → Supplier (**Restrict**) |
| WarehouseId | int FK | → Warehouse (**Restrict**) |
| ReceiptDate | DateTime | |
| Note | string? | |
| CreatedByUserId | string? | |

#### GoodsReceiptLine

| Cột | Kiểu | Ghi chú |
|---|---|---|
| GoodsReceiptLineId | int PK | |
| GoodsReceiptId | int FK | → GoodsReceipt (**Cascade**) |
| ProductId | int FK | → Product (**Restrict**) |
| PurchaseOrderLineId | int? FK | → PurchaseOrderLine |
| BatchNo | string | |
| ExpiryDate | DateTime? | |
| Quantity | int | |
| UnitCost | decimal | |

→ Khi nhập kho: tạo `ProductBatch` + `InventoryTransaction (Import)` + cập nhật `WarehouseStock` + sync `Product.StockQuantity`

---

### 5.5 Marketing, Đánh giá, Chat, Thanh toán

#### Banner

| Cột | Kiểu | Ghi chú |
|---|---|---|
| BannerId | int PK | |
| Title | string (max 100) | |
| Description | string? (max 200) | |
| ImageUrl | string | |
| LinkUrl | string? (max 200) | |
| BannerType | string | Loại banner |
| SortOrder | int | 0–999 |
| IsActive | bool | |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime? | |

#### Review

| Cột | Kiểu | Ghi chú |
|---|---|---|
| ReviewId | int PK | |
| UserId | string? FK | → AspNetUsers (SetNull) |
| ProductId | int? FK | → Product |
| Rating | int? | |
| Comment | string? | |
| ReviewDate | DateTime? | |

#### ChatMessage

| Cột | Kiểu | Ghi chú |
|---|---|---|
| Id | int PK | |
| SenderId | string? | User hoặc admin |
| ReceiverId | string? | |
| Message | string | |
| SentAt | DateTime | |
| IsRead | bool | |

#### PayOSWebhookEvent (idempotency)

| Cột | Kiểu | Ghi chú |
|---|---|---|
| PayOSWebhookEventId | int PK | |
| IdempotencyKey | string | **Unique** |
| OrderId | int? | |
| OrderCode | string? | |
| PaymentSuccess | bool | |
| RawPayload | string? | JSON gốc |
| ProcessedAt | DateTime | |

---

## 6. HẰNG SỐ LƯU TRONG DB (CHUỖI)

### OrderStatuses (`Orders.Status`)

| Hằng | Giá trị DB |
|---|---|
| PendingPayment | Chờ thanh toán |
| PendingConfirmation | Chờ xác nhận |
| Confirmed | Đã xác nhận |
| Packing | Đang đóng gói |
| Shipped | Đang giao |
| Delivered | Đã giao |
| Cancelled | Đã hủy |

### PaymentStatuses (`Orders.PaymentStatus`, `Payments.PaymentStatus`)

| Hằng | Giá trị DB |
|---|---|
| Unpaid | Chưa thanh toán |
| Pending | Pending |
| Paid | Đã thanh toán |
| Failed | Thanh toán thất bại |
| Cancelled | Đã hủy |

### InventoryTransaction.TransactionType

| Giá trị | Ý nghĩa |
|---|---|
| Import | Nhập kho (từ phiếu nhập / NCC) |
| Sale | Xuất kho khi bán (legacy / tổng) |
| BatchSale | Xuất theo lô FEFO — dùng tính COGS báo cáo |
| Return | Hoàn tồn khi hủy/trả đơn |
| Adjustment | Kiểm kê / điều chỉnh tồn |

### PurchaseOrderStatuses

| Hằng | Giá trị DB |
|---|---|
| Draft | Nháp |
| Confirmed | Đã xác nhận |
| PartiallyReceived | Nhận một phần |
| Received | Đã nhận đủ |
| Cancelled | Đã hủy |

### LoyaltyPointTypes

| Giá trị | Ý nghĩa |
|---|---|
| Earn | Tích điểm từ đơn |
| Adjust | Điều chỉnh thủ công |
| Redeem | Đổi quà |

### LoyaltyRewardTypes

| Giá trị | Ý nghĩa |
|---|---|
| VoucherPercent | Quà = voucher giảm % |
| VoucherFixed | Quà = voucher giảm cố định |

### ShippingCarriers (`Shipments.Carrier`)

| Code | Tên hiển thị |
|---|---|
| GHN | Giao Hàng Nhanh |
| GHTK | Giao Hàng Tiết Kiệm |
| ViettelPost | Viettel Post |
| Other | Khác / Tự giao |

---

## 7. RÀNG BUỘC & INDEX (OnModelCreating)

### Unique indexes

| Bảng | Cột | Ghi chú |
|---|---|---|
| UserVouchers | UserId + VoucherId | Mỗi user 1 lần / voucher |
| Carts | UserId | 1 giỏ / user |
| WarehouseStocks | WarehouseId + ProductId | |
| VoucherRedemptions | VoucherId + OrderId | |
| Suppliers | Code | |
| PurchaseOrders | OrderCode | |
| GoodsReceipts | ReceiptCode | |
| Products | Sku | Filtered unique |
| Shipments | OrderId | 1-1 Order |
| PayOSWebhookEvents | IdempotencyKey | Chống webhook trùng |

### OnDelete behaviors

| Quan hệ | Hành vi |
|---|---|
| Order → User | SetNull |
| Review → User | SetNull |
| UserVoucher → Voucher | **Cascade** |
| CartItem → Cart | **Cascade** |
| CartItem → Product | Restrict |
| OrderStatusHistory → Order | **Cascade** |
| InventoryTransaction → Product/Warehouse | Restrict |
| InventoryTransaction → Order/Supplier/Batch/GR | SetNull |
| WarehouseStock → Warehouse/Product | Restrict |
| ProductBatch → Product/Warehouse | Restrict |
| PurchaseOrderLine → PurchaseOrder | **Cascade** |
| GoodsReceiptLine → GoodsReceipt | **Cascade** |
| Shipment → Order | **Cascade** |
| VoucherRedemption → Voucher/Order | Restrict |

### Quy ước thiết kế

- **Soft reference**: Phần lớn FK dùng `SetNull` hoặc `Restrict` — tránh xóa cascade lan rộng
- **Cascade có chủ đích**: Cart→CartItem, Order→StatusHistory, PO→Lines, GR→Lines, Order→Shipment
- **Trạng thái**: Lưu **chuỗi tiếng Việt** trong DB (tương thích UI), định nghĩa hằng trong static class C#
- **Code-First**: Schema thay đổi qua migration, không sửa DB tay ngoài migration

---

## 8. IDENTITY (ASP.NET CORE IDENTITY)

DbContext kế thừa `IdentityDbContext` — các bảng mặc định:

- **AspNetUsers** — UserId (string) dùng làm FK ở Order, Review, Cart, UserRankInfo, ...
- **AspNetRoles** — Admin, WarehouseStaff, CustomerSupport (+ role khách mặc định)
- **AspNetUserRoles**, **AspNetUserClaims**, **AspNetUserLogins**, **AspNetUserTokens**, **AspNetRoleClaims**

Seed accounts (Program.cs):

| Email | Role |
|---|---|
| admin@gmail.com | Admin |
| warehouse@longchau.local | WarehouseStaff |
| support@longchau.local | CustomerSupport |

---

## 9. LỊCH SỬ MIGRATION

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
| 2025-08-04 | AddTransactionIdToPayment | TransactionId PayOS |
| 2026-06-05 | MergeVoucherAndInventoryWarehouse | **Major** — Merge voucher + kho |
| 2026-06-05 | RestoreUserVouchersTable | Khôi phục UserVouchers |
| 2026-06-05 | Phase1_OrderWorkflowAndCart | Cart DB, Order workflow |
| 2026-06-06 | Phase2_WarehouseAndCatalog | **Major** — Warehouse, Supplier, Batch |
| 2026-06-06 | FixProductImagesTableName | Sửa tên bảng |
| 2026-06-07 | AddProductSupplier | Product → Supplier FK |
| 2026-06-08 | Phase3_MarketingAndLoyalty | Loyalty points, VoucherRedemption |
| 2026-06-08 | AddLoyaltyRewards | Bảng LoyaltyReward |
| 2026-06-08 | Phase4_OperationsAndShipping | Shipment, PayOSWebhookEvent |
| 2026-06-09 | PendingChanges | Sửa nhỏ cuối |

### Giai đoạn phát triển schema

1. **Phase 0 (07/2025)**: Catalog, Order, Review, Payment, Chat, Voucher cơ bản, Banner
2. **Phase 1 (06/2025)**: Cart persistent, Order workflow + StatusHistory
3. **Phase 2**: Warehouse, Supplier, Batch, PurchaseOrder, GoodsReceipt, InventoryTransaction mở rộng
4. **Phase 3**: LoyaltyPointTransaction, VoucherRedemption, LoyaltyReward
5. **Phase 4**: Shipment, PayOSWebhookEvent (idempotency)

---

## 10. ĐỒNG BỘ DỮ LIỆU TỒN KHO

```
WarehouseStock.QuantityOnHand (nguồn tồn theo kho)
        ↓ SUM
Product.StockQuantity (tổng tồn hiển thị storefront + cảnh báo)

ProductBatch.QuantityOnHand (tồn theo lô/HSD — FEFO xuất kho)

InventoryTransaction — audit trail mọi biến động
```

**Service xử lý**: `InventoryService.cs` (~27KB)

---

## 11. TRUY VẤN THƯỜNG DÙNG (GỢI Ý BÁO CÁO)

### Doanh thu theo kỳ (đơn đã giao)

```sql
SELECT SUM(TotalAmount) AS Revenue
FROM Orders
WHERE Status = N'Đã giao'
  AND OrderDate BETWEEN @start AND @end;
```

### Tồn kho thấp

```sql
SELECT ProductName, Sku, StockQuantity, MinStockLevel
FROM Products
WHERE IsActive = 1 AND StockQuantity <= MinStockLevel;
```

### Lô sắp hết hạn (< 90 ngày)

```sql
SELECT p.ProductName, pb.BatchNo, pb.ExpiryDate, pb.QuantityOnHand
FROM ProductBatches pb
JOIN Products p ON p.ProductId = pb.ProductId
WHERE pb.QuantityOnHand > 0
  AND pb.ExpiryDate IS NOT NULL
  AND pb.ExpiryDate <= DATEADD(day, 90, GETDATE())
ORDER BY pb.ExpiryDate;
```

### Top khách hàng chi tiêu

```sql
SELECT u.Email, o.FullName, COUNT(*) AS OrderCount, SUM(o.TotalAmount) AS TotalSpent
FROM Orders o
LEFT JOIN AspNetUsers u ON u.Id = o.UserId
WHERE o.Status = N'Đã giao'
GROUP BY u.Email, o.FullName
ORDER BY TotalSpent DESC;
```

---

> **Cách dùng**: Khi viết báo cáo CSDL / ERD / bảng mô tả thuộc tính, lấy trực tiếp từ file này. Chi tiết nghiệp vụ (luồng xử lý, quy tắc kinh doanh) xem [BUSINESS_CONTEXT.md](./BUSINESS_CONTEXT.md).
