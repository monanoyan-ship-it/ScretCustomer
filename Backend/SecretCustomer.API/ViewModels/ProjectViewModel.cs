using SecretCustomer.Core.DTOs.Project;
using SecretCustomer.Core.DTOs.Checklist;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.API.ViewModels;

public class ProjectIndexViewModel
{
    public List<ProjectDto> Projects { get; set; } = new();
    public bool IncludeInactive { get; set; }
}

public class ProjectCreateEditViewModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ChecklistId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(1);
    public List<ChecklistDto> AvailableChecklists { get; set; } = new();
}
