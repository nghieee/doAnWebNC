# Nghiệp vụ cần cải thiện – Nhà thuốc Long Châu

> Trạng thái: Hoàn thành Feature 1/8
> Cập nhật: 2026-07-07

---

## 🟥 Ưu tiên cao – Nghiệp vụ cốt lõi

---

### ✅ [1/8] Xuất kho thủ công & FEFO batch

**Trạng thái:** ✅ **Hoàn thành** – 2026-07-07

**Nghiệp vụ:**
- Xuất kho thủ công: hàng ra khỏi kho không qua đơn online (hủy hết hạn, trả NCC, chuyển kho, bán trực tiếp quầy).
- Nguyên tắc **FEFO** (First Expired First Out): lô sắp hết hạn xuất trước — bắt buộc trong ngành dược.

**Luồng xử lý:**
```
Yêu cầu xuất (từ đơn hàng / phiếu yêu cầu nội bộ / quầy)
   ↓
Nhân viên kho kiểm tra tồn theo batch → áp dụng FEFO
   ↓
Lập phiếu xuất: mã SP + số lô + hạn dùng + SL + lý do + người yêu cầu
   ↓
Cập nhật tồn kho (trừ theo từng batch, không trừ tổng)
   ↓
Quản lý / Kế toán duyệt
   ↓
Log audit (ai xuất, lúc nào, xuất gì, duyệt hay chưa)
```

**Chi tiết triển khai:**

#### Database
- Bảng `StockAdjustments` – lưu phiếu điều chỉnh tồn kho (mã tự động `SA{yyyyMMdd}-{nnnn}`, ví dụ `SA20260707-0001`)
- Bảng `StockAdjustmentDetails` – chi tiết từng dòng sản phẩm theo batch
- Migration: `AddStockAdjustment` (2026-07-07)

#### Model
- `StockAdjustment.cs` – entity + navigation properties
- `StockAdjustmentDetail.cs` – chi tiết dòng gắn batch
- `StockAdjustmentTypes` – enum: `Export`, `Import`, `Positive`, `Negative`
- `StockAdjustmentStatuses` – enum: `Pending`, `Approved`, `Rejected`
- `StockAdjustmentReasons` – dictionary lý do xuất: hết hạn, hỏng, trả NCC, điều chuyển, bán quầy, kiểm kê...
- `StockAdjustmentViewModels.cs` – ViewModels cho controller và view

#### Service (InventoryService)
- `CreateStockAdjustmentAsync()` – tạo phiếu mới (chờ duyệt hoặc tự động duyệt)
- `ApproveStockAdjustmentAsync()` – duyệt: cập nhật tồn kho batch + tạo InventoryTransaction
- `RejectStockAdjustmentAsync()` – từ chối: cập nhật trạng thái + ghi lý do
- `GetFefoBatchesAsync()` – lấy danh sách lô FEFO theo sản phẩm + kho
- `DeductFromBatchesFefoManualAsync()` – trừ tồn batch theo FEFO (hỗ trợ xuất nhiều lô cùng lúc)

#### Controller (AdminInventoryController)
- `GET /AdminInventory/StockAdjustments` – danh sách phiếu (filter theo kho, trạng thái, loại)
- `GET /AdminInventory/CreateStockAdjustment` – form tạo phiếu mới
- `POST /AdminInventory/CreateStockAdjustment` – xử lý tạo phiếu
- `GET /AdminInventory/StockAdjustmentDetails/{id}` – chi tiết phiếu + nút duyệt/từ chối
- `POST /AdminInventory/ApproveStockAdjustment/{id}` – duyệt phiếu
- `POST /AdminInventory/RejectStockAdjustment/{id}` – từ chối phiếu
- `POST /AdminInventory/DeleteStockAdjustment/{id}` – xóa phiếu (chỉ chờ duyệt)
- `GET /AdminInventory/GetFefoBatches?productId=&warehouseId=` – API lấy lô FEFO (cho AJAX)
- `GET /AdminInventory/PrintStockAdjustment/{id}` – in phiếu ra printer

#### Views
- `StockAdjustments.cshtml` – danh sách phiếu: thống kê + bảng lọc + badge trạng thái
- `CreateStockAdjustment.cshtml` – form tạo: chọn kho/loại/lý do + bảng dòng sản phẩm
  - Tìm kiếm sản phẩm AJAX (reuse `GetProductsForWarehouse`)
  - Gợi ý FEFO: khi chọn sản phẩm → tự động load batch FEFO + auto-select lô đầu tiên
  - Validation: phải chọn lô khi xuất kho, phải chọn sản phẩm + SL > 0
  - Thêm/xóa dòng động
- `StockAdjustmentDetails.cshtml` – chi tiết: thông tin phiếu + bảng sản phẩm + modal từ chối
- `PrintStockAdjustment.cshtml` – mẫu in A4: header Long Châu, bảng chi tiết, chữ ký 3 bên

#### Luồng nghiệp vụ đầy đủ
```
1. Nhân viên kho vào Admin → Kho & NCC → Phiếu điều chỉnh → "Tạo phiếu mới"
2. Chọn kho → Chọn loại (Xuất kho / Nhập kho / Điều chỉnh tăng / Điều chỉnh giảm)
3. Chọn lý do (hết hạn / hỏng / trả NCC / điều chuyển / bán quầy / kiểm kê)
4. Thêm dòng sản phẩm:
   - Tìm sản phẩm → hệ thống load lô FEFO (hết hạn sớm nhất lên đầu)
   - Auto-select lô FEFO đầu tiên → nhập SL → nhập ghi chú
   - Nếu SL vượt 1 lô → hệ thống tự trừ nhiều lô theo FEFO khi duyệt
5. Nhấn "Tạo phiếu":
   - Nếu là Admin/WarehouseStaff → tự động duyệt → tồn kho cập nhật ngay
   - Nếu không → trạng thái "Chờ duyệt"
6. Xem chi tiết → Duyệt / Từ chối / In phiếu (A4)
```

**File tạo mới:**
```
web-ban-thuoc/
├── Models/
│   ├── StockAdjustment.cs          ← Entity + navigation
│   ├── StockAdjustmentDetail.cs   ← Chi tiết dòng
│   └── StockAdjustmentViewModels.cs ← ViewModels
├── Services/
│   └── InventoryService.cs         ← (mở rộng) các method StockAdjustment
├── Controllers/Admin/
│   └── AdminInventoryController.cs ← (mở rộng) các action mới
├── Views/Admin/Inventory/
│   ├── StockAdjustments.cshtml     ← Danh sách phiếu
│   ├── CreateStockAdjustment.cshtml ← Form tạo (FEFO)
│   ├── StockAdjustmentDetails.cshtml ← Chi tiết + duyệt/từ chối
│   └── PrintStockAdjustment.cshtml ← Mẫu in A4
└── Migrations/
    └── {timestamp}_AddStockAdjustment.cs ← Migration DB
```

---

## 🟥 Ưu tiên cao – Nghiệp vụ cốt lõi

---

### ✅ [1/8] Xuất kho thủ công & FEFO batch

**Trạng thái:** ✅ Đã hoàn thành – 2026-07-07

**Nghiệp vụ:**
- Xuất kho thủ công: hàng ra khỏi kho không qua đơn online (hủy hết hạn, trả NCC, chuyển kho, bán trực tiếp quầy).
- Nguyên tắc **FEFO** (First Expired First Out): lô sắp hết hạn xuất trước — bắt buộc trong ngành dược.

**Luồng xử lý:**
```
Yêu cầu xuất (từ đơn hàng / phiếu yêu cầu nội bộ / quầy)
   ↓
Nhân viên kho kiểm tra tồn theo batch → áp dụng FEFO
   ↓
Lập phiếu xuất: mã SP + số lô + hạn dùng + SL + lý do + người yêu cầu
   ↓
Cập nhật tồn kho (trừ theo từng batch, không trừ tổng)
   ↓
Quản lý / Kế toán duyệt
   ↓
Log audit (ai xuất, lúc nào, xuất gì, duyệt hay chưa)
```

**Chi tiết triển khai:**
- Bảng `StockAdjustment` – lưu phiếu điều chỉnh tồn kho
- Bảng `StockAdjustmentDetail` – chi tiết từng dòng sản phẩm theo batch
- Enum `AdjustmentType`: `Export` (xuất kho thủ công), `Import` (nhập điều chỉnh), `Positive` (điều chỉnh tăng), `Negative` (điều chỉnh giảm)
- FEFO auto-suggest: khi nhập SL xuất, hệ thống tự gợi ý các batch theo thứ tự hạn dùng gần nhất

---

### ✅ [2/8] Biểu mẫu phiếu nhập/xuất kho

**Trạng thái:** ✅ **Hoàn thành** – 2026-07-23

**Nghiệp vụ:**
- **GRN (Goods Receipt Note – Phiếu nhập kho)**: xác nhận nhận hàng từ NCC, đối chiếu SL thực tế vs đơn đặt (In đơn đặt hàng PO + In phiếu nhập kho theo đợt GRN trong `AdminPurchaseController`).
- **GIN (Goods Issue Note – Phiếu xuất kho)**: phiếu xuất kho kèm chữ ký, dùng trong điều chuyển nội bộ, trả hàng NCC (In phiếu xuất kho / điều chỉnh A4 trong `AdminInventoryController`).
- Chứng từ kế toán chuẩn giao diện Long Châu, sẵn sàng in ấn/xuất PDF từ trình duyệt.

**Đã triển khai:**
- `AdminPurchaseController`: Action `Print(id)` -> Mẫu in Đơn đặt hàng NCC
- `AdminPurchaseController`: Action `PrintReceipt(receiptNumber)` -> Mẫu in Phiếu nhập kho GRN
- `AdminInventoryController`: Action `PrintStockAdjustment(id)` -> Mẫu in Phiếu xuất kho/điều chỉnh GIN (A4)

---

### ✅ [3/8] Vận đơn giao hàng

**Trạng thái:** ✅ **Hoàn thành** – 2026-07-23

**Nghiệp vụ:**
- Tích hợp API Giao Hàng Nhanh (GHN): tạo đơn vận chuyển tự động, tra cứu mã vận đơn, tính phí ship.
- In tem/vận đơn trực tiếp theo định dạng A5 hoặc 80x80mm (`AdminShippingController.PrintLabel`).
- Quản lý trạng thái giao hàng, đồng bộ mã tracking GHN trên hệ thống đơn hàng.

---

### ✅ [4/8] Báo cáo công nợ nhà cung cấp

**Trạng thái:** ✅ **Hoàn thành** – 2026-07-23

**Nghiệp vụ:**
- Quản lý và đối chiếu công nợ nhà cung cấp (`/AdminReport/SupplierDebt`).
- Tính toán dư nợ đầu kỳ, phát sinh tăng (nhập hàng), phát sinh giảm (đã trả), dư nợ cuối kỳ và trạng thái nợ (Trong hạn / Sắp đến hạn / Quá hạn).

---

## 🟨 Ưu tiên trung bình – Cải thiện UX

---

### ✅ [5/8] Dashboard KPI tài chính

**Trạng thái:** ✅ **Hoàn thành** – 2026-07-23

**Nghiệp vụ:**
- Dashboard báo cáo quản trị tổng quan (`/AdminReport`): Doanh thu, lợi nhuận gộp, biên lợi nhuận, dòng tiền ròng, tồn kho và cảnh báo lô sắp hết hạn.
- Tích hợp liên kết nhanh tới Báo cáo Công nợ NCC, Thống kê Voucher, In báo cáo & Xuất file dữ liệu.

---

### ✅ [6/8] Export Excel & Quản lý ảnh sản phẩm

**Trạng thái:** ✅ **Hoàn thành** – 2026-07-23

**Export Excel:**
- Xuất dữ liệu ra file `.xlsx` chuẩn bằng thư viện **ClosedXML** cho Danh sách sản phẩm (`/AdminReport/ExportProductsExcel`), Đơn hàng (`/AdminReport/ExportOrdersExcel`), Tồn kho (`/AdminReport/ExportInventoryExcel`).

---

### ✅ [7/8] Banner preview & scheduling

**Trạng thái:** ✅ **Hoàn thành** – 2026-07-23

**Preview & Scheduling:**
- Modal xem trước hiển thị Banner linh hoạt theo giao diện **Desktop** và **Mobile** trực tiếp trên trang quản lý (`/AdminBanner`).
- Đơn giản hóa việc kiểm tra bố cục và hình ảnh banner trước khi phát hành.

---

### ✅ [8/8] Thống kê sử dụng Voucher

**Trạng thái:** ✅ **Hoàn thành** – 2026-07-23

**Nghiệp vụ:**
- Báo cáo thống kê hiệu quả Voucher (`/AdminReport/VoucherStats`): Tỷ lệ quy đổi (Redemption rate %), tổng tiền chiết khấu và tổng doanh thu mang lại cho từng mã voucher.

---

## 📊 Tổng kết độ phức tạp

| # | Nghiệp vụ | Độ phức tạp | Phụ thuộc | Trạng thái |
|---|---|---|---|---|
| 1 | Xuất kho thủ công + FEFO batch | 🟥 Cao | Batch, Inventory | ✅ Hoàn thành |
| 2 | Biểu mẫu phiếu nhập/xuất | 🟨 TB | Inventory | ✅ Hoàn thành |
| 3 | Vận đơn giao hàng | 🟨 TB | Order, Shipping | ✅ Hoàn thành |
| 4 | Báo cáo công nợ NCC | 🟨 TB | Purchase, Supplier | ✅ Hoàn thành |
| 5 | Dashboard KPI chart | 🟨 TB | Tổng hợp | ✅ Hoàn thành |
| 6 | Export Excel + Ảnh SP | 🟢 Thấp | Product hiện có | ✅ Hoàn thành |
| 7 | Banner preview + scheduling | 🟢 Thấp | Banner hiện có | ✅ Hoàn thành |
| 8 | Thống kê Voucher | 🟢 Thấp | Voucher hiện có | ✅ Hoàn thành |

---

## 🌟 Các nghiệp vụ nâng cấp Premium bổ sung

**Trạng thái:** ✅ **Hoàn thành toàn bộ** – 2026-07-24

### 1. Bảng Chọn sản phẩm Đa năng trong Admin Discount
- **Nghiệp vụ**: Khi thiết lập giảm giá hoặc áp dụng chiến dịch, Admin cần chọn nhanh sản phẩm. Thay vì hiển thị dropdown chọn thủ công dễ lỗi, hệ thống cung cấp một bảng dữ liệu đầy đủ có tìm kiếm, phân trang và checkbox chọn hàng loạt.
- **Triển khai**: File [Views/Admin/Discount/Index.cshtml](file:///d:/DoAnCaNhan/doAnWebNC/web-ban-thuoc/Views/Admin/Discount/Index.cshtml).

### 2. Liên kết Banner quảng cáo với Chiến dịch & Sản phẩm
- **Nghiệp vụ**: Cho phép click vào các banner quảng cáo ngoài trang chủ để chuyển thẳng đến trang danh sách các sản phẩm đang được áp dụng chiến dịch giảm giá tương ứng của banner đó.
- **Triển khai**: Endpoint `GET /Home/Campaign/{id}` và `GET /Home/BannerProducts/{id}` kết hợp với layout lọc AJAX chuyên nghiệp.

### 3. Đồng hồ đếm ngược Khuyến mãi trực quan (Real-time Countdown)
- **Nghiệp vụ**: Hiển thị hộp quà ưu đãi nổi bật kèm đồng hồ đếm ngược (Ngày, Giờ, Phút, Giây) chạy giật lùi theo thời gian thực trên trang Chi tiết sản phẩm để thúc đẩy hành vi mua sắm.
- **Triển khai**: Tích hợp JS đếm ngược tự động và CSS ấm áp tại [Views/Product/Details.cshtml](file:///d:/DoAnCaNhan/doAnWebNC/web-ban-thuoc/Views/Product/Details.cshtml).

### 4. Tự động đồng bộ số lượng sản phẩm danh mục nổi bật
- **Nghiệp vụ**: Tự động tính toán lại số lượng sản phẩm đang kích hoạt (`ProductCount`) bao gồm đệ quy toàn bộ danh mục con/cháu để cập nhật dữ liệu hiển thị chính xác ngoài trang chủ.
- **Triển khai**: Cơ chế đếm đệ quy tự động trong `HomeController` và `AdminCategoryController`.

---

## 📁 Cấu trúc file liên quan (Feature 1 – Xuất kho thủ công)

```
web-ban-thuoc/
├── Models/
│   ├── StockAdjustment.cs           ← Entity + navigation
│   ├── StockAdjustmentDetail.cs    ← Chi tiết dòng
│   └── StockAdjustmentViewModels.cs ← ViewModels
├── Services/
│   └── InventoryService.cs           ← (mở rộng) method StockAdjustment
├── Controllers/Admin/
│   └── AdminInventoryController.cs   ← (mở rộng) action mới
├── Views/Admin/Inventory/
│   ├── StockAdjustments.cshtml      ← Danh sách phiếu
│   ├── CreateStockAdjustment.cshtml  ← Form tạo (FEFO)
│   ├── StockAdjustmentDetails.cshtml ← Chi tiết + duyệt/từ chối
│   └── PrintStockAdjustment.cshtml  ← Mẫu in A4
└── Migrations/
    └── {timestamp}_AddStockAdjustment.cs ← Migration DB
```
