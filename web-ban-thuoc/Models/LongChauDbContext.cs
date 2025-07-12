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
    // ... các DbSet khác nếu có

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Đảm bảo InventoryTransaction có khóa chính
        modelBuilder.Entity<InventoryTransaction>().HasKey(x => x.TransactionId);
        
        // Cấu hình Order với IdentityUser (chỉ cấu hình này)
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Cấu hình Review liên kết với IdentityUser
        modelBuilder.Entity<Review>()
            .HasOne<Microsoft.AspNetCore.Identity.IdentityUser>(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // ... có thể thêm các cấu hình khác nếu cần
        base.OnModelCreating(modelBuilder);
    }
}
