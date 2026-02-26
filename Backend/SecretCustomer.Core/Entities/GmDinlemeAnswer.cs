namespace SecretCustomer.Core.Entities;

/// <summary>
/// Gölge Müşteri - Dinleme Cevap (Evaluation Answer pattern kopyası)
/// </summary>
public class GmDinlemeAnswer : BaseEntity
{
    public int GmDinlemeEvaluationId { get; set; }
    public GmDinlemeEvaluation? GmDinlemeEvaluation { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    public int? AnswerNumeric { get; set; }
    public string? AnswerText { get; set; }

    public decimal GivenPoints { get; set; }
    public decimal EarnedPoints { get; set; }

    public bool ApplyPenalty { get; set; }

    /// <summary>
    /// Seçilen alt kriterler
    /// </summary>
    public ICollection<GmDinlemeAnswerSubCriteria> SubCriteriaSelections { get; set; } = new List<GmDinlemeAnswerSubCriteria>();
}
