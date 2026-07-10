# TIÊU CHÍ ĐÁNH GIÁ DFD

> **Mục đích**: Checklist tiêu chí đánh giá sơ đồ DFD (Data Flow Diagram) trong báo cáo môn Các hệ quản trị CSDL.
> **Cập nhật**: 2026-06-17

---

## 1. VỀ CÚ PHÁP (SYNTAX)

| # | Tiêu chí |
|---|---|
| 1 | **Vẽ đúng ký hiệu** — process (vòng tròn/bộ chia), kho dữ liệu (hình chữ nhật mở), thực thể ngoài (hình chữ nhật), luồng dữ liệu (mũi tên) |
| 2 | **Có đánh số thứ tự các process** — ví dụ: 1.0, 2.0, 2.1, … |
| 3 | **Có kho dữ liệu** — biểu diễn nơi lưu trữ dữ liệu (D1, D2, … hoặc tên bảng) |
| 4 | **Đặt tên process (động từ)** — ví dụ: "Xử lý đơn hàng", "Cập nhật tồn kho" |
| 5 | **Các mũi tên phù hợp** — hướng luồng đúng, không vi phạm quy tắc DFD |
| 6 | **Dữ liệu trên các mũi tên hợp lý** — ghi rõ tên dữ liệu truyền (Đơn hàng, Thông tin SP, …) |

### Checklist nhanh — Cú pháp

- [ ] Ký hiệu process, data store, external entity, data flow đúng chuẩn
- [ ] Process được đánh số thứ tự
- [ ] Có ít nhất một kho dữ liệu
- [ ] Tên process là động từ / cụm động từ
- [ ] Mũi tên và nhãn dữ liệu trên mũi tên rõ ràng, hợp lý

---

## 2. VỀ NGỮ NGHĨA (SEMANTICS)

| # | Tiêu chí |
|---|---|
| 1 | **Các process phải hợp lý** với các bước trong quy trình của doanh nghiệp |
| 2 | **Các process phải có cập nhật kho dữ liệu, hoặc tạo ra thông tin có ích** |
| 3 | **Các dữ liệu quan trọng phải được lưu vào kho dữ liệu** |
| 4 | **Phải thể hiện được dòng chảy** của đối tượng dữ liệu quan trọng |

> Tiêu chí số 2 (cập nhật kho dữ liệu hoặc tạo thông tin có ích) là **bắt buộc** — process không làm gì hữu ích thì không nên tồn tại trên DFD.

### Checklist nhanh — Ngữ nghĩa

- [ ] Mỗi process tương ứng bước nghiệp vụ thực tế
- [ ] Mỗi process đọc/ghi kho dữ liệu HOẶC xuất thông tin có giá trị
- [ ] Dữ liệu quan trọng (đơn, tồn kho, thanh toán…) được lưu vào data store
- [ ] Luồng dữ liệu chính được thể hiện xuyên suốt từ đầu đến cuối

---

## 3. VỀ MỤC TIÊU (OBJECTIVES)

| # | Tiêu chí |
|---|---|
| 1 | **Phải ghi rõ mục đích** vẽ các sơ đồ DFD (context, level 0, level 1…) |

### Mục đích trong đề tài môn học này

Vẽ DFD nhằm:

| Mục tiêu | Ý nghĩa | Dẫn tới cài đặt |
|---|---|---|
| **Tìm ra các trường Trạng thái** của các đối tượng dữ liệu | Ví dụ: `Order.Status`, `PurchaseOrder.Status`, `Payment.PaymentStatus` | Thiết kế cột trạng thái + CHECK constraint |
| **Tìm ra các nghiệp vụ phức tạp** cần viết **Procedure** | Ví dụ: xuất kho FEFO, checkout, nhập kho | Mục 4.3.2 — Stored Procedure |
| **Tìm ra các RBDL phức tạp** cần viết **Trigger** | Ví dụ: không cho tồn âm, ghi audit khi đổi trạng thái | Trigger + Constraint |

> **RBDL** = Ràng buộc dữ liệu (Business Rules ở mức CSDL).

### Checklist nhanh — Mục tiêu

- [ ] Đã ghi mục đích vẽ DFD (context / level 0 / level 1)
- [ ] Đã liệt kê trường Trạng thái tìm được từ DFD
- [ ] Đã xác định nghiệp vụ cần Procedure
- [ ] Đã xác định ràng buộc cần Trigger

---

## 4. GỢI Ý ÁP DỤNG CHO ĐỀ TÀI LONG CHÂU

### Process DFD gợi ý (Level 0 / Level 1)

| Process | Kho dữ liệu liên quan | Trạng thái / RBDL |
|---|---|---|
| Xử lý đặt hàng | D: Orders, OrderItems, Carts | Order.Status |
| Xử lý thanh toán | D: Payments, Orders | PaymentStatus |
| Xuất kho bán hàng | D: WarehouseStocks, ProductBatches, InventoryTransactions | TransactionType = Sale/BatchSale |
| Nhập kho từ NCC | D: GoodsReceipts, PurchaseOrders, ProductBatches | PO Status |
| Quản lý voucher | D: Vouchers, VoucherRedemptions | UsedCount, IsReverted |

### External entities gợi ý

- Khách hàng
- Admin / Nhân viên kho
- Nhà cung cấp (NCC)
- PayOS (cổng thanh toán)

---

## 5. TÓM TẮT 3 NHÓM TIÊU CHÍ

```
1. Cú pháp   → Ký hiệu đúng, đánh số process, kho dữ liệu, tên động từ, mũi tên & nhãn hợp lý
2. Ngữ nghĩa → Process khớp nghiệp vụ, cập nhật kho / tạo thông tin, luồng dữ liệu quan trọng
3. Mục tiêu  → Ghi rõ mục đích; tìm Trạng thái → Procedure → Trigger
```

---

> **Tham chiếu nghiệp vụ dự án**: `BUSINESS_CONTEXT.md` (quy trình bán hàng, kho, thanh toán).
