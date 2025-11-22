namespace SecretCustomer.Core.Entities;

public class Assignment : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public string? ExternalEmail { get; set; } // Dış müşteri için email
    public string? ExternalName { get; set; }   // Dış müşteri için isim

    public string UniqueLink { get; set; } = Guid.NewGuid().ToString();
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
}
