namespace web_ban_thuoc.Models;

public class Cart
{
    public int CartId { get; set; }

    public string UserId { get; set; } = null!;

    public string? VoucherCode { get; set; }

    public decimal VoucherDiscount { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem
{
    public int CartItemId { get; set; }

    public int CartId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Cart Cart { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
