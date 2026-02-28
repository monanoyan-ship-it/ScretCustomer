namespace SecretCustomer.Core.DTOs.EvaluationImport;

public class EvaluationImportSessionDetailDto : EvaluationImportSessionDto
{
    public int UnmatchedPersonCount { get; set; }
    public int UnmatchedEvaluatorCount { get; set; }
    public int UnmatchedProjectCount { get; set; }
    public int ResolvedItemCount { get; set; }
    public int ReadyToImportRowCount { get; set; }
}
