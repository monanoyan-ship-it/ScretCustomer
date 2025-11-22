namespace SecretCustomer.Core.DTOs.Evaluation;

public class EvaluationDto
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid? EvaluatorId { get; set; }
    public string? EvaluatorName { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? ScorePercentage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
}

public class AnswerDto
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public int? AnswerNumeric { get; set; }
    public bool IsNA { get; set; }
    public decimal? EarnedPoints { get; set; }
}
