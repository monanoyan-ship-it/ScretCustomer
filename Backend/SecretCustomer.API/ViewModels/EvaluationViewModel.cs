using SecretCustomer.Core.DTOs.Assignment;

namespace SecretCustomer.API.ViewModels;

public class EvaluationIndexViewModel
{
    public List<AssignmentDto> PendingAssignments { get; set; } = new();
}

public class EvaluationFormViewModel
{
    public Guid AssignmentId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string ChecklistName { get; set; } = string.Empty;
    public List<EvaluationSectionViewModel> Sections { get; set; } = new();
    public string? Notes { get; set; }
}

public class EvaluationSectionViewModel
{
    public string Name { get; set; } = string.Empty;
    public List<EvaluationQuestionViewModel> Questions { get; set; } = new();
}

public class EvaluationQuestionViewModel
{
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool AllowNA { get; set; }
    public string? Options { get; set; }
    public int Order { get; set; }

    // Answer fields
    public int? AnswerNumeric { get; set; }
    public string? AnswerText { get; set; }
    public bool AnswerIsNA { get; set; }
}
