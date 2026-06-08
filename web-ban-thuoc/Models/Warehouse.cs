using System;
using System.Collections.Generic;

namespace web_ban_thuoc.Models;

public class Warehouse
{
    public int WarehouseId { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<WarehouseStock> WarehouseStocks { get; set; } = new List<WarehouseStock>();

    public virtual ICollection<ProductBatch> ProductBatches { get; set; } = new List<ProductBatch>();
}
