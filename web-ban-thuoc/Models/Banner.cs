using System.ComponentModel.DataAnnotations;

namespace web_ban_thuoc.Models;

public class Banner
{
    public int BannerId { get; set; }
    
    [Required(ErrorMessage = "Tên banner là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên banner không được quá 100 ký tự")]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(200, ErrorMessage = "Mô tả không được quá 200 ký tự")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
    
    [Display(Name = "Đường dẫn ảnh")]
    public string ImageUrl { get; set; } = string.Empty;
    
    [StringLength(200, ErrorMessage = "Link không được quá 200 ký tự")]
    [Url(ErrorMessage = "Link không đúng định dạng")]
    [Display(Name = "Link")]
    public string? LinkUrl { get; set; }

    [Required(ErrorMessage = "Loại banner là bắt buộc")]
    [Display(Name = "Loại Banner")]
    public string BannerType { get; set; } = string.Empty;

    [Range(0, 999, ErrorMessage = "Thứ tự phải từ 0 đến 999")]
    [Display(Name = "Thứ tự hiển thị")]
    public int SortOrder { get; set; } = 0;
    
    [Display(Name = "Hoạt động")]
    public bool IsActive { get; set; } = true;
    
    [Display(Name = "Ngày tạo")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Display(Name = "Ngày cập nhật")]
    public DateTime? UpdatedAt { get; set; }
}