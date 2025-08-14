# Tổng hợp chức năng và công nghệ sử dụng

## Chức năng chính
- Auth: Đăng ký/đăng nhập/đăng xuất, quên mật khẩu, hồ sơ cá nhân (ASP.NET Core Identity)
- Danh mục nhiều cấp, lọc và tìm kiếm sản phẩm
- Trang chi tiết sản phẩm, hình ảnh sản phẩm
- Giỏ hàng, đặt hàng, theo dõi trạng thái đơn hàng
- Thanh toán (tích hợp service PayOS trong mã nguồn)
- Đánh giá sản phẩm (sau mua)
- Xếp hạng khách hàng, thống kê chi tiêu 6 tháng
- Voucher: theo %, số tiền, theo danh mục/toàn bộ, giới hạn lượt dùng, cấp cho user
- Banner: CRUD, phân loại (Main/FullWidth/Side), sắp xếp, bulk delete
- Chat realtime khách hàng ↔ admin (SignalR)
- Thông báo Admin (ViewComponent)
- Trang Admin: Users, Products, Orders, Vouchers, Banners, thống kê

## Công nghệ
- Backend: ASP.NET Core 8.0 (MVC, Controllers, Razor Views)
- ORM: Entity Framework Core
- Identity & Security: ASP.NET Core Identity, Role-based Authorization, CSRF, password hashing
- Realtime: SignalR
- Email: SMTP (qua `SmtpEmailSender`), `OrderEmailService`
- DB: SQL Server
- UI: Bootstrap 5, jQuery, Font Awesome, custom CSS
- Đóng gói: Docker, Docker Compose

## Cấu trúc dự án (rút gọn)
- `web-ban-thuoc/Controllers/*`
- `web-ban-thuoc/Models/*` (Entities, ViewModels)
- `web-ban-thuoc/Views/*` (Razor)
- `web-ban-thuoc/Services/*` (Email, Rank, PayOS, ...)
- `web-ban-thuoc/Migrations/*` (EF Core)
- `wwwroot/*` (static assets)

## Docker & Kết nối
- Ứng dụng web: `http://localhost:5000`
- SQL Server (container → host): `localhost:14330`
- App kết nối DB nội bộ qua hostname `db` trong mạng docker
- Seed DB tự động từ `docker/sql/LongChauDB.sql` lần đầu chạy

## Tài khoản mẫu
- Admin: `admin@gmail.com` (mật khẩu đã hash trong DB seed)

Ghi chú: Với thanh toán PayOS cần cấu hình key thật để demo end‑to‑end.
