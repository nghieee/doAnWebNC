# Mermaid - Quy trình Bán hàng trực tuyến

## 1) Flowchart đơn giản

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[1. Khách hàng đặt mua sản phẩm<br>• Truy cập website<br>• Tìm kiếm sản phẩm<br>• Thêm vào giỏ hàng<br>• Cung cấp thông tin giao hàng]
    B --> C{2. Kiểm tra tồn kho?}
    C -->|Đủ hàng| D[3. Lập Hóa đơn giao dịch<br>• Nhân viên xác nhận<br>• Tạo Hóa đơn<br>• Tạo Chi tiết hóa đơn]
    C -->|Không đủ| E[Thông báo<br>không đủ hàng]
    E --> F([Kết thúc])
    D --> G[4. Cập nhật tồn kho<br>• Trừ giảm số lượng tồn<br>• Đảm bảo tính nhất quán]
    G --> H[5. Giao hàng cho khách<br>• Chuyển đơn vị vận chuyển<br>• Khách nhận hàng]
    H --> I[6. Thanh toán & Hoàn tất<br>• Khách thanh toán<br>• Cập nhật "Đã thanh toán"]
    I --> F

    class A,F fill:#2E7D32,stroke:#1B5E20,stroke-width:2px,color:#fff
    class D,G,H,I fill:#E3F2FD,stroke:#1565C0,stroke-width:1px
    class E fill:#FFCCBC,stroke:#BF360C,stroke-width:1px
    class C fill:#FFF3E0,stroke:#E65100,stroke-width:2px
```

## 2) BPMN đơn giản

```mermaid
flowchart LR
    S([Bắt đầu]) --> E1[Khách hàng<br>đặt mua SP]
    E1 --> E2[Kiểm tra<br>tồn kho]
    E2 --> G{Đủ hàng?}
    G -->|Có| E3[Lập hóa đơn<br>giao dịch]
    G -->|Không| N[Thông báo<br>không đủ]
    E3 --> E4[Cập nhật<br>tồn kho]
    E4 --> E5[Giao hàng]
    E5 --> E6[Thanh toán<br>& Hoàn tất]
    E6 --> ZZ([Kết thúc])
    N --> ZZ

    class S,ZZ fill:#c62828,stroke:#b71c1c,color:#fff
    class G fill:#fff9c4,stroke:#f57f17
    class E1,E2,E3,E4,E5,E6 fill:#e3f2fd,stroke:#1565c0
    class N fill:#ffccbc,stroke:#d84315
```

## 3) Swimlane theo vai trò

```mermaid
flowchart TD
    subgraph KH [Khách hàng]
        A[Tìm kiếm sản phẩm]
        B[Thêm giỏ hàng]
        C[Đặt hàng]
        Z[Nhận hàng]
        AA[Thanh toán]
    end

    subgraph HE_THONG [Hệ thống / Oracle DB]
        D[Kiểm tra tồn kho]
        E[Tạo Hóa đơn]
        F[Cập nhật tồn kho]
    end

    subgraph NV [Nhân viên Kinh doanh]
        G[Xác nhận đơn hàng]
    end

    subgraph VC [Đơn vị vận chuyển]
        H[Giao hàng]
    end

    A --> B --> C --> D --> G --> E --> F --> H --> Z --> AA

    class KH fill:#e3f2fd,stroke:#1565c0
    class HE_THONG fill:#e8f5e9,stroke:#2e7d32
    class NV fill:#fff3e0,stroke:#ef6c00
    class VC fill:#f3e5f5,stroke:#6a1b9a
```
