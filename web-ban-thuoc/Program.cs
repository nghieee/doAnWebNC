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
// Đăng ký service gửi mail
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

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
builder.Services.AddHostedService<MonthlyVoucherHostedService>();

var app = builder.Build();

// Seed role và user admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Tạo role Admin nếu chưa có
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
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
