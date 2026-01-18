using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.DTOs.SupportRequest;

/// <summary>
/// Destek talebi listesi DTO
/// </summary>
public class SupportRequestListDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PageUrl { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusIcon { get; set; } = string.Empty;
    public string StatusCssClass { get; set; } = string.Empty;
    public bool HasResponse { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    // Gönderen bilgisi
    public string SenderName { get; set; } = string.Empty;
    public string SenderType { get; set; } = string.Empty; // "User" veya "CustomerPersonnel"
    public int? UserId { get; set; }
    public int? CustomerPersonnelId { get; set; }
}

/// <summary>
/// Destek talebi detay DTO
/// </summary>
public class SupportRequestDetailDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PageUrl { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusIcon { get; set; } = string.Empty;
    public string StatusCssClass { get; set; } = string.Empty;
    public string? AdminResponse { get; set; }
    public DateTime? RespondedAt { get; set; }
    public bool IsReadByUser { get; set; }
    public DateTime CreatedAt { get; set; }

    // Gönderen bilgisi
    public string SenderName { get; set; } = string.Empty;
    public string? SenderEmail { get; set; }
    public string SenderType { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public int? CustomerPersonnelId { get; set; }

    // Cevaplayan bilgisi
    public string? RespondedByUserName { get; set; }
}

/// <summary>
/// Destek talebi oluşturma DTO (Kullanıcılar için)
/// </summary>
public class CreateSupportRequestDto
{
    public string Message { get; set; } = string.Empty;
    public string? PageUrl { get; set; }
}

/// <summary>
/// Destek talebi cevaplama DTO (Admin için)
/// </summary>
public class RespondSupportRequestDto
{
    public int Id { get; set; }
    public string AdminResponse { get; set; } = string.Empty;
}

/// <summary>
/// Destek talebi durumu güncelleme DTO (Admin için)
/// </summary>
public class UpdateSupportRequestStatusDto
{
    public int Id { get; set; }
    public int StatusId { get; set; }
}

/// <summary>
/// Destek talebi filtre DTO
/// </summary>
public class SupportRequestFilterDto
{
    public int? StatusId { get; set; }
    public string? SenderType { get; set; }
    public string? SearchTerm { get; set; }
    public List<DateRangeFilter>? DateRanges { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Kullanıcının kendi talep özeti DTO
/// </summary>
public class MySupportRequestSummaryDto
{
    public int TotalRequests { get; set; }
    public int PendingCount { get; set; }
    public int UnreadResponseCount { get; set; }
    public List<SupportRequestListDto> RecentRequests { get; set; } = new();
}
