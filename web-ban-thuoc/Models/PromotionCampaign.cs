using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_ban_thuoc.Models
{
    public class PromotionCampaign
    {
        [Key]
        public int PromotionCampaignId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        [Range(0, 100)]
        public double DiscountPercent { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int? CategoryId { get; set; }

        public string? Brand { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Category? Category { get; set; }

        public int? BannerId { get; set; }

        [ForeignKey("BannerId")]
        public virtual Banner? Banner { get; set; }
    }
}
