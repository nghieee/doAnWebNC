using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services
{
    public interface IOrderEmailService
    {
        Task SendOrderConfirmationEmailAsync(OrderConfirmationEmail orderData);
        Task SendPaymentSuccessEmailAsync(OrderConfirmationEmail orderData);
        Task SendPaymentFailedEmailAsync(OrderConfirmationEmail orderData);
    }

    public class OrderEmailService : IOrderEmailService
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<OrderEmailService> _logger;

        public OrderEmailService(IEmailSender emailSender, ILogger<OrderEmailService> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task SendOrderConfirmationEmailAsync(OrderConfirmationEmail orderData)
        {
            try
            {
                var subject = $"Xác nhận đơn hàng #{orderData.OrderCode} - Nhà Thuốc Long Châu";
                var htmlBody = GenerateOrderConfirmationEmailHtml(orderData);

                await _emailSender.SendEmailAsync(orderData.CustomerEmail, subject, htmlBody);
                _logger.LogInformation($"Order confirmation email sent to {orderData.CustomerEmail} for order {orderData.OrderCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending order confirmation email for order {orderData.OrderCode}");
            }
        }

        public async Task SendPaymentSuccessEmailAsync(OrderConfirmationEmail orderData)
        {
            try
            {
                var subject = $"Thanh toán thành công - Đơn hàng #{orderData.OrderCode}";
                var htmlBody = GeneratePaymentSuccessEmailHtml(orderData);

                await _emailSender.SendEmailAsync(orderData.CustomerEmail, subject, htmlBody);
                _logger.LogInformation($"Payment success email sent to {orderData.CustomerEmail} for order {orderData.OrderCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending payment success email for order {orderData.OrderCode}");
            }
        }

        public async Task SendPaymentFailedEmailAsync(OrderConfirmationEmail orderData)
        {
            try
            {
                var subject = $"Thanh toán thất bại - Đơn hàng #{orderData.OrderCode}";
                var htmlBody = GeneratePaymentFailedEmailHtml(orderData);

                await _emailSender.SendEmailAsync(orderData.CustomerEmail, subject, htmlBody);
                _logger.LogInformation($"Payment failed email sent to {orderData.CustomerEmail} for order {orderData.OrderCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending payment failed email for order {orderData.OrderCode}");
            }
        }

        private string GenerateOrderConfirmationEmailHtml(OrderConfirmationEmail orderData)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Xác nhận đơn hàng</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f8f9fa; }}
        .order-info {{ background: white; padding: 20px; margin: 20px 0; border-radius: 5px; }}
        .item {{ border-bottom: 1px solid #eee; padding: 10px 0; }}
        .total {{ font-weight: bold; font-size: 18px; color: #007bff; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Đơn hàng đã được xác nhận!</h1>
            <p>Nhà Thuốc Long Châu</p>
        </div>
        
        <div class='content'>
            <h2>Cảm ơn bạn đã đặt hàng!</h2>
            <p>Chúng tôi đã nhận được đơn hàng của bạn và sẽ xử lý trong thời gian sớm nhất.</p>
            
            <div class='order-info'>
                <h3>📋 Thông tin đơn hàng</h3>
                <p><strong>Mã đơn hàng:</strong> #{orderData.OrderCode}</p>
                <p><strong>Ngày đặt:</strong> {orderData.OrderDate:dd/MM/yyyy HH:mm}</p>
                <p><strong>Phương thức thanh toán:</strong> {orderData.PaymentMethod}</p>
                
                <h4>👤 Thông tin khách hàng</h4>
                <p><strong>Họ tên:</strong> {orderData.CustomerName}</p>
                <p><strong>Email:</strong> {orderData.CustomerEmail}</p>
                <p><strong>Số điện thoại:</strong> {orderData.CustomerPhone}</p>
                <p><strong>Địa chỉ:</strong> {orderData.ShippingAddress}</p>
                
                <h4>📦 Chi tiết sản phẩm</h4>
                {GenerateOrderItemsHtml(orderData.OrderItems)}
                
                <div class='total'>
                    <p><strong>Tổng cộng: {orderData.TotalAmount:N0} VNĐ</strong></p>
                    {(orderData.VoucherDiscount > 0 ? $"<p><em>Giảm giá ({orderData.VoucherCode}): -{orderData.VoucherDiscount:N0} VNĐ</em></p>" : "")}
                </div>
            </div>
            
            <p>Chúng tôi sẽ liên hệ với bạn sớm nhất để xác nhận và giao hàng.</p>
        </div>
        
        <div class='footer'>
            <p>© 2024 Nhà Thuốc Long Châu. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GeneratePaymentSuccessEmailHtml(OrderConfirmationEmail orderData)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Thanh toán thành công</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f8f9fa; }}
        .success-message {{ background: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .order-info {{ background: white; padding: 20px; margin: 20px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Thanh toán thành công!</h1>
            <p>Nhà Thuốc Long Châu</p>
        </div>
        
        <div class='content'>
            <div class='success-message'>
                <h3>🎉 Chúc mừng! Thanh toán của bạn đã thành công.</h3>
                <p>Đơn hàng #{orderData.OrderCode} đã được thanh toán thành công và đang được xử lý.</p>
            </div>
            
            <div class='order-info'>
                <h3>📋 Thông tin giao dịch</h3>
                <p><strong>Mã đơn hàng:</strong> #{orderData.OrderCode}</p>
                <p><strong>Ngày thanh toán:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                <p><strong>Số tiền:</strong> {orderData.TotalAmount:N0} VNĐ</p>
                <p><strong>Phương thức:</strong> {orderData.PaymentMethod}</p>
            </div>
            
            <p>Chúng tôi sẽ giao hàng đến địa chỉ bạn đã cung cấp trong thời gian sớm nhất.</p>
            <p>Bạn sẽ nhận được thông báo khi hàng được giao.</p>
        </div>
        
        <div class='footer'>
            <p>© 2024 Nhà Thuốc Long Châu. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GeneratePaymentFailedEmailHtml(OrderConfirmationEmail orderData)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Thanh toán thất bại</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f8f9fa; }}
        .error-message {{ background: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .order-info {{ background: white; padding: 20px; margin: 20px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Thanh toán thất bại</h1>
            <p>Nhà Thuốc Long Châu</p>
        </div>
        
        <div class='content'>
            <div class='error-message'>
                <h3>Rất tiếc! Thanh toán của bạn không thành công.</h3>
                <p>Đơn hàng #{orderData.OrderCode} chưa được thanh toán.</p>
            </div>
            
            <div class='order-info'>
                <h3>📋 Thông tin đơn hàng</h3>
                <p><strong>Mã đơn hàng:</strong> #{orderData.OrderCode}</p>
                <p><strong>Số tiền:</strong> {orderData.TotalAmount:N0} VNĐ</p>
                <p><strong>Phương thức:</strong> {orderData.PaymentMethod}</p>
            </div>
            
            <h4>🔄 Các bước tiếp theo:</h4>
            <ul>
                <li>Kiểm tra lại thông tin thẻ hoặc tài khoản</li>
                <li>Thử thanh toán lại với phương thức khác</li>
                <li>Liên hệ hỗ trợ nếu cần thiết</li>
            </ul>
            
            <p>Bạn có thể đăng nhập vào tài khoản để thử thanh toán lại.</p>
        </div>
        
        <div class='footer'>
            <p>© 2024 Nhà Thuốc Long Châu. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateOrderItemsHtml(List<OrderItem> items)
        {
            var html = "";
            foreach (var item in items)
            {
                html += $@"
                <div class='item'>
                    <p><strong>{item.Product?.ProductName}</strong></p>
                    <p>Số lượng: {item.Quantity} x {item.Price:N0} VNĐ</p>
                    <p>Thành tiền: {(item.Price * item.Quantity):N0} VNĐ</p>
                </div>";
            }
            return html;
        }

        private string GenerateOrderConfirmationEmailText(OrderConfirmationEmail orderData)
        {
            return $@"
XÁC NHẬN ĐƠN HÀNG - NHÀ THUỐC LONG CHÂU

Cảm ơn bạn đã đặt hàng!

Thông tin đơn hàng:
- Mã đơn hàng: #{orderData.OrderCode}
- Ngày đặt: {orderData.OrderDate:dd/MM/yyyy HH:mm}
- Phương thức thanh toán: {orderData.PaymentMethod}

Thông tin khách hàng:
- Họ tên: {orderData.CustomerName}
- Email: {orderData.CustomerEmail}
- Số điện thoại: {orderData.CustomerPhone}
- Địa chỉ: {orderData.ShippingAddress}

Chi tiết sản phẩm:
{GenerateOrderItemsText(orderData.OrderItems)}

Tổng cộng: {orderData.TotalAmount:N0} VNĐ
{(orderData.VoucherDiscount > 0 ? $"Giảm giá ({orderData.VoucherCode}): -{orderData.VoucherDiscount:N0} VNĐ" : "")}

Chúng tôi sẽ liên hệ với bạn sớm nhất để xác nhận và giao hàng.

© 2024 Nhà Thuốc Long Châu";
        }

        private string GeneratePaymentSuccessEmailText(OrderConfirmationEmail orderData)
        {
            return $@"
THANH TOÁN THÀNH CÔNG - NHÀ THUỐC LONG CHÂU

Chúc mừng! Thanh toán của bạn đã thành công.

Thông tin giao dịch:
- Mã đơn hàng: #{orderData.OrderCode}
- Ngày thanh toán: {DateTime.Now:dd/MM/yyyy HH:mm}
- Số tiền: {orderData.TotalAmount:N0} VNĐ
- Phương thức: {orderData.PaymentMethod}

Chúng tôi sẽ giao hàng đến địa chỉ bạn đã cung cấp trong thời gian sớm nhất.

© 2024 Nhà Thuốc Long Châu";
        }

        private string GeneratePaymentFailedEmailText(OrderConfirmationEmail orderData)
        {
            return $@"
THANH TOÁN THẤT BẠI - NHÀ THUỐC LONG CHÂU

Rất tiếc! Thanh toán của bạn không thành công.

Thông tin đơn hàng:
- Mã đơn hàng: #{orderData.OrderCode}
- Số tiền: {orderData.TotalAmount:N0} VNĐ
- Phương thức: {orderData.PaymentMethod}

Các bước tiếp theo:
1. Kiểm tra lại thông tin thẻ hoặc tài khoản
2. Thử thanh toán lại với phương thức khác
3. Liên hệ hỗ trợ nếu cần thiết

Bạn có thể đăng nhập vào tài khoản để thử thanh toán lại.

© 2024 Nhà Thuốc Long Châu";
        }

        private string GenerateOrderItemsText(List<OrderItem> items)
        {
            var text = "";
            foreach (var item in items)
            {
                text += $"- {item.Product?.ProductName}: {item.Quantity} x {item.Price:N0} VNĐ = {(item.Price * item.Quantity):N0} VNĐ\n";
            }
            return text;
        }
    }
} 