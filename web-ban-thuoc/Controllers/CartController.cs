using Microsoft.AspNetCore.Mvc;
using web_ban_thuoc.Models;
using web_ban_thuoc.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace web_ban_thuoc.Controllers;

public class CartController : Controller
{
    private readonly LongChauDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    public CartController(
        LongChauDbContext context,
        UserManager<IdentityUser> userManager,
        ICartService cartService,
        IOrderService orderService)
    {
        _context = context;
        _userManager = userManager;
        _cartService = cartService;
        _orderService = orderService;
    }

    public async Task<IActionResult> Index()
    {
        if (!User.Identity?.IsAuthenticated == true)
        {
            TempData["LoginError"] = "Bạn cần đăng nhập để xem giỏ hàng!";
            return RedirectToAction("Index", "Auth");
        }

        var userId = _userManager.GetUserId(User)!;
        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
        var lines = await _cartService.GetCartLinesAsync(userId);
        var subtotal = lines.Sum(i => i.Price * i.Quantity);
        ViewBag.VoucherDiscount = cart?.VoucherDiscount ?? 0;
        ViewBag.VoucherCode = cart?.VoucherCode;
        ViewBag.TotalAmount = subtotal - (cart?.VoucherDiscount ?? 0);
        if ((decimal)ViewBag.TotalAmount < 0) ViewBag.TotalAmount = 0m;
        return View(lines);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        if (!User.Identity?.IsAuthenticated == true)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng!", requireLogin = true });
            TempData["LoginError"] = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng!";
            return RedirectToAction("Index", "Auth");
        }

        var userId = _userManager.GetUserId(User)!;
        var (success, message) = await _cartService.AddItemAsync(userId, productId, quantity);
        if (!success)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message });
            TempData["CartMessage"] = message;
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        var productName = await _context.Products.Where(p => p.ProductId == productId).Select(p => p.ProductName).FirstOrDefaultAsync();
        var count = await _cartService.GetItemCountAsync(userId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, message = $"Đã thêm {productName} vào giỏ hàng!", cartCount = count });
        }
        TempData["CartMessage"] = $"Đã thêm {productName} vào giỏ hàng!";
        return Redirect(Request.Headers["Referer"].ToString() ?? "/");
    }

    public async Task<IActionResult> Remove(int productId)
    {
        if (!User.Identity?.IsAuthenticated == true)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Bạn cần đăng nhập!", requireLogin = true });
            return RedirectToAction("Index", "Auth");
        }

        var userId = _userManager.GetUserId(User)!;
        var productName = await _context.Products.Where(p => p.ProductId == productId).Select(p => p.ProductName).FirstOrDefaultAsync();
        await _cartService.RemoveItemAsync(userId, productId);
        var count = await _cartService.GetItemCountAsync(userId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, message = $"Đã xóa {productName} khỏi giỏ hàng!", cartCount = count });
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
    {
        if (!User.Identity?.IsAuthenticated == true)
            return Json(new { success = false, message = "Bạn cần đăng nhập!", requireLogin = true });

        var userId = _userManager.GetUserId(User)!;
        var (success, message) = await _cartService.UpdateQuantityAsync(userId, productId, quantity);
        if (!success)
            return Json(new { success = false, message });

        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
        var lines = await _cartService.GetCartLinesAsync(userId);
        var subtotal = lines.Sum(i => i.Price * i.Quantity);
        var total = subtotal - (cart?.VoucherDiscount ?? 0);
        var item = lines.FirstOrDefault(l => l.ProductId == productId);

        return Json(new
        {
            success = true,
            itemTotal = (item!.Price * quantity).ToString("N0"),
            cartTotal = Math.Max(0, total).ToString("N0")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckoutPopup([FromBody] CheckoutPopupViewModel model)
    {
        if (!User.Identity?.IsAuthenticated == true)
            return Json(new { success = false, message = "Vui lòng đăng nhập để đặt hàng!" });

        if (string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Phone) || string.IsNullOrEmpty(model.ShippingAddress))
            return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin!" });

        var userId = _userManager.GetUserId(User)!;
        try
        {
            var initialStatus = model.PaymentMethod == "PayOS"
                ? OrderStatuses.PendingPayment
                : OrderStatuses.PendingConfirmation;

            var order = await _cartService.CreateOrderFromCartAsync(userId, model, initialStatus);

            _context.Payments.Add(new Payment
            {
                OrderId = order.OrderId,
                PaymentMethod = model.PaymentMethod,
                Amount = order.TotalAmount,
                PaymentStatus = PaymentStatuses.Pending,
                PaymentDate = null
            });
            await _context.SaveChangesAsync();

            if (model.PaymentMethod == "PayOS")
            {
                return Json(new
                {
                    success = true,
                    checkoutUrl = $"/PayOS/CreatePayment?orderId={order.OrderId}",
                    orderId = order.OrderId
                });
            }

            return Json(new { success = true, orderId = order.OrderId });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public IActionResult ThankYou() => View();

    [HttpGet]
    public async Task<IActionResult> GetCartSummary()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Json(new { items = Array.Empty<object>(), subtotal = 0, voucherDiscount = 0, total = 0 });

        var lines = await _cartService.GetCartLinesAsync(userId);
        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
        var subtotal = lines.Sum(i => i.Price * i.Quantity);
        var voucherDiscount = cart?.VoucherDiscount ?? 0;
        var total = Math.Max(0, subtotal - voucherDiscount);

        return Json(new
        {
            items = lines.Select(i => new
            {
                productName = i.ProductName,
                imageUrl = i.ImageUrl,
                quantity = i.Quantity,
                total = i.Price * i.Quantity
            }),
            subtotal,
            voucherDiscount,
            total
        });
    }

    [HttpPost]
    public async Task<IActionResult> ApplyVoucher([FromBody] ApplyVoucherModel model)
    {
        if (!User.Identity.IsAuthenticated)
            return Json(new { success = false, message = "Bạn cần đăng nhập để áp dụng mã giảm giá!" });

        var userId = _userManager.GetUserId(User)!;
        var cart = await _cartService.GetOrCreateCartAsync(userId);
        if (cart.Items.Count == 0)
            return Json(new { success = false, message = "Giỏ hàng của bạn đang trống!" });

        var lines = await _cartService.GetCartLinesAsync(userId);
        var total = lines.Sum(i => i.Price * i.Quantity);

        var (voucher, _, error) = await VoucherHelper.ResolveForApplyAsync(_context, userId, model.code, total, lines);
        if (voucher == null)
            return Json(new { success = false, message = error ?? "Mã giảm giá không hợp lệ!" });

        var discount = VoucherHelper.CalculateDiscount(voucher, total, lines);

        cart.VoucherCode = voucher.Code;
        cart.VoucherDiscount = discount;
        cart.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            message = $"Áp dụng mã thành công! Giảm {discount:N0}đ.",
            total = (total - discount).ToString("N0")
        });
    }

    public class ApplyVoucherModel { public string code { get; set; } = string.Empty; }

    [HttpPost]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        if (!User.Identity.IsAuthenticated)
            return Json(new { success = false, message = "Bạn cần đăng nhập!" });

        var userId = _userManager.GetUserId(User)!;
        try
        {
            await _orderService.CancelByCustomerAsync(orderId, userId);
            return Json(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
