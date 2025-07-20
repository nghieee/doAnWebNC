namespace web_ban_thuoc.Models
{
    public class ProfileViewModel
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? NewEmail { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? VerificationCode { get; set; }
        public bool RequireVerification { get; set; } = false;
        public string? VerificationType { get; set; } // "email" hoặc "password"
        public string? CurrentPassword { get; set; }
    }
} 