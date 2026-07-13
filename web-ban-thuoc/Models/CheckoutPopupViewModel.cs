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
    
    [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành")]
    public int ProvinceId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn quận/huyện")]
    public int DistrictId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phường/xã")]
    public string WardCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số nhà/tên đường")]
    public string HouseNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "COD";

        public string? PrescriptionNote { get; set; }
        public int? ServiceId { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal InsuranceValue { get; set; }
        public int Weight { get; set; }
        public int Length { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string? Note { get; set; }
    }
}
