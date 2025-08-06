# Hướng dẫn Debug PayOS

## Vấn đề hiện tại
- Thanh toán PayOS không tạo được QR code
- Hiển thị lỗi "Có lỗi xảy ra khi tạo thanh toán"

## Các bước debug

### 1. Kiểm tra cấu hình
Truy cập: `https://localhost:5226/PayOS/Test`
- Kiểm tra ClientId, ApiKey, ChecksumKey
- Kiểm tra signature generation

### 2. Kiểm tra kết nối
Truy cập: `https://localhost:5226/PayOS/TestConnection`
- Kiểm tra kết nối đến PayOS API

### 3. Test Sandbox
Truy cập: `https://localhost:5226/PayOS/TestSandbox`
- Test với sandbox environment

### 4. Test Raw API
Truy cập: `https://localhost:5226/PayOS/TestPayOSRaw`
- Xem chi tiết request/response

## Các nút test trên trang thanh toán

1. **Test Config**: Kiểm tra cấu hình
2. **Test Raw API**: Test API trực tiếp
3. **Test Connection**: Kiểm tra kết nối
4. **Test Sandbox**: Test với sandbox

## Logs cần kiểm tra

Kiểm tra logs trong console để xem:
- Request payload
- Response từ PayOS
- Error messages

## Cấu hình hiện tại

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

## Troubleshooting

1. **Lỗi 401**: Kiểm tra ClientId và ApiKey
2. **Lỗi 400**: Kiểm tra signature và request format
3. **Lỗi kết nối**: Kiểm tra network và firewall
4. **Lỗi CORS**: Kiểm tra domain configuration

## Test với Postman

```bash
POST https://api-merchant.payos.vn/v2/payment-requests
Headers:
  x-client-id: adbf3b7a-17bd-4867-974e-6f1ddcc9ad6e
  x-api-key: cf4608e4-eb83-4e25-8619-8d9b57a16314
  Content-Type: application/json

Body:
{
  "orderCode": "TEST_20250101120000",
  "amount": 10000,
  "description": "Test payment",
  "cancelUrl": "https://localhost:5226/PayOS/Cancel?orderId=1",
  "returnUrl": "https://localhost:5226/PayOS/Return?orderId=1",
  "signature": "generated_signature_here"
}
``` 