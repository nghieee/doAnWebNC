# Chức Năng Tìm Kiếm - Nhà Thuốc Long Châu

## Tổng Quan
Chức năng tìm kiếm cho phép người dùng tìm kiếm sản phẩm, danh mục và thương hiệu một cách nhanh chóng và hiệu quả. Sử dụng Bootstrap 5 thuần túy và tận dụng trang danh sách sản phẩm hiện có.

## Tính Năng

### 1. Tìm Kiếm Tự Động (Auto-suggest)
- **Vị trí**: Thanh tìm kiếm ở header
- **Cách hoạt động**: 
  - Gõ ít nhất 2 ký tự để kích hoạt gợi ý
  - Hiển thị gợi ý theo 3 loại:
    - **Sản phẩm**: Hiển thị tên, thương hiệu và hình ảnh
    - **Danh mục**: Chỉ hiển thị danh mục cấp 2 và 3
    - **Thương hiệu**: Hiển thị các thương hiệu phù hợp

### 2. Trang Kết Quả Tìm Kiếm
- **URL**: `/Categories/0?search={từ khóa}`
- **Tính năng**:
  - Tận dụng trang danh sách sản phẩm hiện có
  - Hiển thị danh sách sản phẩm phù hợp
  - Phân trang (20 sản phẩm/trang)
  - Sắp xếp theo: Bán chạy, Tên, Giá tăng/giảm
  - Breadcrumb navigation
  - Thêm vào giỏ hàng trực tiếp
  - Bộ lọc bên trái (thương hiệu, nguồn gốc, giá)

### 3. Tìm Kiếm Theo Danh Mục
- **URL**: `/Categories/{categoryId}`
- **Tính năng**: Hiển thị sản phẩm theo danh mục cụ thể

## Cấu Trúc Code

### Controllers
- `ProductController.cs`: Xử lý gợi ý tìm kiếm và redirect
- `CategoriesController.cs`: Xử lý hiển thị theo danh mục và tìm kiếm toàn bộ

### Views
- `_Header.cshtml`: Thanh tìm kiếm với JavaScript
- `Categories/Index.cshtml`: Trang danh sách sản phẩm (dùng chung cho cả danh mục và tìm kiếm)
- `_ProductList.cshtml`: Partial view hiển thị danh sách sản phẩm

### CSS
- `wwwroot/css/site.css`: Styles cho chức năng tìm kiếm (Bootstrap 5 thuần túy)

## API Endpoints

### 1. Suggest API
```
GET /Products/Suggest?query={từ khóa}
```
**Response:**
```json
{
  "products": [
    {
      "productId": 1,
      "productName": "Tên sản phẩm",
      "brand": "Thương hiệu",
      "imageUrl": "tên_file_ảnh.png"
    }
  ],
  "categories": [
    {
      "categoryId": 1,
      "categoryName": "Tên danh mục",
      "categoryLevel": "2"
    }
  ],
  "brands": ["Thương hiệu 1", "Thương hiệu 2"]
}
```

### 2. Search/Categories API
```
GET /Categories/0?search={từ khóa}&page={số trang}&sort={loại sắp xếp}
```
**Parameters:**
- `search`: Từ khóa tìm kiếm
- `page`: Số trang (mặc định: 1)
- `sort`: Loại sắp xếp (empty, name, price_asc, price_desc)
- `brands`: Mảng thương hiệu để lọc
- `origins`: Mảng nguồn gốc để lọc
- `priceRange`: Khoảng giá (1, 2, 3)

## Logic Tìm Kiếm

### Sản Phẩm
- Tìm theo tên sản phẩm (contains)
- Tìm theo thương hiệu (contains)
- Tìm theo danh mục (thông qua CategoryId)

### Danh Mục
- Chỉ tìm trong danh mục cấp 2 và 3
- Tìm theo tên danh mục (contains)

### Thương Hiệu
- Lấy từ trường Brand của sản phẩm
- Loại bỏ trùng lặp (distinct)
- Chỉ hiển thị sản phẩm đang hoạt động (IsActive = true)

## Tính Năng Nâng Cao

### 1. Loading State
- Hiển thị spinner khi đang tìm kiếm
- Debounce 300ms để tránh gọi API quá nhiều

### 2. Error Handling
- Xử lý lỗi network
- Hiển thị thông báo lỗi thân thiện
- Log lỗi để debug

### 3. Responsive Design
- Tối ưu cho mobile và desktop
- Dropdown có thể scroll khi có nhiều kết quả
- Sử dụng Bootstrap 5 classes

### 4. Accessibility
- Keyboard navigation (ESC để đóng)
- ARIA labels
- Screen reader friendly

### 5. Performance
- Tận dụng trang hiện có thay vì tạo mới
- Phân trang hiệu quả
- Lazy loading cho hình ảnh

## Bootstrap 5 Classes Sử Dụng

### Layout
- `container`, `row`, `col-*`: Grid system
- `d-flex`, `justify-content-*`, `align-items-*`: Flexbox utilities
- `position-relative`, `position-absolute`: Positioning

### Components
- `btn`, `btn-*`: Buttons
- `form-control`, `form-select`: Form controls
- `list-group`, `list-group-item`: List groups
- `pagination`, `page-item`, `page-link`: Pagination
- `breadcrumb`, `breadcrumb-item`: Breadcrumbs
- `spinner-border`: Loading spinners

### Utilities
- `text-*`, `bg-*`: Colors
- `shadow-*`: Shadows
- `rounded-*`: Border radius
- `border-*`: Borders
- `p-*`, `m-*`: Spacing
- `d-none`, `d-*`: Display utilities

## Cải Tiến Tương Lai

1. **Tìm kiếm nâng cao**: Thêm filter theo giá, danh mục
2. **Tìm kiếm theo từ khóa**: Sử dụng full-text search
3. **Lịch sử tìm kiếm**: Lưu và hiển thị từ khóa đã tìm
4. **Gợi ý thông minh**: Dựa trên lịch sử mua hàng
5. **Tìm kiếm theo hình ảnh**: Upload ảnh để tìm sản phẩm tương tự

## Troubleshooting

### Vấn đề thường gặp:
1. **"undefined" hiển thị**: Kiểm tra dữ liệu trong database
2. **Ảnh không hiển thị**: Kiểm tra file `default.png` trong thư mục images/products
3. **Gợi ý không hoạt động**: Kiểm tra console browser và logs server
4. **Performance chậm**: Thêm index cho các trường tìm kiếm

### Debug:
- Kiểm tra Network tab trong Developer Tools
- Xem logs trong console browser
- Kiểm tra logs server trong Visual Studio

## Lưu Ý Kỹ Thuật

- Sử dụng Bootstrap 5 thuần túy, không có CSS tùy chỉnh phức tạp
- Tận dụng trang Categories/Index cho cả danh mục và tìm kiếm
- URL structure: `/Categories/0?search=...` cho tìm kiếm toàn bộ
- Responsive design với Bootstrap 5 breakpoints
- Performance tối ưu với phân trang và lazy loading 