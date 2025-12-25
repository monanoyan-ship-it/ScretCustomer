using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.Announcement;

/// <summary>
/// Duyuru DTO
/// </summary>
public class AnnouncementDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public AnnouncementType Type { get; set; }
    public string TypeName => Type.ToString();
    public int Priority { get; set; }
    public DateTime PublishDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsPinned { get; set; }
    public string? TargetRoles { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Duyuru oluşturma/güncelleme DTO
/// </summary>
public class CreateAnnouncementDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public AnnouncementType Type { get; set; } = AnnouncementType.Info;
    public int Priority { get; set; } = 3;
    public DateTime? PublishDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPinned { get; set; } = false;
    public string? TargetRoles { get; set; }
}

/// <summary>
/// Dashboard için özet duyuru
/// </summary>
public class AnnouncementSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public AnnouncementType Type { get; set; }
    public string TypeClass => Type switch
    {
        AnnouncementType.Warning => "warning",
        AnnouncementType.Success => "success",
        AnnouncementType.Important => "danger",
        AnnouncementType.News => "primary",
        AnnouncementType.System => "secondary",
        _ => "info"
    };
    public bool IsPinned { get; set; }
    public DateTime PublishDate { get; set; }
}
