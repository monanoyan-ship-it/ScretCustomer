using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Customer;

/// <summary>
/// Liste görünümü için hafif DTO - Include kullanmadan projection ile çekilir
/// </summary>
public class CustomerListDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Aggregate counts (COUNT subqueries ile hesaplanır)
    public int PersonnelCount { get; set; }
    public int OrganizationCount { get; set; }
    public int BranchCount { get; set; }
    public int ProjectCount { get; set; }

    // Hedefler ve Kotalar
    public int? TargetCount { get; set; }
    public int? DailyQuota { get; set; }
    public int? WeeklyQuota { get; set; }
    public int? MonthlyQuota { get; set; }
}

public class CustomerDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string? Notes { get; set; }
    public int PersonnelCount { get; set; }
    public int OrganizationCount { get; set; }
    public int BranchCount { get; set; }
    public int ProjectCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Hedefler ve Kotalar
    public int? TargetCount { get; set; }
    public int? DailyQuota { get; set; }
    public int? WeeklyQuota { get; set; }
    public int? MonthlyQuota { get; set; }

    // Değerlendirme Bildirimi
    public int EvaluationNotificationFrequencyId { get; set; }
    public int? EvaluationNotificationTemplateId { get; set; }
    public string? EvaluationNotificationTemplateName { get; set; }
    public string? NotificationEmails { get; set; }
    public DateTime? LastNotificationSentAt { get; set; }
}

public class CreateCustomerDto
{
    public string? Code { get; set; }

    [Required(ErrorMessage = "Firma adı zorunludur")]
    [StringLength(255)]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vergi numarası zorunludur")]
    [StringLength(50)]
    public string TaxNumber { get; set; } = string.Empty;

    [StringLength(20)]
    [Phone]
    public string? Phone { get; set; }

    [StringLength(255)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? ContractStartDate { get; set; }

    public DateTime? ContractEndDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    // Hedefler ve Kotalar
    public int? TargetCount { get; set; }
    public int? DailyQuota { get; set; }
    public int? WeeklyQuota { get; set; }
    public int? MonthlyQuota { get; set; }

    // Değerlendirme Bildirimi
    public int EvaluationNotificationFrequencyId { get; set; }
    public int? EvaluationNotificationTemplateId { get; set; }
    public string? NotificationEmails { get; set; }
}

public class UpdateCustomerDto
{
    public string? Code { get; set; }

    [Required(ErrorMessage = "Firma adı zorunludur")]
    [StringLength(255)]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vergi numarası zorunludur")]
    [StringLength(50)]
    public string TaxNumber { get; set; } = string.Empty;

    [StringLength(20)]
    [Phone]
    public string? Phone { get; set; }

    [StringLength(255)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    public bool IsActive { get; set; }

    public DateTime? ContractStartDate { get; set; }

    public DateTime? ContractEndDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    // Hedefler ve Kotalar
    public int? TargetCount { get; set; }
    public int? DailyQuota { get; set; }
    public int? WeeklyQuota { get; set; }
    public int? MonthlyQuota { get; set; }

    // Değerlendirme Bildirimi
    public int EvaluationNotificationFrequencyId { get; set; }
    public int? EvaluationNotificationTemplateId { get; set; }
    public string? NotificationEmails { get; set; }
}

/// <summary>
/// Müşteri filtre DTO - server-side filtering için
/// </summary>
public class CustomerFilterDto
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }

    // Çoklu parametreler
    public List<string>? CompanyNames { get; set; }
    public List<string>? Codes { get; set; }
    public List<string>? Emails { get; set; }
    public List<string>? Cities { get; set; }
    public List<string>? TaxNumbers { get; set; }

    public bool IncludeInactive { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    // Sorting
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
}

/// <summary>
/// Sayfalı müşteri sonucu
/// </summary>
public class PagedCustomerResult
{
    public List<CustomerListDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
