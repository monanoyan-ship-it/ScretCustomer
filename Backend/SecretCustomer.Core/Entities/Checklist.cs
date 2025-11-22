namespace SecretCustomer.Core.Entities;

public class Checklist : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsScored { get; set; } = true; // Puanlı mı puansız mı
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;

    // Navigation properties
    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
