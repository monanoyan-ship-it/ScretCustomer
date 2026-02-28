namespace SecretCustomer.Core.DTOs.EvaluationImport;

public class EvaluationImportSessionDto
{
    public int Id { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? StatusBadgeClass { get; set; }
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int PendingRows { get; set; }
    public int SkippedRows { get; set; }
    public string? Notes { get; set; }
    public int? UploadedByUserId { get; set; }
    public string? UploadedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
