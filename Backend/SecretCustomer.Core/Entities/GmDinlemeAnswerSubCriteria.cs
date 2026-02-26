namespace SecretCustomer.Core.Entities;

/// <summary>
/// Gölge Müşteri - Dinleme Cevap Alt Kriter (junction table)
/// </summary>
public class GmDinlemeAnswerSubCriteria
{
    public int GmDinlemeAnswerId { get; set; }
    public GmDinlemeAnswer? GmDinlemeAnswer { get; set; }

    public int SubCriteriaId { get; set; }
    public QuestionSubCriteria? SubCriteria { get; set; }
}
