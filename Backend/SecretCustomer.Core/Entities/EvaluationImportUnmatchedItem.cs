namespace SecretCustomer.Core.Entities;

public class EvaluationImportUnmatchedItem : BaseEntity
{
    public int ImportSessionId { get; set; }
    public EvaluationImportSession ImportSession { get; set; } = null!;

    public int ItemTypeId { get; set; }
    public string OriginalValue { get; set; } = string.Empty;
    public int AffectedRowCount { get; set; }

    public int? ResolvedEntityId { get; set; }
    public int? ResolutionActionId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByUserId { get; set; }
    public User? ResolvedByUser { get; set; }
}
