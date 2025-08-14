# Web Bán Thuốc – Hướng dẫn chạy nhanh bằng Docker

Dự án ASP.NET Core MVC + SQL Server, đã đóng gói sẵn bằng Docker Compose để demo mà không cần cài SQL Server.

## Yêu cầu
- Docker Desktop (Windows/macOS) hoặc Docker Engine (Linux)
- Git

## Cách chạy
1) Clone mã nguồn
```
2) Khởi chạy (lần đầu sẽ build image và seed database)
- Windows PowerShell:
```
docker-compose down -v
docker-compose up -d --build
```
- macOS/Linux:
```
docker compose down -v
docker compose up -d --build
```
3) Truy cập website: `http://localhost:5000`

Database sẽ tự động được tạo và import dữ liệu mẫu từ `docker/sql/LongChauDB.sql`.

## Kết nối SQL Server bằng SSMS (tùy chọn)
- Server: `localhost,14330`
- Authentication: SQL Server Authentication
- Login: `sa`
- Password: `MyStrongPassword123!`
- Tích "Trust server certificate" nếu cần

## Cấu hình chính
- Web: `http://localhost:5000`
- SQL Server (container → host): `localhost:14330`
- Ứng dụng web kết nối SQL Server nội bộ qua hostname `db` trong mạng docker (không phụ thuộc port host)
