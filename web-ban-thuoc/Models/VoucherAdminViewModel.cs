namespace web_ban_thuoc.Models
{
    public class VoucherAdminViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal? PercentValue { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string UserId { get; set; } = string.Empty; // "Tất cả" hoặc userId cụ thể
        public int DaDung { get; set; }
        public int ConLai { get; set; }
        public bool IsActive { get; set; }
    }
} 