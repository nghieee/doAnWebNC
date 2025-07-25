using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web_ban_thuoc.Models;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using web_ban_thuoc.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace web_ban_thuoc.Controllers
{
public class AuthController : Controller
{
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly LongChauDbContext _context;

        public AuthController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ILogger<AuthController> logger, IEmailSender emailSender, LongChauDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
        }

        [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Tài khoản - Nhà Thuốc Long Châu";
            // Luôn trả về model mới khi GET, không nhận model lỗi
            return View("~/Views/Auth/Index.cshtml", new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            _logger.LogInformation("Login attempt for email: {Email}", model.Email);

            if (!ModelState.IsValid)
            {
                TempData["LoginError"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return View("~/Views/Auth/Index.cshtml", new RegisterViewModel());
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.EmailConfirmed)
            {
                TempData["LoginError"] = "Tài khoản chưa xác nhận email hoặc không tồn tại.";
                ViewBag.LoginModel = model;
                return View("~/Views/Auth/Index.cshtml", new RegisterViewModel());
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("Login successful for email: {Email}", model.Email);
                var loggedInUser = await _userManager.FindByEmailAsync(model.Email);
                if (await _userManager.IsInRoleAsync(loggedInUser, "Admin"))
                {
                    return RedirectToAction("Index", "AdminHome");
                }
                return RedirectToAction("Index", "Home");
            }
            else
            {
                _logger.LogWarning("Login failed for email: {Email}", model.Email);
                TempData["LoginError"] = "Email hoặc mật khẩu không đúng";
                ViewBag.LoginModel = model;
                return View("~/Views/Auth/Index.cshtml", new RegisterViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            _logger.LogInformation("Register attempt for email: {Email}, username: {UserName}", model.Email, model.UserName);

            if (!ModelState.IsValid)
            {
                TempData["RegisterError"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return View("~/Views/Auth/Index.cshtml", model);
            }

            // Kiểm tra email đã tồn tại chưa
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                TempData["RegisterError"] = "Email đã được sử dụng";
                return View("~/Views/Auth/Index.cshtml", model);
            }

            // Tạo user mới với EmailConfirmed = false
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Sinh token xác nhận email
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var tokenBytes = Encoding.UTF8.GetBytes(token);
                var encodedToken = WebEncoders.Base64UrlEncode(tokenBytes);
                var callbackUrl = Url.Action("ConfirmEmail", "Auth", new { userId = user.Id, token = encodedToken }, protocol: Request.Scheme);
                var htmlMessage = $@"
<table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f6fb;padding:0;margin:0;'>
  <tr>
    <td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='background:#fff;border-radius:10px;margin:40px 0;'>
        <tr>
          <td align='center' style='padding:32px 0 16px 0; background:#fff;'>
            <div style='display:inline-block;background:#fff;padding:12px 24px;border-radius:8px;'>
              <img src='https://cdn.nhathuoclongchau.com.vn/unsafe/https://cms-prod.s3-sgn09.fptcloud.com/smalls/logo_default_web_78584a5cc6.png' alt='Nhà Thuốc Long Châu' style='height:60px;display:block;background:#fff;' />
            </div>
          </td>
        </tr>
        <tr>
          <td align='center' style='padding:0 32px 0 32px;'>
            <h2 style='color:#1976d2;font-family:sans-serif;'>Xác nhận đăng ký tài khoản</h2>
            <p style='font-size:16px;color:#333;font-family:sans-serif;'>
              Chào <b>{HtmlEncoder.Default.Encode(model.UserName)}</b>,<br/>
              Cảm ơn bạn đã đăng ký tài khoản tại <b>Nhà Thuốc Long Châu</b>.<br/>
              Vui lòng bấm nút bên dưới để xác nhận tài khoản và hoàn tất đăng ký.
            </p>
            <a href='{callbackUrl}' style='display:inline-block;margin:24px 0 16px 0;padding:14px 32px;background:#1976d2;color:#fff;font-size:18px;font-weight:bold;border-radius:6px;text-decoration:none;font-family:sans-serif;'>Xác nhận tài khoản</a>
            <p style='font-size:14px;color:#888;font-family:sans-serif;'>Nếu bạn không đăng ký tài khoản, vui lòng bỏ qua email này.</p>
          </td>
        </tr>
        <tr>
          <td align='center' style='padding:24px 0 0 0;'>
            <hr style='border:none;border-top:1px solid #eee;width:80%;margin:0 auto 16px auto;'>
            <p style='font-size:12px;color:#aaa;font-family:sans-serif;'>
              © {DateTime.Now.Year} Nhà Thuốc Long Châu. Mọi quyền được bảo lưu.<br/>
              Liên hệ: <a href='mailto:nowrgan@gmail.com' style='color:#1976d2;text-decoration:none;'>nowrgan@gmail.com</a>
            </p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>
";
                await _emailSender.SendEmailAsync(model.Email, "Xác nhận đăng ký tài khoản Nhà Thuốc Long Châu", htmlMessage);
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.";
                return RedirectToAction("Index", "Auth");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Registration failed for {Email}: {Errors}", model.Email, errors);
                TempData["RegisterError"] = $"Đăng ký thất bại: {errors}";
                return View("~/Views/Auth/Index.cshtml", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                TempData["RegisterError"] = "Liên kết xác nhận không hợp lệ.";
                return RedirectToAction("Index", "Auth");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["RegisterError"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Index", "Auth");
            }
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Xác nhận email thành công! Bạn có thể đăng nhập.";
            }
            else
            {
                TempData["RegisterError"] = "Xác nhận email thất bại hoặc đã xác nhận trước đó.";
            }
            return RedirectToAction("Index", "Auth");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index", "Auth");
            
            // Lấy lịch sử đơn hàng của user (loại trừ giỏ hàng - Status = "Cart")
            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id && o.Status != "Cart")
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.ProductImages)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderViewModels = orders.Select(o => new OrderHistoryViewModel
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate ?? DateTime.Now,
                TotalAmount = o.TotalAmount ?? 0,
                Status = o.Status ?? "",
                PaymentStatus = o.PaymentStatus ?? "",
                ShippingAddress = o.ShippingAddress ?? "",
                FullName = o.FullName ?? "",
                Phone = o.Phone ?? "",
                Items = o.OrderItems.Select(oi => new OrderItemViewModel
                {
                    ProductId = oi.ProductId ?? 0,
                    ProductName = oi.Product?.ProductName ?? "",
                    ImageUrl = oi.Product?.ProductImages?.FirstOrDefault(pi => pi.IsMain == true)?.ImageUrl ?? 
                               oi.Product?.ProductImages?.FirstOrDefault()?.ImageUrl ?? "sanpham.png",
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            }).ToList();

            var vm = new ProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Orders = orderViewModels
            };
            return View("~/Views/Auth/Profile.cshtml", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index", "Auth");
            bool requireVerification = false;
            string verificationType = null;
            string code = null;
            if (!string.IsNullOrEmpty(model.NewEmail) && model.NewEmail != user.Email)
            {
                // Gửi mã xác nhận email
                code = new Random().Next(100000, 999999).ToString();
                TempData["EmailVerificationCode"] = code;
                TempData["NewEmail"] = model.NewEmail;
                await _emailSender.SendEmailAsync(model.NewEmail, "Xác nhận đổi email Nhà Thuốc Long Châu", $"Mã xác nhận đổi email của bạn là: <b>{code}</b>");
                requireVerification = true;
                verificationType = "email";
            }
            else if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp");
                    return View("~/Views/Auth/Profile.cshtml", model);
                }
                // Kiểm tra mật khẩu cũ
                if (string.IsNullOrEmpty(model.CurrentPassword) || !await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu cũ không đúng");
                    TempData["ShowToast"] = true;
                    TempData["ProfileError"] = "Mật khẩu cũ không đúng!";
                    return View("~/Views/Auth/Profile.cshtml", model);
                }
                // Gửi mã xác nhận đổi mật khẩu
                code = new Random().Next(100000, 999999).ToString();
                TempData["PasswordVerificationCode"] = code;
                TempData["NewPassword"] = model.NewPassword;
                var htmlMessage = $@"
<table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f6fb;padding:0;margin:0;'>
  <tr>
    <td align='center'>
      <table width='480' cellpadding='0' cellspacing='0' style='background:#fff;border-radius:10px;margin:40px 0;'>
        <tr>
          <td align='center' style='padding:32px 0 16px 0; background:#fff;'>
            <div style='display:inline-block;background:#fff;padding:12px 24px;border-radius:8px;'>
              <img src='https://nhathuoclongchau.com.vn/estore-images/profile/v2/avatar-profile-large.svg' alt='Nhà Thuốc Long Châu' style='height:60px;display:block;background:#fff;' />
            </div>
          </td>
        </tr>
        <tr>
          <td align='center' style='padding:0 32px 0 32px;'>
            <h2 style='color:#1976d2;font-family:sans-serif;'>Xác nhận đổi mật khẩu</h2>
            <p style='font-size:16px;color:#333;font-family:sans-serif;'>
              Bạn vừa yêu cầu đổi mật khẩu tài khoản tại <b>Nhà Thuốc Long Châu</b>.<br/>
              Vui lòng nhập mã xác nhận bên dưới để hoàn tất đổi mật khẩu:
            </p>
            <div style='margin:24px 0 16px 0;'>
              <span style='display:inline-block;font-size:2rem;letter-spacing:12px;font-weight:bold;color:#1976d2;background:#eaeffa;padding:12px 24px;border-radius:8px;'>{code}</span>
            </div>
            <p style='font-size:14px;color:#888;font-family:sans-serif;'>Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email này.</p>
          </td>
        </tr>
        <tr>
          <td align='center' style='padding:24px 0 0 0;'>
            <hr style='border:none;border-top:1px solid #eee;width:80%;margin:0 auto 16px auto;'>
            <p style='font-size:12px;color:#aaa;font-family:sans-serif;'>
              © {DateTime.Now.Year} Nhà Thuốc Long Châu. Mọi quyền được bảo lưu.<br/>
              Liên hệ: <a href='mailto:nowrgan@gmail.com' style='color:#1976d2;text-decoration:none;'>nowrgan@gmail.com</a>
            </p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>
";
                await _emailSender.SendEmailAsync(user.Email, "Xác nhận đổi mật khẩu Nhà Thuốc Long Châu", htmlMessage);
                requireVerification = true;
                verificationType = "password";
            }
            else
            {
                // Cập nhật tên, số điện thoại
                user.UserName = model.UserName;
                user.PhoneNumber = model.PhoneNumber;
                await _userManager.UpdateAsync(user);
                TempData["ProfileSuccess"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }
            // Nếu cần xác thực, render lại form với trạng thái xác thực
            model.RequireVerification = requireVerification;
            model.VerificationType = verificationType;
            return View("~/Views/Auth/Profile.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyProfile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index", "Auth");
            if (model.VerificationType == "email")
            {
                var code = TempData["EmailVerificationCode"] as string;
                var newEmail = TempData["NewEmail"] as string;
                if (model.VerificationCode == code && !string.IsNullOrEmpty(newEmail))
                {
                    user.Email = newEmail;
                    user.UserName = newEmail;
                    await _userManager.UpdateAsync(user);
                    TempData["ProfileSuccess"] = "Đổi email thành công!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    ModelState.AddModelError("VerificationCode", "Mã xác nhận không đúng hoặc đã hết hạn");
                    model.RequireVerification = true;
                    model.VerificationType = "email";
                    return View("~/Views/Auth/Profile.cshtml", model);
                }
            }
            else if (model.VerificationType == "password")
            {
                var code = TempData["PasswordVerificationCode"] as string;
                var newPassword = TempData["NewPassword"] as string;
                if (model.VerificationCode == code && !string.IsNullOrEmpty(newPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    await _userManager.ResetPasswordAsync(user, token, newPassword);
                    TempData["ProfileSuccess"] = "Đổi mật khẩu thành công!";
                    TempData["ShowToast"] = true;
                    return RedirectToAction("Profile");
                }
                else
                {
                    ModelState.AddModelError("VerificationCode", "Mã xác nhận không đúng hoặc đã hết hạn");
                    model.RequireVerification = true;
                    model.VerificationType = "password";
                    return View("~/Views/Auth/Profile.cshtml", model);
                }
            }
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập email.";
                return View();
            }
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "Email không tồn tại trong hệ thống.";
                return View();
            }
            // Sinh mã 6 số
            var code = new Random().Next(100000, 999999).ToString();
            // Lưu mã vào Session (an toàn khi redirect)
            HttpContext.Session.SetString("ResetCode", code);
            HttpContext.Session.SetString("ResetEmail", email);
            // Gửi email
            var emailSender = HttpContext.RequestServices.GetService(typeof(Services.IEmailSender)) as Services.IEmailSender;
            await emailSender.SendEmailAsync(email, "Mã xác nhận đặt lại mật khẩu", $"Mã xác nhận của bạn là: <b>{code}</b>");
            // Chuyển sang trang nhập mã xác nhận
            return RedirectToAction("VerifyResetCode");
        }

        [HttpGet]
        public IActionResult VerifyResetCode()
        {
            return View("VerifyResetCode");
        }

        [HttpPost]
        public async Task<IActionResult> VerifyResetCode(string code, string newPassword, string confirmPassword)
        {
            var expectedCode = HttpContext.Session.GetString("ResetCode");
            var email = HttpContext.Session.GetString("ResetEmail");
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View("VerifyResetCode");
            }
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View("VerifyResetCode");
            }
            if (expectedCode == null || email == null)
            {
                ViewBag.Error = "Phiên xác nhận đã hết hạn. Vui lòng thử lại.";
                return RedirectToAction("ForgotPassword");
            }
            if (code != expectedCode)
            {
                ViewBag.Error = "Mã xác nhận không đúng.";
                return View("VerifyResetCode");
            }
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "Không tìm thấy tài khoản.";
                return View("VerifyResetCode");
            }
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Bạn có thể đăng nhập với mật khẩu mới.";
                // Xóa session sau khi thành công
                HttpContext.Session.Remove("ResetCode");
                HttpContext.Session.Remove("ResetEmail");
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Error = string.Join("<br>", result.Errors.Select(e => e.Description));
                return View("VerifyResetCode");
            }
        }
    }
}