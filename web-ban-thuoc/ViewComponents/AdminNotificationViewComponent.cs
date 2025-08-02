using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.ViewComponents
{
    public class AdminNotificationViewComponent : ViewComponent
    {
        private readonly LongChauDbContext _context;

        public AdminNotificationViewComponent(LongChauDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Chờ xác nhận");
            var unreadMessages = await _context.ChatMessages.CountAsync(m => m.ReceiverId == "admin" && !m.IsRead);

            var model = new AdminNotificationModel
            {
                PendingOrders = pendingOrders,
                UnreadMessages = unreadMessages
            };

            return View(model);
        }
    }

    public class AdminNotificationModel
    {
        public int PendingOrders { get; set; }
        public int UnreadMessages { get; set; }
    }
} 