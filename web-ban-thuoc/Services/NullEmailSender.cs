using System.Threading.Tasks;

namespace web_ban_thuoc.Services
{
    public class NullEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            // Không làm gì - trả về ngay
            return Task.CompletedTask;
        }
    }
}
