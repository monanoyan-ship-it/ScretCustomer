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

    [EmailAddress]
    public string? ExternalEmail { get; set; }

    [MaxLength(200)]
    public string? ExternalName { get; set; }

    [Required]
    public DateTime DueDate { get; set; }
}

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
    public string? ExternalEmail { get; set; }
    public string? ExternalName { get; set; }
    public DateTime DueDate { get; set; }
}
