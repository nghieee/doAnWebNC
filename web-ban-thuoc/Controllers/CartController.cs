using Microsoft.AspNetCore.Mvc;
using web_ban_thuoc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Model cho hiển thị giỏ hàng
public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class CartController : Controller
{
    private readonly LongChauDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public CartController(LongChauDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Hiển thị giỏ hàng
    public IActionResult Index()
    {
        if (!User.Identity.IsAuthenticated)
        {
            TempData["LoginError"] = "Bạn cần đăng nhập để xem giỏ hàng!";
            return RedirectToAction("Index", "Auth");
        }

        var cart = GetCart();
        return View(cart);
    }

    // Thêm sản phẩm vào giỏ
    [HttpPost]
    public IActionResult AddToCart(int productId, int quantity = 1)
    {
        if (!User.Identity.IsAuthenticated)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng!", requireLogin = true });
            }
            TempData["LoginError"] = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng!";
            return RedirectToAction("Index", "Auth");
        }

        // Lấy thông tin sản phẩm từ DB
        var product = _context.Products
            .Where(p => p.ProductId == productId)
            .Select(p => new {
                p.ProductId,
                p.ProductName,
                p.Price,
                ImageUrl = p.ProductImages.FirstOrDefault(i => i.IsMain == true) != null ? p.ProductImages.FirstOrDefault(i => i.IsMain == true).ImageUrl : p.ProductImages.FirstOrDefault().ImageUrl
            })
            .FirstOrDefault();
        if (product == null) return NotFound();

        // Lấy hoặc tạo Order (giỏ hàng) cho user
        var userId = _userManager.GetUserId(User);
        var dbCart = _context.Orders.FirstOrDefault(o => o.UserId == userId && o.Status == "Cart");
        if (dbCart == null)
        {
            dbCart = new Order
            {
                UserId = userId,
                Status = "Cart",
                OrderDate = DateTime.Now,
                TotalAmount = 0
            };
            _context.Orders.Add(dbCart);
            _context.SaveChanges();
        }

        // Kiểm tra sản phẩm đã có trong giỏ chưa
        var cartItem = _context.OrderItems.FirstOrDefault(x => x.OrderId == dbCart.OrderId && x.ProductId == productId);
        if (cartItem != null)
        {
            // Nếu đã có, tăng số lượng
            cartItem.Quantity += quantity;
            _context.SaveChanges();
        }
        else
        {
            // Nếu chưa có, tạo mới
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = dbCart.OrderId,
                ProductId = product.ProductId,
                Quantity = quantity,
                Price = product.Price
            });
            _context.SaveChanges();
        }

        // Cập nhật tổng tiền
        dbCart.TotalAmount = _context.OrderItems.Where(x => x.OrderId == dbCart.OrderId).Sum(x => x.Price * x.Quantity);
        _context.SaveChanges();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new {
                success = true,
                message = $"Đã thêm {product.ProductName} vào giỏ hàng!",
                cartCount = _context.OrderItems.Count(x => x.OrderId == dbCart.OrderId)
            });
        }
        TempData["CartMessage"] = $"Đã thêm {product.ProductName} vào giỏ hàng!";
        return Redirect(Request.Headers["Referer"].ToString());
    }

    // Xóa sản phẩm khỏi giỏ
    public IActionResult Remove(int productId)
    {
        if (!User.Identity.IsAuthenticated)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để xóa sản phẩm khỏi giỏ hàng!", requireLogin = true });
            }
            TempData["LoginError"] = "Bạn cần đăng nhập để xóa sản phẩm khỏi giỏ hàng!";
            return RedirectToAction("Index", "Auth");
        }

        var userId = _userManager.GetUserId(User);
        var dbCart = _context.Orders.FirstOrDefault(o => o.UserId == userId && o.Status == "Cart");
        if (dbCart == null) return RedirectToAction("Index");

        var removedItem = _context.OrderItems.FirstOrDefault(x => x.OrderId == dbCart.OrderId && x.ProductId == productId);
        string productName = "";
        if (removedItem != null)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == removedItem.ProductId);
            productName = product?.ProductName ?? "";
            _context.OrderItems.Remove(removedItem);
            _context.SaveChanges();
        }

        dbCart.TotalAmount = _context.OrderItems.Where(x => x.OrderId == dbCart.OrderId).Sum(x => x.Price * x.Quantity);
        _context.SaveChanges();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new {
                success = true,
                message = $"Đã xóa {productName} khỏi giỏ hàng!",
                cartCount = _context.OrderItems.Count(x => x.OrderId == dbCart.OrderId)
            });
        }
        return RedirectToAction("Index");
    }

    // Cập nhật số lượng sản phẩm
    [HttpPost]
    public IActionResult UpdateQuantity(int productId, int quantity)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập để cập nhật số lượng sản phẩm!", requireLogin = true });
        }

        var userId = _userManager.GetUserId(User);
        var dbCart = _context.Orders.FirstOrDefault(o => o.UserId == userId && o.Status == "Cart");
        if (dbCart == null) return Json(new { success = false, message = "Không tìm thấy giỏ hàng!" });

        var item = _context.OrderItems.FirstOrDefault(x => x.OrderId == dbCart.OrderId && x.ProductId == productId);
        if (item != null && quantity > 0 && quantity <= 99)
        {
            item.Quantity = quantity;
            _context.SaveChanges();
            dbCart.TotalAmount = _context.OrderItems.Where(x => x.OrderId == dbCart.OrderId).Sum(x => x.Price * x.Quantity);
            _context.SaveChanges();
            return Json(new {
                success = true,
                itemTotal = (item.Price * quantity).ToString("N0"),
                cartTotal = (dbCart.TotalAmount ?? 0).ToString("N0")
            });
        }
        return Json(new { success = false, message = "Số lượng không hợp lệ!" });
    }

    // Đã chuyển toàn bộ logic sang modal, xóa các action Checkout cũ dùng CheckoutViewModel

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CheckoutPopup([FromBody] CheckoutPopupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin!" });
        }
        var userId = _userManager.GetUserId(User);
        var dbCart = _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefault(o => o.UserId == userId && o.Status == "Cart");
        if (dbCart == null || dbCart.OrderItems.Count == 0)
        {
            return Json(new { success = false, message = "Giỏ hàng của bạn đang trống!" });
        }
        // Cập nhật thông tin đơn hàng
        dbCart.Status = "Chờ xác nhận";
        dbCart.OrderDate = DateTime.Now;
        dbCart.ShippingAddress = model.ShippingAddress;
        dbCart.PaymentStatus = "Chưa thanh toán";
        // Lưu thêm tên và sđt vào Order nếu muốn (bạn có thể mở rộng model Order)
        _context.SaveChanges();
        // Thêm record vào bảng Payment
        var payment = new Payment
        {
            OrderId = dbCart.OrderId,
            PaymentMethod = "COD",
            Amount = dbCart.TotalAmount,
            PaymentStatus = "Pending",
            PaymentDate = null
        };
        _context.Payments.Add(payment);
        _context.SaveChanges();
        return Json(new { success = true });
    }

    public IActionResult ThankYou()
    {
        return View();
    }

    // Lấy tóm tắt giỏ hàng cho modal (AJAX)
    [HttpGet]
    public IActionResult GetCartSummary()
    {
        var cart = GetCart();
        var total = cart.Sum(i => i.Price * i.Quantity);
        return Json(new {
            items = cart.Select(i => new {
                productName = i.ProductName,
                imageUrl = i.ImageUrl,
                quantity = i.Quantity,
                total = i.Price * i.Quantity
            }),
            total
        });
    }

    // Lấy giỏ hàng từ database
    private List<CartItem> GetCart()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return new List<CartItem>();
        }

        var userId = _userManager.GetUserId(User);
        var dbCart = _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .ThenInclude(p => p.ProductImages)
            .FirstOrDefault(o => o.UserId == userId && o.Status == "Cart");
            
        if (dbCart != null && dbCart.OrderItems.Any())
        {
            return dbCart.OrderItems.Select(oi => new CartItem
            {
                ProductId = oi.ProductId ?? 0,
                ProductName = oi.Product?.ProductName ?? "",
                Price = oi.Price,
                ImageUrl = oi.Product?.ProductImages?.FirstOrDefault(i => i.IsMain == true)?.ImageUrl ?? 
                           oi.Product?.ProductImages?.FirstOrDefault()?.ImageUrl,
                Quantity = oi.Quantity
            }).ToList();
        }
        
        return new List<CartItem>();
    }
} 