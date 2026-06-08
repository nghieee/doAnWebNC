namespace web_ban_thuoc.Models;

public class CartLineViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int StockQuantity { get; set; }

    public int? CategoryId { get; set; }
}
