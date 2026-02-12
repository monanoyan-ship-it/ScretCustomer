using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Cevap Alt Kriter Seçimi - Değerlendirme sırasında hangi alt kriterlerin seçildiğini tutar
/// Many-to-many ilişki: Answer <-> QuestionSubCriteria
/// </summary>
public class AnswerSubCriteriaSelection : BaseEntity
{
    public int AnswerId { get; set; }
    public Answer Answer { get; set; } = null!;

    public int SubCriteriaId { get; set; }
    public QuestionSubCriteria SubCriteria { get; set; } = null!;

    /// <summary>
    /// Seçim sırasında eklenen not (opsiyonel)
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Seçim tarihi
    /// </summary>
    public DateTime SelectedAt { get; set; } = TurkeyTime.Now;
}
