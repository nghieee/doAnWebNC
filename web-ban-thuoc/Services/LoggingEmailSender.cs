using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace web_ban_thuoc.Services
{
    /// <summary>
    /// Mẫu Decorator (C6.3): Bọc IEmailSender để thêm hành vi ghi log trước và sau khi gửi email.
    /// Giữ nguyên interface IEmailSender, không thay đổi client code.
    /// </summary>
    public class LoggingEmailSender : IEmailSender
    {
        private readonly IEmailSender _inner;
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(IEmailSender inner, ILogger<LoggingEmailSender> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            _logger.LogInformation("Đang gửi email đến {To}, chủ đề: {Subject}", toEmail, subject);
            await _inner.SendEmailAsync(toEmail, subject, htmlMessage);
            _logger.LogInformation("Đã gửi email thành công đến {To}", toEmail);
        }
    }
}
