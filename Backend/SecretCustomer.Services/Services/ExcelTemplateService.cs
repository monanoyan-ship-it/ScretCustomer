using ClosedXML.Excel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Core.Attributes;
using SecretCustomer.Data;
using SecretCustomer.Services.Helpers;
using System.Text;
using System.Text.Json;
using System.Reflection;

namespace SecretCustomer.Services.Services;

public class ExcelTemplateService : IExcelTemplateService
{
    private readonly IExcelTemplateRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationDbContext _context;

    public ExcelTemplateService(
        IExcelTemplateRepository repository,
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext context)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    public async Task<ExcelTemplate?> GetByIdAsync(int id, bool includeColumns = false)
    {
        return await _repository.GetByIdAsync(id, includeColumns);
    }

    public async Task<IEnumerable<ExcelTemplate>> GetAllAsync(bool includeInactive = false)
    {
        return await _repository.GetAllAsync(includeInactive);
    }

    public async Task<IEnumerable<ExcelTemplate>> GetByEntityTypeAsync(string entityType)
    {
        return await _repository.GetByEntityTypeAsync(entityType);
    }

    public async Task<ExcelTemplate> CreateAsync(ExcelTemplate template)
    {
        if (string.IsNullOrWhiteSpace(template.Name))
        {
            throw new ArgumentException("Template name is required");
        }

        if (template.Columns == null || !template.Columns.Any())
        {
            throw new ArgumentException("Template must have at least one column");
        }

        return await _repository.CreateAsync(template);
    }

    public async Task<ExcelTemplate> UpdateAsync(ExcelTemplate template)
    {
        if (string.IsNullOrWhiteSpace(template.Name))
        {
            throw new ArgumentException("Template name is required");
        }

        if (template.Columns == null || !template.Columns.Any())
        {
            throw new ArgumentException("Template must have at least one column");
        }

        var exists = await _repository.ExistsAsync(template.Id);
        if (!exists)
        {
            throw new InvalidOperationException($"Template with id {template.Id} not found");
        }

        return await _repository.UpdateAsync(template);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _repository.ExistsAsync(id);
    }

    public async Task<byte[]> GenerateTemplateExcelAsync(int templateId)
    {
        var template = await _repository.GetByIdAsync(templateId, includeColumns: true);

        if (template == null)
        {
            throw new InvalidOperationException($"Template with id {templateId} not found");
        }

        var templateDto = new
        {
            name = template.Name,
            description = template.Description,
            entity_type = template.EntityType,
            sheet_name = template.SheetName,
            has_header = template.HasHeader,
            columns = template.Columns.OrderBy(c => c.Order).Select(c => new
            {
                column_name = c.ColumnName,
                property_name = c.PropertyName,
                column_type = ExcelColumnTypes.GetById(c.ColumnTypeId)?.SystemName ?? "Text",
                order = c.Order,
                is_required = c.IsRequired,
                validation_rules = string.IsNullOrWhiteSpace(c.ValidationRules)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(c.ValidationRules),
                dropdown_options = string.IsNullOrWhiteSpace(c.DropdownOptions)
                    ? null
                    : JsonSerializer.Deserialize<List<string>>(c.DropdownOptions),
                sample_value = c.SampleValue,
                description = c.Description
            }).ToList()
        };

        var httpClient = _httpClientFactory.CreateClient("ExcelProcessor");
        var json = JsonSerializer.Serialize(templateDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("/generate-template", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Excel generation failed: {error}");
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<ExcelParseResult> ParseExcelAsync(int templateId, byte[] fileContent)
    {
        var template = await _repository.GetByIdAsync(templateId, includeColumns: true);

        if (template == null)
        {
            throw new InvalidOperationException($"Template with id {templateId} not found");
        }

        var templateDto = new
        {
            name = template.Name,
            description = template.Description,
            entity_type = template.EntityType,
            sheet_name = template.SheetName,
            has_header = template.HasHeader,
            columns = template.Columns.OrderBy(c => c.Order).Select(c => new
            {
                column_name = c.ColumnName,
                property_name = c.PropertyName,
                column_type = ExcelColumnTypes.GetById(c.ColumnTypeId)?.SystemName ?? "Text",
                order = c.Order,
                is_required = c.IsRequired,
                validation_rules = string.IsNullOrWhiteSpace(c.ValidationRules)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(c.ValidationRules),
                dropdown_options = string.IsNullOrWhiteSpace(c.DropdownOptions)
                    ? null
                    : JsonSerializer.Deserialize<List<string>>(c.DropdownOptions),
                sample_value = c.SampleValue,
                description = c.Description
            }).ToList()
        };

        var httpClient = _httpClientFactory.CreateClient("ExcelProcessor");

        using var formContent = new MultipartFormDataContent();

        var fileStreamContent = new ByteArrayContent(fileContent);
        fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        formContent.Add(fileStreamContent, "file", "upload.xlsx");

        var templateJson = JsonSerializer.Serialize(templateDto);
        formContent.Add(new StringContent(templateJson), "template");

        var response = await httpClient.PostAsync("/parse-excel", formContent);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Excel parsing failed: {error}");
        }

        var resultJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ExcelParseResult>(resultJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result == null)
        {
            throw new Exception("Failed to parse Excel result from Python service");
        }

        return result;
    }

    public List<ExcelColumn> GetColumnsFromAttributes<T>() where T : class
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columns = new List<ExcelColumn>();

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<ExcelColumnAttribute>();
            if (attribute == null)
                continue;

            var column = new ExcelColumn
            {
                ColumnName = attribute.ColumnName,
                PropertyName = property.Name,
                Order = attribute.Order,
                IsRequired = attribute.IsRequired,
                ColumnTypeId = attribute.ColumnType,
                Description = attribute.Description,
                SampleValue = attribute.SampleValue,
                ValidationRules = attribute.ValidationRules,
                DropdownOptions = attribute.DropdownOptions
            };

            columns.Add(column);
        }

        return columns.OrderBy(c => c.Order).ToList();
    }

    public async Task<ExcelTemplate> CreateFromAttributesAsync<T>(string templateName, string? description = null) where T : class
    {
        var type = typeof(T);
        var entityTypeName = type.Name;

        var columns = GetColumnsFromAttributes<T>();

        if (!columns.Any())
        {
            throw new InvalidOperationException($"No ExcelColumn attributes found on type {entityTypeName}");
        }

        var template = new ExcelTemplate
        {
            Name = templateName,
            Description = description ?? $"Excel template for {entityTypeName}",
            EntityType = entityTypeName,
            SheetName = entityTypeName,
            HasHeader = true,
            Columns = columns
        };

        return await CreateAsync(template);
    }

    public async Task<List<ExcelTemplateFilter>> SaveFiltersAsync(int templateId, List<ExcelTemplateFilter> filters)
    {
        var template = await _repository.GetByIdAsync(templateId, includeColumns: true);
        if (template == null)
            throw new InvalidOperationException($"Template with id {templateId} not found");

        var existingFilters = _context.ExcelTemplateFilters
            .Where(f => f.ExcelTemplateId == templateId);
        _context.ExcelTemplateFilters.RemoveRange(existingFilters);

        foreach (var filter in filters)
        {
            filter.ExcelTemplateId = templateId;
            filter.CreatedAt = Core.Helpers.TurkeyTime.Now;
            filter.UpdatedAt = Core.Helpers.TurkeyTime.Now;
            _context.ExcelTemplateFilters.Add(filter);
        }

        await _context.SaveChangesAsync();

        return await _context.ExcelTemplateFilters
            .Where(f => f.ExcelTemplateId == templateId)
            .OrderBy(f => f.Order)
            .ToListAsync();
    }

    public async Task<ExcelExportDto> ExportDataToExcelAsync(int templateId, List<ExportFilterValue>? filterValues = null)
    {
        var template = await _repository.GetByIdAsync(templateId, includeColumns: true);
        if (template == null)
            throw new InvalidOperationException($"Template with id {templateId} not found");

        var columns = template.Columns.OrderBy(c => c.Order).ToList();
        if (!columns.Any())
            throw new InvalidOperationException("Template has no columns");

        // Build and execute SQL via Dapper
        var rows = await ExecuteSqlQuery(template.EntityType, columns, template.GroupByPropertyName, filterValues, limit: null);

        // Build Excel from Dapper rows
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(template.SheetName ?? "Sheet1");

        WriteDataFromDapperRows(worksheet, rows, columns, template.HasHeader);

        worksheet.Columns().AdjustToContents();

        var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"{template.Name.Replace(" ", "_")}_{Core.Helpers.TurkeyTime.Now:yyyyMMddHHmmss}.xlsx",
            FileContent = stream.ToArray()
        };
    }

    public async Task<ExcelPreviewResult> PreviewDataAsync(int templateId, List<ExportFilterValue>? filterValues = null, int maxRows = 20)
    {
        var template = await _repository.GetByIdAsync(templateId, includeColumns: true);
        if (template == null)
            throw new InvalidOperationException($"Template with id {templateId} not found");

        var columns = template.Columns.OrderBy(c => c.Order).ToList();
        if (!columns.Any())
            throw new InvalidOperationException("Template has no columns");

        // Fetch maxRows + 1 to detect truncation
        var rows = await ExecuteSqlQuery(template.EntityType, columns, template.GroupByPropertyName, filterValues, limit: maxRows + 1);

        var isTruncated = rows.Count > maxRows;
        if (isTruncated)
            rows = rows.Take(maxRows).ToList();

        var columnHeaders = columns.Select(c => c.ColumnName).ToList();

        // Convert to named dictionaries
        var resultRows = rows.Select(row =>
        {
            var dict = new Dictionary<string, object?>();
            for (int i = 0; i < columns.Count; i++)
            {
                var alias = $"col_{i}";
                row.TryGetValue(alias, out var value);
                dict[columns[i].ColumnName] = value;
            }
            return dict;
        }).ToList();

        return new ExcelPreviewResult
        {
            ColumnHeaders = columnHeaders,
            Rows = resultRows,
            TotalRowCount = rows.Count + (isTruncated ? 1 : 0),
            IsTruncated = isTruncated
        };
    }

    private async Task<List<Dictionary<string, object?>>> ExecuteSqlQuery(
        string entityType,
        List<ExcelColumn> columns,
        string? groupByPropertyName,
        List<ExportFilterValue>? filterValues,
        int? limit)
    {
        var builder = new ExcelSqlQueryBuilder(_context.Model);
        var queryResult = builder.Build(entityType, columns, groupByPropertyName, filterValues, limit);

        var connection = _context.Database.GetDbConnection();
        var dapperRows = await connection.QueryAsync(queryResult.Sql, queryResult.Parameters);

        return dapperRows.Select(row =>
        {
            var dict = (IDictionary<string, object?>)row;
            return new Dictionary<string, object?>(dict);
        }).ToList();
    }

    private static void WriteDataFromDapperRows(
        IXLWorksheet worksheet,
        List<Dictionary<string, object?>> rows,
        List<ExcelColumn> columns,
        bool hasHeader)
    {
        int row = 1;

        if (hasHeader)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                worksheet.Cell(row, i + 1).Value = columns[i].ColumnName;
                worksheet.Cell(row, i + 1).Style.Font.Bold = true;
                worksheet.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }
            row++;
        }

        foreach (var dataRow in rows)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                var alias = $"col_{i}";
                dataRow.TryGetValue(alias, out var value);
                SetCellValue(worksheet.Cell(row, i + 1), value);
            }
            row++;
        }
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        if (value == null || value is DBNull)
        {
            cell.Value = "";
            return;
        }

        switch (value)
        {
            case int intVal:
                cell.Value = intVal;
                break;
            case long longVal:
                cell.Value = longVal;
                break;
            case decimal decVal:
                cell.Value = (double)decVal;
                break;
            case double dblVal:
                cell.Value = dblVal;
                break;
            case float fltVal:
                cell.Value = (double)fltVal;
                break;
            case bool boolVal:
                cell.Value = boolVal ? "Evet" : "Hayır";
                break;
            case DateTime dtVal:
                cell.Value = dtVal;
                cell.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }
}
