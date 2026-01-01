using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Evaluation;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/evaluations")]
public class EvaluationsApiController : BaseApiController
{
    private readonly IEvaluationService _evaluationService;
    private readonly ILogger<EvaluationsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public EvaluationsApiController(
        IEvaluationService evaluationService,
        ILogger<EvaluationsApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _evaluationService = evaluationService;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Tum degerlendirmeleri getirir (yonetici)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "CanEvaluate")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var evaluations = await _evaluationService.GetAllAsync(page, pageSize);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all evaluations");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.LoadListError"), ex));
        }
    }

    /// <summary>
    /// Degerlendirme detayini getirir
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var evaluation = await _evaluationService.GetByIdAsync(id);
            if (evaluation == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.NotFound")));

            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluation {EvaluationId}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.LoadError"), ex));
        }
    }

    /// <summary>
    /// Atama bazli degerlendirme getirir
    /// </summary>
    [HttpGet("assignment/{assignmentId:int}")]
    [Authorize]
    public async Task<IActionResult> GetByAssignment(int assignmentId)
    {
        try
        {
            var evaluation = await _evaluationService.GetByAssignmentIdAsync(assignmentId);
            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluation for assignment {AssignmentId}", assignmentId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.LoadError"), ex));
        }
    }

    /// <summary>
    /// Proje bazli degerlendirmeleri getirir
    /// </summary>
    [HttpGet("project/{projectId:int}")]
    [Authorize(Policy = "CanEvaluate")]
    public async Task<IActionResult> GetByProject(int projectId)
    {
        try
        {
            var evaluations = await _evaluationService.GetByProjectIdAsync(projectId);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluations for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.LoadListError"), ex));
        }
    }

    /// <summary>
    /// Mevcut kullanicinin degerlendirmelerini getirir
    /// </summary>
    [HttpGet("evaluator")]
    [Authorize]
    public async Task<IActionResult> GetByEvaluator()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.UserNotFound")));
            }

            var evaluations = await _evaluationService.GetByEvaluatorIdAsync(userId);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluations for current user");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.LoadListError"), ex));
        }
    }

    /// <summary>
    /// Degerlendirme formunu yukler (checklist bilgileriyle)
    /// </summary>
    [HttpGet("form/{assignmentId:int}")]
    [Authorize]
    public async Task<IActionResult> GetEvaluationForm(int assignmentId)
    {
        try
        {
            var form = await _evaluationService.GetEvaluationFormAsync(assignmentId);
            if (form == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.AssignmentNotFound")));

            return Ok(form);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluation form for assignment {AssignmentId}", assignmentId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.FormLoadError"), ex));
        }
    }

    /// <summary>
    /// Mevcut degerlendirme formunu yukler (duzenleme icin)
    /// </summary>
    [HttpGet("form/edit/{evaluationId:int}")]
    [Authorize]
    public async Task<IActionResult> GetExistingEvaluationForm(int evaluationId)
    {
        try
        {
            var form = await _evaluationService.GetExistingEvaluationFormAsync(evaluationId);
            if (form == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.NotFound")));

            return Ok(form);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading existing evaluation form {EvaluationId}", evaluationId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.FormLoadError"), ex));
        }
    }

    /// <summary>
    /// Organizasyona gore personel listesi getirir
    /// </summary>
    [HttpGet("personnel-by-org/{organizationId:int}")]
    [Authorize]
    public async Task<IActionResult> GetPersonnelByOrganization(int organizationId)
    {
        try
        {
            var personnel = await _evaluationService.GetPersonnelByOrganizationAsync(organizationId);
            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading personnel for organization {OrganizationId}", organizationId);
            return StatusCode(500, CreateErrorResponse("Personel listesi yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Degerlendirme baslatir
    /// </summary>
    [HttpPost("start")]
    [Authorize]
    public async Task<IActionResult> StartEvaluation([FromBody] StartEvaluationDto dto)
    {
        try
        {
            // Set evaluator from current user if not provided
            if (!dto.EvaluatorId.HasValue)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                {
                    dto.EvaluatorId = userId;
                }
            }

            var evaluation = await _evaluationService.StartEvaluationAsync(dto);
            return Ok(evaluation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting evaluation for assignment {AssignmentId}", dto.AssignmentId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.StartError"), ex));
        }
    }

    /// <summary>
    /// Degerlendirmeyi gonderir (tamamlar)
    /// </summary>
    [HttpPost("submit")]
    [Authorize]
    public async Task<IActionResult> SubmitEvaluation([FromBody] SubmitEvaluationDto dto)
    {
        try
        {
            // Set evaluator from current user if not provided
            if (!dto.EvaluatorId.HasValue)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                {
                    dto.EvaluatorId = userId;
                }
            }

            var evaluation = await _evaluationService.SubmitEvaluationAsync(dto);
            return Ok(new
            {
                message = await _localizationService.GetResourceAsync("Api.Evaluation.SubmitSuccess"),
                evaluation
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting evaluation for assignment {AssignmentId}", dto.AssignmentId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.SubmitError"), ex));
        }
    }

    /// <summary>
    /// Degerlendirmeyi taslak olarak kaydeder
    /// </summary>
    [HttpPost("draft")]
    [Authorize]
    public async Task<IActionResult> SaveDraft([FromBody] SubmitEvaluationDto dto)
    {
        try
        {
            // Set evaluator from current user if not provided
            if (!dto.EvaluatorId.HasValue)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                {
                    dto.EvaluatorId = userId;
                }
            }

            var evaluation = await _evaluationService.SaveDraftAsync(dto);
            return Ok(new
            {
                message = await _localizationService.GetResourceAsync("Api.Evaluation.DraftSaveSuccess"),
                evaluation
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving draft for assignment {AssignmentId}", dto.AssignmentId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.DraftSaveError"), ex));
        }
    }

    /// <summary>
    /// Mevcut taslagi gunceller
    /// </summary>
    [HttpPut("draft")]
    [Authorize]
    public async Task<IActionResult> UpdateDraft([FromBody] UpdateDraftDto dto)
    {
        try
        {
            var evaluation = await _evaluationService.UpdateDraftAsync(dto);
            return Ok(new
            {
                message = await _localizationService.GetResourceAsync("Api.Evaluation.DraftUpdateSuccess"),
                evaluation
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating draft {EvaluationId}", dto.EvaluationId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.DraftUpdateError"), ex));
        }
    }

    /// <summary>
    /// Kapatilmis degerlendirmeyi taslaga alir (Admin yetkisi gerektirir)
    /// Video 2: "Kapatılan Formu Taslağa Alma" özelliği
    /// </summary>
    [HttpPost("{id:int}/revert-to-draft")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> RevertToDraft(int id, [FromBody] RevertToDraftRequest? request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.UserNotFound")));
            }

            var evaluation = await _evaluationService.RevertToDraftAsync(id, userId, request?.Reason);
            return Ok(new
            {
                message = await _localizationService.GetResourceAsync("Api.Evaluation.RevertToDraftSuccess"),
                evaluation
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverting evaluation {EvaluationId} to draft", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.RevertToDraftError"), ex));
        }
    }

    /// <summary>
    /// Degerlendirmeyi iptal eder
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CancelEvaluation(int id, [FromBody] CancelEvaluationRequest? request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.UserNotFound")));
            }

            var evaluation = await _evaluationService.CancelEvaluationAsync(id, userId, request?.Reason);
            return Ok(new
            {
                message = await _localizationService.GetResourceAsync("Api.Evaluation.CancelSuccess"),
                evaluation
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling evaluation {EvaluationId}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.CancelError"), ex));
        }
    }
}

// Request DTOs
public class RevertToDraftRequest
{
    public string? Reason { get; set; }
}

public class CancelEvaluationRequest
{
    public string? Reason { get; set; }
}
