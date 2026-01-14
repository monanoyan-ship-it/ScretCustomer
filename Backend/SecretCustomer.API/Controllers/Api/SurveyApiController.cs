using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Services.Helpers;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/surveys")]
[ApiController]
[Authorize(Roles = "Admin,TeamLeader")]
public class SurveyApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<SurveyApiController> _logger;

    public SurveyApiController(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<SurveyApiController> logger)
    {
        _context = context;
        _emailService = emailService;
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
                    Status = SurveyInvitationStatus.Pending,
                    IsReminder = dto.SendReminders,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SurveyInvitations.Add(invitation);
                await _context.SaveChangesAsync();

                var result = await _emailService.SendEmailAsync(person.Email!, subject, body, true);

                if (result.Success)
                {
                    invitation.Status = SurveyInvitationStatus.Sent;
                    invitation.SentAt = DateTime.UtcNow;
                    successCount++;
                    _logger.LogInformation("Survey invitation sent to {Email} for project {ProjectId}",
                        person.Email, projectId);
                }
                else
                {
                    invitation.Status = SurveyInvitationStatus.Failed;
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
        // Token'ı çöz
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

        // SurveyInvitation kaydını bul ve açıldı olarak işaretle
        var invitation = await _context.SurveyInvitations
            .Where(si => si.ProjectId == tokenData.ProjectId &&
                         si.CustomerPersonnelId == tokenData.PersonnelId &&
                         si.Status == SurveyInvitationStatus.Sent &&
                         !si.IsOpened)
            .OrderByDescending(si => si.CreatedAt)
            .FirstOrDefaultAsync();

        if (invitation != null)
        {
            invitation.IsOpened = true;
            invitation.OpenedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        var isAnonymous = project.SurveyIdentityTypeId == SurveyIdentityTypes.Ids.Anonymous;

        // Checklist sorularını getir (SubCriteria dahil)
        var questions = await _context.Questions
            .Include(q => q.SubCriteria)
            .Where(q => q.ChecklistId == project.ChecklistId && !q.IsDeleted)
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
                q.AllowNA,
                // Yeni alanlar
                q.SelectionTypeId,
                q.ShowScoreInput,
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

        return Ok(new
        {
            valid = true,
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
    /// Anket cevaplarını gönder ve değerlendirme oluştur (PUBLIC)
    /// </summary>
    [HttpPost("submit/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitSurvey(string token, [FromBody] SubmitSurveyDto dto)
    {
        // Token'ı çöz
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
        var existingAssignment = await _context.Assignments
            .FirstOrDefaultAsync(a => a.ProjectId == project.Id &&
                        a.AssignedCustomerPersonnelId == personnel.Id &&
                        !a.IsDeleted);

        if (existingAssignment != null && existingAssignment.IsCompleted)
        {
            return BadRequest(new { message = "Bu anket zaten tamamlanmış." });
        }

        try
        {
            Assignment assignment;
            if (existingAssignment != null)
            {
                assignment = existingAssignment;
            }
            else
            {
                // Yeni Assignment oluştur
                assignment = new Assignment
                {
                    ProjectId = project.Id,
                    ChecklistId = project.ChecklistId,
                    TypeId = AssignmentTypes.Ids.CustomerPersonnel,
                    AssignedCustomerPersonnelId = personnel.Id,
                    DueDate = project.EndDate,
                    UniqueLink = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.Assignments.Add(assignment);
                await _context.SaveChangesAsync();
            }

            // Değerlendirme oluştur
            var evaluation = new Evaluation
            {
                AssignmentId = assignment.Id,
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

            // Cevapları kaydet
            decimal totalScore = 0;
            decimal totalWeight = 0;
            var questions = await _context.Questions
                .Where(q => q.ChecklistId == project.ChecklistId && !q.IsDeleted)
                .ToListAsync();

            foreach (var answerDto in dto.Answers)
            {
                var question = questions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
                if (question == null) continue;

                var answer = new Answer
                {
                    EvaluationId = evaluation.Id,
                    QuestionId = answerDto.QuestionId,
                    AnswerNumeric = answerDto.IsNA ? null : answerDto.Score,
                    IsNA = answerDto.IsNA,
                    Notes = answerDto.Comment,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Answers.Add(answer);
                await _context.SaveChangesAsync(); // Answer ID gerekli

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

                // Puan hesapla (N/A değilse ve showScoreInput true ise)
                if (!answerDto.IsNA && answerDto.Score.HasValue && question.ScoringTypeId == ScoringTypes.Ids.Scored)
                {
                    totalWeight += question.WeightPoints;
                    totalScore += (answerDto.Score.Value / (decimal)question.MaxPoints) * question.WeightPoints;
                }
            }

            // Toplam puanı hesapla
            if (totalWeight > 0)
            {
                evaluation.TotalScore = Math.Round((totalScore / totalWeight) * 100, 2);
            }

            // Assignment'ı tamamlandı olarak işaretle
            assignment.IsCompleted = true;
            assignment.CompletedAt = DateTime.UtcNow;

            // SurveyInvitation kaydını tamamlandı olarak işaretle
            var surveyInvitation = await _context.SurveyInvitations
                .Where(si => si.ProjectId == project.Id &&
                             si.CustomerPersonnelId == personnel.Id &&
                             si.Status == SurveyInvitationStatus.Sent &&
                             !si.IsCompleted)
                .OrderByDescending(si => si.CreatedAt)
                .FirstOrDefaultAsync();

            if (surveyInvitation != null)
            {
                surveyInvitation.IsCompleted = true;
                surveyInvitation.CompletedAt = DateTime.UtcNow;
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
            .Include(a => a.Evaluations)
            .Where(a => a.ProjectId == projectId && !a.IsDeleted && a.IsCompleted && a.AssignedCustomerPersonnelId.HasValue)
            .ToDictionaryAsync(a => a.AssignedCustomerPersonnelId!.Value, a => new
            {
                CompletedAt = a.CompletedAt,
                Score = a.Evaluations.FirstOrDefault()?.TotalScore
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
            sent = invitations.Count(i => i.Status == SurveyInvitationStatus.Sent),
            failed = invitations.Count(i => i.Status == SurveyInvitationStatus.Failed),
            pending = invitations.Count(i => i.Status == SurveyInvitationStatus.Pending),
            opened = invitations.Count(i => i.IsOpened),
            completed = invitations.Count(i => i.IsCompleted),
            // Her durum için Türkçe adlar ve badge'ler
            statuses = new[]
            {
                new { status = SurveyInvitationStatus.Sent, displayName = SurveyInvitationStatus.GetDisplayName(SurveyInvitationStatus.Sent), badgeClass = SurveyInvitationStatus.GetBadgeClass(SurveyInvitationStatus.Sent), count = invitations.Count(i => i.Status == SurveyInvitationStatus.Sent) },
                new { status = SurveyInvitationStatus.Failed, displayName = SurveyInvitationStatus.GetDisplayName(SurveyInvitationStatus.Failed), badgeClass = SurveyInvitationStatus.GetBadgeClass(SurveyInvitationStatus.Failed), count = invitations.Count(i => i.Status == SurveyInvitationStatus.Failed) },
                new { status = SurveyInvitationStatus.Pending, displayName = SurveyInvitationStatus.GetDisplayName(SurveyInvitationStatus.Pending), badgeClass = SurveyInvitationStatus.GetBadgeClass(SurveyInvitationStatus.Pending), count = invitations.Count(i => i.Status == SurveyInvitationStatus.Pending) }
            }
        };

        return Ok(stats);
    }

    /// <summary>
    /// Proje için davetiye listesini getir
    /// </summary>
    [HttpGet("{projectId}/invitations")]
    public async Task<IActionResult> GetInvitations(int projectId, [FromQuery] string? status = null)
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

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(si => si.Status == status);
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
                si.Status,
                StatusDisplayName = SurveyInvitationStatus.GetDisplayName(si.Status),
                StatusBadgeClass = SurveyInvitationStatus.GetBadgeClass(si.Status),
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
            .Where(si => si.ProjectId == projectId && si.Status == SurveyInvitationStatus.Failed)
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
                    invitation.Status = SurveyInvitationStatus.Sent;
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
        text = text.Replace(EmailPlaceholders.SurveyLink, surveyUrl);
        text = text.Replace(EmailPlaceholders.SurveyQRCode, GenerateQRCodeHtml(surveyUrl));

        // Firma/Organizasyon
        text = text.Replace(EmailPlaceholders.CompanyName, project.Customer?.CompanyName ?? "");
        text = text.Replace(EmailPlaceholders.OrganizationName, project.Organization?.Name ?? "");
        text = text.Replace(EmailPlaceholders.BranchName, person.Department ?? "");

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
    /// QR kod HTML'i oluştur (placeholder için)
    /// </summary>
    private string GenerateQRCodeHtml(string url)
    {
        var qrUrl = $"https://chart.googleapis.com/chart?chs=150x150&cht=qr&chl={Uri.EscapeDataString(url)}&choe=UTF-8";
        return $"<img src='{qrUrl}' alt='QR Code' style='width:150px;height:150px;' />";
    }
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
    /// Anket form base URL'i (örn: https://example.com/Anket/Form)
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
    /// Verilen puan (N/A ise null)
    /// </summary>
    public int? Score { get; set; }

    /// <summary>
    /// N/A olarak mı işaretlendi
    /// </summary>
    public bool IsNA { get; set; }

    /// <summary>
    /// Yorum (opsiyonel)
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Seçilen alt kriter ID'leri (Online anket için)
    /// </summary>
    public List<int>? SelectedSubCriteriaIds { get; set; }
}

#endregion
