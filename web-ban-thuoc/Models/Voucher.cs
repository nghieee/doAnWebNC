using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace web_ban_thuoc.Models
{
    /// <summary>
    /// Định nghĩa voucher / chiến dịch khuyến mãi.
    /// Mã dùng chung: IsPublic = true, phát hành qua bảng Vouchers (không bắt buộc UserVoucher trước khi dùng).
    /// Mã gán user: IsPublic = false, user phải có bản ghi UserVouchers.
    /// </summary>
    public class Voucher
    {
        [Key]
        public int VoucherId { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime ExpiryDate { get; set; }

        public decimal? DiscountAmount { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// true = mã dùng chung (mọi user nhập mã, giới hạn bởi MaxUsage + theo dõi từng user qua UserVouchers khi đã dùng).
        /// </summary>
        public bool IsPublic { get; set; }

        public string DiscountType { get; set; } = "FullOrder";

        public decimal? PercentValue { get; set; }

        public int? CategoryId { get; set; }

        public string? CategoryName { get; set; }

        /// <summary>Giá trị đơn tối thiểu để áp dụng (VNĐ).</summary>
        public decimal? MinOrderAmount { get; set; }

        /// <summary>Hạng thành viên tối thiểu: Bạc, Vàng, Bạch kim (null = không yêu cầu).</summary>
        public string? RequiredRank { get; set; }

        public string? Detail { get; set; }

        /// <summary>
        /// Tổng lượt dùng tối đa của cả chiến dịch (null = không giới hạn).
        /// </summary>
        public int? MaxUsage { get; set; }

        public int UsedCount { get; set; } = 0;

        public virtual ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();

        public virtual ICollection<VoucherRedemption> Redemptions { get; set; } = new List<VoucherRedemption>();
    }

    /// <summary>
    /// Gán voucher cho user và trạng thái sử dụng (mỗi user tối đa một lần / một voucher).
    /// </summary>
    public class UserVoucher
    {
        [Key]
        public int UserVoucherId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int VoucherId { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime? UsedDate { get; set; }

        public bool IsNew { get; set; } = false;

        public virtual Voucher Voucher { get; set; } = null!;
    }
}
