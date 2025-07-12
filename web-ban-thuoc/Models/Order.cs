using System;
using System.Collections.Generic;

namespace web_ban_thuoc.Models;

public partial class Order
{
    public int OrderId { get; set; }

    // Đổi UserId sang string? để phù hợp với Identity
    public string? UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Status { get; set; }

    public string? ShippingAddress { get; set; }

    public string? PaymentStatus { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    // Navigation property tới IdentityUser
    public virtual Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }
}
