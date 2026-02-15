namespace SecretCustomer.API.DTOs;

public class ExcelTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string SheetName { get; set; } = string.Empty;
    public bool HasHeader { get; set; }
    public string? GroupByPropertyName { get; set; }
    public List<ExcelColumnDto> Columns { get; set; } = new();
    public List<ExcelTemplateFilterDto> Filters { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ExcelTemplateSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ColumnCount { get; set; }
    public int FilterCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateExcelTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string SheetName { get; set; } = "Sheet1";
    public bool HasHeader { get; set; } = true;
    public string? GroupByPropertyName { get; set; }
    public List<CreateExcelColumnDto> Columns { get; set; } = new();
}

public class CreateFromAttributesDto
{
    public string EntityType { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateExcelTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string SheetName { get; set; } = "Sheet1";
    public bool HasHeader { get; set; } = true;
    public string? GroupByPropertyName { get; set; }
    public List<UpdateExcelColumnDto> Columns { get; set; } = new();
}

public class UpdateExcelColumnDto
{
    public int? Id { get; set; }  // Null for new columns
    public string ColumnName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public int ColumnTypeId { get; set; } = Core.Enums.ExcelColumnTypes.Ids.Text;
    public int AggregateTypeId { get; set; } = Core.Enums.AggregateTypes.Ids.None;
    public int Order { get; set; }
    public bool IsRequired { get; set; } = false;
    public Dictionary<string, object>? ValidationRules { get; set; }
    public List<string>? DropdownOptions { get; set; }
    public string? SampleValue { get; set; }
    public string? Description { get; set; }
}

public class ExcelParseResultDto
{
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public List<ParsedRowDto> Rows { get; set; } = new();
}

public class ParsedRowDto
{
    public int RowNumber { get; set; }
    public Dictionary<string, object?> Data { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool IsValid { get; set; }
}

public class ExcelTemplateFilterDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public int OperatorTypeId { get; set; }
    public string OperatorTypeName => Core.Enums.FilterOperatorTypes.GetById(OperatorTypeId)?.SystemName ?? "Equals";
    public int Order { get; set; }
}

public class SaveExcelTemplateFiltersDto
{
    public List<SaveExcelTemplateFilterItemDto> Filters { get; set; } = new();
}

public class SaveExcelTemplateFilterItemDto
{
    public int? Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public int OperatorTypeId { get; set; } = Core.Enums.FilterOperatorTypes.Ids.Equals;
    public int Order { get; set; }
}

public class ExportDataRequestDto
{
    public List<ExportFilterValueDto> Filters { get; set; } = new();
}

public class ExportFilterValueDto
{
    public string PropertyName { get; set; } = string.Empty;
    public int OperatorTypeId { get; set; }
    public string Value { get; set; } = string.Empty;
}
