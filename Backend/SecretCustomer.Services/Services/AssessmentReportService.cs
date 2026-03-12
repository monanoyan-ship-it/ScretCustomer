using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class AssessmentReportService : IAssessmentReportService
{
    private readonly ApplicationDbContext _context;

    // Gap eşiği: %15'ten büyük fark kör nokta sayılır
    private const decimal BlindSpotThreshold = 15m;

    public AssessmentReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AssessmentPersonReport> GetPersonReportAsync(
        int projectId, int evaluatedCustomerPersonnelId, int? assignmentPeriodId = null)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(x => x.Id == projectId);

        if (project == null)
            throw new InvalidOperationException("Proje bulunamadı.");

        var minRespondents = project.MinRespondentsForAnonymity;
        var isAnonymous = project.IsAnonymous;

        // Kişi bilgisi
        var personnel = await _context.CustomerPersonnel
            .FirstOrDefaultAsync(x => x.Id == evaluatedCustomerPersonnelId);

        var personnelName = personnel != null
            ? $"{personnel.FirstName} {personnel.LastName}".Trim()
            : "Bilinmeyen";

        // Bu kişi için tamamlanmış değerlendirmeleri getir
        var evaluationsQuery = _context.Evaluations
            .Where(x => x.ProjectId == projectId
                     && x.EvaluatedCustomerPersonnelId == evaluatedCustomerPersonnelId
                     && x.FeedbackRoleId.HasValue
                     && x.ScorePercentage.HasValue
                     && x.StatusId == EvaluationStatuses.Ids.Completed);

        // Dönem filtresi
        if (assignmentPeriodId.HasValue)
        {
            evaluationsQuery = evaluationsQuery
                .Where(x => x.AssignmentPeriodId == assignmentPeriodId.Value);
        }

        var evaluations = await evaluationsQuery
            .Include(x => x.Answers)
                .ThenInclude(a => a.Question)
            .ToListAsync();

        var report = new AssessmentPersonReport
        {
            EvaluatedCustomerPersonnelId = evaluatedCustomerPersonnelId,
            PersonnelName = personnelName,
            TotalEvaluationCount = evaluations.Count
        };

        if (!evaluations.Any())
            return report;

        // Rol bazlı grupla
        var roleGroups = evaluations
            .GroupBy(x => x.FeedbackRoleId!.Value)
            .ToList();

        // Rol bazlı ortalama puanlar
        foreach (var group in roleGroups)
        {
            var roleId = group.Key;
            var roleType = FeedbackRoles.GetById(roleId);
            var count = group.Count();

            // Anonim modda MinRespondents kontrolü (Self hariç)
            var isHidden = isAnonymous
                && roleId != FeedbackRoles.Ids.Self
                && count < minRespondents;

            if (isHidden)
                report.HiddenRoleIds.Add(roleId);

            report.RoleScores.Add(new AssessmentRoleScore
            {
                FeedbackRoleId = roleId,
                RoleName = roleType?.Description ?? $"Rol {roleId}",
                AverageScorePercentage = isHidden ? 0 : Math.Round(group.Average(x => x.ScorePercentage!.Value), 2),
                EvaluationCount = count,
                IsHidden = isHidden
            });
        }

        // Yetkinlik bazlı (GroupName) kırılım
        var allAnswers = evaluations
            .SelectMany(e => e.Answers.Select(a => new
            {
                e.FeedbackRoleId,
                a.Question.GroupName,
                a.EarnedPoints,
                a.Question.WeightPoints,
                a.IsNotApplicable
            }))
            .Where(x => !string.IsNullOrEmpty(x.GroupName) && !x.IsNotApplicable && x.EarnedPoints.HasValue)
            .ToList();

        var competencyGroups = allAnswers
            .GroupBy(x => x.GroupName!)
            .ToList();

        foreach (var compGroup in competencyGroups)
        {
            var competency = new AssessmentCompetencyScore
            {
                CompetencyName = compGroup.Key
            };

            var roleSubGroups = compGroup.GroupBy(x => x.FeedbackRoleId!.Value);

            decimal? selfAvg = null;
            var othersSum = 0m;
            var othersCount = 0;

            foreach (var roleSubGroup in roleSubGroups)
            {
                var roleId = roleSubGroup.Key;
                var roleType = FeedbackRoles.GetById(roleId);
                var evalCount = roleGroups.FirstOrDefault(g => g.Key == roleId)?.Count() ?? 0;

                var isHidden = isAnonymous
                    && roleId != FeedbackRoles.Ids.Self
                    && evalCount < minRespondents;

                // Yetkinlik puanı: (EarnedPoints toplamı / WeightPoints toplamı) * 100
                var totalEarned = roleSubGroup.Sum(x => x.EarnedPoints!.Value);
                var totalWeight = roleSubGroup.Sum(x => x.WeightPoints);
                var percentage = totalWeight > 0 ? Math.Round(totalEarned / totalWeight * 100, 2) : 0;

                competency.RoleScores.Add(new AssessmentRoleScore
                {
                    FeedbackRoleId = roleId,
                    RoleName = roleType?.Description ?? $"Rol {roleId}",
                    AverageScorePercentage = isHidden ? 0 : percentage,
                    EvaluationCount = evalCount,
                    IsHidden = isHidden
                });

                if (roleId == FeedbackRoles.Ids.Self)
                {
                    selfAvg = percentage;
                }
                else if (!isHidden)
                {
                    othersSum += percentage;
                    othersCount++;
                }
            }

            // Gap hesapla
            if (selfAvg.HasValue && othersCount > 0)
            {
                var othersAvg = othersSum / othersCount;
                competency.Gap = Math.Round(selfAvg.Value - othersAvg, 2);
            }

            report.CompetencyScores.Add(competency);
        }

        // Kör nokta tespiti: Self vs Diğerleri arasında büyük fark
        foreach (var comp in report.CompetencyScores)
        {
            if (!comp.Gap.HasValue) continue;

            if (Math.Abs(comp.Gap.Value) >= BlindSpotThreshold)
            {
                var selfScore = comp.RoleScores
                    .FirstOrDefault(r => r.FeedbackRoleId == FeedbackRoles.Ids.Self);

                var othersScores = comp.RoleScores
                    .Where(r => r.FeedbackRoleId != FeedbackRoles.Ids.Self && !r.IsHidden)
                    .ToList();

                if (selfScore != null && othersScores.Any())
                {
                    report.BlindSpots.Add(new AssessmentBlindSpot
                    {
                        CompetencyName = comp.CompetencyName,
                        SelfScore = selfScore.AverageScorePercentage,
                        OthersScore = Math.Round(othersScores.Average(x => x.AverageScorePercentage), 2),
                        Gap = comp.Gap.Value
                    });
                }
            }
        }

        return report;
    }

    public async Task<AssessmentPeriodSummary> GetPeriodSummaryAsync(int projectId, int assignmentPeriodId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(x => x.Id == projectId);

        if (project == null)
            throw new InvalidOperationException("Proje bulunamadı.");

        var minRespondents = project.MinRespondentsForAnonymity;
        var isAnonymous = project.IsAnonymous;

        // Dönemdeki tüm katılımcıları al
        var participants = await _context.AssessmentParticipants
            .Include(x => x.CustomerPersonnel)
            .Where(x => x.AssignmentPeriodId == assignmentPeriodId)
            .ToListAsync();

        var personnelIds = participants.Select(x => x.CustomerPersonnelId).ToList();

        // Tamamlanmış değerlendirmeleri al
        var evaluations = await _context.Evaluations
            .Where(x => x.ProjectId == projectId
                     && x.EvaluatedCustomerPersonnelId.HasValue
                     && personnelIds.Contains(x.EvaluatedCustomerPersonnelId.Value)
                     && x.FeedbackRoleId.HasValue
                     && x.ScorePercentage.HasValue
                     && x.StatusId == EvaluationStatuses.Ids.Completed)
            .ToListAsync();

        var summary = new AssessmentPeriodSummary
        {
            ProjectId = projectId,
            AssignmentPeriodId = assignmentPeriodId
        };

        // Kişi bazlı grupla
        var personnelGroups = evaluations
            .GroupBy(x => x.EvaluatedCustomerPersonnelId!.Value);

        foreach (var group in personnelGroups)
        {
            var personnelId = group.Key;
            var participant = participants.FirstOrDefault(x => x.CustomerPersonnelId == personnelId);
            var name = participant?.CustomerPersonnel != null
                ? $"{participant.CustomerPersonnel.FirstName} {participant.CustomerPersonnel.LastName}".Trim()
                : "Bilinmeyen";

            var row = new AssessmentPersonSummaryRow
            {
                CustomerPersonnelId = personnelId,
                PersonnelName = name,
                TotalEvaluationCount = group.Count()
            };

            // Rol bazlı ortalamalar
            var roleGroups = group.GroupBy(x => x.FeedbackRoleId!.Value);

            var othersSum = 0m;
            var othersCount = 0;

            foreach (var roleGroup in roleGroups)
            {
                var roleId = roleGroup.Key;
                var count = roleGroup.Count();
                var avg = Math.Round(roleGroup.Average(x => x.ScorePercentage!.Value), 2);

                var isHidden = isAnonymous
                    && roleId != FeedbackRoles.Ids.Self
                    && count < minRespondents;

                switch (roleId)
                {
                    case FeedbackRoles.Ids.Self:
                        row.SelfScore = avg;
                        break;
                    case FeedbackRoles.Ids.Manager:
                        row.ManagerScore = isHidden ? null : avg;
                        if (!isHidden) { othersSum += avg; othersCount++; }
                        break;
                    case FeedbackRoles.Ids.Peer:
                        row.PeerScore = isHidden ? null : avg;
                        if (!isHidden) { othersSum += avg; othersCount++; }
                        break;
                    case FeedbackRoles.Ids.Subordinate:
                        row.SubordinateScore = isHidden ? null : avg;
                        if (!isHidden) { othersSum += avg; othersCount++; }
                        break;
                }
            }

            // Diğerleri ortalaması ve Gap
            if (othersCount > 0)
            {
                row.OthersAverage = Math.Round(othersSum / othersCount, 2);

                if (row.SelfScore.HasValue)
                    row.Gap = Math.Round(row.SelfScore.Value - row.OthersAverage.Value, 2);
            }

            summary.PersonnelSummaries.Add(row);
        }

        // Self score'a göre sırala
        summary.PersonnelSummaries = summary.PersonnelSummaries
            .OrderByDescending(x => x.SelfScore ?? 0)
            .ToList();

        return summary;
    }
}
