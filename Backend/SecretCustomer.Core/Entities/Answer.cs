namespace SecretCustomer.Core.Entities;

public class Answer : BaseEntity
{
    public Guid EvaluationId { get; set; }
    public Evaluation Evaluation { get; set; } = null!;

    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public string? AnswerText { get; set; }
    public int? AnswerNumeric { get; set; }  // Likert, Star için
    public bool IsNA { get; set; } = false;  // N/A olarak işaretlendi mi?

    public decimal? EarnedPoints { get; set; }  // Bu cevap için kazanılan puan
}
