using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.FieldWorker;

/// <summary>
/// FieldWorker Dashboard özeti
/// </summary>
public class FieldWorkerDashboardDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    // Atanmış projeler
    public List<AssignedProjectDto> AssignedProjects { get; set; } = new();

    // İstatistikler
    public int TotalVisits { get; set; }
    public int TodayVisits { get; set; }
    public int ThisWeekVisits { get; set; }
    // Son ziyaretler
    public List<VisitSummaryDto> RecentVisits { get; set; } = new();
}

/// <summary>
/// Atanmış proje bilgisi
/// </summary>
public class AssignedProjectDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int CustomerDealerCount { get; set; }
    public int CompletedVisits { get; set; }
    public int TotalAssignments { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Bu kullanıcının bu projedeki aktif ataması (ziyaret oluşturmak için gerekli)
    /// </summary>
    public int AssignmentId { get; set; }

    /// <summary>
    /// Atamadaki checklist ID (soruları yüklemek için)
    /// </summary>
    public int ChecklistId { get; set; }

    /// <summary>
    /// Checklist adı
    /// </summary>
    public string ChecklistName { get; set; } = string.Empty;
}

/// <summary>
/// Ziyaret özeti (liste için)
/// </summary>
public class VisitSummaryDto
{
    public int EvaluationId { get; set; }
    public string? VisitId { get; set; }
    public int? CustomerDealerId { get; set; }
    public string? CustomerDealerName { get; set; }
    public string? CustomerDealerCity { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal? ScorePercentage { get; set; }
    public int StatusId { get; set; }
    public string StatusName => GetStatusDisplayName(StatusId);

    private static string GetStatusDisplayName(int statusId) => statusId switch
    {
        4 => "Taslak",      // Draft
        3 => "Tamamlandı",  // Completed
        2 => "Devam Ediyor", // InProgress
        1 => "Beklemede",   // Pending
        5 => "İptal",       // Cancelled
        _ => "Bilinmiyor"
    };
    public DateTime CreatedAt { get; set; }
    public DateTime? ControlDate { get; set; }
    public string? PeriodName { get; set; }
}

/// <summary>
/// Ziyaret oluşturma DTO
/// </summary>
public class CreateVisitDto
{
    /// <summary>
    /// Atama ID (zorunlu)
    /// </summary>
    public int AssignmentId { get; set; }

    /// <summary>
    /// Mevcut değerlendirme ID (güncelleme için)
    /// </summary>
    public int? EvaluationId { get; set; }

    /// <summary>
    /// Ziyaret edilen bayi ID
    /// </summary>
    public int CustomerDealerId { get; set; }

    /// <summary>
    /// Ziyaret tarihi
    /// </summary>
    public DateTime? ControlDate { get; set; }

    /// <summary>
    /// Ziyaret saati
    /// </summary>
    public string? ControlTime { get; set; }

    /// <summary>
    /// Denetim yorumu
    /// </summary>
    public string? EvaluationComment { get; set; }

    /// <summary>
    /// Açıklamalar
    /// </summary>
    public List<string>? Descriptions { get; set; }

    /// <summary>
    /// Konum - Enlem
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Konum - Boylam
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Taslak olarak kaydet
    /// </summary>
    public bool IsDraft { get; set; }

    /// <summary>
    /// Cevaplar
    /// </summary>
    public List<VisitAnswerDto> Answers { get; set; } = new();
}

/// <summary>
/// Ziyaret cevap DTO
/// </summary>
public class VisitAnswerDto
{
    public int QuestionId { get; set; }
    public string? TextAnswer { get; set; }
    public decimal? NumericAnswer { get; set; }
    public decimal? GivenPoints { get; set; }
    public string? Notes { get; set; }
    public string? RecommendationNotes { get; set; }
    public bool ApplyPenalty { get; set; }
    public string? SelectedPenaltyType { get; set; }
    public List<int>? SelectedSubCriteriaIds { get; set; }
    public bool IsIncluded { get; set; } = true;
}

/// <summary>
/// FieldWorker için bayi listesi
/// </summary>
public class FieldWorkerDealerDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? ContactPerson { get; set; }
    public int DealerTypeId { get; set; }
    public string CustomerDealerTypeName => CustomerDealerTypes.GetById(DealerTypeId)?.NameResourceKey ?? "Bilinmiyor";

    // Proje bilgisi
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    // Ziyaret bilgisi
    public int VisitCount { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public decimal? LastVisitScore { get; set; }
}

/// <summary>
/// Ziyaret filtresi
/// </summary>
public class VisitFilterDto
{
    public int? ProjectId { get; set; }
    public int? CustomerDealerId { get; set; }
    public int? CustomerId { get; set; }
    public int? StatusId { get; set; }
    public List<DateRangeFilter>? DateRanges { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Sayfalı ziyaret sonucu
/// </summary>
public class PagedVisitResult
{
    public List<VisitSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Ziyaret bekleyen şube (Dashboard için)
/// </summary>
public class PendingDealerDto
{
    // Şube bilgileri
    public int DealerId { get; set; }
    public string DealerCode { get; set; } = string.Empty;
    public string DealerName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Phone { get; set; }
    public string? ContactPerson { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Atama bilgileri
    public int AssignmentId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int ChecklistId { get; set; }
    public string ChecklistName { get; set; } = string.Empty;

    // Proje tarihleri
    public DateTime? ProjectStartDate { get; set; }
    public DateTime? ProjectEndDate { get; set; }

    /// <summary>
    /// Google Maps URL
    /// </summary>
    public string? GoogleMapsUrl => Latitude.HasValue && Longitude.HasValue
        ? $"https://www.google.com/maps?q={Latitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},{Longitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
        : null;
}
