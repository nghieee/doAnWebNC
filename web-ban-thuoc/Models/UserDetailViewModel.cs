using System.ComponentModel.DataAnnotations;

namespace web_ban_thuoc.Models
{
    public class UserDetailViewModel
    {
        public string Id { get; set; } = string.Empty;
        
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; } = string.Empty;
        
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
        
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedDate { get; set; }
        
        [Display(Name = "Trạng thái")]
        public bool IsLocked { get; set; }
        
        public DateTime? LockoutEnd { get; set; }
        
        [Display(Name = "Xếp hạng")]
        public string Rank { get; set; } = string.Empty;
        
        [Display(Name = "Tổng chi tiêu")]
        public decimal TotalSpent { get; set; }
        
        [Display(Name = "Chi tiêu 6 tháng")]
        public decimal TotalSpent6Months { get; set; }
        
        [Display(Name = "Lần reset rank cuối")]
        public DateTime? LastRankReset { get; set; }
        
        [Display(Name = "Tổng đơn hàng")]
        public int TotalOrders { get; set; }
        
        [Display(Name = "Tổng chi tiêu tất cả thời gian")]
        public decimal TotalSpentAllTime { get; set; }
        
        public List<Order> Orders { get; set; } = new List<Order>();
    }
} 