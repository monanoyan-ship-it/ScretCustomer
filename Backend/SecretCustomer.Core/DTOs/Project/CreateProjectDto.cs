using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Project;

public class CreateProjectDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public Guid ChecklistId { get; set; }

    [Required]
    public string AssignmentType { get; set; } = string.Empty; // Internal, External

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
