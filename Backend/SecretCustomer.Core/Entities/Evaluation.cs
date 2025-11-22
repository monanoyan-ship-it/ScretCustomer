using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

public class Evaluation : BaseEntity
{
    public Guid AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    public Guid? EvaluatorId { get; set; }
    public User? Evaluator { get; set; }

    public EvaluationStatus Status { get; set; } = EvaluationStatus.Pending;

    public decimal? TotalScore { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? ScorePercentage { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
