using System.ComponentModel.DataAnnotations;

namespace web_ban_thuoc.Models;

public class ProfileViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool RequireVerification { get; set; } = false;
    public string VerificationType { get; set; } = string.Empty;
    
    // Các thuộc tính cho chức năng thay đổi thông tin
    public string? NewEmail { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }
    public string? VerificationCode { get; set; }
    public string? CurrentPassword { get; set; }
    
    // Thêm thuộc tính cho lịch sử đơn hàng
    public List<OrderHistoryViewModel> Orders { get; set; } = new List<OrderHistoryViewModel>();
}

// ViewModel cho lịch sử đơn hàng
public class OrderHistoryViewModel
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
}

// ViewModel cho từng sản phẩm trong đơn hàng
public class OrderItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice => Price * Quantity;
} 