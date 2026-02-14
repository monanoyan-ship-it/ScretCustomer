using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Services.Helpers;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/surveys")]
[ApiController]
[Authorize(Roles = "Admin,QualitySpecialist")]
public class SurveyApiController : ControllerBase
{
    private readonly ISurveyService _surveyService;
    private readonly IEmailService _emailService;
    private readonly IQRCodeService _qrCodeService;
    private readonly ILogger<SurveyApiController> _logger;

    public SurveyApiController(
        ISurveyService surveyService,
        IEmailService emailService,
        IQRCodeService qrCodeService,
        ILogger<SurveyApiController> logger)
    {
        _surveyService = surveyService;
        _emailService = emailService;
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    /// <summary>
    /// Anket davetiyelerini gönder - her personel için benzersiz encrypted token oluşturur
    /// </summary>
    [HttpPost("{projectId}/send-invitations")]
    public async Task<IActionResult> SendInvitations(int projectId, [FromBody] SendSurveyInvitationsDto dto)
    {
        // Projeyi kontrol et
        var project = await _surveyService.GetProjectWithDetailsAsync(projectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Proje tipini kontrol et
        if (project.ProjectTypeId != ProjectTypes.Ids.OnlineSurvey)
        {
            return BadRequest(new { message = "Bu proje bir online anket projesi değil." });
        }

        // Email şablonu kontrolü
        var emailTemplate = project.EmailTemplate;
        if (emailTemplate == null && !dto.EmailTemplateId.HasValue)
        {
            return BadRequest(new { message = "Email şablonu seçilmemiş." });
        }

        if (dto.EmailTemplateId.HasValue)
        {
            emailTemplate = await _surveyService.GetEmailTemplateAsync(dto.EmailTemplateId.Value);
        }

        if (emailTemplate == null)
        {
            return BadRequest(new { message = "Email şablonu bulunamadı." });
        }

        // SMTP yapılandırması kontrolü
        if (!await _emailService.IsConfiguredAsync())
        {
            return BadRequest(new { message = "SMTP ayarları yapılandırılmamış. Ayarlar > SMTP Ayarları sayfasından yapılandırın." });
        }

        // Base URL kontrolü
        if (string.IsNullOrWhiteSpace(dto.BaseUrl))
        {
            return BadRequest(new { message = "Anket base URL'i belirtilmemiş." });
        }

        // Müşteri Portal Personellerini getir (CustomerPersonnel)
        var personnelList = await _surveyService.GetProjectPersonnelWithEmailAsync(project.CustomerId, project.OrganizationId);

        if (!personnelList.Any())
        {
            return BadRequest(new { message = "Gönderilecek personel bulunamadı. Email adresi olan aktif personel olduğundan emin olun." });
        }

        // Zaten anketi tamamlamış kişileri bul (Evaluation var mı?)
        var completedPersonnelIds = await _surveyService.GetCompletedPersonnelIdsAsync(projectId);

        // Email gönderim sonuçları
        var successCount = 0;
        var failCount = 0;
        var skippedCount = 0;
        var errors = new List<string>();

        foreach (var person in personnelList)
        {
            try
            {
                // Zaten tamamlamış mı kontrol et (hatırlatma değilse atla)
                if (!dto.SendReminders && completedPersonnelIds.Contains(person.Id))
                {
                    skippedCount++;
                    continue;
                }

                // Encrypted token oluştur
                var token = EncryptionHelper.CreateSurveyToken(projectId, person.Id);

                // Kişiye özel anket URL'i oluştur
                var surveyUrl = $"{dto.BaseUrl.TrimEnd('/')}?token={token}";

                // Placeholder değişimleri
                var subject = ReplacePlaceholders(emailTemplate.Subject, project, person, surveyUrl);
                var body = ReplacePlaceholders(emailTemplate.Body, project, person, surveyUrl);

                // SurveyInvitation kaydı oluştur
                var invitation = await _surveyService.CreateSurveyInvitationAsync(projectId, person.Id, person.Email!, dto.SendReminders);

                var result = await _emailService.SendEmailAsync(person.Email!, subject, body, true);

                if (result.Success)
                {
                    await _surveyService.UpdateSurveyInvitationStatusAsync(invitation, SurveyInvitationStatuses.Ids.Sent);
                    successCount++;
                    _logger.LogInformation("Survey invitation sent to {Email} for project {ProjectId}",
                        person.Email, projectId);
                }
                else
                {
                    await _surveyService.UpdateSurveyInvitationStatusAsync(invitation, SurveyInvitationStatuses.Ids.Failed, result.ErrorMessage);
                    failCount++;
                    errors.Add($"{person.Email}: {result.ErrorMessage}");
                    _logger.LogWarning("Failed to send survey invitation to {Email}: {Error}", person.Email, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"{person.Email}: {ex.Message}");
                _logger.LogError(ex, "Error sending survey invitation to {Email}", person.Email);
            }
        }

        return Ok(new
        {
            success = true,
            totalPersonnel = personnelList.Count,
            successCount,
            failCount,
            skippedCount,
            errors = errors.Take(10).ToList(),
            message = $"{successCount} kişiye davetiye gönderildi." +
                      (failCount > 0 ? $" {failCount} başarısız." : "") +
                      (skippedCount > 0 ? $" {skippedCount} zaten tamamlamış." : "")
        });
    }

    /// <summary>
    /// Token ile anket bilgilerini getir (PUBLIC - Anket formu için)
    /// </summary>
    [HttpGet("validate/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateToken(string token)
    {
        // Önce external invitation token mı kontrol et (GUID format)
        var externalInvitation = await _surveyService.GetExternalInvitationWithProjectAsync(token);

        if (externalInvitation != null)
        {
            return await ValidateExternalToken(externalInvitation);
        }

        // Değilse mevcut encrypted token flow
        var tokenData = EncryptionHelper.ParseSurveyToken(token);
        if (tokenData == null)
        {
            return NotFound(new { message = "Geçersiz token." });
        }

        // Projeyi getir
        var project = await _surveyService.GetProjectWithChecklistAsync(tokenData.ProjectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Proje bitiş tarihine göre expiration kontrolü
        if (tokenData.IsExpiredByDate(project.EndDate))
        {
            return BadRequest(new { message = "Anket süresi dolmuş." });
        }

        // Personeli getir
        var personnel = await _surveyService.GetPersonnelAsync(tokenData.PersonnelId);

        if (personnel == null)
        {
            return NotFound(new { message = "Personel bulunamadı." });
        }

        // Bu personel için zaten değerlendirme var mı?
        var existingEvaluation = await _surveyService.HasCompletedAssignmentAsync(project.Id, personnel.Id);

        if (existingEvaluation)
        {
            return BadRequest(new { message = "Bu anket zaten tamamlanmış." });
        }

        // SurveyInvitation kaydını bul ve açıldı olarak işaretle (opsiyonel - hata verse de devam et)
        try
        {
            await _surveyService.MarkInvitationOpenedAsync(tokenData.ProjectId, tokenData.PersonnelId);
        }
        catch (Exception ex)
        {
            // Davet kaydı bulunamasa veya tablo yoksa devam et - anket yine de çalışmalı
            _logger.LogWarning(ex, "SurveyInvitation update failed for project {ProjectId}, personnel {PersonnelId}",
                tokenData.ProjectId, tokenData.PersonnelId);
        }

        var isAnonymous = project.SurveyIdentityTypeId == SurveyIdentityTypes.Ids.Anonymous;

        // Checklist sorularını getir (SubCriteria dahil)
        var questions = await _surveyService.GetSurveyQuestionsAsync(project.ChecklistId);

        return Ok(new
        {
            valid = true,
            isExternal = false,
            hideGroupNames = project.Checklist?.HideGroupNames ?? false,
            invitation = new
            {
                token,
                projectId = project.Id,
                projectName = project.Name,
                checklistId = project.ChecklistId,
                checklistName = project.Checklist?.Name,
                customerName = project.Customer?.CompanyName,
                organizationName = project.Organization?.Name,
                startDate = project.StartDate,
                endDate = project.EndDate,
                isAnonymous,
                // Anonim değilse personel bilgilerini göster
                personnel = isAnonymous ? null : new
                {
                    id = personnel.Id,
                    firstName = personnel.FirstName,
                    lastName = personnel.LastName,
                    fullName = $"{personnel.FirstName} {personnel.LastName}".Trim(),
                    email = personnel.Email
                }
            },
            questions
        });
    }

    /// <summary>
    /// External invitation token'ını validate et
    /// </summary>
    private async Task<IActionResult> ValidateExternalToken(SurveyExternalInvitation invitation)
    {
        var project = invitation.Project;
        if (project == null || project.IsDeleted)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Proje bitiş tarihine göre expiration kontrolü
        if (project.EndDate < TurkeyTime.Now)
        {
            return BadRequest(new { message = "Anket süresi dolmuş." });
        }

        // Bu davetiye zaten tamamlanmış mı?
        if (invitation.IsCompleted)
        {
            return BadRequest(new { message = "Bu anket zaten tamamlanmış." });
        }

        // İlk kez açılıyorsa işaretle
        await _surveyService.MarkExternalInvitationOpenedAsync(invitation);

        var isAnonymous = project.SurveyIdentityTypeId == SurveyIdentityTypes.Ids.Anonymous;

        // Checklist sorularını getir
        var questions = await _surveyService.GetSurveyQuestionsAsync(project.ChecklistId);

        return Ok(new
        {
            valid = true,
            isExternal = true,
            hideGroupNames = project.Checklist?.HideGroupNames ?? false,
            invitation = new
            {
                token = invitation.Token,
                projectId = project.Id,
                projectName = project.Name,
                checklistId = project.ChecklistId,
                checklistName = project.Checklist?.Name,
                customerName = project.Customer?.CompanyName,
                organizationName = project.Organization?.Name,
                startDate = project.StartDate,
                endDate = project.EndDate,
                isAnonymous,
                // Dış katılımcı bilgileri (anonim değilse)
                personnel = isAnonymous ? null : new
                {
                    id = (int?)null,
                    firstName = invitation.FirstName,
                    lastName = invitation.LastName,
                    fullName = invitation.FullName,
                    email = invitation.Email
                }
            },
            questions
        });
    }

    /// <summary>
    /// Anket cevaplarını gönder ve değerlendirme oluştur (PUBLIC)
    /// </summary>
    [HttpPost("submit/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitSurvey(string token, [FromBody] SubmitSurveyDto dto)
    {
        // Önce external invitation token mı kontrol et
        var externalInvitation = await _surveyService.GetExternalInvitationWithChecklistAsync(token);

        if (externalInvitation != null)
        {
            return await SubmitExternalSurvey(externalInvitation, dto);
        }

        // Değilse mevcut encrypted token flow
        var tokenData = EncryptionHelper.ParseSurveyToken(token);
        if (tokenData == null)
        {
            return NotFound(new { message = "Geçersiz token." });
        }

        // Projeyi getir
        var project = await _surveyService.GetProjectWithChecklistAsync(tokenData.ProjectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Proje bitiş tarihine göre expiration kontrolü
        if (tokenData.IsExpiredByDate(project.EndDate))
        {
            return BadRequest(new { message = "Anket süresi dolmuş." });
        }

        // Personeli getir
        var personnel = await _surveyService.GetPersonnelAsync(tokenData.PersonnelId);

        if (personnel == null)
        {
            return NotFound(new { message = "Personel bulunamadı." });
        }

        // Bu personel için zaten değerlendirme var mı?
        var existingEvaluation = await _surveyService.GetCompletedEvaluationAsync(project.Id, personnel.Id);

        if (existingEvaluation != null)
        {
            return BadRequest(new { message = "Bu anket zaten tamamlanmış." });
        }

        try
        {
            // Değerlendirme oluştur (Assignment oluşturulmaz)
            var evaluation = new Evaluation
            {
                ProjectId = project.Id,
                EvaluatedCustomerPersonnelId = personnel.Id,
                EvaluatedOrganizationId = project.OrganizationId,
                StartedAt = tokenData.CreatedAt,
                CompletedAt = TurkeyTime.Now,
                StatusId = EvaluationStatuses.Ids.Completed,
                Notes = "Online anket ile dolduruldu",
                CreatedAt = TurkeyTime.Now
            };

            evaluation = await _surveyService.CreateEvaluationAsync(evaluation);

            // Cevapları kaydet ve puanı hesapla
            var scoreResult = await _surveyService.SaveAnswersAndCalculateScoreAsync(evaluation.Id, project.ChecklistId, dto.Answers);
            evaluation.TotalScore = scoreResult.TotalScore;
            evaluation.MaxScore = scoreResult.MaxScore;
            evaluation.ScorePercentage = scoreResult.ScorePercentage;

            // SurveyInvitation kaydını tamamlandı olarak işaretle (opsiyonel)
            try
            {
                await _surveyService.MarkSurveyInvitationCompletedAsync(project.Id, personnel.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SurveyInvitation completion update failed for project {ProjectId}, personnel {PersonnelId}",
                    project.Id, personnel.Id);
            }

            await _surveyService.SaveChangesAsync();

            _logger.LogInformation("Survey submitted for project {ProjectId}, personnel {PersonnelId}, EvaluationId: {EvaluationId}, Score: {Score}",
                project.Id, personnel.Id, evaluation.Id, evaluation.TotalScore);

            return Ok(new
            {
                success = true,
                evaluationId = evaluation.Id,
                totalScore = evaluation.TotalScore,
                message = "Anket başarıyla gönderildi."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting survey for project {ProjectId}, personnel {PersonnelId}", project.Id, personnel.Id);
            return StatusCode(500, new { message = "Anket gönderilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Dış katılımcı anketi gönder
    /// </summary>
    private async Task<IActionResult> SubmitExternalSurvey(SurveyExternalInvitation invitation, SubmitSurveyDto dto)
    {
        var project = invitation.Project;
        if (project == null || project.IsDeleted)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Proje bitiş tarihine göre expiration kontrolü
        if (project.EndDate < TurkeyTime.Now)
        {
            return BadRequest(new { message = "Anket süresi dolmuş." });
        }

        // Bu davetiye zaten tamamlanmış mı?
        if (invitation.IsCompleted)
        {
            return BadRequest(new { message = "Bu anket zaten tamamlanmış." });
        }

        try
        {
            // Değerlendirme oluştur (Assignment oluşturulmaz)
            var evaluation = new Evaluation
            {
                ProjectId = project.Id,
                EvaluatedCustomerPersonnelId = null, // External - personnel yok
                EvaluatedOrganizationId = project.OrganizationId,
                StartedAt = invitation.OpenedAt ?? invitation.CreatedAt,
                CompletedAt = TurkeyTime.Now,
                StatusId = EvaluationStatuses.Ids.Completed,
                Notes = $"Dış katılımcı anketi: {invitation.Email}" +
                        (!string.IsNullOrWhiteSpace(invitation.FullName) ? $" ({invitation.FullName})" : ""),
                CreatedAt = TurkeyTime.Now
            };

            evaluation = await _surveyService.CreateEvaluationAsync(evaluation);

            // Cevapları kaydet ve puanı hesapla
            var scoreResult = await _surveyService.SaveAnswersAndCalculateScoreAsync(evaluation.Id, project.ChecklistId, dto.Answers);
            evaluation.TotalScore = scoreResult.TotalScore;
            evaluation.MaxScore = scoreResult.MaxScore;
            evaluation.ScorePercentage = scoreResult.ScorePercentage;

            // External invitation'ı tamamlandı olarak işaretle
            await _surveyService.MarkExternalInvitationCompletedAsync(invitation, evaluation.Id);

            _logger.LogInformation("External survey submitted for project {ProjectId}, email {Email}, EvaluationId: {EvaluationId}, Score: {Score}",
                project.Id, invitation.Email, evaluation.Id, evaluation.TotalScore);

            return Ok(new
            {
                success = true,
                evaluationId = evaluation.Id,
                totalScore = evaluation.TotalScore,
                message = "Anket başarıyla gönderildi."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting external survey for project {ProjectId}, email {Email}",
                project.Id, invitation.Email);
            return StatusCode(500, new { message = "Anket gönderilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Personel preview - kaç kişiye mail gidecek
    /// </summary>
    [HttpGet("{projectId}/personnel-preview")]
    public async Task<IActionResult> GetPersonnelPreview(int projectId)
    {
        var project = await _surveyService.GetProjectAsync(projectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Proje kapsamındaki personelleri getir
        var personnel = await _surveyService.GetProjectPersonnelAsync(project.CustomerId, project.OrganizationId);

        var withEmail = personnel.Count(p => !string.IsNullOrEmpty(p.Email));
        var withoutEmail = personnel.Count - withEmail;

        return Ok(new
        {
            totalCount = personnel.Count,
            withEmail,
            withoutEmail
        });
    }

    /// <summary>
    /// Anket durumlarını getir (tamamlayanlar/tamamlamayanlar)
    /// </summary>
    [HttpGet("{projectId}/status")]
    public async Task<IActionResult> GetSurveyStatus(int projectId)
    {
        var project = await _surveyService.GetProjectAsync(projectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Proje kapsamındaki tüm personelleri getir
        var allPersonnel = await _surveyService.GetProjectPersonnelWithCustomerAsync(project.CustomerId, project.OrganizationId);

        // Tamamlayan personelleri bul
        var completedAssignments = await _surveyService.GetCompletedAssignmentsWithScoresAsync(projectId);

        var personnelStatus = allPersonnel.Select(p => new
        {
            p.Id,
            FullName = $"{p.FirstName} {p.LastName}".Trim(),
            p.Email,
            p.Department,
            IsCompleted = completedAssignments.ContainsKey(p.Id),
            CompletedAt = completedAssignments.TryGetValue(p.Id, out var data) ? data.CompletedAt : null,
            Score = completedAssignments.TryGetValue(p.Id, out var data2) ? data2.Score : null
        })
        .OrderBy(p => p.IsCompleted)
        .ThenBy(p => p.FullName)
        .ToList();

        var stats = new
        {
            total = allPersonnel.Count,
            completed = completedAssignments.Count,
            pending = allPersonnel.Count - completedAssignments.Count,
            completionRate = allPersonnel.Count > 0
                ? Math.Round((decimal)completedAssignments.Count / allPersonnel.Count * 100, 1)
                : 0
        };

        return Ok(new { stats, personnel = personnelStatus });
    }

    /// <summary>
    /// Hatırlatma maili gönder (tamamlamayanlara)
    /// </summary>
    [HttpPost("{projectId}/send-reminders")]
    public async Task<IActionResult> SendReminders(int projectId, [FromBody] SendSurveyInvitationsDto dto)
    {
        dto.SendReminders = false; // Tamamlayanları atla
        return await SendInvitations(projectId, dto);
    }

    /// <summary>
    /// Proje için davetiye istatistiklerini getir
    /// </summary>
    [HttpGet("{projectId}/invitation-stats")]
    public async Task<IActionResult> GetInvitationStats(int projectId)
    {
        var project = await _surveyService.GetProjectAsync(projectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        var invitations = await _surveyService.GetInvitationsRawAsync(projectId);

        var stats = new
        {
            total = invitations.Count,
            sent = invitations.Count(i => i.StatusId == SurveyInvitationStatuses.Ids.Sent),
            failed = invitations.Count(i => i.StatusId == SurveyInvitationStatuses.Ids.Failed),
            pending = invitations.Count(i => i.StatusId == SurveyInvitationStatuses.Ids.Pending),
            opened = invitations.Count(i => i.IsOpened),
            completed = invitations.Count(i => i.IsCompleted),
            // Her durum için Türkçe adlar ve badge'ler
            statuses = new[]
            {
                new { statusId = SurveyInvitationStatuses.Ids.Sent, displayName = SurveyInvitationStatuses.Sent.Description, badgeClass = SurveyInvitationStatuses.Sent.CssClass, count = invitations.Count(i => i.StatusId == SurveyInvitationStatuses.Ids.Sent) },
                new { statusId = SurveyInvitationStatuses.Ids.Failed, displayName = SurveyInvitationStatuses.Failed.Description, badgeClass = SurveyInvitationStatuses.Failed.CssClass, count = invitations.Count(i => i.StatusId == SurveyInvitationStatuses.Ids.Failed) },
                new { statusId = SurveyInvitationStatuses.Ids.Pending, displayName = SurveyInvitationStatuses.Pending.Description, badgeClass = SurveyInvitationStatuses.Pending.CssClass, count = invitations.Count(i => i.StatusId == SurveyInvitationStatuses.Ids.Pending) }
            }
        };

        return Ok(stats);
    }

    /// <summary>
    /// Proje için davetiye listesini getir
    /// </summary>
    [HttpGet("{projectId}/invitations")]
    public async Task<IActionResult> GetInvitations(int projectId, [FromQuery] int? statusId = null)
    {
        var project = await _surveyService.GetProjectAsync(projectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        var invitations = await _surveyService.GetInvitationsAsync(projectId, statusId);

        return Ok(invitations);
    }

    /// <summary>
    /// Başarısız davetiyeleri tekrar gönder
    /// </summary>
    [HttpPost("{projectId}/retry-failed")]
    public async Task<IActionResult> RetryFailedInvitations(int projectId, [FromBody] SendSurveyInvitationsDto dto)
    {
        var project = await _surveyService.GetProjectWithDetailsAsync(projectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Email şablonu kontrolü
        var emailTemplate = project.EmailTemplate;
        if (dto.EmailTemplateId.HasValue)
        {
            emailTemplate = await _surveyService.GetEmailTemplateAsync(dto.EmailTemplateId.Value);
        }

        if (emailTemplate == null)
        {
            return BadRequest(new { message = "Email şablonu bulunamadı." });
        }

        // SMTP yapılandırması kontrolü
        if (!await _emailService.IsConfiguredAsync())
        {
            return BadRequest(new { message = "SMTP ayarları yapılandırılmamış." });
        }

        // Başarısız davetiyeleri getir
        var failedInvitations = await _surveyService.GetFailedInvitationsAsync(projectId);

        if (!failedInvitations.Any())
        {
            return Ok(new { success = true, message = "Yeniden gönderilecek başarısız davetiye yok." });
        }

        var successCount = 0;
        var failCount = 0;
        var errors = new List<string>();

        foreach (var invitation in failedInvitations)
        {
            try
            {
                var person = invitation.CustomerPersonnel;
                if (person == null) continue;

                // Encrypted token oluştur
                var token = EncryptionHelper.CreateSurveyToken(projectId, person.Id);

                // Kişiye özel anket URL'i oluştur
                var surveyUrl = $"{dto.BaseUrl.TrimEnd('/')}?token={token}";

                // Placeholder değişimleri
                var subject = ReplacePlaceholders(emailTemplate.Subject, project, person, surveyUrl);
                var body = ReplacePlaceholders(emailTemplate.Body, project, person, surveyUrl);

                var result = await _emailService.SendEmailAsync(person.Email!, subject, body, true);

                if (result.Success)
                {
                    await _surveyService.UpdateInvitationRetryAsync(invitation, SurveyInvitationStatuses.Ids.Sent);
                    successCount++;
                    _logger.LogInformation("Retry: Survey invitation sent to {Email} for project {ProjectId}",
                        person.Email, projectId);
                }
                else
                {
                    await _surveyService.UpdateInvitationRetryAsync(invitation, SurveyInvitationStatuses.Ids.Failed, result.ErrorMessage);
                    failCount++;
                    errors.Add($"{person.Email}: {result.ErrorMessage}");
                    _logger.LogWarning("Retry failed for {Email}: {Error}", person.Email, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"{invitation.Email}: {ex.Message}");
                _logger.LogError(ex, "Error retrying invitation to {Email}", invitation.Email);
            }
        }

        return Ok(new
        {
            success = true,
            totalRetried = failedInvitations.Count,
            successCount,
            failCount,
            errors = errors.Take(10).ToList(),
            message = $"{successCount} davetiye başarıyla gönderildi." +
                      (failCount > 0 ? $" {failCount} hala başarısız." : "")
        });
    }

    /// <summary>
    /// Placeholder'ları gerçek değerlerle değiştir
    /// </summary>
    private string ReplacePlaceholders(string text, Project project, CustomerPersonnel person, string surveyUrl)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Anket Linkleri
        text = text.Replace(EmailPlaceholders.SurveyUrl, surveyUrl); // Düz URL
        text = text.Replace(EmailPlaceholders.SurveyLink, GenerateHtmlLink(surveyUrl)); // HTML link olarak (<a href="url">url</a>)
        text = text.Replace(EmailPlaceholders.SurveyQRCode, GenerateQRCodeHtml(surveyUrl));

        // Firma/Organizasyon
        text = text.Replace(EmailPlaceholders.CompanyName, project.Customer?.CompanyName ?? "");
        text = text.Replace(EmailPlaceholders.OrganizationName, project.Organization?.Name ?? "");

        // Alıcı Bilgileri
        text = text.Replace(EmailPlaceholders.RecipientName, $"{person.FirstName} {person.LastName}".Trim());
        text = text.Replace(EmailPlaceholders.RecipientFirstName, person.FirstName ?? "");
        text = text.Replace(EmailPlaceholders.RecipientLastName, person.LastName ?? "");
        text = text.Replace(EmailPlaceholders.RecipientEmail, person.Email ?? "");

        // Proje/Anket Bilgileri
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
    /// Tıklanabilir HTML link oluştur (email placeholder için)
    /// Format: &lt;a href="url"&gt;url&lt;/a&gt;
    /// </summary>
    private string GenerateHtmlLink(string url)
    {
        // Tıklanabilir link - mobil cihazlar yanlış parse etmesin
        return $@"<a href=""{url}"" target=""_blank"">{url}</a>";
    }

    /// <summary>
    /// QR kod HTML'i oluştur (placeholder için)
    /// Base64 embedded image kullanır - email istemcileri için daha güvenilir
    /// </summary>
    private string GenerateQRCodeHtml(string url)
    {
        try
        {
            var qrBytes = _qrCodeService.GenerateQRCode(url, pixelPerModule: 5);
            var base64 = Convert.ToBase64String(qrBytes);
            return $"<img src='data:image/png;base64,{base64}' alt='QR Code' style='width:150px;height:150px;' />";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "QR kod oluşturulamadı: {Url}", url);
            // Fallback: link olarak göster
            return $"<a href='{url}'>Ankete Git</a>";
        }
    }

    #region External Invitations (Email Listesi)

    /// <summary>
    /// Dış email listesine anket davetiyesi gönder
    /// </summary>
    [HttpPost("{projectId}/send-external-invitations")]
    public async Task<IActionResult> SendExternalInvitations(int projectId, [FromBody] SendExternalInvitationsDto dto)
    {
        // Projeyi kontrol et
        var project = await _surveyService.GetProjectWithDetailsAsync(projectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Proje tipini kontrol et
        if (project.ProjectTypeId != ProjectTypes.Ids.OnlineSurvey)
        {
            return BadRequest(new { message = "Bu proje bir online anket projesi değil." });
        }

        // Email şablonu kontrolü
        var emailTemplate = project.EmailTemplate;
        if (dto.EmailTemplateId.HasValue)
        {
            emailTemplate = await _surveyService.GetEmailTemplateAsync(dto.EmailTemplateId.Value);
        }

        if (emailTemplate == null)
        {
            return BadRequest(new { message = "Email şablonu seçilmemiş." });
        }

        // SMTP yapılandırması kontrolü
        if (!await _emailService.IsConfiguredAsync())
        {
            return BadRequest(new { message = "SMTP ayarları yapılandırılmamış." });
        }

        // Base URL kontrolü
        if (string.IsNullOrWhiteSpace(dto.BaseUrl))
        {
            return BadRequest(new { message = "Anket base URL'i belirtilmemiş." });
        }

        // Email listesini parse et
        var recipients = ParseEmailInput(dto.Emails);
        if (!recipients.Any())
        {
            return BadRequest(new { message = "Geçerli email adresi bulunamadı." });
        }

        // Gönderim sonuçları
        var successCount = 0;
        var failCount = 0;
        var duplicateCount = 0;
        var errors = new List<string>();

        foreach (var recipient in recipients)
        {
            try
            {
                // Aynı projeye daha önce davetiye gönderilmiş mi?
                var exists = await _surveyService.ExternalInvitationExistsAsync(projectId, recipient.Email);

                if (exists)
                {
                    duplicateCount++;
                    continue;
                }

                // Benzersiz token oluştur
                var token = Guid.NewGuid().ToString("N");

                // SurveyExternalInvitation kaydı oluştur
                var invitation = await _surveyService.CreateExternalInvitationAsync(projectId, recipient.Email, recipient.FirstName, recipient.LastName, token);

                // Kişiye özel anket URL'i oluştur
                var surveyUrl = $"{dto.BaseUrl.TrimEnd('/')}?token={token}";

                // Placeholder değişimleri
                var subject = ReplaceExternalPlaceholders(emailTemplate.Subject, project, recipient, surveyUrl);
                var body = ReplaceExternalPlaceholders(emailTemplate.Body, project, recipient, surveyUrl);

                var result = await _emailService.SendEmailAsync(recipient.Email, subject, body, true);

                if (result.Success)
                {
                    await _surveyService.UpdateExternalInvitationStatusAsync(invitation, SurveyInvitationStatuses.Ids.Sent);
                    successCount++;
                    _logger.LogInformation("External survey invitation sent to {Email} for project {ProjectId}",
                        recipient.Email, projectId);
                }
                else
                {
                    await _surveyService.UpdateExternalInvitationStatusAsync(invitation, SurveyInvitationStatuses.Ids.Failed, result.ErrorMessage);
                    failCount++;
                    errors.Add($"{recipient.Email}: {result.ErrorMessage}");
                    _logger.LogWarning("Failed to send external survey invitation to {Email}: {Error}",
                        recipient.Email, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"{recipient.Email}: {ex.Message}");
                _logger.LogError(ex, "Error sending external survey invitation to {Email}", recipient.Email);
            }
        }

        return Ok(new
        {
            success = true,
            totalEmails = recipients.Count,
            successCount,
            failCount,
            duplicateCount,
            errors = errors.Take(10).ToList(),
            message = $"{successCount} kişiye davetiye gönderildi." +
                      (failCount > 0 ? $" {failCount} başarısız." : "") +
                      (duplicateCount > 0 ? $" {duplicateCount} zaten mevcut." : "")
        });
    }

    /// <summary>
    /// Dış davetiye listesini getir
    /// </summary>
    [HttpGet("{projectId}/external-invitations")]
    public async Task<IActionResult> GetExternalInvitations(int projectId, [FromQuery] int? statusId = null)
    {
        var project = await _surveyService.GetProjectAsync(projectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        var invitations = await _surveyService.GetExternalInvitationsAsync(projectId, statusId);

        return Ok(invitations);
    }

    /// <summary>
    /// Dış davetiye istatistiklerini getir
    /// </summary>
    [HttpGet("{projectId}/external-invitation-stats")]
    public async Task<IActionResult> GetExternalInvitationStats(int projectId)
    {
        var project = await _surveyService.GetProjectAsync(projectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        var invitations = await _surveyService.GetExternalInvitationsRawAsync(projectId);

        var stats = new
        {
            total = invitations.Count,
            sent = invitations.Count(i => i.StatusId == SurveyInvitationStatuses.Ids.Sent),
            failed = invitations.Count(i => i.StatusId == SurveyInvitationStatuses.Ids.Failed),
            pending = invitations.Count(i => i.StatusId == SurveyInvitationStatuses.Ids.Pending),
            opened = invitations.Count(i => i.IsOpened),
            completed = invitations.Count(i => i.IsCompleted),
            completionRate = invitations.Count > 0
                ? Math.Round((decimal)invitations.Count(i => i.IsCompleted) / invitations.Count * 100, 1)
                : 0
        };

        return Ok(stats);
    }

    /// <summary>
    /// Başarısız dış davetiyeleri tekrar gönder
    /// </summary>
    [HttpPost("{projectId}/retry-external-failed")]
    public async Task<IActionResult> RetryExternalFailedInvitations(int projectId, [FromBody] SendExternalInvitationsDto dto)
    {
        var project = await _surveyService.GetProjectWithDetailsAsync(projectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Email şablonu kontrolü
        var emailTemplate = project.EmailTemplate;
        if (dto.EmailTemplateId.HasValue)
        {
            emailTemplate = await _surveyService.GetEmailTemplateAsync(dto.EmailTemplateId.Value);
        }

        if (emailTemplate == null)
        {
            return BadRequest(new { message = "Email şablonu bulunamadı." });
        }

        // SMTP yapılandırması kontrolü
        if (!await _emailService.IsConfiguredAsync())
        {
            return BadRequest(new { message = "SMTP ayarları yapılandırılmamış." });
        }

        // Başarısız davetiyeleri getir
        var failedInvitations = await _surveyService.GetFailedExternalInvitationsAsync(projectId);

        if (!failedInvitations.Any())
        {
            return Ok(new { success = true, message = "Yeniden gönderilecek başarısız davetiye yok." });
        }

        var successCount = 0;
        var failCount = 0;
        var errors = new List<string>();

        foreach (var invitation in failedInvitations)
        {
            try
            {
                // Kişiye özel anket URL'i oluştur
                var surveyUrl = $"{dto.BaseUrl.TrimEnd('/')}?token={invitation.Token}";

                var recipient = new ExternalRecipient
                {
                    Email = invitation.Email,
                    FirstName = invitation.FirstName,
                    LastName = invitation.LastName
                };

                // Placeholder değişimleri
                var subject = ReplaceExternalPlaceholders(emailTemplate.Subject, project, recipient, surveyUrl);
                var body = ReplaceExternalPlaceholders(emailTemplate.Body, project, recipient, surveyUrl);

                var result = await _emailService.SendEmailAsync(invitation.Email, subject, body, true);

                if (result.Success)
                {
                    await _surveyService.UpdateExternalInvitationRetryAsync(invitation, SurveyInvitationStatuses.Ids.Sent);
                    successCount++;
                    _logger.LogInformation("Retry: External survey invitation sent to {Email} for project {ProjectId}",
                        invitation.Email, projectId);
                }
                else
                {
                    await _surveyService.UpdateExternalInvitationRetryAsync(invitation, SurveyInvitationStatuses.Ids.Failed, result.ErrorMessage);
                    failCount++;
                    errors.Add($"{invitation.Email}: {result.ErrorMessage}");
                    _logger.LogWarning("Retry failed for external {Email}: {Error}",
                        invitation.Email, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"{invitation.Email}: {ex.Message}");
                _logger.LogError(ex, "Error retrying external invitation to {Email}", invitation.Email);
            }
        }

        return Ok(new
        {
            success = true,
            totalRetried = failedInvitations.Count,
            successCount,
            failCount,
            errors = errors.Take(10).ToList(),
            message = $"{successCount} davetiye başarıyla gönderildi." +
                      (failCount > 0 ? $" {failCount} hala başarısız." : "")
        });
    }

    /// <summary>
    /// Dış katılımcılara hatırlatma gönder
    /// filter: "all" = Tümü, "completed" = Tamamlananlar, "notCompleted" = Tamamlanmayanlar (varsayılan)
    /// </summary>
    [HttpPost("{projectId}/send-external-reminders")]
    public async Task<IActionResult> SendExternalReminders(int projectId, [FromBody] SendExternalRemindersDto dto)
    {
        var project = await _surveyService.GetProjectWithDetailsAsync(projectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Email şablonu kontrolü
        var emailTemplate = project.EmailTemplate;
        if (dto.EmailTemplateId.HasValue)
        {
            emailTemplate = await _surveyService.GetEmailTemplateAsync(dto.EmailTemplateId.Value);
        }

        if (emailTemplate == null)
        {
            return BadRequest(new { message = "Email şablonu bulunamadı." });
        }

        // SMTP yapılandırması kontrolü
        if (!await _emailService.IsConfiguredAsync())
        {
            return BadRequest(new { message = "SMTP ayarları yapılandırılmamış." });
        }

        // Base URL kontrolü
        if (string.IsNullOrWhiteSpace(dto.BaseUrl))
        {
            return BadRequest(new { message = "Base URL belirtilmemiş." });
        }

        // Davetiyeleri filtrele
        var invitations = await _surveyService.GetExternalInvitationsForReminderAsync(projectId, dto.Filter);

        if (!invitations.Any())
        {
            return Ok(new { success = true, message = "Hatırlatma gönderilecek davetiye bulunamadı." });
        }

        var successCount = 0;
        var failCount = 0;
        var errors = new List<string>();

        foreach (var invitation in invitations)
        {
            try
            {
                // Kişiye özel anket URL'i oluştur
                var surveyUrl = $"{dto.BaseUrl.TrimEnd('/')}?token={invitation.Token}";

                var recipient = new ExternalRecipient
                {
                    Email = invitation.Email,
                    FirstName = invitation.FirstName,
                    LastName = invitation.LastName
                };

                // Placeholder değişimleri
                var subject = ReplaceExternalPlaceholders(emailTemplate.Subject, project, recipient, surveyUrl);
                var body = ReplaceExternalPlaceholders(emailTemplate.Body, project, recipient, surveyUrl);

                var result = await _emailService.SendEmailAsync(invitation.Email, subject, body, true);

                if (result.Success)
                {
                    await _surveyService.UpdateExternalInvitationReminderAsync(invitation, true);
                    successCount++;
                    _logger.LogInformation("Reminder sent to external {Email} for project {ProjectId}",
                        invitation.Email, projectId);
                }
                else
                {
                    await _surveyService.UpdateExternalInvitationReminderAsync(invitation, false);
                    failCount++;
                    errors.Add($"{invitation.Email}: {result.ErrorMessage}");
                    _logger.LogWarning("Reminder failed for external {Email}: {Error}",
                        invitation.Email, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"{invitation.Email}: {ex.Message}");
                _logger.LogError(ex, "Error sending reminder to external {Email}", invitation.Email);
            }
        }

        return Ok(new
        {
            success = true,
            totalSent = invitations.Count,
            successCount,
            failCount,
            errors = errors.Take(10).ToList(),
            message = $"{successCount} hatırlatma başarıyla gönderildi." +
                      (failCount > 0 ? $" {failCount} başarısız." : "")
        });
    }

    /// <summary>
    /// CSV/Excel dosyasından email listesi yükle ve davetiye gönder
    /// </summary>
    [HttpPost("{projectId}/upload-external-emails")]
    public async Task<IActionResult> UploadExternalEmails(int projectId, IFormFile file, [FromForm] int? emailTemplateId = null, [FromForm] bool sendImmediately = true)
    {
        // Projeyi kontrol et
        var project = await _surveyService.GetProjectWithDetailsAsync(projectId);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Proje tipini kontrol et
        if (project.ProjectTypeId != ProjectTypes.Ids.OnlineSurvey)
        {
            return BadRequest(new { message = "Bu proje bir online anket projesi değil." });
        }

        // Dosya kontrolü
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Dosya seçilmedi." });
        }

        // Dosya uzantısı kontrolü
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".csv" && extension != ".xlsx" && extension != ".xls")
        {
            return BadRequest(new { message = "Sadece CSV ve Excel (.xlsx, .xls) dosyaları desteklenir." });
        }

        // Email şablonu kontrolü (gönderim yapılacaksa)
        EmailTemplate? emailTemplate = null;
        if (sendImmediately)
        {
            emailTemplate = project.EmailTemplate;
            if (emailTemplateId.HasValue)
            {
                emailTemplate = await _surveyService.GetEmailTemplateAsync(emailTemplateId.Value);
            }

            if (emailTemplate == null)
            {
                return BadRequest(new { message = "Email şablonu seçilmemiş." });
            }

            // SMTP yapılandırması kontrolü
            if (!await _emailService.IsConfiguredAsync())
            {
                return BadRequest(new { message = "SMTP ayarları yapılandırılmamış." });
            }
        }

        // Dosyayı parse et
        List<ExternalRecipient> recipients;
        try
        {
            if (extension == ".csv")
            {
                recipients = await ParseCsvFileAsync(file);
            }
            else
            {
                recipients = ParseExcelFile(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing file {FileName}", file.FileName);
            return BadRequest(new { message = $"Dosya okunamadı: {ex.Message}" });
        }

        if (!recipients.Any())
        {
            return BadRequest(new { message = "Dosyada geçerli email adresi bulunamadı." });
        }

        // Gönderim sonuçları
        var successCount = 0;
        var failCount = 0;
        var duplicateCount = 0;
        var addedCount = 0;
        var errors = new List<string>();
        var baseUrl = $"{Request.Scheme}://{Request.Host}/Survey/Form";

        foreach (var recipient in recipients)
        {
            try
            {
                // Aynı projeye daha önce davetiye gönderilmiş mi?
                var exists = await _surveyService.ExternalInvitationExistsAsync(projectId, recipient.Email);

                if (exists)
                {
                    duplicateCount++;
                    continue;
                }

                // Benzersiz token oluştur
                var token = Guid.NewGuid().ToString("N");

                // SurveyExternalInvitation kaydı oluştur
                var invitation = await _surveyService.CreateExternalInvitationAsync(projectId, recipient.Email, recipient.FirstName, recipient.LastName, token);
                addedCount++;

                // Hemen gönder
                if (sendImmediately && emailTemplate != null)
                {
                    var surveyUrl = $"{baseUrl}?token={token}";

                    var subject = ReplaceExternalPlaceholders(emailTemplate.Subject, project, recipient, surveyUrl);
                    var body = ReplaceExternalPlaceholders(emailTemplate.Body, project, recipient, surveyUrl);

                    var result = await _emailService.SendEmailAsync(recipient.Email, subject, body, true);

                    if (result.Success)
                    {
                        await _surveyService.UpdateExternalInvitationStatusAsync(invitation, SurveyInvitationStatuses.Ids.Sent);
                        successCount++;
                        _logger.LogInformation("External survey invitation sent to {Email} for project {ProjectId} (file upload)",
                            recipient.Email, projectId);
                    }
                    else
                    {
                        await _surveyService.UpdateExternalInvitationStatusAsync(invitation, SurveyInvitationStatuses.Ids.Failed, result.ErrorMessage);
                        failCount++;
                        errors.Add($"{recipient.Email}: {result.ErrorMessage}");
                        _logger.LogWarning("Failed to send external survey invitation to {Email}: {Error}",
                            recipient.Email, result.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                failCount++;
                errors.Add($"{recipient.Email}: {ex.Message}");
                _logger.LogError(ex, "Error processing external invitation for {Email}", recipient.Email);
            }
        }

        var message = sendImmediately
            ? $"{successCount} kişiye davetiye gönderildi." +
              (failCount > 0 ? $" {failCount} başarısız." : "") +
              (duplicateCount > 0 ? $" {duplicateCount} zaten mevcut." : "")
            : $"{addedCount} email adresi listeye eklendi." +
              (duplicateCount > 0 ? $" {duplicateCount} zaten mevcut." : "");

        return Ok(new
        {
            success = true,
            totalEmails = recipients.Count,
            addedCount,
            successCount = sendImmediately ? successCount : 0,
            failCount = sendImmediately ? failCount : 0,
            duplicateCount,
            errors = errors.Take(10).ToList(),
            message
        });
    }

    /// <summary>
    /// CSV dosyasını parse et
    /// </summary>
    private async Task<List<ExternalRecipient>> ParseCsvFileAsync(IFormFile file)
    {
        var recipients = new List<ExternalRecipient>();

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }

        if (lines.Count < 1)
            return recipients;

        // Header var mı kontrol et
        var firstLine = lines[0].ToLowerInvariant();
        var hasHeader = firstLine.Contains("email") || firstLine.Contains("mail") ||
                        firstLine.Contains("firstname") || firstLine.Contains("ad");

        var startIndex = hasHeader ? 1 : 0;

        // Header'dan kolon indekslerini al
        var emailIndex = 0;
        var firstNameIndex = -1;
        var lastNameIndex = -1;

        if (hasHeader)
        {
            var header = ParseCsvLine(lines[0]);
            for (int i = 0; i < header.Length; i++)
            {
                var col = header[i].ToLowerInvariant().Trim();
                if (col == "email" || col == "e-mail" || col == "mail" || col == "eposta" || col == "e-posta")
                    emailIndex = i;
                else if (col == "firstname" || col == "ad" || col == "first_name" || col == "isim")
                    firstNameIndex = i;
                else if (col == "lastname" || col == "soyad" || col == "last_name" || col == "soyisim")
                    lastNameIndex = i;
                else if (col == "fullname" || col == "adsoyad" || col == "ad soyad" || col == "name" || col == "isim")
                {
                    // FullName varsa, firstName olarak al, sonra parse edilecek
                    if (firstNameIndex == -1) firstNameIndex = i;
                }
            }
        }

        // Satırları işle
        for (int i = startIndex; i < lines.Count; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length == 0) continue;

            var email = emailIndex < values.Length ? values[emailIndex].Trim().ToLower() : "";
            if (!IsValidEmail(email)) continue;

            string? firstName = null;
            string? lastName = null;

            if (firstNameIndex >= 0 && firstNameIndex < values.Length)
            {
                var nameValue = values[firstNameIndex].Trim();

                // Eğer lastName kolonu yoksa, firstName'i parse et
                if (lastNameIndex < 0 && !string.IsNullOrEmpty(nameValue))
                {
                    var parts = nameValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1) firstName = parts[0];
                    if (parts.Length >= 2) lastName = string.Join(" ", parts.Skip(1));
                }
                else
                {
                    firstName = nameValue;
                }
            }

            if (lastNameIndex >= 0 && lastNameIndex < values.Length)
            {
                lastName = values[lastNameIndex].Trim();
            }

            recipients.Add(new ExternalRecipient
            {
                Email = email,
                FirstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName,
                LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName
            });
        }

        // Duplicate'leri kaldır
        return recipients.GroupBy(r => r.Email).Select(g => g.First()).ToList();
    }

    /// <summary>
    /// CSV satırını parse et
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if ((c == ',' || c == ';' || c == '\t') && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString().Trim());
        return result.ToArray();
    }

    /// <summary>
    /// Excel dosyasını parse et
    /// </summary>
    private List<ExternalRecipient> ParseExcelFile(IFormFile file)
    {
        var recipients = new List<ExternalRecipient>();

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
            return recipients;

        var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        if (rowCount < 1)
            return recipients;

        // Header'dan kolon indekslerini al
        var emailCol = 1;
        var firstNameCol = -1;
        var lastNameCol = -1;
        var fullNameCol = -1;
        var hasHeader = false;

        // İlk satırı kontrol et
        var firstCell = worksheet.Cell(1, 1).GetString().ToLowerInvariant();
        if (firstCell.Contains("email") || firstCell.Contains("mail") ||
            firstCell.Contains("ad") || firstCell.Contains("name"))
        {
            hasHeader = true;
            var colCount = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;

            for (int col = 1; col <= colCount; col++)
            {
                var header = worksheet.Cell(1, col).GetString().ToLowerInvariant().Trim();
                if (header == "email" || header == "e-mail" || header == "mail" || header == "eposta" || header == "e-posta")
                    emailCol = col;
                else if (header == "firstname" || header == "ad" || header == "first_name" || header == "isim")
                    firstNameCol = col;
                else if (header == "lastname" || header == "soyad" || header == "last_name" || header == "soyisim")
                    lastNameCol = col;
                else if (header == "fullname" || header == "adsoyad" || header == "ad soyad" || header == "name")
                    fullNameCol = col;
            }
        }

        var startRow = hasHeader ? 2 : 1;

        for (int row = startRow; row <= rowCount; row++)
        {
            var email = worksheet.Cell(row, emailCol).GetString().Trim().ToLower();
            if (!IsValidEmail(email)) continue;

            string? firstName = null;
            string? lastName = null;

            if (fullNameCol > 0)
            {
                var fullName = worksheet.Cell(row, fullNameCol).GetString().Trim();
                if (!string.IsNullOrEmpty(fullName))
                {
                    var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1) firstName = parts[0];
                    if (parts.Length >= 2) lastName = string.Join(" ", parts.Skip(1));
                }
            }

            if (firstNameCol > 0)
            {
                var fn = worksheet.Cell(row, firstNameCol).GetString().Trim();
                if (!string.IsNullOrEmpty(fn))
                {
                    // Eğer lastName kolonu yoksa, firstName'i parse et
                    if (lastNameCol <= 0)
                    {
                        var parts = fn.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 1) firstName = parts[0];
                        if (parts.Length >= 2) lastName = string.Join(" ", parts.Skip(1));
                    }
                    else
                    {
                        firstName = fn;
                    }
                }
            }

            if (lastNameCol > 0)
            {
                var ln = worksheet.Cell(row, lastNameCol).GetString().Trim();
                if (!string.IsNullOrEmpty(ln)) lastName = ln;
            }

            recipients.Add(new ExternalRecipient
            {
                Email = email,
                FirstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName,
                LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName
            });
        }

        // Duplicate'leri kaldır
        return recipients.GroupBy(r => r.Email).Select(g => g.First()).ToList();
    }

    /// <summary>
    /// Email şablonu indir (CSV/Excel için örnek)
    /// </summary>
    [HttpGet("external-email-template")]
    public IActionResult GetExternalEmailTemplate([FromQuery] string format = "csv")
    {
        if (format.ToLowerInvariant() == "xlsx" || format.ToLowerInvariant() == "excel")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Email Listesi");

            // Header
            worksheet.Cell(1, 1).Value = "Email";
            worksheet.Cell(1, 2).Value = "Ad";
            worksheet.Cell(1, 3).Value = "Soyad";

            // Örnek satır
            worksheet.Cell(2, 1).Value = "ornek@email.com";
            worksheet.Cell(2, 2).Value = "Ahmet";
            worksheet.Cell(2, 3).Value = "Yılmaz";

            worksheet.Columns().AdjustToContents();
            ExcelHelper.ApplyLongTextColumnStyles(worksheet);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "email_listesi_sablonu.xlsx");
        }
        else
        {
            var csv = "Email,Ad,Soyad\nornek@email.com,Ahmet,Yılmaz";
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "email_listesi_sablonu.csv");
        }
    }

    /// <summary>
    /// Email girdisini parse et (virgül, noktalı virgül veya satır ile ayrılmış)
    /// Formatlar:
    /// - email@x.com
    /// - email1@x.com, email2@x.com
    /// - email1@x.com; email2@x.com
    /// - email@x.com Ahmet Yılmaz (email sonrası isim)
    /// - Ahmet Yılmaz email@x.com (email öncesi isim)
    /// - Ahmet Yılmaz &lt;email@x.com&gt; (açılı parantez formatı)
    /// </summary>
    private List<ExternalRecipient> ParseEmailInput(string input)
    {
        var results = new List<ExternalRecipient>();
        if (string.IsNullOrWhiteSpace(input)) return results;

        // Tüm ayırıcıları normalize et: virgül, noktalı virgül, yeni satır → yeni satır
        var normalized = input
            .Replace(",", "\n")
            .Replace(";", "\n");

        var lines = normalized.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // <email> formatını kontrol et: "Ahmet Yılmaz <ahmet@example.com>"
            var angleMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"^(.+?)\s*<([^>]+)>$");
            if (angleMatch.Success)
            {
                var namePart = angleMatch.Groups[1].Value.Trim();
                var emailPart = angleMatch.Groups[2].Value.Trim();

                if (IsValidEmail(emailPart))
                {
                    var nameTokens = namePart.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                    results.Add(CreateRecipient(emailPart.ToLower(), nameTokens));
                    continue;
                }
            }

            // Satırı boşluk ile tokenize et
            var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string? email = null;
            var tokensBeforeEmail = new List<string>();
            var tokensAfterEmail = new List<string>();

            foreach (var tokenItem in tokens)
            {
                // <> işaretlerini temizle
                var cleanToken = tokenItem.Trim('<', '>');

                if (email == null && IsValidEmail(cleanToken))
                {
                    // İlk geçerli email bulundu
                    email = cleanToken.ToLower();
                }
                else if (email != null && IsValidEmail(cleanToken))
                {
                    // Yeni bir email bulundu - öncekini kaydet ve yenisine geç
                    var nameTokens = tokensBeforeEmail.Count > 0 ? tokensBeforeEmail : tokensAfterEmail;
                    results.Add(CreateRecipient(email, nameTokens));
                    email = cleanToken.ToLower();
                    tokensBeforeEmail.Clear();
                    tokensAfterEmail.Clear();
                }
                else if (email == null)
                {
                    // Email öncesi isim token'ı
                    tokensBeforeEmail.Add(tokenItem);
                }
                else
                {
                    // Email sonrası isim token'ı
                    tokensAfterEmail.Add(tokenItem);
                }
            }

            // Son email'i kaydet
            if (email != null)
            {
                // Öncelik: email öncesi isim, yoksa email sonrası isim
                var finalNameTokens = tokensBeforeEmail.Count > 0 ? tokensBeforeEmail : tokensAfterEmail;
                results.Add(CreateRecipient(email, finalNameTokens));
            }
        }

        // Duplicate'leri kaldır
        return results.GroupBy(r => r.Email).Select(g => g.First()).ToList();
    }

    private static ExternalRecipient CreateRecipient(string email, List<string> nameTokens)
    {
        string? firstName = null;
        string? lastName = null;

        if (nameTokens.Count >= 1)
        {
            firstName = nameTokens[0];
        }
        if (nameTokens.Count >= 2)
        {
            lastName = string.Join(" ", nameTokens.Skip(1));
        }

        return new ExternalRecipient
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };
    }

    /// <summary>
    /// Email formatını kontrol et
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Dış alıcı için placeholder'ları değiştir
    /// </summary>
    private string ReplaceExternalPlaceholders(string text, Project project, ExternalRecipient recipient, string surveyUrl)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Anket Linkleri
        text = text.Replace(EmailPlaceholders.SurveyUrl, surveyUrl); // Düz URL
        text = text.Replace(EmailPlaceholders.SurveyLink, GenerateHtmlLink(surveyUrl)); // HTML link olarak (<a href="url">url</a>)
        text = text.Replace(EmailPlaceholders.SurveyQRCode, GenerateQRCodeHtml(surveyUrl));

        // Firma/Organizasyon
        text = text.Replace(EmailPlaceholders.CompanyName, project.Customer?.CompanyName ?? "");
        text = text.Replace(EmailPlaceholders.OrganizationName, project.Organization?.Name ?? "");

        // Alıcı Bilgileri
        var fullName = $"{recipient.FirstName} {recipient.LastName}".Trim();
        text = text.Replace(EmailPlaceholders.RecipientName, !string.IsNullOrWhiteSpace(fullName) ? fullName : recipient.Email);
        text = text.Replace(EmailPlaceholders.RecipientFirstName, recipient.FirstName ?? "");
        text = text.Replace(EmailPlaceholders.RecipientLastName, recipient.LastName ?? "");
        text = text.Replace(EmailPlaceholders.RecipientEmail, recipient.Email);

        // Proje/Anket Bilgileri
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

    #endregion
}

#region DTOs

/// <summary>
/// Anket davetiyesi gönderme DTO
/// </summary>
public class SendSurveyInvitationsDto
{
    /// <summary>
    /// Kullanılacak email şablonu ID (null ise proje şablonu)
    /// </summary>
    public int? EmailTemplateId { get; set; }

    /// <summary>
    /// Anket form base URL'i (örn: https://example.com/Survey/Form)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Tamamlamış olanlara da hatırlatma gönder
    /// </summary>
    public bool SendReminders { get; set; } = false;
}

/// <summary>
/// Anket cevapları gönderme DTO
/// </summary>
public class SubmitSurveyDto
{
    /// <summary>
    /// Soru cevapları listesi
    /// </summary>
    public List<SurveyAnswerDto> Answers { get; set; } = new();
}

/// <summary>
/// Dış email listesine davetiye gönderme DTO
/// </summary>
public class SendExternalInvitationsDto
{
    /// <summary>
    /// Email listesi (virgül, boşluk veya satır ile ayrılmış)
    /// Format: "email1@x.com, email2@x.com" veya
    /// "email1@x.com; Ad Soyad\nemail2@x.com; Ad Soyad"
    /// </summary>
    public string Emails { get; set; } = string.Empty;

    /// <summary>
    /// Kullanılacak email şablonu ID (null ise proje şablonu)
    /// </summary>
    public int? EmailTemplateId { get; set; }

    /// <summary>
    /// Anket form base URL'i (örn: https://example.com/Survey/Form)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Dış alıcı bilgileri (email parse sonrası)
/// </summary>
public class ExternalRecipient
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

/// <summary>
/// Dış katılımcılara hatırlatma gönderme DTO
/// </summary>
public class SendExternalRemindersDto
{
    /// <summary>
    /// Anket form base URL'i (örn: https://example.com/Survey/Form)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Kullanılacak email şablonu ID (null ise proje şablonu)
    /// </summary>
    public int? EmailTemplateId { get; set; }

    /// <summary>
    /// Filtre: "all" = Tümü, "completed" = Tamamlananlar, "notCompleted" = Tamamlanmayanlar (varsayılan)
    /// </summary>
    public string? Filter { get; set; } = "notCompleted";
}

#endregion
