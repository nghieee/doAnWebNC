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
    private readonly IGHNService _ghnService;
    private readonly IConfiguration _configuration;

    public CartController(
        LongChauDbContext context,
        UserManager<IdentityUser> userManager,
        ICartService cartService,
        IOrderService orderService,
        IGHNService ghnService,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _cartService = cartService;
        _orderService = orderService;
        _ghnService = ghnService;
        _configuration = configuration;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
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

        if (string.IsNullOrWhiteSpace(model.FullName) ||
            string.IsNullOrWhiteSpace(model.Phone) ||
            string.IsNullOrWhiteSpace(model.HouseNumber) ||
            model.ProvinceId <= 0 ||
            model.DistrictId <= 0 ||
            string.IsNullOrWhiteSpace(model.WardCode))
        {
            return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin!" });
        }

        model.ShippingAddress = $"{model.HouseNumber}";

        var provinceName = (await _ghnService.GetProvincesAsync(HttpContext.RequestAborted))
            .Provinces.FirstOrDefault(p => p.ProvinceID == model.ProvinceId)?.ProvinceName;

        if (!string.IsNullOrEmpty(provinceName))
        {
            model.ShippingAddress += $", {provinceName}";
        }

        var districtName = (await _ghnService.GetDistrictsAsync(model.ProvinceId, HttpContext.RequestAborted))
            .Districts.FirstOrDefault(d => d.DistrictID == model.DistrictId)?.DistrictName;

        if (!string.IsNullOrEmpty(districtName))
        {
            model.ShippingAddress += $", {districtName}";
        }

        var wardName = (await _ghnService.GetWardsAsync(model.DistrictId, HttpContext.RequestAborted))
            .Wards.FirstOrDefault(w => w.WardCode == model.WardCode)?.WardName;

        if (!string.IsNullOrEmpty(wardName))
        {
            model.ShippingAddress += $", {wardName}";
        }

        var userId = _userManager.GetUserId(User)!;
        try
        {
            var services = await _ghnService.GetServicesAsync(model.DistrictId, model.WardCode, HttpContext.RequestAborted);
            var defaultServiceTypeId = _configuration.GetValue<int>("GHN:DefaultServiceTypeId", 2);
            model.ServiceId = services
                .Where(s => s.ServiceTypeId == defaultServiceTypeId)
                .OrderBy(s => s.ServiceId)
                .Select(s => (int?)s.ServiceId)
                .FirstOrDefault() ?? services.Select(s => (int?)s.ServiceId).FirstOrDefault();

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGhnOrder([FromBody] CreateGhnOrderModel model)
    {
        if (!User.Identity?.IsAuthenticated == true)
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập để tạo đơn GHN." });
        }

        if (model.OrderId <= 0)
        {
            return Json(new { success = false, message = "Thiếu mã đơn hàng." });
        }

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.OrderId == model.OrderId);

        if (order == null || order.DistrictId == null || string.IsNullOrWhiteSpace(order.WardCode))
        {
            return Json(new { success = false, message = "Thiếu thông tin địa chỉ hoặc đơn hàng không hợp lệ." });
        }

        var selectedServiceId = model.ServiceId;
        if (!selectedServiceId.HasValue)
        {
            var services = await _ghnService.GetServicesAsync(order.DistrictId.Value, order.WardCode, HttpContext.RequestAborted);
            var defaultServiceTypeId = _configuration.GetValue<int>("GHN:DefaultServiceTypeId", 2);
            selectedServiceId = services
                .Where(s => s.ServiceTypeId == defaultServiceTypeId)
                .OrderBy(s => s.ServiceId)
                .Select(s => (int?)s.ServiceId)
                .FirstOrDefault() ?? services.Select(s => (int?)s.ServiceId).FirstOrDefault();
        }

        var userId = _userManager.GetUserId(User)!;

        var request = new GhnCreateOrderRequest
        {
            ClientOrderCode = $"ORD-{order.OrderId}",
            ToName = order.FullName ?? "Khách hàng",
            ToPhone = order.Phone ?? string.Empty,
            ToAddress = ParseShippingAddress(order.ShippingAddress ?? string.Empty),
            PaymentTypeId = 2,
            RequiredNote = "KHONGCHOXEMHANG",
            CodAmount = Convert.ToInt32(Math.Round(order.TotalAmount ?? 0, 0, MidpointRounding.AwayFromZero)),
            InsuranceValue = Convert.ToInt32(Math.Round(order.TotalAmount ?? 0, 0, MidpointRounding.AwayFromZero)),
            ServiceId = selectedServiceId,
            Weight = order.OrderItems.Sum(i => i.Quantity * 200),
            Length = 15,
            Width = 15,
            Height = 10,
            ToDistrictId = order.DistrictId ?? 0,
            ToWardCode = order.WardCode ?? string.Empty,
            Items = order.OrderItems.Select(i => new GhnCreateOrderItem
            {
                Name = i.Product?.ProductName ?? "Sản phẩm",
                Code = i.Product?.Sku,
                Quantity = i.Quantity,
                Price = Convert.ToInt32(Math.Round(i.Price, 0, MidpointRounding.AwayFromZero)),
                Length = 15,
                Width = 15,
                Weight = 200,
                Height = 10
            }).ToList()
        };

        var createResult = await _ghnService.CreateOrderAsync(request, HttpContext.RequestAborted);
        if (!createResult.Success || string.IsNullOrWhiteSpace(createResult.OrderCode))
        {
            return Json(new { success = false, message = createResult.Message ?? "Tạo đơn GHN thất bại." });
        }

        if (order.Shipment == null)
        {
            order.Shipment = new Shipment
            {
                OrderId = order.OrderId,
                Carrier = ShippingCarriers.Ghn,
                TrackingCode = createResult.OrderCode,
                CreatedByUserId = userId
            };
            _context.Shipments.Add(order.Shipment);
        }
        else
        {
            order.Shipment.Carrier = ShippingCarriers.Ghn;
            order.Shipment.TrackingCode = createResult.OrderCode;
        }

        await _context.SaveChangesAsync();

        var printResult = await _ghnService.CreatePrintTokenAsync(createResult.OrderCode, HttpContext.RequestAborted);
        if (printResult.Success && order.Shipment != null)
        {
            order.Shipment.GhnOrderCode = createResult.OrderCode;
            order.Shipment.GhnPrintToken = printResult.Token;
            order.Shipment.GhnPrintTokenExpiredAt = DateTime.Now.AddMinutes(30);
            order.Shipment.GhnPrintFormat = "A5";
            order.Shipment.Note = order.Shipment.Note ?? "Đã tạo đơn GHN";
            await _context.SaveChangesAsync();
        }

        return Json(new
        {
            success = true,
            message = "Đã tạo đơn GHN.",
            orderCode = createResult.OrderCode,
            printUrl = printResult.A5Url,
            serviceId = selectedServiceId
        });
    }

    private static string ParseShippingAddress(string address)
    {
        return address.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    public class CreateGhnOrderModel
    {
        public int OrderId { get; set; }
        public int? ServiceId { get; set; }
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

    [HttpGet]
    public async Task<IActionResult> AddressLookup(string? type, int? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return Json(new { success = false, message = "Thiếu loại địa chỉ." });
        }

        var result = await _ghnService.SafeGetAddressLookupAsync(type, id, ct);

        if (!result.Success && result.Districts.Count == 0 && result.Wards.Count == 0 && result.Provinces.Count == 0)
        {
            return Json(new { success = false, message = result.Message ?? "Không lấy được dữ liệu địa chỉ." });
        }

        var data = result.Provinces.Count > 0 ? result.Provinces :
                   result.Districts.Count > 0 ? result.Districts :
                   result.Wards;

        return Json(new
        {
            success = true,
            data = data.Select(x => new
            {
                id = result.Provinces.Count > 0 ? (object)x.ProvinceID :
                     result.Districts.Count > 0 ? (object)x.DistrictID :
                     (object)x.WardCode,
                name = result.Provinces.Count > 0 ? x.ProvinceName :
                       result.Districts.Count > 0 ? x.DistrictName :
                       x.WardName,
                code = x.WardCode
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> SearchAddress(string keyword, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
        {
            return Json(new { success = false, message = "Vui lòng nhập ít nhất 2 ký tự." });
        }

        var result = await _ghnService.GetAddressByKeywordAsync(keyword, ct);
        if (!result.Success)
        {
            return Json(new { success = false, message = result.Message ?? "Không tìm kiếm được địa chỉ." });
        }

        var combined = new List<object>();
        if (result.Provinces.Any())
        {
            combined.AddRange(result.Provinces.Select(p => new
            {
                type = "province",
                id = p.ProvinceID,
                name = p.ProvinceName,
                code = (string?)null
            }));
        }
        if (result.Districts.Any())
        {
            combined.AddRange(result.Districts.Select(d => new
            {
                type = "district",
                id = d.DistrictID,
                name = $"{d.DistrictName}, {d.ProvinceName}",
                code = (string?)null
            }));
        }
        if (result.Wards.Any())
        {
            combined.AddRange(result.Wards.Select(w => new
            {
                type = "ward",
                id = 0,
                name = $"{w.WardName}, {w.DistrictName}, {w.ProvinceName}",
                code = w.WardCode
            }));
        }

        return Json(new { success = true, data = combined });
    }

    [HttpGet]
    public async Task<IActionResult> EstimateShippingFee(int? provinceId, int? districtId, string? wardCode, CancellationToken ct)
    {
        if (!districtId.HasValue || string.IsNullOrWhiteSpace(wardCode))
        {
            return Json(new { success = false, message = "Thiếu thông tin địa chỉ giao hàng." });
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập để ước tính phí ship." });
        }

        var lines = await _cartService.GetCartLinesAsync(userId);
        if (!lines.Any())
        {
            return Json(new { success = false, message = "Giỏ hàng trống, không thể ước tính phí ship." });
        }

        var totalWeight = lines.Sum(i => i.Quantity * 200);
        var maxLength = 15;
        var maxWidth = 15;
        var maxHeight = 10;

        var fromDistrictId = _configuration.GetValue<int?>("GHN:FromDistrictId");
        var fromWardCode = _configuration.GetValue<string?>("GHN:FromWardCode");

        try
        {
            var defaultServiceTypeId = _configuration.GetValue<int>("GHN:DefaultServiceTypeId", 2);
            var services = await _ghnService.GetServicesAsync(districtId.Value, wardCode, ct);
            var selected = services.FirstOrDefault(s => s.ServiceTypeId == defaultServiceTypeId)
                           ?? services.FirstOrDefault();
            if (selected == null)
            {
                return Json(new { success = false, message = "Chưa có dịch vụ GHN phù hợp cho địa chỉ này." });
            }

            var request = new GhnEstimateFeeRequest
            {
                ToDistrictId = districtId.Value,
                ToWardCode = wardCode,
                Weight = totalWeight,
                Length = maxLength,
                Width = maxWidth,
                Height = maxHeight,
                InsuranceValue = (int)Math.Round(lines.Sum(i => i.Price * i.Quantity), 0),
                FromDistrictId = fromDistrictId,
                FromWardCode = fromWardCode,
                ServiceId = selected.ServiceId,
                ServiceTypeId = selected.ServiceTypeId
            };

            var fee = await _ghnService.EstimateShippingFeeAsync(request, ct);
            if (fee == null)
            {
                return Json(new { success = false, message = "Không ước tính được phí ship từ GHN." });
            }
            return Json(new { success = true, fee });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi ước tính phí ship: " + ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> AvailableServices(CancellationToken ct)
    {
        var provinceId = HttpContext.Request.Query.TryGetValue("provinceId", out var provinceValue) && int.TryParse(provinceValue, out var parsedProvince) ? new int?(parsedProvince) : null;
        var districtId = HttpContext.Request.Query.TryGetValue("districtId", out var districtValue) && int.TryParse(districtValue, out var parsedDistrict) ? new int?(parsedDistrict) : null;
        var wardCode = HttpContext.Request.Query.TryGetValue("wardCode", out var wardValue) ? wardValue.ToString() : null;

        if (!districtId.HasValue || string.IsNullOrWhiteSpace(wardCode))
        {
            return Json(new { success = false, message = "Thiếu thông tin địa chỉ." });
        }

        var services = await _ghnService.GetServicesAsync(districtId.Value, wardCode, ct);
        var defaultServiceTypeId = _configuration.GetValue<int>("GHN:DefaultServiceTypeId", 2);

        var preferred = services
            .Where(s => s.ServiceTypeId == defaultServiceTypeId)
            .OrderBy(s => s.ServiceId)
            .FirstOrDefault();

        int? selectedServiceId;
        if (preferred != null)
        {
            selectedServiceId = preferred.ServiceId;
        }
        else
        {
            selectedServiceId = services
                .Select(s => (int?)s.ServiceId)
                .FirstOrDefault();
        }

        return Json(new
        {
            success = true,
            services = services.Select(s => new
            {
                serviceId = s.ServiceId,
                shortName = s.ShortName,
                serviceTypeId = s.ServiceTypeId,
                preferred = preferred != null && preferred.ServiceId == s.ServiceId
            }),
            selectedServiceId
        });
    }

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
