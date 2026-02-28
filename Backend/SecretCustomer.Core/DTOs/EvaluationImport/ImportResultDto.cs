namespace SecretCustomer.Core.DTOs.EvaluationImport;

public class ImportResultDto
{
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
