# Hướng dẫn cài đặt — Nhà Thuốc Long Châu

## Yêu cầu

- .NET 8 SDK
- SQL Server (LocalDB hoặc SQL Express)
- VS Code / Visual Studio

## Cách setup lần đầu

### 1. Clone repo

```bash
git clone <repo-url>
cd doAnWebNC
```

### 2. Tạo file cấu hình

Project **đã ignore** các file `appsettings*.json` để không lộ secrets. Bạn cần tạo 2 file:

**File 1: `web-ban-thuoc/appsettings.json`**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS;Database=LongChauDB_New;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "your-email@gmail.com",
    "SmtpPass": "YOUR_GMAIL_APP_PASSWORD",
    "SenderEmail": "your-email@gmail.com",
    "SenderName": "Nhà Thuốc Long Châu"
  },
  "AllowedHosts": "*",
  "AppSettings": {
    "BaseUrl": "https://localhost:5226"
  },
  "PayOS": {
    "ClientId": "YOUR_PAYOS_CLIENT_ID",
    "ApiKey": "YOUR_PAYOS_API_KEY",
    "ChecksumKey": "YOUR_PAYOS_CHECKSUM_KEY",
    "BaseUrl": "https://api-merchant.payos.vn"
  }
}
```

**File 2: `web-ban-thuoc/appsettings.Development.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY"
  },
  "AppSettings": {
    "BaseUrl": "https://localhost:5226"
  },
  "PayOS": {
    "ClientId": "YOUR_PAYOS_CLIENT_ID",
    "ApiKey": "YOUR_PAYOS_API_KEY",
    "ChecksumKey": "YOUR_PAYOS_CHECKSUM_KEY",
    "BaseUrl": "https://api-merchant.payos.vn"
  }
}
```

### 3. Lấy các key cần thiết

| Service | Cách lấy | Ghi chú |
|---|---|---|
| **SQL Server** | Dùng `localhost\SQLEXPRESS` nếu cài SQL Express, hoặc tạo database mới | |
| **Gmail SMTP** | [App Passwords](https://myaccount.google.com/apppasswords) | Cần bật 2FA trước |
| **PayOS** | [payos.vn](https://payos.vn) → Tạo tài khoản merchant | Dùng Sandbox để test |
| **Gemini API** | [Google AI Studio](https://aistudio.google.com/) | Miễn phí, có free tier |

### 4. Chạy migration & khởi động

```bash
cd web-ban-thuoc
dotnet ef database update
dotnet run
```

Mở trình duyệt: `https://localhost:5226`

### 5. Tạo tài khoản admin đầu tiên

Truy cập `/register` để đăng ký, sau đó vào database chạy SQL:

```sql
UPDATE AspNetUsers SET Role = 'Admin' WHERE Email = 'your-email@example.com';
```

## Bảo mật

- **KHÔNG BAO GIỜ** commit file `appsettings*.json` lên git
- Các secrets nên lưu trong biến môi trường hoặc secret manager ở môi trường production
- Đã ignore đầy đủ: `appsettings.json`, `appsettings.Development.json`, `.env`, `.env.*`
