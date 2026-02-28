using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

public class EvaluationImportSession : BaseEntity
{
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string FileName { get; set; } = string.Empty;

    public int StatusId { get; set; } = EvaluationImportSessionStatuses.Ids.Pending;

    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int PendingRows { get; set; }
    public int SkippedRows { get; set; }

    public string? Notes { get; set; }

    public int? UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }

    public ICollection<EvaluationImportPendingRow> PendingRowItems { get; set; } = new List<EvaluationImportPendingRow>();
    public ICollection<EvaluationImportUnmatchedItem> UnmatchedItems { get; set; } = new List<EvaluationImportUnmatchedItem>();
}
