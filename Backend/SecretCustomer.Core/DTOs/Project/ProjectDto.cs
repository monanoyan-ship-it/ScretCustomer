namespace SecretCustomer.Core.DTOs.Project;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ChecklistId { get; set; }
    public string ChecklistName { get; set; } = string.Empty;
    public string AssignmentType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
}
