using System;
using System.Collections.Generic;

namespace web_ban_thuoc.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    // Đổi UserId sang string? để liên kết với IdentityUser
    public string? UserId { get; set; }

    public int? ProductId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? ReviewDate { get; set; }

    public virtual Product? Product { get; set; }

    // Đổi navigation property sang IdentityUser
    public virtual Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }
}
