using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Checklist;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/checklists")]
[Authorize]
public class ChecklistsApiController : BaseApiController
{
    private readonly IChecklistService _checklistService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;

    public ChecklistsApiController(
        IChecklistService checklistService,
        IAuditLogService auditLogService,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _checklistService = checklistService;
        _auditLogService = auditLogService;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Get all checklists with optional filtering - Admin, QualitySpecialist, and Evaluator can access
    /// Optimize edilmiş liste - Questions yüklemez
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector,Evaluator")]
    public async Task<IActionResult> GetAll([FromQuery] ChecklistFilterDto filter)
    {
        try
        {
            var checklists = await _checklistService.GetListAsync(filter);
            return Ok(checklists);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading checklists", "Checklists", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.LoadListError"), ex));
        }
    }

    /// <summary>
    /// Get checklist by ID - Admin, QualitySpecialist, and Evaluator can access
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector,Evaluator")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            await _auditLogService.LogInfoAsync($"Loading checklist {id}", "Checklists");
            var checklist = await _checklistService.GetByIdAsync(id);

            if (checklist == null)
            {
                await _auditLogService.LogWarningAsync($"Checklist {id} not found", "Checklists");
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.NotFound")));
            }

            await _auditLogService.LogInfoAsync($"Successfully loaded checklist {id} with {checklist.Questions?.Count ?? 0} questions", "Checklists");

            return Ok(checklist);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading checklist {id}. Exception: {ex.Message}, StackTrace: {ex.StackTrace}", "Checklists", ex);

            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.LoadError"), ex));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateChecklistDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var checklist = await _checklistService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = checklist.Id }, checklist);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error creating checklist", "Checklists", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.CreateError"), ex));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateChecklistDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Debug: Gelen veriyi logla
            await _auditLogService.LogInfoAsync($"UPDATE Checklist {id} - Questions: {dto.Questions?.Count ?? 0}", "Checklists");
            foreach (var q in dto.Questions ?? new List<UpdateQuestionDto>())
            {
                await _auditLogService.LogInfoAsync($"  Question: Id={q.Id}, Text={q.Text?.Substring(0, Math.Min(50, q.Text?.Length ?? 0))}, ShowScoreInput={q.ShowScoreInput}, SelectionTypeId={q.SelectionTypeId}", "Checklists");
            }

            dto.Id = id;
            var checklist = await _checklistService.UpdateAsync(dto);
            if (checklist == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.NotFound")));
            }

            return Ok(checklist);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            // Detaylı hata mesajı
            var entries = ex.Entries.Select(e => new {
                Entity = e.Entity.GetType().Name,
                State = e.State.ToString(),
                Id = e.Property("Id")?.CurrentValue?.ToString() ?? "null"
            }).ToList();

            await _auditLogService.LogErrorAsync($"Concurrency error updating checklist {id}. Affected entities: {string.Join(", ", entries.Select(e => $"{e.Entity}({e.Id})"))} ", "Checklists", ex);

            return StatusCode(500, new {
                message = "Veritabanı güncelleme hatası",
                error = ex.Message,
                affectedEntities = entries,
                hint = "Muhtemelen güncellenmeye çalışılan kayıt veritabanında yok veya silinmiş."
            });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating checklist {id}", "Checklists", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.UpdateError"), ex));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _checklistService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.NotFound")));
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting checklist {id}", "Checklists", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.DeleteError"), ex));
        }
    }

    [HttpPost("{id}/clone")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Clone(int id, [FromBody] string newName)
    {
        try
        {
            var checklist = await _checklistService.CloneChecklistAsync(id, newName);
            if (checklist == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.NotFound")));
            }

            return CreatedAtAction(nameof(GetById), new { id = checklist.Id }, checklist);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error cloning checklist {id}", "Checklists", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.CloneError"), ex));
        }
    }

    /// <summary>
    /// Belirli bir kontrol listesinin soru gruplarını getir (autocomplete için)
    /// </summary>
    [HttpGet("{id}/question-groups")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector,Evaluator")]
    public async Task<IActionResult> GetQuestionGroups(int id)
    {
        try
        {
            var groups = await _checklistService.GetQuestionGroupsAsync(id);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading question groups for checklist {id}", "Checklists", ex);
            return StatusCode(500, CreateErrorResponse("Soru grupları yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Kontrol listesini Excel olarak dışa aktar (sorular ve alt kriterler dahil)
    /// </summary>
    [HttpGet("{id}/export/excel")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> ExportToExcel(int id)
    {
        try
        {
            var result = await _checklistService.ExportChecklistToExcelAsync(id);
            if (result == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.NotFound")));
            }

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error exporting checklist {id} to Excel", "Checklists", ex);
            return StatusCode(500, CreateErrorResponse("Kontrol listesi dışa aktarılırken hata oluştu", ex));
        }
    }
}
