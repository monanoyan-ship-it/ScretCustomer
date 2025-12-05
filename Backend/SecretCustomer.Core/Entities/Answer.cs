using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Cevap", Description = "Anket cevapları için Excel import/export", IsAvailable = true)]
public class Answer : BaseEntity
{
    public Guid EvaluationId { get; set; }
    public Evaluation Evaluation { get; set; } = null!;

    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    [ExcelColumn("Cevap (Metin)", 1, ColumnType = ExcelColumnType.Text,
        Description = "Metin cevabı", SampleValue = "Çok memnunum")]
    public string? AnswerText { get; set; }

    [ExcelColumn("Cevap (Sayısal)", 2, ColumnType = ExcelColumnType.Number,
        Description = "Sayısal cevap (Likert, Star için)", SampleValue = "5")]
    public int? AnswerNumeric { get; set; }

    [ExcelColumn("N/A", 3, ColumnType = ExcelColumnType.Boolean,
        Description = "N/A olarak işaretlendi mi?", SampleValue = "false")]
    public bool IsNA { get; set; } = false;

    [ExcelColumn("Kazanılan Puan", 4, ColumnType = ExcelColumnType.Number,
        Description = "Bu cevap için kazanılan puan", SampleValue = "10")]
    public decimal? EarnedPoints { get; set; }
}
