using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace web_ban_thuoc.Models;

public class LongChauDbContext : IdentityDbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public LongChauDbContext(DbContextOptions<LongChauDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<UserRankInfo> UserRankInfos { get; set; }
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<UserVoucher> UserVouchers { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<WarehouseStock> WarehouseStocks { get; set; }
    public DbSet<ProductBatch> ProductBatches { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
    public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
    public DbSet<GoodsReceiptLine> GoodsReceiptLines { get; set; }
    public DbSet<VoucherRedemption> VoucherRedemptions { get; set; }
    public DbSet<LoyaltyPointTransaction> LoyaltyPointTransactions { get; set; }
    public DbSet<LoyaltyReward> LoyaltyRewards { get; set; }
    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<PayOSWebhookEvent> PayOSWebhookEvents { get; set; }
    public DbSet<DbActivityLog> DbActivityLogs { get; set; }
    public DbSet<News> News { get; set; }
    public DbSet<StockAdjustment> StockAdjustments { get; set; }
    public DbSet<StockAdjustmentDetail> StockAdjustmentDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryTransaction>().HasKey(x => x.TransactionId);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Review>()
            .HasOne<Microsoft.AspNetCore.Identity.IdentityUser>(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UserVoucher>()
            .HasOne(uv => uv.Voucher)
            .WithMany(v => v.UserVouchers)
            .HasForeignKey(uv => uv.VoucherId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserVoucher>()
            .HasIndex(uv => new { uv.UserId, uv.VoucherId })
            .IsUnique();

        modelBuilder.Entity<Cart>()
            .HasIndex(c => c.UserId)
            .IsUnique();

        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Product)
            .WithMany()
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne(h => h.Order)
            .WithMany(o => o.StatusHistories)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Product)
            .WithMany(p => p.InventoryTransactions)
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Warehouse)
            .WithMany(w => w.InventoryTransactions)
            .HasForeignKey(t => t.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Order)
            .WithMany()
            .HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WarehouseStock>()
            .HasIndex(ws => new { ws.WarehouseId, ws.ProductId })
            .IsUnique();

        modelBuilder.Entity<WarehouseStock>()
            .HasOne(ws => ws.Warehouse)
            .WithMany(w => w.WarehouseStocks)
            .HasForeignKey(ws => ws.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WarehouseStock>()
            .HasOne(ws => ws.Product)
            .WithMany(p => p.WarehouseStocks)
            .HasForeignKey(ws => ws.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductBatch>()
            .HasOne(b => b.Product)
            .WithMany(p => p.ProductBatches)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductBatch>()
            .HasOne(b => b.Warehouse)
            .WithMany(w => w.ProductBatches)
            .HasForeignKey(b => b.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductBatch>()
            .HasOne(b => b.Supplier)
            .WithMany()
            .HasForeignKey(b => b.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProductBatch>()
            .HasOne(b => b.GoodsReceiptLine)
            .WithOne(l => l.ProductBatch)
            .HasForeignKey<ProductBatch>(b => b.GoodsReceiptLineId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Supplier>()
            .HasIndex(s => s.Code)
            .IsUnique();

        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(po => po.OrderCode)
            .IsUnique();

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(l => l.PurchaseOrder)
            .WithMany(po => po.Lines)
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.Supplier)
            .WithMany(s => s.PurchaseOrders)
            .HasForeignKey(po => po.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.Warehouse)
            .WithMany()
            .HasForeignKey(po => po.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(gr => gr.Supplier)
            .WithMany(s => s.GoodsReceipts)
            .HasForeignKey(gr => gr.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(gr => gr.Warehouse)
            .WithMany()
            .HasForeignKey(gr => gr.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(gr => gr.PurchaseOrder)
            .WithMany(po => po.GoodsReceipts)
            .HasForeignKey(gr => gr.PurchaseOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GoodsReceiptLine>()
            .HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceipt>()
            .HasIndex(gr => gr.ReceiptCode)
            .IsUnique();

        modelBuilder.Entity<GoodsReceiptLine>()
            .HasOne(l => l.GoodsReceipt)
            .WithMany(gr => gr.Lines)
            .HasForeignKey(l => l.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Supplier)
            .WithMany()
            .HasForeignKey(t => t.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.ProductBatch)
            .WithMany()
            .HasForeignKey(t => t.ProductBatchId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.GoodsReceipt)
            .WithMany()
            .HasForeignKey(t => t.GoodsReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique()
            .HasFilter("[Sku] IS NOT NULL AND [Sku] <> ''");

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Supplier)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<VoucherRedemption>()
            .HasOne(r => r.Voucher)
            .WithMany(v => v.Redemptions)
            .HasForeignKey(r => r.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VoucherRedemption>()
            .HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VoucherRedemption>()
            .HasIndex(r => new { r.VoucherId, r.OrderId })
            .IsUnique();

        modelBuilder.Entity<LoyaltyPointTransaction>()
            .HasOne(t => t.Order)
            .WithMany()
            .HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<LoyaltyPointTransaction>()
            .HasIndex(t => new { t.UserId, t.OrderId, t.TransactionType })
            .HasFilter("[OrderId] IS NOT NULL");

        modelBuilder.Entity<LoyaltyPointTransaction>()
            .HasOne<LoyaltyReward>()
            .WithMany()
            .HasForeignKey(t => t.LoyaltyRewardId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Voucher>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(v => v.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Shipment>()
            .HasOne(s => s.Order)
            .WithOne(o => o.Shipment)
            .HasForeignKey<Shipment>(s => s.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Shipment>()
            .HasIndex(s => s.OrderId)
            .IsUnique();

        modelBuilder.Entity<PayOSWebhookEvent>()
            .HasIndex(e => e.IdempotencyKey)
            .IsUnique();

        modelBuilder.Entity<StockAdjustment>()
            .HasIndex(sa => sa.AdjustmentCode)
            .IsUnique();

        modelBuilder.Entity<StockAdjustment>()
            .HasOne(sa => sa.Warehouse)
            .WithMany()
            .HasForeignKey(sa => sa.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockAdjustmentDetail>()
            .HasOne(sad => sad.StockAdjustment)
            .WithMany(sa => sa.Details)
            .HasForeignKey(sad => sad.StockAdjustmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockAdjustmentDetail>()
            .HasOne(sad => sad.Product)
            .WithMany()
            .HasForeignKey(sad => sad.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockAdjustmentDetail>()
            .HasOne(sad => sad.ProductBatch)
            .WithMany()
            .HasForeignKey(sad => sad.ProductBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = base.SaveChanges();
        OnAfterSaveChanges(auditEntries);
        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChangesAsync(auditEntries, cancellationToken);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged || entry.Entity is DbActivityLog)
                continue;

            var friendlyName = GetEntityFriendlyName(entry.Entity);
            if (friendlyName == null)
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                EntityName = friendlyName,
                Action = entry.State switch
                {
                    EntityState.Added => "Thêm",
                    EntityState.Deleted => "Xóa",
                    EntityState.Modified => "Sửa",
                    _ => "Khác"
                }
            };

            auditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.EntityId = property.CurrentValue?.ToString();
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.NewValues[propertyName] = property.CurrentValue ?? "";
                        break;

                    case EntityState.Deleted:
                        auditEntry.OldValues[propertyName] = property.OriginalValue ?? "";
                        break;

                    case EntityState.Modified:
                        if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                        {
                            auditEntry.OldValues[propertyName] = property.OriginalValue ?? "";
                            auditEntry.NewValues[propertyName] = property.CurrentValue ?? "";
                            auditEntry.ChangedProperties.Add(propertyName);
                        }
                        break;
                }
            }
        }

        return auditEntries;
    }

    private void OnAfterSaveChanges(List<AuditEntry> auditEntries)
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return;

        var httpContext = _httpContextAccessor?.HttpContext;
        string? userId = null;
        string? userEmail = "Hệ thống";

        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            userEmail = httpContext.User.Identity.Name;
        }

        foreach (var auditEntry in auditEntries)
        {
            if (auditEntry.Action == "Sửa" && auditEntry.ChangedProperties.Count == 0)
                continue;

            var log = auditEntry.ToActivityLog(userId, userEmail);
            DbActivityLogs.Add(log);
        }

        base.SaveChanges();
    }

    private async Task OnAfterSaveChangesAsync(List<AuditEntry> auditEntries, CancellationToken cancellationToken)
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return;

        var httpContext = _httpContextAccessor?.HttpContext;
        string? userId = null;
        string? userEmail = "Hệ thống";

        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            userEmail = httpContext.User.Identity.Name;
        }

        foreach (var auditEntry in auditEntries)
        {
            if (auditEntry.Action == "Sửa" && auditEntry.ChangedProperties.Count == 0)
                continue;

            var log = auditEntry.ToActivityLog(userId, userEmail);
            DbActivityLogs.Add(log);
        }

        await base.SaveChangesAsync(cancellationToken);
    }

    private string? GetEntityFriendlyName(object entity)
    {
        return entity switch
        {
            Product => "Sản phẩm",
            Category => "Danh mục sản phẩm",
            Banner => "Banner",
            Voucher => "Voucher",
            Supplier => "Nhà cung cấp",
            Warehouse => "Kho hàng",
            LoyaltyReward => "Quà đổi điểm",
            web_ban_thuoc.Models.News => "Bài viết",
            StockAdjustment => "Phiếu điều chỉnh tồn kho",
            _ => null
        };
    }
}

public class AuditEntry
{
    public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; }
    public string Action { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string? EntityId { get; set; }
    public Dictionary<string, object> OldValues { get; } = new();
    public Dictionary<string, object> NewValues { get; } = new();
    public List<string> ChangedProperties { get; } = new();

    public AuditEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        Entry = entry;
    }

    public DbActivityLog ToActivityLog(string? userId, string? userEmail)
    {
        var log = new DbActivityLog
        {
            UserId = userId,
            UserEmail = userEmail,
            Action = Action,
            EntityName = EntityName,
            EntityId = EntityId ?? Entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "",
            CreatedAt = DateTime.Now
        };

        if (Action == "Thêm")
        {
            log.Description = $"Thêm mới {EntityName.ToLower()} '{GetEntityDisplayName()}'";
            if (Entry.Entity is Product p)
            {
                log.Description += $" với giá bán {p.Price:N0}đ, giá vốn {p.CostPrice?.ToString("N0") ?? "0"}đ";
            }
            log.Description += ".";
        }
        else if (Action == "Xóa")
        {
            log.Description = $"Xóa {EntityName.ToLower()} '{GetEntityDisplayName()}'.";
        }
        else if (Action == "Sửa")
        {
            var changes = new List<string>();
            foreach (var prop in ChangedProperties)
            {
                var oldVal = OldValues[prop];
                var newVal = NewValues[prop];
                var propNameVi = GetPropertyNameVi(prop);

                if (prop.Contains("Price") || prop.Equals("Price") || prop.Equals("CostPrice"))
                {
                    decimal oldPrice = 0;
                    decimal newPrice = 0;
                    try { oldPrice = Convert.ToDecimal(oldVal); } catch { }
                    try { newPrice = Convert.ToDecimal(newVal); } catch { }
                    changes.Add($"{propNameVi} thay đổi từ {oldPrice:N0}đ thành {newPrice:N0}đ");
                }
                else
                {
                    changes.Add($"{propNameVi} thay đổi từ '{oldVal}' thành '{newVal}'");
                }
            }
            log.Description = $"Cập nhật {EntityName.ToLower()} '{GetEntityDisplayName()}': " + string.Join(", ", changes) + ".";
        }

        return log;
    }

    private string GetEntityDisplayName()
    {
        try
        {
            if (Entry.Entity is Product p) return p.ProductName ?? "";
            if (Entry.Entity is Category c) return c.CategoryName ?? "";
            if (Entry.Entity is Banner b) return b.Title ?? $"Banner {b.BannerId}";
            if (Entry.Entity is Voucher v) return v.Code ?? "";
            if (Entry.Entity is Supplier s) return s.Name ?? "";
            if (Entry.Entity is Warehouse w) return w.Name ?? "";
            if (Entry.Entity is LoyaltyReward lr) return lr.Title ?? "";
            if (Entry.Entity is News n) return n.Title ?? "";
        }
        catch { }
        return Entry.Entity.GetType().Name;
    }

    private string GetPropertyNameVi(string propName)
    {
        return propName switch
        {
            "ProductName" => "Tên sản phẩm",
            "Price" => "Giá bán",
            "CostPrice" => "Giá vốn",
            "StockQuantity" => "Tồn kho",
            "Origin" => "Xuất xứ",
            "Brand" => "Thương hiệu",
            "IsActive" => "Trạng thái hoạt động",
            "CategoryName" => "Tên danh mục",
            "Description" => "Mô tả",
            "Code" => "Mã",
            "DiscountAmount" => "Số tiền giảm",
            "PercentValue" => "Phần trăm giảm",
            "ExpiryDate" => "Ngày hết hạn",
            "Title" => "Tiêu đề",
            "ImageUrl" => "Đường dẫn ảnh",
            _ => propName
        };
    }
}
