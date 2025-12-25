using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Assignment;

public class CreateAssignmentDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int ChecklistId { get; set; }

    public int? BranchId { get; set; }

    public int? AssignedUserId { get; set; }

    public int? AssignedFieldWorkerId { get; set; }

    public int? AssignedCustomerPersonnelId { get; set; }

    [EmailAddress]
    public string? ExternalEmail { get; set; }

    [MaxLength(200)]
    public string? ExternalName { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public string? Notes { get; set; }
}

public class UpdateAssignmentDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int ChecklistId { get; set; }

    public int? BranchId { get; set; }

    public int? AssignedUserId { get; set; }

    public int? AssignedFieldWorkerId { get; set; }

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
/// Toplu atama oluşturma
/// </summary>
public class BulkAssignmentDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int ChecklistId { get; set; }

    [Required]
    public List<AssignmentItemDto> Assignments { get; set; } = new();
}

public class AssignmentItemDto
{
    public int? BranchId { get; set; }
    public int? AssignedUserId { get; set; }
    public int? AssignedFieldWorkerId { get; set; }
    public string? ExternalEmail { get; set; }
    public string? ExternalName { get; set; }
    public DateTime DueDate { get; set; }
}

/// <summary>
/// Proje şubelerine toplu atama oluşturma
/// </summary>
public class BulkProjectAssignmentDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Atanacak kullanıcı (boş bırakılırsa tüm proje takımına eşit dağıtılır)
    /// </summary>
    public int? AssignedUserId { get; set; }

    /// <summary>
    /// Her şube için kaç atama oluşturulacak (default: 1)
    /// </summary>
    public int AssignmentsPerBranch { get; set; } = 1;

    /// <summary>
    /// Sadece belirli şubeler için oluştur (boş ise tüm proje şubeleri)
    /// </summary>
    public List<int>? BranchIds { get; set; }
}

/// <summary>
/// Atama yeniden atama
/// </summary>
public class ReassignAssignmentDto
{
    public int? NewAssignedUserId { get; set; }
    public int? NewAssignedFieldWorkerId { get; set; }
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
/// Atama filtreleme
/// </summary>
public class AssignmentFilterDto
{
    public int? ProjectId { get; set; }
    public int? BranchId { get; set; }
    public int? AssignedUserId { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool? IsCompleted { get; set; }
    public bool? IsExpired { get; set; }
    public string? SearchTerm { get; set; }
}
