using SecretCustomer.Core.DTOs.Checklist;

namespace SecretCustomer.API.ViewModels;

public class ChecklistIndexViewModel
{
    public List<ChecklistDto> Checklists { get; set; } = new();
}

public class ChecklistCreateEditViewModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public List<SectionDto> Sections { get; set; } = new();
}
