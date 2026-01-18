using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Assignment;

public class CreateAssignmentDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int ChecklistId { get; set; }

    public int? AssignedUserId { get; set; }

    public int? AssignedCustomerPersonnelId { get; set; }

    [EmailAddress]
    public string? ExternalEmail { get; set; }

    [MaxLength(200)]
    public string? ExternalName { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// True ise proje aktif değilse otomatik aktif edilir
    /// </summary>
    public bool ForceActivateProject { get; set; } = false;
}

public class UpdateAssignmentDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int ChecklistId { get; set; }

    public int? AssignedUserId { get; set; }

    public int? AssignedCustomerPersonnelId { get; set; }

    [EmailAddress]
    public string? ExternalEmail { get; set; }

    [MaxLength(200)]
    public string? ExternalName { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// Atama yeniden atama
/// </summary>
public class ReassignAssignmentDto
{
    public int? NewAssignedUserId { get; set; }
    public int? NewAssignedCustomerPersonnelId { get; set; }
    public string? NewExternalEmail { get; set; }
    public string? NewExternalName { get; set; }
    public DateTime? NewDueDate { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Atama iptal
/// </summary>
public class CancelAssignmentDto
{
    public string? Reason { get; set; }
}

/// <summary>
/// Tarih güncelleme DTO
/// </summary>
public class UpdateDueDateDto
{
    [Required]
    public DateTime NewDueDate { get; set; }
}

/// <summary>
/// Atama filtreleme
/// </summary>
public class AssignmentFilterDto
{
    // Çoklu filtreler (OR mantığı)
    public List<int>? ProjectIds { get; set; }
    public List<int>? AssignedUserIds { get; set; }
    public List<int>? ChecklistIds { get; set; }
    public List<string>? Statuses { get; set; }

    // Tekil filtreler (çoklu mantıklı değil)
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool? IsCompleted { get; set; }
    public bool? IsExpired { get; set; }
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Son tarih filtresi - atama son tarihine göre filtreleme
    /// Değerler: overdue, today, tomorrow, next7Days, thisWeek, next30Days, thisMonth, nextMonth
    /// </summary>
    public string? DueDateFilter { get; set; }

    // Sorting
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "desc";

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Sayfalanmış atama sonucu
/// </summary>
public class PagedAssignmentResult
{
    public List<AssignmentDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
