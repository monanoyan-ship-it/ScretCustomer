using System.ComponentModel.DataAnnotations;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.Assignment;

/// <summary>
/// İç değerlendirme ataması için toplu oluşturma DTO
/// </summary>
public class CreateInternalAssignmentsDto
{
    /// <summary>
    /// Hangi müşterinin personeline atama yapılacak
    /// </summary>
    [Required]
    public int CustomerId { get; set; }

    /// <summary>
    /// Hangi proje atanacak
    /// </summary>
    [Required]
    public int ProjectId { get; set; }

    /// <summary>
    /// Atanacak personel ID'leri (tek tek seçim)
    /// </summary>
    public List<int>? PersonnelIds { get; set; }

    /// <summary>
    /// Rol bazlı toplu atama (örn: tüm süpervizörler) - CustomerPersonnelRoles.Ids kullanılır
    /// </summary>
    public int? RoleFilterId { get; set; }

    /// <summary>
    /// Organizasyon bazlı filtreleme (opsiyonel)
    /// </summary>
    public int? OrganizationId { get; set; }

    /// <summary>
    /// Teslim tarihi
    /// </summary>
    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Notlar
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// İç değerlendirme ataması sonucu
/// </summary>
public class InternalAssignmentResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<InternalAssignmentItemResult> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class InternalAssignmentItemResult
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = "";
    public int? AssignmentId { get; set; }
    public string Status { get; set; } = ""; // Created, Skipped, Failed
    public string? Message { get; set; }
}

/// <summary>
/// İç değerlendirme atamaları filtreleme
/// </summary>
public class InternalAssignmentFilterDto
{
    public int? CustomerId { get; set; }
    public int? ProjectId { get; set; }
    public int? OrganizationId { get; set; }
    public int? RoleId { get; set; }
    public bool? IsCompleted { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }

    /// <summary>
    /// Belirli bir personele atanmış olanları filtrele
    /// </summary>
    public int? AssignedCustomerPersonnelId { get; set; }
}

/// <summary>
/// İç değerlendirme özet bilgisi
/// </summary>
public class InternalAssignmentSummaryDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int PendingAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public decimal CompletionRate { get; set; }
}
