using System.Threading.Tasks;

namespace web_ban_thuoc.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
} 