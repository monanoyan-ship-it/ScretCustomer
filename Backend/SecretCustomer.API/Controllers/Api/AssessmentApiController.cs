using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Services.Helpers;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/assessment")]
[Authorize(Roles = "Admin")]
public class AssessmentApiController : BaseApiController
{
    private readonly IAssessmentService _assessmentService;
    private readonly IAssessmentReportService _reportService;
    private readonly IAuditLogService _auditLogService;

    public AssessmentApiController(
        IAssessmentService assessmentService,
        IAssessmentReportService reportService,
        IAuditLogService auditLogService,
        IConfiguration configuration) : base(configuration)
    {
        _assessmentService = assessmentService;
        _reportService = reportService;
        _auditLogService = auditLogService;
    }

    // ===== Hiyerarşi Yönetimi =====

    /// <summary>
    /// Dönemdeki katılımcıları hiyerarşi ağacı olarak getir
    /// </summary>
    [HttpGet("participants/{assignmentPeriodId}")]
    public async Task<IActionResult> GetParticipants(int assignmentPeriodId)
    {
        try
        {
            var participants = await _assessmentService.GetParticipantsAsync(assignmentPeriodId);
            return Ok(participants);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading assessment participants for period {assignmentPeriodId}", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Katılımcılar yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Katılımcı ekle
    /// </summary>
    [HttpPost("participants")]
    public async Task<IActionResult> AddParticipant([FromBody] AddParticipantDto dto)
    {
        try
        {
            var participant = await _assessmentService.AddParticipantAsync(dto.AssignmentPeriodId, dto.CustomerPersonnelId, dto.ParentId);
            return Ok(participant);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error adding assessment participant", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Katılımcı eklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Katılımcı çıkar
    /// </summary>
    [HttpDelete("participants/{participantId}")]
    public async Task<IActionResult> RemoveParticipant(int participantId)
    {
        try
        {
            await _assessmentService.RemoveParticipantAsync(participantId);
            return Ok(new { message = "Katılımcı silindi." });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error removing assessment participant {participantId}", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Katılımcı silinirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Katılımcı taşı (üstünü değiştir)
    /// </summary>
    [HttpPut("participants/{participantId}/move")]
    public async Task<IActionResult> MoveParticipant(int participantId, [FromBody] MoveParticipantDto dto)
    {
        try
        {
            await _assessmentService.MoveParticipantAsync(participantId, dto.NewParentId);
            return Ok(new { message = "Katılımcı taşındı." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error moving assessment participant {participantId}", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Katılımcı taşınırken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Organizasyondan hiyerarşi aktar
    /// </summary>
    [HttpPost("participants/import/{assignmentPeriodId}")]
    public async Task<IActionResult> ImportFromOrganization(int assignmentPeriodId, [FromBody] ImportOrganizationDto dto)
    {
        try
        {
            var count = await _assessmentService.ImportFromOrganizationAsync(assignmentPeriodId, dto.ProjectId);
            return Ok(new { message = $"{count} katılımcı aktarıldı.", count });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error importing organization for period {assignmentPeriodId}", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Organizasyon aktarılırken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Katılımcı sayısı
    /// </summary>
    [HttpGet("participants/{assignmentPeriodId}/count")]
    public async Task<IActionResult> GetParticipantCount(int assignmentPeriodId)
    {
        try
        {
            var count = await _assessmentService.GetParticipantCountAsync(assignmentPeriodId);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting participant count for period {assignmentPeriodId}", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Katılımcı sayısı alınırken hata oluştu", ex));
        }
    }

    // ===== Task Zincir Oluşturma =====

    /// <summary>
    /// Değerlendirme task'larını oluştur (davetiye + zincir)
    /// </summary>
    [HttpPost("generate-tasks")]
    public async Task<IActionResult> GenerateAssessmentTasks([FromBody] GenerateTasksDto dto)
    {
        try
        {
            var count = await _assessmentService.GenerateAssessmentTasksAsync(dto.AssignmentPeriodId, dto.ProjectId);
            return Ok(new { message = $"{count} değerlendirme görevi oluşturuldu.", count });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error generating assessment tasks", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Değerlendirme görevleri oluşturulurken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Davetiyenin task zincirini getir
    /// </summary>
    [HttpGet("tasks/chain/{surveyInvitationId}")]
    public async Task<IActionResult> GetTaskChain(int surveyInvitationId)
    {
        try
        {
            var tasks = await _assessmentService.GetTaskChainAsync(surveyInvitationId);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading task chain for invitation {surveyInvitationId}", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Task zinciri yüklenirken hata oluştu", ex));
        }
    }

    // ===== Tek Link Doldurma Akışı (PUBLIC) =====

    /// <summary>
    /// Token ile doldurma context'ini getir (PUBLIC - anonim erişim)
    /// </summary>
    [HttpGet("fill/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFillContext(string token)
    {
        try
        {
            var context = await _assessmentService.GetFillContextByTokenAsync(token);
            if (context == null)
                return NotFound(new { message = "Geçersiz veya süresi dolmuş link." });

            if (context.IsAllCompleted)
            {
                return Ok(new
                {
                    isAllCompleted = true,
                    totalTaskCount = context.TotalTaskCount,
                    completedTaskCount = context.CompletedTaskCount,
                    message = "Tüm değerlendirmeler tamamlanmıştır. Katılımınız için teşekkür ederiz."
                });
            }

            var currentTask = context.CurrentTask;
            var project = context.Project;

            return Ok(new
            {
                isAllCompleted = false,
                totalTaskCount = context.TotalTaskCount,
                completedTaskCount = context.CompletedTaskCount,
                useSelfText = context.UseSelfText,
                currentTask = new
                {
                    id = currentTask.Id,
                    feedbackRoleId = currentTask.FeedbackRoleId,
                    order = currentTask.Order,
                    evaluatedPersonnelName = currentTask.EvaluatedCustomerPersonnel != null
                        ? $"{currentTask.EvaluatedCustomerPersonnel.FirstName} {currentTask.EvaluatedCustomerPersonnel.LastName}".Trim()
                        : null
                },
                project = new
                {
                    id = project.Id,
                    name = project.Name,
                    checklistId = project.ChecklistId,
                    isAnonymous = project.IsAnonymous
                },
                questions = project.Checklist?.Questions
                    .OrderBy(q => q.Order)
                    .Select(q => new
                    {
                        id = q.Id,
                        text = context.UseSelfText && !string.IsNullOrEmpty(q.SelfText) ? q.SelfText : q.Text,
                        groupName = q.GroupName,
                        maxPoints = q.MaxPoints,
                        weightPoints = q.WeightPoints,
                        order = q.Order,
                        subCriteria = q.SubCriteria
                            .OrderBy(sc => sc.Order)
                            .Select(sc => new
                            {
                                id = sc.Id,
                                description = context.UseSelfText && !string.IsNullOrEmpty(sc.SelfDescription) ? sc.SelfDescription : sc.Description,
                                weightPoints = sc.WeightPoints
                            })
                    })
            });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error getting assessment fill context", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Değerlendirme formu yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Task tamamla (PUBLIC - anonim erişim)
    /// </summary>
    [HttpPost("fill/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteTask([FromBody] CompleteAssessmentTaskDto dto)
    {
        try
        {
            var result = await _assessmentService.CompleteTaskAsync(dto.AssessmentTaskId, dto.EvaluationId);
            return Ok(new
            {
                hasNextTask = result.HasNextTask,
                isAllCompleted = result.IsAllCompleted,
                nextTask = result.NextTask != null ? new
                {
                    id = result.NextTask.Id,
                    feedbackRoleId = result.NextTask.FeedbackRoleId,
                    order = result.NextTask.Order,
                    evaluatedPersonnelName = result.NextTask.EvaluatedCustomerPersonnel != null
                        ? $"{result.NextTask.EvaluatedCustomerPersonnel.FirstName} {result.NextTask.EvaluatedCustomerPersonnel.LastName}".Trim()
                        : null
                } : null
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error completing assessment task", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Değerlendirme tamamlanırken hata oluştu", ex));
        }
    }

    // ===== Raporlama =====

    /// <summary>
    /// Kişi bazlı rapor
    /// </summary>
    [HttpGet("reports/person/{projectId}/{evaluatedCustomerPersonnelId}")]
    public async Task<IActionResult> GetPersonReport(int projectId, int evaluatedCustomerPersonnelId, [FromQuery] int? assignmentPeriodId = null)
    {
        try
        {
            var report = await _reportService.GetPersonReportAsync(projectId, evaluatedCustomerPersonnelId, assignmentPeriodId);
            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading person report for project {projectId}, personnel {evaluatedCustomerPersonnelId}", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Kişi raporu yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Dönem özeti raporu
    /// </summary>
    [HttpGet("reports/period-summary/{projectId}/{assignmentPeriodId}")]
    public async Task<IActionResult> GetPeriodSummary(int projectId, int assignmentPeriodId)
    {
        try
        {
            var summary = await _reportService.GetPeriodSummaryAsync(projectId, assignmentPeriodId);
            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading period summary for project {projectId}, period {assignmentPeriodId}", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Dönem özeti yüklenirken hata oluştu", ex));
        }
    }
}

// ===== Request DTOs =====

public class AddParticipantDto
{
    public int AssignmentPeriodId { get; set; }
    public int CustomerPersonnelId { get; set; }
    public int? ParentId { get; set; }
}

public class MoveParticipantDto
{
    public int? NewParentId { get; set; }
}

public class ImportOrganizationDto
{
    public int ProjectId { get; set; }
}

public class GenerateTasksDto
{
    public int AssignmentPeriodId { get; set; }
    public int ProjectId { get; set; }
}

public class CompleteAssessmentTaskDto
{
    public int AssessmentTaskId { get; set; }
    public int EvaluationId { get; set; }
}
