using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_ban_thuoc.Models;

public class News
{
    [Key]
    public int NewsId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Slug { get; set; }

    [MaxLength(500)]
    public string? Summary { get; set; }

    public string? Content { get; set; }

    [MaxLength(300)]
    public string? ImageUrl { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public bool IsFeature { get; set; } = false;

    public bool IsPublished { get; set; } = true;

    public int ViewCount { get; set; } = 0;

    [MaxLength(100)]
    public string? Author { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    [MaxLength(50)]
    public string? Source { get; set; }
}
