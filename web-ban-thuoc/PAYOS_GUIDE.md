# Hướng dẫn sử dụng PayOS

## Tổng quan
PayOS là cổng thanh toán trực tuyến cho phép khách hàng thanh toán đơn hàng thông qua nhiều phương thức khác nhau như thẻ ATM, thẻ quốc tế, ví điện tử, v.v.

## Cấu hình

### 1. Thông tin cấu hình trong appsettings.json
```json
{
  "PayOS": {
    "ClientId": "adbf3b7a-17bd-4867-974e-6f1ddcc9ad6e",
    "ApiKey": "cf4608e4-eb83-4e25-8619-8d9b57a16314",
    "ChecksumKey": "a94f6207b4e5d2311009fe88ff58a3aaa6a547795696cf1450e3003a552085b7",
    "BaseUrl": "https://api-merchant.payos.vn"
  }
}
```

### 2. Các thông tin cần thiết
- **ClientId**: ID khách hàng từ PayOS
- **ApiKey**: Khóa API từ PayOS
- **ChecksumKey**: Khóa bảo mật để tạo chữ ký
- **BaseUrl**: URL API của PayOS

## Luồng thanh toán

### 1. Khách hàng chọn thanh toán PayOS
- Khách hàng vào giỏ hàng
- Chọn phương thức thanh toán "PayOS"
- Nhập thông tin giao hàng
- Nhấn "Đặt hàng"

### 2. Tạo thanh toán
- Hệ thống tạo đơn hàng trong database
- Gọi API PayOS để tạo thanh toán
- Tạo chữ ký bảo mật theo định dạng: `orderCode|amount|description|cancelUrl|returnUrl`
- Chuyển hướng khách hàng đến trang thanh toán PayOS

### 3. Xử lý kết quả thanh toán
- **Thành công**: Khách hàng được chuyển về trang Success
- **Thất bại**: Khách hàng được chuyển về trang Failed
- **Hủy**: Khách hàng được chuyển về trang Cancel

### 4. Webhook
- PayOS gửi webhook về server để cập nhật trạng thái thanh toán
- Hệ thống xác thực chữ ký webhook
- Cập nhật trạng thái đơn hàng và gửi email thông báo

## Các file chính

### 1. PayOSService.cs
- Xử lý tương tác với API PayOS
- Tạo và xác thực chữ ký
- Gọi API tạo thanh toán và kiểm tra trạng thái

### 2. PayOSController.cs
- Xử lý các request từ frontend
- Hiển thị các trang thanh toán
- Xử lý webhook từ PayOS

### 3. PayOSModels.cs
- Định nghĩa các model cho request/response PayOS
- Model cho webhook và trạng thái thanh toán

## Cách tạo chữ ký

### 1. Chữ ký cho request tạo thanh toán
```csharp
var dataToSign = $"{orderCode}|{amount}|{description}|{cancelUrl}|{returnUrl}";
var signature = GenerateSignature(dataToSign);
```

### 2. Chữ ký cho webhook
```csharp
var dataToVerify = $"{orderCode}|{amount}|{description}|{transactionTime}|{status}|{message}";
var isValid = VerifyWebhookSignature(signature, dataToVerify);
```

## Các endpoint

### 1. Tạo thanh toán
- **GET** `/PayOS/CreatePayment?orderId={orderId}`: Hiển thị trang tạo thanh toán
- **POST** `/PayOS/CreatePayment`: Tạo thanh toán và chuyển hướng đến PayOS

### 2. Xử lý kết quả
- **GET** `/PayOS/Return?orderId={orderId}&orderCode={orderCode}`: Xử lý khi thanh toán thành công
- **GET** `/PayOS/Cancel?orderId={orderId}`: Xử lý khi khách hàng hủy thanh toán
- **GET** `/PayOS/Success?orderId={orderId}`: Hiển thị trang thành công
- **GET** `/PayOS/Failed?orderId={orderId}`: Hiển thị trang thất bại

### 3. Webhook
- **POST** `/PayOS/Webhook`: Nhận webhook từ PayOS

### 4. Test endpoints
- **GET** `/PayOS/Test`: Kiểm tra cấu hình PayOS
- **GET** `/PayOS/TestPayOS`: Test API PayOS
- **GET** `/PayOS/TestPayOSRaw`: Test API PayOS với response raw

## Xử lý lỗi

### 1. Lỗi 201 - Chữ ký không khớp
- Kiểm tra ChecksumKey trong appsettings.json
- Đảm bảo định dạng chuỗi dữ liệu để tạo chữ ký đúng
- Kiểm tra encoding UTF-8

### 2. Lỗi 401 - Unauthorized
- Kiểm tra ClientId và ApiKey
- Đảm bảo headers được gửi đúng

### 3. Lỗi 400 - Bad Request
- Kiểm tra dữ liệu request
- Đảm bảo các trường bắt buộc được điền đầy đủ

## Debug và Logging

### 1. Bật logging chi tiết
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "web_ban_thuoc.Services.PayOSService": "Debug"
    }
  }
}
```

### 2. Kiểm tra logs
- Xem logs trong console hoặc file log
- Kiểm tra các thông tin debug được log ra

## Bảo mật

### 1. Bảo vệ webhook
- Luôn xác thực chữ ký webhook
- Kiểm tra IP của PayOS (nếu có thể)
- Sử dụng HTTPS

### 2. Bảo vệ thông tin khách hàng
- Không log thông tin nhạy cảm
- Mã hóa dữ liệu khi cần thiết
- Tuân thủ quy định bảo mật

## Testing

### 1. Test với sandbox
- Sử dụng tài khoản sandbox của PayOS
- Test các trường hợp thành công và thất bại
- Test webhook

### 2. Test production
- Đảm bảo cấu hình đúng
- Test với số tiền nhỏ trước
- Monitor logs và errors

## Troubleshooting

### 1. Thanh toán không thành công
- Kiểm tra logs để tìm lỗi
- Kiểm tra cấu hình PayOS
- Kiểm tra network connectivity

### 2. Webhook không nhận được
- Kiểm tra URL webhook trong PayOS dashboard
- Kiểm tra firewall và network
- Test webhook endpoint

### 3. Chữ ký không khớp
- Kiểm tra ChecksumKey
- Kiểm tra định dạng chuỗi dữ liệu
- Kiểm tra encoding

## Liên hệ hỗ trợ
- PayOS Support: support@payos.vn
- Documentation: https://docs.payos.vn
- API Reference: https://docs.payos.vn/api 