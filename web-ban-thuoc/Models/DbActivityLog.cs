using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_ban_thuoc.Models
{
    [Table("DbActivityLogs")]
    public class DbActivityLog
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        public string? UserEmail { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // "Thêm", "Sửa", "Xóa"

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; } = string.Empty; // "Sản phẩm", "Danh mục", "Banner", "Voucher", ...

        [MaxLength(50)]
        public string? EntityId { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
