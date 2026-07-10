# LOGIC ĐỀ TÀI — CÁC HỆ QUẢN TRỊ CƠ SỞ DỮ LIỆU

> **Mục đích**: Ánh xạ giữa sơ đồ phân tích thiết kế ↔ mục báo cáo ↔ phần cài đặt CSDL. Dùng làm khung logic khi viết báo cáo môn CHQTCSDL.
> **Cập nhật**: 2026-06-17
> **Liên quan**: [DATABASE_CONTEXT.md](../DATABASE_CONTEXT.md) · [BUSINESS_CONTEXT.md](../BUSINESS_CONTEXT.md)

---

## 1. ÁNH XẠ SƠ ĐỒ → MỤC BÁO CÁO → CÀI ĐẶT

Luồng logic từ phân tích đến triển khai:

```
Organization Chart
       ↓
      BFD
       ↓
      DFD
       ↓
Business Rules
       ↓
      ERD
       ↓
Oracle Tables
       ↓
Procedure / Trigger
       ↓
  Transaction
       ↓
   User / Role
```

| Các sơ đồ | Mô tả tại phần nào | Các phần cài đặt liên quan |
|---|---|---|
| **Organization Chart** | Mục 1.1.2 | Mục 4.3.5 (Mô hình phân quyền — Authorization model) |
| **BFD** (Business Function Diagram) | Mục 3.1.1 | Mục 4.3.2 (Procedure) |
| **DFD** (Data Flow Diagram) | Mục 3.2.2 | Mục 4.3.3 (Transaction) |
| **Business Rules** | Mục 3.1.3 (Business Rule) | Trigger và Constraint |
| **ERD** (Entity Relationship Diagram) | Mục 3.2.1 | Mục 4.1 |
| **Oracle Tables** | Mục 4.1, Mục 4.2 | — |
| **Procedure / Trigger** | Mục 4.3.2 | — |
| **Transaction** | Mục 4.3.3 | — |
| **User / Role** | Mục 4.3.5 | — |

> **Lưu ý**: Trong file hướng dẫn môn học thiếu mục **3.1.3 Business Rule** — cần bổ sung khi viết báo cáo.

---

## 2. MỤC ĐÍCH TỪNG SƠ ĐỒ

| Sơ đồ | Trả lời câu hỏi | Dùng cho chương / thành phần |
|---|---|---|
| **Organization Chart** | Ai tham gia hệ thống? | User, Role, Privilege |
| **BFD** | Hệ thống làm gì? | Procedure, Function |
| **DFD** | Dữ liệu di chuyển thế nào? | Transaction |
| **Business Rules** | Ràng buộc gì tồn tại? | Trigger, Constraint |
| **ERD** | Lưu dữ liệu gì? | Table Design |

---

## 3. GIẢI THÍCH NGẮN GỌN LUỒNG LOGIC

### 3.1 Organization Chart → User / Role

- Xác định **ai** tương tác với hệ thống (Admin, NV kho, CSKH, Khách hàng, NCC…).
- Dẫn tới thiết kế **User, Role, Privilege** ở mục 4.3.5.

### 3.2 BFD → Procedure

- Liệt kê **chức năng nghiệp vụ** cấp cao (bán hàng, quản lý kho, báo cáo…).
- Các nghiệp vụ phức tạp được triển khai bằng **Stored Procedure** ở mục 4.3.2.

### 3.3 DFD → Transaction

- Mô tả **luồng dữ liệu** giữa process, kho dữ liệu và thực thể bên ngoài.
- Dẫn tới thiết kế **Transaction** (mục 4.3.3) — đảm bảo tính toàn vẹn khi cập nhật nhiều bảng.

### 3.4 Business Rules → Trigger & Constraint

- Ghi nhận **ràng buộc nghiệp vụ** (trạng thái đơn, tồn kho không âm, HSD lô hàng…).
- Triển khai bằng **Trigger** và **Constraint** (CHECK, FK, UNIQUE…).

### 3.5 ERD → Table Design (4.1, 4.2)

- Mô hình hóa **thực thể và quan hệ** — trả lời “lưu dữ liệu gì”.
- Chuyển thành **Oracle Tables** (mục 4.1 thiết kế, 4.2 chi tiết bảng).

---

## 4. GỢI Ý ÁP DỤNG CHO ĐỀ TÀI LONG CHÂU

| Sơ đồ / Phần cài đặt | Tham chiếu dự án |
|---|---|
| Organization Chart | `BUSINESS_CONTEXT.md` mục 2 — Actors, RBAC |
| BFD | `BUSINESS_CONTEXT.md` mục 3 — Phân hệ chức năng |
| DFD | Luồng đơn hàng, nhập kho, thanh toán — `BUSINESS_CONTEXT.md` mục 4, 5 |
| Business Rules | Trạng thái đơn, FEFO, voucher, loyalty — `BUSINESS_CONTEXT.md` mục 4–7 |
| ERD | `docs/ERD/` + `DATABASE_CONTEXT.md` mục 4 |
| Oracle Tables | Map từ 29 DbSet EF Core → Oracle DDL |
| Procedure | Xuất kho FEFO, chuyển trạng thái đơn, tính COGS |
| Trigger | Kiểm tra tồn kho, audit OrderStatusHistory, validate HSD |
| Transaction | Checkout, nhập kho GoodsReceipt, hủy đơn hoàn kho |
| User / Role | Admin, WarehouseStaff, CustomerSupport — `StaffRoles.cs` |

---

> **Cách dùng**: Khi viết từng chương báo cáo CHQTCSDL, đối chiếu bảng mục 1 để biết sơ đồ đặt ở đâu và cài đặt tương ứng ở đâu.
