namespace SecretCustomer.Core.Entities;

public class Section : BaseEntity
{
    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }

    // Navigation properties
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
