using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers.Admin
{
    public class AdminOrderController : Controller
    {
        private readonly LongChauDbContext _context;
        public AdminOrderController(LongChauDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            var orders = _context.Orders
                .Where(o => o.Status != "Cart")
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.ProductImages)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o => (o.FullName != null && o.FullName.Contains(search)) ||
                                             (o.Phone != null && o.Phone.Contains(search)) ||
                                             (o.OrderId.ToString().Contains(search)));
            }
            if (!string.IsNullOrEmpty(status) && status != "Tất cả")
            {
                orders = orders.Where(o => o.Status == status);
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            var orderList = await orders.ToListAsync();
            return View("~/Views/Admin/Order/Index.cshtml", orderList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int orderId, string newStatus)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                    return Json(new { success = false, message = "Order null" });
                if (order.Status == "Cart" || order.Status == "Đã hủy")
                    return Json(new { success = false, message = "Không thể thay đổi trạng thái đơn hàng này!" });

                if (newStatus == "Chờ xác nhận" && order.Status != "Chờ xác nhận")
                    return Json(new { success = false, message = "Không thể chuyển lại trạng thái 'Chờ xác nhận'!" });

                if (newStatus == "Đã hủy")
                    return Json(new { success = false, message = "Không thể chuyển trạng thái sang 'Đã hủy' từ phía admin!" });

                order.Status = newStatus;

                // Nếu là COD và chuyển sang 'Đã giao' thì cập nhật PaymentStatus cả Order và tất cả Payment liên quan
                if (order.Status == "Đã giao" && (string.IsNullOrEmpty(order.PaymentStatus) || order.PaymentStatus == "Chưa thanh toán"))
                {
                    if (order.Payments == null)
                    {
                        // Thử lấy lại từ DB nếu navigation property bị null
                        order.Payments = await _context.Payments.Where(p => p.OrderId == order.OrderId).ToListAsync();
                    }
                    if (order.Payments == null)
                        return Json(new { success = false, message = "Payments null" });

                    var payments = order.Payments.Where(p => p != null && (p.PaymentMethod ?? "").Equals("COD", StringComparison.OrdinalIgnoreCase)).ToList();
                    bool updated = false;
                    if (payments.Count > 0)
                    {
                        foreach (var payment in payments)
                        {
                            if (payment == null)
                                return Json(new { success = false, message = "Có payment null trong danh sách" });
                            payment.PaymentStatus = "Đã thanh toán";
                            updated = true;
                        }
                        if (updated)
                            order.PaymentStatus = "Đã thanh toán";
                    }
                    else
                    {
                        // Không có payment nào, vẫn cập nhật Order.PaymentStatus để không bị null
                        order.PaymentStatus = "Đã thanh toán";
                    }
                }

                // Khi chuyển sang 'Đã giao', cập nhật SoldQuantity cho từng sản phẩm
                if (order.Status == "Đã giao")
                {
                    // Lấy lại OrderItems nếu chưa có
                    var orderItems = await _context.OrderItems.Where(oi => oi.OrderId == order.OrderId).ToListAsync();
                    foreach (var item in orderItems)
                    {
                        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                        if (product != null)
                        {
                            product.SoldQuantity = (product.SoldQuantity ?? 0) + item.Quantity;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã cập nhật trạng thái đơn hàng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "") });
            }
        }
    }
} 