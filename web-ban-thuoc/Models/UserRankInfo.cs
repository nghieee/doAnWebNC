using System;
using System.ComponentModel.DataAnnotations;

namespace web_ban_thuoc.Models
{
    public class UserRankInfo
    {
        [Key]
        public string UserId { get; set; } = string.Empty; // liên kết với AspNetUsers.Id
        public decimal TotalSpent { get; set; } = 0;
        public decimal TotalSpent6Months { get; set; } = 0;
        public string Rank { get; set; } = "Bạc"; // Bạc, Vàng, Bạch kim
        public DateTime? LastRankMailSent { get; set; }
        public DateTime? LastNotiMailSent { get; set; }
        public DateTime? LastRankReset { get; set; }
    }
} 