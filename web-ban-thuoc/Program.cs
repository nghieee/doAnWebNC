using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;
using Microsoft.AspNetCore.Identity;
using web_ban_thuoc.Services;

var builder = WebApplication.CreateBuilder(args);

// Register the LongChauDbContext with dependency injection
builder.Services.AddDbContext<LongChauDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Thêm cấu hình Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // ... các option khác nếu có ...
})
    .AddEntityFrameworkStores<LongChauDbContext>()
    .AddErrorDescriber<web_ban_thuoc.Services.CustomIdentityErrorDescriber>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// Đăng ký cấu hình EmailSettings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
// Đăng ký service gửi mail: hoán đổi Strategy theo môi trường tại DI Container (Dev = Null, Prod = SMTP) + Decorator (LoggingEmailSender)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<IEmailSender>(sp =>
        new LoggingEmailSender(new NullEmailSender(), sp.GetRequiredService<ILogger<LoggingEmailSender>>()));
}
else
{
    builder.Services.AddTransient<IEmailSender>(sp =>
        new LoggingEmailSender(new SmtpEmailSender(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailSettings>>()), sp.GetRequiredService<ILogger<LoggingEmailSender>>()));
}

// Thêm cấu hình LoginPath cho cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Index";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});

// Register the NavbarFilter as a scoped service
builder.Services.AddScoped<NavbarFilter>();
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<NavbarFilter>());

// Thêm vào trước builder.Build()
builder.Services.AddSession();
builder.Services.AddSignalR();
builder.Services.AddScoped<UserRankService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
builder.Services.AddScoped<IOrderNotificationService, OrderNotificationService>();
builder.Services.AddScoped<IPayOSWebhookProcessor, PayOSWebhookProcessor>();
builder.Services.AddScoped<IProductExcelImportService, ProductExcelImportService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddHostedService<MonthlyVoucherHostedService>();

// Đăng ký PayOS Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPayOSService, PayOSService>();
builder.Services.AddScoped<IOrderEmailService, OrderEmailService>();

var app = builder.Build();

// Dev: tự apply migration + seed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<LongChauDbContext>();
    if (app.Environment.IsDevelopment())
        await db.Database.MigrateAsync();

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var roleName in new[] { "Admin", "WarehouseStaff", "CustomerSupport" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
    }

    // Tạo tài khoản admin nếu chưa có
    var adminEmail = "admin@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Admin123."); // Đặt mật khẩu mạnh
    }

    // Gán role Admin cho tài khoản này
    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    await SeedStaffAccountAsync(userManager, "warehouse@longchau.local", "Kho123456.", StaffRoles.WarehouseStaff);
    await SeedStaffAccountAsync(userManager, "support@longchau.local", "Support123.", StaffRoles.CustomerSupport);

    if (!await db.Suppliers.AnyAsync())
    {
        db.Suppliers.Add(new Supplier
        {
            Code = "NCC-MAC-DINH",
            Name = "Nhà cung cấp mặc định",
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    if (!await db.LoyaltyRewards.AnyAsync())
    {
        db.LoyaltyRewards.AddRange(
            new LoyaltyReward
            {
                Title = "Voucher giảm 30.000đ",
                Description = "Áp dụng cho đơn từ 200.000đ",
                PointsCost = 300,
                RewardType = LoyaltyRewardTypes.VoucherFixed,
                DiscountAmount = 30_000,
                MinOrderAmount = 200_000,
                ExpiryDays = 30,
                StockRemaining = 500,
                MaxPerUser = 3,
                SortOrder = 1,
                IsActive = true
            },
            new LoyaltyReward
            {
                Title = "Voucher giảm 5%",
                Description = "Giảm 5% tổng đơn, tối đa không giới hạn đơn tối thiểu",
                PointsCost = 500,
                RewardType = LoyaltyRewardTypes.VoucherPercent,
                PercentValue = 5,
                ExpiryDays = 14,
                MaxPerUser = 2,
                SortOrder = 2,
                IsActive = true
            },
            new LoyaltyReward
            {
                Title = "Voucher 100.000đ (Vàng+)",
                Description = "Dành thành viên Vàng trở lên",
                PointsCost = 1000,
                RewardType = LoyaltyRewardTypes.VoucherFixed,
                DiscountAmount = 100_000,
                MinOrderAmount = 500_000,
                RequiredRank = LoyaltyTiers.Gold,
                ExpiryDays = 30,
                StockRemaining = 100,
                MaxPerUser = 1,
                SortOrder = 3,
                IsActive = true
            });
        await db.SaveChangesAsync();
    }
}

static async Task SeedStaffAccountAsync(UserManager<IdentityUser> userManager, string email, string password, string role)
{
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user, password);
    }

    if (!await userManager.IsInRoleAsync(user, role))
        await userManager.AddToRoleAsync(user, role);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thêm UseAuthentication trước UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

// Thêm vào pipeline, sau UseRouting, trước UseEndpoints hoặc UseAuthorization
app.UseSession();

app.UseEndpoints(endpoints =>
{
    // Route cho admin
    endpoints.MapControllerRoute(
        name: "admin",
        pattern: "admin/{action=Index}/{id?}",
        defaults: new { controller = "AdminHome", action = "Index" }
    );
    // Route mặc định
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    // Endpoint cho SignalR ChatHub
    endpoints.MapHub<web_ban_thuoc.ChatHub>("/chathub");
});

// Thêm route cho Identity UI
app.MapRazorPages();

app.Run();
