using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Evaluation;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/evaluations")]
public class EvaluationsApiController : BaseApiController
{
    private readonly IEvaluationService _evaluationService;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<EvaluationsApiController> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly ApplicationDbContext _context;

    public EvaluationsApiController(
        IEvaluationService evaluationService,
        IFileUploadService fileUploadService,
        ILogger<EvaluationsApiController> logger,
        ILocalizationService localizationService,
        ApplicationDbContext context,
        IConfiguration configuration) : base(configuration)
    {
        _evaluationService = evaluationService;
        _fileUploadService = fileUploadService;
        _logger = logger;
        _localizationService = localizationService;
        _context = context;
    }

    [HttpGet("past-descriptions")]
    [Authorize]
    public async Task<IActionResult> GetPastDescriptions()
    {
        var descriptions = await _evaluationService.GetPastDescriptionsAsync();
        return Ok(descriptions);
    }

    /// <summary>
    /// Tum degerlendirmeleri getirir (yonetici) - Çoklu filtre desteği
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "CanEvaluate")]
    public async Task<IActionResult> GetAll(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? customerIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] List<int>? evaluatorIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] List<string>? statuses,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.Evaluations
                .Include(e => e.Project)
                .Include(e => e.Project).ThenInclude(p => p.Checklist)
                .Include(e => e.EvaluatedCustomerPersonnel)
                .Include(e => e.Evaluator)
                .Where(e => !e.IsDeleted);

            // Çoklu filtreler
            if (projectIds?.Any() == true)
                query = query.Where(e => projectIds.Contains(e.ProjectId));

            if (customerIds?.Any() == true)
                query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                    customerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

            if (organizationIds?.Any() == true)
                query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                    e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa =>
                        organizationIds.Contains(oa.CustomerOrganizationId)));

            if (evaluatorIds?.Any() == true)
                query = query.Where(e => e.EvaluatorId.HasValue && evaluatorIds.Contains(e.EvaluatorId.Value));

            if (checklistIds?.Any() == true)
                query = query.Where(e => checklistIds.Contains(e.Project.ChecklistId));

            if (statuses?.Any() == true)
            {
                var statusIds = statuses
                    .Select(s => EvaluationStatuses.GetBySystemName(s))
                    .Where(s => s != null)
                    .Select(s => s!.Id)
                    .ToList();
                if (statusIds.Any())
                    query = query.Where(e => statusIds.Contains(e.StatusId));
            }

            if (startDate.HasValue)
            {
                var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(e => e.CreatedAt >= start);
            }

            if (endDate.HasValue)
            {
                var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(e => e.CreatedAt <= end);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    e.Id,
                    e.StatusId,
                    Status = EvaluationStatuses.GetById(e.StatusId)!.SystemName,
                    ProjectName = e.Project.Name,
                    ChecklistName = e.Project.Checklist.Name,
                    EvaluatorName = e.Evaluator != null ? e.Evaluator.FirstName + " " + e.Evaluator.LastName : null,
                    PersonnelName = e.EvaluatedCustomerPersonnel != null
                        ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                        : e.EvaluatedUnknownPersonnel,
                    e.TotalScore,
                    e.MaxScore,
                    e.ScorePercentage,
                    e.CompletedAt,
                    e.CreatedAt
                })
                .ToListAsync();

            return Ok(new { items, totalCount, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all evaluations");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.LoadError"), ex));
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.NotFound")));

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
            var attachments = await _context.EvaluationAttachments
                .Where(a => a.EvaluationId == id && !a.IsDeleted)
                .Select(a => new
                {
                    id = a.Id,
                    fileName = a.FileName,
                    fileSize = a.FileSize,
                    contentType = a.ContentType,
                    uploadedAt = a.CreatedAt
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
    /// Değerlendirmeye dosya ekler
    /// </summary>
    [HttpPost("{id:int}/attachments")]
    [Authorize]
    [RequestSizeLimit(52428800)] // 50 MB
    public async Task<IActionResult> UploadAttachment(int id, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotSelected")));
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            using var stream = file.OpenReadStream();
            var result = await _fileUploadService.UploadEvaluationAttachmentAsync(
                id,
                stream,
                file.FileName,
                file.ContentType,
                userId);

            if (!result.Success)
            {
                return BadRequest(CreateErrorResponse(result.ErrorMessage ?? "Upload failed"));
            }

            return Ok(new
            {
                message = await _localizationService.GetResourceAsync("Api.Common.FileUploadSuccess"),
                attachmentId = result.AttachmentId,
                fileName = result.FileName,
                fileSize = result.FileSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading attachment for evaluation {EvaluationId}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Answer.UploadError"), ex));
        }
    }

    /// <summary>
    /// Değerlendirme ekini siler
    /// </summary>
    [HttpDelete("attachments/{attachmentId:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteAttachment(int attachmentId)
    {
        try
        {
            var success = await _fileUploadService.DeleteEvaluationAttachmentAsync(attachmentId);
            if (!success)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Common.FileDeleteSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {AttachmentId}", attachmentId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Answer.DeleteError"), ex));
        }
    }

    /// <summary>
    /// Değerlendirme ekini indirir
    /// </summary>
    [HttpGet("attachments/{attachmentId:int}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadAttachment(int attachmentId)
    {
        try
        {
            var result = await _fileUploadService.GetEvaluationAttachmentAsync(attachmentId);
            if (result == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            var (fileStream, fileName, contentType) = result.Value;
            return File(fileStream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading attachment {AttachmentId}", attachmentId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Answer.DownloadError"), ex));
        }
    }

    /// <summary>
    /// Proje bazli tek degerlendirme getirir
    /// </summary>
    [HttpGet("by-project/{projectId:int}")]
    [Authorize]
    public async Task<IActionResult> GetByProjectSingle(int projectId)
    {
        try
        {
            var evaluation = await _evaluationService.GetByProjectIdSingleAsync(projectId);
            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluation for project {ProjectId}", projectId);
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.LoadError"), ex));
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
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Auth.UserNotFound")));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.LoadError"), ex));
        }
    }

    /// <summary>
    /// Temsilcinin kendisi hakkındaki değerlendirmeleri getirir
    /// CustomerOperator'ın kendi performansını görmesi için kullanılır
    /// </summary>
    [HttpGet("my-evaluations")]
    [Authorize(Roles = "CustomerOperator,CustomerSupervisor,CustomerManager,Admin")]
    public async Task<IActionResult> GetMyEvaluations()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Auth.UserNotFound")));
            }

            // Sadece CustomerPersonnel kullanıcıları için çalışır
            if (userType != "CustomerPersonnel")
            {
                return BadRequest(CreateErrorResponse("Bu endpoint sadece müşteri personeli için kullanılabilir."));
            }

            var evaluations = await _evaluationService.GetByEvaluatedCustomerPersonnelIdAsync(userId);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading my evaluations for current user");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.LoadError"), ex));
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
                              e.Project.CustomerId == customerId &&
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.NotFound")));

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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.NotFound")));

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
                .FirstOrDefaultAsync(a => a.ProjectId == dto.ProjectId);

            if (assignment == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.NotFound")));
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
            _logger.LogError(ex, "Error starting evaluation for project {ProjectId}", dto.ProjectId);
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
                message = await _localizationService.GetResourceAsync("Evaluation.SubmitSuccess"),
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
            _logger.LogError(ex, "Error submitting evaluation for project {ProjectId}", dto.ProjectId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.SubmitError"), ex));
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
                message = await _localizationService.GetResourceAsync("Evaluation.DraftSaved"),
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
            _logger.LogError(ex, "Error saving draft for project {ProjectId}", dto.ProjectId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.DraftSaveError"), ex));
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
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Auth.UserNotFound")));
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
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Auth.UserNotFound")));
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
            var userType = User.FindFirst("UserType")?.Value;
            var approval = new Approval
            {
                ReferenceNumber = referenceNumber,
                ApprovalTypeId = ApprovalTypes.Ids.Evaluation,
                StatusId = ApprovalStatuses.Ids.Pending,
                Title = $"Taslağa Alma Talebi - Değerlendirme #{id}",
                Description = dto?.Reason ?? "Neden belirtilmedi",
                RelatedEntityId = id,
                RelatedEntityType = "EvaluationRevert",
                RequestedByUserId = userType == "CustomerPersonnel" ? null : userId,
                RequestedByCustomerPersonnelId = userType == "CustomerPersonnel" ? userId : null,
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
    /// Durum ID'sinden Türkçe durum adını döndürür
    /// </summary>
    private static string GetStatusDisplayName(int statusId)
    {
        return statusId switch
        {
            EvaluationStatuses.Ids.Pending => "Beklemede",
            EvaluationStatuses.Ids.InProgress => "Devam Ediyor",
            EvaluationStatuses.Ids.Completed => "Tamamlandı",
            EvaluationStatuses.Ids.Draft => "Taslak",
            EvaluationStatuses.Ids.Cancelled => "İptal Edildi",
            _ => "-"
        };
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
                .Include(e => e.Project)
                    .ThenInclude(p => p.Checklist)
                        .ThenInclude(c => c!.Questions)
                .Include(e => e.Answers)
                .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed);

            if (projectId.HasValue)
                query = query.Where(e => e.ProjectId == projectId.Value);

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

    /// <summary>
    /// Değerlendirmeleri Excel'e aktarır - Çoklu filtre desteği
    /// </summary>
    [HttpGet("export")]
    [Authorize]
    public async Task<IActionResult> ExportToExcel(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? customerIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] List<int>? evaluatorIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] List<string>? statuses,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? personnel = null,
        [FromQuery] string? projectName = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] DateTime? controlStartDate = null,
        [FromQuery] DateTime? controlEndDate = null)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Auth.UserNotFound")));
            }

            // Kullanıcının erişebildiği değerlendirmeleri getir
            IQueryable<Evaluation> query;
            if (userType == "CustomerPersonnel")
            {
                query = _context.Evaluations
                    .Include(e => e.Project).ThenInclude(p => p.Checklist)
                    .Include(e => e.EvaluatedPersonnel)
                    .Include(e => e.EvaluatedCustomerPersonnel)
                    .Where(e => !e.IsDeleted && e.EvaluatorCustomerPersonnelId == userId);
            }
            else
            {
                query = _context.Evaluations
                    .Include(e => e.Project).ThenInclude(p => p.Checklist)
                    .Include(e => e.EvaluatedPersonnel)
                    .Include(e => e.EvaluatedCustomerPersonnel)
                    .Where(e => !e.IsDeleted && e.EvaluatorId == userId);
            }

            // Çoklu filtreler (OR mantığı)
            if (projectIds?.Any() == true)
                query = query.Where(e => projectIds.Contains(e.ProjectId));

            if (customerIds?.Any() == true)
                query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                    customerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

            if (organizationIds?.Any() == true)
                query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                    e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa =>
                        organizationIds.Contains(oa.CustomerOrganizationId)));

            if (evaluatorIds?.Any() == true)
                query = query.Where(e => e.EvaluatorId.HasValue && evaluatorIds.Contains(e.EvaluatorId.Value));

            if (checklistIds?.Any() == true)
                query = query.Where(e => checklistIds.Contains(e.Project.ChecklistId));

            // Çoklu status filtresi
            if (statuses?.Any() == true)
            {
                var statusIds = statuses
                    .Select(s => EvaluationStatuses.GetBySystemName(s))
                    .Where(s => s != null)
                    .Select(s => s!.Id)
                    .ToList();
                if (statusIds.Any())
                    query = query.Where(e => statusIds.Contains(e.StatusId));
            }
            // Tekil status filtresi (geriye uyumluluk)
            else if (!string.IsNullOrEmpty(status))
            {
                var statusType = EvaluationStatuses.GetBySystemName(status);
                if (statusType != null)
                {
                    query = query.Where(e => e.StatusId == statusType.Id);
                }
            }

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(e =>
                    (e.Project != null && e.Project.Name.ToLower().Contains(searchLower)) ||
                    (e.Project.Checklist != null && e.Project.Checklist.Name.ToLower().Contains(searchLower)) ||
                    (e.EvaluatedPersonnel != null && (e.EvaluatedPersonnel.FirstName + " " + e.EvaluatedPersonnel.LastName).ToLower().Contains(searchLower)) ||
                    (e.EvaluatedUnknownPersonnel != null && e.EvaluatedUnknownPersonnel.ToLower().Contains(searchLower)) ||
                    (e.CallId != null && e.CallId.ToLower().Contains(searchLower)));
            }

            if (!string.IsNullOrEmpty(personnel))
            {
                var personnelLower = personnel.ToLower();
                query = query.Where(e =>
                    (e.EvaluatedPersonnel != null && (e.EvaluatedPersonnel.FirstName + " " + e.EvaluatedPersonnel.LastName).ToLower().Contains(personnelLower)) ||
                    (e.EvaluatedCustomerPersonnel != null && (e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName).ToLower().Contains(personnelLower)) ||
                    (e.EvaluatedUnknownPersonnel != null && e.EvaluatedUnknownPersonnel.ToLower().Contains(personnelLower)));
            }

            // Proje adı filtresi
            if (!string.IsNullOrEmpty(projectName))
            {
                query = query.Where(e => e.Project != null && e.Project.Name == projectName);
            }

            // Çağrı tarihi filtresi (CallDate)
            if (startDate.HasValue)
            {
                var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(e => e.CallDate.HasValue && e.CallDate.Value >= start);
            }
            if (endDate.HasValue)
            {
                var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(e => e.CallDate.HasValue && e.CallDate.Value <= end);
            }

            // Değerlendirme tarihi filtresi (CreatedAt)
            if (controlStartDate.HasValue)
            {
                var cStart = DateTime.SpecifyKind(controlStartDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(e => e.CreatedAt >= cStart);
            }
            if (controlEndDate.HasValue)
            {
                var cEnd = DateTime.SpecifyKind(controlEndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(e => e.CreatedAt <= cEnd);
            }

            var evaluations = await query
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            // Excel oluştur
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Dinlemeler");

            // Başlık satırı
            var headers = new[] { "Çağrı ID", "Çağrı Tarihi", "Çağrı Saati", "Süre", "Personel", "Proje", "Kontrol Listesi", "Puan (%)", "Sarı Kart", "Kırmızı Kart", "Durum", "Kontrol Tarihi" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            // Başlık stili
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

            // Veri satırları
            for (int row = 0; row < evaluations.Count; row++)
            {
                var e = evaluations[row];
                var currentRow = row + 2;

                worksheet.Cell(currentRow, 1).Value = e.CallId ?? "-";
                worksheet.Cell(currentRow, 2).Value = e.CallDate?.ToString("dd.MM.yyyy") ?? "-";
                worksheet.Cell(currentRow, 3).Value = e.CallTime ?? "-";
                worksheet.Cell(currentRow, 4).Value = e.Duration ?? "-";
                worksheet.Cell(currentRow, 5).Value = e.EvaluatedCustomerPersonnel != null
                    ? $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}"
                    : (e.EvaluatedPersonnel != null
                        ? $"{e.EvaluatedPersonnel.FirstName} {e.EvaluatedPersonnel.LastName}"
                        : (e.EvaluatedUnknownPersonnel ?? "-"));
                worksheet.Cell(currentRow, 6).Value = e.Project?.Name ?? "-";
                worksheet.Cell(currentRow, 7).Value = e.Project?.Checklist?.Name ?? "-";
                worksheet.Cell(currentRow, 8).Value = e.ScorePercentage?.ToString("F2") ?? "0";
                worksheet.Cell(currentRow, 9).Value = e.YellowCardCount;
                worksheet.Cell(currentRow, 10).Value = e.RedCardCount;
                worksheet.Cell(currentRow, 11).Value = GetStatusDisplayName(e.StatusId);
                worksheet.Cell(currentRow, 12).Value = e.CreatedAt.ToString("dd.MM.yyyy HH:mm");
            }

            // Sütun genişliklerini ayarla
            worksheet.Columns().AdjustToContents();
            ExcelHelper.ApplyLongTextColumnStyles(worksheet, callIdColumns: new[] { 1 });

            // Minimum genişlikler (tarih sütunları için)
            if (worksheet.Column(2).Width < 12) worksheet.Column(2).Width = 12; // Çağrı Tarihi
            if (worksheet.Column(12).Width < 18) worksheet.Column(12).Width = 18; // Kontrol Tarihi

            // Dosyayı memory stream'e yaz
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Dinlemeler_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting evaluations to Excel");
            return StatusCode(500, CreateErrorResponse("Excel dışa aktarma hatası", ex));
        }
    }

    /// <summary>
    /// Taslak değerlendirmeyi siler
    /// - Kullanıcı kendi taslağını silebilir
    /// - Admin tüm taslakları silebilir
    /// - Sadece Draft durumundakiler silinebilir
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteDraft(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Auth.UserNotFound")));
            }

            // Değerlendirmeyi bul
            var evaluation = await _context.Evaluations
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

            if (evaluation == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.NotFound")));
            }

            // Sadece Draft durumundakiler silinebilir
            if (evaluation.StatusId != EvaluationStatuses.Ids.Draft)
            {
                return BadRequest(CreateErrorResponse("Sadece taslak durumundaki değerlendirmeler silinebilir."));
            }

            // Yetki kontrolü: Admin değilse kendi taslağı olmalı
            if (!isAdmin)
            {
                bool isOwner = false;
                if (userType == "CustomerPersonnel")
                {
                    isOwner = evaluation.EvaluatorCustomerPersonnelId == userId;
                }
                else
                {
                    isOwner = evaluation.EvaluatorId == userId;
                }

                if (!isOwner)
                {
                    return Forbid();
                }
            }

            // Soft delete
            evaluation.IsDeleted = true;
            evaluation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Taslak değerlendirme silindi: EvaluationId={EvaluationId}, UserId={UserId}, IsAdmin={IsAdmin}",
                id, userId, isAdmin);

            return Ok(new { message = "Taslak başarıyla silindi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting draft evaluation {EvaluationId}", id);
            return StatusCode(500, CreateErrorResponse("Taslak silinirken hata oluştu.", ex));
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
