using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.API.DTOs;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using System.Reflection;
using System.Text.Json;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/excel-templates")]
[Authorize]
public class ExcelTemplatesApiController : BaseApiController
{
    private readonly IExcelTemplateService _excelTemplateService;
    private readonly ILocalizationService _localizationService;

    public ExcelTemplatesApiController(
        IExcelTemplateService excelTemplateService,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _excelTemplateService = excelTemplateService;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Get all Excel templates
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExcelTemplateSummaryDto>>> GetAll(
        [FromQuery] bool includeInactive = false)
    {
        var templates = await _excelTemplateService.GetAllAsync(includeInactive);

        var dtos = templates.Select(t => new ExcelTemplateSummaryDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            EntityType = t.EntityType,
            IsActive = t.IsActive,
            ColumnCount = t.Columns?.Count ?? 0,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt ?? t.CreatedAt
        });

        return Ok(dtos);
    }

    /// <summary>
    /// Get Excel template by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ExcelTemplateDto>> GetById(int id)
    {
        var template = await _excelTemplateService.GetByIdAsync(id, includeColumns: true);

        if (template == null)
        {
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.ExcelTemplate.NotFound")));
        }

        var dto = MapToDto(template);

        return Ok(dto);
    }

    /// <summary>
    /// Get templates by entity type
    /// </summary>
    [HttpGet("by-entity/{entityType}")]
    public async Task<ActionResult<IEnumerable<ExcelTemplateDto>>> GetByEntityType(string entityType)
    {
        var templates = await _excelTemplateService.GetByEntityTypeAsync(entityType);

        var dtos = templates.Select(MapToDto);

        return Ok(dtos);
    }

    /// <summary>
    /// Create new Excel template
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExcelTemplateDto>> Create(
        [FromBody] CreateExcelTemplateDto createDto)
    {
        try
        {
            var template = new ExcelTemplate
            {
                Name = createDto.Name,
                Description = createDto.Description,
                EntityType = createDto.EntityType,
                SheetName = createDto.SheetName,
                HasHeader = createDto.HasHeader,
                IsActive = true,
                Columns = createDto.Columns.Select(c => new ExcelColumn
                {
                    ColumnName = c.ColumnName,
                    PropertyName = c.PropertyName,
                    ColumnType = c.ColumnType,
                    Order = c.Order,
                    IsRequired = c.IsRequired,
                    ValidationRules = c.ValidationRules != null
                        ? JsonSerializer.Serialize(c.ValidationRules)
                        : null,
                    DropdownOptions = c.DropdownOptions != null
                        ? JsonSerializer.Serialize(c.DropdownOptions)
                        : null,
                    SampleValue = c.SampleValue,
                    Description = c.Description
                }).ToList()
            };

            var created = await _excelTemplateService.CreateAsync(template);

            var dto = MapToDto(created);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Update Excel template
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ExcelTemplateDto>> Update(
        int id,
        [FromBody] UpdateExcelTemplateDto updateDto)
    {
        try
        {
            var template = new ExcelTemplate
            {
                Id = id,
                Name = updateDto.Name,
                Description = updateDto.Description,
                EntityType = updateDto.EntityType,
                IsActive = updateDto.IsActive,
                SheetName = updateDto.SheetName,
                HasHeader = updateDto.HasHeader,
                Columns = updateDto.Columns.Select(c => new ExcelColumn
                {
                    Id = c.Id ?? 0,
                    ColumnName = c.ColumnName,
                    PropertyName = c.PropertyName,
                    ColumnType = c.ColumnType,
                    Order = c.Order,
                    IsRequired = c.IsRequired,
                    ValidationRules = c.ValidationRules != null
                        ? JsonSerializer.Serialize(c.ValidationRules)
                        : null,
                    DropdownOptions = c.DropdownOptions != null
                        ? JsonSerializer.Serialize(c.DropdownOptions)
                        : null,
                    SampleValue = c.SampleValue,
                    Description = c.Description
                }).ToList()
            };

            var updated = await _excelTemplateService.UpdateAsync(template);

            var dto = MapToDto(updated);

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Delete Excel template (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await _excelTemplateService.DeleteAsync(id);

        if (!result)
        {
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.ExcelTemplate.NotFound")));
        }

        return NoContent();
    }

    /// <summary>
    /// Generate and download sample Excel file based on template
    /// </summary>
    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportTemplate(int id)
    {
        try
        {
            var excelBytes = await _excelTemplateService.GenerateTemplateExcelAsync(id);

            var template = await _excelTemplateService.GetByIdAsync(id);
            var filename = $"{template?.Name.Replace(" ", "_") ?? "template"}_sample.xlsx";

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse(string.Format(await _localizationService.GetResourceAsync("Api.ExcelTemplate.GenerationFailed"), ex.Message), ex));
        }
    }

    /// <summary>
    /// Upload and parse Excel file based on template
    /// </summary>
    [HttpPost("{id}/import")]
    public async Task<ActionResult<ExcelParseResultDto>> ImportExcel(
        int id,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotSelected")));
        }

        if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
        {
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.ExcelTemplate.OnlyExcelAllowed")));
        }

        try
        {
            // Read file content
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileContent = memoryStream.ToArray();

            // Parse Excel
            var result = await _excelTemplateService.ParseExcelAsync(id, fileContent);

            // Map to DTO
            var resultDto = new ExcelParseResultDto
            {
                TotalRows = result.TotalRows,
                ValidRows = result.ValidRows,
                InvalidRows = result.InvalidRows,
                Rows = result.Rows.Select(r => new ParsedRowDto
                {
                    RowNumber = r.RowNumber,
                    Data = r.Data,
                    Errors = r.Errors,
                    IsValid = r.IsValid
                }).ToList()
            };

            return Ok(resultDto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse(string.Format(await _localizationService.GetResourceAsync("Api.ExcelTemplate.ParsingFailed"), ex.Message), ex));
        }
    }

    /// <summary>
    /// Create template from entity attributes
    /// </summary>
    [HttpPost("from-attributes")]
    public async Task<ActionResult<ExcelTemplateDto>> CreateFromAttributes(
        [FromBody] CreateFromAttributesDto dto)
    {
        try
        {
            ExcelTemplate template;

            switch (dto.EntityType.ToLower())
            {
                case "user":
                    template = await _excelTemplateService.CreateFromAttributesAsync<Core.Entities.User>(
                        dto.TemplateName, dto.Description);
                    break;
                case "project":
                    template = await _excelTemplateService.CreateFromAttributesAsync<Core.Entities.Project>(
                        dto.TemplateName, dto.Description);
                    break;
                case "assignment":
                    template = await _excelTemplateService.CreateFromAttributesAsync<Core.Entities.Assignment>(
                        dto.TemplateName, dto.Description);
                    break;
                case "checklist":
                    template = await _excelTemplateService.CreateFromAttributesAsync<Core.Entities.Checklist>(
                        dto.TemplateName, dto.Description);
                    break;
                case "section":
                    template = await _excelTemplateService.CreateFromAttributesAsync<Core.Entities.Section>(
                        dto.TemplateName, dto.Description);
                    break;
                case "question":
                    template = await _excelTemplateService.CreateFromAttributesAsync<Core.Entities.Question>(
                        dto.TemplateName, dto.Description);
                    break;
                case "evaluation":
                    template = await _excelTemplateService.CreateFromAttributesAsync<Core.Entities.Evaluation>(
                        dto.TemplateName, dto.Description);
                    break;
                case "answer":
                    template = await _excelTemplateService.CreateFromAttributesAsync<Core.Entities.Answer>(
                        dto.TemplateName, dto.Description);
                    break;
                default:
                    return BadRequest(CreateErrorResponse(string.Format(await _localizationService.GetResourceAsync("Api.ExcelTemplate.UnsupportedEntityType"), dto.EntityType)));
            }

            var resultDto = MapToDto(template);

            return CreatedAtAction(nameof(GetById), new { id = template.Id }, resultDto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Preview columns from entity attributes without creating template
    /// </summary>
    [HttpGet("attributes/{entityType}")]
    public async Task<ActionResult<List<ExcelColumnDto>>> GetColumnsFromAttributes(string entityType)
    {
        try
        {
            List<ExcelColumn> columns;

            switch (entityType.ToLower())
            {
                case "user":
                    columns = _excelTemplateService.GetColumnsFromAttributes<Core.Entities.User>();
                    break;
                case "project":
                    columns = _excelTemplateService.GetColumnsFromAttributes<Core.Entities.Project>();
                    break;
                case "assignment":
                    columns = _excelTemplateService.GetColumnsFromAttributes<Core.Entities.Assignment>();
                    break;
                case "checklist":
                    columns = _excelTemplateService.GetColumnsFromAttributes<Core.Entities.Checklist>();
                    break;
                case "section":
                    columns = _excelTemplateService.GetColumnsFromAttributes<Core.Entities.Section>();
                    break;
                case "question":
                    columns = _excelTemplateService.GetColumnsFromAttributes<Core.Entities.Question>();
                    break;
                case "evaluation":
                    columns = _excelTemplateService.GetColumnsFromAttributes<Core.Entities.Evaluation>();
                    break;
                case "answer":
                    columns = _excelTemplateService.GetColumnsFromAttributes<Core.Entities.Answer>();
                    break;
                default:
                    return BadRequest(CreateErrorResponse(string.Format(await _localizationService.GetResourceAsync("Api.ExcelTemplate.UnsupportedEntityType"), entityType)));
            }

            if (!columns.Any())
            {
                return NotFound(CreateErrorResponse(string.Format(await _localizationService.GetResourceAsync("Api.ExcelTemplate.NoAttributesFound"), entityType)));
            }

            var dtos = columns.Select(c => new ExcelColumnDto
            {
                ColumnName = c.ColumnName,
                PropertyName = c.PropertyName,
                ColumnType = c.ColumnType,
                Order = c.Order,
                IsRequired = c.IsRequired,
                ValidationRules = string.IsNullOrWhiteSpace(c.ValidationRules)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(c.ValidationRules),
                DropdownOptions = string.IsNullOrWhiteSpace(c.DropdownOptions)
                    ? null
                    : JsonSerializer.Deserialize<List<string>>(c.DropdownOptions),
                SampleValue = c.SampleValue,
                Description = c.Description
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse(ex.Message, ex));
        }
    }

    /// <summary>
    /// Get available entities and their properties for template creation
    /// Automatically discovers entities marked with ExcelTemplateAttribute
    /// </summary>
    [HttpGet("schema")]
    public IActionResult GetEntitySchema()
    {
        // Automatically discover all entities with ExcelTemplateAttribute
        var assembly = typeof(Core.Entities.User).Assembly;
        var entityTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "SecretCustomer.Core.Entities")
            .Select(t => new
            {
                Type = t,
                Attribute = t.GetCustomAttribute<Core.Attributes.ExcelTemplateAttribute>()
            })
            .Where(x => x.Attribute != null && x.Attribute.IsAvailable)
            .OrderBy(x => x.Attribute!.DisplayName)
            .ToList();

        var schema = entityTypes.Select(item => new
        {
            entityName = item.Type.Name,
            displayName = item.Attribute!.DisplayName,
            description = item.Attribute.Description,
            properties = item.Type.GetProperties()
                .Where(p => p.GetCustomAttribute<Core.Attributes.ExcelColumnAttribute>() != null) // Only properties with ExcelColumn attribute
                .Select(p => new
                {
                    propertyName = p.Name,
                    propertyType = GetSimpleTypeName(p.PropertyType)
                })
                .OrderBy(p => p.propertyName)
                .ToList()
        }).ToList();

        return Ok(schema);
    }

    private string GetSimpleTypeName(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(int?)) return "int";
        if (type == typeof(long) || type == typeof(long?)) return "long";
        if (type == typeof(decimal) || type == typeof(decimal?)) return "decimal";
        if (type == typeof(double) || type == typeof(double?)) return "double";
        if (type == typeof(bool) || type == typeof(bool?)) return "bool";
        if (type == typeof(DateTime) || type == typeof(DateTime?)) return "DateTime";
        if (type == typeof(Guid) || type == typeof(Guid?)) return "Guid";
        if (type.IsEnum) return "enum";
        return type.Name;
    }

    private ExcelTemplateDto MapToDto(ExcelTemplate template)
    {
        return new ExcelTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            EntityType = template.EntityType,
            IsActive = template.IsActive,
            SheetName = template.SheetName,
            HasHeader = template.HasHeader,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt ?? template.CreatedAt,
            Columns = template.Columns?.OrderBy(c => c.Order).Select(c => new ExcelColumnDto
            {
                Id = c.Id,
                ColumnName = c.ColumnName,
                PropertyName = c.PropertyName,
                ColumnType = c.ColumnType,
                Order = c.Order,
                IsRequired = c.IsRequired,
                ValidationRules = string.IsNullOrWhiteSpace(c.ValidationRules)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(c.ValidationRules),
                DropdownOptions = string.IsNullOrWhiteSpace(c.DropdownOptions)
                    ? null
                    : JsonSerializer.Deserialize<List<string>>(c.DropdownOptions),
                SampleValue = c.SampleValue,
                Description = c.Description
            }).ToList() ?? new List<ExcelColumnDto>()
        };
    }
}
