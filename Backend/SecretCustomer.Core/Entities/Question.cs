using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Attributes;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Soru", Description = "Anket soruları için Excel import/export", IsAvailable = true)]
public class Question : BaseEntity
{
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;

    [ExcelColumn("Soru Metni", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Sorunun tam metni", SampleValue = "Ürün kalitesinden memnun musunuz?")]
    public string Text { get; set; } = string.Empty;

    [ExcelColumn("Soru Tipi", 2, IsRequired = true, ColumnType = ExcelColumnType.Dropdown,
        Description = "Sorunun tipi",
        DropdownOptions = "[\"YesNo\", \"Rating\", \"Text\", \"MultipleChoice\"]",
        SampleValue = "Rating")]
    public QuestionType Type { get; set; }

    [ExcelColumn("Sıra", 3, IsRequired = true, ColumnType = ExcelColumnType.Number,
        Description = "Sorunun sırası", SampleValue = "1")]
    public int Order { get; set; }

    [ExcelColumn("Puan", 4, ColumnType = ExcelColumnType.Number,
        Description = "Sorunun puanı", SampleValue = "10")]
    public int Points { get; set; } = 0;

    [ExcelColumn("N/A İzni", 5, ColumnType = ExcelColumnType.Boolean,
        Description = "N/A seçeneğine izin verilir mi?", SampleValue = "false")]
    public bool AllowNA { get; set; } = false;

    [ExcelColumn("Zorunlu", 6, ColumnType = ExcelColumnType.Boolean,
        Description = "Soru zorunlu mu?", SampleValue = "true")]
    public bool IsRequired { get; set; } = true;

    [ExcelColumn("Seçenekler (JSON)", 7, ColumnType = ExcelColumnType.Text,
        Description = "Çoktan seçmeli sorular için seçenekler (JSON formatında)")]
    public string? OptionsJson { get; set; }

    // Navigation properties
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
