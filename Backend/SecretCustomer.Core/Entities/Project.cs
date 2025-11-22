using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;
    public AssignmentType AssignmentType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
