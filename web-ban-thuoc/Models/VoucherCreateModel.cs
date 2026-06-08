namespace web_ban_thuoc.Models
{
    public class VoucherCreateModel
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = "FullOrder";
        public decimal? PercentValue { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? Detail { get; set; }
        public bool ForAllUsers { get; set; }
        public List<string>? Ranks { get; set; }
        public int? MaxUsage { get; set; } // Số lượt sử dụng tối đa (null = không giới hạn)
        public decimal? MinOrderAmount { get; set; }
        public string? RequiredRank { get; set; }
        public int? CategoryId { get; set; }
        public bool IsPublicCode { get; set; }
    }

    public class VoucherEditModel
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = "FullOrder";
        public decimal? PercentValue { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? Detail { get; set; }
        public int? MaxUsage { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public string? RequiredRank { get; set; }
        public int? CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
    }
} 