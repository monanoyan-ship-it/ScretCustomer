using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class SurveyService : ISurveyService
{
    private readonly ApplicationDbContext _context;

    public SurveyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetProjectWithDetailsAsync(int projectId)
    {
        return await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.Checklist)
            .Include(p => p.EmailTemplate)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
    }

    public async Task<Project?> GetProjectAsync(int projectId)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
    }

    public async Task<Project?> GetProjectWithChecklistAsync(int projectId)
    {
        return await _context.Projects
            .Include(p => p.Checklist)
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
    }

    public async Task<EmailTemplate?> GetEmailTemplateAsync(int emailTemplateId)
    {
        return await _context.EmailTemplates
            .FirstOrDefaultAsync(e => e.Id == emailTemplateId && !e.IsDeleted);
    }

    public async Task<List<CustomerPersonnel>> GetProjectPersonnelWithEmailAsync(int? customerId, int? organizationId)
    {
        var personnelQuery = _context.CustomerPersonnel
            .Include(p => p.Customer)
            .Include(p => p.OrganizationAssignments)
                .ThenInclude(oa => oa.CustomerOrganization)
            .Where(p => !p.IsDeleted && p.IsActive && !string.IsNullOrEmpty(p.Email));

        if (customerId.HasValue)
        {
            personnelQuery = personnelQuery.Where(p => p.CustomerId == customerId.Value);
        }

        if (organizationId.HasValue)
        {
            personnelQuery = personnelQuery.Where(p =>
                p.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == organizationId.Value && !oa.IsDeleted));
        }

        return await personnelQuery.ToListAsync();
    }

    public async Task<List<CustomerPersonnel>> GetProjectPersonnelAsync(int? customerId, int? organizationId)
    {
        var personnelQuery = _context.CustomerPersonnel
            .Where(p => !p.IsDeleted && p.IsActive);

        if (customerId.HasValue)
        {
            personnelQuery = personnelQuery.Where(p => p.CustomerId == customerId.Value);
        }

        if (organizationId.HasValue)
        {
            personnelQuery = personnelQuery.Where(p =>
                p.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == organizationId.Value && !oa.IsDeleted));
        }

        return await personnelQuery.ToListAsync();
    }

    public async Task<List<CustomerPersonnel>> GetProjectPersonnelWithCustomerAsync(int? customerId, int? organizationId)
    {
        var personnelQuery = _context.CustomerPersonnel
            .Include(p => p.Customer)
            .Where(p => !p.IsDeleted && p.IsActive && !string.IsNullOrEmpty(p.Email));

        if (customerId.HasValue)
        {
            personnelQuery = personnelQuery.Where(p => p.CustomerId == customerId.Value);
        }

        if (organizationId.HasValue)
        {
            personnelQuery = personnelQuery.Where(p =>
                p.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == organizationId.Value && !oa.IsDeleted));
        }

        return await personnelQuery.ToListAsync();
    }

    public async Task<List<int>> GetCompletedPersonnelIdsAsync(int projectId)
    {
        return await _context.Assignments
            .Where(a => a.ProjectId == projectId && !a.IsDeleted && a.IsCompleted)
            .Where(a => a.AssignedCustomerPersonnelId.HasValue)
            .Select(a => a.AssignedCustomerPersonnelId!.Value)
            .ToListAsync();
    }

    public async Task<SurveyInvitation> CreateSurveyInvitationAsync(int projectId, int customerPersonnelId, string email, bool isReminder)
    {
        var invitation = new SurveyInvitation
        {
            ProjectId = projectId,
            CustomerPersonnelId = customerPersonnelId,
            Email = email,
            StatusId = SurveyInvitationStatuses.Ids.Pending,
            IsReminder = isReminder,
            CreatedAt = TurkeyTime.Now
        };
        _context.SurveyInvitations.Add(invitation);
        await _context.SaveChangesAsync();
        return invitation;
    }

    public async Task UpdateSurveyInvitationStatusAsync(SurveyInvitation invitation, int statusId, string? errorMessage = null)
    {
        invitation.StatusId = statusId;
        if (statusId == SurveyInvitationStatuses.Ids.Sent)
        {
            invitation.SentAt = TurkeyTime.Now;
        }
        if (errorMessage != null)
        {
            invitation.ErrorMessage = errorMessage;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<SurveyExternalInvitation?> GetExternalInvitationByTokenAsync(string token)
    {
        return await _context.SurveyExternalInvitations
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsDeleted);
    }

    public async Task<SurveyExternalInvitation?> GetExternalInvitationWithProjectAsync(string token)
    {
        return await _context.SurveyExternalInvitations
            .Include(i => i.Project)
                .ThenInclude(p => p!.Checklist)
            .Include(i => i.Project)
                .ThenInclude(p => p!.Customer)
            .Include(i => i.Project)
                .ThenInclude(p => p!.Organization)
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsDeleted);
    }

    public async Task<SurveyExternalInvitation?> GetExternalInvitationWithChecklistAsync(string token)
    {
        return await _context.SurveyExternalInvitations
            .Include(i => i.Project)
                .ThenInclude(p => p!.Checklist)
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsDeleted);
    }

    public async Task<CustomerPersonnel?> GetPersonnelAsync(int personnelId)
    {
        return await _context.CustomerPersonnel
            .FirstOrDefaultAsync(p => p.Id == personnelId && !p.IsDeleted);
    }

    public async Task<bool> HasCompletedAssignmentAsync(int projectId, int personnelId)
    {
        return await _context.Assignments
            .Where(a => a.ProjectId == projectId &&
                        a.AssignedCustomerPersonnelId == personnelId &&
                        !a.IsDeleted && a.IsCompleted)
            .AnyAsync();
    }

    public async Task<Evaluation?> GetCompletedEvaluationAsync(int projectId, int personnelId)
    {
        return await _context.Evaluations
            .FirstOrDefaultAsync(e => e.ProjectId == projectId &&
                        e.EvaluatedCustomerPersonnelId == personnelId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        !e.IsDeleted);
    }

    public async Task MarkInvitationOpenedAsync(int projectId, int personnelId)
    {
        try
        {
            var invitation = await _context.SurveyInvitations
                .Where(si => si.ProjectId == projectId &&
                             si.CustomerPersonnelId == personnelId &&
                             si.StatusId == SurveyInvitationStatuses.Ids.Sent &&
                             !si.IsOpened)
                .OrderByDescending(si => si.CreatedAt)
                .FirstOrDefaultAsync();

            if (invitation != null)
            {
                invitation.IsOpened = true;
                invitation.OpenedAt = TurkeyTime.Now;
                await _context.SaveChangesAsync();
            }
        }
        catch
        {
            // Davet kaydı bulunamasa veya tablo yoksa devam et - anket yine de çalışmalı
            // Caller will handle logging
        }
    }

    public async Task MarkExternalInvitationOpenedAsync(SurveyExternalInvitation invitation)
    {
        if (!invitation.IsOpened)
        {
            invitation.IsOpened = true;
            invitation.OpenedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<object> GetSurveyQuestionsAsync(int? checklistId)
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

    public async Task<Evaluation> CreateEvaluationAsync(Evaluation evaluation)
    {
        _context.Evaluations.Add(evaluation);
        await _context.SaveChangesAsync();
        return evaluation;
    }

    public async Task<(decimal? TotalScore, decimal? MaxScore, decimal? ScorePercentage)> SaveAnswersAndCalculateScoreAsync(int evaluationId, int? checklistId, List<SurveyAnswerDto> answers)
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
                CreatedAt = TurkeyTime.Now
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
                        SelectedAt = TurkeyTime.Now,
                        CreatedAt = TurkeyTime.Now
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

    public async Task MarkSurveyInvitationCompletedAsync(int projectId, int personnelId)
    {
        try
        {
            var surveyInvitation = await _context.SurveyInvitations
                .Where(si => si.ProjectId == projectId &&
                             si.CustomerPersonnelId == personnelId &&
                             si.StatusId == SurveyInvitationStatuses.Ids.Sent &&
                             !si.IsCompleted)
                .OrderByDescending(si => si.CreatedAt)
                .FirstOrDefaultAsync();

            if (surveyInvitation != null)
            {
                surveyInvitation.IsCompleted = true;
                surveyInvitation.CompletedAt = TurkeyTime.Now;
            }
        }
        catch
        {
            // Caller will handle logging
        }
    }

    public async Task MarkExternalInvitationCompletedAsync(SurveyExternalInvitation invitation, int evaluationId)
    {
        invitation.IsCompleted = true;
        invitation.CompletedAt = TurkeyTime.Now;
        invitation.EvaluationId = evaluationId;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateEvaluationScoreAsync(int evaluationId, decimal? totalScore, decimal? maxScore, decimal? scorePercentage)
    {
        var evaluation = await _context.Evaluations.FindAsync(evaluationId);
        if (evaluation != null)
        {
            evaluation.TotalScore = totalScore;
            evaluation.MaxScore = maxScore;
            evaluation.ScorePercentage = scorePercentage;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Dictionary<int, (DateTime? CompletedAt, decimal? Score)>> GetCompletedAssignmentsWithScoresAsync(int projectId)
    {
        var completedAssignments = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Evaluations)
            .Where(a => a.ProjectId == projectId && !a.IsDeleted && a.IsCompleted && a.AssignedCustomerPersonnelId.HasValue)
            .ToDictionaryAsync(a => a.AssignedCustomerPersonnelId!.Value, a => (
                CompletedAt: a.CompletedAt,
                Score: a.Project.Evaluations.FirstOrDefault()?.ScorePercentage
            ));

        return completedAssignments;
    }

    public async Task<List<SurveyInvitation>> GetInvitationsRawAsync(int projectId)
    {
        return await _context.SurveyInvitations
            .Where(si => si.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task<List<CustomerPersonnel>> GetCustomerPersonnelByIdsAsync(List<int> ids)
    {
        return await _context.CustomerPersonnel
            .Where(cp => ids.Contains(cp.Id) && !cp.IsDeleted)
            .ToListAsync();
    }

    public async Task<object> GetInvitationsAsync(int projectId, int? statusId)
    {
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

        return invitations;
    }

    public async Task<List<SurveyInvitation>> GetFailedInvitationsAsync(int projectId)
    {
        return await _context.SurveyInvitations
            .Include(si => si.CustomerPersonnel)
            .Where(si => si.ProjectId == projectId && si.StatusId == SurveyInvitationStatuses.Ids.Failed)
            .ToListAsync();
    }

    public async Task UpdateInvitationRetryAsync(SurveyInvitation invitation, int statusId, string? errorMessage = null)
    {
        invitation.AttemptCount++;
        invitation.StatusId = statusId;

        if (statusId == SurveyInvitationStatuses.Ids.Sent)
        {
            invitation.SentAt = TurkeyTime.Now;
            invitation.Email = invitation.CustomerPersonnel?.Email ?? invitation.Email; // Güncel email'i kaydet
            invitation.ErrorMessage = null;
        }
        else
        {
            invitation.ErrorMessage = errorMessage;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExternalInvitationExistsAsync(int projectId, string email)
    {
        return await _context.SurveyExternalInvitations
            .AnyAsync(i => i.ProjectId == projectId &&
                           i.Email == email &&
                           !i.IsDeleted);
    }

    public async Task<SurveyExternalInvitation> CreateExternalInvitationAsync(int projectId, string email, string? firstName, string? lastName, string token)
    {
        var invitation = new SurveyExternalInvitation
        {
            ProjectId = projectId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Token = token,
            StatusId = SurveyInvitationStatuses.Ids.Pending,
            CreatedAt = TurkeyTime.Now
        };
        _context.SurveyExternalInvitations.Add(invitation);
        await _context.SaveChangesAsync();
        return invitation;
    }

    public async Task UpdateExternalInvitationStatusAsync(SurveyExternalInvitation invitation, int statusId, string? errorMessage = null)
    {
        invitation.StatusId = statusId;
        if (statusId == SurveyInvitationStatuses.Ids.Sent)
        {
            invitation.SentAt = TurkeyTime.Now;
        }
        if (errorMessage != null)
        {
            invitation.ErrorMessage = errorMessage;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<object> GetExternalInvitationsAsync(int projectId, int? statusId)
    {
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

        return invitations;
    }

    public async Task<List<SurveyExternalInvitation>> GetExternalInvitationsRawAsync(int projectId)
    {
        return await _context.SurveyExternalInvitations
            .Where(si => si.ProjectId == projectId && !si.IsDeleted)
            .ToListAsync();
    }

    public async Task<List<SurveyExternalInvitation>> GetFailedExternalInvitationsAsync(int projectId)
    {
        return await _context.SurveyExternalInvitations
            .Where(si => si.ProjectId == projectId &&
                         si.StatusId == SurveyInvitationStatuses.Ids.Failed &&
                         !si.IsDeleted)
            .ToListAsync();
    }

    public async Task UpdateExternalInvitationRetryAsync(SurveyExternalInvitation invitation, int statusId, string? errorMessage = null)
    {
        invitation.AttemptCount++;
        invitation.StatusId = statusId;

        if (statusId == SurveyInvitationStatuses.Ids.Sent)
        {
            invitation.SentAt = TurkeyTime.Now;
            invitation.ErrorMessage = null;
        }
        else
        {
            invitation.ErrorMessage = errorMessage;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<SurveyExternalInvitation>> GetExternalInvitationsForReminderAsync(int projectId, string? filter)
    {
        var query = _context.SurveyExternalInvitations
            .Where(si => si.ProjectId == projectId &&
                         si.StatusId == SurveyInvitationStatuses.Ids.Sent &&
                         !si.IsDeleted);

        // Filtre uygula
        switch (filter?.ToLower())
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

        return await query.ToListAsync();
    }

    public async Task UpdateExternalInvitationReminderAsync(SurveyExternalInvitation invitation, bool success)
    {
        if (success)
        {
            invitation.AttemptCount++;
            invitation.SentAt = TurkeyTime.Now;
        }
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
