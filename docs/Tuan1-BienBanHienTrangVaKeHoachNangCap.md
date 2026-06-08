# TUẦN 1 – BIÊN BẢN ĐÁNH GIÁ HIỆN TRẠNG VÀ KẾ HOẠCH NÂNG CẤP (7 TUẦN)

**Đề tài:** Website bán thuốc trực tuyến – Nhà thuốc Long Châu  
**Sinh viên:** Nguyễn Trung Hiếu – 22DH111077  
**Công nghệ:** ASP.NET Core MVC, SQL Server, Entity Framework Core, ASP.NET Core Identity, SignalR, PayOS  
**CSDL:** `LongChauDB_New` (tham chiếu ERD đã chèn vào file nộp bài)

---

## I. NHIỆM VỤ 1 – KIỂM TRA CSDL VÀ ERD HIỆN TẠI

### 1.1. Mô tả ERD hiện tại (tóm tắt)

Hệ thống sử dụng **mô hình quan hệ** với các nhóm bảng chính:

| Nhóm | Bảng |
|------|------|
| Xác thực | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, … (Identity) |
| Danh mục & sản phẩm | `Categories` (cha–con), `Products`, `ProductImages` |
| Đơn hàng | `Orders`, `OrderItems`, `Payments` |
| Khuyến mãi & hạng | `Vouchers`, `UserVouchers`, `UserRankInfos` |
| Tương tác | `Reviews`, `ChatMessages`, `Banners` |
| Tồn kho (dự phòng) | `InventoryTransactions` |

*Hình ERD: sinh viên đã xuất và chèn vào file nộp bài (SSMS / dbdiagram / Draw.io).*

### 1.2. Ít nhất 5 điểm bất hợp lý / cần cải thiện

Dưới đây là các vấn đề **đối chiếu migration + mã nguồn** (gồm di sản CSDL ban đầu và thiết kế hiện tại):

#### (1) Hai mô hình người dùng song song (đã từng tồn tại – nghiêm trọng)

Migration `InitialCreate` tạo đồng thời:

- Bảng tự quản lý `User` (`UserId` int, `Username`, `PasswordHash`, `Role`…)
- Bảng `AspNetUsers` của Identity

Bảng `Orders` có **hai khóa ngoại user**: `UserId` (string → AspNetUsers) và `UserId1` (int → bảng `User`).

**Hậu quả:** Trùng lặp dữ liệu người dùng, khó đồng bộ đơn hàng/đánh giá, rủi ro bảo mật (mật khẩu quản lý không theo chuẩn Identity).  
**Đã xử lý một phần:** Migration `UpdateReviewUserToIdentity` **xóa bảng `User`**, chuẩn hóa `Reviews.UserId` sang AspNetUsers.  
**Còn trong kế hoạch:** Rà soát dữ liệu cũ, đảm bảo mọi `Orders.UserId` trỏ đúng Identity, không còn cột thừa trong DB thực tế.

#### (2) Lưu trữ mật khẩu không an toàn ở phiên bản CSDL đầu (ví dụ đề bài)

Bảng `User` cũ có cột `PasswordHash` kiểu `nvarchar(max)` **không** gắn với cơ chế salt/băm PBKDF2 của Identity; thực tế dễ là hash đơn giản hoặc lưu gần plain-text tùy code cũ.

**Hiện trạng sau nâng cấp:** Dùng **ASP.NET Core Identity** (`AspNetUsers.PasswordHash` do framework quản lý).  
**Kế hoạch:** Không tái sử dụng bảng `User` cũ; bắt buộc xác nhận email (`EmailConfirmed`) trước khi đăng nhập.

#### (3) Trùng lặp trạng thái thanh toán (Order vs Payment)

- `Orders.PaymentStatus` (chuỗi tự do)
- `Payments.PaymentStatus`, `TransactionId`, `PaymentMethod`

**Bất hợp lý:** Cùng một nghiệp vụ thanh toán lưu **hai nơi**, dễ lệch (COD đã giao nhưng Payment chưa cập nhật, PayOS callback cập nhật một phía).  
**Hướng xử lý:** Payment là nguồn sự thật; Order chỉ giữ trạng thái đơn (`Status`) và có thể thêm trường tổng hợp đọc từ Payment hoặc bỏ `Order.PaymentStatus` sau migration.

#### (4) Bảng `InventoryTransactions` có trong ERD nhưng không dùng trong nghiệp vụ

Bảng đã tạo từ đầu (nhập/xuất kho) nhưng **không có** thao tác ghi trong `CartController`, `AdminOrderController`, v.v.

Trong khi đó `Products.StockQuantity` **không bị trừ** khi khách đặt hàng (chỉ lọc `StockQuantity > 0` ở trang chủ).

**Hậu quả:** Tồn kho trên CSDL không phản ánh đơn thực tế → overselling.

#### (5) Gộp giỏ hàng vào `Orders` với `Status = "Cart"`

Thiết kế dùng **một bảng Order** cho cả giỏ tạm và đơn chính thức.

**Ưu:** Ít bảng, code giỏ hàng đơn giản.  
**Nhược:** Khó báo cáo doanh thu (phải lọc `Status != 'Cart'` mọi nơi), dễ nhầm thống kê, khó audit lịch sử giỏ bỏ.

**Hướng cải thiện (tùy chọn):** Tách `Carts` / `CartItems` hoặc chuẩn hóa bảng `OrderStatus` lookup.

#### (6) Voucher liên kết danh mục bằng tên chuỗi (`CategoryName`) thay vì FK

`Vouchers.CategoryName` không ràng buộc `Categories.CategoryId` → đổi tên danh mục làm voucher sai phạm vi.

*(Có thể chọn 3–4 ý trên khi trình bày miệng; đề bài yêu cầu tối thiểu 3.)*

---

## II. NHIỆM VỤ 2 – KIỂM TRA CHỨC NĂNG

### 2.1. Khách hàng

| STT | Tính năng | Trạng thái | Ghi chú |
|-----|-----------|------------|---------|
| 1 | Đăng ký + xác nhận email | **Chạy được** | `AuthController.Register`, `ConfirmEmail` |
| 2 | Đăng nhập / đăng xuất | **Chạy được** | Phân nhánh Admin → `AdminHome` |
| 3 | Quên mật khẩu | **Chạy được** | Mã 6 số qua Session + email |
| 4 | Hồ sơ, đổi email/mật khẩu | **Chạy được** | Có bước xác thực mã |
| 5 | Duyệt danh mục đa cấp | **Chạy được** | `CategoriesController` |
| 6 | Tìm kiếm, lọc sản phẩm | **Chạy được** | `ProductController`, `HomeController` |
| 7 | Chi tiết sản phẩm, ảnh | **Chạy được** | `ProductImages` |
| 8 | Giỏ hàng (thêm/sửa/xóa) | **Chạy được** | Order `Status = Cart` |
| 9 | Áp dụng voucher | **Chạy được** | `CartController.ApplyVoucher` |
| 10 | Đặt hàng COD | **Chạy được** | Cập nhật trạng thái Chờ xác nhận |
| 11 | Thanh toán PayOS | **Chạy được / phụ thuộc cấu hình** | Cần key PayOS, callback đúng `BaseUrl` |
| 12 | Theo dõi đơn (Profile) | **Chạy được** | Lịch sử đơn trong Profile |
| 13 | Đánh giá sản phẩm | **Chạy được** | Chỉ khi đã mua, 1 review/user/sp |
| 14 | Chat hỗ trợ (SignalR) | **Chạy được / cần hoàn thiện** | Lưu DB OK; broadcast `Clients.All` – thiếu phòng riêng |
| 15 | Xếp hạng + voucher tự động | **Chạy được** | `UserRankService`, `MonthlyVoucherHostedService` |
| 16 | AI tư vấn sản phẩm | **Chưa ổn định** | Cần `Gemini:ApiKey` trong cấu hình |
| 17 | Email (dev) | **Chạy “giả”** | `Development` → `NullEmailSender` (không gửi thật) |

### 2.2. Quản trị viên

| STT | Tính năng | Trạng thái | Ghi chú |
|-----|-----------|------------|---------|
| 1 | Dashboard thống kê | **Chạy được** | `AdminHomeController` |
| 2 | CRUD danh mục | **Chạy được** | `[Authorize(Roles=Admin)]` |
| 3 | CRUD sản phẩm + ảnh | **Chạy được** | |
| 4 | Quản lý đơn hàng | **Chạy được / thiếu bảo mật** | `AdminOrderController` **không** có `[Authorize]` |
| 5 | Quản lý voucher | **Chạy được** | Tạo/xóa qua `AdminHome`; **chưa** sửa voucher, tắt/bật nhanh |
| 6 | Quản lý banner | **Chạy được** | |
| 7 | Quản lý user, khóa TK, Excel | **Chạy được** | Lockout Identity |
| 8 | Chat admin | **Chạy được** | Cùng hạn chế SignalR như trên |

### 2.3. Tính năng chạy sai hoặc rủi ro cao (ưu tiên sửa)

1. **Tồn kho không giảm khi đặt hàng** – logic nghiệp vụ sai so với field `StockQuantity`.
2. **`AdminOrderController` thiếu `[Authorize(Roles = "Admin")]`** – lộ chức năng admin nếu biết URL.
3. **Trùng `PaymentStatus` Order/Payment** – có thể hiển thị sai trạng thái thanh toán.
4. **Chat gửi `Clients.All`** – user có thể thấy tin nhắn người khác (thiếu group theo cuộc hội thoại).
5. **Gửi email production** – cần `ASPNETCORE_ENVIRONMENT=Production` + SMTP hợp lệ; dev không test được gửi thật.
6. **Mật khẩu admin seed cứng** trong `Program.cs` – cần đổi khi triển khai thật.

---

## III. NHIỆM VỤ 3 – KẾ HOẠCH NÂNG CẤP

### 3.1. CSDL – Bảng cần sửa / thêm

| Hạng mục | Thao tác | Mô tả |
|----------|----------|--------|
| `User` (legacy) | **Đã xóa** (migration) | Kiểm tra DB thực tế không còn bảng |
| `Orders` | **Sửa** | Chuẩn hóa `Status`, `PaymentStatus`; có thể bỏ `PaymentStatus` trên Order |
| `Payments` | **Sửa** | Bắt buộc khi PayOS; thêm index `OrderId`, `TransactionId` |
| `Vouchers` | **Sửa** | Thêm `CategoryId` (FK), bỏ hoặc map `CategoryName` |
| `InventoryTransactions` | **Tích hợp** hoặc **bỏ** | Ghi transaction khi đặt/hủy/ admin nhập kho |
| `Products` | **Sửa logic** | Trigger/service trừ `StockQuantity` |
| `OrderStatus` (lookup) | **Thêm (khuyến nghị)** | Thay chuỗi magic `"Cart"`, `"Chờ xác nhận"`… |
| `ChatConversations` | **Thêm (tùy chọn)** | `ConversationId`, participants – phục vụ SignalR đúng phạm vi |

### 3.2. Tính năng bắt buộc hoàn thiện trong 7 tuần

| Ưu tiên | Tính năng |
|---------|-----------|
| P0 | Bảo mật admin: `[Authorize]` đủ controller admin |
| P0 | Đồng bộ tồn kho + `InventoryTransactions` (hoặc bỏ bảng thừa) |
| P0 | Đồng bộ trạng thái thanh toán Order ↔ Payment ↔ PayOS webhook |
| P1 | Email xác nhận đơn / thanh toán ổn định (SMTP, User Secrets) |
| P1 | Chat theo phòng (user–admin), đánh dấu đã đọc |
| P1 | Hoàn thiện voucher: sửa, vô hiệu hóa (`IsActive`) |
| P2 | Chuẩn hóa voucher–category bằng FK |
| P2 | AI Bot cấu hình key, giới hạn prompt |
| P2 | Triển khai Production (HTTPS, biến môi trường, không hard-code secret) |

---

## IV. KẾ HOẠCH 7 TUẦN (SẢN PHẨM GIAO NỘP)

| Tuần | Nội dung chính | Sản phẩm / kết quả |
|------|----------------|-------------------|
| **1** | Khảo sát hiện trạng: ERD, danh sách lỗi CSDL & chức năng, kế hoạch tổng | **Biên bản này** + Hình ERD |
| **2** | Chuẩn hóa CSDL: migration FK voucher, index Payment; xóa cột thừa; tài liệu ràng buộc | Script migration + ERD cập nhật |
| **3** | Bảo mật & Identity: Authorize admin, secrets, khóa seed admin; test đăng ký/đăng nhập | Checklist bảo mật |
| **4** | Đơn hàng & tồn kho: trừ stock, ghi InventoryTransaction, hủy đơn hoàn kho | Test case đặt/hủy hàng |
| **5** | Thanh toán PayOS + email: đồng bộ Payment, webhook, gửi mail Production | Demo thanh toán + email |
| **6** | Chat SignalR + voucher admin (sửa/vô hiệu hóa) + hoàn thiện báo cáo chức năng | Demo chat + voucher |
| **7** | Kiểm thử tổng hợp, deploy, hoàn thiện báo cáo & slide vấn đáp | Bản build + biên bản test |

### Phân công kỹ thuật gợi ý theo tuần

- **Tuần 2–3:** EF Core migrations, `Program.cs`, filters `[Authorize]`.
- **Tuần 4:** `CartController`, `AdminOrderController`, service `InventoryService`.
- **Tuần 5:** `PayOSController`, `IOrderEmailService`, cấu hình `appsettings.Production.json`.
- **Tuần 6:** `ChatHub` (Groups), `AdminHome` voucher CRUD đầy đủ.
- **Tuần 7:** Regression test toàn luồng khách + admin.

---

## V. KẾT LUẬN TUẦN 1

Hệ thống **đã có nền tảng đầy đủ** (Identity, đơn hàng chi tiết `OrderItems`, Payment, Voucher, Rank, Chat, PayOS) nhưng còn **nợ kỹ thuật** từ giai đoạn CSDL đầu (hai bảng user, tồn kho chưa khóa nghiệp vụ, trạng thái thanh toán trùng) và **một số module chưa siết bảo mật / chưa hoàn thiện** (authorize admin order, chat broadcast, email môi trường dev).

Kế hoạch 7 tuần tập trung: **(1)** làm sạch & chuẩn hóa CSDL, **(2)** siết bảo mật, **(3)** hoàn thiện nghiệp vụ cốt lõi bán hàng (kho – thanh toán – email – chat), **(4)** triển khai và kiểm thử.

---

*Tài liệu biên soạn theo mã nguồn dự án `web-ban-thuoc` và lịch sử migration EF Core (tháng 07–08/2025).*
