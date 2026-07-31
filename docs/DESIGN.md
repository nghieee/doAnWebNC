# 🎨 DESIGN.md — NHÀ THUỐC LONG CHÂU (PHIÊN BẢN PREMIUM)

> **Mục đích file này**: Tài liệu hóa ngôn ngữ thiết kế, thiết kế UI/UX hiện tại và các nâng cấp giao diện "premium" đã thực hiện cho hệ thống Storefront và Admin.
> **Cập nhật lần cuối**: 2026-07-24
> **Phạm vi**: Toàn bộ hệ thống giao diện (Storefront khách hàng & Admin Back-office).

---

## 1. TỔNG QUAN HỆ THỐNG GIAO DIỆN

| Thuộc tính | Giá trị |
|---|---|
| **Tên hệ thống** | Nhà Thuốc Long Châu — Premium E-Commerce Platform |
| **Đối tượng hướng tới** | Khách hàng mua thuốc / TPCN / Dược mỹ phẩm online |
| **Frontend Stack** | Razor Views (.cshtml) + CSS Variables + Bootstrap 5 + jQuery + Font Awesome 6 + SignalR |
| **Typography** | Inter, Outfit & Roboto (Google Fonts) |
| **Layout System** | Bootstrap 5 grid + Custom Utility Classes + Modern Flexbox/Grid |
| **Các Layout chính** | `_Layout.cshtml` (Người dùng storefront) & `Admin/_Layout.cshtml` (Quản trị viên) |

---

## 2. NÂNG CẤP THIẾT KẾ PREMIUM (PHIÊN BẢN HIỆN TẠI)

Hệ thống đã trải qua đợt nâng cấp giao diện toàn diện nhằm mang lại trải nghiệm mua sắm dược phẩm đẳng cấp, chuyên nghiệp và tối ưu tỷ lệ chuyển đổi:

### 2.1 Bảng màu đồng nhất & Thiết kế Tokens
Primary color đã được đồng nhất hóa thông qua biến CSS (CSS Variables) để loại bỏ sự phân tán màu sắc cũ:
- **Primary Color**: `#0052cc` (Xanh dương thương hiệu chuẩn, sâu và chuyên nghiệp).
- **Secondary Color**: `#64748b` (Màu xám Slate tinh tế cho chữ phụ).
- **Accent Red (Sales/Alerts)**: `#ef4444` (Mỏ neo màu đỏ cho các cảnh báo, giảm giá, và countdown).
- **Theme Background**: `#f8fafc` (Nền xám nhạt hiện đại thay thế cho nền xanh quá đậm cũ).

### 2.2 Bộ lọc AJAX Sidebar Cao cấp (`_FilterSidebar.cshtml`)
- **Visuals**: Sử dụng đổ bóng nhẹ, tinh tế (`box-shadow: 0 4px 20px rgba(0,0,0,0.04)`), viền mỏng (`border: 1px solid #f1f5f9`), và bo góc lớn (`border-radius: 12px`).
- **Interactive**: 
  - Các checkbox và radio button được đổi màu xanh dương đồng bộ với thương hiệu.
  - Các nhãn tags đang lọc (active filter chips) được cách điệu dạng hình con nhộng bo tròn dễ thương, có nút tắt `x` để xóa nhanh.
  - Tự động lọc sản phẩm bất đồng bộ (AJAX) không reload trang, đi kèm hiệu ứng chuyển đổi mượt mà.

### 2.3 Thiết kế Card Sản phẩm Tối ưu (`_ProductList.cshtml`)
- **Tỉ lệ chiều cao đồng đều**: Khắc phục triệt để lỗi lệch layout khi sản phẩm có/không có giảm giá. Khi sản phẩm không giảm giá, bình chứa giá gạch ngang gốc được **ẩn đi bằng CSS** (`visibility: hidden` hoặc giữ nguyên chiều cao) thay vì xóa bỏ hoàn toàn khỏi DOM, giúp các hàng sản phẩm thẳng tắp, chuyên nghiệp.
- **Nhãn phần trăm giảm giá đỏ rực**: 
  - Thêm một nhãn đỏ góc trên bên trái của ảnh sản phẩm (ví dụ: `-15%`).
  - Thiết kế bo góc nhẹ, đổ bóng và sử dụng hiệu ứng màu chuyển từ đỏ tươi sang cam cháy để kích thích thị giác mua sắm.

### 2.4 Hộp đếm ngược khuyến mãi thực tế trên Trang chi tiết (`Details.cshtml`)
- **Vị trí**: Nằm nổi bật giữa thông tin giá tiền và nút chọn số lượng đặt hàng.
- **Phong cách**: Khung viền màu đỏ nhạt, nền gradient ấm áp (`linear-gradient(135deg, #fff5f5 0%, #fff0f0 100%)`) kèm biểu tượng ngọn lửa chuyển động (`fa-fire animate-pulse`).
- **Đồng hồ đếm ngược (Countdown Timer)**: Hiển thị Ngày, Giờ, Phút, Giây chia làm 4 khối riêng biệt màu đỏ đậm chữ trắng, tự động đếm lùi từng giây nhờ JavaScript thời gian thực. Tự động ẩn đi khi chiến dịch kết thúc.

### 2.5 Bảng chọn sản phẩm đa năng trong Admin Discount (`Admin/Discount/Index.cshtml`)
- Thay thế dropdown lựa chọn sản phẩm thủ công đơn điệu bằng **Bảng danh mục sản phẩm nâng cao**:
  - Tích hợp ô tìm kiếm nhanh (Search input) sản phẩm theo tên/SKU.
  - Phân trang bất đồng bộ cho danh sách sản phẩm.
  - Checkbox chọn hàng loạt sản phẩm cực kỳ nhanh chóng để áp dụng hoặc gỡ bỏ chiến dịch giảm giá.

---

## 3. CẤU TRÚC LAYOUT HIỆN TẠI

```
┌─────────────────────────────────────────────────────────┐
│ HEADER (`_Header.cshtml`)                                │
│ - Top bar (welcome / email / hotline)                   │
│ - Logo + Search box + Account/Cart                      │
├─────────────────────────────────────────────────────────┤
│ NAVBAR (ViewComponent — Categories 3 cấp tự động)       │
├─────────────────────────────────────────────────────────┤
│ MAIN CONTENT (RenderBody)                                │
│   - container py-4 (Storefront)                         │
├─────────────────────────────────────────────────────────┤
│ PROMOTION BANNER LINKING SYSTEM                          │
│   - Tự động liên kết click Banner sang danh sách        │
│     sản phẩm của chiến dịch/bộ lọc tương ứng.           │
├─────────────────────────────────────────────────────────┤
│ FOOTER (`_Footer.cshtml`)                                │
│ - Top banner + 5 cột (Về tôi / Danh mục / Tổng đài /    │
│   Kết nối / Chứng nhận)                                  │
├─────────────────────────────────────────────────────────┤
│ CHAT & AI CUSTOMER SERVICE POPUPS                       │
│ - Live Chat SignalR & Trợ lý ảo Google Gemini AI        │
└─────────────────────────────────────────────────────────┘
```

---

## 4. CHI TIẾT CÁC MÀU SẮC CHỦ ĐẠO (CSS VARIABLES)

```css
:root {
  --primary-color: #0052cc;
  --primary-hover: #0747a6;
  --secondary-color: #64748b;
  --danger-color: #ef4444;
  --success-color: #22c55e;
  --warning-color: #f59e0b;
  --bg-main: #f8fafc;
  --card-shadow: 0 4px 20px rgba(0, 0, 0, 0.04);
  --border-radius-base: 12px;
}
```

---

## 5. TỐI ƯU HÓA MOBILE (RESPONSIVE)

- **Sidebar bộ lọc**: Tự động thu gọn và xếp chồng trên thiết bị di động, bổ sung nút chuyển đổi hiển thị bộ lọc nhanh.
- **Card sản phẩm**: Tự động chuyển đổi từ grid 4-cột (desktop) sang grid 2-cột (mobile) mà không làm vỡ các nhãn giảm giá đỏ hay thông tin giá cả.
- **Hộp quà khuyến mãi & Countdown**: Co giãn linh hoạt các ô đếm ngược theo độ rộng màn hình.
