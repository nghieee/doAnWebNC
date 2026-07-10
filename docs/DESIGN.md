# 🎨 DESIGN.md — NHÀ THUỐC LONG CHÂU (PHIÊN BẢN TRƯỚC NÂNG CẤP)

> **Mục đích file này**: Tài liệu hóa trạng thái thiết kế UX/UI hiện tại của storefront trước khi nâng cấp lên phiên bản "premium". Sau khi nâng cấp, file này sẽ được cập nhật lại để phản ánh ngôn ngữ thiết kế mới.
> **Cập nhật lần cuối**: 2026-07-06
> **Phạm vi**: Giao diện người dùng phía khách (storefront) — không bao gồm admin.

---

## 1. TỔNG QUAN

| Thuộc tính | Giá trị |
|---|---|
| **Tên hệ thống** | Nhà Thuốc Long Châu — Storefront |
| **Target** | Khách hàng mua thuốc / TPCN / DCMP online |
| **Frontend stack** | Razor Views (.cshtml) + Bootstrap 5 + jQuery + Font Awesome 6 |
| **Fonts** | Inter (Google Fonts) |
| **Layout system** | Bootstrap 5 grid + custom CSS classes |
| **Các layout** | `_Layout.cshtml` (user) — duy nhất cho mọi trang khách |

---

## 2. CẤU TRÚC LAYOUT HIỆN TẠI

```
┌─────────────────────────────────────────────────────────┐
│ HEADER (`_Header.cshtml`)                                │
│ - Top bar (welcome / email / hotline)                   │
│ - Logo + Search box + Account/Cart                      │
├─────────────────────────────────────────────────────────┤
│ NAVBAR (ViewComponent — Categories 3 cấp)               │
├─────────────────────────────────────────────────────────┤
│ MAIN CONTENT (RenderBody)                                │
│   - container py-4                                       │
├─────────────────────────────────────────────────────────┤
│ FOOTER (`_Footer.cshtml`)                                │
│ - Top banner + 5 cột (Về tôi / Danh mục / Tổng đài /    │
│   Kết nối / Chứng nhận)                                  │
├─────────────────────────────────────────────────────────┤
│ AI Chat popup (`_AiChatPopup.cshtml`)                    │
│ Chat popup (`_ChatPopup.cshtml`)                        │
│ Toast container                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 3. BẢNG MÀU & TYPOGRAPHY HIỆN TẠI

### 3.1 Màu chính (đang dùng rải rác)

| Token | Giá trị | Vai trò |
|---|---|---|
| Primary | `#0d6efd` / `#3b82f6` / `#2563eb` (không đồng nhất) | Nút chính, link |
| Secondary | `#6c757d` | Chữ phụ |
| Background chính | `#EAEFFA` (`#root` style) | Body nền xanh nhạt |
| Background phụ | `#ffffff` | Card, header |
| Text chính | `#212529` / `#0f172a` |  |
| Text phụ | `#64748b` / `#6b7280` |  |
| Border | `#e5e7eb`, `#dee2e6`, `#e2e8f0` |  |

⚠️ **Vấn đề**: Màu primary bị phân tán giữa ít nhất 3 hex khác nhau (`#0d6efd`, `#3b82f6`, `#2563eb`).

### 3.2 Typography

| Element | Size / Weight |
|---|---|
| Body | `Inter`, 14px (`html { font-size: 14px }`) |
| H1 hero | 1.4rem / 700 |
| H3 section | 1.2rem / medium |
| Small / muted | 0.875rem, 0.75rem |

⚠️ **Vấn đề**: Thiếu thang type rõ ràng (`xs` → `3xl`). Font-size 14px ở root gây cảm giác nén chữ.

### 3.3 Spacing & radii

| Token | Giá trị |
|---|---|
| Border-radius nhỏ | `0.25rem` |
| Border-radius card | `0.5rem` / `0.75rem` / `1rem` / `1.5rem` (không thống nhất) |
| Border-radius pill | `50rem` (nút) |
| Box-shadow | `0 4px 12px rgba(0,0,0,.15)` (chuẩn duy nhất) |

---

## 4. HEADER & NAVIGATION

### Header (`_Header.cshtml`)
- **Top bar**: welcome text + email + hotline (chỉ hiện desktop ≥ lg)
- **Logo**: ảnh `/images/default/header_logo_brand.png`, height 56px
- **Search**: input pill, fetch live `/Products/Suggest?q=...` với debounce 300ms, hiển thị categories + brands + products
- **Auth area**: dropdown khi đã đăng nhập; link "Đăng nhập" khi chưa
- **Cart**: button pill primary, link sang `/Cart`

### Navbar (`Views/Shared/Components/Navbar/Default.cshtml`)
- Menu ngang hiển thị các Category parent (Level 1)
- Hover → mở menu thả xuống 2 cột: cấp 2 bên trái + cấp 3 bên phải
- Đổi child (cấp 2) → đổi grandchildren (cấp 3) bằng JS

### Footer (`_Footer.cshtml`)
- Top banner image (`/images/default/footer_top_banner.png`)
- 5 cột: Về chúng tôi / Danh mục / Tổng đài / Kết nối / Chứng nhận
- Bottom: bản quyền + địa chỉ công ty

---

## 5. TRANG CHỦ (`Views/Home/Index.cshtml`)

### Cấu trúc
1. **Full-Width Carousel** (banner `FullWidth`)
2. **Slider 2 cột** (banner `Main` 8 col + `Side` 4 col)
3. **Danh mục nổi bật** (6 col grid)
4. **Sản phẩm nổi bật** (6 col grid)
5. **Doctor section** (CTA background image)

### Vấn đề
- Carousel bootstrap mặc định — hơi "generic"
- Filter feature categories bị thiếu trên mobile
- Không có section "Sản phẩm bán chạy", "Thương hiệu"
- Banner `Side` chỉ lấy tối đa 2 — nếu nhiều hơn không hiển thị
- Không có empty state chuyên nghiệp khi thiếu data

---

## 6. TRANG DANH MỤC (`Categories/Index.cshtml`)

### Cấu trúc
- Breadcrumb (Trang chủ / Danh mục)
- Grid 2 cột: Filter sidebar (3 col) + Product list (9 col)
- Sort: Bán chạy / Tên / Giá thấp / Giá cao
- Pagination: dynamic page count

### Filter sidebar (`_FilterSidebar.cshtml`)
- Filter: Thương hiệu (checkbox) + Giá (radio) + Nguồn gốc (checkbox)
- Active filters hiển thị dạng "chip" có thể xóa
- AJAX filter (không reload trang)

### Vấn đề
- Sidebar trên mobile ẩn hoàn toàn — không có drawer/modal để mở filter
- Product card dùng chung `_ProductList.cshtml` nhưng thiếu rating / "Yêu thích"
- Không có skeleton loading

---

## 7. TRANG CHI TIẾT SP (`Product/Details.cshtml`)

### Cấu trúc
- Breadcrumb
- Grid: image slider (sticky, 5 col) + product info (7 col)
- Tab: Mô tả / Hướng dẫn / Lưu ý
- Phần đánh giá (reviews)
- Phần sản phẩm liên quan

### Style
- Đã có custom CSS `.pd-*` riêng — khá đầy đủ (nền gradient, sticky, badges)
- Tuy nhiên chiếm **39 KB** và rất nhiều inline style

### Vấn đề
- 39KB (lớn nhất frontend user) — khó bảo trì
- Tabs chỉ dùng cho 1 dòng sản phẩm — cảm giác lặp
- Không có "Sticky bottom add-to-cart" trên mobile

---

## 8. GIỎ HÀNG (`Cart/Index.cshtml`)

### Cấu trúc
- Card danh sách sản phẩm (ảnh + tên + qty control + tổng + nút xóa)
- Card tổng đơn (subtotal + voucher input + total + checkout button)
- Modal checkout (popup)

### Vấn đề
- Card sản phẩm trống: icon shopping cart khá basic
- Voucher input nằm chung — khó phân biệt đã áp dụng/chưa
- Checkout popup chứa nhiều field — hơi dài

---

## 9. AUTH (`Auth/Index.cshtml`)

### Cấu trúc
- 2 cột: Login (5 col) + Register (6 col) — desktop
- Card padding, shadow nhẹ, gradient button

### Vấn đề
- Cảm giác **hơi "admin form"** — không có hero/marketing
- ReCAPTCHA notice chỉ là text — không có CTA marketing
- Banner giới thiệu (loyalty, đổi quà) **không có**

---

## 10. ASSETS HIỆN CÓ

### `/wwwroot/images/`
```
banners/
  2_3_banner1.png          (image nằm ngang — dùng cho ratio 2:3?)
  2_3_banner2.png
  3_2_banner1.png          (image nằm dọc — ratio 3:2)
  fullWidth_banner1.png    (banner toàn chiều ngang)
  fullWidth_banner2.png

categories/                (ảnh icon cho từng Category)
certifications/            (bo_cong_thuong.png, dmca.png)
default/
  header_desktop.png       (background gradient cho header)
  header_logo_brand.png    (logo)
  home_bgdoctor.png        (background section doctor)
  home_bgdoctor2.png       (ảnh bác sĩ)
  footer_top_banner.png    (banner trên footer)
  title.png                (?)
  ai-bot-doctor.png        (icon chatbot)
  gift.png                 (popup gift)
products/                  (ảnh sản phẩm)
social/
  facebook_logo.png
  zalo_logo.png
  download_qr.png
```

### Skeleton thư mục
- ❌ Không có `/css/storefront.css` riêng (mọi thứ trong `site.css` + inline)
- ❌ Không có design tokens file

---

## 11. CÁC VẤN ĐỀ UX/UI TỔNG QUAN

1. **Màu primary không thống nhất** — 3+ hex khác nhau xuất hiện cùng lúc
2. **Spacing & radius không có hệ thống** — mỗi file dùng 1 bộ riêng
3. **Typography scale thiếu** — không có `xs/sm/md/lg/xl/2xl/3xl`
4. **Shadow rất đơn điệu** — chỉ 1 quy ước duy nhất
5. **Không có design tokens** (variables)
6. **State rỗng/loading/error chưa được polish**
7. **Trên mobile chưa được tối ưu** — sidebar filter ẩn hoàn toàn
8. **Nút CTA chưa nổi bật** — không có gradient/icon nhất quán
9. **Thiếu trust signals** — chứng nhận, cam kết chỉ ở footer
10. **AI/Chat popup chưa tuân theo 1 ngôn ngữ** — popup trôi nổi không có vị trí

---

> 📝 **Bước tiếp theo**: Nâng cấp UX/UI dựa trên đánh giá này — thống nhất color tokens, type scale, spacing, radius, shadow. Sau đó cập nhật file này thành phiên bản mới.
