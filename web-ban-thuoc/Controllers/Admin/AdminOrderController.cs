using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;
using web_ban_thuoc.Services;

namespace web_ban_thuoc.Controllers.Admin
{
    [Authorize(Roles = "Admin,WarehouseStaff,CustomerSupport")]
    public class AdminOrderController : Controller
    {
        private readonly LongChauDbContext _context;
        private readonly IOrderService _orderService;
        private readonly IGHNService _ghnService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public AdminOrderController(
            LongChauDbContext context,
            IOrderService orderService,
            IGHNService ghnService,
            UserManager<IdentityUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _orderService = orderService;
            _ghnService = ghnService;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(o => o.StatusHistories)
                .Include(o => o.Shipment)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o => (o.FullName != null && o.FullName.Contains(search)) ||
                                           (o.Phone != null && o.Phone.Contains(search)) ||
                                           o.OrderId.ToString().Contains(search));
            }
            if (!string.IsNullOrEmpty(status) && status != "Tất cả")
                orders = orders.Where(o => o.Status == status);

            const int pageSize = 10;
            if (page < 1) page = 1;
            int totalItems = await orders.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            var orderList = await orders
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return View("~/Views/Admin/Order/Index.cshtml", orderList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.Shipment)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

            if (newStatus == OrderStatuses.Shipped)
            {
                var carrier = order.Shipment?.Carrier ?? string.Empty;
                if (carrier == ShippingCarriers.Ghn)
                {
                    if (string.IsNullOrWhiteSpace(order.Shipment?.GhnOrderCode))
                    {
                        return Json(new { success = false, message = "Vui lòng tạo đơn GHN trước khi chuyển sang Đang giao." });
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(order.Shipment?.TrackingCode))
                    {
                        return Json(new { success = false, message = "Vui lòng nhập và lưu mã vận đơn trước khi chuyển sang Đang giao." });
                    }
                }
            }

            var adminId = _userManager.GetUserId(User);
            var result = await _orderService.ChangeStatusAsync(orderId, newStatus, adminId, "Admin cập nhật trạng thái");
            return Json(new { success = result.success, message = result.message });
        }

        [HttpGet]
        public async Task<IActionResult> EditShippingAddress(int orderId, CancellationToken ct)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

            return Json(new
            {
                success = true,
                orderId = order.OrderId,
                fullName = order.FullName,
                phone = order.Phone,
                provinceId = order.ProvinceId,
                districtId = order.DistrictId,
                wardCode = order.WardCode,
                houseNumber = order.HouseNumber,
                shippingAddress = order.ShippingAddress
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShippingAddress(int orderId, [FromBody] UpdateShippingAddressModel model, CancellationToken ct)
        {
            if (orderId != model.OrderId)
            {
                return Json(new { success = false, message = "Thông tin đơn hàng không khớp." });
            }

            var order = await _context.Orders
                .Include(o => o.Shipment)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

            if (model.ProvinceId <= 0 || model.DistrictId <= 0 || string.IsNullOrWhiteSpace(model.WardCode))
                return Json(new { success = false, message = "Vui lòng chọn đầy đủ tỉnh/quận/phường." });

            var addressParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(model.HouseNumber))
                addressParts.Add(model.HouseNumber.Trim());

            var provinceName = (await _ghnService.GetProvincesAsync(ct))
                .Provinces.FirstOrDefault(p => p.ProvinceID == model.ProvinceId)?.ProvinceName;

            var districtName = (await _ghnService.GetDistrictsAsync(model.ProvinceId, ct))
                .Districts.FirstOrDefault(d => d.DistrictID == model.DistrictId)?.DistrictName;

            var wardName = (await _ghnService.GetWardsAsync(model.DistrictId, ct))
                .Wards.FirstOrDefault(w => w.WardCode == model.WardCode)?.WardName;

            if (!string.IsNullOrEmpty(provinceName))
                addressParts.Add(provinceName);
            if (!string.IsNullOrEmpty(districtName))
                addressParts.Add(districtName);
            if (!string.IsNullOrEmpty(wardName))
                addressParts.Add(wardName);

            order.FullName = model.FullName;
            order.Phone = model.Phone;
            order.ProvinceId = model.ProvinceId;
            order.DistrictId = model.DistrictId;
            order.WardCode = model.WardCode;
            order.HouseNumber = model.HouseNumber;
            order.ShippingAddress = string.Join(", ", addressParts);

            if (order.Shipment != null && order.Shipment.Carrier == ShippingCarriers.Ghn)
            {
                if (!string.IsNullOrWhiteSpace(order.Shipment.GhnOrderCode))
                {
                    order.Shipment.GhnOrderCode = null;
                    order.Shipment.GhnPrintToken = null;
                    order.Shipment.GhnPrintTokenExpiredAt = null;
                    order.Shipment.GhnPrintFormat = null;
                    order.Shipment.TrackingCode = null;
                    order.Shipment.Note = order.Shipment.Note ?? "Địa chỉ đã thay đổi, cần tạo lại đơn GHN.";
                }
            }

            await _context.SaveChangesAsync(ct);
            return Json(new { success = true, message = "Đã cập nhật địa chỉ giao hàng." });
        }

        [HttpGet]
        public async Task<IActionResult> ShippingAddressForm(int orderId, CancellationToken ct)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

            if (order == null)
            {
                TempData["ShippingError"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }

            ViewBag.OrderId = order.OrderId;
            ViewBag.FullName = order.FullName;
            ViewBag.Phone = order.Phone;
            ViewBag.ProvinceId = order.ProvinceId;
            ViewBag.DistrictId = order.DistrictId;
            ViewBag.WardCode = order.WardCode;
            ViewBag.HouseNumber = order.HouseNumber;
            return View("~/Views/Admin/Order/EditShippingAddress.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> AddressLookup(string? type, int? id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(type))
                return Json(new { success = false, message = "Thiếu loại địa chỉ." });

            var result = await _ghnService.SafeGetAddressLookupAsync(type, id, ct);
            if (!result.Success && result.Provinces.Count == 0 && result.Districts.Count == 0 && result.Wards.Count == 0)
                return Json(new { success = false, message = result.Message ?? "Không lấy được dữ liệu địa chỉ." });

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
    }

    public class UpdateShippingAddressModel
    {
        public int OrderId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public string WardCode { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
    }
}
