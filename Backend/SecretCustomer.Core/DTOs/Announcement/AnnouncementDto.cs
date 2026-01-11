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
    public int TypeId { get; set; }
    public string TypeName => AnnouncementTypes.GetById(TypeId)?.SystemName ?? "";
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
    public int TypeId { get; set; } = AnnouncementTypes.Ids.Info;
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
    public int TypeId { get; set; }
    public string TypeClass => TypeId switch
    {
        1 => "warning",   // AnnouncementTypes.Ids.Warning
        2 => "success",   // AnnouncementTypes.Ids.Success
        3 => "danger",    // AnnouncementTypes.Ids.Important
        4 => "primary",   // AnnouncementTypes.Ids.News
        5 => "secondary", // AnnouncementTypes.Ids.System
        _ => "info"       // AnnouncementTypes.Ids.Info (0)
    };
    public bool IsPinned { get; set; }
    public DateTime PublishDate { get; set; }
}
