using SecretCustomer.Core.Enums;

namespace SecretCustomer.API.DTOs;

public class ExcelColumnDto
{
    public Guid Id { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public ExcelColumnType ColumnType { get; set; }
    public int Order { get; set; }
    public bool IsRequired { get; set; }
    public Dictionary<string, object>? ValidationRules { get; set; }
    public List<string>? DropdownOptions { get; set; }
    public string? SampleValue { get; set; }
    public string? Description { get; set; }
}

public class CreateExcelColumnDto
{
    public string ColumnName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public ExcelColumnType ColumnType { get; set; }
    public int Order { get; set; }
    public bool IsRequired { get; set; } = false;
    public Dictionary<string, object>? ValidationRules { get; set; }
    public List<string>? DropdownOptions { get; set; }
    public string? SampleValue { get; set; }
    public string? Description { get; set; }
}
