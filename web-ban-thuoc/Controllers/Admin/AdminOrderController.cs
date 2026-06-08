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
        private readonly UserManager<IdentityUser> _userManager;

        public AdminOrderController(
            LongChauDbContext context,
            IOrderService orderService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _orderService = orderService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, string? status)
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

            ViewBag.Search = search;
            ViewBag.Status = status;
            var orderList = await orders.ToListAsync();
            return View("~/Views/Admin/Order/Index.cshtml", orderList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int orderId, string newStatus)
        {
            var adminId = _userManager.GetUserId(User);
            var result = await _orderService.ChangeStatusAsync(orderId, newStatus, adminId, "Admin cập nhật trạng thái");
            return Json(new { success = result.success, message = result.message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveShipment(int orderId, string carrier, string trackingCode, decimal? shippingFee, string? note)
        {
            if (string.IsNullOrWhiteSpace(trackingCode))
                return Json(new { success = false, message = "Vui lòng nhập mã vận đơn." });

            var order = await _context.Orders.Include(o => o.Shipment).FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

            var adminId = _userManager.GetUserId(User);
            if (order.Shipment == null)
            {
                order.Shipment = new Shipment
                {
                    OrderId = orderId,
                    Carrier = carrier,
                    TrackingCode = trackingCode.Trim(),
                    ShippingFee = shippingFee,
                    Note = note,
                    CreatedByUserId = adminId
                };
                _context.Shipments.Add(order.Shipment);
            }
            else
            {
                order.Shipment.Carrier = carrier;
                order.Shipment.TrackingCode = trackingCode.Trim();
                order.Shipment.ShippingFee = shippingFee;
                order.Shipment.Note = note;
                order.Shipment.ShippedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã lưu thông tin vận chuyển." });
        }
    }
}
