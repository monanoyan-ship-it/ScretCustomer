using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Soru Alt Kriteri (Öneri) - Her soruya bağlı alt kriterler/öneriler
/// Değerlendirme sırasında seçilebilir (puan düşürme nedeni olarak)
/// </summary>
[ExcelTemplate("Alt Kriter", Description = "Soru alt kriterleri/önerileri için Excel import/export", IsAvailable = true)]
public class QuestionSubCriteria : BaseEntity
{
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    /// <summary>
    /// Alt kriter/öneri açıklaması
    /// Örnek: "İlgili davranılmadı", "İsim ile hitap etmedi"
    /// </summary>
    [ExcelColumn("Açıklama", 1, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Alt kriter açıklaması", SampleValue = "İlgili davranılmadı")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Öz değerlendirme açıklaması (PersonnelAssessment için)
    /// Örnek: "Fikirlerimi açık ve net ifade ederim" (Self rolünde gösterilir, boşsa Description kullanılır)
    /// </summary>
    public string? SelfDescription { get; set; }

    /// <summary>
    /// Sıralama
    /// </summary>
    [ExcelColumn("Sıra", 2, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Alt kriterin sırası", SampleValue = "1")]
    public int Order { get; set; }

    /// <summary>
    /// Ağırlık puanı - Bu alt kriter seçildiğinde düşürülecek puan
    /// </summary>
    [ExcelColumn("Ağırlık Puanı", 3, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Seçildiğinde düşürülecek puan", SampleValue = "1")]
    public decimal WeightPoints { get; set; } = 1;

    /// <summary>
    /// Aktif mi?
    /// </summary>
    [ExcelColumn("Aktif", 4, ColumnType = ExcelColumnTypes.Ids.Boolean,
        Description = "Alt kriter aktif mi?", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    // Navigation - Değerlendirmelerde bu alt kriterin seçildiği cevaplar
    public ICollection<AnswerSubCriteriaSelection> Selections { get; set; } = new List<AnswerSubCriteriaSelection>();
}
