# 📚 HƯỚNG DẪN HỌC CODE - NHÀ THUỐC LONG CHÂU

## 🎯 Mục tiêu tài liệu

Tài liệu này cung cấp hướng dẫn chi tiết để hiểu và học code từ dự án **Nhà Thuốc Long Châu**. Mỗi phần sẽ giải thích cơ chế hoạt động, cách implement và best practices.

---

## 🏗️ **KIẾN TRÚC TỔNG QUAN**

### **1. MVC Pattern (Model-View-Controller)**

```csharp
// Ví dụ: ProductController.cs
public class ProductController : Controller
{
    private readonly LongChauDbContext _context;
    
    // Controller nhận request và xử lý logic
    public IActionResult Details(int id)
    {
        // 1. Lấy dữ liệu từ Model
        var product = _context.Products.Find(id);
        
        // 2. Trả về View với dữ liệu
        return View(product);
    }
}
```

**Cơ chế hoạt động:**
1. **Request** → Controller
2. **Controller** → Gọi Model để lấy dữ liệu
3. **Model** → Trả dữ liệu về Controller
4. **Controller** → Truyền dữ liệu cho View
5. **View** → Render HTML cho user

---

## 📁 **CẤU TRÚC THƯ MỤC VÀ CHỨC NĂNG**

### **1. Controllers/ - Xử lý logic nghiệp vụ**

#### **🔐 AuthController.cs - Xác thực người dùng**
```csharp
// Đăng ký tài khoản
[HttpPost]
public async Task<IActionResult> Register(RegisterViewModel model)
{
    // 1. Validate dữ liệu
    if (!ModelState.IsValid) return View(model);
    
    // 2. Tạo user mới
    var user = new IdentityUser { UserName = model.Email, Email = model.Email };
    var result = await _userManager.CreateAsync(user, model.Password);
    
    // 3. Gán role và gửi email xác nhận
    if (result.Succeeded)
    {
        await _userManager.AddToRoleAsync(user, "User");
        await SendConfirmationEmail(user);
    }
}
```

**Học được:**
- ASP.NET Core Identity
- Model validation
- Async/await pattern
- Email service integration

#### **🛍️ CartController.cs - Quản lý giỏ hàng**
```csharp
// Thêm sản phẩm vào giỏ hàng
[HttpPost]
public IActionResult AddToCart(int productId, int quantity)
{
    // 1. Lấy session cart
    var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
    
    // 2. Kiểm tra sản phẩm đã có chưa
    var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);
    
    // 3. Cập nhật hoặc thêm mới
    if (existingItem != null)
        existingItem.Quantity += quantity;
    else
        cart.Add(new CartItem { ProductId = productId, Quantity = quantity });
    
    // 4. Lưu vào session
    HttpContext.Session.Set("Cart", cart);
}
```

**Học được:**
- Session management
- LINQ operations
- Extension methods
- JSON serialization

#### **👨‍💼 AdminProductController.cs - Quản lý sản phẩm (Admin)**
```csharp
// CRUD Operations với Entity Framework
public async Task<IActionResult> Create([Bind("ProductName,Price,CategoryId")] Product product, IFormFile image)
{
    // 1. Validate và upload file
    if (image != null)
    {
        var fileName = await UploadImage(image);
        product.ImageUrl = fileName;
    }
    
    // 2. Lưu vào database
    _context.Products.Add(product);
    await _context.SaveChangesAsync();
}
```

**Học được:**
- File upload handling
- Entity Framework CRUD
- Model binding
- Image processing

### **2. Models/ - Định nghĩa dữ liệu**

#### **📦 Product.cs - Entity Model**
```csharp
public class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    
    // Navigation properties - Relationships
    public int? CategoryId { get; set; }
    public Category Category { get; set; }
    public ICollection<ProductImage> ProductImages { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; }
}
```

**Học được:**
- Entity Framework relationships
- Navigation properties
- Data annotations
- Lazy loading vs Eager loading

#### **📊 ViewModels - Data Transfer Objects**
```csharp
public class HomeViewModel
{
    public List<Banner> Banners { get; set; }
    public List<Category> FeaturedCategories { get; set; }
    public List<Product> FeaturedProducts { get; set; }
}
```

**Học được:**
- ViewModel pattern
- Data aggregation
- Separation of concerns

### **3. Views/ - Giao diện người dùng**

#### **🎨 Razor Syntax**
```html
@model List<Product>

@foreach (var product in Model)
{
    <div class="card">
        <img src="@product.ImageUrl" alt="@product.ProductName" />
        <h5>@product.ProductName</h5>
        <p class="price">@product.Price.ToString("N0") VNĐ</p>
        
        @if (product.StockQuantity > 0)
        {
            <button class="btn btn-primary">Thêm vào giỏ</button>
        }
        else
        {
            <span class="badge bg-danger">Hết hàng</span>
        }
    </div>
}
```

**Học được:**
- Razor syntax (@model, @foreach, @if)
- HTML helpers
- Model binding trong View
- Conditional rendering

#### **🔧 Partial Views - Tái sử dụng UI**
```html
<!-- _ProductCard.cshtml -->
@model Product
<div class="product-card">
    <img src="@Model.ImageUrl" />
    <h5>@Model.ProductName</h5>
    <p>@Model.Price.ToString("N0") VNĐ</p>
</div>

<!-- Sử dụng trong View khác -->
@foreach (var product in Model.Products)
{
    @await Html.PartialAsync("_ProductCard", product)
}
```

**Học được:**
- Partial view pattern
- Component reusability
- DRY principle

### **4. Services/ - Business Logic**

#### **📧 EmailService.cs - Gửi email**
```csharp
public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
}

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _emailSettings;
    
    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        using var client = new SmtpClient(_emailSettings.SmtpServer)
        {
            Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPass),
            EnableSsl = true
        };
        
        var message = new MailMessage(_emailSettings.SenderEmail, toEmail, subject, htmlMessage)
        {
            IsBodyHtml = true
        };
        
        await client.SendMailAsync(message);
    }
}
```

**Học được:**
- Interface pattern
- Dependency injection
- Configuration management
- Async programming

#### **🏆 UserRankService.cs - Hệ thống xếp hạng**
```csharp
public class UserRankService
{
    public async Task UpdateUserRank(string userId)
    {
        // 1. Tính tổng chi tiêu 6 tháng gần nhất
        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        var totalSpent = await _context.Orders
            .Where(o => o.UserId == userId && o.OrderDate >= sixMonthsAgo)
            .SumAsync(o => o.TotalAmount ?? 0);
        
        // 2. Xác định rank mới
        var newRank = totalSpent switch
        {
            >= 10000000 => "Platinum",
            >= 5000000 => "Gold",
            >= 2000000 => "Silver",
            _ => "Bronze"
        };
        
        // 3. Cập nhật rank
        var userRank = await _context.UserRankInfos.FindAsync(userId);
        if (userRank != null)
        {
            userRank.Rank = newRank;
            userRank.TotalSpent = totalSpent;
            await _context.SaveChangesAsync();
        }
    }
}
```

**Học được:**
- Business logic encapsulation
- Switch expressions (C# 8.0+)
- LINQ aggregation
- Service layer pattern

### **5. ViewComponents/ - Reusable UI Components**

#### **🔔 AdminNotificationViewComponent.cs**
```csharp
public class AdminNotificationViewComponent : ViewComponent
{
    private readonly LongChauDbContext _context;
    
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var pendingOrders = await _context.Orders
            .CountAsync(o => o.Status == "Chờ xác nhận");
            
        var unreadMessages = await _context.ChatMessages
            .CountAsync(m => m.ReceiverId == "admin" && !m.IsRead);
            
        var model = new AdminNotificationModel
        {
            PendingOrders = pendingOrders,
            UnreadMessages = unreadMessages
        };
        
        return View(model);
    }
}
```

**Học được:**
- ViewComponent pattern
- Reusable UI logic
- Async data loading
- Component-based architecture

---

## 🔄 **CƠ CHẾ HOẠT ĐỘNG CHI TIẾT**

### **1. Request Pipeline**

```csharp
// Program.cs - Middleware Pipeline
app.UseHttpsRedirection();           // Redirect HTTP → HTTPS
app.UseStaticFiles();                // Serve static files (CSS, JS, Images)
app.UseRouting();                    // Enable routing
app.UseAuthentication();             // Check user identity
app.UseAuthorization();              // Check user permissions
app.MapControllerRoute();            // Route to controllers
```

**Luồng xử lý request:**
1. **Request** → Middleware Pipeline
2. **Routing** → Tìm controller/action phù hợp
3. **Authentication** → Kiểm tra user đã đăng nhập chưa
4. **Authorization** → Kiểm tra quyền truy cập
5. **Controller** → Xử lý logic nghiệp vụ
6. **View** → Render HTML response

### **2. Database Operations**

#### **Entity Framework Core - Code First**
```csharp
// LongChauDbContext.cs
public class LongChauDbContext : IdentityDbContext<IdentityUser>
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);
            
        // Configure constraints
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");
    }
}
```

**Học được:**
- DbContext configuration
- Entity relationships
- Data constraints
- Migration management

#### **LINQ Queries**
```csharp
// Simple query
var products = await _context.Products
    .Where(p => p.IsActive)
    .OrderBy(p => p.ProductName)
    .ToListAsync();

// Complex query with includes
var orders = await _context.Orders
    .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
    .Include(o => o.User)
    .Where(o => o.Status == "Chờ xác nhận")
    .OrderByDescending(o => o.OrderDate)
    .ToListAsync();
```

**Học được:**
- LINQ syntax
- Eager loading với Include()
- Async/await pattern
- Query optimization

### **3. Authentication & Authorization**

#### **ASP.NET Core Identity**
```csharp
// AuthController.cs
[HttpPost]
public async Task<IActionResult> Login(LoginViewModel model)
{
    // 1. Validate model
    if (!ModelState.IsValid) return View(model);
    
    // 2. Sign in user
    var result = await _signInManager.PasswordSignInAsync(
        model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
    
    // 3. Redirect based on result
    if (result.Succeeded)
    {
        return RedirectToAction("Index", "Home");
    }
    
    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
    return View(model);
}
```

**Học được:**
- Password authentication
- Session management
- Model validation
- Error handling

#### **Role-based Authorization**
```csharp
// AdminController.cs
[Authorize(Roles = "Admin")]
public class AdminProductController : Controller
{
    // Chỉ admin mới truy cập được
    public IActionResult Index()
    {
        // Admin logic here
    }
}
```

**Học được:**
- Authorization attributes
- Role-based security
- Policy-based authorization

### **4. Real-time Communication**

#### **SignalR - Chat System**
```csharp
// ChatHub.cs
public class ChatHub : Hub
{
    public async Task SendMessage(string message)
    {
        // 1. Lưu message vào database
        var chatMessage = new ChatMessage
        {
            SenderId = Context.UserIdentifier,
            ReceiverId = "admin",
            Message = message,
            Timestamp = DateTime.Now
        };
        
        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();
        
        // 2. Broadcast message đến tất cả clients
        await Clients.All.SendAsync("ReceiveMessage", 
            Context.User.Identity.Name, message);
    }
}
```

**Học được:**
- SignalR Hub
- Real-time communication
- Client-server messaging
- Connection management

---

## 🎨 **FRONTEND PATTERNS**

### **1. Bootstrap 5 - Responsive Design**
```html
<!-- Grid System -->
<div class="container">
    <div class="row">
        <div class="col-md-6 col-lg-4">
            <!-- Product card -->
        </div>
        <div class="col-md-6 col-lg-4">
            <!-- Product card -->
        </div>
        <div class="col-md-6 col-lg-4">
            <!-- Product card -->
        </div>
    </div>
</div>
```

**Học được:**
- Bootstrap grid system
- Responsive breakpoints
- Utility classes
- Component library

### **2. jQuery - DOM Manipulation**
```javascript
// AJAX request
$.ajax({
    url: '/Cart/AddToCart',
    type: 'POST',
    data: { productId: id, quantity: qty },
    success: function(response) {
        if (response.success) {
            showToast('Thêm vào giỏ hàng thành công!', 'success');
            updateCartCount(response.cartCount);
        }
    },
    error: function() {
        showToast('Có lỗi xảy ra!', 'error');
    }
});

// Event handling
$('.add-to-cart-btn').click(function() {
    var productId = $(this).data('product-id');
    var quantity = $('#quantity-' + productId).val();
    addToCart(productId, quantity);
});
```

**Học được:**
- AJAX requests
- Event handling
- DOM manipulation
- Error handling

### **3. Custom CSS - Styling**
```css
/* Custom styles */
.product-card {
    transition: transform 0.2s ease-in-out;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.product-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 4px 16px rgba(0,0,0,0.15);
}

/* Responsive design */
@media (max-width: 768px) {
    .product-card {
        margin-bottom: 1rem;
    }
}
```

**Học được:**
- CSS transitions
- Box shadows
- Media queries
- Responsive design

---

## 🔧 **DEVELOPMENT PATTERNS**

### **1. Dependency Injection**
```csharp
// Program.cs
builder.Services.AddScoped<LongChauDbContext>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<UserRankService>();

// Controller constructor
public class ProductController : Controller
{
    private readonly LongChauDbContext _context;
    private readonly IEmailSender _emailSender;
    
    public ProductController(LongChauDbContext context, IEmailSender emailSender)
    {
        _context = context;
        _emailSender = emailSender;
    }
}
```

**Học được:**
- Service lifetime (Scoped, Singleton, Transient)
- Interface-based programming
- Constructor injection
- IoC container

### **2. Configuration Management**
```csharp
// appsettings.json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "your-email@gmail.com",
    "SmtpPass": "your-password"
  }
}

// EmailSettings.cs
public class EmailSettings
{
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string SmtpUser { get; set; }
    public string SmtpPass { get; set; }
}

// Program.cs
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
```

**Học được:**
- Configuration binding
- Options pattern
- Environment-specific settings
- Secure configuration

### **3. Error Handling**
```csharp
// Global exception handling
app.UseExceptionHandler("/Home/Error");

// Controller-level error handling
public async Task<IActionResult> Create(Product product)
{
    try
    {
        if (ModelState.IsValid)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
    catch (DbUpdateException ex)
    {
        ModelState.AddModelError("", "Có lỗi xảy ra khi lưu sản phẩm");
        _logger.LogError(ex, "Error creating product");
    }
    
    return View(product);
}
```

**Học được:**
- Try-catch blocks
- Global exception handling
- Logging
- User-friendly error messages

---

## 📊 **DATABASE DESIGN PATTERNS**

### **1. Entity Relationships**
```csharp
// One-to-Many: Category → Products
public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    public ICollection<Product> Products { get; set; }
}

public class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int? CategoryId { get; set; }
    public Category Category { get; set; }
}

// Many-to-Many: Users ↔ Vouchers
public class UserVoucher
{
    public string UserId { get; set; }
    public int VoucherId { get; set; }
    public bool IsUsed { get; set; }
    public bool IsNew { get; set; }
    
    public IdentityUser User { get; set; }
    public Voucher Voucher { get; set; }
}
```

**Học được:**
- Foreign key relationships
- Navigation properties
- Junction tables
- Lazy vs Eager loading

### **2. Data Annotations**
```csharp
public class Product
{
    [Key]
    public int ProductId { get; set; }
    
    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [StringLength(100, ErrorMessage = "Tên sản phẩm không quá 100 ký tự")]
    public string ProductName { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
    public decimal Price { get; set; }
    
    [Display(Name = "Số lượng tồn kho")]
    public int StockQuantity { get; set; }
}
```

**Học được:**
- Validation attributes
- Display attributes
- Custom error messages
- Data constraints

---

## 🚀 **BEST PRACTICES HỌC ĐƯỢC**

### **1. Code Organization**
- **Separation of Concerns**: Tách biệt logic nghiệp vụ
- **Single Responsibility**: Mỗi class chỉ có một trách nhiệm
- **DRY Principle**: Không lặp lại code

### **2. Performance Optimization**
- **Async/Await**: Sử dụng cho I/O operations
- **Eager Loading**: Include() cho related data
- **Pagination**: Phân trang cho large datasets

### **3. Security**
- **Input Validation**: Validate tất cả input
- **CSRF Protection**: Anti-forgery tokens
- **Authorization**: Kiểm tra quyền truy cập

### **4. User Experience**
- **Responsive Design**: Tương thích mobile
- **Loading States**: Hiển thị trạng thái loading
- **Error Handling**: Thông báo lỗi thân thiện

---

## 📚 **TÀI LIỆU THAM KHẢO**

### **ASP.NET Core**
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Identity](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)

### **Frontend**
- [Bootstrap 5 Documentation](https://getbootstrap.com/docs/5.3/)
- [jQuery Documentation](https://api.jquery.com/)
- [Font Awesome](https://fontawesome.com/)

### **Database**
- [SQL Server Documentation](https://docs.microsoft.com/en-us/sql/sql-server/)
- [Entity Framework Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

---

## 🎯 **BÀI TẬP THỰC HÀNH**

### **Level 1: Hiểu cơ bản**
1. Tạo một Product mới với validation
2. Implement search functionality
3. Tạo một ViewComponent đơn giản

### **Level 2: Intermediate**
1. Implement pagination cho Product list
2. Tạo API endpoint cho AJAX calls
3. Implement file upload với validation

### **Level 3: Advanced**
1. Implement caching cho Product data
2. Tạo background service cho email notifications
3. Implement real-time notifications

---

*Tài liệu này cung cấp roadmap chi tiết để học và hiểu code từ dự án Nhà Thuốc Long Châu. Hãy thực hành từng phần một cách có hệ thống.* 