using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_ban_thuoc.Models
{
    public class Voucher
    {
        [Key]
        public int VoucherId { get; set; }
        [Required]
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        /// <summary>
        /// Nếu giảm số tiền cố định, giá trị số tiền (ví dụ: 9000, 19000)
        /// </summary>
        public decimal? DiscountAmount { get; set; }
        public bool IsActive { get; set; } = true;
        /// <summary>
        /// Loại voucher: FullOrder (áp dụng toàn bộ đơn), Category (áp dụng cho sản phẩm thuộc danh mục)
        /// </summary>
        public string DiscountType { get; set; } = "FullOrder"; // FullOrder, Category
        /// <summary>
        /// Nếu giảm theo %, giá trị phần trăm (ví dụ: 5, 10, 15)
        /// </summary>
        public decimal? PercentValue { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Detail { get; set; }
    }

    public class UserVoucher
    {
        [Key]
        public int UserVoucherId { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("Voucher")]
        public int VoucherId { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime? UsedDate { get; set; }
        public virtual Voucher Voucher { get; set; } = null!;
    }
} 