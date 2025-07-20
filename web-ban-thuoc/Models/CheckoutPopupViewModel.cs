using System.ComponentModel.DataAnnotations;

namespace web_ban_thuoc.Models
{
    public class CheckoutPopupViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        public string FullName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string Phone { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        public string ShippingAddress { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "COD";
    }
} 