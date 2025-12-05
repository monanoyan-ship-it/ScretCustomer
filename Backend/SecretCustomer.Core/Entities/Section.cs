using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Bölüm", Description = "Kontrol listesi bölümleri için Excel import/export", IsAvailable = true)]
public class Section : BaseEntity
{
    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    [ExcelColumn("Bölüm Adı", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Bölümün adı", SampleValue = "Hizmet Kalitesi")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Açıklama", 2, ColumnType = ExcelColumnType.Text,
        Description = "Bölüm hakkında açıklama", SampleValue = "Hizmet kalitesi ile ilgili sorular")]
    public string? Description { get; set; }

    [ExcelColumn("Sıra", 3, IsRequired = true, ColumnType = ExcelColumnType.Number,
        Description = "Bölümün sırası", SampleValue = "1")]
    public int Order { get; set; }

    // Navigation properties
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
