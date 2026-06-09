using System;
using System.Collections.Generic;

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

    public virtual Category? Category { get; set; }

    public virtual Supplier? Supplier { get; set; }

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<WarehouseStock> WarehouseStocks { get; set; } = new List<WarehouseStock>();

    public virtual ICollection<ProductBatch> ProductBatches { get; set; } = new List<ProductBatch>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
