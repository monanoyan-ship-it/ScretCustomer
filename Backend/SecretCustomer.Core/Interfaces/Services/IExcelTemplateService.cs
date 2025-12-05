using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IExcelTemplateService
{
    Task<ExcelTemplate?> GetByIdAsync(Guid id, bool includeColumns = false);
    Task<IEnumerable<ExcelTemplate>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<ExcelTemplate>> GetByEntityTypeAsync(string entityType);
    Task<ExcelTemplate> CreateAsync(ExcelTemplate template);
    Task<ExcelTemplate> UpdateAsync(ExcelTemplate template);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<byte[]> GenerateTemplateExcelAsync(Guid templateId);
    Task<ExcelParseResult> ParseExcelAsync(Guid templateId, byte[] fileContent);
    Task<ExcelTemplate> CreateFromAttributesAsync<T>(string templateName, string? description = null) where T : class;
    List<ExcelColumn> GetColumnsFromAttributes<T>() where T : class;
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
