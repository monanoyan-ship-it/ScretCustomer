namespace SecretCustomer.Core.DTOs.EvaluationImport;

public class PagedPendingRowResult
{
    public List<EvaluationImportPendingRowDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class PagedUnmatchedItemResult
{
    public List<EvaluationImportUnmatchedItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class EvaluationImportPendingRowDto
{
    public int Id { get; set; }
    public int ImportSessionId { get; set; }
    public int RowNumber { get; set; }
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
    public string? MatchedProjectName { get; set; }
    public int? MatchedEvaluatorId { get; set; }
    public string? MatchedEvaluatorName { get; set; }
    public int? MatchedCustomerPersonnelId { get; set; }
    public string? MatchedCustomerPersonnelName { get; set; }
    public string? UnmatchedProjectValue { get; set; }
    public string? UnmatchedEvaluatorValue { get; set; }
    public string? UnmatchedPersonValue { get; set; }
    public int StatusId { get; set; }
    public string? StatusName { get; set; }
    public int? EvaluationId { get; set; }
}
