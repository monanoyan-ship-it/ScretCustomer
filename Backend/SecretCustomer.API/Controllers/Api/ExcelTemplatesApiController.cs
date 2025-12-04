using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.API.DTOs;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using System.Text.Json;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/excel-templates")]
[Authorize]
public class ExcelTemplatesApiController : ControllerBase
{
    private readonly IExcelTemplateService _excelTemplateService;

    public ExcelTemplatesApiController(IExcelTemplateService excelTemplateService)
    {
        _excelTemplateService = excelTemplateService;
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
    public async Task<ActionResult<ExcelTemplateDto>> GetById(Guid id)
    {
        var template = await _excelTemplateService.GetByIdAsync(id, includeColumns: true);

        if (template == null)
        {
            return NotFound(new { message = "Template not found" });
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
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Description = createDto.Description,
                EntityType = createDto.EntityType,
                SheetName = createDto.SheetName,
                HasHeader = createDto.HasHeader,
                IsActive = true,
                Columns = createDto.Columns.Select(c => new ExcelColumn
                {
                    Id = Guid.NewGuid(),
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
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update Excel template
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ExcelTemplateDto>> Update(
        Guid id,
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
                    Id = c.Id ?? Guid.NewGuid(),
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
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete Excel template (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _excelTemplateService.DeleteAsync(id);

        if (!result)
        {
            return NotFound(new { message = "Template not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Generate and download sample Excel file based on template
    /// </summary>
    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportTemplate(Guid id)
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
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Excel generation failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Upload and parse Excel file based on template
    /// </summary>
    [HttpPost("{id}/import")]
    public async Task<ActionResult<ExcelParseResultDto>> ImportExcel(
        Guid id,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
        {
            return BadRequest(new { message = "Only Excel files (.xlsx, .xls) are allowed" });
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
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Excel parsing failed: {ex.Message}" });
        }
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
