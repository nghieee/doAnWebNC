using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using web_ban_thuoc.Models;
using web_ban_thuoc.Services;

namespace web_ban_thuoc.Controllers.Admin;

[Authorize(Roles = "Admin,WarehouseStaff,CustomerSupport")]
public class AdminShippingController : Controller
{
    private readonly LongChauDbContext _context;
    private readonly IGHNService _ghnService;
    private readonly IOrderService _orderService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;

    public AdminShippingController(
        LongChauDbContext context,
        IGHNService ghnService,
        IOrderService orderService,
        UserManager<IdentityUser> userManager,
        IConfiguration configuration)
    {
        _context = context;
        _ghnService = ghnService;
        _orderService = orderService;
        _userManager = userManager;
        _configuration = configuration;
    }

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
                    id = x.ProvinceID != 0 ? (object)x.ProvinceID : x.DistrictID != 0 ? (object)x.DistrictID : (object)x.WardCode,
                    name = x.ProvinceName ?? x.DistrictName ?? x.WardName,
                    code = x.WardCode
                })
            });
        }

        [HttpGet]
        public async Task<IActionResult> AvailableServices(int orderId, CancellationToken ct)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

            if (order == null || order.DistrictId == null || string.IsNullOrWhiteSpace(order.WardCode))
            {
                return Json(new { success = false, message = "Thiếu thông tin địa chỉ giao hàng của đơn." });
            }

            var services = await _ghnService.GetServicesAsync(order.DistrictId.Value, order.WardCode, ct);
            var defaultServiceTypeId = _configuration.GetValue<int>("GHN:DefaultServiceTypeId", 2);

            var preferred = services
                .Where(s => s.ServiceTypeId == defaultServiceTypeId)
                .OrderBy(s => s.ServiceId)
                .FirstOrDefault();

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
                selectedServiceId = preferred != null ? preferred.ServiceId : services.Select(s => (int?)s.ServiceId).FirstOrDefault()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShippingOrder(int orderId, int? serviceId, CancellationToken ct)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Shipment)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

            if (order == null || order.DistrictId == null || string.IsNullOrWhiteSpace(order.WardCode))
            {
                return Json(new { success = false, message = "Thiếu thông tin địa chỉ hoặc đơn hàng không hợp lệ." });
            }

            var selectedServiceId = serviceId;
            if (!selectedServiceId.HasValue)
            {
                var services = await _ghnService.GetServicesAsync(order.DistrictId.Value, order.WardCode, ct);
                var defaultServiceTypeId = _configuration.GetValue<int>("GHN:DefaultServiceTypeId", 2);
                selectedServiceId = services
                    .Where(s => s.ServiceTypeId == defaultServiceTypeId)
                    .OrderBy(s => s.ServiceId)
                    .Select(s => (int?)s.ServiceId)
                    .FirstOrDefault() ?? services.Select(s => (int?)s.ServiceId).FirstOrDefault();
            }

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

            var createResult = await _ghnService.CreateOrderAsync(request, ct);
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
                    CreatedByUserId = _userManager.GetUserId(User)
                };
                _context.Shipments.Add(order.Shipment);
            }
            else
            {
                order.Shipment.Carrier = ShippingCarriers.Ghn;
                order.Shipment.TrackingCode = createResult.OrderCode;
            }

            await _context.SaveChangesAsync(ct);

            var printResult = await _ghnService.CreatePrintTokenAsync(createResult.OrderCode, ct);
            if (printResult.Success && order.Shipment != null)
            {
                order.Shipment.GhnOrderCode = createResult.OrderCode;
                order.Shipment.GhnPrintToken = printResult.Token;
                order.Shipment.GhnPrintTokenExpiredAt = DateTime.Now.AddMinutes(30);
                order.Shipment.GhnPrintFormat = "A5";
                order.Shipment.Note = order.Shipment.Note ?? "Đã tạo đơn GHN";
                await _context.SaveChangesAsync(ct);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelGhnOrder(int orderId, CancellationToken ct)
        {
            var order = await _context.Orders
                .Include(o => o.Shipment)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

            if (order == null || order.Shipment == null || string.IsNullOrWhiteSpace(order.Shipment.GhnOrderCode))
            {
                return Json(new { success = false, message = "Đơn này chưa có mã GHN để hủy." });
            }

            order.Shipment.Carrier = ShippingCarriers.Other;
            order.Shipment.TrackingCode = order.Shipment.GhnOrderCode;
            order.Shipment.GhnOrderCode = null;
            order.Shipment.GhnPrintToken = null;
            order.Shipment.GhnPrintTokenExpiredAt = null;
            order.Shipment.GhnPrintFormat = null;
            order.Shipment.Note = (order.Shipment.Note ?? "") + " | Đã hủy đơn GHN.";
            await _context.SaveChangesAsync(ct);

            return Json(new { success = true, message = "Đã hủy đơn GHN trên hệ thống." });
        }

    public async Task<IActionResult> PrintLabel(int orderId, string format = "A5", CancellationToken ct = default)
    {
        var order = await _context.Orders
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

        if (order == null || order.Shipment == null)
        {
            TempData["ShippingError"] = "Không tìm thấy đơn hàng hoặc chưa có vận đơn.";
            return RedirectToAction("Index", "AdminOrder");
        }

        if (string.IsNullOrWhiteSpace(order.Shipment.GhnOrderCode))
        {
            TempData["ShippingError"] = "Chưa có mã đơn GHN, hãy tạo đơn trước.";
            return RedirectToAction("Index", "AdminOrder");
        }

        if (string.Equals(format, "80x80", StringComparison.OrdinalIgnoreCase))
        {
            order.Shipment.GhnPrintFormat = "80x80";
            await _context.SaveChangesAsync(ct);
        }

        var printResult = await _ghnService.CreatePrintTokenAsync(order.Shipment.GhnOrderCode, ct);
        if (!printResult.Success)
        {
            TempData["ShippingError"] = printResult.Message ?? "Tạo token in thất bại.";
            return RedirectToAction("Index", "AdminOrder");
        }

        order.Shipment.GhnPrintToken = printResult.Token;
        order.Shipment.GhnPrintTokenExpiredAt = DateTime.Now.AddMinutes(30);
        order.Shipment.GhnPrintFormat = string.Equals(format, "80x80", StringComparison.OrdinalIgnoreCase) ? "80x80" : "A5";
        await _context.SaveChangesAsync(ct);

        var url = string.Equals(format, "80x80", StringComparison.OrdinalIgnoreCase)
            ? printResult.Print80x80Url
            : printResult.A5Url;

        return View("~/Views/Admin/Shipping/PrintLabel.cshtml", url);
    }

    private static string ParseShippingAddress(string address)
    {
        return address.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
