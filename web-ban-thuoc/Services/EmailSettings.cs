namespace web_ban_thuoc.Services
{
    public class EmailSettings
    {
        /// <summary> true = gửi email thật (SmtpEmailSender), false = không gửi (NullEmailSender) </summary>
        public bool Enabled { get; set; } = true;
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
    }
} 