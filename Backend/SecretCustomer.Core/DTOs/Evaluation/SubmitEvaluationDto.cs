using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Evaluation;

public class SubmitEvaluationDto
{
    [Required]
    public Guid AssignmentId { get; set; }

    public Guid? EvaluatorId { get; set; }

    [Required]
    public List<SubmitAnswerDto> Answers { get; set; } = new();

    public string? Notes { get; set; }
}

public class SubmitAnswerDto
{
    [Required]
    public Guid QuestionId { get; set; }

    public string? AnswerText { get; set; }

    public int? AnswerNumeric { get; set; }

    public bool IsNA { get; set; } = false;
}
