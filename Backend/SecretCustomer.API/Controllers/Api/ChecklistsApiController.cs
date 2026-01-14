using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Checklist;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/checklists")]
[Authorize]
public class ChecklistsApiController : BaseApiController
{
    private readonly IChecklistService _checklistService;
    private readonly ILogger<ChecklistsApiController> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly ApplicationDbContext _context;

    public ChecklistsApiController(
        IChecklistService checklistService,
        ILogger<ChecklistsApiController> logger,
        ILocalizationService localizationService,
        ApplicationDbContext context,
        IConfiguration configuration) : base(configuration)
    {
        _checklistService = checklistService;
        _logger = logger;
        _localizationService = localizationService;
        _context = context;
    }

    /// <summary>
    /// Get all checklists with optional filtering - Admin, TeamLeader, and Evaluator can access
    /// Optimize edilmiş liste - Questions yüklemez
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,TeamLeader,Evaluator")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int? customerId = null,
        [FromQuery] int? customerOrganizationId = null,
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            // Optimize edilmiş liste methodu kullan (Questions yüklemez)
            var checklists = await _checklistService.GetListAsync(search, customerId, customerOrganizationId, includeInactive);
            return Ok(checklists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading checklists");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.LoadListError"), ex));
        }
    }

    /// <summary>
    /// Get checklist by ID - Admin, TeamLeader, and Evaluator can access
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,TeamLeader,Evaluator")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            _logger.LogInformation("Loading checklist {Id}", id);
            var checklist = await _checklistService.GetByIdAsync(id);

            if (checklist == null)
            {
                _logger.LogWarning("Checklist {Id} not found", id);
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.NotFound")));
            }

            _logger.LogInformation("Successfully loaded checklist {Id} with {QuestionCount} questions",
                id, checklist.Questions?.Count ?? 0);

            return Ok(checklist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading checklist {Id}. Exception: {Message}, StackTrace: {StackTrace}",
                id, ex.Message, ex.StackTrace);

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
            _logger.LogError(ex, "Error creating checklist");
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
            _logger.LogInformation("UPDATE Checklist {Id} - Questions: {QuestionCount}", id, dto.Questions?.Count ?? 0);
            foreach (var q in dto.Questions ?? new List<UpdateQuestionDto>())
            {
                _logger.LogInformation("  Question: Id={QuestionId}, Text={Text}, SubCriteria={SubCriteriaCount}",
                    q.Id, q.Text?.Substring(0, Math.Min(50, q.Text?.Length ?? 0)), q.SubCriteria?.Count ?? 0);
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

            _logger.LogError(ex, "Concurrency error updating checklist {Id}. Affected entities: {@Entries}", id, entries);

            return StatusCode(500, new {
                message = "Veritabanı güncelleme hatası",
                error = ex.Message,
                affectedEntities = entries,
                hint = "Muhtemelen güncellenmeye çalışılan kayıt veritabanında yok veya silinmiş."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating checklist {Id}", id);
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
            _logger.LogError(ex, "Error deleting checklist {Id}", id);
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
            _logger.LogError(ex, "Error cloning checklist {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.CloneError"), ex));
        }
    }

    /// <summary>
    /// Belirli bir kontrol listesinin soru gruplarını getir (autocomplete için)
    /// </summary>
    [HttpGet("{id}/question-groups")]
    [Authorize(Roles = "Admin,TeamLeader,Evaluator")]
    public async Task<IActionResult> GetQuestionGroups(int id)
    {
        try
        {
            var groups = await _context.Questions
                .Where(q => q.ChecklistId == id && !q.IsDeleted && !string.IsNullOrEmpty(q.GroupName))
                .Select(q => q.GroupName)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading question groups for checklist {Id}", id);
            return StatusCode(500, CreateErrorResponse("Soru grupları yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Kontrol listesini Excel olarak dışa aktar (sorular ve alt kriterler dahil)
    /// </summary>
    [HttpGet("{id}/export/excel")]
    [Authorize(Roles = "Admin,TeamLeader")]
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
            _logger.LogError(ex, "Error exporting checklist {Id} to Excel", id);
            return StatusCode(500, CreateErrorResponse("Kontrol listesi dışa aktarılırken hata oluştu", ex));
        }
    }
}
