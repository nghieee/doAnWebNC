using System.ComponentModel.DataAnnotations;

namespace web_ban_thuoc.ViewModels;

public class CheckoutPopupViewModel
{
    [Required]
    public string FullName { get; set; }
    [Required]
    public string Phone { get; set; }
    [Required]
    public string ShippingAddress { get; set; }
} 