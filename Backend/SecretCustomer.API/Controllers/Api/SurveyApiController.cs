using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Services.Helpers;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/surveys")]
[ApiController]
[Authorize(Roles = "Admin,QualitySpecialist")]
public class SurveyApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IQRCodeService _qrCodeService;
    private readonly ILogger<SurveyApiController> _logger;

    public SurveyApiController(
        ApplicationDbContext context,
        IEmailService emailService,
        IQRCodeService qrCodeService,
        ILogger<SurveyApiController> logger)
    {
        _context = context;
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
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.Checklist)
            .Include(p => p.EmailTemplate)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

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
            emailTemplate = await _context.EmailTemplates
                .FirstOrDefaultAsync(e => e.Id == dto.EmailTemplateId.Value && !e.IsDeleted);
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
        var personnelQuery = _context.CustomerPersonnel
            .Include(p => p.Customer)
            .Include(p => p.OrganizationAssignments)
                .ThenInclude(oa => oa.CustomerOrganization)
            .Where(p => !p.IsDeleted && p.IsActive && !string.IsNullOrEmpty(p.Email));

        // Müşteri filtresi (project'in müşterisine göre)
        if (project.CustomerId.HasValue)
        {
            personnelQuery = personnelQuery.Where(p => p.CustomerId == project.CustomerId.Value);
        }

        // Organizasyon filtresi (proje organizasyonuna göre)
        if (project.OrganizationId.HasValue)
        {
            personnelQuery = personnelQuery.Where(p =>
                p.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == project.OrganizationId.Value && !oa.IsDeleted));
        }

        var personnelList = await personnelQuery.ToListAsync();

        if (!personnelList.Any())
        {
            return BadRequest(new { message = "Gönderilecek personel bulunamadı. Email adresi olan aktif personel olduğundan emin olun." });
        }

        // Zaten anketi tamamlamış kişileri bul (Evaluation var mı?)
        var completedPersonnelIds = await _context.Assignments
            .Where(a => a.ProjectId == projectId && !a.IsDeleted && a.IsCompleted)
            .Where(a => a.AssignedCustomerPersonnelId.HasValue)
            .Select(a => a.AssignedCustomerPersonnelId!.Value)
            .ToListAsync();

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
                var invitation = new SurveyInvitation
                {
                    ProjectId = projectId,
                    CustomerPersonnelId = person.Id,
                    Email = person.Email!,
                    StatusId = SurveyInvitationStatuses.Ids.Pending,
                    IsReminder = dto.SendReminders,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SurveyInvitations.Add(invitation);
                await _context.SaveChangesAsync();

                var result = await _emailService.SendEmailAsync(person.Email!, subject, body, true);

                if (result.Success)
                {
                    invitation.StatusId = SurveyInvitationStatuses.Ids.Sent;
                    invitation.SentAt = DateTime.UtcNow;
                    successCount++;
                    _logger.LogInformation("Survey invitation sent to {Email} for project {ProjectId}",
                        person.Email, projectId);
                }
                else
                {
                    invitation.StatusId = SurveyInvitationStatuses.Ids.Failed;
                    invitation.ErrorMessage = result.ErrorMessage;
                    failCount++;
                    errors.Add($"{person.Email}: {result.ErrorMessage}");
                    _logger.LogWarning("Failed to send survey invitation to {Email}: {Error}", person.Email, result.ErrorMessage);
                }
                await _context.SaveChangesAsync();
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
        var externalInvitation = await _context.SurveyExternalInvitations
            .Include(i => i.Project)
                .ThenInclude(p => p!.Checklist)
            .Include(i => i.Project)
                .ThenInclude(p => p!.Customer)
            .Include(i => i.Project)
                .ThenInclude(p => p!.Organization)
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsDeleted);

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
        var project = await _context.Projects
            .Include(p => p.Checklist)
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .FirstOrDefaultAsync(p => p.Id == tokenData.ProjectId && !p.IsDeleted);

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
        var personnel = await _context.CustomerPersonnel
            .FirstOrDefaultAsync(p => p.Id == tokenData.PersonnelId && !p.IsDeleted);

        if (personnel == null)
        {
            return NotFound(new { message = "Personel bulunamadı." });
        }

        // Bu personel için zaten değerlendirme var mı?
        var existingEvaluation = await _context.Assignments
            .Where(a => a.ProjectId == project.Id &&
                        a.AssignedCustomerPersonnelId == personnel.Id &&
                        !a.IsDeleted && a.IsCompleted)
            .AnyAsync();

        if (existingEvaluation)
        {
            return BadRequest(new { message = "Bu anket zaten tamamlanmış." });
        }

        // SurveyInvitation kaydını bul ve açıldı olarak işaretle (opsiyonel - hata verse de devam et)
        try
        {
            var invitation = await _context.SurveyInvitations
                .Where(si => si.ProjectId == tokenData.ProjectId &&
                             si.CustomerPersonnelId == tokenData.PersonnelId &&
                             si.StatusId == SurveyInvitationStatuses.Ids.Sent &&
                             !si.IsOpened)
                .OrderByDescending(si => si.CreatedAt)
                .FirstOrDefaultAsync();

            if (invitation != null)
            {
                invitation.IsOpened = true;
                invitation.OpenedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // Davet kaydı bulunamasa veya tablo yoksa devam et - anket yine de çalışmalı
            _logger.LogWarning(ex, "SurveyInvitation update failed for project {ProjectId}, personnel {PersonnelId}",
                tokenData.ProjectId, tokenData.PersonnelId);
        }

        var isAnonymous = project.SurveyIdentityTypeId == SurveyIdentityTypes.Ids.Anonymous;

        // Checklist sorularını getir (SubCriteria dahil)
        var questions = await GetSurveyQuestions(project.ChecklistId);

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
        if (project.EndDate < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Anket süresi dolmuş." });
        }

        // Bu davetiye zaten tamamlanmış mı?
        if (invitation.IsCompleted)
        {
            return BadRequest(new { message = "Bu anket zaten tamamlanmış." });
        }

        // İlk kez açılıyorsa işaretle
        if (!invitation.IsOpened)
        {
            invitation.IsOpened = true;
            invitation.OpenedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        var isAnonymous = project.SurveyIdentityTypeId == SurveyIdentityTypes.Ids.Anonymous;

        // Checklist sorularını getir
        var questions = await GetSurveyQuestions(project.ChecklistId);

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
    /// Checklist sorularını getir (ortak method)
    /// </summary>
    private async Task<object> GetSurveyQuestions(int? checklistId)
    {
        if (!checklistId.HasValue) return new List<object>();

        return await _context.Questions
            .Include(q => q.SubCriteria)
            .Where(q => q.ChecklistId == checklistId && !q.IsDeleted)
            .OrderBy(q => q.GroupName)
            .ThenBy(q => q.Order)
            .Select(q => new
            {
                q.Id,
                q.Text,
                q.HelpText,
                q.GroupName,
                q.Order,
                ScoringType = ScoringTypes.GetById(q.ScoringTypeId).SystemName,
                q.MaxPoints,
                q.WeightPoints,
                q.IsRequired,
                q.SelectionTypeId,
                q.ShowScoreInput,
                q.AllowComment,
                SubCriteria = q.SubCriteria
                    .Where(sc => sc.IsActive && !sc.IsDeleted)
                    .OrderBy(sc => sc.Order)
                    .Select(sc => new
                    {
                        sc.Id,
                        sc.Description,
                        sc.Order
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    /// <summary>
    /// Anket cevaplarını gönder ve değerlendirme oluştur (PUBLIC)
    /// </summary>
    [HttpPost("submit/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitSurvey(string token, [FromBody] SubmitSurveyDto dto)
    {
        // Önce external invitation token mı kontrol et
        var externalInvitation = await _context.SurveyExternalInvitations
            .Include(i => i.Project)
                .ThenInclude(p => p!.Checklist)
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsDeleted);

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
        var project = await _context.Projects
            .Include(p => p.Checklist)
            .FirstOrDefaultAsync(p => p.Id == tokenData.ProjectId && !p.IsDeleted);

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
        var personnel = await _context.CustomerPersonnel
            .FirstOrDefaultAsync(p => p.Id == tokenData.PersonnelId && !p.IsDeleted);

        if (personnel == null)
        {
            return NotFound(new { message = "Personel bulunamadı." });
        }

        // Bu personel için zaten değerlendirme var mı?
        var existingEvaluation = await _context.Evaluations
            .FirstOrDefaultAsync(e => e.ProjectId == project.Id &&
                        e.EvaluatedCustomerPersonnelId == personnel.Id &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        !e.IsDeleted);

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
                CompletedAt = DateTime.UtcNow,
                StatusId = EvaluationStatuses.Ids.Completed,
                Notes = "Online anket ile dolduruldu",
                CreatedAt = DateTime.UtcNow
            };

            _context.Evaluations.Add(evaluation);
            await _context.SaveChangesAsync();

            // Cevapları kaydet ve puanı hesapla
            var scoreResult = await SaveAnswersAndCalculateScore(evaluation.Id, project.ChecklistId, dto.Answers);
            evaluation.TotalScore = scoreResult.TotalScore;
            evaluation.MaxScore = scoreResult.MaxScore;
            evaluation.ScorePercentage = scoreResult.ScorePercentage;

            // SurveyInvitation kaydını tamamlandı olarak işaretle (opsiyonel)
            try
            {
                var surveyInvitation = await _context.SurveyInvitations
                    .Where(si => si.ProjectId == project.Id &&
                                 si.CustomerPersonnelId == personnel.Id &&
                                 si.StatusId == SurveyInvitationStatuses.Ids.Sent &&
                                 !si.IsCompleted)
                    .OrderByDescending(si => si.CreatedAt)
                    .FirstOrDefaultAsync();

                if (surveyInvitation != null)
                {
                    surveyInvitation.IsCompleted = true;
                    surveyInvitation.CompletedAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SurveyInvitation completion update failed for project {ProjectId}, personnel {PersonnelId}",
                    project.Id, personnel.Id);
            }

            await _context.SaveChangesAsync();

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
        if (project.EndDate < DateTime.UtcNow)
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
                CompletedAt = DateTime.UtcNow,
                StatusId = EvaluationStatuses.Ids.Completed,
                Notes = $"Dış katılımcı anketi: {invitation.Email}" +
                        (!string.IsNullOrWhiteSpace(invitation.FullName) ? $" ({invitation.FullName})" : ""),
                CreatedAt = DateTime.UtcNow
            };

            _context.Evaluations.Add(evaluation);
            await _context.SaveChangesAsync();

            // Cevapları kaydet ve puanı hesapla
            var scoreResult = await SaveAnswersAndCalculateScore(evaluation.Id, project.ChecklistId, dto.Answers);
            evaluation.TotalScore = scoreResult.TotalScore;
            evaluation.MaxScore = scoreResult.MaxScore;
            evaluation.ScorePercentage = scoreResult.ScorePercentage;

            // External invitation'ı tamamlandı olarak işaretle
            invitation.IsCompleted = true;
            invitation.CompletedAt = DateTime.UtcNow;
            invitation.EvaluationId = evaluation.Id;

            await _context.SaveChangesAsync();

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
    /// Cevapları kaydet ve toplam puanı hesapla (ortak method)
    /// Returns: (TotalScore, MaxScore, ScorePercentage)
    ///
    /// Puan hesaplama mantığı:
    /// 1. ShowScoreInput = true: Manuel puan girişi ile hesaplama (Score / MaxPoints * WeightPoints)
    /// 2. ShowScoreInput = false: SubCriteria seçimine göre hesaplama
    ///    - Tekli seçim: Seçilen SubCriteria.WeightPoints (max: Question.WeightPoints)
    ///    - Çoklu seçim: (Seçilen ağırlık toplamı / Tüm ağırlık toplamı) * Question.WeightPoints
    /// 3. Zorunlu değilse ve cevaplanmamışsa: Max puana dahil edilmez
    /// 4. Penalty sorular: Puan kesmek için kullanılır (kazanılan puandan düşer)
    /// </summary>
    private async Task<(decimal? TotalScore, decimal? MaxScore, decimal? ScorePercentage)> SaveAnswersAndCalculateScore(int evaluationId, int? checklistId, List<SurveyAnswerDto> answers)
    {
        if (!checklistId.HasValue) return (null, null, null);

        decimal totalEarned = 0;
        decimal totalMax = 0;
        decimal penaltyDeduction = 0;
        int yellowCardCount = 0;
        int redCardCount = 0;

        // Soruları SubCriteria ile birlikte yükle
        var questions = await _context.Questions
            .Include(q => q.SubCriteria.Where(sc => !sc.IsDeleted && sc.IsActive))
            .Where(q => q.ChecklistId == checklistId && !q.IsDeleted)
            .ToListAsync();

        // Cevaplanan soru ID'lerini takip et
        var answeredQuestionIds = answers
            .Where(a => a.Score.HasValue || (a.SelectedSubCriteriaIds != null && a.SelectedSubCriteriaIds.Any()))
            .Select(a => a.QuestionId)
            .ToHashSet();

        foreach (var answerDto in answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
            if (question == null) continue;

            var answer = new Answer
            {
                EvaluationId = evaluationId,
                QuestionId = answerDto.QuestionId,
                AnswerNumeric = answerDto.Score,
                Notes = answerDto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Answers.Add(answer);
            await _context.SaveChangesAsync();

            // Seçilen SubCriteria'ları kaydet
            if (answerDto.SelectedSubCriteriaIds != null && answerDto.SelectedSubCriteriaIds.Any())
            {
                foreach (var subCriteriaId in answerDto.SelectedSubCriteriaIds)
                {
                    _context.AnswerSubCriteriaSelections.Add(new AnswerSubCriteriaSelection
                    {
                        AnswerId = answer.Id,
                        SubCriteriaId = subCriteriaId,
                        SelectedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Unscored sorular puana dahil edilmez
            if (question.ScoringTypeId == ScoringTypes.Ids.Unscored)
            {
                continue;
            }

            // Soru cevaplanmış mı kontrol et
            bool isAnswered = answerDto.Score.HasValue ||
                             (answerDto.SelectedSubCriteriaIds != null && answerDto.SelectedSubCriteriaIds.Any());

            // Zorunlu değilse ve cevaplanmamışsa, max puana dahil etme
            if (!question.IsRequired && !isAnswered)
            {
                continue;
            }

            // Penalty sorular - puan kesmek için
            if (question.ScoringTypeId == ScoringTypes.Ids.Penalty)
            {
                // Penalty sorusu seçildiyse (cevaplandıysa) cezayı uygula
                if (isAnswered)
                {
                    if (question.PenaltyTypeId == PenaltyTypes.Ids.YellowCard)
                    {
                        yellowCardCount++;
                        // Sarı kart: Sorunun ağırlık puanı kadar düş
                        penaltyDeduction += question.WeightPoints;
                    }
                    else if (question.PenaltyTypeId == PenaltyTypes.Ids.RedCard)
                    {
                        redCardCount++;
                        // Kırmızı kart: Sorunun ağırlık puanı kadar düş (veya daha fazla)
                        penaltyDeduction += question.WeightPoints;
                    }
                }
                continue; // Penalty sorular max puana dahil edilmez
            }

            // Scored sorular - puan hesaplama
            if (question.ScoringTypeId == ScoringTypes.Ids.Scored)
            {
                // Max puana ekle (zorunlu veya cevaplanmış)
                totalMax += question.WeightPoints;

                decimal earnedPoints = 0;

                // ShowScoreInput = true: Manuel puan girişi
                if (question.ShowScoreInput && answerDto.Score.HasValue)
                {
                    earnedPoints = (answerDto.Score.Value / (decimal)question.MaxPoints) * question.WeightPoints;
                }
                // ShowScoreInput = false: SubCriteria bazlı hesaplama
                else if (!question.ShowScoreInput && answerDto.SelectedSubCriteriaIds != null && answerDto.SelectedSubCriteriaIds.Any())
                {
                    var allSubCriteria = question.SubCriteria.ToList();

                    if (allSubCriteria.Any())
                    {
                        // Seçilen SubCriteria'ların ağırlık toplamı
                        var selectedWeight = allSubCriteria
                            .Where(sc => answerDto.SelectedSubCriteriaIds.Contains(sc.Id))
                            .Sum(sc => sc.WeightPoints);

                        // Tekli seçim (SelectionTypeId = 1)
                        if (question.SelectionTypeId == SelectionTypes.Ids.Single)
                        {
                            // Seçilen SubCriteria.WeightPoints (max: Question.WeightPoints)
                            earnedPoints = Math.Min(selectedWeight, question.WeightPoints);
                        }
                        // Çoklu seçim (SelectionTypeId = 2)
                        else
                        {
                            // Tüm SubCriteria'ların ağırlık toplamı
                            var totalSubCriteriaWeight = allSubCriteria.Sum(sc => sc.WeightPoints);

                            if (totalSubCriteriaWeight > 0)
                            {
                                // (Seçilen / Toplam) * Question.WeightPoints
                                earnedPoints = (selectedWeight / totalSubCriteriaWeight) * question.WeightPoints;
                                // Max: Question.WeightPoints
                                earnedPoints = Math.Min(earnedPoints, question.WeightPoints);
                            }
                        }
                    }
                }

                // EarnedPoints'i Answer'a kaydet (raporlama için)
                answer.EarnedPoints = Math.Round(earnedPoints, 2);

                totalEarned += earnedPoints;
            }
        }

        await _context.SaveChangesAsync();

        // Penalty kesintisini uygula
        totalEarned = Math.Max(0, totalEarned - penaltyDeduction);

        // Evaluation'a kart sayılarını kaydet
        var evaluation = await _context.Evaluations.FindAsync(evaluationId);
        if (evaluation != null)
        {
            evaluation.YellowCardCount = yellowCardCount;
            evaluation.RedCardCount = redCardCount;
        }

        // Toplam puanı hesapla
        if (totalMax > 0)
        {
            var percentage = Math.Round((totalEarned / totalMax) * 100, 2);
            return (Math.Round(totalEarned, 2), Math.Round(totalMax, 2), percentage);
        }

        return (null, null, null);
    }

    /// <summary>
    /// Personel preview - kaç kişiye mail gidecek
    /// </summary>
    [HttpGet("{projectId}/personnel-preview")]
    public async Task<IActionResult> GetPersonnelPreview(int projectId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Proje kapsamındaki personelleri getir
        var personnelQuery = _context.CustomerPersonnel
            .Where(p => !p.IsDeleted && p.IsActive);

        if (project.CustomerId.HasValue)
        {
            personnelQuery = personnelQuery.Where(p => p.CustomerId == project.CustomerId.Value);
        }

        if (project.OrganizationId.HasValue)
        {
            personnelQuery = personnelQuery.Where(p =>
                p.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == project.OrganizationId.Value && !oa.IsDeleted));
        }

        var personnel = await personnelQuery.ToListAsync();

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
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Proje kapsamındaki tüm personelleri getir
        var personnelQuery = _context.CustomerPersonnel
            .Include(p => p.Customer)
            .Where(p => !p.IsDeleted && p.IsActive && !string.IsNullOrEmpty(p.Email));

        if (project.CustomerId.HasValue)
        {
            personnelQuery = personnelQuery.Where(p => p.CustomerId == project.CustomerId.Value);
        }

        if (project.OrganizationId.HasValue)
        {
            personnelQuery = personnelQuery.Where(p =>
                p.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == project.OrganizationId.Value && !oa.IsDeleted));
        }

        var allPersonnel = await personnelQuery.ToListAsync();

        // Tamamlayan personelleri bul
        var completedAssignments = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Evaluations)
            .Where(a => a.ProjectId == projectId && !a.IsDeleted && a.IsCompleted && a.AssignedCustomerPersonnelId.HasValue)
            .ToDictionaryAsync(a => a.AssignedCustomerPersonnelId!.Value, a => new
            {
                CompletedAt = a.CompletedAt,
                Score = a.Project.Evaluations.FirstOrDefault()?.ScorePercentage
            });

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
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        var invitations = await _context.SurveyInvitations
            .Where(si => si.ProjectId == projectId)
            .ToListAsync();

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
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        var query = _context.SurveyInvitations
            .Include(si => si.CustomerPersonnel)
            .Where(si => si.ProjectId == projectId);

        if (statusId.HasValue)
        {
            query = query.Where(si => si.StatusId == statusId.Value);
        }

        var invitations = await query
            .OrderByDescending(si => si.CreatedAt)
            .Select(si => new
            {
                si.Id,
                si.Email,
                PersonnelName = si.CustomerPersonnel != null
                    ? $"{si.CustomerPersonnel.FirstName} {si.CustomerPersonnel.LastName}".Trim()
                    : null,
                si.StatusId,
                si.ErrorMessage,
                si.SentAt,
                si.IsOpened,
                si.OpenedAt,
                si.IsCompleted,
                si.CompletedAt,
                si.AttemptCount,
                si.IsReminder,
                si.CreatedAt
            })
            .ToListAsync();

        return Ok(invitations);
    }

    /// <summary>
    /// Başarısız davetiyeleri tekrar gönder
    /// </summary>
    [HttpPost("{projectId}/retry-failed")]
    public async Task<IActionResult> RetryFailedInvitations(int projectId, [FromBody] SendSurveyInvitationsDto dto)
    {
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.Checklist)
            .Include(p => p.EmailTemplate)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Email şablonu kontrolü
        var emailTemplate = project.EmailTemplate;
        if (dto.EmailTemplateId.HasValue)
        {
            emailTemplate = await _context.EmailTemplates
                .FirstOrDefaultAsync(e => e.Id == dto.EmailTemplateId.Value && !e.IsDeleted);
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
        var failedInvitations = await _context.SurveyInvitations
            .Include(si => si.CustomerPersonnel)
            .Where(si => si.ProjectId == projectId && si.StatusId == SurveyInvitationStatuses.Ids.Failed)
            .ToListAsync();

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

                invitation.AttemptCount++;

                if (result.Success)
                {
                    invitation.StatusId = SurveyInvitationStatuses.Ids.Sent;
                    invitation.SentAt = DateTime.UtcNow;
                    invitation.Email = person.Email!; // Güncel email'i kaydet
                    invitation.ErrorMessage = null;
                    successCount++;
                    _logger.LogInformation("Retry: Survey invitation sent to {Email} for project {ProjectId}",
                        person.Email, projectId);
                }
                else
                {
                    invitation.ErrorMessage = result.ErrorMessage;
                    failCount++;
                    errors.Add($"{person.Email}: {result.ErrorMessage}");
                    _logger.LogWarning("Retry failed for {Email}: {Error}", person.Email, result.ErrorMessage);
                }
                await _context.SaveChangesAsync();
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
        text = text.Replace(EmailPlaceholders.CurrentDate, DateTime.Now.ToString("dd.MM.yyyy"));
        text = text.Replace(EmailPlaceholders.CurrentYear, DateTime.Now.Year.ToString());
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
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.Checklist)
            .Include(p => p.EmailTemplate)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

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
            emailTemplate = await _context.EmailTemplates
                .FirstOrDefaultAsync(e => e.Id == dto.EmailTemplateId.Value && !e.IsDeleted);
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
                var existingInvitation = await _context.SurveyExternalInvitations
                    .FirstOrDefaultAsync(i => i.ProjectId == projectId &&
                                              i.Email == recipient.Email &&
                                              !i.IsDeleted);

                if (existingInvitation != null)
                {
                    duplicateCount++;
                    continue;
                }

                // Benzersiz token oluştur
                var token = Guid.NewGuid().ToString("N");

                // SurveyExternalInvitation kaydı oluştur
                var invitation = new SurveyExternalInvitation
                {
                    ProjectId = projectId,
                    Email = recipient.Email,
                    FirstName = recipient.FirstName,
                    LastName = recipient.LastName,
                    Token = token,
                    StatusId = SurveyInvitationStatuses.Ids.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SurveyExternalInvitations.Add(invitation);
                await _context.SaveChangesAsync();

                // Kişiye özel anket URL'i oluştur
                var surveyUrl = $"{dto.BaseUrl.TrimEnd('/')}?token={token}";

                // Placeholder değişimleri
                var subject = ReplaceExternalPlaceholders(emailTemplate.Subject, project, recipient, surveyUrl);
                var body = ReplaceExternalPlaceholders(emailTemplate.Body, project, recipient, surveyUrl);

                var result = await _emailService.SendEmailAsync(recipient.Email, subject, body, true);

                if (result.Success)
                {
                    invitation.StatusId = SurveyInvitationStatuses.Ids.Sent;
                    invitation.SentAt = DateTime.UtcNow;
                    successCount++;
                    _logger.LogInformation("External survey invitation sent to {Email} for project {ProjectId}",
                        recipient.Email, projectId);
                }
                else
                {
                    invitation.StatusId = SurveyInvitationStatuses.Ids.Failed;
                    invitation.ErrorMessage = result.ErrorMessage;
                    failCount++;
                    errors.Add($"{recipient.Email}: {result.ErrorMessage}");
                    _logger.LogWarning("Failed to send external survey invitation to {Email}: {Error}",
                        recipient.Email, result.ErrorMessage);
                }
                await _context.SaveChangesAsync();
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
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        var query = _context.SurveyExternalInvitations
            .Where(si => si.ProjectId == projectId && !si.IsDeleted);

        if (statusId.HasValue)
        {
            query = query.Where(si => si.StatusId == statusId.Value);
        }

        var invitations = await query
            .OrderByDescending(si => si.CreatedAt)
            .Select(si => new
            {
                si.Id,
                si.Email,
                si.FirstName,
                si.LastName,
                FullName = si.FirstName != null || si.LastName != null
                    ? $"{si.FirstName} {si.LastName}".Trim()
                    : null,
                si.StatusId,
                si.ErrorMessage,
                si.SentAt,
                si.IsOpened,
                si.OpenedAt,
                si.IsCompleted,
                si.CompletedAt,
                si.AttemptCount,
                si.CreatedAt
            })
            .ToListAsync();

        return Ok(invitations);
    }

    /// <summary>
    /// Dış davetiye istatistiklerini getir
    /// </summary>
    [HttpGet("{projectId}/external-invitation-stats")]
    public async Task<IActionResult> GetExternalInvitationStats(int projectId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        var invitations = await _context.SurveyExternalInvitations
            .Where(si => si.ProjectId == projectId && !si.IsDeleted)
            .ToListAsync();

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
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.Checklist)
            .Include(p => p.EmailTemplate)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Email şablonu kontrolü
        var emailTemplate = project.EmailTemplate;
        if (dto.EmailTemplateId.HasValue)
        {
            emailTemplate = await _context.EmailTemplates
                .FirstOrDefaultAsync(e => e.Id == dto.EmailTemplateId.Value && !e.IsDeleted);
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
        var failedInvitations = await _context.SurveyExternalInvitations
            .Where(si => si.ProjectId == projectId &&
                         si.StatusId == SurveyInvitationStatuses.Ids.Failed &&
                         !si.IsDeleted)
            .ToListAsync();

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

                invitation.AttemptCount++;

                if (result.Success)
                {
                    invitation.StatusId = SurveyInvitationStatuses.Ids.Sent;
                    invitation.SentAt = DateTime.UtcNow;
                    invitation.ErrorMessage = null;
                    successCount++;
                    _logger.LogInformation("Retry: External survey invitation sent to {Email} for project {ProjectId}",
                        invitation.Email, projectId);
                }
                else
                {
                    invitation.ErrorMessage = result.ErrorMessage;
                    failCount++;
                    errors.Add($"{invitation.Email}: {result.ErrorMessage}");
                    _logger.LogWarning("Retry failed for external {Email}: {Error}",
                        invitation.Email, result.ErrorMessage);
                }
                await _context.SaveChangesAsync();
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
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.Checklist)
            .Include(p => p.EmailTemplate)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        // Email şablonu kontrolü
        var emailTemplate = project.EmailTemplate;
        if (dto.EmailTemplateId.HasValue)
        {
            emailTemplate = await _context.EmailTemplates
                .FirstOrDefaultAsync(e => e.Id == dto.EmailTemplateId.Value && !e.IsDeleted);
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
        var query = _context.SurveyExternalInvitations
            .Where(si => si.ProjectId == projectId &&
                         si.StatusId == SurveyInvitationStatuses.Ids.Sent &&
                         !si.IsDeleted);

        // Filtre uygula
        switch (dto.Filter?.ToLower())
        {
            case "completed":
                query = query.Where(si => si.IsCompleted);
                break;
            case "notcompleted":
                query = query.Where(si => !si.IsCompleted);
                break;
            case "all":
            default:
                // Tümü - filtre yok
                break;
        }

        var invitations = await query.ToListAsync();

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
                    invitation.AttemptCount++;
                    invitation.SentAt = DateTime.UtcNow;
                    successCount++;
                    _logger.LogInformation("Reminder sent to external {Email} for project {ProjectId}",
                        invitation.Email, projectId);
                }
                else
                {
                    failCount++;
                    errors.Add($"{invitation.Email}: {result.ErrorMessage}");
                    _logger.LogWarning("Reminder failed for external {Email}: {Error}",
                        invitation.Email, result.ErrorMessage);
                }
                await _context.SaveChangesAsync();
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
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.Checklist)
            .Include(p => p.EmailTemplate)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

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
                emailTemplate = await _context.EmailTemplates
                    .FirstOrDefaultAsync(e => e.Id == emailTemplateId.Value && !e.IsDeleted);
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
                var existingInvitation = await _context.SurveyExternalInvitations
                    .FirstOrDefaultAsync(i => i.ProjectId == projectId &&
                                              i.Email == recipient.Email &&
                                              !i.IsDeleted);

                if (existingInvitation != null)
                {
                    duplicateCount++;
                    continue;
                }

                // Benzersiz token oluştur
                var token = Guid.NewGuid().ToString("N");

                // SurveyExternalInvitation kaydı oluştur
                var invitation = new SurveyExternalInvitation
                {
                    ProjectId = projectId,
                    Email = recipient.Email,
                    FirstName = recipient.FirstName,
                    LastName = recipient.LastName,
                    Token = token,
                    StatusId = SurveyInvitationStatuses.Ids.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SurveyExternalInvitations.Add(invitation);
                await _context.SaveChangesAsync();
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
                        invitation.StatusId = SurveyInvitationStatuses.Ids.Sent;
                        invitation.SentAt = DateTime.UtcNow;
                        successCount++;
                        _logger.LogInformation("External survey invitation sent to {Email} for project {ProjectId} (file upload)",
                            recipient.Email, projectId);
                    }
                    else
                    {
                        invitation.StatusId = SurveyInvitationStatuses.Ids.Failed;
                        invitation.ErrorMessage = result.ErrorMessage;
                        failCount++;
                        errors.Add($"{recipient.Email}: {result.ErrorMessage}");
                        _logger.LogWarning("Failed to send external survey invitation to {Email}: {Error}",
                            recipient.Email, result.ErrorMessage);
                    }
                    await _context.SaveChangesAsync();
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

            foreach (var token in tokens)
            {
                // <> işaretlerini temizle
                var cleanToken = token.Trim('<', '>');

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
                    tokensBeforeEmail.Add(token);
                }
                else
                {
                    // Email sonrası isim token'ı
                    tokensAfterEmail.Add(token);
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
        text = text.Replace(EmailPlaceholders.CurrentDate, DateTime.Now.ToString("dd.MM.yyyy"));
        text = text.Replace(EmailPlaceholders.CurrentYear, DateTime.Now.Year.ToString());
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
/// Anket soru cevabı DTO
/// </summary>
public class SurveyAnswerDto
{
    /// <summary>
    /// Soru ID
    /// </summary>
    public int QuestionId { get; set; }

    /// <summary>
    /// Verilen puan
    /// </summary>
    public int? Score { get; set; }

    /// <summary>
    /// Yorum (opsiyonel)
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Seçilen alt kriter ID'leri (Online anket için)
    /// </summary>
    public List<int>? SelectedSubCriteriaIds { get; set; }
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
