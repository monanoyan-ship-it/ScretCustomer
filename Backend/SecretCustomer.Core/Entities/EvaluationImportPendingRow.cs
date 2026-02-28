using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

public class EvaluationImportPendingRow : BaseEntity
{
    public int ImportSessionId { get; set; }
    public EvaluationImportSession ImportSession { get; set; } = null!;

    public int RowNumber { get; set; }
    public string RawDataJson { get; set; } = string.Empty;

    public string? ParsedProjectName { get; set; }
    public string? ParsedEvaluatorName { get; set; }
    public string? ParsedEvaluatedPersonName { get; set; }
    public string? ParsedCallId { get; set; }
    public DateTime? ParsedCallDate { get; set; }
    public string? ParsedCallTime { get; set; }
    public string? ParsedDuration { get; set; }
    public string? ParsedComment { get; set; }
    public decimal? ParsedScore { get; set; }
    public string? ParsedPeriod { get; set; }
    public string? ParsedPeriodMonth { get; set; }
    public DateTime? ParsedCreatedDate { get; set; }
    public DateTime? ParsedModifiedDate { get; set; }

    public int? MatchedProjectId { get; set; }
    public Project? MatchedProject { get; set; }

    public int? MatchedEvaluatorId { get; set; }
    public User? MatchedEvaluator { get; set; }

    public int? MatchedCustomerPersonnelId { get; set; }
    public CustomerPersonnel? MatchedCustomerPersonnel { get; set; }

    public string? UnmatchedProjectValue { get; set; }
    public string? UnmatchedEvaluatorValue { get; set; }
    public string? UnmatchedPersonValue { get; set; }

    public int StatusId { get; set; } = EvaluationImportRowStatuses.Ids.Pending;

    public int? EvaluationId { get; set; }
    public Evaluation? Evaluation { get; set; }
}
