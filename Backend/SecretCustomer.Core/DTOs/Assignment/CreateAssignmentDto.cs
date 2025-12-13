using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Assignment;

public class CreateAssignmentDto
{
    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    public Guid ChecklistId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? AssignedUserId { get; set; }

    public Guid? AssignedFieldWorkerId { get; set; }

    public Guid? AssignedCustomerPersonnelId { get; set; }

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
    public Guid ProjectId { get; set; }

    [Required]
    public Guid ChecklistId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? AssignedUserId { get; set; }

    public Guid? AssignedFieldWorkerId { get; set; }

    public Guid? AssignedCustomerPersonnelId { get; set; }

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
    public Guid ProjectId { get; set; }

    [Required]
    public Guid ChecklistId { get; set; }

    [Required]
    public List<AssignmentItemDto> Assignments { get; set; } = new();
}

public class AssignmentItemDto
{
    public Guid? BranchId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? AssignedFieldWorkerId { get; set; }
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
    public Guid ProjectId { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Atanacak kullanıcı (boş bırakılırsa tüm proje takımına eşit dağıtılır)
    /// </summary>
    public Guid? AssignedUserId { get; set; }

    /// <summary>
    /// Her şube için kaç atama oluşturulacak (default: 1)
    /// </summary>
    public int AssignmentsPerBranch { get; set; } = 1;

    /// <summary>
    /// Sadece belirli şubeler için oluştur (boş ise tüm proje şubeleri)
    /// </summary>
    public List<Guid>? BranchIds { get; set; }
}

/// <summary>
/// Atama yeniden atama
/// </summary>
public class ReassignAssignmentDto
{
    public Guid? NewAssignedUserId { get; set; }
    public Guid? NewAssignedFieldWorkerId { get; set; }
    public Guid? NewAssignedCustomerPersonnelId { get; set; }
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
    public Guid? ProjectId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool? IsCompleted { get; set; }
    public bool? IsExpired { get; set; }
    public string? SearchTerm { get; set; }
}
