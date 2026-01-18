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
    public int PendingRequests { get; set; }

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
    public int DealerCount { get; set; }
    public int CompletedVisits { get; set; }
    public int TotalAssignments { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Ziyaret özeti (liste için)
/// </summary>
public class VisitSummaryDto
{
    public int EvaluationId { get; set; }
    public string? VisitId { get; set; }
    public int? DealerId { get; set; }
    public string? DealerName { get; set; }
    public string? DealerCity { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal? ScorePercentage { get; set; }
    public int StatusId { get; set; }
    public string StatusName => EvaluationStatuses.GetById(StatusId)?.NameResourceKey ?? "Bilinmiyor";
    public DateTime CreatedAt { get; set; }
    public DateTime? ControlDate { get; set; }
}

/// <summary>
/// Ziyaret oluşturma DTO
/// </summary>
public class CreateVisitDto
{
    public int ProjectId { get; set; }
    public int DealerId { get; set; }
    public int ChecklistId { get; set; }
    public DateTime? ControlDate { get; set; }
    public string? ControlTime { get; set; }
    public string? AuditorComment { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDraft { get; set; }
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
    public string? Notes { get; set; }
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
    public string DealerTypeName => DealerTypes.GetById(DealerTypeId)?.NameResourceKey ?? "Bilinmiyor";

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
    public int? DealerId { get; set; }
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
