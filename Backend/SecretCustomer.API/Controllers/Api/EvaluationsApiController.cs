using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Evaluation;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/evaluations")]
public class EvaluationsApiController : BaseApiController
{
    private readonly IEvaluationService _evaluationService;
    private readonly ILogger<EvaluationsApiController> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly ApplicationDbContext _context;

    public EvaluationsApiController(
        IEvaluationService evaluationService,
        ILogger<EvaluationsApiController> logger,
        ILocalizationService localizationService,
        ApplicationDbContext context,
        IConfiguration configuration) : base(configuration)
    {
        _evaluationService = evaluationService;
        _logger = logger;
        _localizationService = localizationService;
        _context = context;
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
    /// Degerlendirmenin ekli dosyalarini listeler
    /// </summary>
    [HttpGet("{id:int}/attachments")]
    [Authorize]
    public async Task<IActionResult> GetAttachments(int id)
    {
        try
        {
            var attachments = await _context.Answers
                .Where(a => a.EvaluationId == id && a.AttachmentFileName != null && a.AttachmentFileName != "")
                .Select(a => new
                {
                    answerId = a.Id,
                    questionId = a.QuestionId,
                    questionText = a.Question != null ? a.Question.Text : null,
                    fileName = a.AttachmentFileName
                })
                .ToListAsync();

            return Ok(attachments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading attachments for evaluation {EvaluationId}", id);
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
            var userType = User.FindFirst("UserType")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.UserNotFound")));
            }

            // CustomerPersonnel kullanıcıları için EvaluatorCustomerPersonnelId ile filtrele
            IEnumerable<EvaluationDto> evaluations;
            if (userType == "CustomerPersonnel")
            {
                evaluations = await _evaluationService.GetByEvaluatorCustomerPersonnelIdAsync(userId);
            }
            else
            {
                evaluations = await _evaluationService.GetByEvaluatorIdAsync(userId);
            }

            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluations for current user");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.LoadListError"), ex));
        }
    }

    /// <summary>
    /// Çağrı ID'nin müşteri için mevcut olup olmadığını kontrol eder
    /// </summary>
    [HttpGet("check-call-id")]
    [Authorize]
    public async Task<IActionResult> CheckCallIdExists([FromQuery] string callId, [FromQuery] int assignmentId, [FromQuery] int? evaluationId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(callId))
                return Ok(new { exists = false });

            // Assignment'tan CustomerId'yi al
            var assignment = await _context.Assignments
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted);

            if (assignment?.Project?.CustomerId == null)
                return Ok(new { exists = false });

            var customerId = assignment.Project.CustomerId.Value;

            // Aynı müşteriye ait aynı CallId'li başka dinleme var mı?
            var exists = await _context.Evaluations
                .AnyAsync(e => !e.IsDeleted &&
                              e.CallId == callId &&
                              e.Assignment.Project.CustomerId == customerId &&
                              (!evaluationId.HasValue || e.Id != evaluationId.Value));

            return Ok(new { exists = exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking CallId existence");
            return StatusCode(500, CreateErrorResponse("CallId kontrol hatası", ex));
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
            // Aktif proje kontrolü
            var assignment = await _context.Assignments
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId);

            if (assignment == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.NotFound")));
            }

            if (assignment.Project.StatusId != ProjectStatuses.Ids.Active)
            {
                return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.ProjectNotActive")));
            }

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
    /// Puan hesapla - Tek merkezi hesaplama noktası
    /// Kaydetmez, sadece hesaplayıp sonucu döndürür
    /// Frontend'den canlı puan önizlemesi için kullanılır
    /// </summary>
    [HttpPost("calculate-score")]
    [Authorize]
    public async Task<IActionResult> CalculateScore([FromBody] CalculateScoreRequestDto request)
    {
        try
        {
            var result = await _evaluationService.CalculateScoreAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating score for checklist {ChecklistId}", request.ChecklistId);
            return StatusCode(500, CreateErrorResponse("Puan hesaplanırken hata oluştu", ex));
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
            // Personel kontrolü - en az biri dolu olmalı
            if (!HasEvaluatedPersonnel(dto))
            {
                return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.PersonnelRequired")));
            }

            // Set evaluator from current user if not provided
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                // CustomerPersonnel kullanıcıları için EvaluatorCustomerPersonnelId kullan
                if (userType == "CustomerPersonnel")
                {
                    dto.EvaluatorCustomerPersonnelId = userId;
                    dto.EvaluatorId = null; // Users tablosunda yok
                }
                else if (!dto.EvaluatorId.HasValue)
                {
                    dto.EvaluatorId = userId;
                }
            }

            var evaluation = await _evaluationService.SubmitEvaluationAsync(dto);

            // Answer ID'leri frontend'e dön (dosya yükleme için gerekli)
            var answers = await _context.Answers
                .Where(a => a.EvaluationId == evaluation.Id)
                .Select(a => new { a.Id, a.QuestionId })
                .ToListAsync();

            return Ok(new
            {
                message = await _localizationService.GetResourceAsync("Api.Evaluation.SubmitSuccess"),
                evaluation,
                answers
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
            // Personel kontrolü - en az biri dolu olmalı
            if (!HasEvaluatedPersonnel(dto))
            {
                return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.PersonnelRequired")));
            }

            // Set evaluator from current user if not provided
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                // CustomerPersonnel kullanıcıları için EvaluatorCustomerPersonnelId kullan
                if (userType == "CustomerPersonnel")
                {
                    dto.EvaluatorCustomerPersonnelId = userId;
                    dto.EvaluatorId = null; // Users tablosunda yok
                }
                else if (!dto.EvaluatorId.HasValue)
                {
                    dto.EvaluatorId = userId;
                }
            }

            var evaluation = await _evaluationService.SaveDraftAsync(dto);

            // Answer ID'leri frontend'e dön (dosya yükleme için gerekli)
            var answers = await _context.Answers
                .Where(a => a.EvaluationId == evaluation.Id)
                .Select(a => new { a.Id, a.QuestionId })
                .ToListAsync();

            return Ok(new
            {
                message = await _localizationService.GetResourceAsync("Api.Evaluation.DraftSaveSuccess"),
                evaluation,
                answers
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

    /// <summary>
    /// Taslağa alma talebi gönderir (kullanıcı tarafından)
    /// Tamamlanmış değerlendirmeler için admin onayı gerektirir
    /// </summary>
    [HttpPost("{id:int}/request-revert")]
    [Authorize]
    public async Task<IActionResult> RequestRevertToDraft(int id, [FromBody] RequestRevertDto? dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse("Kullanıcı bulunamadı"));
            }

            // 1. Değerlendirmeyi kontrol et (Assignment include etmiyoruz çünkü silinmiş olabilir)
            var evaluation = await _context.Evaluations
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

            if (evaluation == null)
            {
                return NotFound(CreateErrorResponse("Değerlendirme bulunamadı"));
            }

            // Sadece Completed durumundaki değerlendirmeler için talep gönderilebilir
            if (evaluation.StatusId != EvaluationStatuses.Ids.Completed)
            {
                return BadRequest(CreateErrorResponse("Sadece tamamlanmış değerlendirmeler için taslağa alma talebi gönderilebilir."));
            }

            // 2. Zaten bekleyen talep var mı kontrol et
            var existingRequest = await _context.Approvals
                .AnyAsync(a => a.ApprovalTypeId == ApprovalTypes.Ids.Evaluation
                            && a.RelatedEntityId == id
                            && a.RelatedEntityType == "EvaluationRevert"
                            && a.StatusId == ApprovalStatuses.Ids.Pending);

            if (existingRequest)
            {
                return BadRequest(CreateErrorResponse("Bu değerlendirme için zaten bekleyen bir taslağa alma talebi var."));
            }

            // 3. Referans numarası oluştur
            var year = DateTime.UtcNow.Year;
            var count = await _context.Approvals.CountAsync(a => a.CreatedAt.Year == year) + 1;
            var referenceNumber = $"REV-{year}-{count:D4}";

            // 4. Approval kaydı oluştur
            var approval = new Approval
            {
                ReferenceNumber = referenceNumber,
                ApprovalTypeId = ApprovalTypes.Ids.Evaluation,
                StatusId = ApprovalStatuses.Ids.Pending,
                Title = $"Taslağa Alma Talebi - Değerlendirme #{id}",
                Description = dto?.Reason ?? "Neden belirtilmedi",
                RelatedEntityId = id,
                RelatedEntityType = "EvaluationRevert", // Taslağa alma talebi olduğunu belirtmek için
                RequestedByUserId = userId,
                RequestedAt = DateTime.UtcNow,
                PriorityId = NotificationPriorities.Ids.Normal
            };

            _context.Approvals.Add(approval);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Taslağa alma talebi oluşturuldu: EvaluationId={EvaluationId}, ApprovalId={ApprovalId}, UserId={UserId}",
                id, approval.Id, userId);

            return Ok(new
            {
                message = "Taslağa alma talebi gönderildi. Admin onayı bekleniyor.",
                approvalId = approval.Id,
                referenceNumber = approval.ReferenceNumber
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating revert request for evaluation {EvaluationId}", id);
            return StatusCode(500, CreateErrorResponse("Talep oluşturulurken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Değerlendirilen personel bilgisi var mı kontrol eder
    /// EvaluatedPersonnelId, EvaluatedCustomerPersonnelId, EvaluatedUnknownPersonnel veya NewPersonnel'den biri dolu olmalı
    /// </summary>
    private static bool HasEvaluatedPersonnel(SubmitEvaluationDto dto)
    {
        return dto.EvaluatedPersonnelId.HasValue && dto.EvaluatedPersonnelId > 0
            || dto.EvaluatedCustomerPersonnelId.HasValue && dto.EvaluatedCustomerPersonnelId > 0
            || !string.IsNullOrWhiteSpace(dto.EvaluatedUnknownPersonnel)
            || (dto.NewPersonnel != null && !string.IsNullOrWhiteSpace(dto.NewPersonnel.FirstName));
    }

    /// <summary>
    /// Tamamlanmış değerlendirmelerin puanlarını yeniden hesaplar (Admin only)
    /// Eski bug nedeniyle 0 kalan puanları düzeltmek için kullanılır
    /// </summary>
    [HttpPost("recalculate-scores")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RecalculateScores([FromQuery] int? projectId = null)
    {
        try
        {
            var query = _context.Evaluations
                .Include(e => e.Assignment)
                    .ThenInclude(a => a.Checklist)
                        .ThenInclude(c => c!.Questions)
                .Include(e => e.Answers)
                .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed);

            if (projectId.HasValue)
                query = query.Where(e => e.Assignment.ProjectId == projectId.Value);

            var evaluations = await query.ToListAsync();
            int updatedCount = 0;
            int errorCount = 0;

            foreach (var evaluation in evaluations)
            {
                try
                {
                    var result = await _evaluationService.RecalculateScoreAsync(evaluation.Id);
                    if (result.Success)
                        updatedCount++;
                    else
                        errorCount++;
                }
                catch
                {
                    errorCount++;
                }
            }

            return Ok(new {
                message = $"{updatedCount} değerlendirme güncellendi, {errorCount} hata oluştu.",
                updatedCount,
                errorCount,
                totalProcessed = evaluations.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating scores");
            return StatusCode(500, CreateErrorResponse("Puanlar yeniden hesaplanırken hata oluştu.", ex));
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

public class RequestRevertDto
{
    public string? Reason { get; set; }
}
