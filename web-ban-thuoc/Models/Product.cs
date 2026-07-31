using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_ban_thuoc.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Sku { get; set; }

    public string? Barcode { get; set; }

    /// <summary>Số đăng ký lưu hành (BYT).</summary>
    public string? RegistrationNumber { get; set; }

    public bool RequiresPrescription { get; set; }

    public decimal? CostPrice { get; set; }

    public string? Brand { get; set; }

    public decimal Price { get; set; }

    public string? Package { get; set; }

    public int? CategoryId { get; set; }

    public int? SupplierId { get; set; }

    public string? Ingredients { get; set; }

    public string? Uses { get; set; }

    public string? Dosage { get; set; }

    public string? TargetUsers { get; set; }

    public string? Contraindications { get; set; }

    public bool IsFeature { get; set; }

    public string? Origin { get; set; }

    public int StockQuantity { get; set; }

    /// <summary>Tổng tồn các kho (đồng bộ từ WarehouseStocks).</summary>

    public bool IsActive { get; set; }

    public string? IngredientUnit { get; set; }

    public string? Slug { get; set; }

    public int? SoldQuantity { get; set; }

    /// <summary>Ngưỡng tồn kho tối thiểu, khi tồn <= ngưỡng sẽ đề xuất nhập hàng.</summary>
    public int MinStockLevel { get; set; } = 0;

    /// <summary>Giá bán khuyến mãi / sau giảm (nếu có).</summary>
    public decimal? DiscountPrice { get; set; }

    /// <summary>Tỷ lệ giảm giá (%) ví dụ: 10, 15, 20.</summary>
    public double? DiscountPercent { get; set; }

    /// <summary>Ngày bắt đầu giảm giá.</summary>
    public DateTime? DiscountStartDate { get; set; }

    /// <summary>Ngày kết thúc giảm giá.</summary>
    public DateTime? DiscountEndDate { get; set; }

    /// <summary>Bật/tắt chương trình giảm giá cho sản phẩm.</summary>
    public bool IsDiscountActive { get; set; } = true;

    /// <summary>Kiểm tra sản phẩm có đang trong thời gian giảm giá hợp lệ hay không.</summary>
    [NotMapped]
    public bool IsOnDiscount
    {
        get
        {
            if (!IsDiscountActive) return false;
            var now = DateTime.Now;
            bool dateValid = (!DiscountStartDate.HasValue || DiscountStartDate.Value <= now) &&
                             (!DiscountEndDate.HasValue || DiscountEndDate.Value >= now);
            if (!dateValid) return false;

            if (DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < Price)
                return true;

            if (DiscountPercent.HasValue && DiscountPercent.Value > 0 && DiscountPercent.Value < 100)
                return true;

            return false;
        }
    }

    /// <summary>Giá thực tế áp dụng (nếu đang giảm giá thì lấy giá sau giảm, ngược lại lấy giá gốc).</summary>
    [NotMapped]
    public decimal EffectivePrice
    {
        get
        {
            if (!IsOnDiscount) return Price;
            if (DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < Price)
                return DiscountPrice.Value;
            if (DiscountPercent.HasValue && DiscountPercent.Value > 0)
                return Math.Round(Price * (decimal)(1.0 - DiscountPercent.Value / 100.0));
            return Price;
        }
    }

    /// <summary>Phần trăm giảm giá thực tế (dùng hiển thị Badge -20%).</summary>
    [NotMapped]
    public int DiscountPercentCalculated
    {
        get
        {
            if (!IsOnDiscount || Price <= 0) return 0;
            if (DiscountPercent.HasValue && DiscountPercent.Value > 0)
                return (int)Math.Round(DiscountPercent.Value);
            return (int)Math.Round((Price - EffectivePrice) / Price * 100);
        }
    }

    public int? BannerId { get; set; }

    [ForeignKey("BannerId")]
    public virtual Banner? Banner { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Supplier? Supplier { get; set; }

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<WarehouseStock> WarehouseStocks { get; set; } = new List<WarehouseStock>();

    public virtual ICollection<ProductBatch> ProductBatches { get; set; } = new List<ProductBatch>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
