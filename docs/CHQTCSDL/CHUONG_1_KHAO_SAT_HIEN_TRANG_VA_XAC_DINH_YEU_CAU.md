# CHƯƠNG 1
# KHẢO SÁT HIỆN TRẠNG VÀ XÁC ĐỊNH YÊU CẦU

---

## 1.1. KHẢO SÁT HIỆN TRẠNG

### 1.1.1. Giới thiệu doanh nghiệp và quy trình nghiệp vụ

#### 1.1.1.1. Giới thiệu về Nhà Thuốc Long Châu

Nhà Thuốc Long Châu là một trong những chuỗi bán lẻ dược phẩm lớn nhất Việt Nam. Nhằm đáp ứng xu hướng chuyển đổi số và phục vụ nhu cầu mua sắm tiện lợi, an toàn của khách hàng, hệ thống bán thuốc trực tuyến (E-commerce dược phẩm) được xây dựng. Hệ thống hoạt động theo mô hình **B2C (Business-to-Consumer)**, cho phép khách hàng cá nhân tìm kiếm, nhận tư vấn và mua các sản phẩm thuốc không kê đơn (OTC), thực phẩm chức năng và dược mỹ phẩm trực tiếp qua website.

Hệ thống ứng dụng công nghệ **ASP.NET Core 8.0 MVC** cho giao diện phía Client, kết nối và quản trị dữ liệu tập trung trên hệ quản trị cơ sở dữ liệu **Oracle Database 19c**. Việc sử dụng Oracle giúp hệ thống tối ưu hóa khả năng xử lý đồng thời cho lượng người dùng lớn, bảo mật phân quyền nghiêm ngặt và đảm bảo tính toàn vẹn của dữ liệu giao dịch.

**Ghi chú:** Phiên bản web hiện tại sử dụng Microsoft SQL Server với Entity Framework Core làm prototype để phát triển nhanh chức năng. Trong khuôn khổ đồ án môn **Các Hệ Quản trị Cơ Sở Dữ Liệu**, hệ thống sẽ được chuyển đổi sang **Oracle Database** với đầy đủ các đối tượng PL/SQL, đảm bảo các yêu cầu về stored procedure, trigger, transaction, phân quyền và sao lưu phục hồi.

#### 1.1.1.2. Lĩnh vực hoạt động

Hệ thống tập trung vào lĩnh vực: **Bán lẻ dược phẩm và sản phẩm chăm sóc sức khỏe trực tuyến.**

#### 1.1.1.3. Mô tả quy trình nghiệp vụ tin học hóa

Để đảm bảo tính tập trung và khả năng xử lý dữ liệu lớn, hệ thống chỉ tập trung tin học hóa **01 quy trình cốt lõi** là: **Quy trình Quản lý Bán hàng trực tuyến (Online Sales Management)**. Quy trình này diễn ra gồm các bước cụ thể như sau:

**Bước 1: Khách hàng đặt mua sản phẩm**
- Khách hàng truy cập website, tìm kiếm sản phẩm, thêm vào giỏ hàng và tiến hành đặt hàng bằng cách cung cấp thông tin giao hàng.

**Bước 2: Hệ thống tiếp nhận và kiểm tra tồn kho**
- Hệ thống tự động kiểm tra số lượng tồn kho của sản phẩm trong cơ sở dữ liệu Oracle.
- Nếu đủ hàng, đơn hàng được chuyển sang trạng thái "Chờ xử lý".
- Nếu không đủ hàng, hệ thống sẽ thông báo cho khách hàng.

**Bước 3: Lập hóa đơn giao dịch**
- Nhân viên kinh doanh xác nhận đơn hàng.
- Hệ thống tự động khởi tạo 01 Hóa đơn bán hàng (ghi nhận mã hóa đơn, ngày lập, thông tin khách hàng) và các Chi tiết hóa đơn tương ứng (mã sản phẩm, số lượng, đơn giá, thành tiền).

**Bước 4: Cập nhật tồn kho**
- Ngay khi hóa đơn được xác nhận, hệ thống kích hoạt cơ chế trừ giảm số lượng tồn kho của sản phẩm tương ứng để đảm bảo tính nhất quán dữ liệu.

**Bước 5: Giao hàng và hoàn tất**
- Đơn hàng được chuyển cho đơn vị vận chuyển.
- Sau khi khách hàng nhận hàng và thanh toán thành công, hóa đơn được cập nhật trạng thái "Đã thanh toán", hoàn tất một giao dịch.

**Sơ đồ 1.1: Quy trình Quản lý Bán hàng trực tuyến**

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Khách hàng   │     │   Kiểm tra     │     │   Lập hóa đơn  │
│   đặt mua SP   │────►│   tồn kho      │────►│   giao dịch    │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                         │
                                                         ▼
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Hoàn tất      │     │   Giao hàng     │     │ Cập nhật tồn  │
│   giao dịch     │◄────│   cho khách     │◄────│      kho       │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

---

### 1.1.2. Cơ cấu tổ chức liên quan

#### 1.1.2.1. Sơ đồ tổ chức

Hệ thống Nhà Thuốc Long Châu có cơ cấu tổ chức gồm 3 bộ phận chính tham gia xử lý dữ liệu:

**Sơ đồ 1.3: Cơ cấu tổ chức Nhà Thuốc Long Châu**

```
                         ┌─────────────────┐
                         │    Ban Giám Đốc    │
                         │     (Admin)        │
                         └────────┬──────────┘
                                  │
           ┌──────────────────────┼──────────────────────┐
           │                      │                      │
           ▼                      ▼                      ▼
┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐
│   Bộ phận Kinh     │  │   Bộ phận Kho      │  │   Bộ phận CSKH     │
│      doanh         │  │      hàng          │  │                     │
│  (Quản lý đơn    │  │  (Quản lý nhập/   │  │  (Hỗ trợ khách    │
│   hàng, sản phẩm,│  │   xuất kho, đặt   │  │   hàng, chat,    │
│   voucher, banner)│  │   hàng NCC)       │  │   đánh giá)      │
└─────────────────────┘  └─────────────────────┘  └─────────────────────┘
```

#### 1.1.2.2. Vai trò và quyền hạn

| Bộ phận | Vai trò (Role) | Quyền truy cập | Người dùng |
|---------|----------------|----------------|------------|
| Ban Giám Đốc | **Admin** | Toàn quyền quản trị hệ thống | admin@gmail.com |
| Kho hàng | **WarehouseStaff** | Quản lý kho, nhập/xuất, đặt hàng NCC, xem báo cáo kho | warehouse@longchau.local |
| CSKH | **CustomerSupport** | Chat hỗ trợ khách, quản lý đơn hàng | support@longchau.local |

#### 1.1.2.3. Các bộ phận tham gia xử lý dữ liệu

**1. Bộ phận Kinh doanh (Admin):**
- Quản lý danh mục sản phẩm (CRUD sản phẩm, danh mục 3 cấp).
- Quản lý đơn hàng (xác nhận, chuyển trạng thái, hủy đơn).
- Quản lý voucher và chiến dịch khuyến mãi.
- Quản lý banner quảng cáo trang chủ.
- Quản lý người dùng và nhân viên.
- Xem báo cáo KPI tài chính và tồn kho.

**2. Bộ phận Kho hàng (WarehouseStaff):**
- Quản lý kho hàng (xem, tạo, sửa kho).
- Nhập hàng từ nhà cung cấp (tạo PurchaseOrder, GoodsReceipt).
- Xuất kho thủ công (tạo StockAdjustment).
- Theo dõi tồn kho và cảnh báo hết hàng, sắp hết hạn.
- Quản lý nhà cung cấp.

**3. Bộ phận CSKH (CustomerSupport):**
- Chat trực tuyến với khách hàng qua SignalR.
- Xem và quản lý đơn hàng của khách hàng.
- Hỗ trợ khách hàng về thông tin sản phẩm, đơn hàng.

---

### 1.1.3. Đánh giá hệ thống hiện tại

#### 1.1.3.1. Cách quản lý dữ liệu hiện nay

Trước khi xây dựng hệ thống Nhà Thuốc Long Châu, việc quản lý dữ liệu được thực hiện theo phương pháp thủ công:

- **Excel và sổ sách**: Các thông tin sản phẩm, đơn hàng, tồn kho được ghi chép trên bảng tính Excel hoặc sổ sách giấy.
- **Không có hệ thống tập trung**: Dữ liệu phân tán ở nhiều bộ phận, nhiều file Excel khác nhau.
- **Không có cơ chế phân quyền**: Mọi nhân viên đều có thể truy cập và chỉnh sửa mọi dữ liệu.
- **Thanh toán thủ công**: Chỉ hỗ trợ thanh toán COD, chưa tích hợp thanh toán trực tuyến.

#### 1.1.3.2. Những khó khăn còn tồn tại

| Khó khăn | Mô tả |
|----------|-------|
| **Dữ liệu phân tán** | Thông tin sản phẩm, đơn hàng, tồn kho nằm ở nhiều file Excel khác nhau, không có cơ sở dữ liệu tập trung. |
| **Nhập liệu trùng lặp** | Cùng một thông tin sản phẩm phải nhập nhiều lần ở các bảng khác nhau (danh mục, tồn kho, đơn hàng). |
| **Khó tra cứu** | Khi cần tìm thông tin một sản phẩm hoặc đơn hàng, phải mở nhiều file Excel và tìm kiếm thủ công. |
| **Chưa hỗ trợ nhiều người dùng** | Nhiều nhân viên không thể làm việc đồng thời trên cùng một file Excel. |
| **Khó đảm bảo tính toàn vẹn dữ liệu** | Không có ràng buộc kiểm tra dữ liệu, dễ xảy ra lỗi nhập liệu (số âm, null, trùng lặp). |
| **Khó phân quyền** | Không thể giới hạn quyền truy cập theo vai trò (Admin, Kho, CSKH). |
| **Khó mở rộng** | Khi lượng dữ liệu tăng lên, Excel không đáp ứng được về hiệu suất và dung lượng. |
| **Không theo dõi được lịch sử** | Khi sửa hoặc xóa dữ liệu, không có bản ghi lưu lại ai đã thay đổi và thay đổi gì. |
| **Thiếu báo cáo tự động** | Phải tự tổng hợp dữ liệu từ nhiều nguồn để tạo báo cáo doanh thu, tồn kho. |
| **Quản lý tồn kho thủ công** | Không theo dõi được tồn kho theo từng lô hàng và hạn sử dụng, không áp dụng được FEFO. |

#### 1.1.3.3. Lý do lựa chọn Oracle để xây dựng hệ thống mới

Dựa trên các yêu cầu nghiệp vụ của Nhà Thuốc Long Châu, việc lựa chọn **Oracle Database** để xây dựng hệ thống mới được đề xuất vì những lý do sau:

| Lý do | Giải thích |
|-------|------------|
| **Dữ liệu giao dịch lớn** | Hệ thống cần lưu trữ hàng trăm nghìn bản ghi đơn hàng, tồn kho, giao dịch. Oracle có khả năng xử lý dữ liệu lớn với hiệu suất cao. |
| **Nhiều người dùng đồng thời** | Hệ thống phục vụ nhiều nhân viên (Admin, Kho, CSKH) và khách hàng truy cập đồng thời. Oracle hỗ trợ tốt concurrency với various isolation levels. |
| **Cần phân quyền** | Oracle cung cấp hệ thống phân quyền mạnh mẽ với User, Role, Privilege, Profile cho từng bộ phận. |
| **Cần sao lưu và phục hồi** | Dữ liệu dược phẩm cần được bảo toàn. Oracle cung cấp cơ chế Backup, Restore, Recovery, Redo Log, Archive Log chuyên nghiệp. |
| **Cần Transaction và đảm bảo ACID** | Các nghiệp vụ như xuất kho FEFO, tạo đơn hàng cần đảm bảo tính nhất quán dữ liệu. Oracle đảm bảo ACID properties. |
| **Cần xử lý đồng thời** | Nhiều giao dịch có thể xảy ra đồng thời (nhiều đơn hàng, nhiều phiếu nhập/xuất). Oracle xử lý tốt các vấn đề concurrency như Lost Update, Dirty Read. |
| **Nghiệp vụ phức tạp** | Hệ thống cần PL/SQL để hiện thực các stored procedure, trigger cho nghiệp vụ phức tạp như FEFO, tính giá vốn, tích điểm loyalty. |
| **Tính toàn vẹn dữ liệu** | Oracle cung cấp các ràng buộc toàn vẹn (constraints, triggers) để đảm bảo dữ liệu hợp lệ (số lượng không âm, mã sản phẩm duy nhất, ...). |

---

## 1.2. XÁC ĐỊNH YÊU CẦU NGHIỆP VỤ (BUSINESS REQUIREMENTS)

Sau khi khảo sát hiện trạng, các yêu cầu nghiệp vụ (Business Requirements) của hệ thống Nhà Thuốc Long Châu được xác định như sau:

---

### 1.2.1. Danh sách Business Requirement

**Bảng 1.1: Danh sách Business Requirements**

| BR ID | Business Requirement | Phân hệ | Độ ưu tiên |
|-------|---------------------|---------|------------|
| BR01 | Hệ thống phải cho phép khách hàng đăng ký, đăng nhập và quản lý tài khoản cá nhân. | Tài khoản | Cao |
| BR02 | Hệ thống phải cho phép khách hàng duyệt, tìm kiếm và xem chi tiết sản phẩm theo danh mục 3 cấp. | Sản phẩm | Cao |
| BR03 | Hệ thống phải quản lý thông tin sản phẩm bao gồm giá, tồn kho, thành phần, công dụng, hạn sử dụng. | Sản phẩm | Cao |
| BR04 | Hệ thống phải cho phép khách hàng tạo và quản lý giỏ hàng (thêm, sửa, xóa sản phẩm). | Giỏ hàng | Cao |
| BR05 | Hệ thống phải cho phép khách hàng thanh toán qua PayOS (QR ngân hàng) hoặc COD. | Thanh toán | Cao |
| BR06 | Hệ thống phải quản lý đơn hàng và trạng thái đơn hàng (Chờ xác nhận → Đã xác nhận → Đang giao → Đã giao). | Đơn hàng | Cao |
| BR07 | Hệ thống phải tự động xuất kho theo nguyên tắc FEFO khi đơn hàng được xác nhận. | Kho hàng | Cao |
| BR08 | Hệ thống phải quản lý tồn kho theo từng kho, từng lô hàng và hạn sử dụng. | Kho hàng | Cao |
| BR09 | Hệ thống phải cho phép tạo phiếu điều chỉnh tồn kho (nhập/xuất thủ công) với quy trình duyệt. | Kho hàng | Cao |
| BR10 | Hệ thống phải quản lý đơn mua hàng từ nhà cung cấp và phiếu nhập kho. | Mua hàng | Trung bình |
| BR11 | Hệ thống phải cảnh báo khi tồn kho thấp hoặc lô hàng sắp hết hạn. | Kho hàng | Cao |
| BR12 | Hệ thống phải quản lý chương trình khách hàng thân thiết (tích điểm, xếp hạng, đổi quà). | Loyalty | Trung bình |
| BR13 | Hệ thống phải quản lý voucher khuyến mãi (giảm cố định hoặc %). | Marketing | Trung bình |
| BR14 | Hệ thống phải cung cấp báo cáo doanh thu, lợi nhuận, tồn kho cho Admin. | Báo cáo | Cao |
| BR15 | Hệ thống phải ghi log hoạt động của người dùng (audit trail). | Quản trị | Trung bình |
| BR16 | Hệ thống phải phân quyền truy cập theo vai trò (Admin, WarehouseStaff, CustomerSupport). | Quản trị | Cao |
| BR17 | Hệ thống phải hỗ trợ chat trực tuyến giữa khách hàng và CSKH. | CSKH | Trung bình |

---

### 1.2.2. Phạm vi của hệ thống

**Bảng 1.2: Phạm vi hệ thống**

| Trong phạm vi | Ngoài phạm vi |
|---------------|---------------|
| Quản lý sản phẩm và danh mục | Quản lý nhân sự (tuyển dụng, chấm công) |
| Quản lý đơn hàng và thanh toán | Kế toán tổng hợp, báo cáo thuế |
| Quản lý kho hàng đa kho với FEFO | Quản lý tài chính (công nợ, thu chi) |
| Quản lý mua hàng nhà cung cấp | Quản lý tài sản cố định |
| Chương trình khách hàng thân thiết | Quản lý marketing (email, SMS) |
| Voucher và khuyến mãi | Thanh toán trực tuyến qua ví điện tử khác (VnPay, MoMo) |
| Báo cáo KPI (doanh thu, tồn kho) | Tích hợp API bên thứ ba (GHN, GHTK) |
| Chat hỗ trợ khách hàng | Ứng dụng di động |
| Phân quyền người dùng theo vai trò | Quản lý chuỗi cửa hàng |

---

### 1.2.3. Dữ liệu cần quản lý

#### 1.2.3.1. Danh mục (Master Data)

**Bảng 1.3: Dữ liệu danh mục**

| Danh mục | Mô tả | Các thuộc tính chính |
|----------|-------|---------------------|
| **Danh mục sản phẩm** | Phân loại sản phẩm theo 3 cấp (Level 1, 2, 3) | CategoryId, CategoryName, ParentCategoryId, Description, ImageUrl, IsFeature |
| **Sản phẩm** | Thông tin thuốc và dược phẩm | ProductId, ProductName, Sku, Barcode, Price, CostPrice, StockQuantity, CategoryId, SupplierId, Ingredients, Uses, Dosage, RegistrationNumber, RequiresPrescription, MinStockLevel, IsActive |
| **Nhà cung cấp** | Danh sách nhà cung cấp | SupplierId, Code, Name, Phone, Email, Address, TaxCode, IsActive |
| **Kho hàng** | Các kho hàng trong hệ thống | WarehouseId, Name, Address, IsDefault, IsActive |
| **Lô hàng** | Thông tin lô hàng theo FEFO | ProductBatchId, BatchNo, ExpiryDate, QuantityOnHand, UnitCost, ProductId, WarehouseId, SupplierId |
| **Người dùng** | Tài khoản khách hàng và nhân viên | UserId, Email, Password, FullName, Phone, Role |
| **Banner** | Hình ảnh quảng cáo trang chủ | BannerId, Title, ImageUrl, LinkUrl, BannerType, SortOrder, IsActive |
| **Voucher** | Mã giảm giá khuyến mãi | VoucherId, Code, DiscountAmount, PercentValue, ExpiryDate, MaxUsage, UsedCount, IsPublic, IsActive |
| **Quà đổi điểm** | Quà tặng trong chương trình loyalty | LoyaltyRewardId, Title, PointsCost, RewardType, DiscountAmount, PercentValue, IsActive |

#### 1.2.3.2. Dữ liệu giao dịch (Transactional Data)

**Bảng 1.4: Dữ liệu giao dịch**

| Giao dịch | Mô tả | Các thuộc tính chính |
|-----------|-------|----------------------|
| **Giỏ hàng** | Giỏ hàng của khách hàng | CartId, UserId, VoucherCode, VoucherDiscount, UpdatedAt |
| **Chi tiết giỏ hàng** | Sản phẩm trong giỏ | CartItemId, CartId, ProductId, Quantity, UnitPrice |
| **Đơn hàng** | Thông tin đơn hàng khách | OrderId, UserId, OrderDate, TotalAmount, Status, ShippingAddress, PaymentStatus, VoucherCode, VoucherDiscount |
| **Chi tiết đơn hàng** | Sản phẩm trong đơn | OrderItemId, OrderId, ProductId, Quantity, Price |
| **Thanh toán** | Thông tin thanh toán | PaymentId, OrderId, PaymentMethod, Amount, PaymentDate, PaymentStatus, TransactionId |
| **Vận đơn** | Thông tin giao hàng | ShipmentId, OrderId, Carrier, TrackingCode, ShippingFee, ShippedAt |
| **Lịch sử trạng thái** | Theo dõi thay đổi đơn hàng | OrderStatusHistoryId, OrderId, FromStatus, ToStatus, ChangedByUserId, ChangedAt |
| **Đơn mua hàng NCC** | Đơn đặt hàng với nhà cung cấp | PurchaseOrderId, OrderCode, SupplierId, WarehouseId, Status, OrderDate, ExpectedDate |
| **Chi tiết đơn mua** | Sản phẩm trong đơn mua | PurchaseOrderLineId, PurchaseOrderId, ProductId, QuantityOrdered, QuantityReceived, UnitCost |
| **Phiếu nhập kho** | Phiếu nhập hàng từ NCC | GoodsReceiptId, ReceiptCode, PurchaseOrderId, SupplierId, WarehouseId, ReceiptDate |
| **Chi tiết phiếu nhập** | Sản phẩm nhập kho | GoodsReceiptLineId, GoodsReceiptId, ProductId, BatchNo, ExpiryDate, Quantity, UnitCost |
| **Phiếu điều chỉnh** | Phiếu nhập/xuất kho thủ công | StockAdjustmentId, AdjustmentCode, AdjustmentType, Reason, Status, WarehouseId, RequestedBy, ApprovedBy |
| **Chi tiết điều chỉnh** | Sản phẩm trong phiếu điều chỉnh | StockAdjustmentDetailId, StockAdjustmentId, ProductId, ProductBatchId, Quantity |
| **Giao dịch tồn kho** | Lịch sử biến động tồn kho | TransactionId, ProductId, WarehouseId, TransactionType, Quantity, QuantityBefore, QuantityAfter, OrderId, SupplierId |
| **Đánh giá sản phẩm** | Đánh giá của khách hàng | ReviewId, UserId, ProductId, Rating, Comment, ReviewDate |
| **Giao dịch điểm** | Lịch sử tích/đổi điểm loyalty | LoyaltyPointTransactionId, UserId, Points, TransactionType, OrderId, Description |
| **Voucher đã dùng** | Lịch sử sử dụng voucher | VoucherRedemptionId, VoucherId, UserId, OrderId, DiscountAmount, RedeemedAt |
| **Tin nhắn chat** | Tin nhắn chat giữa KH và CSKH | ChatMessageId, SenderId, ReceiverId, Message, SentAt, IsRead |
| **Nhật ký hoạt động** | Log hoạt động hệ thống | DbActivityLogId, UserId, Action, EntityName, EntityId, Details, Timestamp |

#### 1.2.3.3. Báo cáo cần thiết

**Bảng 1.5: Các báo cáo cần thiết**

| Báo cáo | Nội dung | Nguồn dữ liệu |
|---------|----------|---------------|
| Doanh thu theo kỳ | Tổng doanh thu, số đơn, đơn trung bình | Orders (Status = "Đã giao") |
| Lợi nhuận gộp | Doanh thu - Giá vốn (COGS) | Orders + InventoryTransaction (BatchSale) |
| Tồn kho hiện tại | Giá trị tồn kho theo kho, theo sản phẩm | ProductBatch × UnitCost |
| Cảnh báo hết hàng | Sản phẩm có StockQuantity <= MinStockLevel | Products |
| Cảnh báo hết hạn | Lô hàng sắp hết hạn (≤ 30, 90, 180 ngày) | ProductBatch |
| Sản phẩm bán chạy | Top sản phẩm theo số lượng, doanh thu | Orders + OrderItems |
| Top khách hàng | Khách hàng chi tiêu nhiều nhất | Orders |
| Đơn hàng theo trạng thái | Số đơn theo từng trạng thái | Orders |
| Công nợ NCC | Số tiền còn nợ nhà cung cấp | PurchaseOrders + GoodsReceipts |
| Voucher usage | Tỷ lệ sử dụng voucher | VoucherRedemptions |

---

### 1.2.4. Các yêu cầu kỹ thuật

#### 1.2.4.1. Yêu cầu về cơ sở dữ liệu

| Yêu cầu | Mô tả |
|----------|-------|
| **Dữ liệu giao dịch lớn** | Hệ thống cần lưu trữ tối thiểu 100.000 bản ghi đơn hàng và giao dịch tồn kho, đảm bảo hiệu suất truy vấn nhanh. |
| **Nhiều người dùng đồng thời** | Hỗ trợ tối thiểu 50 người dùng đồng thời (nhân viên + khách hàng truy cập). |
| **Phân quyền** | User, Role, Privilege, Profile để kiểm soát truy cập theo vai trò. |
| **Backup/Restore** | Có chiến lược sao lưu định kỳ và khả năng phục hồi dữ liệu khi cần. |
| **Transaction** | Đảm bảo ACID properties cho các giao dịch nghiệp vụ quan trọng. |
| **Toàn vẹn dữ liệu** | Ràng buộc toàn vẹn bằng constraints và triggers. |
| **Xử lý đồng thời** | Isolation levels phù hợp để ngăn ngừa Lost Update, Dirty Read. |

#### 1.2.4.2. Yêu cầu về nghiệp vụ

| Yêu cầu | Mô tả |
|----------|-------|
| **CRUD Operations** | Thêm, sửa, xóa, tra cứu cho tất cả các bảng danh mục và giao dịch. |
| **Stored Procedure** | Các procedure cho nghiệp vụ phức tạp: tạo đơn hàng, xuất kho FEFO, tích điểm loyalty. |
| **Trigger** | Trigger kiểm tra ràng buộc: không cho sửa đơn hàng đã giao, cập nhật tồn kho tự động. |
| **Báo cáo** | Procedure thống kê cho các báo cáo KPI. |
| **PL/SQL** | Sử dụng PL/SQL blocks, functions, procedures, triggers cho logic nghiệp vụ. |

#### 1.2.4.3. Yêu cầu về bảo mật

| Yêu cầu | Mô tả |
|----------|-------|
| **Mã hóa password** | Password được hash trước khi lưu (sử dụng Oracle's DBMS_CRYPTO hoặc application-level hashing). |
| **Phân quyền tối thiểu** | Mỗi user chỉ được granted các quyền cần thiết cho vai trò của mình. |
| **Audit trail** | Ghi nhận mọi thao tác thay đổi dữ liệu quan trọng. |
| **Profile** | Giới hạn tài nguyên sử dụng (CPU, memory, session) cho từng loại user. |

---

## 1.3. MA TRẬN TRUY VẾT YÊU CẦU NGHIỆP VỤ

**Bảng 1.6: Ma trận truy vết Business Requirements**

| BR ID | Business Requirement | Phân hệ | ERD | Chức năng | Stored Procedure | Trigger |
|-------|---------------------|---------|-----|------------|------------------|---------|
| BR01 | Đăng ký, đăng nhập | Tài khoản | AspNetUsers, AspNetRoles | Quản lý user | - | - |
| BR02 | Duyệt sản phẩm | Sản phẩm | Category, Product | Tra cứu SP | - | - |
| BR03 | Quản lý sản phẩm | Sản phẩm | Product, Category, Supplier | CRUD sản phẩm | - | - |
| BR04 | Quản lý giỏ hàng | Giỏ hàng | Cart, CartItem | CRUD giỏ hàng | - | - |
| BR05 | Thanh toán | Thanh toán | Payment, Order | Tạo thanh toán | PROC_CREATE_ORDER | TRG_UPDATE_STOCK |
| BR06 | Quản lý đơn hàng | Đơn hàng | Order, OrderItem, OrderStatusHistory | CRUD đơn hàng | - | TRG_PREVENT_UPDATE |
| BR07 | Xuất kho FEFO | Kho hàng | ProductBatch, InventoryTransaction | Xuất kho | PROC_EXPORT_FEFO | - |
| BR08 | Quản lý tồn kho | Kho hàng | WarehouseStock, ProductBatch | Tra cứu tồn kho | - | - |
| BR09 | Phiếu điều chỉnh | Kho hàng | StockAdjustment, StockAdjustmentDetail | CRUD phiếu | PROC_CREATE_ADJUSTMENT | - |
| BR10 | Đơn mua NCC | Mua hàng | PurchaseOrder, GoodsReceipt | CRUD PO, GR | - | - |
| BR11 | Cảnh báo tồn kho | Kho hàng | Product, ProductBatch | Báo cáo | PROC_STOCK_ALERT | - |
| BR12 | Loyalty program | Loyalty | UserRankInfo, LoyaltyPointTransaction | Tích/đổi điểm | PROC_EARN_POINTS | TRG_UPDATE_RANK |
| BR13 | Quản lý voucher | Marketing | Voucher, UserVoucher, VoucherRedemption | CRUD voucher | - | - |
| BR14 | Báo cáo KPI | Báo cáo | Orders, InventoryTransaction | Báo cáo | PROC_REPORT_* | - |
| BR15 | Audit trail | Quản trị | DbActivityLog | Ghi log | - | TRG_AUDIT_* |
| BR16 | Phân quyền | Quản trị | AspNetRoles, AspNetUserRoles | Gán role | - | - |
| BR17 | Chat CSKH | CSKH | ChatMessage | Chat real-time | - | - |

---

## TÀI LIỆU THAM KHẢO

[1] Nguyễn Thanh Trung, "Hướng dẫn thực hiện đề tài môn Các Hệ quản trị cơ sở dữ liệu," Khoa CNTT, HUFLIT, 2026.

[2] Oracle Corporation, "Database PL/SQL Language Reference," Oracle Database Documentation, 2024.

[3] Oracle Corporation, "Database Administrator's Guide," Oracle Database Documentation, 2024.
