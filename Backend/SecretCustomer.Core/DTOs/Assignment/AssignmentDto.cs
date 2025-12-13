namespace SecretCustomer.Core.DTOs.Assignment;

public class AssignmentDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid ChecklistId { get; set; }
    public string ChecklistName { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public Guid? AssignedFieldWorkerId { get; set; }
    public string? AssignedFieldWorkerName { get; set; }
    public Guid? AssignedCustomerPersonnelId { get; set; }
    public string? AssignedCustomerPersonnelName { get; set; }
    public string? ExternalEmail { get; set; }
    public string? ExternalName { get; set; }
    public string UniqueLink { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
