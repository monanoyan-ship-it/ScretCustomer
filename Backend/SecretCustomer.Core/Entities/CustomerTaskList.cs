using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;
using TaskStatus = SecretCustomer.Core.Enums.TaskStatus;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Müşteri Görev Listesi", Description = "Müşterilere özel görev listeleri", IsAvailable = true)]
public class CustomerTaskList : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [ExcelColumn("Görev Adı", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Görev listesinin adı", SampleValue = "Şube Denetimi")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Açıklama", 2, ColumnType = ExcelColumnType.Text,
        Description = "Görev hakkında açıklama", SampleValue = "Aylık şube denetim görevleri")]
    public string? Description { get; set; }

    [ExcelColumn("Görev Tipi", 3, IsRequired = true, ColumnType = ExcelColumnType.Dropdown,
        Description = "Görev tipi",
        DropdownOptions = "[\"Inspection\", \"Audit\", \"Survey\", \"FieldWork\", \"Reporting\"]",
        SampleValue = "Inspection")]
    public CustomerTaskType TaskType { get; set; }

    [ExcelColumn("Öncelik", 4, ColumnType = ExcelColumnType.Dropdown,
        Description = "Görev önceliği",
        DropdownOptions = "[\"Low\", \"Medium\", \"High\", \"Critical\"]",
        SampleValue = "Medium")]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [ExcelColumn("Başlangıç Tarihi", 5, ColumnType = ExcelColumnType.Date,
        Description = "Görev başlangıç tarihi")]
    public DateTime? StartDate { get; set; }

    [ExcelColumn("Bitiş Tarihi", 6, ColumnType = ExcelColumnType.Date,
        Description = "Görev bitiş tarihi")]
    public DateTime? EndDate { get; set; }

    [ExcelColumn("Durum", 7, ColumnType = ExcelColumnType.Dropdown,
        Description = "Görev durumu",
        DropdownOptions = "[\"NotStarted\", \"InProgress\", \"Completed\", \"Cancelled\"]",
        SampleValue = "NotStarted")]
    public TaskStatus Status { get; set; } = TaskStatus.NotStarted;

    [ExcelColumn("Aktif", 8, ColumnType = ExcelColumnType.Boolean,
        Description = "Görevin aktif olup olmadığı", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    // İlgili Checklist (opsiyonel)
    public int? ChecklistId { get; set; }
    public Checklist? Checklist { get; set; }

    // Navigation Properties
    public ICollection<CustomerPersonnelTaskAssignment> PersonnelAssignments { get; set; } = new List<CustomerPersonnelTaskAssignment>();
}