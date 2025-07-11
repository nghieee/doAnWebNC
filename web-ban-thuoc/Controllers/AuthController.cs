using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Tài khoản - Nhà Thuốc Long Châu";
            // Luôn trả về model mới khi GET, không nhận model lỗi
            return View("~/Views/Auth/Index.cshtml", new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(string Email, string Password, bool RememberMe = false)
        {
            _logger.LogInformation("Login attempt for email: {Email}", Email);

            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                TempData["LoginError"] = "Email và mật khẩu không được để trống";
                return RedirectToAction("Index", "Auth");
            }

            var result = await _signInManager.PasswordSignInAsync(Email, Password, RememberMe, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Login successful for email: {Email}", Email);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                _logger.LogWarning("Login failed for email: {Email}", Email);
                TempData["LoginError"] = "Email hoặc mật khẩu không đúng";
                return RedirectToAction("Index", "Auth");
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

            // Tạo user mới
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true // Tự động xác nhận email cho demo
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User registered successfully: {Email}", model.Email);
                // Tự động đăng nhập sau khi đăng ký thành công
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["SuccessMessage"] = "Đăng ký thành công!";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Registration failed for {Email}: {Errors}", model.Email, errors);
                TempData["RegisterError"] = $"Đăng ký thất bại: {errors}";
                return View("~/Views/Auth/Index.cshtml", model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}