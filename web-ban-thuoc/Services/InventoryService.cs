using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services;

public interface IInventoryService
{
    Task<Warehouse> GetDefaultWarehouseAsync();
    Task<int> GetAvailableStockAsync(int productId, int? warehouseId = null);
    Task<Dictionary<int, int>> GetAvailableStockMapAsync(IEnumerable<int> productIds, int? warehouseId = null);
    Task SyncProductStockQuantityAsync(int productId);
    Task<InventoryTransaction> ImportStockAsync(int productId, int quantity, string? supplierName, decimal? unitCost, string? note, string? createdByUserId, int? warehouseId = null, string? batchNo = null, DateTime? expiryDate = null, int? supplierId = null);
    Task<GoodsReceipt> ReceiveGoodsAsync(GoodsReceiptInput input, string? createdByUserId);
    Task<PurchaseOrder> CreatePurchaseOrderAsync(PurchaseOrderInput input, string? createdByUserId);
    Task ConfirmPurchaseOrderAsync(int purchaseOrderId);
    Task ExportStockForOrderAsync(int orderId, string? createdByUserId);
    Task ReturnStockForOrderAsync(int orderId, string? createdByUserId);
    Task<InventoryTransaction> AdjustStockAsync(int productId, int newQuantity, string? note, string? createdByUserId, int? warehouseId = null);
    Task<bool> HasExportedOrderAsync(int orderId);
}

public class GoodsReceiptLineInput
{
    public int ProductId { get; set; }
    public int? PurchaseOrderLineId { get; set; }
    public string BatchNo { get; set; } = null!;
    public DateTime? ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
}

public class GoodsReceiptInput
{
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public string? Note { get; set; }
    public List<GoodsReceiptLineInput> Lines { get; set; } = new();
}

public class PurchaseOrderLineInput
{
    public int ProductId { get; set; }
    public int QuantityOrdered { get; set; }
    public decimal UnitCost { get; set; }
}

public class PurchaseOrderInput
{
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public string? Note { get; set; }
    public List<PurchaseOrderLineInput> Lines { get; set; } = new();
}

public class InventoryService : IInventoryService
{
    private readonly LongChauDbContext _context;

    public InventoryService(LongChauDbContext context)
    {
        _context = context;
    }

    public async Task<Warehouse> GetDefaultWarehouseAsync()
    {
        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.IsDefault && w.IsActive);
        if (warehouse != null)
            return warehouse;

        warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.IsActive);
        if (warehouse != null)
            return warehouse;

        warehouse = new Warehouse
        {
            Name = "Kho chính",
            Address = "Kho trung tâm",
            IsDefault = true,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();
        return warehouse;
    }

    public async Task<int> GetAvailableStockAsync(int productId, int? warehouseId = null)
    {
        if (warehouseId.HasValue)
        {
            var ws = await _context.WarehouseStocks
                .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId.Value && x.ProductId == productId);
            return ws?.AvailableQuantity ?? 0;
        }

        return await _context.WarehouseStocks
            .Where(x => x.ProductId == productId)
            .SumAsync(x => x.QuantityOnHand - x.QuantityReserved);
    }

    public async Task<Dictionary<int, int>> GetAvailableStockMapAsync(IEnumerable<int> productIds, int? warehouseId = null)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, int>();

        IQueryable<WarehouseStock> query = _context.WarehouseStocks.Where(x => ids.Contains(x.ProductId));
        if (warehouseId.HasValue)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        var rows = await query.ToListAsync();
        return ids.ToDictionary(
            id => id,
            id => rows.Where(r => r.ProductId == id).Sum(r => r.AvailableQuantity));
    }

    public async Task SyncProductStockQuantityAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return;

        product.StockQuantity = await _context.WarehouseStocks
            .Where(ws => ws.ProductId == productId)
            .SumAsync(ws => ws.QuantityOnHand);
        await _context.SaveChangesAsync();
    }

    private async Task<WarehouseStock> GetOrCreateWarehouseStockAsync(int warehouseId, int productId)
    {
        var stock = await _context.WarehouseStocks
            .FirstOrDefaultAsync(ws => ws.WarehouseId == warehouseId && ws.ProductId == productId);

        if (stock != null)
            return stock;

        stock = new WarehouseStock
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            QuantityOnHand = 0,
            QuantityReserved = 0,
            UpdatedAt = DateTime.Now
        };
        _context.WarehouseStocks.Add(stock);
        await _context.SaveChangesAsync();
        return stock;
    }

    public async Task<InventoryTransaction> ImportStockAsync(
        int productId, int quantity, string? supplierName, decimal? unitCost, string? note,
        string? createdByUserId, int? warehouseId = null, string? batchNo = null,
        DateTime? expiryDate = null, int? supplierId = null)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Số lượng nhập phải lớn hơn 0.");

        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId)
            ?? throw new InvalidOperationException("Không tìm thấy sản phẩm.");

        var warehouse = warehouseId.HasValue
            ? await _context.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == warehouseId.Value && w.IsActive)
                ?? throw new InvalidOperationException("Không tìm thấy kho.")
            : await GetDefaultWarehouseAsync();

        var stock = await GetOrCreateWarehouseStockAsync(warehouse.WarehouseId, productId);
        var before = stock.QuantityOnHand;
        stock.QuantityOnHand += quantity;
        stock.UpdatedAt = DateTime.Now;

        var batch = new ProductBatch
        {
            ProductId = productId,
            WarehouseId = warehouse.WarehouseId,
            BatchNo = string.IsNullOrWhiteSpace(batchNo) ? $"LOT-{DateTime.Now:yyyyMMddHHmmss}" : batchNo.Trim(),
            ExpiryDate = expiryDate,
            QuantityOnHand = quantity,
            UnitCost = unitCost,
            SupplierId = supplierId,
            CreatedAt = DateTime.Now
        };
        _context.ProductBatches.Add(batch);

        var transaction = new InventoryTransaction
        {
            ProductId = productId,
            WarehouseId = warehouse.WarehouseId,
            TransactionType = "Import",
            Quantity = quantity,
            QuantityBefore = before,
            QuantityAfter = stock.QuantityOnHand,
            SupplierId = supplierId,
            SupplierName = supplierName,
            UnitCost = unitCost,
            Note = note,
            CreatedByUserId = createdByUserId,
            TransactionDate = DateTime.Now
        };
        _context.InventoryTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        batch.GoodsReceiptLineId = null;
        transaction.ProductBatchId = batch.ProductBatchId;
        await SyncProductStockQuantityAsync(productId);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<GoodsReceipt> ReceiveGoodsAsync(GoodsReceiptInput input, string? createdByUserId)
    {
        if (input.Lines == null || input.Lines.Count == 0)
            throw new InvalidOperationException("Phiếu nhập phải có ít nhất một dòng.");

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == input.SupplierId && s.IsActive)
            ?? throw new InvalidOperationException("Không tìm thấy nhà cung cấp.");

        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == input.WarehouseId && w.IsActive)
            ?? throw new InvalidOperationException("Không tìm thấy kho.");

        PurchaseOrder? po = null;
        if (input.PurchaseOrderId.HasValue)
        {
            po = await _context.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.PurchaseOrderId == input.PurchaseOrderId.Value)
                ?? throw new InvalidOperationException("Không tìm thấy đơn đặt hàng.");
        }

        var receiptCode = await GenerateReceiptCodeAsync();
        var receipt = new GoodsReceipt
        {
            ReceiptCode = receiptCode,
            PurchaseOrderId = input.PurchaseOrderId,
            SupplierId = supplier.SupplierId,
            WarehouseId = warehouse.WarehouseId,
            ReceiptDate = DateTime.Now,
            Note = input.Note,
            CreatedByUserId = createdByUserId
        };
        _context.GoodsReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        foreach (var line in input.Lines)
        {
            if (line.Quantity <= 0)
                throw new InvalidOperationException("Số lượng nhập phải lớn hơn 0.");

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == line.ProductId && p.IsActive)
                ?? throw new InvalidOperationException($"Sản phẩm #{line.ProductId} không tồn tại.");

            PurchaseOrderLine? poLine = null;
            if (line.PurchaseOrderLineId.HasValue)
            {
                poLine = po?.Lines.FirstOrDefault(l => l.PurchaseOrderLineId == line.PurchaseOrderLineId.Value)
                    ?? throw new InvalidOperationException("Dòng đơn đặt hàng không hợp lệ.");
                if (line.Quantity > poLine.RemainingQuantity)
                    throw new InvalidOperationException($"Số lượng nhập vượt quá còn lại của đơn đặt hàng ({poLine.RemainingQuantity}).");
            }

            var stock = await GetOrCreateWarehouseStockAsync(warehouse.WarehouseId, line.ProductId);
            var before = stock.QuantityOnHand;
            stock.QuantityOnHand += line.Quantity;
            stock.UpdatedAt = DateTime.Now;

            var receiptLine = new GoodsReceiptLine
            {
                GoodsReceiptId = receipt.GoodsReceiptId,
                ProductId = line.ProductId,
                PurchaseOrderLineId = line.PurchaseOrderLineId,
                BatchNo = string.IsNullOrWhiteSpace(line.BatchNo) ? $"LOT-{DateTime.Now:yyyyMMddHHmmss}" : line.BatchNo.Trim(),
                ExpiryDate = line.ExpiryDate,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost
            };
            _context.GoodsReceiptLines.Add(receiptLine);
            await _context.SaveChangesAsync();

            var batch = new ProductBatch
            {
                ProductId = line.ProductId,
                WarehouseId = warehouse.WarehouseId,
                BatchNo = receiptLine.BatchNo,
                ExpiryDate = line.ExpiryDate,
                QuantityOnHand = line.Quantity,
                UnitCost = line.UnitCost,
                SupplierId = supplier.SupplierId,
                GoodsReceiptLineId = receiptLine.GoodsReceiptLineId,
                CreatedAt = DateTime.Now
            };
            _context.ProductBatches.Add(batch);

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = line.ProductId,
                WarehouseId = warehouse.WarehouseId,
                TransactionType = "Import",
                Quantity = line.Quantity,
                QuantityBefore = before,
                QuantityAfter = stock.QuantityOnHand,
                SupplierId = supplier.SupplierId,
                SupplierName = supplier.Name,
                GoodsReceiptId = receipt.GoodsReceiptId,
                UnitCost = line.UnitCost,
                Note = $"Phiếu nhập {receiptCode} — Lô {receiptLine.BatchNo}",
                CreatedByUserId = createdByUserId,
                TransactionDate = DateTime.Now
            });

            if (poLine != null)
            {
                poLine.QuantityReceived += line.Quantity;
            }

            if (product.CostPrice == null || product.CostPrice == 0)
                product.CostPrice = line.UnitCost;

            await SyncProductStockQuantityAsync(line.ProductId);
        }

        if (po != null)
            await UpdatePurchaseOrderStatusAsync(po);

        await _context.SaveChangesAsync();
        return receipt;
    }

    public async Task<PurchaseOrder> CreatePurchaseOrderAsync(PurchaseOrderInput input, string? createdByUserId)
    {
        if (input.Lines == null || input.Lines.Count == 0)
            throw new InvalidOperationException("Đơn đặt hàng phải có ít nhất một dòng.");

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == input.SupplierId && s.IsActive)
            ?? throw new InvalidOperationException("Không tìm thấy nhà cung cấp.");

        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == input.WarehouseId && w.IsActive)
            ?? throw new InvalidOperationException("Không tìm thấy kho.");

        var productIds = input.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToListAsync();

        foreach (var line in input.Lines)
        {
            var product = products.FirstOrDefault(p => p.ProductId == line.ProductId)
                ?? throw new InvalidOperationException($"Không tìm thấy sản phẩm #{line.ProductId}.");
            if (product.SupplierId != supplier.SupplierId)
                throw new InvalidOperationException(
                    $"Sản phẩm \"{product.ProductName}\" không thuộc nhà cung cấp \"{supplier.Name}\".");
        }

        var orderCode = await GeneratePurchaseOrderCodeAsync();
        var po = new PurchaseOrder
        {
            OrderCode = orderCode,
            SupplierId = supplier.SupplierId,
            WarehouseId = warehouse.WarehouseId,
            Status = PurchaseOrderStatuses.Draft,
            OrderDate = DateTime.Now,
            ExpectedDate = input.ExpectedDate,
            Note = input.Note,
            CreatedByUserId = createdByUserId,
            Lines = input.Lines.Select(l => new PurchaseOrderLine
            {
                ProductId = l.ProductId,
                QuantityOrdered = l.QuantityOrdered,
                QuantityReceived = 0,
                UnitCost = l.UnitCost
            }).ToList()
        };

        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();
        return po;
    }

    public async Task ConfirmPurchaseOrderAsync(int purchaseOrderId)
    {
        var po = await _context.PurchaseOrders.FindAsync(purchaseOrderId)
            ?? throw new InvalidOperationException("Không tìm thấy đơn đặt hàng.");

        if (po.Status != PurchaseOrderStatuses.Draft)
            throw new InvalidOperationException("Chỉ xác nhận đơn ở trạng thái Nháp.");

        po.Status = PurchaseOrderStatuses.Confirmed;
        await _context.SaveChangesAsync();
    }

    public async Task ExportStockForOrderAsync(int orderId, string? createdByUserId)
    {
        if (await HasExportedOrderAsync(orderId))
            return;

        var orderItems = await _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .ToListAsync();

        if (orderItems.Count == 0)
            return;

        var warehouse = await GetDefaultWarehouseAsync();

        foreach (var item in orderItems)
        {
            if (!item.ProductId.HasValue || item.Quantity <= 0)
                continue;

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId.Value);
            if (product == null)
                continue;

            var stock = await GetOrCreateWarehouseStockAsync(warehouse.WarehouseId, product.ProductId);
            if (stock.AvailableQuantity < item.Quantity)
                throw new InvalidOperationException($"Sản phẩm '{product.ProductName}' không đủ tồn kho (còn {stock.AvailableQuantity}, cần {item.Quantity}).");

            await DeductFromBatchesFefoAsync(product.ProductId, warehouse.WarehouseId, item.Quantity, orderId, createdByUserId);

            var before = stock.QuantityOnHand;
            stock.QuantityOnHand -= item.Quantity;
            stock.UpdatedAt = DateTime.Now;
            product.SoldQuantity = (product.SoldQuantity ?? 0) + item.Quantity;

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = product.ProductId,
                WarehouseId = warehouse.WarehouseId,
                TransactionType = "Sale",
                Quantity = item.Quantity,
                QuantityBefore = before,
                QuantityAfter = stock.QuantityOnHand,
                OrderId = orderId,
                Note = $"Xuất kho FEFO cho đơn hàng #{orderId}",
                CreatedByUserId = createdByUserId,
                TransactionDate = DateTime.Now
            });

            await SyncProductStockQuantityAsync(product.ProductId);
        }

        await _context.SaveChangesAsync();
    }

    private async Task DeductFromBatchesFefoAsync(int productId, int warehouseId, int quantity, int orderId, string? createdByUserId)
    {
        var batches = await _context.ProductBatches
            .Where(b => b.ProductId == productId && b.WarehouseId == warehouseId && b.QuantityOnHand > 0)
            .OrderBy(b => b.ExpiryDate == null)
            .ThenBy(b => b.ExpiryDate)
            .ThenBy(b => b.CreatedAt)
            .ToListAsync();

        if (batches.Count == 0)
        {
            var stock = await GetOrCreateWarehouseStockAsync(warehouseId, productId);
            if (stock.QuantityOnHand > 0)
            {
                var legacy = new ProductBatch
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    BatchNo = $"LEGACY-{productId}",
                    QuantityOnHand = stock.QuantityOnHand,
                    CreatedAt = DateTime.Now
                };
                _context.ProductBatches.Add(legacy);
                await _context.SaveChangesAsync();
                batches.Add(legacy);
            }
        }

        var remaining = quantity;
        foreach (var batch in batches)
        {
            if (remaining <= 0) break;

            var take = Math.Min(batch.QuantityOnHand, remaining);
            batch.QuantityOnHand -= take;
            remaining -= take;

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                ProductBatchId = batch.ProductBatchId,
                TransactionType = "BatchSale",
                Quantity = take,
                QuantityBefore = batch.QuantityOnHand + take,
                QuantityAfter = batch.QuantityOnHand,
                OrderId = orderId,
                Note = $"Xuất lô {batch.BatchNo} (FEFO) — đơn #{orderId}",
                CreatedByUserId = createdByUserId,
                TransactionDate = DateTime.Now
            });
        }

        if (remaining > 0)
            throw new InvalidOperationException($"Không đủ lô hàng để xuất (thiếu {remaining} đơn vị). Vui lòng kiểm tra phiếu nhập/lô.");
    }

    public async Task ReturnStockForOrderAsync(int orderId, string? createdByUserId)
    {
        var saleTransactions = await _context.InventoryTransactions
            .Where(t => t.OrderId == orderId && (t.TransactionType == "Sale" || t.TransactionType == "BatchSale"))
            .ToListAsync();

        if (saleTransactions.Count == 0)
            return;

        var returnExists = await _context.InventoryTransactions
            .AnyAsync(t => t.OrderId == orderId && t.TransactionType == "Return");
        if (returnExists)
            return;

        var warehouse = await GetDefaultWarehouseAsync();

        foreach (var sale in saleTransactions.Where(t => t.TransactionType == "Sale"))
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == sale.ProductId);
            if (product == null) continue;

            var stock = await GetOrCreateWarehouseStockAsync(sale.WarehouseId, sale.ProductId);
            var before = stock.QuantityOnHand;
            stock.QuantityOnHand += sale.Quantity;
            stock.UpdatedAt = DateTime.Now;
            product.SoldQuantity = Math.Max(0, (product.SoldQuantity ?? 0) - sale.Quantity);

            var batchNo = $"RET-{orderId}-{DateTime.Now:yyyyMMdd}";
            _context.ProductBatches.Add(new ProductBatch
            {
                ProductId = sale.ProductId,
                WarehouseId = sale.WarehouseId,
                BatchNo = batchNo,
                ExpiryDate = null,
                QuantityOnHand = sale.Quantity,
                CreatedAt = DateTime.Now
            });

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = sale.ProductId,
                WarehouseId = sale.WarehouseId,
                TransactionType = "Return",
                Quantity = sale.Quantity,
                QuantityBefore = before,
                QuantityAfter = stock.QuantityOnHand,
                OrderId = orderId,
                Note = $"Nhập lại kho từ đơn hàng #{orderId}",
                CreatedByUserId = createdByUserId,
                TransactionDate = DateTime.Now
            });

            await SyncProductStockQuantityAsync(sale.ProductId);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<InventoryTransaction> AdjustStockAsync(int productId, int newQuantity, string? note, string? createdByUserId, int? warehouseId = null)
    {
        if (newQuantity < 0)
            throw new InvalidOperationException("Tồn kho không được âm.");

        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId)
            ?? throw new InvalidOperationException("Không tìm thấy sản phẩm.");

        var warehouse = warehouseId.HasValue
            ? await _context.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == warehouseId.Value)
                ?? throw new InvalidOperationException("Không tìm thấy kho.")
            : await GetDefaultWarehouseAsync();

        var stock = await GetOrCreateWarehouseStockAsync(warehouse.WarehouseId, productId);
        var before = stock.QuantityOnHand;
        var delta = Math.Abs(newQuantity - before);
        stock.QuantityOnHand = newQuantity;
        stock.UpdatedAt = DateTime.Now;

        var transaction = new InventoryTransaction
        {
            ProductId = productId,
            WarehouseId = warehouse.WarehouseId,
            TransactionType = "Adjustment",
            Quantity = delta,
            QuantityBefore = before,
            QuantityAfter = newQuantity,
            Note = note ?? "Kiểm kê/điều chỉnh tồn kho",
            CreatedByUserId = createdByUserId,
            TransactionDate = DateTime.Now
        };

        _context.InventoryTransactions.Add(transaction);
        await SyncProductStockQuantityAsync(productId);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public Task<bool> HasExportedOrderAsync(int orderId)
    {
        return _context.InventoryTransactions.AnyAsync(t => t.OrderId == orderId && t.TransactionType == "Sale");
    }

    private async Task UpdatePurchaseOrderStatusAsync(PurchaseOrder po)
    {
        var lines = await _context.PurchaseOrderLines.Where(l => l.PurchaseOrderId == po.PurchaseOrderId).ToListAsync();
        if (lines.All(l => l.QuantityReceived >= l.QuantityOrdered))
            po.Status = PurchaseOrderStatuses.Received;
        else if (lines.Any(l => l.QuantityReceived > 0))
            po.Status = PurchaseOrderStatuses.PartiallyReceived;
    }

    private async Task<string> GeneratePurchaseOrderCodeAsync()
    {
        var prefix = $"PO{DateTime.Now:yyyyMMdd}";
        var count = await _context.PurchaseOrders.CountAsync(p => p.OrderCode.StartsWith(prefix));
        return $"{prefix}-{(count + 1):D4}";
    }

    private async Task<string> GenerateReceiptCodeAsync()
    {
        var prefix = $"GR{DateTime.Now:yyyyMMdd}";
        var count = await _context.GoodsReceipts.CountAsync(r => r.ReceiptCode.StartsWith(prefix));
        return $"{prefix}-{(count + 1):D4}";
    }
}
