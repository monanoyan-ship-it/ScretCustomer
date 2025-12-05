using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Kontrol Listesi", Description = "Kontrol listeleri için Excel import/export", IsAvailable = true)]
public class Checklist : BaseEntity
{
    [ExcelColumn("Kontrol Listesi Adı", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Kontrol listesinin adı", SampleValue = "Müşteri Memnuniyeti Anketi")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Açıklama", 2, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Kontrol listesi hakkında açıklama", SampleValue = "Genel müşteri memnuniyeti değerlendirmesi")]
    public string Description { get; set; } = string.Empty;

    [ExcelColumn("Puanlı", 3, ColumnType = ExcelColumnType.Boolean,
        Description = "Puanlı mı puansız mı?", SampleValue = "true")]
    public bool IsScored { get; set; } = true;

    [ExcelColumn("Aktif", 4, ColumnType = ExcelColumnType.Boolean,
        Description = "Kontrol listesi aktif mi?", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    [ExcelColumn("Versiyon", 5, ColumnType = ExcelColumnType.Number,
        Description = "Kontrol listesi versiyonu", SampleValue = "1")]
    public int Version { get; set; } = 1;

    // Navigation properties
    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
