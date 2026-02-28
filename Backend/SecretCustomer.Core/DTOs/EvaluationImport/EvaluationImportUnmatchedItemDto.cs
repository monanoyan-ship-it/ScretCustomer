namespace SecretCustomer.Core.DTOs.EvaluationImport;

public class EvaluationImportUnmatchedItemDto
{
    public int Id { get; set; }
    public int ImportSessionId { get; set; }
    public int ItemTypeId { get; set; }
    public string? ItemTypeName { get; set; }
    public string OriginalValue { get; set; } = string.Empty;
    public int AffectedRowCount { get; set; }
    public int? ResolvedEntityId { get; set; }
    public string? ResolvedEntityName { get; set; }
    public int? ResolutionActionId { get; set; }
    public string? ResolutionActionName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByUserId { get; set; }
    public string? ResolvedByUserName { get; set; }
    public bool IsResolved => ResolutionActionId.HasValue;
}
