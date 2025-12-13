using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Evaluation;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/evaluations")]
public class EvaluationsApiController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;
    private readonly ILogger<EvaluationsApiController> _logger;

    public EvaluationsApiController(
        IEvaluationService evaluationService,
        ILogger<EvaluationsApiController> logger)
    {
        _evaluationService = evaluationService;
        _logger = logger;
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
            return StatusCode(500, new { message = "Degerlendirmeler yuklenirken bir hata olustu." });
        }
    }

    /// <summary>
    /// Degerlendirme detayini getirir
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var evaluation = await _evaluationService.GetByIdAsync(id);
            if (evaluation == null)
                return NotFound(new { message = "Degerlendirme bulunamadi." });

            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluation {EvaluationId}", id);
            return StatusCode(500, new { message = "Degerlendirme yuklenirken bir hata olustu." });
        }
    }

    /// <summary>
    /// Atama bazli degerlendirme getirir
    /// </summary>
    [HttpGet("assignment/{assignmentId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetByAssignment(Guid assignmentId)
    {
        try
        {
            var evaluation = await _evaluationService.GetByAssignmentIdAsync(assignmentId);
            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluation for assignment {AssignmentId}", assignmentId);
            return StatusCode(500, new { message = "Degerlendirme yuklenirken bir hata olustu." });
        }
    }

    /// <summary>
    /// Proje bazli degerlendirmeleri getirir
    /// </summary>
    [HttpGet("project/{projectId:guid}")]
    [Authorize(Policy = "CanEvaluate")]
    public async Task<IActionResult> GetByProject(Guid projectId)
    {
        try
        {
            var evaluations = await _evaluationService.GetByProjectIdAsync(projectId);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluations for project {ProjectId}", projectId);
            return StatusCode(500, new { message = "Degerlendirmeler yuklenirken bir hata olustu." });
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
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanici bilgisi bulunamadi." });
            }

            var evaluations = await _evaluationService.GetByEvaluatorIdAsync(userId);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluations for current user");
            return StatusCode(500, new { message = "Degerlendirmeler yuklenirken bir hata olustu." });
        }
    }

    /// <summary>
    /// Degerlendirme formunu yukler (checklist bilgileriyle)
    /// </summary>
    [HttpGet("form/{assignmentId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetEvaluationForm(Guid assignmentId)
    {
        try
        {
            var form = await _evaluationService.GetEvaluationFormAsync(assignmentId);
            if (form == null)
                return NotFound(new { message = "Atama bulunamadi." });

            return Ok(form);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluation form for assignment {AssignmentId}", assignmentId);
            return StatusCode(500, new { message = "Degerlendirme formu yuklenirken bir hata olustu." });
        }
    }

    /// <summary>
    /// Mevcut degerlendirme formunu yukler (duzenleme icin)
    /// </summary>
    [HttpGet("form/edit/{evaluationId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetExistingEvaluationForm(Guid evaluationId)
    {
        try
        {
            var form = await _evaluationService.GetExistingEvaluationFormAsync(evaluationId);
            if (form == null)
                return NotFound(new { message = "Degerlendirme bulunamadi." });

            return Ok(form);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading existing evaluation form {EvaluationId}", evaluationId);
            return StatusCode(500, new { message = "Degerlendirme formu yuklenirken bir hata olustu." });
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
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    dto.EvaluatorId = userId;
                }
            }

            var evaluation = await _evaluationService.StartEvaluationAsync(dto);
            return Ok(evaluation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting evaluation for assignment {AssignmentId}", dto.AssignmentId);
            return StatusCode(500, new { message = "Degerlendirme baslatilirken bir hata olustu." });
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
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    dto.EvaluatorId = userId;
                }
            }

            var evaluation = await _evaluationService.SubmitEvaluationAsync(dto);
            return Ok(new
            {
                message = "Degerlendirme basariyla tamamlandi.",
                evaluation
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting evaluation for assignment {AssignmentId}", dto.AssignmentId);
            return StatusCode(500, new { message = "Degerlendirme gonderilirken bir hata olustu." });
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
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    dto.EvaluatorId = userId;
                }
            }

            var evaluation = await _evaluationService.SaveDraftAsync(dto);
            return Ok(new
            {
                message = "Taslak basariyla kaydedildi.",
                evaluation
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving draft for assignment {AssignmentId}", dto.AssignmentId);
            return StatusCode(500, new { message = "Taslak kaydedilirken bir hata olustu." });
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
                message = "Taslak basariyla guncellendi.",
                evaluation
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating draft {EvaluationId}", dto.EvaluationId);
            return StatusCode(500, new { message = "Taslak guncellenirken bir hata olustu." });
        }
    }

    /// <summary>
    /// Kapatilmis degerlendirmeyi taslaga alir (Admin yetkisi gerektirir)
    /// Video 2: "Kapatılan Formu Taslağa Alma" özelliği
    /// </summary>
    [HttpPost("{id:guid}/revert-to-draft")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> RevertToDraft(Guid id, [FromBody] RevertToDraftRequest? request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanici bilgisi bulunamadi." });
            }

            var evaluation = await _evaluationService.RevertToDraftAsync(id, userId, request?.Reason);
            return Ok(new
            {
                message = "Degerlendirme basariyla taslaga alindi.",
                evaluation
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverting evaluation {EvaluationId} to draft", id);
            return StatusCode(500, new { message = "Degerlendirme taslaga alinirken bir hata olustu." });
        }
    }

    /// <summary>
    /// Degerlendirmeyi iptal eder
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CancelEvaluation(Guid id, [FromBody] CancelEvaluationRequest? request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanici bilgisi bulunamadi." });
            }

            var evaluation = await _evaluationService.CancelEvaluationAsync(id, userId, request?.Reason);
            return Ok(new
            {
                message = "Degerlendirme basariyla iptal edildi.",
                evaluation
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling evaluation {EvaluationId}", id);
            return StatusCode(500, new { message = "Degerlendirme iptal edilirken bir hata olustu." });
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
