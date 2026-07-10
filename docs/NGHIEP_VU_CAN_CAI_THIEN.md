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

### [2/8] Biểu mẫu phiếu nhập/xuất kho

**Trạng thái:** ⏳ Chưa triển khai

**Nghiệp vụ:**
- **GRN (Goods Receipt Note – Phiếu nhập kho)**: xác nhận nhận hàng từ NCC, đối chiếu SL thực tế vs đơn đặt.
- **GIN (Goods Issue Note – Phiếu xuất kho)**: phiếu xuất kho kèm chữ ký, dùng trong điều chuyển nội bộ, trả hàng NCC.
- Đây là chứng từ kế toán pháp lý, lưu trữ, đối chiếu với hóa đơn.

**Luồng xử lý:**
```
Nhận hàng từ NCC → Đếm + đối chiếu với PO
   ↓
Lập GRN: mã NCC, mã PO, ngày, SL thực tế, số lô, hạn dùng, vị trí kệ
   ↓
Cập nhật tồn kho (cộng theo batch)
   ↓
In 2-3 bản: lưu kho / kế toán / gửi NCC
   ↓
Đối chiếu hóa đơn → thanh toán trong hạn
```

**Yêu cầu chức năng:**
- In phiếu nhập / phiếu xuất ra PDF
- Có chữ ký điện tử (audit log thay chữ ký)
- Export PDF / in trực tiếp từ trình duyệt

---

### [3/8] Vận đơn giao hàng

**Trạng thái:** ⏳ Chưa triển khai

**Nghiệp vụ:**
- Vận đơn đi kèm kiện hàng giao cho khách. Trong ngành dược, thuốc kê đơn cần xác nhận đúng người nhận.
- Ghi nhận điều kiện bảo quản (thuốc cần mát, tránh sáng).

**Luồng xử lý:**
```
Đơn hàng confirmed → Kho đóng gói → Phát sinh vận đơn
   ↓
Vận đơn: thông tin khách, SP/SL, ĐVVC, mã tracking, mã QR
   ↓
In 3 bản: khách / kho / shipper
   ↓
Shipper giao → Khách ký (hoặc quét QR)
   ↓
Cập nhật trạng thái đơn → nếu thất bại: lập phiếu hoàn
```

---

### [4/8] Báo cáo công nợ nhà cung cấp

**Trạng thái:** ⏳ Chưa triển khai

**Nghiệp vụ:**
- Công nợ NCC = số tiền còn nợ NCC chưa thanh toán. Giúp quản lý dòng tiền, tránh trả muộn gây mất quan hệ.

**Cấu trúc báo cáo:**
| Cột | Ý nghĩa |
|---|---|
| Mã NCC, Tên NCC | |
| Dư nợ đầu kỳ | Nợ còn lại từ kỳ trước |
| Phát sinh tăng | Tổng tiền nhập hàng trong kỳ |
| Phát sinh giảm | Tổng tiền đã thanh toán |
| Dư nợ cuối kỳ | Đầu + Tăng – Giảm |
| Ngày đến hạn kế tiếp | Deadline trả tiếp |
| Trạng thái | Trong hạn / Sắp đến hạn / Quá hạn |

**Luồng xử lý:**
```
Nhập hàng → Ghi nợ tự động (kèm hạn thanh toán theo HĐ NCC)
   ↓
Thanh toán → Ghi có (giảm nợ)
   ↓
Cuối kỳ: Tổng hợp báo cáo → Đối chiếu với NCC → Lên kế hoạch thanh toán
```

---

## 🟨 Ưu tiên trung bình – Cải thiện UX

---

### [5/8] Dashboard KPI tài chính

**Trạng thái:** ⏳ Chưa triển khai

**Nghiệp vụ:** Mỗi ngày quản lý cần nhìn nhanh doanh thu, đơn hàng, tồn kho, cảnh báo thuốc sắp hết hạn — thay vì lọc từng bảng riêng.

**Các chỉ số:**
- Doanh thu / lợi nhuận gộp theo ngày-tuần-tháng
- Số đơn, giá trị đơn trung bình
- Top 10 sản phẩm bán chạy
- Tồn kho theo danh mục, cảnh báo thuốc sắp hết hạn
- So sánh cùng kỳ (tháng này vs tháng trước)

---

### [6/8] Export Excel & Quản lý ảnh sản phẩm

**Trạng thái:** ⏳ Chưa triển khai

**Export Excel:**
- Tải danh sách sản phẩm / đơn hàng / tồn kho ra `.xlsx`
- Dùng thư viện **ClosedXML** (đã có trong references)

**Quản lý ảnh:**
- Upload nhiều ảnh/sản phẩm (chính + phụ)
- Resize tự động (thumbnail, medium, large)
- Validate: size ≤ 5MB, định dạng jpg/png/webp

---

### [7/8] Banner preview & scheduling

**Trạng thái:** ⏳ Chưa triển khai

**Preview:**
- Xem trước banner trên desktop + mobile trước khi publish.
- Tránh lỗi ảnh vỡ, text nhỏ, méo khi đã publish.

**Scheduling:**
- Đặt ngày bắt đầu / kết thúc để tự động ẩn/hiện theo mùa/chiến dịch (Tết, Trung Thu, flash sale...).

**Luồng xử lý:**
```
Tạo banner: upload ảnh + link + tiêu đề + ngày bắt đầu + kết thúc
   ↓
Preview trong mockup (desktop + mobile)
   ↓
Lưu trạng thái "Scheduled"
   ↓
Hệ thống check mỗi lần load trang:
   - Trước ngày bắt đầu → ẩn
   - Trong khoảng → hiện (Active)
   - Sau ngày kết thúc → ẩn (Expired)
```

---

### [8/8] Thống kê sử dụng Voucher

**Trạng thái:** ⏳ Chưa triển khai

**Nghiệp vụ:** Sau mỗi chiến dịch voucher, marketing cần đo lường hiệu quả.

**Các chỉ số cần đo:**
- Số lượt sử dụng / số lượt phát ra = **tỷ lệ đổi (redemption rate)**
- Doanh thu từ đơn có voucher vs không voucher
- Top voucher được dùng nhiều nhất
- Phân bố thời gian sử dụng (khách hay dùng giờ nào)

---

## 📊 Tổng kết độ phức tạp

| # | Nghiệp vụ | Độ phức tạp | Phụ thuộc | Trạng thái |
|---|---|---|---|---|
| 1 | Xuất kho thủ công + FEFO batch | 🟥 Cao | Batch, Inventory | ✅ Hoàn thành |
| 2 | Biểu mẫu phiếu nhập/xuất | 🟨 TB | Inventory | ⏳ Chưa |
| 3 | Vận đơn giao hàng | 🟨 TB | Order, Shipping | ⏳ Chưa |
| 4 | Báo cáo công nợ NCC | 🟨 TB | Purchase, Supplier | ⏳ Chưa |
| 5 | Dashboard KPI chart | 🟨 TB | Tổng hợp | ⏳ Chưa |
| 6 | Export Excel + Ảnh SP | 🟢 Thấp | Product hiện có | ⏳ Chưa |
| 7 | Banner preview + scheduling | 🟢 Thấp | Banner hiện có | ⏳ Chưa |
| 8 | Thống kê Voucher | 🟢 Thấp | Voucher hiện có | ⏳ Chưa |

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
