using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IExcelTemplateService
{
    Task<ExcelTemplate?> GetByIdAsync(int id, bool includeColumns = false);
    Task<IEnumerable<ExcelTemplate>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<ExcelTemplate>> GetByEntityTypeAsync(string entityType);
    Task<ExcelTemplate> CreateAsync(ExcelTemplate template);
    Task<ExcelTemplate> UpdateAsync(ExcelTemplate template);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<byte[]> GenerateTemplateExcelAsync(int templateId);
    Task<ExcelParseResult> ParseExcelAsync(int templateId, byte[] fileContent);
    Task<ExcelTemplate> CreateFromAttributesAsync<T>(string templateName, string? description = null) where T : class;
    List<ExcelColumn> GetColumnsFromAttributes<T>() where T : class;
    Task<ExcelExportDto> ExportDataToExcelAsync(int templateId, List<ExportFilterValue>? filterValues = null);
    Task<ExcelPreviewResult> PreviewDataAsync(int templateId, List<ExportFilterValue>? filterValues = null, int maxRows = 20);
    Task<List<ExcelTemplateFilter>> SaveFiltersAsync(int templateId, List<ExcelTemplateFilter> filters);
}

public class ExportFilterValue
{
    public string PropertyName { get; set; } = string.Empty;
    public int OperatorTypeId { get; set; }
    public string Value { get; set; } = string.Empty;
}

public class ExcelPreviewResult
{
    public List<string> ColumnHeaders { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int TotalRowCount { get; set; }
    public bool IsTruncated { get; set; }
}

public class ExcelParseResult
{
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public List<ParsedRow> Rows { get; set; } = new();
}

public class ParsedRow
{
    public int RowNumber { get; set; }
    public Dictionary<string, object?> Data { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool IsValid { get; set; }
}
