using SecretCustomer.Core.Enums;

namespace SecretCustomer.API.DTOs;

public class ExcelColumnDto
{
    public int Id { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public int ColumnTypeId { get; set; } = ExcelColumnTypes.Ids.Text;
    public string ColumnTypeName => ExcelColumnTypes.GetById(ColumnTypeId)?.SystemName ?? "Text";
    public int AggregateTypeId { get; set; } = AggregateTypes.Ids.None;
    public string AggregateTypeName => AggregateTypes.GetById(AggregateTypeId)?.SystemName ?? "None";
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
    public int ColumnTypeId { get; set; } = ExcelColumnTypes.Ids.Text;
    public int AggregateTypeId { get; set; } = AggregateTypes.Ids.None;
    public int Order { get; set; }
    public bool IsRequired { get; set; } = false;
    public Dictionary<string, object>? ValidationRules { get; set; }
    public List<string>? DropdownOptions { get; set; }
    public string? SampleValue { get; set; }
    public string? Description { get; set; }
}
