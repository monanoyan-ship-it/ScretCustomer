using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Http;

namespace SecretCustomer.Services.Services;

public class ExcelTemplateService : IExcelTemplateService
{
    private readonly IExcelTemplateRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExcelTemplateService(
        IExcelTemplateRepository repository,
        IHttpClientFactory httpClientFactory)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ExcelTemplate?> GetByIdAsync(Guid id, bool includeColumns = false)
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
        // Validate template
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
        // Validate template
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

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _repository.ExistsAsync(id);
    }

    public async Task<byte[]> GenerateTemplateExcelAsync(Guid templateId)
    {
        // Get template with columns
        var template = await _repository.GetByIdAsync(templateId, includeColumns: true);

        if (template == null)
        {
            throw new InvalidOperationException($"Template with id {templateId} not found");
        }

        // Prepare template data for Python service
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
                column_type = c.ColumnType.ToString(),
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

        // Call Python service
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

    public async Task<ExcelParseResult> ParseExcelAsync(Guid templateId, byte[] fileContent)
    {
        // Get template with columns
        var template = await _repository.GetByIdAsync(templateId, includeColumns: true);

        if (template == null)
        {
            throw new InvalidOperationException($"Template with id {templateId} not found");
        }

        // Prepare template data for Python service
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
                column_type = c.ColumnType.ToString(),
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

        // Call Python service with multipart form data
        var httpClient = _httpClientFactory.CreateClient("ExcelProcessor");

        using var formContent = new MultipartFormDataContent();

        // Add Excel file
        var fileStreamContent = new ByteArrayContent(fileContent);
        fileStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        formContent.Add(fileStreamContent, "file", "upload.xlsx");

        // Add template as JSON string
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
}
