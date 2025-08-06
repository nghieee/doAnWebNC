# 🛠️ TỔNG HỢP CÔNG NGHỆ SỬ DỤNG TRONG DỰ ÁN

## 📋 Tổng quan công nghệ

Dự án **Nhà Thuốc Long Châu** được xây dựng trên nền tảng **ASP.NET Core 8.0** với kiến trúc **MVC (Model-View-Controller)** và sử dụng nhiều công nghệ hiện đại để tạo ra một hệ thống thương mại điện tử hoàn chỉnh.

---

## 🎯 **BACKEND TECHNOLOGIES**

### **1. ASP.NET Core 8.0**
- **Mục đích**: Framework chính cho backend
- **Tính năng sử dụng**:
  - MVC Pattern
  - Dependency Injection
  - Middleware Pipeline
  - Configuration Management
  - Logging System
- **File cấu hình**: `Program.cs`, `appsettings.json`

### **2. Entity Framework Core 9.0**
- **Mục đích**: ORM (Object-Relational Mapping)
- **Tính năng sử dụng**:
  - Code-First Approach
  - Database Migrations
  - LINQ Queries
  - Relationship Management
  - Change Tracking
- **Models chính**:
  - `Product`, `Category`, `Order`, `User`
  - `Banner`, `Voucher`, `ChatMessage`

### **3. ASP.NET Core Identity**
- **Mục đích**: Hệ thống xác thực và phân quyền
- **Tính năng sử dụng**:
  - User Authentication
  - Role-based Authorization
  - Password Hashing
  - Email Confirmation
  - Account Lockout
- **Customizations**:
  - `CustomIdentityErrorDescriber.cs`
  - Custom User Management

### **4. SignalR 1.2.0**
- **Mục đích**: Real-time communication
- **Tính năng sử dụng**:
  - Live Chat System
  - Real-time Notifications
  - Connection Management
- **Implementation**: `ChatHub.cs`

### **5. Newtonsoft.Json 13.0.3**
- **Mục đích**: JSON Serialization/Deserialization
- **Tính năng sử dụng**:
  - API Responses
  - Data Transfer Objects
  - Configuration Files

---

## 🎨 **FRONTEND TECHNOLOGIES**

### **1. Bootstrap 5.3.2**
- **Mục đích**: CSS Framework cho responsive design
- **Tính năng sử dụng**:
  - Grid System
  - Components (Cards, Modals, Forms)
  - Utilities (Spacing, Colors, Typography)
  - Responsive Breakpoints
- **Customizations**: `site.css`

### **2. jQuery 3.6.0**
- **Mục đích**: JavaScript Library
- **Tính năng sử dụng**:
  - DOM Manipulation
  - AJAX Requests
  - Event Handling
  - Form Validation
  - Dynamic Content Loading

### **3. Font Awesome 6.4.2**
- **Mục đích**: Icon Library
- **Tính năng sử dụng**:
  - UI Icons
  - Navigation Icons
  - Status Icons
  - Action Icons

### **4. Razor Pages**
- **Mục đích**: Server-side rendering
- **Tính năng sử dụng**:
  - View Templates
  - Partial Views
  - View Components
  - Layout Pages

---

## 🗄️ **DATABASE TECHNOLOGIES**

### **1. SQL Server**
- **Mục đích**: Relational Database
- **Tính năng sử dụng**:
  - Data Storage
  - ACID Transactions
  - Indexing
  - Stored Procedures (nếu cần)

### **2. Entity Framework Core Migrations**
- **Mục đích**: Database Schema Management
- **Tính năng sử dụng**:
  - Code-First Migrations
  - Database Updates
  - Schema Changes
  - Seed Data

---

## 📧 **EMAIL TECHNOLOGIES**

### **1. SMTP Email Service**
- **Mục đích**: Email notifications
- **Implementation**:
  - `IEmailSender.cs` (Interface)
  - `SmtpEmailSender.cs` (Implementation)
  - `EmailSettings.cs` (Configuration)

---

## 🔧 **DEVELOPMENT TOOLS**

### **1. Visual Studio 2022 / VS Code**
- **Mục đích**: IDE/Code Editor
- **Tính năng sử dụng**:
  - IntelliSense
  - Debugging
  - Git Integration
  - Extensions

### **2. .NET CLI Tools**
- **Mục đích**: Command-line development
- **Commands sử dụng**:
  - `dotnet build`
  - `dotnet run`
  - `dotnet ef migrations`
  - `dotnet ef database update`

---

## 📦 **NUGET PACKAGES**

### **Core Packages**
```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Identity.UI" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.2.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.6" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.6" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### **Package Purposes**
- **Identity.EntityFrameworkCore**: User management
- **Identity.UI**: Default UI components
- **SignalR**: Real-time communication
- **EF Core Design**: Migration tools
- **EF Core SqlServer**: SQL Server provider
- **EF Core Tools**: Database tools
- **Newtonsoft.Json**: JSON handling

---

## 🏗️ **ARCHITECTURE PATTERNS**

### **1. MVC Pattern**
- **Model**: Entity classes, ViewModels
- **View**: Razor pages, layouts
- **Controller**: Business logic, request handling

### **2. Repository Pattern**
- **Implementation**: Entity Framework Core
- **Purpose**: Data access abstraction

### **3. Service Layer Pattern**
- **Implementation**: Custom services
- **Examples**: `UserRankService`, `EmailSender`

### **4. ViewComponent Pattern**
- **Implementation**: Reusable UI components
- **Examples**: `NavbarViewComponent`, `AdminNotificationViewComponent`

---

## 🔒 **SECURITY TECHNOLOGIES**

### **1. ASP.NET Core Identity**
- Password hashing
- Account lockout
- Email confirmation
- Two-factor authentication (có thể mở rộng)

### **2. CSRF Protection**
- Anti-forgery tokens
- Form validation

### **3. Input Validation**
- Model validation
- Custom validation attributes
- Client-side validation

---

## 📊 **PERFORMANCE TECHNOLOGIES**

### **1. Entity Framework Core**
- Lazy loading
- Eager loading with Include()
- Query optimization

### **2. Caching (Potential)**
- Memory caching
- Distributed caching
- Response caching

### **3. Compression**
- Response compression
- Static file compression

---

## 🧪 **TESTING TECHNOLOGIES**

### **1. Unit Testing (Potential)**
- xUnit
- Moq
- FluentAssertions

### **2. Integration Testing (Potential)**
- TestServer
- WebApplicationFactory

---

## 📱 **RESPONSIVE DESIGN**

### **1. Bootstrap 5 Grid System**
- Mobile-first approach
- Responsive breakpoints
- Flexible layouts

### **2. CSS Media Queries**
- Custom responsive styles
- Device-specific optimizations

---

## 🚀 **DEPLOYMENT TECHNOLOGIES**

### **1. IIS (Windows)**
- Web server hosting
- Application pool management

### **2. Azure (Cloud)**
- App Service hosting
- SQL Database
- Blob Storage (cho images)

### **3. Docker (Containerization)**
- Container deployment
- Multi-stage builds

---

## 📈 **MONITORING & LOGGING**

### **1. ASP.NET Core Logging**
- Built-in logging framework
- Structured logging
- Log levels

### **2. Application Insights (Potential)**
- Performance monitoring
- Error tracking
- Usage analytics

---

## 🔄 **VERSION CONTROL**

### **1. Git**
- Source code management
- Branch strategy
- Commit conventions

---

## 📋 **CONFIGURATION MANAGEMENT**

### **1. appsettings.json**
- Database connection strings
- Email settings
- Application settings

### **2. User Secrets (Development)**
- Sensitive configuration
- Local development settings

---

## 🎯 **TECHNOLOGY MATRIX**

| Category | Technology | Version | Purpose |
|----------|------------|---------|---------|
| **Framework** | ASP.NET Core | 8.0 | Main framework |
| **ORM** | Entity Framework Core | 9.0 | Data access |
| **Database** | SQL Server | Latest | Data storage |
| **Authentication** | ASP.NET Identity | 8.0 | User management |
| **Real-time** | SignalR | 1.2.0 | Live communication |
| **Frontend** | Bootstrap | 5.3.2 | UI framework |
| **JavaScript** | jQuery | 3.6.0 | DOM manipulation |
| **Icons** | Font Awesome | 6.4.2 | Icon library |
| **JSON** | Newtonsoft.Json | 13.0.3 | JSON handling |

---

## 🚀 **FUTURE TECHNOLOGIES (Gợi ý mở rộng)**

### **1. Microservices**
- Service decomposition
- API Gateway
- Service discovery

### **2. Message Queues**
- RabbitMQ
- Azure Service Bus
- Background job processing

### **3. Caching**
- Redis
- Memory caching
- Distributed caching

### **4. Search Engine**
- Elasticsearch
- Lucene.NET
- Product search optimization

### **5. Payment Gateway**
- Stripe
- PayPal
- VNPay integration

### **6. Mobile App**
- Xamarin
- React Native
- Flutter

### **7. Analytics**
- Google Analytics
- Application Insights
- Custom analytics

---

*Tài liệu này cung cấp cái nhìn tổng quan về tất cả công nghệ được sử dụng trong dự án Nhà Thuốc Long Châu.* 