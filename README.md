# 🏥 NHÀ THUỐC LONG CHÂU PHAKE — HỆ THỐNG THƯƠNG MẠI ĐIỆN TỬ & QUẢN TRỊ DƯỢC PHẨM TOÀN DIỆN

> **Đồ án Môn học / Dự án Chuyên đề Web Nâng cao (ASP.NET Core 8.0 MVC)**  
> **Tác giả:** Nguyễn Trung Hiếu (MSSV: 22DH111077)  
> **Repository:** [nghieee/doAnWebNC](https://github.com/nghieee/doAnWebNC)

---

## 📋 MỤC LỤC
1. [Giới Thiệu & Tổng Quan Dự Án](#1-giới-thiệu--tổng-quan-dự-án)
2. [Mục Tiêu & Phạm Vi Hệ Thống](#2-mục-tiêu--phạm-vi-hệ-thống)
3. [Kiến Trúc Phần Mềm & Mẫu Thiết Kế (Design Patterns)](#3-kiến-trúc-phần-mềm--mẫu-thiết-kế-design-patterns)
4. [Công Nghệ, Thư Viện & Nền Tảng Sử Dụng](#4-công-nghệ-thư-viện--nền-tảng-sử-dụng)
5. [Chi Tiết Chức Năng & Luồng Nghiệp Vụ](#5-chi-tiết-chức-năng--luồng-nghiệp-vụ)
   - [5.1. Phía Khách Hàng (Customer Platform)](#51-phía-khách-hàng-customer-platform)
   - [5.2. Phía Quản Trị & Nhân Viên (Admin & Staff Platform)](#52-phía-quản-trị--nhân-viên-admin--staff-platform)
6. [Chi Tiết Cơ Sở Dữ Liệu (Database Schema - 34 Bảng)](#6-chi-tiết-cơ-sở-dữ-liệu-database-schema---34-bảng)
7. [Cấu Trúc Thư Mục Dự Án](#7-cấu-trúc-thư-mục-dự-án)
8. [Hướng Dẫn Cấu Hình Hệ Thống (appsettings.json)](#8-hướng-dẫn-cấu-hình-hệ-thống-appsettingsjson)
9. [Hướng Dẫn Cài Đặt & Vận Hành](#9-hướng-dẫn-cài-đặt--vận-hành)
10. [Tài Khoản Thử Nghiệm Mặc Định](#10-tài-khoản-thử-nghiệm-mặc-định)
11. [📸 Thư Viện Đề Mục Ảnh Màn Hình (Screenshot Placeholders)](#11--thư-viện-đề-mục-ảnh-màn-hình-screenshot-placeholders)

---

## 1. GIỚI THIỆU & TỔNG QUAN DỰ ÁN

**Nhà Thuốc Long Châu Phake** là hệ thống thương mại điện tử chuyên biệt cho ngành dược phẩm, thực phẩm chức năng, thiết bị y tế và chăm sóc sức khỏe. Hệ thống giải quyết các bài toán đặc thù của ngành dược bao gồm: quản lý kê đơn/không kê đơn, quản lý theo số lô và hạn sử dụng (FEFO), quy trình nhập kho từ nhà cung cấp, tích hợp cổng thanh toán trực tuyến QR code, tự động tính phí vận chuyển và đẩy đơn qua đối tác giao hàng, kết hợp với trợ lý tư vấn y tế sử dụng Trí tuệ Nhân tạo (AI).

### Thông tin tổng quan:
* **Tên hệ thống:** Hệ thống Bán thuốc & Quản trị Dược phẩm Trực tuyến Long Châu Phake
* **Khung phát triển:** ASP.NET Core 8.0 MVC (`net8.0`)
* **Cơ sở dữ liệu:** SQL Server + Entity Framework Core 9.x (Code-First Migration)
* **Root Namespace:** `web_ban_thuoc`
* **Cấu trúc Solution:** `doAnWebNC.sln` / `web-ban-thuoc.sln`

---

## 2. MỤC TIÊU & PHẠM VI HỆ THỐNG

### 🎯 Mục tiêu
1. **Đối với Khách hàng:** Cung cấp trải nghiệm mua sắm thuốc trực tuyến an toàn, nhanh chóng, minh bạch về nguồn gốc xuất xứ, hỗ trợ tư vấn y khoa 24/7 bằng Trợ lý AI và chat trực tiếp với nhân viên y tế.
2. **Đối với Dược sĩ & Nhân viên Kho:** Tối ưu hóa quy trình nhập hàng, kiểm soát tồn kho theo lô/hạn dùng (FEFO), điều chỉnh kho hàng, xử lý đơn hàng và đóng gói vận chuyển.
3. **Đối với Nhà quản lý (Admin):** Cung cấp bộ công cụ quản trị kinh doanh toàn diện, báo cáo doanh thu KPI, phân tích hiệu quả khuyến mãi, quản lý nhân sự và theo dõi nhật ký hoạt động hệ thống.

---

## 3. KIẾN TRÚC PHẦN MỀM & MẪU THIẾT KẾ (DESIGN PATTERNS)

Dự án tuân thủ nghiêm ngặt các nguyên lý thiết kế phần mềm sạch (Clean Code), áp dụng kiến trúc phân lớp và các mẫu thiết kế hướng đối tượng:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      PRESENTATION LAYER (Razor Views)                   │
│         User Layout (_Layout.cshtml) | Admin Layout (Admin/_Layout)       │
│         ViewComponents (Navbar, AdminSidebar, AdminNotification)        │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ HTTP Requests / AJAX / WebSockets
┌────────────────────────────────────▼────────────────────────────────────┐
│                   CONTROLLER LAYER (MVC Controllers)                    │
│   Client: HomeController, AuthController, ProductController, CartController... │
│   Admin: AdminReport, AdminProduct, AdminOrder, AdminInventory...       │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ Dependency Injection (Scoped)
┌────────────────────────────────────▼────────────────────────────────────┐
│                    SERVICE LAYER (Business Logic Services)              │
│   CartService, OrderService, InventoryService, GeminiAiService,         │
│   PayOSService, GHNService, UserRankService, OrderEmailService...        │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ Entity Framework Core 9.x
┌────────────────────────────────────▼────────────────────────────────────┐
│                   DATA ACCESS LAYER (LongChauDbContext)                 │
│                 34 DbSets | SQL Server | Code-First Migrations          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Các Mẫu Thiết Kế (Design Patterns) Áp Dụng:
1. **Strategy Pattern (Chiến lược gửi Email linh hoạt):**
   - Định nghĩa Interface `IEmailSender`.
   - `SmtpEmailSender`: Chiến lược gửi mail thực qua SMTP Gmail server.
   - `NullEmailSender`: Chiến lược giả lập (Mock) chạy trong môi trường test/dev.
   - Hoán đổi linh hoạt thông qua cấu hình `EmailSettings:Enabled` trong `Program.cs`.
2. **Decorator Pattern (Ghi nhật ký Mail):**
   - `LoggingEmailSender` đóng vai trò là Decorator bọc ngoài một instance `IEmailSender` bất kỳ để tự động ghi log trước/sau khi phát mail mà không sửa đổi logic gốc.
3. **Factory Pattern (Khởi tạo Email Sender):**
   - `EmailSenderFactory`: Khởi tạo instance phù hợp với môi trường thực thi.
4. **Observer Pattern / Event-Driven (Real-time SignalR):**
   - `ChatHub`: Đăng ký và lắng nghe sự kiện chat giữa Khách hàng và Admin theo nhóm (`chat_{userId}`).
5. **Hosted Service (Background Job Daemon):**
   - `MonthlyVoucherHostedService`: Chạy ngầm định kỳ hàng tháng để tính toán lại điểm tích lũy, hạ/nâng hạng thành viên và tự động tặng voucher tri ân.
6. **Action Filter (Global Navigation Filtering):**
   - `NavbarFilter`: Tự động nạp cấu trúc danh mục 3 cấp vào `ViewData` trước khi render bất kỳ View nào có Header/Navbar.

---

## 4. CÔNG NGHỆ, THƯ VIỆN & NỀN TẢNG SỬ DỤNG

### 🖥️ Backend
* **ASP.NET Core 8.0 MVC:** Nền tảng cốt lõi của ứng dụng Web Server.
* **Entity Framework Core 9.0:** ORM quản lý truy vấn dữ liệu SQL Server (`Microsoft.EntityFrameworkCore.SqlServer 9.0.7`, `Tools 9.0.6`, `Design 9.0.6`).
* **ASP.NET Core Identity 8.0:** Xác thực (Authentication) và Phân quyền (Authorization) dựa trên Role.
* **SignalR:** Truyền thông điệp thời gian thực (Real-time WebSockets) phục vụ tính năng Live Chat.
* **Newtonsoft.Json 13.0.3:** Xử lý chuỗi JSON truyền nhận cho cổng thanh toán PayOS.
* **ClosedXML 0.104.2:** Xuất/Nhập báo cáo và danh mục sản phẩm ra file Excel (`.xlsx`).
* **System.Security.Cryptography:** Tính toán mã băm checksum HMAC-SHA256 bảo mật giao dịch thanh toán.

### 🎨 Frontend
* **Razor Views Engine (`.cshtml`):** Template engine phía Server.
* **Bootstrap 5.3 + jQuery 3.6:** Framework giao diện phản hồi tốt trên mọi màn hình.
* **Font Awesome 6 Pro:** Bộ icon trực quan.
* **Custom CSS Tokens:** `site.css`, `admin.css`, `payos.css`, `reset.css`.
* **Glassmorphism CSS & Micro-animations:** Áp dụng hiệu ứng kính mờ cho bong bóng Chat AI, các hiệu ứng tải trang Skeleton Shimmer (`ai-shimmer`).

### 🔌 Tích hợp Dịch vụ Bên thứ Ba (Third-party Integrations)
1. **Google Gemini API (1.5-Flash / 2.0-Flash):** Trợ lý AI y tế.
2. **Groq API (Llama-3.3-70B-Versatile):** Mô hình AI tốc độ cao dự phòng.
3. **OpenAI API (ChatGPT):** Tùy chọn mô hình AI tích hợp.
4. **PayOS Merchant API (`api-merchant.payos.vn`):** Cổng thanh toán quét mã QR Banking tự động.
5. **Giao Hàng Nhanh (GHN API):** Tính phí ship, tra cứu mã vùng hành chính Việt Nam và tạo vận đơn shipper.
6. **Gmail SMTP Service:** Máy chủ phát mail thông báo tự động.

---

## 5. CHI TIẾT CHỨC NĂNG & LUỒNG NGHIỆP VỤ

### 5.1. Phía Khách Hàng (Customer Platform)

#### 1. Quản lý Tài khoản & Hồ sơ (`AuthController`)
* **Đăng ký / Đăng nhập:** Hệ thống xác thực bằng Cookie Identity, kiểm tra tính hợp lệ của Email, Mật khẩu có độ bảo mật cao.
* **Khôi phục Mật khẩu:** Gửi mã xác thực OTP qua Email, cho phép thiết lập lại mật khẩu an toàn.
* **Trang cá nhân (Profile):** Cập nhật họ tên, số điện thoại, địa chỉ mặc định, xem lịch sử đơn hàng, xem hạng thành viên hiện tại và đổi mật khẩu.
* **Hạng thành viên (Loyalty Rank):** Tự động xếp hạng thành viên 4 cấp (`Bronze`, `Silver`, `Gold`, `Platinum`) dựa trên chi tiêu 6 tháng.

#### 2. Duyệt & Tìm kiếm Sản phẩm (`ProductController`, `CategoriesController`)
* **Danh mục 3 cấp (Self-referencing Category):** Cấu trúc danh mục dạng cây (Ví dụ: *Dược phẩm -> Thuốc kê đơn -> Thuốc kháng sinh*).
* **Tìm kiếm & Lọc đa chỉ tiêu (Multi-criteria Filter):** Lọc theo tầm giá, thương hiệu, xuất xứ, đối tượng sử dụng, danh mục và sắp xếp theo giá tăng/giảm, bán chạy.
* **Trang chi tiết sản phẩm:** Hiển thị thông tin thuốc (Thành phần, Công dụng, Liều dùng, Chống chỉ định, Đối tượng dùng), số đăng ký Bộ Y tế, các lô còn hạn, thư viện ảnh và danh sách đánh giá.
* **Đánh giá & Bình luận (Review System):** Chỉ khách hàng đã mua sản phẩm và đơn hàng hoàn thành (`Đã giao`) mới được gửi đánh giá kèm chấm điểm sao.

#### 3. Giỏ hàng & Đặt hàng (`CartController`, `PayOSController`)
* **Giỏ hàng thời gian thực:** Lưu trữ trong CSDL theo `UserId` hoặc `SessionId`, tính toán tự động tổng tiền, số tiền giảm giá và thuế.
* **Áp dụng Voucher:** Kiểm tra điều kiện đơn hàng tối thiểu (`MinOrderAmount`), hạn sử dụng, tổng số lượt dùng còn lại và hạng thành viên yêu cầu để trừ tiền trực tiếp.
* **Tích hợp Vận chuyển GHN:** Người dùng chọn Tỉnh/Thành, Quận/Huyện, Phường/Xã -> Hệ thống gọi API GHN tính cước phí vận chuyển chính xác đến tận nhà.
* **Thanh toán Đa phương thức:**
  - `COD`: Thanh toán tiền mặt khi nhận hàng.
  - `PayOS (QR Code)`: Tạo đường link thanh toán PayOS -> Hiển thị mã QR ngân hàng -> Webhook tự động cập nhật trạng thái đơn thành `Đã thanh toán` ngay khi tiền vào tài khoản mà không cần duyệt tay.

#### 4. 🤖 Trợ lý ảo Dược sĩ Lâm sàng AI (`AiBotController`, `GeminiAiService`)
* **Kiến trúc Multi-Provider:** Cấu hình cho phép chuyển đổi giữa `Gemini`, `Groq` hoặc `OpenAI`.
* **An toàn lâm sàng:** AI tự động kiểm tra thuộc tính `Ingredients`, `Contraindications` và `TargetUsers` của sản phẩm trước khi gợi ý (Tránh kê nhầm thuốc cho phụ nữ mang thai, trẻ sơ sinh...).
* **Truy vấn triệu chứng đa lượt:** AI giữ ngữ cảnh cuộc nói chuyện. Nếu khách hỏi mơ hồ (*"tôi bị đau bụng"*), AI sẽ hỏi lại các câu phân loại trước khi gợi ý thuốc.
* **Thẻ sản phẩm & Nút mua ngay AJAX:** AI trả về cú pháp `{PRODUCT:ID}`, giao diện JS tự động dựng card sản phẩm đẹp mắt kèm nút **"Mua ngay"** bấm là thêm thẳng vào giỏ hàng.
* **Bộ máy dự phòng CSDL (Local Fallback):** Tự động phân tích từ khóa CSDL khi mất kết nối API AI.

#### 5. Chat trực tiếp với CSKH (`ChatHub`, `_ChatPopup.cshtml`)
* Khách hàng có thể mở khung chat trực tuyến để gửi tin nhắn đến bộ phận hỗ trợ CSKH của nhà thuốc. Sử dụng SignalR đẩy tin nhắn tức thì.

---

### 5.2. Phía Quản Trị & Nhân Viên (Admin & Staff Platform)

#### 1. Báo cáo & Thống kê KPI (`AdminReportController`)
* **Báo cáo Doanh thu & Đơn hàng:** Thống kê theo ngày, tháng, năm, biểu đồ tăng trưởng, tỷ lệ đơn hủy/hoàn thành.
* **Báo cáo sản phẩm bán chạy:** Top sản phẩm mang lại doanh thu cao nhất.
* **Báo cáo công nợ Nhà cung cấp (`SupplierDebtReport`):** Theo dõi số tiền đã thanh toán và còn nợ đối tác cung ứng thuốc.
* **Báo cáo hiệu quả Voucher (`VoucherStats`):** Thống kê số lượng voucher phát ra, lượt sử dụng và tổng tiền giảm giá.
* **Xuất báo cáo Excel:** Tích hợp `ClosedXML` cho phép tải dữ liệu báo cáo ra file Excel tiêu chuẩn.

#### 2. Quản lý Sản phẩm & Danh mục (`AdminProductController`, `AdminCategoryController`)
* **CRUD Sản phẩm:** Thêm mới, chỉnh sửa, dừng kinh doanh, tải nhiều hình ảnh, gán nhãn sản phẩm nổi bật (`IsFeature`).
* **Import sản phẩm từ Excel:** Cho phép tải file Excel danh sách thuốc hàng loạt vào hệ thống với kiểm tra trùng mã SKU/Barcode và định dạng dữ liệu.
* **Cảnh báo tồn kho:** Tự động lọc các sản phẩm có số lượng tồn dưới ngưỡng `MinStockLevel`.
* **Quản lý danh mục 3 cấp:** Thêm/sửa/xóa danh mục cha - con linh hoạt.

#### 3. Quản lý Đơn hàng & Vận đơn (`AdminOrderController`, `AdminShippingController`)
* **Quy trình duyệt đơn:** `Chờ xác nhận` -> `Đã xác nhận` -> `Đang đóng gói` -> `Đang giao` -> `Đã giao` (hoặc `Đã hủy`).
* **Tự động trừ/hoàn tồn kho:** Khi đơn chuyển sang `Đã xác nhận`, hệ thống tạo giao dịch xuất kho `InventoryTransaction`. Khi đơn bị hủy, hệ thống hoàn lại số lượng tồn kho.
* **In tem vận đơn GHN (`PrintLabel`):** Xuất mã vạch vận đơn GHN trực tiếp ra khổ in tem để dán lên thùng hàng.

#### 4. Quản lý Kho hàng & Điều chỉnh Tồn kho (`AdminInventoryController`, `AdminWarehouseController`)
* **Theo dõi tồn kho đa kho:** Quản lý số lượng tồn của từng sản phẩm theo từng nhà kho (`WarehouseStock`).
* **Phiếu điều chỉnh tồn kho (`StockAdjustment`):** Thủ kho lập phiếu điều chỉnh tăng/giảm tồn kho (do hư hỏng, hết hạn, kiểm kê chênh lệch). Phiếu ở trạng thái `Chờ duyệt` -> Admin phê duyệt -> Hệ thống mới cập nhật kho thực tế.
* **In mẫu phiếu GIN (Goods Issue Note):** Hỗ trợ in mẫu phiếu xuất/nhập kho khổ A4 chuẩn kế toán.

#### 5. Quản lý Mua hàng & Lô sản phẩm (`AdminPurchaseController`)
* **Tạo Đơn mua hàng (PO):** Đặt hàng sản phẩm từ Nhà cung cấp (`Supplier`).
* **Nhập kho thực tế (GRN - Goods Receipt):** Khi hàng về, thủ kho kiểm kê và tạo phiếu nhập kho. Hệ thống tự động sinh các **Lô sản phẩm mới (`ProductBatch`)** lưu lại số lô và hạn sử dụng (`ExpiryDate`).

#### 6. Quản lý Khuyến mãi & Voucher (`AdminDiscountController`, `AdminVoucherController`)
* **Chiến dịch Flash Sale (`PromotionCampaign`):** Tạo chiến dịch giảm giá theo % cho toàn bộ sản phẩm thuộc một Danh mục hoặc Thương hiệu trong khoảng thời gian nhất định (`StartDate` -> `EndDate`).
* **Banner Khuyến mãi:** Quản lý vị trí hiển thị banner trên trang chủ, trang danh mục, xem trước giao diện Desktop/Mobile.
* **Quản lý Voucher:** Tạo mã giảm giá theo số tiền hoặc %, đặt hạn mức chi tiêu tối thiểu, số lượt dùng tối đa, gán riêng cho khách hàng cụ thể.

#### 7. Quản lý Nhân sự & Phân quyền (`AdminStaffController`, `AdminUserController`)
* **Phân quyền vai trò (Roles):**
  - `Admin`: Toàn quyền hệ thống.
  - `WarehouseStaff`: Chỉ truy cập các chức năng quản lý kho, nhập hàng, điều chỉnh tồn kho.
  - `CustomerSupport`: Chỉ truy cập màn hình Chat hỗ trợ khách hàng.
* **Quản lý Khách hàng:** Xem chi tiết chi tiêu, lịch sử đơn hàng, hạng thành viên, hỗ trợ khóa/mở khóa tài khoản.

#### 8. Nhật ký Hoạt động Hệ thống (`AdminActivityLogController`)
* Tự động ghi lại mọi thao tác quan trọng (Thêm, Sửa, Xóa sản phẩm, duyệt kho, đổi đơn hàng) của Admin/Nhân viên vào bảng `DbActivityLogs` để phục vụ công tác truy vết bảo mật.

---

## 6. CHI TIẾT CƠ SỞ DỮ LIỆU (DATABASE SCHEMA - 34 BẢNG)

Hệ thống sử dụng **SQL Server** được ánh xạ thông qua `LongChauDbContext` với 34 DbSets:

### Danh sách 34 Bảng Dữ liệu:

| STT | Tên Bảng (DbSet) | Thực Thể (Model) | Chức Năng Chính |
| :---: | :--- | :--- | :--- |
| 1 | `Categories` | `Category` | Danh mục sản phẩm (Hỗ trợ 3 cấp tự tham chiếu) |
| 2 | `Products` | `Product` | Sản phẩm/Thuốc (chứa thông tin dược lý, giá, giảm giá) |
| 3 | `ProductImages` | `ProductImage` | Thư viện hình ảnh của từng sản phẩm |
| 4 | `Orders` | `Order` | Thông tin đơn hàng (Địa chỉ, tổng tiền, trạng thái, mã GHN) |
| 5 | `OrderItems` | `OrderItem` | Chi tiết từng sản phẩm trong đơn hàng |
| 6 | `OrderStatusHistories` | `OrderStatusHistory` | Lịch sử vết thay đổi trạng thái đơn hàng |
| 7 | `Carts` | `Cart` | Giỏ hàng của người dùng / khách vãng lai |
| 8 | `CartItems` | `CartItem` | Chi tiết các mục nằm trong giỏ hàng |
| 9 | `Reviews` | `Review` | Đánh giá và bình luận sản phẩm |
| 10 | `Payments` | `Payment` | Giao dịch thanh toán (COD / PayOS) |
| 11 | `Vouchers` | `Voucher` | Mã giảm giá (Public/Private, %, tiền cố định) |
| 12 | `UserVouchers` | `UserVoucher` | Kho voucher cá nhân của từng khách hàng |
| 13 | `VoucherRedemptions` | `VoucherRedemption` | Lịch sử đã sử dụng voucher theo đơn hàng |
| 14 | `Banners` | `Banner` | Banner quảng cáo và khuyến mãi trên website |
| 15 | `ChatMessages` | `ChatMessage` | Năng lượng tin nhắn chat thời gian thực |
| 16 | `UserRankInfos` | `UserRankInfo` | Thông tin hạng thành viên và tổng chi tiêu 6 tháng |
| 17 | `LoyaltyPointTransactions` | `LoyaltyPointTransaction` | Lịch sử cộng/trừ điểm thưởng thành viên |
| 18 | `LoyaltyRewards` | `LoyaltyReward` | Danh mục quà tặng/voucher có thể đổi bằng điểm |
| 19 | `Warehouses` | `Warehouse` | Danh sách nhà kho trong hệ thống |
| 20 | `WarehouseStocks` | `WarehouseStock` | Số lượng tồn kho của từng sản phẩm theo kho |
| 21 | `InventoryTransactions` | `InventoryTransaction` | Nhật ký biến động tồn kho (Nhập/Xuất/Bán/Hoàn) |
| 22 | `ProductBatches` | `ProductBatch` | Lô sản phẩm (Số lô, Hạn sử dụng FEFO) |
| 23 | `Suppliers` | `Supplier` | Danh sách nhà cung cấp thuốc/dược phẩm |
| 24 | `PurchaseOrders` | `PurchaseOrder` | Đơn đặt mua hàng gửi Nhà cung cấp |
| 25 | `PurchaseOrderLines` | `PurchaseOrderLine` | Chi tiết từng mặt hàng trong đơn PO |
| 26 | `GoodsReceipts` | `GoodsReceipt` | Phiếu nhập kho thực tế từ đơn PO |
| 27 | `GoodsReceiptLines` | `GoodsReceiptLine` | Chi tiết dòng nhập kho (sinh Lô sản phẩm mới) |
| 28 | `Shipments` | `Shipment` | Thông tin vận chuyển, mã vận đơn GHN, phí ship |
| 29 | `PayOSWebhookEvents` | `PayOSWebhookEvent` | Lưu sự kiện Webhook PayOS (Tránh trùng lặp - Idempotency) |
| 30 | `DbActivityLogs` | `DbActivityLog` | Nhật ký thao tác hệ thống của Admin/Staff |
| 31 | `News` | `News` | Bài viết tin tức y tế và mẹo chăm sóc sức khỏe |
| 32 | `StockAdjustments` | `StockAdjustment` | Phiếu yêu cầu điều chỉnh tồn kho thủ công |
| 33 | `StockAdjustmentDetails` | `StockAdjustmentDetail` | Chi tiết từng sản phẩm trong phiếu điều chỉnh kho |
| 34 | `PromotionCampaigns` | `PromotionCampaign` | Các chiến dịch khuyến mãi Flash Sale theo thời gian |
| + | *AspNetUsers, AspNetRoles...* | *Identity Models* | Bảng mặc định của ASP.NET Core Identity |

---

## 7. CẤU TRÚC THƯ MỤC DỰ ÁN

```
doAnWebNC/                                  (Thư mục gốc Repository)
├── .github/                                File cấu hình GitHub Workflows
├── docker/                                 Thư mục chứa SQL scripts khởi tạo Docker
│   └── sql/LongChauDB.sql
├── docs/                                   Tài liệu thiết kế & Thư mục chứa ảnh Screenshots
│   └── screenshots/                        Thư mục chứa ảnh chụp màn hình giao diện
├── docker-compose.yml                      File cấu hình chạy Docker toàn bộ hệ thống
├── doAnWebNC.sln                           Visual Studio Solution File
│
└── web-ban-thuoc/                          (Thư mục mã nguồn chính ASP.NET Core)
    ├── Program.cs                          ★ Entry point, cấu hình DI Container, Middleware & Seed Data
    ├── GlobalUsings.cs                     Khai báo global namespaces
    ├── ChatHub.cs                          SignalR Hub xử lý Chat Realtime
    ├── appsettings.json                    File cấu hình môi trường chính (DB, Mail, PayOS, GHN)
    ├── appsettings.Development.json        File cấu hình môi trường Dev (AI API Keys)
    ├── Dockerfile                          File đóng gói Docker Container cho Web App
    │
    ├── Controllers/                        Thư mục chứa các Controllers (MVC & API)
    │   ├── HomeController.cs               Xử lý Trang chủ & hiển thị banner/campaign
    │   ├── AuthController.cs               Xử lý Đăng nhập, Đăng ký, Profile, Quên mật khẩu
    │   ├── ProductController.cs            Xử lý Trang danh sách & Chi tiết sản phẩm
    │   ├── CategoriesController.cs         Xử lý Duyệt sản phẩm theo danh mục 3 cấp
    │   ├── CartController.cs               Xử lý Giỏ hàng & Đặt hàng
    │   ├── PayOSController.cs              Xử lý Thanh toán QR Code & Webhook PayOS
    │   ├── AiBotController.cs              API xử lý hội thoại Trợ lý ảo AI
    │   ├── LoyaltyController.cs            Xử lý Trang điểm thưởng & đổi quà
    │   └── Admin/                          Nhóm Controllers dành cho Quản trị viên
    │       ├── AdminHomeController.cs      Dashboard tổng quan
    │       ├── AdminReportController.cs    Báo cáo doanh thu KPI, công nợ, xuất Excel
    │       ├── AdminProductController.cs   Quản lý Sản phẩm & Import Excel
    │       ├── AdminCategoryController.cs  Quản lý Danh mục 3 cấp
    │       ├── AdminOrderController.cs     Quản lý & Duyệt đơn hàng
    │       ├── AdminInventoryController.cs Quản lý Tồn kho & Phiếu điều chỉnh kho SA
    │       ├── AdminPurchaseController.cs  Quản lý Đơn mua hàng PO & Nhập kho GRN
    │       ├── AdminShippingController.cs  Quản lý Vận đơn GHN & In tem mã vạch
    │       ├── AdminDiscountController.cs  Quản lý Flash Sale & Giảm giá sản phẩm
    │       ├── AdminVoucherController.cs   Quản lý Mã giảm giá
    │       ├── AdminBannerController.cs    Quản lý Banner quảng cáo
    │       ├── AdminUserController.cs      Quản lý Khách hàng
    │       ├── AdminStaffController.cs     Quản lý Nhân viên & Phân quyền
    │       ├── AdminSupplierController.cs  Quản lý Nhà cung cấp
    │       ├── AdminWarehouseController.cs Quản lý Thông tin Kho hàng
    │       ├── AdminNewsController.cs      Quản lý Tin tức y tế
    │       ├── AdminChatController.cs      Quản lý Khung Chat hỗ trợ khách hàng
    │       └── AdminActivityLogController.cs Nhật ký thao tác hệ thống
    │
    ├── Models/                             Thư mục chứa 46 Entity Models & ViewModels
    │   ├── LongChauDbContext.cs            ★ Khai báo 34 DbSets & Cấu hình Fluent API Relationships
    │   ├── Product.cs / Order.cs...        Các Entity Models ánh xạ CSDL
    │   └── ...ViewModels.cs                Các ViewModels truyền dữ liệu ra View
    │
    ├── Services/                           Thư mục chứa 19 Services xử lý logic nghiệp vụ
    │   ├── GeminiAiService.cs              Xử lý kết nối Đa mô hình AI (Gemini / Groq / OpenAI)
    │   ├── InventoryService.cs             Xử lý Xuất/Nhập kho, kiểm tra tồn kho FEFO
    │   ├── PayOSService.cs                 Xử lý API Tạo link thanh toán PayOS
    │   ├── GHNService.cs                   Xử lý API Giao Hàng Nhanh (Tính phí ship, tạo vận đơn)
    │   ├── SmtpEmailSender.cs              Dịch vụ gửi mail SMTP thực qua Gmail
    │   ├── NullEmailSender.cs              Dịch vụ gửi mail giả lập (Mock) cho Dev
    │   ├── LoggingEmailSender.cs           Decorator ghi log thao tác gửi mail
    │   ├── OrderEmailService.cs            Tạo và gửi template mail hóa đơn
    │   ├── UserRankService.cs              Tính toán nâng/hạ hạng thành viên
    │   └── ProductExcelImportService.cs    Đọc và kiểm tra file Excel nhập sản phẩm
    │
    ├── Filters/                            Custom Action Filters
    │   └── NavbarFilter.cs                 Filter nạp tự động danh mục vào Navbar
    │
    ├── ViewComponents/                     Các UI Components tái sử dụng
    │   ├── NavbarViewComponent.cs          Component hiển thị Navbar danh mục
    │   ├── AdminSidebarViewComponent.cs    Component hiển thị Sidebar Admin
    │   └── AdminNotificationViewComponent.cs Component hiển thị thông báo chờ duyệt
    │
    ├── Views/                              Tập hợp các file giao diện Razor (.cshtml)
    │   ├── Shared/
    │   │   ├── _Layout.cshtml              Layout chung phía Khách hàng
    │   │   ├── _Header.cshtml / _Footer.cshtml Header và Footer
    │   │   ├── _AiChatPopup.cshtml         Bong bóng Chat AI Dược sĩ
    │   │   ├── _ChatPopup.cshtml           Bong bóng Live Chat với CSKH
    │   │   └── _FilterSidebar.cshtml       Sidebar lọc sản phẩm
    │   └── Admin/                          Thư mục chứa toàn bộ Views giao diện Quản trị
    │
    └── wwwroot/                            Tài nguyên静态 tĩnh của ứng dụng
        ├── css/                            File thiết kế giao diện (site.css, admin.css...)
        ├── js/                             File kịch bản JavaScript (site.js...)
        └── images/                         Hình ảnh sản phẩm, logo, banner
```

---

## 8. HƯỚNG DẪN CẤU HÌNH HỆ THỐNG (appsettings.json)

Tệp `appsettings.json` (hoặc `appsettings.Development.json`) đóng vai trò cấu hình toàn bộ thông số kết nối của ứng dụng. Dưới đây là cấu hình mẫu hoàn chỉnh:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LongChauDB_New;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "EmailSettings": {
    "Enabled": true,
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "your-email@gmail.com",
    "SmtpPass": "your-16-digit-app-password",
    "SenderEmail": "your-email@gmail.com",
    "SenderName": "Nhà Thuốc Long Châu Phake"
  },
  "PayOS": {
    "ClientId": "your-payos-client-id",
    "ApiKey": "your-payos-api-key",
    "ChecksumKey": "your-payos-checksum-key",
    "BaseUrl": "https://api-merchant.payos.vn"
  },
  "GHN": {
    "Token": "your-ghn-api-token",
    "ShopId": "your-ghn-shop-id",
    "BaseUrl": "https://online-gateway.ghn.vn/shiip/public-api/"
  },
  "AiProvider": "Groq",
  "Gemini": {
    "ApiKey": "your-gemini-api-key"
  },
  "Groq": {
    "ApiKey": "your-groq-api-key-starting-with-gsk"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-4o-mini"
  },
  "AppSettings": {
    "BaseUrl": "https://localhost:5226"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## 9. HƯỚNG DẪN CÀI ĐẶT & VẬN HÀNH

### 🐳 Phương pháp 1: Chạy bằng Docker Compose (Khuyên dùng)
*Yêu cầu:* Máy tính đã cài đặt **Docker Desktop**.

1. **Clone mã nguồn từ GitHub:**
   ```bash
   git clone https://github.com/nghieee/doAnWebNC.git
   cd doAnWebNC
   ```

2. **Khởi chạy hệ thống bằng Docker Compose:**
   ```bash
   docker-compose up --build
   ```

3. **Truy cập ứng dụng:**
   * **Website Khách hàng:** `http://localhost:5000`
   * **SQL Server Container:** `localhost:14330`
     - **Tài khoản (User):** `sa`
     - **Mật khẩu (Password):** `MyStrongPassword123!`
     - **Tên CSDL (Database):** `LongChauDB_New`

4. **Dừng hệ thống Docker:**
   ```bash
   docker-compose down
   ```

---

### 💻 Phương pháp 2: Chạy Thủ công trên Máy cục bộ (Local Development)
*Yêu cầu:* Đã cài đặt **.NET 8.0 SDK** và **SQL Server (SQLEXPRESS hoặc LocalDB)**.

1. **Clone mã nguồn từ GitHub:**
   ```bash
   git clone https://github.com/nghieee/doAnWebNC.git
   cd doAnWebNC/web-ban-thuoc
   ```

2. **Cấu hình Chuỗi kết nối Database:**
   Mở tệp `appsettings.json` và cập nhật chuỗi `DefaultConnection` phù hợp với máy của bạn.

3. **Khôi phục Packages & Cập nhật CSDL (Migrations):**
   ```bash
   dotnet restore
   dotnet ef database update
   ```

4. **Khởi chạy ứng dụng Web:**
   ```bash
   dotnet run
   ```
   *Ứng dụng sẽ chạy tại:* `https://localhost:5226` hoặc `http://localhost:5044`

---

## 10. TÀI KHOẢN THỬ NGHIỆM MẶC ĐỊNH

Khi chạy ứng dụng lần đầu, hệ thống sẽ tự động khởi tạo dữ liệu mẫu (Seed Data) bao gồm 3 tài khoản thử nghiệm tương ứng với các vai trò trong hệ thống:

| Vai Trò (Role) | Email Đăng Nhập | Mật Khẩu Mặc Định | Phạm Vi Quyền Hạn Kiểm Thử |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@gmail.com` | `Admin123.` | **Toàn quyền hệ thống**: Báo cáo doanh thu, Quản lý Sản phẩm, Đơn hàng, Voucher, Khách hàng, Cấu hình Khuyến mãi, Quản lý Nhân viên. |
| **WarehouseStaff** | `warehouse@longchau.local` | `Kho123456.` | **Quản lý Kho hàng**: Xem danh sách tồn kho, Tạo đơn mua hàng PO từ nhà cung cấp, Lập phiếu nhập kho GRN, Lập phiếu điều chỉnh tồn kho SA. |
| **CustomerSupport** | `support@longchau.local` | `Support123.` | **Chăm sóc Khách hàng**: Giao diện tiếp nhận và phản hồi tin nhắn Live Chat thời gian thực với người dùng. |

---

## 11. 📸 THƯ VIỆN ĐỀ MỤC ẢNH MÀN HÌNH (SCREENSHOT PLACEHOLDERS)

> *(Dưới đây là các khung đề mục đã được phân loại sẵn. Bạn chỉ cần chụp ảnh màn hình giao diện tương ứng trên trình duyệt và dán đường dẫn ảnh theo cú pháp `![Mô tả ảnh](đường-dẫn-đến-file-ảnh)` vào từng mục bên dưới).*

### 11.1. Giao Diện Khách Hàng (Customer Platform)

#### 🛍️ Trang Chủ & Banner Khuyến Mãi
> *(Khung chụp toàn cảnh Trang chủ: Header, Navbar danh mục, Banner khuyến mãi slider, Khung Flash Sale đếm ngược và Danh sách sản phẩm bán chạy)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Trang chủ tổng quan -->

#### 🔍 Trang Danh Mục & Lọc Sản Phẩm
> *(Khung chụp trang danh mục sản phẩm bao gồm Sidebar bộ lọc giá/thương hiệu/xuất xứ và danh sách sản phẩm hiển thị)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Trang danh mục và bộ lọc -->

#### 💊 Trang Chi Tiết Sản Phẩm & Đánh Giá
> *(Khung chụp chi tiết 1 sản phẩm: Hình ảnh, giá niêm yết/giảm giá, các thuộc tính dược lý thành phần/liều dùng, khung đánh giá sao từ khách hàng)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Trang chi tiết sản phẩm -->

#### 🛒 Trang Giỏ Hàng & Nhập Mã Voucher
> *(Khung chụp giao diện giỏ hàng: Danh sách mặt hàng, tùy chỉnh số lượng, ô nhập mã giảm giá Voucher và tổng tiền tạm tính)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Trang giỏ hàng -->

#### 🚚 Trang Thanh Toán (Checkout) & Tính Phí Ship GHN
> *(Khung chụp màn hình thanh toán: Điền địa chỉ giao hàng 3 cấp Tỉnh/Huyện/Xã, phí vận chuyển GHN tự động và chọn phương thức thanh toán)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Trang thanh toán Checkout -->

#### 💳 Thanh Toán Trực Tuyến QR Code PayOS
> *(Khung chụp màn hình giao diện chuyển hướng thanh toán PayOS có mã QR ngân hàng tự động)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Trang thanh toán PayOS QR -->

#### 🤖 Trợ Lý Ảo Dược Sĩ AI (AI Chatbot Popup)
> *(Khung chụp bong bóng Chatbot AI: Giao diện mờ Glassmorphism, câu hỏi gợi ý nhanh, câu trả lời tư vấn an toàn từ AI và Thẻ sản phẩm kèm nút "Mua ngay")*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Khung chat AI Dược sĩ -->

#### 👤 Trang Hồ Sơ Cá Nhân & Hạng Thành Viên (Profile & Loyalty)
> *(Khung chụp trang Profile cá nhân: Thông tin tài khoản, thẻ Hạng thành viên Bạc/Vàng/Bạch kim, số điểm thưởng tích lũy và Lịch sử đơn hàng)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Trang Profile và Hạng thành viên -->

---

### 11.2. Giao Diện Quản Trị & Nhân Viên (Admin & Staff Platform)

#### 📊 Admin Dashboard & Báo Cáo Doanh Thu KPI
> *(Khung chụp trang tổng quan Admin: Các thẻ KPI doanh thu/đơn hàng/sản phẩm, biểu đồ tăng trưởng doanh thu theo tháng và thông báo chờ duyệt)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Admin Dashboard -->

#### 🛍️ Admin Quản Lý Sản Phẩm & Import Excel
> *(Khung chụp màn hình danh sách sản phẩm Admin: Nút thêm mới, nút Import/Export Excel, trạng thái tồn kho và công cụ lọc)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Admin Quản lý sản phẩm -->

#### 📦 Admin Quản Lý Đơn Hàng & Duyệt Trạng Thái
> *(Khung chụp danh sách đơn hàng Admin: Bộ lọc trạng thái đơn, chi tiết đơn hàng, nút duyệt đóng gói/giao hàng và nút In tem vận đơn GHN)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Admin Quản lý đơn hàng -->

#### 🏭 Admin Quản Lý Kho & Phiếu Điều Chỉnh Tồn Kho (Stock Adjustment)
> *(Khung chụp màn hình quản lý kho: Tồn kho theo từng nhà kho, danh sách lô sản phẩm hạn dùng FEFO và biểu mẫu lập Phiếu điều chỉnh tồn kho)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Admin Quản lý tồn kho -->

#### 📄 Admin In Mẫu Phiếu Xuất Kho GIN A4
> *(Khung chụp xem trước mẫu in phiếu xuất kho A4 tiêu chuẩn có chữ ký thủ kho và kế toán)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Mẫu in phiếu GIN A4 -->

#### 🚚 Admin Quản Lý Vận Đơn & In Tem Mã Vạch Shipper GHN
> *(Khung chụp giao diện quản lý vận đơn và xem trước mẫu in tem tem dán mã vạch GHN)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Admin In tem vận đơn GHN -->

#### ⚡ Admin Quản Lý Chiến Dịch Khuyến Mãi Flash Sale & Voucher
> *(Khung chụp màn hình thiết lập chiến dịch Flash Sale theo danh mục và bảng quản lý phân phối Voucher)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Admin Quản lý Flash Sale và Voucher -->

#### 💬 Admin Quản Lý Live Chat Hỗ Trợ Khách Hàng Real-time
> *(Khung chụp giao diện màn hình Chat Admin: Danh sách khách hàng đang online, khung hội thoại nhắn tin thời gian thực)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Admin Live Chat CSKH -->

#### 📜 Admin Nhật Ký Hoạt Động Hệ Thống (Activity Logs)
> *(Khung chụp màn hình nhật ký theo dõi thao tác người dùng: Thời gian, tài khoản, hành động thêm/sửa/xóa và đối tượng tác động)*  
<!-- [DÁN_ẢNH_TẠI_ĐÂY]: Admin Activity Logs -->

---

*Dự án được xây dựng và phát triển cho mục đích Học tập, Nghiên cứu và Báo cáo Chuyên đề Web Nâng cao.*
