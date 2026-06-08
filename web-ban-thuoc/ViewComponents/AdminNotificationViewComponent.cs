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
            var user = HttpContext.User;
            var canSeeOrders = user.IsInRole(StaffRoles.Admin)
                || user.IsInRole(StaffRoles.WarehouseStaff)
                || user.IsInRole(StaffRoles.CustomerSupport);
            var canSeeChat = user.IsInRole(StaffRoles.Admin)
                || user.IsInRole(StaffRoles.CustomerSupport);

            var pendingOrders = canSeeOrders
                ? await _context.Orders.CountAsync(o => o.Status == OrderStatuses.PendingConfirmation)
                : 0;
            var unreadMessages = canSeeChat
                ? await _context.ChatMessages.CountAsync(m => m.ReceiverId == "admin" && !m.IsRead)
                : 0;

            return View(new AdminNotificationModel
            {
                PendingOrders = pendingOrders,
                UnreadMessages = unreadMessages
            });
        }
    }

    public class AdminNotificationModel
    {
        public int PendingOrders { get; set; }
        public int UnreadMessages { get; set; }
    }
} 