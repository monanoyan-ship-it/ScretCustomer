using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Evaluation;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/evaluations")]
public class EvaluationsApiController : BaseApiController
{
    private readonly IEvaluationService _evaluationService;
    private readonly IFileUploadService _fileUploadService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;

    public EvaluationsApiController(
        IEvaluationService evaluationService,
        IFileUploadService fileUploadService,
        IAuditLogService auditLogService,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _evaluationService = evaluationService;
        _fileUploadService = fileUploadService;
        _auditLogService = auditLogService;
        _localizationService = localizationService;
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
            var result = await _evaluationService.GetAllFilteredAsync(
                projectIds, customerIds, organizationIds,
                evaluatorIds, checklistIds, statuses,
                startDate, endDate, page, pageSize);

            return Ok(new { items = result.Items, totalCount = result.TotalCount, page = result.Page, pageSize = result.PageSize });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error loading all evaluations", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error loading evaluation {id}", "Evaluations", ex);
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
            var attachments = await _evaluationService.GetAttachmentsAsync(id);
            return Ok(attachments);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading attachments for evaluation {id}", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error uploading attachment for evaluation {id}", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error deleting attachment {attachmentId}", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error downloading attachment {attachmentId}", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error loading evaluation for project {projectId}", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error loading evaluations for project {projectId}", "Evaluations", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.LoadError"), ex));
        }
    }

    /// <summary>
    /// Mevcut kullanicinin degerlendirmelerini getirir - Server-side filtre desteği
    /// </summary>
    [HttpGet("evaluator")]
    [Authorize]
    public async Task<IActionResult> GetByEvaluator(
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] List<string>? statuses = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? personnel = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] DateTime? controlStartDate = null,
        [FromQuery] DateTime? controlEndDate = null,
        [FromQuery] int? evaluationId = null)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Auth.UserNotFound")));
            }

            var evaluations = await _evaluationService.GetByEvaluatorFilteredAsync(userId, userType,
                status, search, personnel,
                startDate, endDate, controlStartDate, controlEndDate,
                projectIds, statuses, evaluationId);

            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error loading evaluations for current user", "Evaluations", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.LoadError"), ex));
        }
    }

    /// <summary>
    /// Temsilcinin kendisi hakkındaki değerlendirmeleri getirir
    /// CustomerOperator'ın kendi performansını görmesi için kullanılır
    /// </summary>
    [HttpGet("my-evaluations")]
    [Authorize(Roles = "CustomerOperator,CustomerSupervisor,CustomerManager,Admin")]
    public async Task<IActionResult> GetMyEvaluations(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges,
        [FromQuery] string? evaluationType = null)
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

            var evaluations = await _evaluationService.GetByEvaluatedCustomerPersonnelIdAsync(userId, projectIds, dateRanges, evaluationType);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error loading my evaluations for current user", "Evaluations", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.LoadError"), ex));
        }
    }

    /// <summary>
    /// Çağrı ID'nin müşteri için mevcut olup olmadığını kontrol eder
    /// </summary>
    [HttpGet("check-call-id")]
    [Authorize]
    public async Task<IActionResult> CheckCallIdExists([FromQuery] string callId, [FromQuery] int projectId, [FromQuery] int? evaluationId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(callId))
                return Ok(new { exists = false });

            var exists = await _evaluationService.CheckCallIdExistsAsync(callId, projectId, evaluationId);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error checking CallId existence", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error loading evaluation form for assignment {assignmentId}", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error loading existing evaluation form {evaluationId}", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error loading personnel for organization {organizationId}", "Evaluations", ex);
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
            var (found, isActive) = await _evaluationService.CheckProjectActiveForAssignmentAsync(dto.ProjectId);

            if (!found)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.NotFound")));
            }

            if (!isActive)
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
            await _auditLogService.LogErrorAsync($"Error starting evaluation for project {dto.ProjectId}", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error calculating score for checklist {request.ChecklistId}", "Evaluations", ex);
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
            var answers = await _evaluationService.GetAnswerIdsAsync(evaluation.Id);

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
            await _auditLogService.LogErrorAsync($"Error submitting evaluation for project {dto.ProjectId}", "Evaluations", ex);
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
            var answers = await _evaluationService.GetAnswerIdsAsync(evaluation.Id);

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
            await _auditLogService.LogErrorAsync($"Error saving draft for project {dto.ProjectId}", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error updating draft {dto.EvaluationId}", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error reverting evaluation {id} to draft", "Evaluations", ex);
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
            await _auditLogService.LogErrorAsync($"Error cancelling evaluation {id}", "Evaluations", ex);
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

            var userType = User.FindFirst("UserType")?.Value;
            var result = await _evaluationService.CreateRevertRequestAsync(id, userId, userType, dto?.Reason);

            if (!result.Success)
            {
                if (result.StatusCode == 404)
                    return NotFound(CreateErrorResponse(result.ErrorMessage!));
                return BadRequest(CreateErrorResponse(result.ErrorMessage!));
            }

            return Ok(new
            {
                message = "Taslağa alma talebi gönderildi. Admin onayı bekleniyor.",
                approvalId = result.ApprovalId,
                referenceNumber = result.ReferenceNumber
            });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error creating revert request for evaluation {id}", "Evaluations", ex);
            return StatusCode(500, CreateErrorResponse("Talep oluşturulurken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Tamamlanmış değerlendirme için sonradan bildirim maili gönderir
    /// </summary>
    [HttpPost("{id:int}/send-notification")]
    [Authorize]
    public async Task<IActionResult> SendNotification(int id)
    {
        try
        {
            await _evaluationService.SendNotificationAsync(id);
            return Ok(new { message = await _localizationService.GetResourceAsync("Evaluation.MailSentSuccess") });
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
            await _auditLogService.LogErrorAsync($"Error sending notification for evaluation {id}", "Evaluations", ex);
            return StatusCode(500, CreateErrorResponse("Bildirim gönderilirken hata oluştu.", ex));
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
            var result = await _evaluationService.RecalculateAllScoresAsync(projectId);

            return Ok(new {
                message = $"{result.UpdatedCount} değerlendirme güncellendi, {result.ErrorCount} hata oluştu.",
                result.UpdatedCount,
                result.ErrorCount,
                totalProcessed = result.TotalProcessed
            });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error recalculating scores", "Evaluations", ex);
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
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] DateTime? controlStartDate = null,
        [FromQuery] DateTime? controlEndDate = null,
        [FromQuery] int? evaluationId = null)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Auth.UserNotFound")));
            }

            var evaluations = await _evaluationService.GetEvaluationsForExportAsync(userId, userType,
                status, search, personnel,
                startDate, endDate, controlStartDate, controlEndDate,
                projectIds, customerIds, organizationIds, evaluatorIds, checklistIds, statuses,
                evaluationId);

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
                worksheet.Cell(currentRow, 5).Value = e.PersonnelName ?? "-";
                worksheet.Cell(currentRow, 6).Value = e.ProjectName ?? "-";
                worksheet.Cell(currentRow, 7).Value = e.ChecklistName ?? "-";
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

            var fileName = $"Dinlemeler_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error exporting evaluations to Excel", "Evaluations", ex);
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

            var result = await _evaluationService.DeleteDraftAsync(id, userId, userType, isAdmin);

            if (!result.Success)
            {
                if (result.StatusCode == 404)
                    return NotFound(CreateErrorResponse(result.ErrorMessage!));
                if (result.StatusCode == 403)
                    return Forbid();
                return BadRequest(CreateErrorResponse(result.ErrorMessage!));
            }

            return Ok(new { message = "Taslak başarıyla silindi." });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting draft evaluation {id}", "Evaluations", ex);
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
