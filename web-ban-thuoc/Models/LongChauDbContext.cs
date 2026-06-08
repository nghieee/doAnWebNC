using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace web_ban_thuoc.Models;

public class LongChauDbContext : IdentityDbContext
{
    public LongChauDbContext(DbContextOptions<LongChauDbContext> options)
        : base(options)
    {
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

        base.OnModelCreating(modelBuilder);
    }
}
