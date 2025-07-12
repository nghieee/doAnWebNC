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

// Register the NavbarFilter as a scoped service
builder.Services.AddScoped<NavbarFilter>();
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<NavbarFilter>());

// Thêm vào trước builder.Build()
builder.Services.AddSession();

var app = builder.Build();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Thêm route cho Identity UI
app.MapRazorPages();

app.Run();
