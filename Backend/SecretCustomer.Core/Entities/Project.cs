using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Attributes;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Proje", Description = "Projeler için Excel import/export", IsAvailable = true)]
public class Project : BaseEntity
{
    [ExcelColumn("Proje Adı", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Projenin adı", SampleValue = "2024 Müşteri Memnuniyeti Projesi")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Açıklama", 2, ColumnType = ExcelColumnType.Text,
        Description = "Proje hakkında detaylı açıklama", SampleValue = "Q1 dönemi müşteri memnuniyeti ölçümü")]
    public string? Description { get; set; }

    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    [ExcelColumn("Atama Tipi", 3, IsRequired = true, ColumnType = ExcelColumnType.Dropdown,
        Description = "Atama tipi",
        DropdownOptions = "[\"InternalBranch\", \"InternalUser\", \"ExternalCustomer\", \"CustomerPersonnel\", \"FieldWorker\"]",
        SampleValue = "InternalBranch")]
    public AssignmentType AssignmentType { get; set; }

    [ExcelColumn("Başlangıç Tarihi", 4, IsRequired = true, ColumnType = ExcelColumnType.Date,
        Description = "Projenin başlangıç tarihi", SampleValue = "2024-01-01")]
    public DateTime StartDate { get; set; }

    [ExcelColumn("Bitiş Tarihi", 5, IsRequired = true, ColumnType = ExcelColumnType.Date,
        Description = "Projenin bitiş tarihi", SampleValue = "2024-12-31")]
    public DateTime EndDate { get; set; }

    [ExcelColumn("Aktif", 6, ColumnType = ExcelColumnType.Boolean,
        Description = "Projenin aktif olup olmadığı", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}
