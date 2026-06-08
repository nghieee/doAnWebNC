using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace web_ban_thuoc.Services
{
    public static class EmailSenderFactory
    {
        public static IEmailSender CreateForEnvironment(IHostEnvironment env, IOptions<EmailSettings> options)
        {
            if (env.IsDevelopment())
                return new NullEmailSender();
            return new SmtpEmailSender(options!);
        }
    }
}
