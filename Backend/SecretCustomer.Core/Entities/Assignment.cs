using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Atama", Description = "Görev atamaları için Excel import/export", IsAvailable = false)]
public class Assignment : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    /// <summary>
    /// Atama tipi (AssignmentTypes.Ids kullanılır)
    /// </summary>
    public int TypeId { get; set; } = AssignmentTypes.Ids.InternalUser;

    public int? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public int? AssignedCustomerPersonnelId { get; set; }
    public CustomerPersonnel? AssignedCustomerPersonnel { get; set; }

    [ExcelColumn("Dış Müşteri E-postası", 1, ColumnType = ExcelColumnTypes.Ids.Email,
        Description = "Dış müşteri için e-posta adresi", SampleValue = "musteri@example.com")]
    public string? ExternalEmail { get; set; }

    [ExcelColumn("Dış Müşteri Adı", 2, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Dış müşteri için ad soyad", SampleValue = "Ayşe Yıldız")]
    public string? ExternalName { get; set; }

    [ExcelColumn("Benzersiz Link", 3, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Atama için benzersiz link")]
    public string UniqueLink { get; set; } = Guid.NewGuid().ToString();

    [ExcelColumn("Teslim Tarihi", 4, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Atamanın teslim tarihi", SampleValue = "2024-12-31")]
    public DateTime DueDate { get; set; }

    [ExcelColumn("Tamamlandı", 5, ColumnType = ExcelColumnTypes.Ids.Boolean,
        Description = "Atama tamamlandı mı?", SampleValue = "false")]
    public bool IsCompleted { get; set; } = false;

    [ExcelColumn("Tamamlanma Tarihi", 6, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Atamanın tamamlanma tarihi")]
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    public ICollection<AssignmentPeriod> Periods { get; set; } = new List<AssignmentPeriod>();
    public ICollection<AssignmentCustomerDealer> AssignmentCustomerDealers { get; set; } = new List<AssignmentCustomerDealer>();
}
