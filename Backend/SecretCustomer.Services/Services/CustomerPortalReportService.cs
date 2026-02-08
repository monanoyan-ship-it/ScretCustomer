using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Core.Entities;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class CustomerPortalReportService : ICustomerPortalReportService
{
    private readonly ApplicationDbContext _context;

    public CustomerPortalReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ==================== SURVEY ====================

    public async Task<List<SurveyProjectListItemDto>> GetSurveyProjectsAsync(int customerId)
    {
        // Enneagram checklist'lerini hariç tut - sadece Survey olanlar gelsin (admin ile aynı)
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        var projects = await _context.Projects
            .Where(p => p.CustomerId == customerId &&
                   p.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey &&
                   !p.IsDeleted &&
                   !enneagramChecklistIds.Contains(p.ChecklistId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var result = new List<SurveyProjectListItemDto>();

        foreach (var project in projects)
        {
            // Gönderilen davetiye sayısı (internal + external)
            var internalInvitations = await _context.SurveyInvitations
                .Where(si => si.ProjectId == project.Id &&
                       (si.StatusId == SurveyInvitationStatuses.Ids.Sent || si.StatusId == SurveyInvitationStatuses.Ids.Pending))
                .CountAsync();

            var externalInvitations = await _context.SurveyExternalInvitations
                .Where(si => si.ProjectId == project.Id &&
                       (si.StatusId == SurveyInvitationStatuses.Ids.Sent || si.StatusId == SurveyInvitationStatuses.Ids.Pending))
                .CountAsync();

            var invitationCount = internalInvitations + externalInvitations;

            // Tamamlanan anket sayısı
            var completedCount = await _context.Evaluations
                .Where(e => e.ProjectId == project.Id && e.StatusId == EvaluationStatuses.Ids.Completed)
                .CountAsync();

            // Ortalama puan
            var avgScore = await _context.Evaluations
                .Where(e => e.ProjectId == project.Id &&
                       e.StatusId == EvaluationStatuses.Ids.Completed &&
                       e.ScorePercentage.HasValue)
                .Select(e => e.ScorePercentage)
                .AverageAsync() ?? 0;

            // Son yanıt tarihi
            var lastResponse = await _context.Evaluations
                .Where(e => e.ProjectId == project.Id && e.StatusId == EvaluationStatuses.Ids.Completed)
                .OrderByDescending(e => e.CompletedAt)
                .Select(e => e.CompletedAt)
                .FirstOrDefaultAsync();

            result.Add(new SurveyProjectListItemDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                ProjectCode = project.Code,
                TotalInvitations = invitationCount,
                TotalResponses = completedCount,
                ResponseRate = invitationCount > 0 ? Math.Round((decimal)completedCount / invitationCount * 100, 1) : 0,
                AverageScore = completedCount > 0 ? Math.Round(avgScore, 1) : null,
                LastResponseAt = lastResponse,
                IsActive = project.IsActive
            });
        }

        return result;
    }

    public async Task<List<RecentSurveyResponseDto>> GetRecentSurveyResponsesAsync(
        int customerId, int count = 20, int? projectId = null,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        // Enneagram checklist'lerini hariç tut (admin ile aynı)
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        var query = _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Where(e => e.Project.CustomerId == customerId &&
                   e.Project.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey &&
                   e.StatusId == EvaluationStatuses.Ids.Completed &&
                   !e.Project.IsDeleted &&
                   !enneagramChecklistIds.Contains(e.Project.ChecklistId))
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(e => e.ProjectId == projectId.Value);

        if (startDate.HasValue)
        {
            var startDateUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt >= startDateUtc);
        }

        if (endDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt <= endDateUtc);
        }

        var evaluations = await query
            .OrderByDescending(e => e.CompletedAt)
            .Take(count)
            .ToListAsync();

        // External invitations for evaluations without CustomerPersonnel
        var evaluationIds = evaluations.Where(e => e.EvaluatedCustomerPersonnelId == null).Select(e => e.Id).ToList();
        var externalInvitations = new Dictionary<int, (string? FirstName, string? LastName, string? Email)>();
        if (evaluationIds.Any())
        {
            var extList = await _context.SurveyExternalInvitations
                .Where(sei => sei.EvaluationId != null && evaluationIds.Contains(sei.EvaluationId.Value))
                .Select(sei => new { EvalId = sei.EvaluationId!.Value, sei.FirstName, sei.LastName, sei.Email })
                .ToListAsync();
            foreach (var item in extList)
                externalInvitations[item.EvalId] = (item.FirstName, item.LastName, item.Email);
        }

        var responses = evaluations.Select(e =>
        {
            string? respondentName = null;
            string? respondentEmail = null;

            if (e.EvaluatedCustomerPersonnel != null)
            {
                respondentName = $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}".Trim();
                respondentEmail = e.EvaluatedCustomerPersonnel.Email;
            }
            else if (externalInvitations.TryGetValue(e.Id, out var ext))
            {
                respondentName = $"{ext.FirstName} {ext.LastName}".Trim();
                respondentEmail = ext.Email;
            }

            return new RecentSurveyResponseDto
            {
                EvaluationId = e.Id,
                ProjectId = e.ProjectId,
                ProjectName = e.Project.Name,
                RespondentName = string.IsNullOrWhiteSpace(respondentName) ? null : respondentName,
                RespondentEmail = respondentEmail,
                Score = e.ScorePercentage,
                CompletedAt = e.CompletedAt
            };
        }).ToList();

        return responses;
    }

    public async Task<SurveyProjectDetailDto?> GetSurveyProjectDetailAsync(int customerId, int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Organization)
            .Include(p => p.Checklist)
            .FirstOrDefaultAsync(p => p.Id == projectId &&
                   p.CustomerId == customerId &&
                   !p.IsDeleted);

        if (project == null || project.ProjectTypeId != ProjectTypes.Ids.OnlineSurvey)
            return null;

        // Sorular
        var questions = await _context.Questions
            .Where(q => q.ChecklistId == project.ChecklistId && !q.IsDeleted)
            .ToListAsync();

        // Değerlendirmeler
        var evaluations = await _context.Evaluations
            .Include(e => e.Answers)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.OrganizationAssignments)
                    .ThenInclude(oa => oa.CustomerOrganization)
            .Where(e => e.ProjectId == projectId && e.StatusId == EvaluationStatuses.Ids.Completed)
            .OrderByDescending(e => e.CompletedAt)
            .ToListAsync();

        // Davetiye sayısı
        var invitationCount = await _context.SurveyInvitations
            .Where(si => si.ProjectId == projectId && si.StatusId == SurveyInvitationStatuses.Ids.Sent)
            .CountAsync();

        // Grup bazlı puan hesaplaması (admin ile aynı)
        var groupScores = new List<SurveyGroupScoreDto>();
        var groups = questions.GroupBy(q => q.GroupName ?? "Genel");

        foreach (var group in groups)
        {
            var groupQuestionIds = group.Select(q => q.Id).ToList();
            var groupAnswers = evaluations
                .SelectMany(e => e.Answers.Where(a => groupQuestionIds.Contains(a.QuestionId) && a.AnswerNumeric.HasValue))
                .ToList();

            if (groupAnswers.Any())
            {
                var totalScore = 0m;
                var totalMaxScore = 0m;

                foreach (var question in group.Where(q => q.ShowScoreInput))
                {
                    var questionAnswers = groupAnswers.Where(a => a.QuestionId == question.Id).ToList();
                    if (questionAnswers.Any())
                    {
                        totalScore += questionAnswers.Sum(a => a.AnswerNumeric ?? 0);
                        totalMaxScore += questionAnswers.Count * question.MaxPoints;
                    }
                }

                groupScores.Add(new SurveyGroupScoreDto
                {
                    GroupName = group.Key ?? "Genel",
                    QuestionCount = group.Count(),
                    TotalResponses = evaluations.Count,
                    AverageScore = totalMaxScore > 0 ? Math.Round(totalScore / totalMaxScore * 100, 1) : null
                });
            }
            else
            {
                groupScores.Add(new SurveyGroupScoreDto
                {
                    GroupName = group.Key ?? "Genel",
                    QuestionCount = group.Count(),
                    TotalResponses = evaluations.Count,
                    AverageScore = null
                });
            }
        }

        // Son 10 katılımcı - External invitation'ları da al
        var top10 = evaluations.Take(10).ToList();
        var extEvalIds = top10.Where(e => e.EvaluatedCustomerPersonnelId == null).Select(e => e.Id).ToList();
        var extInvs = new Dictionary<int, (string? FirstName, string? LastName, string? Email)>();
        if (extEvalIds.Any())
        {
            var extList = await _context.SurveyExternalInvitations
                .Where(sei => sei.EvaluationId != null && extEvalIds.Contains(sei.EvaluationId.Value))
                .Select(sei => new { EvalId = sei.EvaluationId!.Value, sei.FirstName, sei.LastName, sei.Email })
                .ToListAsync();
            foreach (var item in extList)
                extInvs[item.EvalId] = (item.FirstName, item.LastName, item.Email);
        }

        var recentRespondents = top10.Select(e =>
        {
            string? fullName = null;
            string? email = null;
            string? orgName = null;

            if (e.EvaluatedCustomerPersonnel != null)
            {
                fullName = $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}".Trim();
                email = e.EvaluatedCustomerPersonnel.Email;
                orgName = e.EvaluatedCustomerPersonnel.OrganizationAssignments.FirstOrDefault()?.CustomerOrganization?.Name;
            }
            else if (extInvs.TryGetValue(e.Id, out var ext))
            {
                fullName = $"{ext.FirstName} {ext.LastName}".Trim();
                email = ext.Email;
            }

            return new SurveyRespondentDto
            {
                PersonnelId = e.EvaluatedCustomerPersonnelId ?? 0,
                EvaluationId = e.Id,
                FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName,
                Email = email,
                OrganizationName = orgName,
                Score = e.ScorePercentage,
                CompletedAt = e.CompletedAt
            };
        }).ToList();

        return new SurveyProjectDetailDto
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            OrganizationName = project.Organization?.Name,
            TotalInvitations = invitationCount > 0 ? invitationCount : evaluations.Count,
            TotalResponses = evaluations.Count,
            ResponseRate = invitationCount > 0 ? Math.Round((decimal)evaluations.Count / invitationCount * 100, 1) : 100,
            AverageScore = evaluations.Any(e => e.ScorePercentage.HasValue)
                ? Math.Round((decimal)evaluations.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage!.Value), 1)
                : null,
            TotalQuestions = questions.Count,
            GroupScores = groupScores.OrderBy(g => g.GroupName).ToList(),
            RecentRespondents = recentRespondents
        };
    }

    public async Task<SurveyQuestionScoreDistributionResultDto> GetSurveyQuestionScoreDistributionAsync(
        int customerId, int? projectId = null)
    {
        var emptyResult = new SurveyQuestionScoreDistributionResultDto
        {
            Questions = new List<SurveyQuestionScoreDistributionDto>(),
            TotalResponses = 0,
            OverallAverageScore = 0
        };

        if (!projectId.HasValue)
            return emptyResult;

        // Proje müşteriye ait mi kontrol et
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId.Value &&
                   p.CustomerId == customerId &&
                   p.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey &&
                   !p.IsDeleted);

        if (project == null)
            return emptyResult;

        // Tamamlanmış değerlendirmeler (admin ile aynı)
        var evaluationIds = await _context.Evaluations
            .Where(e => !e.IsDeleted &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.Project.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey &&
                        e.ProjectId == projectId.Value)
            .Select(e => e.Id)
            .ToListAsync();

        if (!evaluationIds.Any())
            return emptyResult;

        // Cevapları ve soruları getir
        var answers = await _context.Answers
            .Include(a => a.Question)
            .Where(a => evaluationIds.Contains(a.EvaluationId) && !a.Question.IsDeleted)
            .ToListAsync();

        // Soru bazlı gruplama - EarnedPoints kullan (admin ile aynı)
        var questionStats = answers
            .GroupBy(a => new
            {
                a.QuestionId,
                a.Question.Text,
                a.Question.GroupName,
                a.Question.Order,
                a.Question.WeightPoints
            })
            .Select(g => new SurveyQuestionScoreDistributionDto
            {
                QuestionId = g.Key.QuestionId,
                QuestionText = g.Key.Text,
                GroupName = g.Key.GroupName,
                Order = g.Key.Order,
                MaxPoints = (int)g.Key.WeightPoints,
                ResponseCount = g.Count(),
                AverageRawScore = g.Where(a => a.EarnedPoints.HasValue).Any()
                    ? (decimal?)Math.Round(g.Where(a => a.EarnedPoints.HasValue).Average(a => a.EarnedPoints!.Value), 2)
                    : null,
                AverageScore = g.Where(a => a.EarnedPoints.HasValue).Any() && g.Key.WeightPoints > 0
                    ? (decimal?)Math.Round(g.Where(a => a.EarnedPoints.HasValue).Average(a => a.EarnedPoints!.Value) / g.Key.WeightPoints * 100, 1)
                    : null
            })
            .OrderBy(q => q.GroupName)
            .ThenBy(q => q.Order)
            .ToList();

        var overallAverage = questionStats.Where(q => q.AverageScore.HasValue).Any()
            ? Math.Round(questionStats.Where(q => q.AverageScore.HasValue).Average(q => q.AverageScore!.Value), 1)
            : 0;

        return new SurveyQuestionScoreDistributionResultDto
        {
            Questions = questionStats,
            TotalResponses = evaluationIds.Count,
            OverallAverageScore = overallAverage
        };
    }

    /// <summary>
    /// Soru puan detayı ve cevap dağılımları - admin ReportService ile birebir aynı mantık
    /// </summary>
    public async Task<SurveyQuestionScoreDetailResultDto?> GetSurveyQuestionScoreDetailAsync(int customerId, int projectId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId &&
                   p.CustomerId == customerId &&
                   !p.IsDeleted);

        if (project == null)
            return null;

        // Bu projedeki tamamlanmış değerlendirmeler
        var evaluationIds = await _context.Evaluations
            .Where(e => !e.IsDeleted &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.ProjectId == projectId)
            .Select(e => e.Id)
            .ToListAsync();

        if (!evaluationIds.Any())
        {
            return new SurveyQuestionScoreDetailResultDto
            {
                ProjectId = projectId,
                ProjectName = project.Name,
                TotalResponses = 0,
                OverallAverageScore = null,
                Questions = new List<SurveyQuestionScoreDetailDto>()
            };
        }

        // Cevapları ve soruları getir (alt kriterlerle birlikte) - admin ile aynı
        var answers = await _context.Answers
            .Include(a => a.Question)
                .ThenInclude(q => q.SubCriteria.Where(sc => !sc.IsDeleted))
            .Include(a => a.SubCriteriaSelections)
                .ThenInclude(s => s.SubCriteria)
            .Where(a => evaluationIds.Contains(a.EvaluationId) && !a.Question.IsDeleted)
            .ToListAsync();

        // Soru bazlı gruplama
        var questionGroups = answers
            .GroupBy(a => a.QuestionId)
            .ToList();

        var questionDetails = new List<SurveyQuestionScoreDetailDto>();

        foreach (var group in questionGroups)
        {
            var firstAnswer = group.First();
            var question = firstAnswer.Question;
            var responseCount = group.Count();

            // Puansız soruları atla
            if (question.ScoringTypeId == ScoringTypes.Ids.Unscored)
                continue;

            // Ortalama puan hesapla
            decimal? avgScorePercentage = null;

            // Cezalı sorular için
            if (question.ScoringTypeId == ScoringTypes.Ids.Penalty)
            {
                var penaltyAppliedCount = group.Count(a => a.IsPenaltyApplied);
                avgScorePercentage = responseCount > 0
                    ? Math.Round((decimal)penaltyAppliedCount / responseCount * 100, 1)
                    : 0;
            }
            // Normal puanlı sorular
            else if (question.WeightPoints > 0 && responseCount > 0)
            {
                var answersWithEarned = group.Where(a => a.EarnedPoints.HasValue).ToList();
                if (answersWithEarned.Any())
                {
                    var avgEarned = answersWithEarned.Average(a => a.EarnedPoints!.Value);
                    avgScorePercentage = Math.Round(avgEarned / question.WeightPoints * 100, 1);
                }
                else
                {
                    var answerScores = group.Select(a =>
                        a.SubCriteriaSelections.Sum(s => s.SubCriteria?.WeightPoints ?? 0)
                    ).ToList();

                    if (answerScores.Any())
                    {
                        var avgScore = answerScores.Average();
                        avgScorePercentage = Math.Round((decimal)avgScore / question.WeightPoints * 100, 1);
                    }
                }
            }

            // Alt kriter dağılımları
            var answerDistributions = new List<SurveyAnswerDistributionDto>();
            var allSubCriteria = question.SubCriteria.OrderBy(sc => sc.Order).ToList();

            foreach (var subCriteria in allSubCriteria)
            {
                var selectionCount = group
                    .SelectMany(a => a.SubCriteriaSelections)
                    .Count(ss => ss.SubCriteriaId == subCriteria.Id);

                var percentage = responseCount > 0
                    ? Math.Round((decimal)selectionCount / responseCount * 100, 1)
                    : 0;

                answerDistributions.Add(new SurveyAnswerDistributionDto
                {
                    SubCriteriaId = subCriteria.Id,
                    AnswerText = subCriteria.Description,
                    Points = subCriteria.WeightPoints,
                    SelectionCount = selectionCount,
                    Percentage = percentage
                });
            }

            questionDetails.Add(new SurveyQuestionScoreDetailDto
            {
                QuestionId = question.Id,
                QuestionText = question.Text,
                GroupName = question.GroupName,
                Order = question.Order,
                ScoringTypeId = question.ScoringTypeId,
                ResponseCount = responseCount,
                MaxPoints = question.MaxPoints,
                AverageScorePercentage = avgScorePercentage,
                AnswerDistributions = answerDistributions
            });
        }

        // Sırala
        questionDetails = questionDetails
            .OrderBy(q => q.GroupName)
            .ThenBy(q => q.Order)
            .ToList();

        // Genel ortalama (penalty sorular hariç)
        var overallAverage = questionDetails
            .Where(q => q.AverageScorePercentage.HasValue && q.ScoringTypeId != ScoringTypes.Ids.Penalty)
            .Select(q => q.AverageScorePercentage!.Value)
            .DefaultIfEmpty(0)
            .Average();

        return new SurveyQuestionScoreDetailResultDto
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            TotalResponses = evaluationIds.Count,
            OverallAverageScore = Math.Round(overallAverage, 1),
            Questions = questionDetails
        };
    }

    // ==================== ENNEAGRAM ====================

    public async Task<List<EnneagramProjectListItemDto>> GetEnneagramProjectsAsync(int customerId)
    {
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        if (!enneagramChecklistIds.Any())
            return new List<EnneagramProjectListItemDto>();

        var projects = await _context.Projects
            .Where(p => p.CustomerId == customerId &&
                   enneagramChecklistIds.Contains(p.ChecklistId) &&
                   !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var result = new List<EnneagramProjectListItemDto>();

        foreach (var project in projects)
        {
            var completedCount = await _context.Evaluations
                .Where(e => e.ProjectId == project.Id &&
                           e.StatusId == EvaluationStatuses.Ids.Completed)
                .CountAsync();

            var lastResponse = await _context.Evaluations
                .Where(e => e.ProjectId == project.Id &&
                           e.StatusId == EvaluationStatuses.Ids.Completed)
                .OrderByDescending(e => e.CompletedAt)
                .Select(e => e.CompletedAt)
                .FirstOrDefaultAsync();

            result.Add(new EnneagramProjectListItemDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                ProjectCode = project.Code,
                TotalResponses = completedCount,
                LastResponseAt = lastResponse,
                IsActive = project.IsActive
            });
        }

        return result;
    }

    public async Task<EnneagramResultsPagedDto> GetEnneagramResultsAsync(
        int customerId, int? projectId = null, string? searchTerm = null,
        int page = 1, int pageSize = 50)
    {
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        if (!enneagramChecklistIds.Any())
            return new EnneagramResultsPagedDto();

        // Temel sorgu - admin ile aynı include'lar (CalculateEnneagramScores için gerekli)
        var query = _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => !e.IsDeleted &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.Project != null &&
                        e.Project.CustomerId == customerId &&
                        enneagramChecklistIds.Contains(e.Project.ChecklistId));

        if (projectId.HasValue)
            query = query.Where(e => e.ProjectId == projectId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(e =>
                (e.EvaluatedCustomerPersonnel != null &&
                    ((e.EvaluatedCustomerPersonnel.FirstName != null && e.EvaluatedCustomerPersonnel.FirstName.ToLower().Contains(term)) ||
                     (e.EvaluatedCustomerPersonnel.LastName != null && e.EvaluatedCustomerPersonnel.LastName.ToLower().Contains(term)) ||
                     (e.EvaluatedCustomerPersonnel.Email != null && e.EvaluatedCustomerPersonnel.Email.ToLower().Contains(term)))));
        }

        var totalCount = await query.CountAsync();

        var evaluations = await query
            .OrderByDescending(e => e.CompletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // External invitations
        var evaluationIds = evaluations.Where(e => e.EvaluatedCustomerPersonnelId == null).Select(e => e.Id).ToList();
        var externalInvitations = new Dictionary<int, (string? FirstName, string? LastName, string? Email)>();
        if (evaluationIds.Any())
        {
            var extList = await _context.SurveyExternalInvitations
                .Where(sei => sei.EvaluationId != null && evaluationIds.Contains(sei.EvaluationId.Value))
                .Select(sei => new { EvalId = sei.EvaluationId!.Value, sei.FirstName, sei.LastName, sei.Email })
                .ToListAsync();
            foreach (var item in extList)
                externalInvitations[item.EvalId] = (item.FirstName, item.LastName, item.Email);
        }

        var results = new List<EnneagramResultListDto>();
        var dominantTypes = new Dictionary<string, int>();

        foreach (var eval in evaluations)
        {
            var scores = CalculateEnneagramScores(eval);
            var dominantScore = scores.OrderByDescending(s => s.Percentage).FirstOrDefault();

            if (dominantScore != null && !string.IsNullOrEmpty(dominantScore.PersonalityType))
            {
                if (!dominantTypes.ContainsKey(dominantScore.PersonalityType))
                    dominantTypes[dominantScore.PersonalityType] = 0;
                dominantTypes[dominantScore.PersonalityType]++;
            }

            string? respondentName = null;
            string? respondentEmail = null;
            if (eval.EvaluatedCustomerPersonnel != null)
            {
                respondentName = $"{eval.EvaluatedCustomerPersonnel.FirstName} {eval.EvaluatedCustomerPersonnel.LastName}".Trim();
                respondentEmail = eval.EvaluatedCustomerPersonnel.Email;
            }
            else if (externalInvitations.TryGetValue(eval.Id, out var ext))
            {
                respondentName = $"{ext.FirstName} {ext.LastName}".Trim();
                respondentEmail = ext.Email;
            }

            results.Add(new EnneagramResultListDto
            {
                EvaluationId = eval.Id,
                ProjectId = eval.ProjectId,
                ProjectName = eval.Project?.Name ?? "",
                RespondentName = string.IsNullOrWhiteSpace(respondentName) ? null : respondentName,
                RespondentEmail = respondentEmail,
                DominantType = dominantScore?.PersonalityType,
                DominantPercentage = dominantScore?.Percentage,
                TotalScore = scores.Sum(s => s.TotalPoints),
                CompletedAt = eval.CompletedAt
            });
        }

        var mostCommonType = dominantTypes.OrderByDescending(x => x.Value).FirstOrDefault().Key;
        var projectCount = evaluations.Select(e => e.ProjectId).Distinct().Count();

        return new EnneagramResultsPagedDto
        {
            Results = results,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Summary = new EnneagramSummaryDto
            {
                TotalResponses = totalCount,
                DominantType = mostCommonType,
                ProjectCount = projectCount,
                AverageCompletionRate = totalCount > 0 ? 100m : 0m
            }
        };
    }

    public async Task<EnneagramResultDetailDto?> GetEnneagramResultDetailAsync(int customerId, int evaluationId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .FirstOrDefaultAsync(e => e.Id == evaluationId &&
                   !e.IsDeleted &&
                   e.Project.CustomerId == customerId);

        if (evaluation == null)
            return null;

        // Respondent info
        string? respondentName = null;
        string? respondentEmail = null;

        if (evaluation.EvaluatedCustomerPersonnel != null)
        {
            respondentName = $"{evaluation.EvaluatedCustomerPersonnel.FirstName} {evaluation.EvaluatedCustomerPersonnel.LastName}".Trim();
            respondentEmail = evaluation.EvaluatedCustomerPersonnel.Email;
        }
        else
        {
            var ext = await _context.SurveyExternalInvitations
                .Where(sei => sei.EvaluationId == evaluationId)
                .Select(sei => new { sei.FirstName, sei.LastName, sei.Email })
                .FirstOrDefaultAsync();
            if (ext != null)
            {
                respondentName = $"{ext.FirstName} {ext.LastName}".Trim();
                respondentEmail = ext.Email;
            }
        }

        // Admin ile aynı hesaplama
        var scores = CalculateEnneagramScores(evaluation);
        var dominantScore = scores.OrderByDescending(s => s.Percentage).FirstOrDefault();

        return new EnneagramResultDetailDto
        {
            EvaluationId = evaluation.Id,
            ProjectId = evaluation.Project?.Id ?? 0,
            ProjectName = evaluation.Project?.Name ?? "",
            RespondentName = string.IsNullOrWhiteSpace(respondentName) ? null : respondentName,
            RespondentEmail = respondentEmail,
            DominantType = dominantScore?.PersonalityType,
            DominantPercentage = dominantScore?.Percentage,
            CompletedAt = evaluation.CompletedAt,
            Scores = scores
        };
    }

    public async Task<EnneagramDistributionResultDto?> GetEnneagramDistributionAsync(int customerId, int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Checklist)
            .FirstOrDefaultAsync(p => p.Id == projectId &&
                   p.CustomerId == customerId &&
                   !p.IsDeleted);

        if (project == null)
            return null;

        // Checklist Enneagram tipinde mi kontrol et (admin ile aynı)
        if (project.Checklist?.ChecklistTypeId != ChecklistTypes.Ids.Enneagram)
            return null;

        // Tamamlanmış değerlendirmeler (admin ile aynı include'lar)
        var evaluations = await _context.Evaluations
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => e.ProjectId == projectId &&
                       e.StatusId == EvaluationStatuses.Ids.Completed &&
                       !e.IsDeleted)
            .ToListAsync();

        if (!evaluations.Any())
        {
            return new EnneagramDistributionResultDto
            {
                ProjectId = projectId,
                ProjectName = project.Name,
                TotalResponses = 0,
                Distribution = new List<EnneagramDistributionDto>()
            };
        }

        // Tüm kişilik tiplerini ve puanlarını topla (admin ile aynı)
        var personalityScores = new Dictionary<string, List<decimal>>();

        foreach (var eval in evaluations)
        {
            var scores = CalculateEnneagramScores(eval);
            foreach (var score in scores)
            {
                if (!personalityScores.ContainsKey(score.PersonalityType))
                    personalityScores[score.PersonalityType] = new List<decimal>();
                personalityScores[score.PersonalityType].Add(score.Percentage);
            }
        }

        var distribution = personalityScores
            .Select(kvp => new EnneagramDistributionDto
            {
                PersonalityType = kvp.Key,
                AveragePercentage = kvp.Value.Any() ? kvp.Value.Average() : 0,
                ResponseCount = kvp.Value.Count,
                TotalPoints = (int)(kvp.Value.Any() ? kvp.Value.Average() * 50 / 100 : 0),
                MaxPoints = 50
            })
            .OrderByDescending(d => d.AveragePercentage)
            .ToList();

        return new EnneagramDistributionResultDto
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            TotalResponses = evaluations.Count,
            Distribution = distribution
        };
    }

    // ==================== HELPER ====================

    /// <summary>
    /// Enneagram kişilik puanlarını hesapla - admin ReportService.CalculateEnneagramScores ile birebir aynı
    /// </summary>
    private List<EnneagramPersonalityScoreDto> CalculateEnneagramScores(Evaluation evaluation)
    {
        var scores = new List<EnneagramPersonalityScoreDto>();

        var groupedAnswers = evaluation.Answers
            .Where(a => a.Question != null && !string.IsNullOrEmpty(a.Question.GroupName))
            .GroupBy(a => a.Question.GroupName!);

        foreach (var group in groupedAnswers)
        {
            var totalPoints = 0;
            var questionCount = 0;

            foreach (var answer in group)
            {
                var selectedPoints = answer.SubCriteriaSelections
                    .Select(sc => sc.SubCriteria?.WeightPoints ?? 0)
                    .DefaultIfEmpty(0)
                    .Max();

                totalPoints += (int)selectedPoints;
                questionCount++;
            }

            var maxPoints = questionCount * 5;
            if (maxPoints == 0) maxPoints = 50;

            var percentage = maxPoints > 0 ? (decimal)totalPoints / maxPoints * 100 : 0;

            scores.Add(new EnneagramPersonalityScoreDto
            {
                PersonalityType = group.Key,
                TotalPoints = totalPoints,
                MaxPoints = maxPoints,
                Percentage = percentage
            });
        }

        return scores.OrderByDescending(s => s.Percentage).ToList();
    }
}
