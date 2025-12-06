using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Assignment;

public class UpdateAssignmentDto
{
    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    public Guid ChecklistId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? AssignedUserId { get; set; }
    
    public Guid? AssignedFieldWorkerId { get; set; }

    [EmailAddress]
    public string? ExternalEmail { get; set; }

    [MaxLength(200)]
    public string? ExternalName { get; set; }

    [Required]
    public DateTime DueDate { get; set; }
}
