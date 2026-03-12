using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
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
    private readonly ISurveyService _surveyService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IAuditLogService _auditLogService;

    public AssessmentApiController(
        IAssessmentService assessmentService,
        IAssessmentReportService reportService,
        ISurveyService surveyService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IAuditLogService auditLogService,
        IConfiguration configuration) : base(configuration)
    {
        _assessmentService = assessmentService;
        _reportService = reportService;
        _surveyService = surveyService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _auditLogService = auditLogService;
    }

    // ===== Proje Bazlı Dönem Yönetimi (PersonnelAssessment) =====

    /// <summary>
    /// Projeye ait dönemleri getir
    /// </summary>
    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriodsByProject([FromQuery] int projectId)
    {
        try
        {
            var periods = await _assessmentService.GetPeriodsByProjectAsync(projectId);
            return Ok(periods);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading periods for project {projectId}", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Dönemler yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Proje bazlı dönem oluştur (assignment gerektirmez)
    /// </summary>
    [HttpPost("periods")]
    public async Task<IActionResult> CreatePeriod([FromBody] Core.DTOs.AssignmentPeriod.CreateAssignmentPeriodDto dto)
    {
        try
        {
            if (!dto.ProjectId.HasValue)
                return BadRequest(new { message = "ProjectId zorunludur." });

            var period = await _assessmentService.CreatePeriodForProjectAsync(dto.ProjectId.Value, dto);
            return Ok(period);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error creating assessment period", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Dönem oluşturulurken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Dönem güncelle
    /// </summary>
    [HttpPut("periods/{periodId}")]
    public async Task<IActionResult> UpdatePeriod(int periodId, [FromBody] Core.DTOs.AssignmentPeriod.UpdateAssignmentPeriodDto dto)
    {
        try
        {
            dto.Id = periodId;
            var period = await _assessmentService.UpdatePeriodAsync(periodId, dto);
            if (period == null)
                return NotFound(new { message = "Dönem bulunamadı." });
            return Ok(period);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating assessment period {periodId}", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Dönem güncellenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Dönem sil
    /// </summary>
    [HttpDelete("periods/{periodId}")]
    public async Task<IActionResult> DeletePeriod(int periodId)
    {
        try
        {
            var result = await _assessmentService.DeletePeriodAsync(periodId);
            if (!result)
                return NotFound(new { message = "Dönem bulunamadı." });
            return Ok(new { message = "Dönem silindi." });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting assessment period {periodId}", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Dönem silinirken hata oluştu", ex));
        }
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

    // ===== Email Gönderimi =====

    /// <summary>
    /// Değerlendirme davetiyelerini gönder - her katılımcı için encrypted token ile
    /// </summary>
    [HttpPost("send-invitations")]
    public async Task<IActionResult> SendInvitations([FromBody] SendAssessmentInvitationsDto dto)
    {
        try
        {
            // Proje kontrolü
            var project = await _surveyService.GetProjectWithDetailsAsync(dto.ProjectId);
            if (project == null)
                return NotFound(new { message = "Proje bulunamadı." });

            if (project.ProjectTypeId != ProjectTypes.Ids.PersonnelAssessment)
                return BadRequest(new { message = "Bu proje bir personel değerlendirme projesi değil." });

            // Email şablonu kontrolü
            var emailTemplate = await _emailTemplateService.FindByIdAsync(dto.EmailTemplateId);
            if (emailTemplate == null)
                return BadRequest(new { message = "Email şablonu bulunamadı." });

            // SMTP kontrolü
            if (!await _emailService.IsConfiguredAsync())
                return BadRequest(new { message = "SMTP ayarları yapılandırılmamış. Ayarlar > SMTP Ayarları sayfasından yapılandırın." });

            // Base URL kontrolü
            if (string.IsNullOrWhiteSpace(dto.BaseUrl))
                return BadRequest(new { message = "Değerlendirme base URL'i belirtilmemiş." });

            // Dönem kontrolü
            var periods = await _assessmentService.GetPeriodsByProjectAsync(dto.ProjectId);
            var period = dto.AssignmentPeriodId.HasValue
                ? periods.FirstOrDefault(p => p.Id == dto.AssignmentPeriodId.Value)
                : periods.FirstOrDefault();

            if (period == null)
                return BadRequest(new { message = "Değerlendirme dönemi bulunamadı." });

            // Katılımcıları getir (zaten flat liste — EF fix-up ile Children populate edilir)
            var participants = await _assessmentService.GetParticipantsAsync(period.Id);

            if (!participants.Any())
                return BadRequest(new { message = "Dönemde katılımcı bulunamadı. Önce katılımcı ekleyin." });

            // Mevcut davetiyeleri getir (görev oluşturma ile oluşturulanları yeniden kullan)
            var existingInvitations = await _assessmentService.GetOpenInvitationsByProjectAsync(dto.ProjectId);

            // Email gönderim sonuçları
            var successCount = 0;
            var failCount = 0;
            var skippedCount = 0;
            var errors = new List<string>();

            foreach (var participant in participants)
            {
                try
                {
                    var person = participant.CustomerPersonnel;
                    if (person == null || string.IsNullOrWhiteSpace(person.Email))
                    {
                        skippedCount++;
                        continue;
                    }

                    // Encrypted token oluştur
                    var token = EncryptionHelper.CreateSurveyToken(dto.ProjectId, person.Id);

                    // Kişiye özel değerlendirme URL'i
                    var assessmentUrl = $"{dto.BaseUrl.TrimEnd('/')}?token={token}";

                    // Placeholder değişimleri
                    var subject = ReplaceAssessmentPlaceholders(emailTemplate.Subject, project, person, assessmentUrl, period.Name, project.IsAnonymous);
                    var body = ReplaceAssessmentPlaceholders(emailTemplate.Body, project, person, assessmentUrl, period.Name, project.IsAnonymous);

                    // Mevcut davetiyeyi kullan (görev oluşturma ile oluşturulan) veya yeni oluştur
                    SurveyInvitation invitation;
                    if (existingInvitations.TryGetValue(person.Id, out var existing))
                    {
                        invitation = existing;
                    }
                    else
                    {
                        invitation = await _surveyService.CreateSurveyInvitationAsync(dto.ProjectId, person.Id, person.Email, false);
                    }

                    var result = await _emailService.SendEmailAsync(person.Email, subject, body, true);

                    if (result.Success)
                    {
                        await _surveyService.UpdateSurveyInvitationStatusAsync(invitation, SurveyInvitationStatuses.Ids.Sent);
                        successCount++;
                        await _auditLogService.LogInfoAsync($"Assessment invitation sent to {person.Email} for project {dto.ProjectId}", "Assessment");
                    }
                    else
                    {
                        await _surveyService.UpdateSurveyInvitationStatusAsync(invitation, SurveyInvitationStatuses.Ids.Failed, result.ErrorMessage);
                        failCount++;
                        errors.Add($"{person.Email}: {result.ErrorMessage}");
                        await _auditLogService.LogWarningAsync($"Failed to send assessment invitation to {person.Email}: {result.ErrorMessage}", "Assessment");
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    errors.Add($"{participant.CustomerPersonnel?.Email}: {ex.Message}");
                    await _auditLogService.LogErrorAsync($"Error sending assessment invitation to {participant.CustomerPersonnel?.Email}", "Assessment", ex);
                }
            }

            return Ok(new
            {
                success = true,
                totalPersonnel = participants.Count,
                successCount,
                failCount,
                skippedCount,
                errors = errors.Take(10).ToList(),
                message = $"{successCount} kişiye davetiye gönderildi." +
                          (failCount > 0 ? $" {failCount} başarısız." : "") +
                          (skippedCount > 0 ? $" {skippedCount} email adresi yok." : "")
            });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error sending assessment invitations", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Davetiyeler gönderilirken hata oluştu", ex));
        }
    }

    private static List<AssessmentParticipant> FlattenParticipants(List<AssessmentParticipant> participants)
    {
        var result = new List<AssessmentParticipant>();
        foreach (var p in participants)
        {
            result.Add(p);
            if (p.Children != null && p.Children.Any())
                result.AddRange(FlattenParticipants(p.Children.ToList()));
        }
        return result;
    }

    private static string ReplaceAssessmentPlaceholders(string text, Project project, CustomerPersonnel person,
        string assessmentUrl, string periodName, bool isAnonymous)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Değerlendirme Linkleri
        text = text.Replace(EmailPlaceholders.AssessmentUrl, assessmentUrl);
        text = text.Replace(EmailPlaceholders.AssessmentLink, $@"<a href=""{assessmentUrl}"" target=""_blank"">{assessmentUrl}</a>");
        text = text.Replace(EmailPlaceholders.PeriodName, periodName ?? "");
        text = text.Replace(EmailPlaceholders.AssessmentMode, isAnonymous ? "Anonim" : "Açık");

        // Anket Linkleri (geri uyumluluk)
        text = text.Replace(EmailPlaceholders.SurveyUrl, assessmentUrl);
        text = text.Replace(EmailPlaceholders.SurveyLink, $@"<a href=""{assessmentUrl}"" target=""_blank"">{assessmentUrl}</a>");

        // Firma/Organizasyon
        text = text.Replace(EmailPlaceholders.CompanyName, project.Customer?.CompanyName ?? "");
        text = text.Replace(EmailPlaceholders.OrganizationName, project.Organization?.Name ?? "");

        // Alıcı Bilgileri
        text = text.Replace(EmailPlaceholders.RecipientName, $"{person.FirstName} {person.LastName}".Trim());
        text = text.Replace(EmailPlaceholders.RecipientFirstName, person.FirstName ?? "");
        text = text.Replace(EmailPlaceholders.RecipientLastName, person.LastName ?? "");
        text = text.Replace(EmailPlaceholders.RecipientEmail, person.Email ?? "");

        // Proje Bilgileri
        text = text.Replace(EmailPlaceholders.ProjectName, project.Name);
        text = text.Replace(EmailPlaceholders.SurveyName, project.Checklist?.Name ?? project.Name);
        text = text.Replace(EmailPlaceholders.DueDate, project.EndDate.ToString("dd.MM.yyyy"));
        text = text.Replace(EmailPlaceholders.StartDate, project.StartDate.ToString("dd.MM.yyyy"));
        text = text.Replace(EmailPlaceholders.EndDate, project.EndDate.ToString("dd.MM.yyyy"));

        // Sistem
        text = text.Replace(EmailPlaceholders.CurrentDate, TurkeyTime.Now.ToString("dd.MM.yyyy"));
        text = text.Replace(EmailPlaceholders.CurrentYear, TurkeyTime.Now.Year.ToString());
        text = text.Replace(EmailPlaceholders.SystemName, "Secret Customer");

        return text;
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
                    .OrderBy(q => q.GroupName)
                    .ThenBy(q => q.Order)
                    .Select(q => new
                    {
                        id = q.Id,
                        text = context.UseSelfText && !string.IsNullOrEmpty(q.SelfText) ? q.SelfText : q.Text,
                        description = context.UseSelfText && !string.IsNullOrEmpty(q.SelfText) ? null : q.HelpText,
                        groupName = q.GroupName,
                        scoringType = ScoringTypes.GetById(q.ScoringTypeId)!.SystemName,
                        maxPoints = q.MaxPoints,
                        weightPoints = q.WeightPoints,
                        order = q.Order,
                        isRequired = q.IsRequired,
                        selectionTypeId = q.SelectionTypeId,
                        showScoreInput = q.ShowScoreInput,
                        allowComment = q.AllowComment,
                        subCriteria = q.SubCriteria
                            .Where(sc => sc.IsActive && !sc.IsDeleted)
                            .OrderBy(sc => sc.Order)
                            .Select(sc => new
                            {
                                id = sc.Id,
                                description = context.UseSelfText && !string.IsNullOrEmpty(sc.SelfDescription) ? sc.SelfDescription : sc.Description,
                                order = sc.Order
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

    /// <summary>
    /// Cevapları gönder: Evaluation oluştur + cevapları kaydet + task tamamla + sonraki task döndür (PUBLIC)
    /// </summary>
    [HttpPost("fill/submit")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitAssessmentTask([FromBody] SubmitAssessmentTaskDto dto)
    {
        try
        {
            // 1. Token doğrula ve context al
            var context = await _assessmentService.GetFillContextByTokenAsync(dto.Token);
            if (context == null)
                return NotFound(new { message = "Geçersiz veya süresi dolmuş link." });

            if (context.IsAllCompleted)
                return BadRequest(new { message = "Tüm değerlendirmeler zaten tamamlanmış." });

            // Task ID kontrolü
            if (context.CurrentTask.Id != dto.AssessmentTaskId)
                return BadRequest(new { message = "Geçersiz değerlendirme görevi." });

            var project = context.Project;

            // 2. Evaluation oluştur
            var evaluation = new Evaluation
            {
                ProjectId = project.Id,
                ChecklistId = project.ChecklistId,
                EvaluatedCustomerPersonnelId = context.CurrentTask.EvaluatedCustomerPersonnelId,
                EvaluatedOrganizationId = project.OrganizationId,
                FeedbackRoleId = context.CurrentTask.FeedbackRoleId,
                StartedAt = context.Invitation.OpenedAt ?? TurkeyTime.Now,
                CompletedAt = TurkeyTime.Now,
                StatusId = EvaluationStatuses.Ids.Completed,
                Notes = $"Personel değerlendirme - {FeedbackRoles.GetById(context.CurrentTask.FeedbackRoleId)?.Description ?? ""}",
                CreatedAt = TurkeyTime.Now
            };

            evaluation = await _surveyService.CreateEvaluationAsync(evaluation);

            // 3. Cevapları kaydet ve puanı hesapla
            var scoreResult = await _surveyService.SaveAnswersAndCalculateScoreAsync(evaluation.Id, project.ChecklistId, dto.Answers);
            evaluation.TotalScore = scoreResult.TotalScore;
            evaluation.MaxScore = scoreResult.MaxScore;
            evaluation.ScorePercentage = scoreResult.ScorePercentage;
            await _surveyService.SaveChangesAsync();

            // 4. Task'ı tamamla
            var result = await _assessmentService.CompleteTaskAsync(dto.AssessmentTaskId, evaluation.Id);

            await _auditLogService.LogInfoAsync(
                $"Assessment task completed: TaskId={dto.AssessmentTaskId}, EvaluationId={evaluation.Id}, Score={evaluation.TotalScore}",
                "Assessment");

            // 5. Sonraki task bilgisini döndür
            return Ok(new
            {
                success = true,
                evaluationId = evaluation.Id,
                totalScore = evaluation.TotalScore,
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
            await _auditLogService.LogErrorAsync("Error submitting assessment task", "Assessment", ex);
            return StatusCode(500, CreateErrorResponse("Değerlendirme gönderilirken hata oluştu", ex));
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

public class SubmitAssessmentTaskDto
{
    public string Token { get; set; } = string.Empty;
    public int AssessmentTaskId { get; set; }
    public List<SurveyAnswerDto> Answers { get; set; } = new();
}

public class SendAssessmentInvitationsDto
{
    public int ProjectId { get; set; }
    public int EmailTemplateId { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public int? AssignmentPeriodId { get; set; }
}
