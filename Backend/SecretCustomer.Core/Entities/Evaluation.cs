using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Attributes;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Değerlendirme", Description = "Değerlendirmeler için Excel import/export", IsAvailable = true)]
public class Evaluation : BaseEntity
{
    public Guid AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    public Guid? EvaluatorId { get; set; }
    public User? Evaluator { get; set; }

    [ExcelColumn("Durum", 1, ColumnType = ExcelColumnType.Dropdown,
        Description = "Değerlendirme durumu",
        DropdownOptions = "[\"Pending\", \"InProgress\", \"Completed\"]",
        SampleValue = "Pending")]
    public EvaluationStatus Status { get; set; } = EvaluationStatus.Pending;

    [ExcelColumn("Toplam Puan", 2, ColumnType = ExcelColumnType.Number,
        Description = "Alınan toplam puan", SampleValue = "85")]
    public decimal? TotalScore { get; set; }

    [ExcelColumn("Maksimum Puan", 3, ColumnType = ExcelColumnType.Number,
        Description = "Alınabilecek maksimum puan", SampleValue = "100")]
    public decimal? MaxScore { get; set; }

    [ExcelColumn("Yüzde", 4, ColumnType = ExcelColumnType.Number,
        Description = "Puan yüzdesi", SampleValue = "85")]
    public decimal? ScorePercentage { get; set; }

    [ExcelColumn("Başlangıç Zamanı", 5, ColumnType = ExcelColumnType.Date,
        Description = "Değerlendirmenin başlangıç zamanı")]
    public DateTime? StartedAt { get; set; }

    [ExcelColumn("Tamamlanma Zamanı", 6, ColumnType = ExcelColumnType.Date,
        Description = "Değerlendirmenin tamamlanma zamanı")]
    public DateTime? CompletedAt { get; set; }

    [ExcelColumn("Notlar", 7, ColumnType = ExcelColumnType.Text,
        Description = "Değerlendirme ile ilgili notlar", SampleValue = "Genel olarak başarılı")]
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
