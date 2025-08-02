using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace web_ban_thuoc.Controllers.Admin
{
    [Route("admin/chat")]
    public class AdminChatController : Controller
    {
        private readonly LongChauDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public AdminChatController(LongChauDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Giao diện chat 2 cột: Index
        [HttpGet("")]
        public async Task<IActionResult> Index(string? userId = null)
        {
            // Lấy danh sách user đã nhắn tin (distinct SenderId)
            var userIds = await _context.ChatMessages
                .Where(m => m.ReceiverId == "admin")
                .Select(m => m.SenderId)
                .Distinct()
                .ToListAsync();
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            // Lấy tin nhắn gần nhất của mỗi user
            var lastMessages = userIds.Select(uid => _context.ChatMessages
                .Where(m => (m.SenderId == uid && m.ReceiverId == "admin") || (m.SenderId == "admin" && m.ReceiverId == uid))
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefault()).ToList();
            // Số tin nhắn chưa đọc
            var unreadCounts = userIds.ToDictionary(
                uid => uid,
                uid => _context.ChatMessages.Count(m => m.SenderId == uid && m.ReceiverId == "admin" && !m.IsRead)
            );
            // User đang chọn
            Microsoft.AspNetCore.Identity.IdentityUser? selectedUser = null;
            List<web_ban_thuoc.Models.ChatMessage> messages = new();
            if (!string.IsNullOrEmpty(userId))
            {
                selectedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                messages = await _context.ChatMessages
                    .Where(m => (m.SenderId == userId && m.ReceiverId == "admin") || (m.SenderId == "admin" && m.ReceiverId == userId))
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();
                // Đánh dấu đã đọc
                var unread = _context.ChatMessages.Where(m => m.SenderId == userId && m.ReceiverId == "admin" && !m.IsRead);
                foreach (var msg in unread) msg.IsRead = true;
                await _context.SaveChangesAsync();
            }
            ViewBag.Users = users;
            ViewBag.LastMessages = lastMessages;
            ViewBag.UnreadCounts = unreadCounts;
            ViewBag.SelectedUser = selectedUser;
            ViewBag.Messages = messages;
            return View("~/Views/Admin/Chat/Index.cshtml");
        }

        // Chat với user cụ thể
        [HttpGet("{userId}")]
        public async Task<IActionResult> ChatWithUser(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == userId && m.ReceiverId == "admin") || (m.SenderId == "admin" && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
            // Đánh dấu đã đọc
            var unread = _context.ChatMessages.Where(m => m.SenderId == userId && m.ReceiverId == "admin" && !m.IsRead);
            foreach (var msg in unread) msg.IsRead = true;
            await _context.SaveChangesAsync();
            ViewBag.User = user;
            ViewBag.Messages = messages;
            return View("~/Views/Admin/Chat/ChatWithUser.cshtml");
        }
    }
} 