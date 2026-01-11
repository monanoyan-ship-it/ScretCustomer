using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Müşteri Görev Listesi", Description = "Müşterilere özel görev listeleri", IsAvailable = true)]
public class CustomerTaskList : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [ExcelColumn("Görev Adı", 1, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Görev listesinin adı", SampleValue = "Şube Denetimi")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Açıklama", 2, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Görev hakkında açıklama", SampleValue = "Aylık şube denetim görevleri")]
    public string? Description { get; set; }

    [ExcelColumn("Görev Tipi", 3, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Dropdown,
        Description = "Görev tipi",
        DropdownOptions = "[\"Inspection\", \"Audit\", \"Survey\", \"FieldWork\", \"Reporting\"]",
        SampleValue = "Inspection")]
    public int TaskTypeId { get; set; } = CustomerTaskTypes.Ids.Inspection;

    [ExcelColumn("Öncelik", 4, ColumnType = ExcelColumnTypes.Ids.Dropdown,
        Description = "Görev önceliği",
        DropdownOptions = "[\"Low\", \"Medium\", \"High\", \"Critical\"]",
        SampleValue = "Medium")]
    public int PriorityId { get; set; } = TaskPriorities.Ids.Medium;

    [ExcelColumn("Başlangıç Tarihi", 5, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Görev başlangıç tarihi")]
    public DateTime? StartDate { get; set; }

    [ExcelColumn("Bitiş Tarihi", 6, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Görev bitiş tarihi")]
    public DateTime? EndDate { get; set; }

    [ExcelColumn("Durum", 7, ColumnType = ExcelColumnTypes.Ids.Dropdown,
        Description = "Görev durumu",
        DropdownOptions = "[\"NotStarted\", \"InProgress\", \"Completed\", \"Cancelled\"]",
        SampleValue = "NotStarted")]
    public int StatusId { get; set; } = TaskStatuses.Ids.NotStarted;

    [ExcelColumn("Aktif", 8, ColumnType = ExcelColumnTypes.Ids.Boolean,
        Description = "Görevin aktif olup olmadığı", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    // İlgili Checklist (opsiyonel)
    public int? ChecklistId { get; set; }
    public Checklist? Checklist { get; set; }

    // Navigation Properties
    public ICollection<CustomerPersonnelTaskAssignment> PersonnelAssignments { get; set; } = new List<CustomerPersonnelTaskAssignment>();
}