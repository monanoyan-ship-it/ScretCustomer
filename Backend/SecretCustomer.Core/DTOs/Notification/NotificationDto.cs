namespace SecretCustomer.Core.DTOs.Notification;

/// <summary>
/// Bildirim DTO
/// </summary>
public class NotificationDto
{
    public int Id { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int RecipientUserId { get; set; }
    public string? RecipientUserName { get; set; }
    public int? SenderUserId { get; set; }
    public string? SenderUserName { get; set; }
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Bildirim listesi DTO
/// </summary>
public class NotificationListDto
{
    public int Id { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo => GetTimeAgo(CreatedAt);

    private static string GetTimeAgo(DateTime dateTime)
    {
        var diff = DateTime.UtcNow - dateTime;
        if (diff.TotalMinutes < 1) return "Az önce";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dakika önce";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} saat önce";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} gün önce";
        return dateTime.ToString("dd.MM.yyyy");
    }
}

/// <summary>
/// Bildirim oluşturma DTO
/// </summary>
public class CreateNotificationDto
{
    public string NotificationType { get; set; } = "Info";
    public string Channel { get; set; } = "InApp";
    public string Priority { get; set; } = "Normal";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int RecipientUserId { get; set; }
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? GroupId { get; set; }
}

/// <summary>
/// Toplu bildirim oluşturma DTO
/// </summary>
public class CreateBulkNotificationDto
{
    public string NotificationType { get; set; } = "Info";
    public string Channel { get; set; } = "InApp";
    public string Priority { get; set; } = "Normal";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<int> RecipientUserIds { get; set; } = new();
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

/// <summary>
/// Bildirim filtre DTO
/// </summary>
public class NotificationFilterDto
{
    public string? NotificationType { get; set; }
    public string? Channel { get; set; }
    public string? Priority { get; set; }
    public bool? IsRead { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Bildirim özeti DTO
/// </summary>
public class NotificationSummaryDto
{
    public int TotalNotifications { get; set; }
    public int UnreadCount { get; set; }
    public int TodayCount { get; set; }
    public List<NotificationListDto> RecentNotifications { get; set; } = new();
}

/// <summary>
/// Bildirim ayarları DTO
/// </summary>
public class NotificationSettingDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool PushEnabled { get; set; }
}

/// <summary>
/// Bildirim ayarları güncelleme DTO
/// </summary>
public class UpdateNotificationSettingDto
{
    public string NotificationType { get; set; } = string.Empty;
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool PushEnabled { get; set; }
}
