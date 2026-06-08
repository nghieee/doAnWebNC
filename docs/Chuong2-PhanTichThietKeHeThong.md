# CHƯƠNG 2 – PHÂN TÍCH, THIẾT KẾ HỆ THỐNG

Chương này trình bày phân tích yêu cầu, use case, thiết kế cơ sở dữ liệu và thiết kế kiến trúc của hệ thống website bán thuốc trực tuyến Nhà thuốc Long Châu.

---

## 2.1. Phân tích yêu cầu hệ thống

### 2.1.1. Yêu cầu chức năng

Hệ thống được phân tích theo hai nhóm người dùng chính: khách hàng và quản trị viên.

**Đối với khách hàng:**

- **Quản lý tài khoản:** Đăng ký, đăng nhập, đăng xuất; quên mật khẩu (xác thực qua email); xem và cập nhật hồ sơ cá nhân. Hệ thống hỗ trợ xếp hạng khách hàng (Bạc, Vàng, Bạch kim) dựa trên tổng chi tiêu trong 6 tháng và gửi email thông báo khi lên hạng hoặc nhận voucher.
- **Mua sắm:** Duyệt danh mục sản phẩm theo cấu trúc nhiều cấp (Level 1, 2, 3); tìm kiếm sản phẩm theo tên, thương hiệu, danh mục; lọc theo giá, xuất xứ, thương hiệu; xem chi tiết sản phẩm (thông tin, hình ảnh, đánh giá); thêm vào giỏ hàng, chỉnh số lượng, áp dụng voucher; đặt hàng với thông tin giao hàng và thanh toán (tiền mặt hoặc PayOS); theo dõi trạng thái đơn hàng.
- **Tương tác:** Đánh giá sản phẩm (chỉ sau khi đã mua); chat trực tuyến với bộ phận hỗ trợ (SignalR); nhận thông báo voucher mới (qua email hoặc trong tài khoản).

**Đối với quản trị viên (Admin):**

- **Dashboard:** Thống kê tổng quan (số đơn hàng, doanh thu, sản phẩm, khách hàng); biểu đồ doanh thu theo tháng; thông báo đơn hàng chờ xác nhận và tin nhắn chat chưa đọc.
- **Quản lý sản phẩm:** CRUD sản phẩm (tên, giá, mô tả, hình ảnh, tồn kho, xuất xứ, danh mục); quản lý danh mục nhiều cấp; tìm kiếm và lọc sản phẩm.
- **Quản lý đơn hàng:** Xem danh sách đơn hàng; cập nhật trạng thái (Chờ xác nhận, Đã xác nhận, Đang giao, Đã giao, Hủy); tìm kiếm theo tên, số điện thoại; lọc theo trạng thái.
- **Quản lý voucher:** Tạo voucher (mã, mô tả, loại giảm giá %, số tiền cố định, áp dụng toàn đơn hoặc theo danh mục, hạn dùng, số lượt dùng tối đa); phân phối voucher cho khách hàng; theo dõi lượt sử dụng; xóa hoặc vô hiệu hóa voucher.
- **Quản lý banner:** CRUD banner (tiêu đề, ảnh, link, loại: chính/phụ/khuyến mãi); sắp xếp thứ tự hiển thị; xóa hàng loạt.
- **Quản lý người dùng:** Xem danh sách khách hàng; thông tin xếp hạng và chi tiêu; xuất danh sách Excel; khóa/mở khóa tài khoản.
- **Quản lý chat:** Chat trực tuyến với khách hàng; xem tin nhắn chưa đọc và lịch sử chat.

### 2.1.2. Yêu cầu phi chức năng

- **Hiệu năng:** Trang chủ và danh sách sản phẩm tải trong thời gian chấp nhận được; truy vấn cơ sở dữ liệu được tối ưu (index, include cần thiết).
- **Bảo mật:** Xác thực bằng ASP.NET Core Identity; phân quyền theo vai trò (Admin/User); bảo vệ CSRF; kiểm tra dữ liệu đầu vào; mật khẩu được băm, không lưu dạng thường.
- **Khả năng mở rộng:** Kiến trúc phân tầng (MVC, Services), sử dụng interface và Dependency Injection để dễ thay thế hoặc bổ sung module (ví dụ đổi cổng thanh toán, thêm kênh gửi email).
- **Sử dụng lại:** Các service (PayOS, Email, UserRank) tách biệt, View Component và Filter dùng chung cho nhiều trang.

---

## 2.2. Biểu đồ use case

Hệ thống có hai tác nhân chính: **Khách hàng** và **Quản trị viên**. Các use case tiêu biểu được tóm tắt như sau.

**Khách hàng:** Đăng ký, Đăng nhập, Quên mật khẩu, Xem/Cập nhật hồ sơ, Duyệt danh mục, Tìm kiếm/Lọc sản phẩm, Xem chi tiết sản phẩm, Thêm/Xóa sản phẩm giỏ hàng, Áp dụng voucher, Đặt hàng, Thanh toán (tiền mặt/PayOS), Theo dõi đơn hàng, Đánh giá sản phẩm, Chat với admin, Xem voucher của tôi.

**Quản trị viên:** Đăng nhập (Admin), Xem dashboard, Quản lý danh mục, Quản lý sản phẩm, Quản lý đơn hàng, Quản lý voucher, Quản lý banner, Quản lý người dùng, Chat với khách hàng.

*Bạn có thể chèn **Hình 2.1. Biểu đồ use case** (vẽ bằng công cụ như Draw.io, StarUML, hoặc Visio) tại đây, mô tả các actor và use case tương ứng.*

---

## 2.3. Thiết kế cơ sở dữ liệu

Cơ sở dữ liệu được thiết kế theo mô hình quan hệ, triển khai bằng SQL Server và Entity Framework Core (code-first). Các bảng chính và quan hệ được mô tả dưới đây.

### 2.3.1. Các thực thể chính

- **AspNetUsers / AspNetRoles:** Bảng người dùng và vai trò do ASP.NET Core Identity quản lý (đăng ký, đăng nhập, phân quyền).
- **Category:** Danh mục sản phẩm nhiều cấp; có quan hệ tự tham chiếu qua `ParentCategoryId` (Category 1 → Category 2 → Category 3).
- **Product:** Sản phẩm (tên, giá, thương hiệu, xuất xứ, mô tả, tồn kho, danh mục); liên kết với Category (N–1), OrderItem (1–N), ProductImage (1–N), Review (1–N).
- **ProductImage:** Hình ảnh sản phẩm (đường dẫn, cờ ảnh chính).
- **Order:** Đơn hàng (người dùng, ngày đặt, tổng tiền, trạng thái, địa chỉ giao, họ tên, số điện thoại, mã voucher, số tiền giảm); liên kết với AspNetUsers (N–1), OrderItem (1–N), Payment (1–N). Trạng thái đơn: Cart, Chờ xác nhận, Đã xác nhận, Đang giao, Đã giao, Hủy.
- **OrderItem:** Chi tiết đơn hàng (sản phẩm, số lượng, đơn giá); liên kết Order (N–1), Product (N–1).
- **Payment:** Thanh toán (đơn hàng, số tiền, trạng thái, mã giao dịch PayOS nếu có).
- **Voucher:** Mã giảm giá (mã, mô tả, loại giảm giá, giá trị %, số tiền cố định, áp dụng toàn đơn hoặc theo danh mục, hạn dùng, số lượt dùng tối đa, số lượt đã dùng).
- **UserVoucher:** Liên kết người dùng – voucher (ai được cấp voucher nào, đã dùng chưa, ngày dùng).
- **UserRankInfo:** Thông tin xếp hạng khách hàng (tổng chi tiêu, chi tiêu 6 tháng, hạng Bạc/Vàng/Bạch kim, ngày reset, ngày gửi mail).
- **Review:** Đánh giá sản phẩm (người dùng, sản phẩm, nội dung, điểm, ngày).
- **ChatMessage:** Tin nhắn chat (người gửi, người nhận, nội dung, thời gian, đã đọc chưa).
- **Banner:** Banner hiển thị (tiêu đề, ảnh, link, loại, thứ tự, trạng thái).
- **InventoryTransaction:** Giao dịch tồn kho (sản phẩm, số lượng, loại giao dịch) – phục vụ theo dõi nhập/xuất nếu mở rộng.

### 2.3.2. Quan hệ giữa các thực thể

- **Category – Category:** quan hệ cha-con (ParentCategoryId) → danh mục nhiều cấp.
- **Category – Product:** 1–N (một danh mục có nhiều sản phẩm).
- **AspNetUsers – Order:** 1–N (một user có nhiều đơn hàng).
- **Order – OrderItem:** 1–N; **OrderItem – Product:** N–1.
- **Order – Payment:** 1–N (một đơn có thể có nhiều lần thanh toán/hoàn tiền).
- **AspNetUsers – UserVoucher – Voucher:** N–N qua bảng UserVoucher (user được cấp nhiều voucher, voucher có thể cấp cho nhiều user).
- **AspNetUsers – UserRankInfo:** 1–1 (mỗi user một bản ghi xếp hạng).
- **AspNetUsers – Review:** 1–N; **Product – Review:** 1–N.

*Bạn có thể chèn **Hình 2.2. Sơ đồ thực thể quan hệ (ERD)** tại đây. Trong thư mục `docs/screenshots` đã có file `ERD.jpg` – có thể dùng làm Hình 2.2 nếu nội dung khớp với thiết kế trên.*

---

## 2.4. Thiết kế kiến trúc và luồng xử lý

### 2.4.1. Kiến trúc tổng thể

Kiến trúc hệ thống tuân theo mô hình nhiều tầng và MVC như đã trình bày tại Chương 1 (mục 1.2.1). Tầng Presentation (Razor Views, Controllers) tiếp nhận request và hiển thị giao diện; tầng Business (Services) xử lý logic nghiệp vụ; tầng Data Access (Entity Framework Core, LongChauDbContext) truy xuất dữ liệu. Các hệ thống ngoài (SQL Server, PayOS API, SMTP, SignalR) được tích hợp thông qua các service tương ứng. *Có thể tham chiếu lại Hình 1.1. Kiến trúc hệ thống.*

### 2.4.2. Luồng xử lý chính

**Luồng đặt hàng và thanh toán (khách hàng):** Khách đăng nhập → chọn sản phẩm → thêm vào giỏ (Order trạng thái Cart, OrderItem) → vào trang thanh toán, nhập địa chỉ, chọn voucher (nếu có) → xác nhận đặt hàng (cập nhật Order trạng thái Chờ xác nhận, gửi email xác nhận qua IOrderEmailService) → chọn thanh toán PayOS hoặc tiền mặt. Nếu PayOS: PayOSController gọi IPayOSService tạo link thanh toán → khách chuyển sang PayOS → sau khi thanh toán xong, webhook/callback cập nhật Payment và trạng thái đơn, gửi email thanh toán thành công/thất bại. UserRankService có thể được gọi sau khi đơn chuyển sang Đã giao để cập nhật xếp hạng và tặng voucher.

**Luồng quản trị đơn hàng:** Admin đăng nhập → vào Dashboard (thống kê, đơn chờ xác nhận) → vào Quản lý đơn hàng → xem danh sách, tìm kiếm, lọc → cập nhật trạng thái đơn (xác nhận, đang giao, đã giao hoặc hủy). Các thay đổi trạng thái có thể kích hoạt gửi email hoặc cập nhật xếp hạng tùy nghiệp vụ.

**Luồng chat:** Khách hàng hoặc Admin mở trang chat → kết nối SignalR (ChatHub) → gửi tin nhắn (SendMessage) → ChatHub lưu ChatMessage vào cơ sở dữ liệu và gửi tới Clients.All (ReceiveMessage) để hiển thị realtime.

*Tùy yêu cầu báo cáo, bạn có thể bổ sung **Hình 2.3. Sơ đồ tuần tự (Sequence diagram)** cho một trong các luồng trên (ví dụ luồng đặt hàng hoặc luồng thanh toán PayOS).*

---

*Các mẫu thiết kế được áp dụng cụ thể trong từng thành phần của hệ thống sẽ được trình bày tại Chương 4.*
