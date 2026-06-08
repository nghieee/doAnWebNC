using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services;

public interface ICartService
{
    Task<Cart> GetOrCreateCartAsync(string userId);
    Task<List<CartLineViewModel>> GetCartLinesAsync(string userId);
    Task<int> GetItemCountAsync(string userId);
    Task<(bool success, string? message)> AddItemAsync(string userId, int productId, int quantity);
    Task<(bool success, string? message)> UpdateQuantityAsync(string userId, int productId, int quantity);
    Task RemoveItemAsync(string userId, int productId);
    Task RecalculateTotalsAsync(Cart cart);
    Task<(bool valid, string? error)> ValidateStockAsync(string userId);
    Task<Order> CreateOrderFromCartAsync(string userId, CheckoutPopupViewModel checkout, string initialStatus);
    Task ClearCartAsync(string userId);
}

public class CartService : ICartService
{
    private readonly LongChauDbContext _context;
    private readonly IInventoryService _inventoryService;

    public CartService(LongChauDbContext context, IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task<Cart> GetOrCreateCartAsync(string userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart != null)
            return cart;

        cart = new Cart { UserId = userId, UpdatedAt = DateTime.Now };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    public async Task<List<CartLineViewModel>> GetCartLinesAsync(string userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || cart.Items.Count == 0)
            return new List<CartLineViewModel>();

        var stockMap = await _inventoryService.GetAvailableStockMapAsync(cart.Items.Select(i => i.ProductId));

        return cart.Items.Select(i => new CartLineViewModel
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.ProductName ?? "",
            Price = i.UnitPrice,
            ImageUrl = i.Product?.ProductImages?.FirstOrDefault(pi => pi.IsMain == true)?.ImageUrl
                       ?? i.Product?.ProductImages?.FirstOrDefault()?.ImageUrl,
            Quantity = i.Quantity,
            StockQuantity = stockMap.GetValueOrDefault(i.ProductId, 0),
            CategoryId = i.Product?.CategoryId
        }).ToList();
    }

    public async Task<int> GetItemCountAsync(string userId)
    {
        return await _context.CartItems
            .Where(i => i.Cart.UserId == userId)
            .SumAsync(i => (int?)i.Quantity) ?? 0;
    }

    public async Task<(bool success, string? message)> AddItemAsync(string userId, int productId, int quantity)
    {
        if (quantity <= 0)
            return (false, "Số lượng không hợp lệ!");

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive);
        if (product == null)
            return (false, "Sản phẩm không tồn tại!");

        var available = await _inventoryService.GetAvailableStockAsync(productId);
        var cart = await GetOrCreateCartAsync(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        var newQty = (item?.Quantity ?? 0) + quantity;

        if (newQty > available)
            return (false, $"Sản phẩm '{product.ProductName}' chỉ còn {available} trong kho.");

        if (item != null)
        {
            item.Quantity = newQty;
            item.UnitPrice = product.Price;
        }
        else
        {
            _context.CartItems.Add(new CartItem
            {
                CartId = cart.CartId,
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price
            });
        }

        cart.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool success, string? message)> UpdateQuantityAsync(string userId, int productId, int quantity)
    {
        if (quantity <= 0 || quantity > 99)
            return (false, "Số lượng không hợp lệ!");

        var cart = await GetOrCreateCartAsync(userId);
        var item = await _context.CartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == productId);
        if (item == null)
            return (false, "Không tìm thấy sản phẩm trong giỏ!");

        var available = await _inventoryService.GetAvailableStockAsync(productId);
        if (quantity > available)
            return (false, $"Chỉ còn {available} sản phẩm trong kho.");

        item.Quantity = quantity;
        cart.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task RemoveItemAsync(string userId, int productId)
    {
        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null) return;

        var item = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == productId);
        if (item != null)
        {
            _context.CartItems.Remove(item);
            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public Task RecalculateTotalsAsync(Cart cart) => Task.CompletedTask;

    public async Task<(bool valid, string? error)> ValidateStockAsync(string userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || cart.Items.Count == 0)
            return (false, "Giỏ hàng của bạn đang trống!");

        var stockMap = await _inventoryService.GetAvailableStockMapAsync(cart.Items.Select(i => i.ProductId));

        foreach (var item in cart.Items)
        {
            if (item.Product == null || !item.Product.IsActive)
                return (false, "Có sản phẩm không còn bán trong giỏ hàng.");

            var available = stockMap.GetValueOrDefault(item.ProductId, 0);
            if (item.Quantity > available)
                return (false, $"Sản phẩm '{item.Product.ProductName}' không đủ tồn kho (còn {available}).");
        }

        return (true, null);
    }

    public async Task<Order> CreateOrderFromCartAsync(string userId, CheckoutPopupViewModel checkout, string initialStatus)
    {
        var (valid, error) = await ValidateStockAsync(userId);
        if (!valid)
            throw new InvalidOperationException(error);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId)
            ?? throw new InvalidOperationException("Giỏ hàng trống!");

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var total = subtotal - cart.VoucherDiscount;
        if (total < 0) total = 0;

        var order = new Order
        {
            UserId = userId,
            Status = initialStatus,
            OrderDate = DateTime.Now,
            ShippingAddress = checkout.ShippingAddress,
            FullName = checkout.FullName,
            Phone = checkout.Phone,
            PaymentStatus = PaymentStatuses.Unpaid,
            VoucherCode = cart.VoucherCode,
            VoucherDiscount = cart.VoucherDiscount,
            TotalAmount = total,
            OrderItems = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.UnitPrice
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.OrderId,
            FromStatus = null,
            ToStatus = initialStatus,
            ChangedByUserId = userId,
            Note = "Tạo đơn từ giỏ hàng"
        });

        await ClearCartAsync(userId);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task ClearCartAsync(string userId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null) return;

        _context.CartItems.RemoveRange(cart.Items);
        cart.VoucherCode = null;
        cart.VoucherDiscount = 0;
        cart.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }
}
