using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace web_ban_thuoc.Services
{
    public class UserRankService
    {
        private readonly LongChauDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;
        public UserRankService(LongChauDbContext context, IEmailSender emailSender, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
        }
        public async Task UpdateUserRankAndSendMailAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;
            // Lấy info cũ
            var info = await _context.UserRankInfos.FindAsync(userId);
            if (info == null)
            {
                info = new UserRankInfo { UserId = userId };
                _context.UserRankInfos.Add(info);
            }
            // Kiểm tra reset 6 tháng
            var now = DateTime.Now;
            if (info.LastRankReset == null || (now - info.LastRankReset.Value).TotalDays >= 180)
            {
                info.TotalSpent6Months = 0;
                info.Rank = "Bạc";
                info.LastRankReset = now;
            }
            // Tính tổng chi tiêu 6 tháng gần nhất
            var sixMonthsAgo = now.AddMonths(-6);
            var total6Months = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == "Đã giao" && o.OrderDate >= sixMonthsAgo)
                .SumAsync(o => o.TotalAmount ?? 0);
            info.TotalSpent6Months = total6Months;
            // Tính tổng chi tiêu toàn thời gian
            var total = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == "Đã giao")
                .SumAsync(o => o.TotalAmount ?? 0);
            info.TotalSpent = total;
            // Xác định hạng dựa trên 6 tháng gần nhất
            string newRank = "Bạc";
            if (total6Months >= 10000000) newRank = "Bạch kim";
            else if (total6Months >= 5000000) newRank = "Vàng";
            bool needSendRankMail = false;
            bool needSendNotiMail = false;
            // Nếu lên hạng mới
            if (info.Rank != newRank)
            {
                needSendRankMail = true;
                info.Rank = newRank;
                info.LastRankMailSent = DateTime.Now;
                // Tặng voucher khi lên rank Vàng/Bạch kim
                if (newRank == "Vàng" || newRank == "Bạch kim")
                {
                    decimal percent = newRank == "Vàng" ? 10 : 15;
                    string rankCode = newRank == "Vàng" ? "VANG" : "BK";
                    string code = await GenerateUniqueVoucherCodeAsync(rankCode, 6);
                    var voucher = new Voucher
                    {
                        Code = code,
                        Description = $"Voucher giảm {percent}% tổng đơn cho thành viên {newRank}",
                        DiscountType = "FullOrder",
                        PercentValue = percent,
                        DiscountAmount = null,
                        Detail = "Áp dụng cho toàn bộ đơn hàng. Không giới hạn giá trị đơn hàng.",
                        ExpiryDate = DateTime.Now.AddMonths(1),
                        IsActive = true
                    };
                    _context.Vouchers.Add(voucher);
                    await _context.SaveChangesAsync();
                    var userVoucher = new UserVoucher
                    {
                        UserId = user.Id,
                        VoucherId = voucher.VoucherId,
                        IsUsed = false
                    };
                    _context.UserVouchers.Add(userVoucher);
                    await _context.SaveChangesAsync();
                }
            }
            // Nếu là rank Bạc và phát sinh đơn hàng đầu tiên thì tặng voucher 5%
            if (info.Rank == "Bạc")
            {
                // Đếm số đơn đã giao
                var countOrder = await _context.Orders.CountAsync(o => o.UserId == userId && o.Status == "Đã giao");
                if (countOrder == 1)
                {
                    // Kiểm tra đã tặng voucher chưa
                    var exist = await _context.UserVouchers.Include(uv => uv.Voucher)
                        .AnyAsync(uv => uv.UserId == userId && uv.Voucher.DiscountType == "Percent" && uv.Voucher.PercentValue == 5);
                    if (!exist)
                    {
                        string code = await GenerateUniqueVoucherCodeAsync("BAC", 6);
                        var voucher = new Voucher
                        {
                            Code = code,
                            Description = "Voucher giảm 5% tổng đơn cho thành viên Bạc (đơn đầu tiên)",
                            DiscountType = "FullOrder",
                            PercentValue = 5,
                            DiscountAmount = null,
                            Detail = "Áp dụng cho toàn bộ đơn hàng. Không giới hạn giá trị đơn hàng. Chỉ tặng khi phát sinh đơn hàng đầu tiên.",
                            ExpiryDate = DateTime.Now.AddMonths(1),
                            IsActive = true
                        };
                        _context.Vouchers.Add(voucher);
                        await _context.SaveChangesAsync();
                        var userVoucher = new UserVoucher
                        {
                            UserId = user.Id,
                            VoucherId = voucher.VoucherId,
                            IsUsed = false
                        };
                        _context.UserVouchers.Add(userVoucher);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            await _context.SaveChangesAsync();
            // Gửi mail nếu cần
            if (needSendRankMail)
            {
                string subject = $"Chúc mừng bạn đã lên hạng {newRank}!";
                string body = $@"
                <div style='font-family:sans-serif;max-width:500px;margin:auto;background:#f4f6fb;padding:32px 24px;border-radius:12px;'>
                    <div style='text-align:center;margin-bottom:24px;'>
                        <img src='https://cdn.nhathuoclongchau.com.vn/unsafe/https://cms-prod.s3-sgn09.fptcloud.com/smalls/logo_default_web_78584a5cc6.png' alt='Nhà Thuốc Long Châu' style='height:60px;margin-bottom:12px;'>
                        <h2 style='color:#1976d2;'>Chúc mừng bạn đã lên hạng <span style='color:#ff9800'>{newRank}</span>!</h2>
                    </div>
                    <div style='background:#fff;padding:24px 20px;border-radius:10px;box-shadow:0 2px 8px #e3e8f0;'>
                        <p style='font-size:16px;color:#333;'>Xin chúc mừng, bạn đã đạt hạng <b style='color:#1976d2'>{newRank}</b> trên hệ thống!</p>
                        <p style='font-size:15px;color:#444;'>Bạn nhận được <b>voucher ưu đãi đặc biệt</b> dành riêng cho hạng {newRank}.<br>Hãy kiểm tra tài khoản để sử dụng ngay!</p>
                        <div style='text-align:center;margin:24px 0;'>
                            <a href='https://localhost:5001/Auth/Profile' style='display:inline-block;padding:12px 32px;background:#1976d2;color:#fff;font-weight:bold;border-radius:6px;text-decoration:none;font-size:16px;'>Khám phá ưu đãi</a>
                        </div>
                    </div>
                    <p style='font-size:13px;color:#888;text-align:center;margin-top:32px;'>Cảm ơn bạn đã đồng hành cùng Nhà Thuốc Long Châu!</p>
                </div>";
                await _emailSender.SendEmailAsync(user.Email, subject, body);
            }
            else if (needSendNotiMail)
            {
                string nextRank = info.Rank == "Bạc" ? "Vàng" : "Bạch kim";
                string subject = $"Bạn sắp đạt hạng {nextRank}!";
                string body = $@"
                <div style='font-family:sans-serif;max-width:500px;margin:auto;background:#f4f6fb;padding:32px 24px;border-radius:12px;'>
                    <div style='text-align:center;margin-bottom:24px;'>
                        <img src='https://cdn.nhathuoclongchau.com.vn/unsafe/https://cms-prod.s3-sgn09.fptcloud.com/smalls/logo_default_web_78584a5cc6.png' alt='Nhà Thuốc Long Châu' style='height:60px;margin-bottom:12px;'>
                        <h2 style='color:#1976d2;'>Bạn sắp đạt hạng <span style='color:#ff9800'>{nextRank}</span>!</h2>
                    </div>
                    <div style='background:#fff;padding:24px 20px;border-radius:10px;box-shadow:0 2px 8px #e3e8f0;'>
                        <p style='font-size:16px;color:#333;'>Bạn chỉ còn thiếu <b style='color:#e53935'>{(info.Rank == "Bạc" ? 5000000 - total6Months : 10000000 - total6Months):N0}đ</b> để lên hạng <b style='color:#1976d2'>{nextRank}</b> và nhận voucher ưu đãi.</p>
                        <p style='font-size:15px;color:#444;'>Hãy tiếp tục mua sắm để không bỏ lỡ ưu đãi hấp dẫn nhé!</p>
                        <div style='text-align:center;margin:24px 0;'>
                            <a href='https://localhost:5001/Auth/Profile' style='display:inline-block;padding:12px 32px;background:#1976d2;color:#fff;font-weight:bold;border-radius:6px;text-decoration:none;font-size:16px;'>Mua sắm ngay</a>
                        </div>
                    </div>
                    <p style='font-size:13px;color:#888;text-align:center;margin-top:32px;'>Cảm ơn bạn đã đồng hành cùng Nhà Thuốc Long Châu!</p>
                </div>";
                await _emailSender.SendEmailAsync(user.Email, subject, body);
            }
        }
        public async Task GrantMonthlyVoucherAndSendMailAsync()
        {
            var now = DateTime.Now;
            var users = await _userManager.Users.ToListAsync();
            foreach (var user in users)
            {
                var info = await _context.UserRankInfos.FindAsync(user.Id);
                if (info == null) continue;
                // Kiểm tra đã tặng voucher tháng này chưa
                var month = now.Month;
                var year = now.Year;
                string rankCode = info.Rank == "Bạch kim" ? "BK" : (info.Rank == "Vàng" ? "VANG" : "BAC");
                string code = await GenerateUniqueVoucherCodeAsync(rankCode, 6);
                var exist = await _context.UserVouchers.Include(uv => uv.Voucher)
                    .AnyAsync(uv => uv.UserId == user.Id && uv.Voucher.Code == code);
                if (exist) continue;
                // Tạo voucher mới cho từng rank
                string desc = info.Rank switch {
                    "Bạch kim" => "Voucher 100K cho thành viên Bạch kim tháng " + month,
                    "Vàng" => "Voucher 50K cho thành viên Vàng tháng " + month,
                    _ => "Voucher 30K cho thành viên Bạc tháng " + month
                };
                decimal amount = info.Rank switch {
                    "Bạch kim" => 100000,
                    "Vàng" => 50000,
                    _ => 30000
                };
                var voucher = new Voucher
                {
                    Code = code,
                    Description = desc,
                    DiscountAmount = amount,
                    ExpiryDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1),
                    IsActive = true
                };
                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();
                var userVoucher = new UserVoucher
                {
                    UserId = user.Id,
                    VoucherId = voucher.VoucherId,
                    IsUsed = false
                };
                _context.UserVouchers.Add(userVoucher);
                await _context.SaveChangesAsync();
                // Gửi email
                string subject = $"Tặng bạn voucher ưu đãi tháng {month}!";
                string body = $@"
                <div style='font-family:sans-serif;max-width:500px;margin:auto;background:#f4f6fb;padding:32px 24px;border-radius:12px;'>
                    <div style='text-align:center;margin-bottom:24px;'>
                        <img src='https://cdn.nhathuoclongchau.com.vn/unsafe/https://cms-prod.s3-sgn09.fptcloud.com/smalls/logo_default_web_78584a5cc6.png' alt='Nhà Thuốc Long Châu' style='height:60px;margin-bottom:12px;'>
                        <h2 style='color:#1976d2;'>Tặng bạn voucher ưu đãi tháng {month}!</h2>
                    </div>
                    <div style='background:#fff;padding:24px 20px;border-radius:10px;box-shadow:0 2px 8px #e3e8f0;'>
                        <p style='font-size:16px;color:#333;'>Chào <b>{user.UserName}</b>,</p>
                        <p style='font-size:15px;color:#444;'>Nhà thuốc Long Châu tặng bạn <b style='color:#1976d2'>{desc}</b> trị giá <b style='color:#e53935'>{amount:N0}đ</b> sử dụng đến hết ngày <b>{voucher.ExpiryDate:dd/MM/yyyy}</b>.</p>
                        <div style='margin:24px 0;text-align:center;'>
                            <div style='display:inline-block;background:#1976d2;color:#fff;font-size:20px;font-weight:bold;padding:12px 32px;border-radius:8px;letter-spacing:2px;'>{code}</div>
                        </div>
                        <p style='font-size:14px;color:#888;text-align:center;'>Đăng nhập tài khoản để sử dụng voucher ngay!</p>
                        <div style='text-align:center;margin:24px 0;'>
                            <a href='https://localhost:5001/Auth/Profile' style='display:inline-block;padding:12px 32px;background:#1976d2;color:#fff;font-weight:bold;border-radius:6px;text-decoration:none;font-size:16px;'>Mua sắm ngay</a>
                        </div>
                    </div>
                    <p style='font-size:13px;color:#888;text-align:center;margin-top:32px;'>Cảm ơn bạn đã đồng hành cùng Nhà Thuốc Long Châu!</p>
                </div>";
                await _emailSender.SendEmailAsync(user.Email, subject, body);
            }
        }
        // Hàm sinh mã voucher ngẫu nhiên
        private static string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }
            return sb.ToString();
        }

        private async Task<string> GenerateUniqueVoucherCodeAsync(string prefix, int length)
        {
            string code;
            do
            {
                code = prefix + GenerateRandomCode(length);
            } while (await _context.Vouchers.AnyAsync(v => v.Code == code));
            return code;
        }
    }

    public class MonthlyVoucherHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        public MonthlyVoucherHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                // Chỉ chạy vào ngày đầu tháng hoặc khi app khởi động lần đầu trong tháng
                if (now.Day == 1)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var rankService = scope.ServiceProvider.GetRequiredService<UserRankService>();
                        await rankService.GrantMonthlyVoucherAndSendMailAsync();
                    }
                }
                // Chờ đến 6h sáng ngày hôm sau
                var next = now.Date.AddDays(1).AddHours(6);
                var delay = next - DateTime.Now;
                if (delay.TotalMilliseconds > 0)
                    await Task.Delay(delay, stoppingToken);
                else
                    await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
    }
} 