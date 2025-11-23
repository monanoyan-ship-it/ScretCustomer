using SecretCustomer.Core.DTOs.Assignment;
using SecretCustomer.Core.DTOs.Project;
using SecretCustomer.Core.DTOs.Branch;
using SecretCustomer.Core.DTOs.Checklist;
using SecretCustomer.Core.DTOs.User;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.API.ViewModels;

public class AssignmentIndexViewModel
{
    public List<AssignmentDto> Assignments { get; set; } = new();
    public string? StatusFilter { get; set; }
    public string? TypeFilter { get; set; }
    public string? SearchQuery { get; set; }
}

public class AssignmentCreateViewModel
{
    public Guid ProjectId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid ChecklistId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7);

    public List<ProjectDto> AvailableProjects { get; set; } = new();
    public List<BranchDto> AvailableBranches { get; set; } = new();
    public List<ChecklistDto> AvailableChecklists { get; set; } = new();
    public List<UserDto> AvailableEvaluators { get; set; } = new();
}
