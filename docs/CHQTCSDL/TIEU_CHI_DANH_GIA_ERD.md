# TIÊU CHÍ ĐÁNH GIÁ ERD

> **Mục đích**: Checklist tiêu chí đánh giá sơ đồ ERD trong báo cáo môn Các hệ quản trị CSDL. Dùng khi vẽ và tự kiểm tra trước khi nộp.
> **Cập nhật**: 2026-06-17

---

## 1. KÝ HIỆU

- **Ưu tiên sử dụng ký hiệu ER chuẩn**; hạn chế sử dụng các ký hiệu của ER mở rộng.
- Ở **Báo cáo 1**, **nên** vẽ ở mức **Concept**, và theo **bộ ký hiệu Chen**.
- Trong đề tài môn học, **phải ghi chú** vẽ theo bộ ký hiệu nào (Chen, Crow's Foot, …).

---

## 2. THỰC THỂ (ENTITY)

| Tiêu chí | Yêu cầu |
|---|---|
| Số lượng | **Nhiều** — đủ phản ánh phạm vi đề tài, không quá ít |
| Đặt tên | Là **danh từ** (Product, Order, Supplier, …) |
| Định danh | **Có ID ở thế giới thực** — mỗi thực thể phải có khóa/định danh rõ ràng |
| Loại thực thể hợp lệ | Đối tượng vật lý ở thế giới thực |
| | Chứng từ kinh doanh (Đơn hàng, Phiếu nhập, …) |
| | Các bảng danh mục, bảng phân loại (Category, Supplier, …) |
| Phạm vi | **Không nên** có các thực thể dư thừa, ngoài phạm vi đề tài |

### Checklist nhanh — Thực thể

- [ ] Tên thực thể là danh từ
- [ ] Mỗi thực thể có khóa chính / ID thế giới thực
- [ ] Có đủ thực thể trong phạm vi đề tài
- [ ] Không có thực thể thừa, không liên quan
- [ ] Đã ghi chú bộ ký hiệu (Chen cho Báo cáo 1)

---

## 3. MỐI KẾT HỢP (RELATIONSHIP)

| Tiêu chí | Yêu cầu |
|---|---|
| Đặt tên | Là **động từ** hoặc cụm động từ (đặt, chứa, thuộc, giao, …) |
| Quan hệ sở hữu | Thể hiện các quan hệ sở hữu: **là, gồm, của, thuộc**, … |
| Nghiệp vụ | Thể hiện các **nghiệp vụ ở thế giới thực** |
| Bản số | Các **bản số hợp lý** (1:1, 1:N, N:M) — phù hợp thực tế |

### Checklist nhanh — Mối kết hợp

- [ ] Tên quan hệ là động từ / cụm động từ
- [ ] Có quan hệ sở hữu (composition / thuộc về) khi cần
- [ ] Phản ánh nghiệp vụ thực tế (không chỉ liên kết kỹ thuật)
- [ ] Bản số (cardinality) hợp lý và nhất quán

---

## 4. THỰC THỂ YẾU (WEAK ENTITY)

| Tiêu chí | Yêu cầu |
|---|---|
| Thực thể mạnh | **Phải có** thực thể mạnh tương ứng |
| Mối kết hợp | **Phải có** mối kết hợp gắn với thực thể yếu (identifying relationship) |
| Phạm vi dùng | **Chỉ áp dụng** để mô hình hóa các nghiệp vụ / dữ liệu **phức tạp** |

### Gợi ý áp dụng đề tài Long Châu

Thực thể yếu **có thể** dùng khi mô hình hóa phụ thuộc mạnh, ví dụ:

- `OrderItem` phụ thuộc `Order` (dòng đơn không tồn tại độc lập)
- `GoodsReceiptLine` phụ thuộc `GoodsReceipt`
- `PurchaseOrderLine` phụ thuộc `PurchaseOrder`

Chỉ vẽ weak entity khi thực sự cần thiết — tránh lạm dụng.

### Checklist nhanh — Thực thể yếu

- [ ] Mỗi weak entity có strong entity cha
- [ ] Có identifying relationship (mũi tên đậm / diamond đậm — theo Chen)
- [ ] Chỉ dùng cho trường hợp phức tạp, có lý do rõ ràng

---

## 5. TÓM TẮT 4 NHÓM TIÊU CHÍ

```
1. Ký hiệu     → Chen, mức Concept (Báo cáo 1), ghi chú bộ ký hiệu
2. Thực thể    → Danh từ, có ID, đủ & không thừa
3. Mối kết hợp → Động từ, sở hữu, nghiệp vụ, bản số hợp lý
4. Thực thể yếu → Có strong entity + identifying relationship, dùng có chọn lọc
```

---

> **Tham chiếu ERD dự án**: `docs/ERD/chen-erd-core.png`, `docs/ERD/chen-erd.png`, `DATABASE_CONTEXT.md`.
