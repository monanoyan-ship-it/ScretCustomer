using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class CustomerPortalDataService : ICustomerPortalDataService
{
    private readonly ApplicationDbContext _context;
    private readonly ICustomerScoreThresholdService _customerScoreThresholdService;

    public CustomerPortalDataService(ApplicationDbContext context, ICustomerScoreThresholdService customerScoreThresholdService)
    {
        _context = context;
        _customerScoreThresholdService = customerScoreThresholdService;
    }

    public async Task<object> GetProjectsAsync(int customerId, int? projectTypeId, List<int>? allowedPersonnelIds)
    {
        var query = _context.Projects
            .Where(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted);

        // Proje tipi filtresi
        if (projectTypeId.HasValue)
            query = query.Where(p => p.ProjectTypeId == projectTypeId.Value);

        // Supervisor için sadece kendi personelinin değerlendirildiği projeler
        if (allowedPersonnelIds != null)
        {
            var projectIdsWithPersonnel = await _context.Evaluations
                .Where(e => e.EvaluatedCustomerPersonnelId.HasValue &&
                           allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                           e.Project != null &&
                           e.Project.CustomerId == customerId &&
                           e.StatusId == EvaluationStatuses.Ids.Completed)
                .Select(e => e.ProjectId)
                .Distinct()
                .ToListAsync();

            query = query.Where(p => projectIdsWithPersonnel.Contains(p.Id));
        }

        var projects = await query
            .Select(p => new
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                City = "",
                Address = "",
                IsActive = p.IsActive,
                evaluationCount = allowedPersonnelIds == null
                    ? _context.Evaluations.Count(e => e.ProjectId == p.Id &&
                        e.StatusId == EvaluationStatuses.Ids.Completed)
                    : _context.Evaluations.Count(e => e.ProjectId == p.Id &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.EvaluatedCustomerPersonnelId.HasValue &&
                        allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value)),
                averageScore = allowedPersonnelIds == null
                    ? _context.Evaluations
                        .Where(e => e.ProjectId == p.Id && e.ScorePercentage.HasValue &&
                            e.StatusId == EvaluationStatuses.Ids.Completed)
                        .Average(e => (double?)e.ScorePercentage) ?? 0
                    : _context.Evaluations
                        .Where(e => e.ProjectId == p.Id && e.ScorePercentage.HasValue &&
                            e.StatusId == EvaluationStatuses.Ids.Completed &&
                            e.EvaluatedCustomerPersonnelId.HasValue &&
                            allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value))
                        .Average(e => (double?)e.ScorePercentage) ?? 0
            })
            .OrderBy(p => p.Name)
            .ToListAsync();

        return projects;
    }

    public async Task<object> GetOrganizationsAsync(int customerId, List<int>? allowedOrgIds)
    {
        var query = _context.CustomerOrganizations
            .Where(o => o.CustomerId == customerId && o.IsActive && !o.IsDeleted);

        // Supervisor için sadece yetkili olduğu organizasyonları filtrele
        if (allowedOrgIds != null)
            query = query.Where(o => allowedOrgIds.Contains(o.Id));

        var organizations = await query
            .OrderBy(o => o.ParentId)
            .ThenBy(o => o.Order)
            .ThenBy(o => o.Name)
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.Code,
                o.Description,
                o.ParentId,
                parentName = o.Parent != null ? o.Parent.Name : null,
                o.Level,
                o.Order,
                personnelCount = _context.CustomerPersonnelOrganizations
                    .Count(cpo => cpo.CustomerOrganizationId == o.Id &&
                                  !cpo.CustomerPersonnel.IsDeleted &&
                                  cpo.CustomerPersonnel.IsActive),
                evaluationCount = _context.Evaluations
                    .Count(e => e.StatusId == EvaluationStatuses.Ids.Completed &&
                               (e.EvaluatedOrganizationId == o.Id ||
                                (e.EvaluatedCustomerPersonnelId.HasValue &&
                                 _context.CustomerPersonnelOrganizations
                                     .Any(cpo => cpo.CustomerPersonnelId == e.EvaluatedCustomerPersonnelId.Value &&
                                                 cpo.CustomerOrganizationId == o.Id)))),
                averageScore = _context.Evaluations
                    .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed &&
                               e.ScorePercentage.HasValue &&
                               (e.EvaluatedOrganizationId == o.Id ||
                                (e.EvaluatedCustomerPersonnelId.HasValue &&
                                 _context.CustomerPersonnelOrganizations
                                     .Any(cpo => cpo.CustomerPersonnelId == e.EvaluatedCustomerPersonnelId.Value &&
                                                 cpo.CustomerOrganizationId == o.Id))))
                    .Average(e => (double?)e.ScorePercentage) ?? 0
            })
            .ToListAsync();

        // Group by parent (null parent = independent/root level)
        var grouped = organizations
            .GroupBy(o => o.parentName ?? "Bağımsız")
            .Select(g => new
            {
                groupName = g.Key,
                organizations = g.ToList()
            })
            .OrderBy(g => g.groupName == "Bağımsız" ? "" : g.groupName) // Bağımsız en başa
            .ToList();

        return grouped;
    }

    public async Task<object> GetSupervisorsAsync(int customerId, List<int>? allowedOrgIds, List<int>? organizationIds, string? searchText)
    {
        // Süpervizör olan personelleri bul (CustomerPersonnelOrganization'da SupervisorId olarak geçenler)
        var supervisorIdsQuery = _context.CustomerPersonnelOrganizations
            .Where(cpo => cpo.SupervisorId.HasValue &&
                          cpo.CustomerOrganization.CustomerId == customerId);

        // Supervisor için sadece yetkili olduğu organizasyonları filtrele
        if (allowedOrgIds != null)
            supervisorIdsQuery = supervisorIdsQuery.Where(cpo => allowedOrgIds.Contains(cpo.CustomerOrganizationId));

        // Organizasyon filtresi
        if (organizationIds?.Any() == true)
        {
            supervisorIdsQuery = supervisorIdsQuery.Where(cpo =>
                organizationIds.Contains(cpo.CustomerOrganizationId));
        }

        var supervisorIds = await supervisorIdsQuery
            .Select(cpo => cpo.SupervisorId!.Value)
            .Distinct()
            .ToListAsync();

        // Her süpervizörün takımındaki personel ID'lerini al (organizasyon filtresine göre)
        var supervisorTeamsQuery = _context.CustomerPersonnelOrganizations
            .Where(cpo => cpo.SupervisorId.HasValue && supervisorIds.Contains(cpo.SupervisorId.Value));

        // Organizasyon filtresi uygula
        if (organizationIds?.Any() == true)
            supervisorTeamsQuery = supervisorTeamsQuery.Where(cpo => organizationIds.Contains(cpo.CustomerOrganizationId));

        var supervisorTeams = await supervisorTeamsQuery
            .GroupBy(cpo => cpo.SupervisorId!.Value)
            .Select(g => new
            {
                SupervisorId = g.Key,
                TeamMemberIds = g.Select(x => x.CustomerPersonnelId).Distinct().ToList()
            })
            .ToListAsync();

        var supervisorTeamDict = supervisorTeams.ToDictionary(x => x.SupervisorId, x => x.TeamMemberIds);

        var supervisorsQuery = _context.CustomerPersonnel
            .Where(cp => supervisorIds.Contains(cp.Id) && cp.IsActive && !cp.IsDeleted);

        // Metin arama filtresi
        if (!string.IsNullOrEmpty(searchText))
        {
            var searchLower = searchText.ToLower();
            supervisorsQuery = supervisorsQuery.Where(cp =>
                (cp.FirstName + " " + cp.LastName).ToLower().Contains(searchLower) ||
                (cp.Title != null && cp.Title.ToLower().Contains(searchLower)));
        }

        // Organizasyon filtresi için liste (boşsa tüm organizasyonlar)
        var orgFilterIds = organizationIds?.Any() == true ? organizationIds : null;

        var supervisors = await supervisorsQuery
            .OrderBy(cp => cp.FirstName).ThenBy(cp => cp.LastName)
            .Select(cp => new
            {
                cp.Id,
                fullName = cp.FirstName + " " + cp.LastName,
                cp.Email,
                cp.Title,
                organizations = _context.CustomerPersonnelOrganizations
                    .Where(cpo => cpo.SupervisorId == cp.Id &&
                                  (orgFilterIds == null || orgFilterIds.Contains(cpo.CustomerOrganizationId)))
                    .Select(cpo => new { cpo.CustomerOrganization.Id, cpo.CustomerOrganization.Name })
                    .Distinct()
                    .ToList(),
                personnelCount = _context.CustomerPersonnelOrganizations
                    .Count(cpo => cpo.SupervisorId == cp.Id &&
                                  (orgFilterIds == null || orgFilterIds.Contains(cpo.CustomerOrganizationId)))
            })
            .ToListAsync();

        // Takım bazlı değerlendirme sayısı ve ortalamasını hesapla (takımın ALDIĞI değerlendirmeler)
        var result = supervisors.Select(s =>
        {
            var teamMemberIds = supervisorTeamDict.ContainsKey(s.Id) ? supervisorTeamDict[s.Id] : new List<int>();

            var evaluationCount = _context.Evaluations
                .Count(e => e.EvaluatedCustomerPersonnelId.HasValue &&
                           teamMemberIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                           e.StatusId == EvaluationStatuses.Ids.Completed);

            var averageScore = _context.Evaluations
                .Where(e => e.EvaluatedCustomerPersonnelId.HasValue &&
                           teamMemberIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                           e.StatusId == EvaluationStatuses.Ids.Completed &&
                           e.ScorePercentage.HasValue)
                .Average(e => (double?)e.ScorePercentage) ?? 0;

            return new
            {
                s.Id,
                s.fullName,
                s.Email,
                s.Title,
                s.organizations,
                s.personnelCount,
                evaluationCount,
                averageScore
            };
        }).ToList();

        // Group by first organization
        var grouped = result
            .GroupBy(s => s.organizations.FirstOrDefault()?.Name ?? "Atanmamış")
            .Select(g => new
            {
                groupName = g.Key,
                supervisors = g.ToList()
            })
            .OrderBy(g => g.groupName == "Atanmamış" ? "ZZZZ" : g.groupName)
            .ToList();

        return grouped;
    }

    public async Task<object?> GetSupervisorMonthlyTrendAsync(int customerId, int supervisorId)
    {
        // Süpervizörü doğrula
        var supervisor = await _context.CustomerPersonnel
            .FirstOrDefaultAsync(cp => cp.Id == supervisorId && cp.CustomerId == customerId && !cp.IsDeleted);

        if (supervisor == null)
            return null;

        // Süpervizörün takımındaki personel ID'lerini al
        var teamMemberIds = await _context.CustomerPersonnelOrganizations
            .Where(cpo => cpo.SupervisorId == supervisorId)
            .Select(cpo => cpo.CustomerPersonnelId)
            .Distinct()
            .ToListAsync();

        var now = TurkeyTime.Now;
        var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);

        // Takımın aldığı değerlendirmeleri al
        var evaluations = await _context.Evaluations
            .Where(e => e.EvaluatedCustomerPersonnelId.HasValue &&
                        teamMemberIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        (e.CallDate ?? e.ControlDate) >= startDate)
            .ToListAsync();

        // Aylık trend verisi oluştur
        var monthlyData = new List<object>();
        for (int i = 0; i < 12; i++)
        {
            var monthStart = startDate.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);

            var monthEvals = evaluations.Where(e => (e.CallDate ?? e.ControlDate) >= monthStart && (e.CallDate ?? e.ControlDate) < monthEnd).ToList();
            var withScore = monthEvals.Where(e => e.ScorePercentage.HasValue).ToList();
            var avgScore = withScore.Any() ? withScore.Average(e => (double)e.ScorePercentage!.Value) : 0;

            monthlyData.Add(new
            {
                month = monthStart.ToString("MMM", new System.Globalization.CultureInfo("tr-TR")),
                year = monthStart.Year,
                count = monthEvals.Count,
                averageScore = Math.Round(avgScore, 2)
            });
        }

        return new
        {
            supervisorId,
            supervisorName = supervisor.FirstName + " " + supervisor.LastName,
            teamMemberCount = teamMemberIds.Count,
            monthlyTrend = monthlyData
        };
    }

    // ==================== DASHBOARD ====================

    public async Task<object> GetDashboardStatsAsync(int customerId, List<int>? allowedPersonnelIds)
    {
        // Organizasyon sayısı (Supervisor için kendi organizasyonları)
        int organizationCount;
        if (allowedPersonnelIds == null)
        {
            organizationCount = await _context.CustomerOrganizations
                .CountAsync(o => o.CustomerId == customerId && !o.IsDeleted && o.IsActive);
        }
        else
        {
            organizationCount = await _context.CustomerPersonnelOrganizations
                .Where(cpo => allowedPersonnelIds.Contains(cpo.CustomerPersonnelId))
                .Select(cpo => cpo.CustomerOrganizationId)
                .Distinct()
                .CountAsync();
        }

        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Project)
            .Where(e => e.Project != null &&
                        e.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed);

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        var evaluations = await evaluationsQuery.ToListAsync();

        var totalEvaluations = evaluations.Count;
        var averageScore = evaluations.Any() ? evaluations.Average(e => e.ScorePercentage ?? 0) : 0;

        var thisMonth = TurkeyTime.Now.Month;
        var thisYear = TurkeyTime.Now.Year;
        var thisMonthEvaluations = evaluations.Count(e =>
            (e.CallDate ?? e.ControlDate)?.Month == thisMonth && (e.CallDate ?? e.ControlDate)?.Year == thisYear);

        return new
        {
            organizationCount,
            totalEvaluations,
            averageScore = Math.Round(averageScore, 2),
            thisMonthEvaluations
        };
    }

    public async Task<object> GetMonthlyTrendAsync(int customerId, List<int>? allowedPersonnelIds, int? projectId, int? projectTypeId, DateTime? startDate, DateTime? endDate)
    {
        var now = TurkeyTime.Now;
        var defaultStartDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
        var effectiveStartDate = startDate.HasValue
            ? DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc)
            : defaultStartDate;

        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Where(e => e.Project != null &&
                        e.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        (e.CallDate ?? e.ControlDate) >= effectiveStartDate);

        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => (e.CallDate ?? e.ControlDate) <= end);
        }

        if (projectId.HasValue)
        {
            evaluationsQuery = evaluationsQuery.Where(e => e.ProjectId == projectId.Value);
        }

        if (projectTypeId.HasValue)
        {
            evaluationsQuery = evaluationsQuery.Where(e => e.Project!.ProjectTypeId == projectTypeId.Value);
        }

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        var evaluations = await evaluationsQuery.ToListAsync();

        var loopStart = new DateTime(effectiveStartDate.Year, effectiveStartDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var loopEnd = endDate.HasValue
            ? DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc)
            : now;
        var monthCount = ((loopEnd.Year - loopStart.Year) * 12) + loopEnd.Month - loopStart.Month + 1;
        if (monthCount < 1) monthCount = 1;
        if (monthCount > 36) monthCount = 36;

        var monthlyData = new List<object>();
        for (int i = 0; i < monthCount; i++)
        {
            var monthStart = loopStart.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);

            var monthEvals = evaluations.Where(e => (e.CallDate ?? e.ControlDate) >= monthStart && (e.CallDate ?? e.ControlDate) < monthEnd).ToList();
            var withScore = monthEvals.Where(e => e.ScorePercentage.HasValue).ToList();
            var avgScore = withScore.Any() ? withScore.Average(e => (double)e.ScorePercentage!.Value) : 0;

            var yellowCardCount = monthEvals.Sum(e => e.YellowCardCount);
            var redCardCount = monthEvals.Sum(e => e.RedCardCount);

            monthlyData.Add(new
            {
                month = monthStart.ToString("MMM", new System.Globalization.CultureInfo("tr-TR")),
                year = monthStart.Year,
                count = monthEvals.Count,
                averageScore = Math.Round(avgScore, 2),
                yellowCardCount,
                redCardCount
            });
        }

        return monthlyData;
    }

    public async Task<object> GetMonthlyTrendByTypeAsync(int customerId, List<int>? allowedPersonnelIds, DateTime? startDate, DateTime? endDate, int? projectTypeId = null, int? projectId = null, int? checklistTypeId = null)
    {
        var now = TurkeyTime.Now;
        var defaultStartDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
        var effectiveStartDate = startDate.HasValue
            ? DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc)
            : defaultStartDate;

        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Where(e => e.Project != null &&
                        e.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        (e.CallDate ?? e.ControlDate ?? e.CreatedAt) >= effectiveStartDate);

        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => (e.CallDate ?? e.ControlDate ?? e.CreatedAt) <= end);
        }

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        // Belirli proje tipine göre filtrele (panel bazlı yenileme için)
        if (projectTypeId.HasValue)
        {
            evaluationsQuery = evaluationsQuery.Where(e => e.Project.ProjectTypeId == projectTypeId.Value);
        }

        // Belirli projeye göre filtrele
        if (projectId.HasValue)
        {
            evaluationsQuery = evaluationsQuery.Where(e => e.ProjectId == projectId.Value);
        }

        // Belirli checklist tipine göre filtrele
        if (checklistTypeId.HasValue)
        {
            evaluationsQuery = evaluationsQuery.Where(e => e.Project.Checklist != null && e.Project.Checklist.ChecklistTypeId == checklistTypeId.Value);
        }

        var evaluations = await evaluationsQuery.ToListAsync();

        // ChecklistType'a göre grupla
        var groupedByType = evaluations
            .Where(e => e.Project?.Checklist != null)
            .GroupBy(e => e.Project!.Checklist!.ChecklistTypeId)
            .ToList();

        // Her ChecklistType için proje listesi
        var projectsByType = evaluations
            .Where(e => e.Project?.Checklist != null)
            .Select(e => new { e.Project!.Checklist!.ChecklistTypeId, e.ProjectId, e.Project.Name, e.Project.Code })
            .Distinct()
            .GroupBy(p => p.ChecklistTypeId)
            .ToDictionary(g => g.Key, g => g.Select(p => new { id = p.ProjectId, name = p.Name, code = p.Code }).DistinctBy(p => p.id).OrderBy(p => p.name).Select(p => (object)p).ToList());

        var loopStart = new DateTime(effectiveStartDate.Year, effectiveStartDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var loopEnd = endDate.HasValue
            ? DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc)
            : now;
        var monthCount = ((loopEnd.Year - loopStart.Year) * 12) + loopEnd.Month - loopStart.Month + 1;
        if (monthCount < 1) monthCount = 1;
        if (monthCount > 36) monthCount = 36;

        var result = new List<object>();

        foreach (var group in groupedByType.OrderBy(g => g.Key))
        {
            var checklistType = ChecklistTypes.GetById(group.Key);
            if (checklistType == null) continue;

            var projects = projectsByType.ContainsKey(group.Key)
                ? projectsByType[group.Key]
                : new List<object>();

            object panel;
            switch (group.Key)
            {
                case ChecklistTypes.Ids.CallPerformance:
                case ChecklistTypes.Ids.PhysicalAudit:
                case ChecklistTypes.Ids.MysteryShopping:
                    panel = BuildScoreTrendPanel(group.Key, checklistType, projects, group.ToList(), loopStart, monthCount);
                    break;
                case ChecklistTypes.Ids.OnlineEvaluation:
                    panel = BuildScoreTrendNoCardsPanel(group.Key, checklistType, projects, group.ToList(), loopStart, monthCount);
                    break;
                case ChecklistTypes.Ids.Survey:
                    panel = await BuildSurveyPanelAsync(customerId, checklistType, projects, group.ToList(), loopStart, monthCount);
                    break;
                case ChecklistTypes.Ids.Enneagram:
                    panel = await BuildEnneagramPanelAsync(customerId, checklistType, projects, group.ToList(), loopStart, monthCount);
                    break;
                default:
                    panel = BuildScoreTrendPanel(group.Key, checklistType, projects, group.ToList(), loopStart, monthCount);
                    break;
            }

            result.Add(panel);
        }

        return result;
    }

    /// <summary>
    /// Puanlı + Kartlı panel (Çağrı, Fiziksel, Gizli Müşteri)
    /// </summary>
    private object BuildScoreTrendPanel(int typeId, TypeItem checklistType, List<object> projects, List<Evaluation> evals, DateTime loopStart, int monthCount)
    {
        var trend = BuildMonthlyTrend(evals, loopStart, monthCount, includeScore: true, includeCards: true);
        return new
        {
            projectTypeId = typeId,
            projectTypeName = checklistType.Description ?? checklistType.SystemName,
            projectTypeIcon = checklistType.Icon ?? "bi-folder",
            panelType = "scoreTrend",
            projects,
            trend
        };
    }

    /// <summary>
    /// Puanlı + Kartsız panel (Online Değerlendirme)
    /// </summary>
    private object BuildScoreTrendNoCardsPanel(int typeId, TypeItem checklistType, List<object> projects, List<Evaluation> evals, DateTime loopStart, int monthCount)
    {
        var trend = BuildMonthlyTrend(evals, loopStart, monthCount, includeScore: true, includeCards: false);
        return new
        {
            projectTypeId = typeId,
            projectTypeName = checklistType.Description ?? checklistType.SystemName,
            projectTypeIcon = checklistType.Icon ?? "bi-folder",
            panelType = "scoreTrendNoCards",
            projects,
            trend
        };
    }

    /// <summary>
    /// Anket paneli - yanıt trendi + özet kartlar
    /// </summary>
    private async Task<object> BuildSurveyPanelAsync(int customerId, TypeItem checklistType, List<object> projects, List<Evaluation> evals, DateTime loopStart, int monthCount)
    {
        // Anket projelerinin ID'lerini al
        var surveyProjectIds = evals.Select(e => e.ProjectId).Distinct().ToList();

        // Davetiye sayıları (internal + external)
        var internalInvCount = await _context.SurveyInvitations
            .Where(si => surveyProjectIds.Contains(si.ProjectId))
            .CountAsync();

        var externalInvCount = await _context.SurveyExternalInvitations
            .Where(si => surveyProjectIds.Contains(si.ProjectId))
            .CountAsync();

        var totalInvitations = internalInvCount + externalInvCount;
        var totalResponses = evals.Count;
        var responseRate = totalInvitations > 0 ? Math.Round((decimal)totalResponses / totalInvitations * 100, 2) : 0m;

        // Ortalama puan
        var withScore = evals.Where(e => e.ScorePercentage.HasValue).ToList();
        var averageScore = withScore.Any() ? Math.Round((double)withScore.Average(e => (double)e.ScorePercentage!.Value), 2) : 0.0;

        // Aylık yanıt trendi (yanıt sayısı + ortalama puan)
        var trend = new List<object>();
        for (int i = 0; i < monthCount; i++)
        {
            var monthStart = loopStart.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);
            var monthEvals = evals.Where(e => e.CreatedAt >= monthStart && e.CreatedAt < monthEnd).ToList();
            var monthWithScore = monthEvals.Where(e => e.ScorePercentage.HasValue).ToList();
            var monthAvgScore = monthWithScore.Any()
                ? Math.Round((double)monthWithScore.Average(e => (double)e.ScorePercentage!.Value), 2)
                : (double?)null;

            trend.Add(new
            {
                month = monthStart.ToString("MMM", new System.Globalization.CultureInfo("tr-TR")),
                year = monthStart.Year,
                responseCount = monthEvals.Count,
                averageScore = monthAvgScore
            });
        }

        return new
        {
            projectTypeId = ChecklistTypes.Ids.Survey,
            projectTypeName = checklistType.Description ?? checklistType.SystemName,
            projectTypeIcon = checklistType.Icon ?? "bi-folder",
            panelType = "survey",
            projects,
            summary = new
            {
                projectCount = surveyProjectIds.Count,
                totalInvitations,
                totalResponses,
                responseRate,
                averageScore
            },
            trend
        };
    }

    /// <summary>
    /// Enneagram paneli - yanıt trendi + kişilik dağılımı
    /// </summary>
    private async Task<object> BuildEnneagramPanelAsync(int customerId, TypeItem checklistType, List<object> projects, List<Evaluation> evals, DateTime loopStart, int monthCount)
    {
        var projectIds = evals.Select(e => e.ProjectId).Distinct().ToList();

        // Kişilik dağılımı hesapla - answers + subcriteria gerekiyor, lazy load
        var evalIds = evals.Select(e => e.Id).ToList();
        var evalsWithAnswers = await _context.Evaluations
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => evalIds.Contains(e.Id))
            .ToListAsync();

        // Genel kişilik dağılımı hesapla
        var personalityScores = CalculatePersonalityScores(evalsWithAnswers);

        var distribution = personalityScores
            .Select(kvp => new
            {
                personalityType = kvp.Key,
                averagePercentage = kvp.Value.Any() ? Math.Round((double)kvp.Value.Average(), 2) : 0.0,
                responseCount = kvp.Value.Count
            })
            .OrderByDescending(d => d.averagePercentage)
            .ToList();

        var dominantType = distribution.FirstOrDefault()?.personalityType;

        // Tüm kişilik tiplerini topla (sıralama tutarlı olsun)
        var allTypes = distribution.Select(d => d.personalityType).ToList();

        // Aylık yanıt trendi + tip bazlı puan trendi
        var trend = new List<object>();
        var typeTrend = new List<object>();
        for (int i = 0; i < monthCount; i++)
        {
            var monthStart = loopStart.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);
            var monthEvals = evals.Where(e => e.CreatedAt >= monthStart && e.CreatedAt < monthEnd).ToList();
            var monthLabel = monthStart.ToString("MMM", new System.Globalization.CultureInfo("tr-TR"));

            trend.Add(new
            {
                month = monthLabel,
                year = monthStart.Year,
                responseCount = monthEvals.Count
            });

            // Aylık kişilik tipi puanları
            var monthEvalIds = monthEvals.Select(e => e.Id).ToHashSet();
            var monthEvalsWithAnswers = evalsWithAnswers.Where(e => monthEvalIds.Contains(e.Id)).ToList();
            var monthScores = CalculatePersonalityScores(monthEvalsWithAnswers);
            var types = new Dictionary<string, double?>();
            foreach (var t in allTypes)
            {
                types[t] = monthScores.ContainsKey(t) && monthScores[t].Any()
                    ? Math.Round((double)monthScores[t].Average(), 2)
                    : (double?)null;
            }
            typeTrend.Add(new { month = monthLabel, year = monthStart.Year, types });
        }

        return new
        {
            projectTypeId = ChecklistTypes.Ids.Enneagram,
            projectTypeName = checklistType.Description ?? checklistType.SystemName,
            projectTypeIcon = checklistType.Icon ?? "bi-folder",
            panelType = "enneagram",
            projects,
            summary = new
            {
                totalResponses = evals.Count,
                dominantType = dominantType ?? "-",
                projectCount = projectIds.Count
            },
            trend,
            distribution,
            typeTrend
        };
    }

    /// <summary>
    /// Enneagram kişilik tipi puanlarını hesapla (GroupName bazlı)
    /// </summary>
    private static Dictionary<string, List<decimal>> CalculatePersonalityScores(List<Evaluation> evalsWithAnswers)
    {
        var scores = new Dictionary<string, List<decimal>>();
        foreach (var eval in evalsWithAnswers)
        {
            var groupedAnswers = eval.Answers
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

                if (!scores.ContainsKey(group.Key))
                    scores[group.Key] = new List<decimal>();
                scores[group.Key].Add(percentage);
            }
        }
        return scores;
    }

    /// <summary>
    /// Ortak aylık trend builder
    /// </summary>
    private List<object> BuildMonthlyTrend(List<Evaluation> evals, DateTime loopStart, int monthCount, bool includeScore, bool includeCards)
    {
        var monthlyData = new List<object>();
        for (int i = 0; i < monthCount; i++)
        {
            var monthStart = loopStart.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);

            var monthEvals = evals.Where(e => (e.CallDate ?? e.ControlDate) >= monthStart && (e.CallDate ?? e.ControlDate) < monthEnd).ToList();
            var withScore = monthEvals.Where(e => e.ScorePercentage.HasValue).ToList();
            var avgScore = withScore.Any() ? withScore.Average(e => (double)e.ScorePercentage!.Value) : 0;

            if (includeCards)
            {
                monthlyData.Add(new
                {
                    month = monthStart.ToString("MMM", new System.Globalization.CultureInfo("tr-TR")),
                    year = monthStart.Year,
                    count = monthEvals.Count,
                    averageScore = Math.Round(avgScore, 2),
                    yellowCardCount = monthEvals.Sum(e => e.YellowCardCount),
                    redCardCount = monthEvals.Sum(e => e.RedCardCount)
                });
            }
            else
            {
                monthlyData.Add(new
                {
                    month = monthStart.ToString("MMM", new System.Globalization.CultureInfo("tr-TR")),
                    year = monthStart.Year,
                    count = monthEvals.Count,
                    averageScore = Math.Round(avgScore, 2)
                });
            }
        }
        return monthlyData;
    }

    public async Task<object> GetQuestionGroupTrendAsync(int customerId, List<int>? allowedPersonnelIds, List<int>? projectIds, DateTime? startDate, DateTime? endDate)
    {
        var now = TurkeyTime.Now;
        var effectiveStartDate = startDate.HasValue
            ? DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc)
            : new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);

        var projectQuery = _context.Projects
            .Where(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted);

        // Operator/Supervisor: sadece kendi değerlendirmelerinin olduğu projeler
        if (allowedPersonnelIds != null)
        {
            var projectIdsWithPersonnel = await _context.Evaluations
                .Where(e => e.EvaluatedCustomerPersonnelId.HasValue &&
                           allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                           e.Project != null &&
                           e.Project.CustomerId == customerId &&
                           e.StatusId == EvaluationStatuses.Ids.Completed)
                .Select(e => e.ProjectId)
                .Distinct()
                .ToListAsync();

            projectQuery = projectQuery.Where(p => projectIdsWithPersonnel.Contains(p.Id));
        }

        var projects = await projectQuery
            .Select(p => new { p.Id, p.Name, p.Code })
            .OrderBy(p => p.Name)
            .ToListAsync();

        var answersQuery = _context.Answers
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Project)
            .Include(a => a.Question)
            .Where(a => a.Evaluation.Project != null &&
                        a.Evaluation.Project.CustomerId == customerId &&
                        a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed &&
                        (a.Evaluation.CallDate ?? a.Evaluation.ControlDate) >= effectiveStartDate &&
                        a.Question.GroupName != null &&
                        a.Question.GroupName != "" &&
                        a.EarnedPoints.HasValue &&
                        a.Question.WeightPoints > 0);

        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            answersQuery = answersQuery.Where(a => (a.Evaluation.CallDate ?? a.Evaluation.ControlDate) <= end);
        }

        if (projectIds?.Any() == true)
        {
            answersQuery = answersQuery.Where(a => projectIds.Contains(a.Evaluation.ProjectId));
        }

        if (allowedPersonnelIds != null)
        {
            answersQuery = answersQuery.Where(a =>
                a.Evaluation.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(a.Evaluation.EvaluatedCustomerPersonnelId.Value));
        }

        var answers = await answersQuery
            .Select(a => new
            {
                EvalDate = a.Evaluation.CallDate ?? a.Evaluation.ControlDate,
                a.Question.GroupName,
                a.EarnedPoints,
                a.Question.WeightPoints
            })
            .ToListAsync();

        var groupNames = answers.Select(a => a.GroupName).Distinct().OrderBy(g => g).ToList();

        var loopStart = new DateTime(effectiveStartDate.Year, effectiveStartDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var loopEnd = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc) : now;
        var monthCount = ((loopEnd.Year - loopStart.Year) * 12) + loopEnd.Month - loopStart.Month + 1;
        if (monthCount < 1) monthCount = 1;
        if (monthCount > 36) monthCount = 36;

        var monthLabels = new List<string>();
        for (int i = 0; i < monthCount; i++)
        {
            var monthDate = loopStart.AddMonths(i);
            monthLabels.Add(monthDate.ToString("MMM", new System.Globalization.CultureInfo("tr-TR")));
        }

        var groupTrends = new List<object>();
        foreach (var groupName in groupNames)
        {
            var monthlyScores = new List<double>();
            for (int i = 0; i < monthCount; i++)
            {
                var monthStart = loopStart.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1);

                var monthAnswers = answers.Where(a =>
                    a.GroupName == groupName &&
                    a.EvalDate >= monthStart &&
                    a.EvalDate < monthEnd).ToList();

                double avgScore = 0;
                if (monthAnswers.Any())
                {
                    avgScore = monthAnswers.Average(a =>
                        (double)(a.EarnedPoints!.Value / a.WeightPoints * 100));
                }
                monthlyScores.Add(Math.Round(avgScore, 2));
            }

            groupTrends.Add(new
            {
                groupName,
                scores = monthlyScores
            });
        }

        return new
        {
            projects,
            selectedProjectIds = projectIds,
            monthLabels,
            groupTrends
        };
    }

    public async Task<object> GetQuestionTrendAsync(int customerId, List<int>? allowedPersonnelIds, List<int>? projectIds, string? groupName, DateTime? startDate, DateTime? endDate)
    {
        var now = TurkeyTime.Now;
        var effectiveStartDate = startDate.HasValue
            ? DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc)
            : new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);

        var answersQuery = _context.Answers
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Project)
            .Include(a => a.Question)
            .Where(a => a.Evaluation.Project != null &&
                        a.Evaluation.Project.CustomerId == customerId &&
                        a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed &&
                        (a.Evaluation.CallDate ?? a.Evaluation.ControlDate) >= effectiveStartDate &&
                        a.EarnedPoints.HasValue &&
                        a.Question.WeightPoints > 0 &&
                        a.Question.ScoringTypeId == ScoringTypes.Ids.Scored);

        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            answersQuery = answersQuery.Where(a => (a.Evaluation.CallDate ?? a.Evaluation.ControlDate) <= end);
        }

        if (projectIds?.Any() == true)
        {
            answersQuery = answersQuery.Where(a => projectIds.Contains(a.Evaluation.ProjectId));
        }

        if (!string.IsNullOrEmpty(groupName))
        {
            answersQuery = answersQuery.Where(a => a.Question.GroupName == groupName);
        }

        if (allowedPersonnelIds != null)
        {
            answersQuery = answersQuery.Where(a =>
                a.Evaluation.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(a.Evaluation.EvaluatedCustomerPersonnelId.Value));
        }

        var answers = await answersQuery
            .Select(a => new
            {
                EvalDate = a.Evaluation.CallDate ?? a.Evaluation.ControlDate,
                a.QuestionId,
                QuestionText = a.Question.Text,
                a.Question.GroupName,
                a.Question.Order,
                a.EarnedPoints,
                a.Question.WeightPoints
            })
            .ToListAsync();

        var questions = answers
            .GroupBy(a => new { a.QuestionId, a.QuestionText, a.GroupName, a.Order })
            .Select(g => new
            {
                g.Key.QuestionId,
                g.Key.QuestionText,
                g.Key.GroupName,
                g.Key.Order,
                AnswerCount = g.Count()
            })
            .OrderByDescending(q => q.AnswerCount)
            .Take(10)
            .OrderBy(q => q.GroupName)
            .ThenBy(q => q.Order)
            .ToList();

        var loopStart = new DateTime(effectiveStartDate.Year, effectiveStartDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var loopEnd = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc) : now;
        var monthCount = ((loopEnd.Year - loopStart.Year) * 12) + loopEnd.Month - loopStart.Month + 1;
        if (monthCount < 1) monthCount = 1;
        if (monthCount > 36) monthCount = 36;

        var monthLabels = new List<string>();
        for (int i = 0; i < monthCount; i++)
        {
            var monthDate = loopStart.AddMonths(i);
            monthLabels.Add(monthDate.ToString("MMM", new System.Globalization.CultureInfo("tr-TR")));
        }

        var questionTrends = new List<object>();
        foreach (var question in questions)
        {
            var monthlyScores = new List<double>();
            for (int i = 0; i < monthCount; i++)
            {
                var monthStart = loopStart.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1);

                var monthAnswers = answers.Where(a =>
                    a.QuestionId == question.QuestionId &&
                    a.EvalDate >= monthStart &&
                    a.EvalDate < monthEnd).ToList();

                double avgScore = 0;
                if (monthAnswers.Any())
                {
                    avgScore = monthAnswers.Average(a =>
                        (double)(a.EarnedPoints!.Value / a.WeightPoints * 100));
                }
                monthlyScores.Add(Math.Round(avgScore, 2));
            }

            questionTrends.Add(new
            {
                questionId = question.QuestionId,
                questionText = question.QuestionText.Length > 50
                    ? question.QuestionText.Substring(0, 47) + "..."
                    : question.QuestionText,
                groupName = question.GroupName,
                scores = monthlyScores
            });
        }

        var groupNames = await _context.Questions
            .Where(q => q.GroupName != null && q.GroupName != "")
            .Select(q => q.GroupName)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();

        return new
        {
            groupNames,
            selectedGroupName = groupName,
            monthLabels,
            questionTrends
        };
    }

    public async Task<object> GetScoreDistributionAsync(int customerId, List<int>? allowedPersonnelIds, int? projectId, DateTime? startDate, DateTime? endDate)
    {
        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Where(e => e.Project != null &&
                        e.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed);

        if (projectId.HasValue)
        {
            evaluationsQuery = evaluationsQuery.Where(e => e.ProjectId == projectId.Value);
        }

        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => (e.CallDate ?? e.ControlDate) >= start);
        }
        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => (e.CallDate ?? e.ControlDate) <= end);
        }

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        var scores = await evaluationsQuery
            .Select(e => new
            {
                ProjectTypeId = e.Project!.ProjectTypeId,
                Score = e.ScorePercentage ?? 0
            })
            .ToListAsync();

        var groupedByType = scores.GroupBy(s => s.ProjectTypeId).ToList();
        if (groupedByType.Count == 0)
            return new List<object>();

        var thresholds = await _customerScoreThresholdService.GetAllAsync(customerId);

        var result = groupedByType.Select(g =>
        {
            var threshold = thresholds.FirstOrDefault(t => t.ProjectTypeId == g.Key);
            var successThreshold = threshold?.SuccessThreshold ?? 80m;
            var warningThreshold = threshold?.WarningThreshold ?? 60m;
            var projectType = ProjectTypes.GetById(g.Key);

            var typeScores = g.Select(s => s.Score).ToList();

            return new
            {
                projectTypeId = g.Key,
                projectTypeName = threshold?.ProjectTypeName ?? projectType?.Description ?? "Bilinmeyen",
                projectTypeIcon = threshold?.ProjectTypeIcon ?? projectType?.Icon ?? "bi-folder",
                projectTypeColor = threshold?.ProjectTypeColor ?? projectType?.CssClass ?? "bg-secondary",
                successThreshold,
                warningThreshold,
                success = typeScores.Count(s => s >= successThreshold),
                warning = typeScores.Count(s => s >= warningThreshold && s < successThreshold),
                danger = typeScores.Count(s => s < warningThreshold),
                total = typeScores.Count
            };
        })
        .OrderBy(r => r.projectTypeId)
        .ToList();

        return result;
    }

    public async Task<object?> GetScoreDistributionEvaluationsAsync(int customerId, List<int>? allowedPersonnelIds, string category, int projectTypeId, DateTime? startDate, DateTime? endDate, int page, int pageSize)
    {
        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Where(e => e.Project != null &&
                        e.Project.CustomerId == customerId &&
                        e.Project.ProjectTypeId == projectTypeId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed);

        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => (e.CallDate ?? e.ControlDate) >= start);
        }
        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => (e.CallDate ?? e.ControlDate) <= end);
        }

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        var thresholds = await _customerScoreThresholdService.GetAllAsync(customerId);
        var threshold = thresholds.FirstOrDefault(t => t.ProjectTypeId == projectTypeId);
        var st = threshold?.SuccessThreshold ?? 80m;
        var wt = threshold?.WarningThreshold ?? 60m;

        switch (category?.ToLower())
        {
            case "success":
                evaluationsQuery = evaluationsQuery.Where(e => (e.ScorePercentage ?? 0) >= st);
                break;
            case "warning":
                evaluationsQuery = evaluationsQuery.Where(e => (e.ScorePercentage ?? 0) >= wt && (e.ScorePercentage ?? 0) < st);
                break;
            case "danger":
                evaluationsQuery = evaluationsQuery.Where(e => e.ScorePercentage == null || (e.ScorePercentage ?? 0) < wt);
                break;
            default:
                return null; // Controller will return BadRequest
        }

        var total = await evaluationsQuery.CountAsync();

        var evaluations = await evaluationsQuery
            .OrderByDescending(e => e.CallDate ?? e.ControlDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                evaluationDate = e.CallDate ?? e.ControlDate,
                projectName = e.Project!.Name,
                personnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : "-",
                organizationName = e.EvaluatedOrganization != null
                    ? e.EvaluatedOrganization.Name
                    : (e.EvaluatedCustomerPersonnel != null
                        ? e.EvaluatedCustomerPersonnel.OrganizationAssignments
                            .Select(oa => oa.CustomerOrganization.Name)
                            .FirstOrDefault() ?? "-"
                        : "-"),
                score = e.ScorePercentage ?? 0,
                projectTypeId = e.Project!.ProjectTypeId,
                e.YellowCardCount,
                e.RedCardCount
            })
            .ToListAsync();

        return new { items = evaluations, total, page, pageSize };
    }

    public async Task<(byte[] FileContent, string FileName)?> ExportScoreDistributionEvaluationsAsync(int customerId, List<int>? allowedPersonnelIds, string category, int projectTypeId, DateTime? startDate, DateTime? endDate)
    {
        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Where(e => e.Project != null &&
                        e.Project.CustomerId == customerId &&
                        e.Project.ProjectTypeId == projectTypeId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed);

        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => (e.CallDate ?? e.ControlDate) >= start);
        }
        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => (e.CallDate ?? e.ControlDate) <= end);
        }

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        var thresholds = await _customerScoreThresholdService.GetAllAsync(customerId);
        var threshold = thresholds.FirstOrDefault(t => t.ProjectTypeId == projectTypeId);
        var st = threshold?.SuccessThreshold ?? 80m;
        var wt = threshold?.WarningThreshold ?? 60m;

        var categoryLabel = "";
        switch (category?.ToLower())
        {
            case "success":
                evaluationsQuery = evaluationsQuery.Where(e => (e.ScorePercentage ?? 0) >= st);
                categoryLabel = $"Başarılı ({st}+)";
                break;
            case "warning":
                evaluationsQuery = evaluationsQuery.Where(e => (e.ScorePercentage ?? 0) >= wt && (e.ScorePercentage ?? 0) < st);
                categoryLabel = $"Uyarı ({wt}-{st})";
                break;
            case "danger":
                evaluationsQuery = evaluationsQuery.Where(e => e.ScorePercentage == null || (e.ScorePercentage ?? 0) < wt);
                categoryLabel = $"Başarısız (<{wt})";
                break;
            default:
                return null;
        }

        var evaluations = await evaluationsQuery
            .OrderByDescending(e => e.CallDate ?? e.ControlDate)
            .Select(e => new
            {
                evaluationDate = e.CallDate ?? e.ControlDate,
                projectName = e.Project!.Name,
                personnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : "-",
                organizationName = e.EvaluatedOrganization != null
                    ? e.EvaluatedOrganization.Name
                    : (e.EvaluatedCustomerPersonnel != null
                        ? e.EvaluatedCustomerPersonnel.OrganizationAssignments
                            .Select(oa => oa.CustomerOrganization.Name)
                            .FirstOrDefault() ?? "-"
                        : "-"),
                score = e.ScorePercentage ?? 0,
                yellowCardCount = e.YellowCardCount,
                redCardCount = e.RedCardCount
            })
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Değerlendirmeler");

        worksheet.Cell(1, 1).Value = "Tarih";
        worksheet.Cell(1, 2).Value = "Proje";
        worksheet.Cell(1, 3).Value = "Personel";
        worksheet.Cell(1, 4).Value = "Organizasyon";
        worksheet.Cell(1, 5).Value = "Puan";
        worksheet.Cell(1, 6).Value = "Sarı Kart";
        worksheet.Cell(1, 7).Value = "Kırmızı Kart";

        var headerRange = worksheet.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        for (int i = 0; i < evaluations.Count; i++)
        {
            var row = i + 2;
            var eval = evaluations[i];
            worksheet.Cell(row, 1).Value = eval.evaluationDate?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 2).Value = eval.projectName;
            worksheet.Cell(row, 3).Value = eval.personnelName;
            worksheet.Cell(row, 4).Value = eval.organizationName;
            worksheet.Cell(row, 5).Value = eval.score;
            worksheet.Cell(row, 6).Value = eval.yellowCardCount;
            worksheet.Cell(row, 7).Value = eval.redCardCount;
        }

        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"PuanDagilimi_{categoryLabel.Replace(" ", "_")}_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return (stream.ToArray(), fileName);
    }

    // ==================== EVALUATIONS ====================

    public async Task<object> GetRecentEvaluationsAsync(int customerId, List<int>? allowedPersonnelIds, int count)
    {
        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Where(e => e.Project != null &&
                        e.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed);

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        var evaluations = await evaluationsQuery
            .OrderByDescending(e => e.CallDate ?? e.ControlDate)
            .Take(count)
            .Select(e => new
            {
                e.Id,
                evaluationDate = e.CallDate ?? e.ControlDate,
                projectName = e.Project!.Name,
                checklistName = e.Project.Checklist != null ? e.Project.Checklist.Name : "N/A",
                personnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : e.EvaluatedUnknownPersonnel ?? "-",
                score = e.ScorePercentage ?? 0,
                projectTypeId = e.Project!.ProjectTypeId,
                statusId = e.StatusId
            })
            .ToListAsync();

        var result = evaluations.Select(e => new
        {
            e.Id,
            e.evaluationDate,
            e.projectName,
            e.checklistName,
            e.personnelName,
            e.score,
            e.projectTypeId,
            status = EvaluationStatuses.GetById(e.statusId)?.SystemName ?? "",
            statusText = GetStatusText(e.statusId)
        });

        return result;
    }

    public async Task<object> GetEvaluationsAsync(int customerId, string? role, int? personnelId, List<int>? allowedPersonnelIds, int page, int pageSize, int? projectId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Where(e => e.Project != null &&
                        e.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed);

        // Rol bazlı filtreleme
        if (role == "CustomerOperator" && personnelId.HasValue)
        {
            query = query.Where(e => e.EvaluatedCustomerPersonnelId == personnelId.Value);
        }
        else if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            if (allowedPersonnelIds != null)
            {
                query = query.Where(e =>
                    e.EvaluatorCustomerPersonnelId == personnelId.Value ||
                    (e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value)));
            }
        }

        // Modal filtreleri (CallDate bazlı - ExternalEvaluations ile aynı pattern)
        if (projectId.HasValue)
            query = query.Where(e => e.ProjectId == projectId.Value);
        if (startDate.HasValue)
            query = query.Where(e => e.CallDate.HasValue && e.CallDate.Value >= startDate.Value.Date);
        if (endDate.HasValue)
            query = query.Where(e => e.CallDate.HasValue && e.CallDate.Value < endDate.Value.Date.AddDays(1));

        var totalCount = await query.CountAsync();

        var evaluations = await query
            .OrderByDescending(e => e.CallDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                evaluationDate = e.CallDate,
                projectName = e.Project!.Name,
                checklistName = e.Project.Checklist != null ? e.Project.Checklist.Name : "N/A",
                scoringMethodId = e.Project.Checklist != null ? e.Project.Checklist.ScoringMethodId : 1,
                personnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : e.EvaluatedUnknownPersonnel ?? "-",
                score = e.ScorePercentage ?? 0,
                projectTypeId = e.Project!.ProjectTypeId,
                statusId = e.StatusId
            })
            .ToListAsync();

        var mappedEvaluations = evaluations.Select(e => new
        {
            e.Id,
            e.evaluationDate,
            e.projectName,
            e.checklistName,
            e.personnelName,
            scoringMethod = ScoringMethods.GetById(e.scoringMethodId)?.SystemName ?? "Maximum",
            e.score,
            e.projectTypeId,
            status = EvaluationStatuses.GetById(e.statusId)?.SystemName ?? "",
            statusText = GetStatusText(e.statusId)
        });

        return new
        {
            items = mappedEvaluations,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<(byte[] FileContent, string FileName)> ExportAllEvaluationsToExcelAsync(int customerId, string? role, int? personnelId, List<int>? allowedPersonnelIds, int? projectId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Where(e => e.Project != null &&
                        e.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed);

        if (role == "CustomerOperator" && personnelId.HasValue)
            query = query.Where(e => e.EvaluatedCustomerPersonnelId == personnelId.Value);
        else if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            if (allowedPersonnelIds != null)
                query = query.Where(e =>
                    e.EvaluatorCustomerPersonnelId == personnelId.Value ||
                    (e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value)));
        }

        if (projectId.HasValue)
            query = query.Where(e => e.ProjectId == projectId.Value);
        if (startDate.HasValue)
            query = query.Where(e => e.CallDate.HasValue && e.CallDate.Value >= startDate.Value.Date);
        if (endDate.HasValue)
            query = query.Where(e => e.CallDate.HasValue && e.CallDate.Value < endDate.Value.Date.AddDays(1));

        var evaluations = await query
            .OrderByDescending(e => e.CallDate)
            .Select(e => new
            {
                evaluationDate = e.CallDate,
                projectName = e.Project!.Name,
                checklistName = e.Project.Checklist != null ? e.Project.Checklist.Name : "N/A",
                score = e.ScorePercentage ?? 0,
                statusId = e.StatusId
            })
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Değerlendirmeler");

        worksheet.Cell(1, 1).Value = "Tarih";
        worksheet.Cell(1, 2).Value = "Proje";
        worksheet.Cell(1, 3).Value = "Checklist";
        worksheet.Cell(1, 4).Value = "Puan";
        worksheet.Cell(1, 5).Value = "Durum";

        var headerRange = worksheet.Range(1, 1, 1, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        for (int i = 0; i < evaluations.Count; i++)
        {
            var row = i + 2;
            var eval = evaluations[i];
            worksheet.Cell(row, 1).Value = eval.evaluationDate?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 2).Value = eval.projectName;
            worksheet.Cell(row, 3).Value = eval.checklistName;
            worksheet.Cell(row, 4).Value = eval.score;
            worksheet.Cell(row, 5).Value = GetStatusText(eval.statusId);
        }

        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"Degerlendirmeler_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return (stream.ToArray(), fileName);
    }

    private static string GetStatusText(int statusId)
    {
        return statusId switch
        {
            EvaluationStatuses.Ids.Pending => "Beklemede",
            EvaluationStatuses.Ids.Draft => "Taslak",
            EvaluationStatuses.Ids.InProgress => "Devam Ediyor",
            EvaluationStatuses.Ids.Completed => "Tamamlandı",
            EvaluationStatuses.Ids.Cancelled => "İptal Edildi",
            _ => EvaluationStatuses.GetById(statusId)?.SystemName ?? "Bilinmeyen"
        };
    }

    // ==================== REPORTS ====================

    public async Task<object> GetProjectPerformanceAsync(int customerId, List<int>? allowedPersonnelIds, DateTime? startDate, DateTime? endDate, List<int>? projectIds, List<int>? organizationIds, bool? isInternal)
    {
        var start = startDate ?? TurkeyTime.Now.AddMonths(-3);
        var end = endDate ?? TurkeyTime.Now;

        // UTC'ye çevir
        if (start.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (end.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var projectsQuery = _context.Projects
            .Where(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted);

        // Project filter
        if (projectIds?.Any() == true)
            projectsQuery = projectsQuery.Where(p => projectIds.Contains(p.Id));

        var projects = await projectsQuery.ToListAsync();

        var excludedChecklistTypes = new[] { ChecklistTypes.Ids.Survey, ChecklistTypes.Ids.Enneagram };

        var evalQuery = _context.Evaluations
            .Include(e => e.Project).ThenInclude(p => p.Checklist)
            .Where(e => e.Project != null && e.Project.CustomerId == customerId
                && (e.CallDate ?? e.ControlDate) >= start
                && (e.CallDate ?? e.ControlDate) <= end
                && e.StatusId == EvaluationStatuses.Ids.Completed
                && !excludedChecklistTypes.Contains(e.Project!.Checklist!.ChecklistTypeId));

        // İç/Dış dinleme filtresi
        if (isInternal == true)
            evalQuery = evalQuery.Where(e => e.EvaluatorCustomerPersonnelId != null);
        else if (isInternal == false)
            evalQuery = evalQuery.Where(e => e.EvaluatorId != null);

        // Supervisor personel filtresi
        if (allowedPersonnelIds != null)
            evalQuery = evalQuery.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));

        // Project filter
        if (projectIds?.Any() == true)
            evalQuery = evalQuery.Where(e => projectIds.Contains(e.ProjectId));

        // Organization filter
        if (organizationIds?.Any() == true)
            evalQuery = evalQuery.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));

        var evaluations = await evalQuery.ToListAsync();

        // Supervisor için sadece değerlendirmesi olan projeleri göster
        var projectIdsWithEvaluations = evaluations.Select(e => e.ProjectId).Distinct().ToHashSet();
        var filteredProjects = allowedPersonnelIds != null
            ? projects.Where(p => projectIdsWithEvaluations.Contains(p.Id)).ToList()
            : projects;

        var projectPerformance = filteredProjects.Select(p =>
        {
            var projectEvals = evaluations.Where(e => e.ProjectId == p.Id).ToList();
            return new
            {
                projectId = p.Id,
                projectCode = p.Code ?? "",
                projectName = p.Name,
                evaluationCount = projectEvals.Count,
                averageScore = projectEvals.Where(e => e.ScorePercentage.HasValue).Any() ? Math.Round(projectEvals.Where(e => e.ScorePercentage.HasValue).Average(e => (double)e.ScorePercentage!.Value), 2) : 0,
                minScore = projectEvals.Where(e => e.ScorePercentage.HasValue).Any() ? projectEvals.Where(e => e.ScorePercentage.HasValue).Min(e => e.ScorePercentage!.Value) : 0,
                maxScore = projectEvals.Where(e => e.ScorePercentage.HasValue).Any() ? projectEvals.Where(e => e.ScorePercentage.HasValue).Max(e => e.ScorePercentage!.Value) : 0
            };
        })
        .OrderByDescending(p => p.averageScore)
        .ToList();

        return projectPerformance;
    }

    public async Task<object> GetReportSummaryAsync(int customerId, List<int>? allowedPersonnelIds, DateTime? startDate, DateTime? endDate, List<int>? projectIds, List<int>? organizationIds, bool? isInternal)
    {
        var start = startDate ?? TurkeyTime.Now.AddMonths(-3);
        var end = endDate ?? TurkeyTime.Now;

        if (start.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (end.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var excludedChecklistTypes = new[] { ChecklistTypes.Ids.Survey, ChecklistTypes.Ids.Enneagram };

        var query = _context.Evaluations
            .Include(e => e.Project).ThenInclude(p => p.Checklist)
            .Where(e => e.Project != null && e.Project.CustomerId == customerId
                && (e.CallDate ?? e.ControlDate) >= start
                && (e.CallDate ?? e.ControlDate) <= end
                && e.StatusId == EvaluationStatuses.Ids.Completed
                && !excludedChecklistTypes.Contains(e.Project!.Checklist!.ChecklistTypeId));

        // İç/Dış dinleme filtresi
        if (isInternal == true)
            query = query.Where(e => e.EvaluatorCustomerPersonnelId != null);
        else if (isInternal == false)
            query = query.Where(e => e.EvaluatorId != null);

        // Supervisor personel filtresi
        if (allowedPersonnelIds != null)
            query = query.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));

        // Project filter
        if (projectIds?.Any() == true)
            query = query.Where(e => projectIds.Contains(e.ProjectId));

        // Organization filter
        if (organizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));

        var evaluations = await query.ToListAsync();

        // Supervisor için sadece değerlendirmesi olan proje sayısı
        int projectCount;
        if (allowedPersonnelIds != null)
        {
            projectCount = evaluations.Select(e => e.ProjectId).Distinct().Count();
        }
        else
        {
            projectCount = await _context.Projects
                .CountAsync(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted);
        }

        // Get score thresholds for this customer (fallback: 80/60)
        var thresholds = await _customerScoreThresholdService.GetAllAsync(customerId);
        // Use the first available threshold (general/default) - pick highest success/warning across project types
        var successThreshold = thresholds.Any() ? thresholds.Max(t => t.SuccessThreshold) : 80;
        var warningThreshold = thresholds.Any() ? thresholds.Max(t => t.WarningThreshold) : 60;

        var summary = new
        {
            periodStart = start,
            periodEnd = end,
            totalEvaluations = evaluations.Count,
            projectCount,
            averageScore = evaluations.Where(e => e.ScorePercentage.HasValue).Any() ? Math.Round(evaluations.Where(e => e.ScorePercentage.HasValue).Average(e => (double)e.ScorePercentage!.Value), 2) : 0,
            minScore = evaluations.Where(e => e.ScorePercentage.HasValue).Any() ? evaluations.Where(e => e.ScorePercentage.HasValue).Min(e => e.ScorePercentage!.Value) : 0,
            maxScore = evaluations.Where(e => e.ScorePercentage.HasValue).Any() ? evaluations.Where(e => e.ScorePercentage.HasValue).Max(e => e.ScorePercentage!.Value) : 0,
            successCount = evaluations.Count(e => e.ScorePercentage >= successThreshold),
            warningCount = evaluations.Count(e => e.ScorePercentage >= warningThreshold && e.ScorePercentage < successThreshold),
            dangerCount = evaluations.Count(e => e.ScorePercentage < warningThreshold),
            successThreshold,
            warningThreshold
        };

        return summary;
    }

    public async Task<object> GetEvaluationsByScoreRangeAsync(int customerId, List<int>? allowedPersonnelIds, DateTime? startDate, DateTime? endDate, List<int>? projectIds, List<int>? organizationIds, decimal minScore, decimal maxScore, bool? isInternal)
    {
        var start = startDate ?? TurkeyTime.Now.AddMonths(-3);
        var end = endDate ?? TurkeyTime.Now;

        if (start.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (end.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var excludedChecklistTypes = new[] { ChecklistTypes.Ids.Survey, ChecklistTypes.Ids.Enneagram };

        var query = _context.Evaluations
            .Include(e => e.Project).ThenInclude(p => p.Checklist)
            .Include(e => e.EvaluatedPersonnel)
            .Where(e => e.Project != null && e.Project.CustomerId == customerId
                && (e.CallDate ?? e.ControlDate) >= start
                && (e.CallDate ?? e.ControlDate) <= end
                && e.StatusId == EvaluationStatuses.Ids.Completed
                && e.ScorePercentage.HasValue
                && e.ScorePercentage >= minScore
                && e.ScorePercentage < maxScore
                && !excludedChecklistTypes.Contains(e.Project!.Checklist!.ChecklistTypeId));

        // İç/Dış dinleme filtresi
        if (isInternal == true)
            query = query.Where(e => e.EvaluatorCustomerPersonnelId != null);
        else if (isInternal == false)
            query = query.Where(e => e.EvaluatorId != null);

        // Supervisor personel filtresi
        if (allowedPersonnelIds != null)
            query = query.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));

        // Project filter
        if (projectIds?.Any() == true)
            query = query.Where(e => projectIds.Contains(e.ProjectId));

        // Organization filter
        if (organizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));

        var evaluations = await query
            .OrderByDescending(e => e.CallDate ?? e.ControlDate)
            .Take(100)
            .Select(e => new
            {
                evaluationId = e.Id,
                evaluationDate = e.CallDate ?? e.ControlDate,
                projectName = e.Project != null ? e.Project.Name : "-",
                personnelName = e.EvaluatedPersonnel != null
                    ? e.EvaluatedPersonnel.FirstName + " " + e.EvaluatedPersonnel.LastName
                    : "-",
                scorePercentage = e.ScorePercentage,
                yellowCards = e.YellowCardCount,
                redCards = e.RedCardCount
            })
            .ToListAsync();

        return evaluations;
    }

    public async Task<object> GetReportMonthlyTrendAsync(int customerId, List<int>? allowedPersonnelIds, DateTime? startDate, DateTime? endDate, List<int>? projectIds, List<int>? organizationIds, bool? isInternal)
    {
        var start = startDate ?? TurkeyTime.Now.AddMonths(-6);
        var end = endDate ?? TurkeyTime.Now;

        if (start.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (end.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var excludedChecklistTypes = new[] { ChecklistTypes.Ids.Survey, ChecklistTypes.Ids.Enneagram };

        var query = _context.Evaluations
            .Include(e => e.Project).ThenInclude(p => p.Checklist)
            .Where(e => e.Project != null && e.Project.CustomerId == customerId
                && (e.CallDate ?? e.ControlDate) >= start
                && (e.CallDate ?? e.ControlDate) <= end
                && e.StatusId == EvaluationStatuses.Ids.Completed
                && !excludedChecklistTypes.Contains(e.Project!.Checklist!.ChecklistTypeId));

        // İç/Dış dinleme filtresi
        if (isInternal == true)
            query = query.Where(e => e.EvaluatorCustomerPersonnelId != null);
        else if (isInternal == false)
            query = query.Where(e => e.EvaluatorId != null);

        // Supervisor personel filtresi
        if (allowedPersonnelIds != null)
            query = query.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));

        // Project filter
        if (projectIds?.Any() == true)
            query = query.Where(e => projectIds.Contains(e.ProjectId));

        // Organization filter
        if (organizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));

        var evaluations = await query.ToListAsync();

        // Aylara göre grupla
        var monthlyData = evaluations
            .Where(e => (e.CallDate ?? e.ControlDate).HasValue)
            .GroupBy(e => new { (e.CallDate ?? e.ControlDate)!.Value.Year, (e.CallDate ?? e.ControlDate)!.Value.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                year = g.Key.Year,
                month = g.Key.Month,
                monthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy", new System.Globalization.CultureInfo("tr-TR")),
                count = g.Count(),
                averageScore = g.Where(e => e.ScorePercentage.HasValue).Any() ? Math.Round(g.Where(e => e.ScorePercentage.HasValue).Average(e => (double)e.ScorePercentage!.Value), 2) : 0
            })
            .ToList();

        return monthlyData;
    }

    // ==================== INTERNAL EVALUATIONS ====================

    /// <summary>
    /// Supervisor rolü için allowedPersonnelIds hesaplar (controller'daki GetAllowedPersonnelIdsAsync mantığının aynısı)
    /// </summary>
    private async Task<List<int>?> ComputeAllowedPersonnelIdsAsync(string? role, int? personnelId)
    {
        // Admin ve CustomerManager tüm personeli görebilir
        if (role == "Admin" || role == "CustomerManager")
            return null;

        // CustomerSupervisor - Organizasyon bazında hibrit kontrol
        if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            // 1. Süpervizörün atandığı organizasyonları bul
            var myOrgIds = await _context.CustomerPersonnelOrganizations
                .Where(cpo => cpo.CustomerPersonnelId == personnelId.Value && !cpo.IsDeleted)
                .Select(cpo => cpo.CustomerOrganizationId)
                .Distinct()
                .ToListAsync();

            // Hiçbir organizasyona atanmamış → TÜM veriyi görebilir (null = filtre yok)
            if (!myOrgIds.Any())
                return null;

            // 2. Bu organizasyonlarda süpervizör olduğu personeller
            var supervisedPersonnel = await _context.CustomerPersonnelOrganizations
                .Where(cpo => myOrgIds.Contains(cpo.CustomerOrganizationId) &&
                             cpo.SupervisorId == personnelId.Value &&
                             !cpo.IsDeleted)
                .Select(cpo => new { cpo.CustomerOrganizationId, cpo.CustomerPersonnelId })
                .ToListAsync();

            // 3. Hangi organizasyonlarda altında personel var?
            var orgsWithTeam = supervisedPersonnel
                .Select(x => x.CustomerOrganizationId)
                .Distinct()
                .ToHashSet();

            // 4. Altında personel olmayan organizasyonlar
            var orgsWithoutTeam = myOrgIds.Except(orgsWithTeam).ToList();

            var result = new HashSet<int>();

            // Altında personel olan org'lardan sadece o personeller
            foreach (var p in supervisedPersonnel)
                result.Add(p.CustomerPersonnelId);

            // Altında personel olmayan org'lardan TÜM personeller
            if (orgsWithoutTeam.Any())
            {
                var allPersonnelInEmptyOrgs = await _context.CustomerPersonnelOrganizations
                    .Where(cpo => orgsWithoutTeam.Contains(cpo.CustomerOrganizationId) && !cpo.IsDeleted)
                    .Select(cpo => cpo.CustomerPersonnelId)
                    .Distinct()
                    .ToListAsync();

                foreach (var id in allPersonnelInEmptyOrgs)
                    result.Add(id);
            }

            // Kendisini de ekle
            result.Add(personnelId.Value);

            return result.ToList();
        }

        // CustomerOperator sadece kendini görebilir
        if (personnelId.HasValue)
            return new List<int> { personnelId.Value };

        return new List<int>(); // Hiçbir şey göremez
    }

    public async Task<object> GetInternalEvaluationsAsync(int customerId, string? role, int? personnelId,
        int? page, int? pageSize, string? search, DateTime? startDate, DateTime? endDate,
        List<int>? projectIds, List<string>? evaluatorNames, List<string>? personnelNames,
        List<int>? organizationIds, List<string>? callIds)
    {
        // Anket ve Enneagram checklist tipleri hariç (kendi raporları var)
        var excludedChecklistTypes = new[] { ChecklistTypes.Ids.Survey, ChecklistTypes.Ids.Enneagram };

        var query = _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Include(e => e.EvaluatorCustomerPersonnel)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Include(e => e.CustomerDealer)
            .Where(e => e.Project != null &&
                       e.Project.CustomerId == customerId &&
                       e.EvaluatorCustomerPersonnelId != null &&
                       !excludedChecklistTypes.Contains(e.Project!.Checklist!.ChecklistTypeId)); // Anket/Enneagram hariç

        // Rol bazlı filtreleme (İç Dinlemeler)
        // Operator: Sadece kendisinin değerlendirildiği kayıtlar
        // Supervisor: Kendi yaptığı değerlendirmeler + takımındaki personelin değerlendirildiği kayıtlar
        if (role == "CustomerOperator" && personnelId.HasValue)
        {
            query = query.Where(e => e.EvaluatedCustomerPersonnelId == personnelId.Value);
        }
        else if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            var allowedPersonnelIds = await ComputeAllowedPersonnelIdsAsync(role, personnelId);
            if (allowedPersonnelIds != null)
            {
                query = query.Where(e =>
                    e.EvaluatorCustomerPersonnelId == personnelId.Value ||
                    (e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value)));
            }
        }
        // Manager ve Admin: Tüm kayıtları görür

        // Date filters (çağrı tarihi - CallDate)
        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e =>
             (e.CallDate.HasValue && e.CallDate.Value >= start) ||
             (e.ControlDate.HasValue && e.ControlDate.Value >= start)
            );
        }
        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            query = query.Where(e =>
             (e.CallDate.HasValue && e.CallDate.Value <= end) ||
             (e.ControlDate.HasValue && e.ControlDate.Value <= end)
            );
        }

        // Project filter
        if (projectIds?.Any() == true)
        {
            query = query.Where(e => projectIds.Contains(e.ProjectId));
        }

        // Evaluator name filter
        if (evaluatorNames?.Any() == true)
        {
            var lowerNames = evaluatorNames.Select(n => n.ToLower()).ToList();
            query = query.Where(e => e.EvaluatorCustomerPersonnel != null &&
                lowerNames.Any(n => (e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName).ToLower().Contains(n)));
        }

        // Personnel name filter
        if (personnelNames?.Any() == true)
        {
            var lowerNames = personnelNames.Select(n => n.ToLower()).ToList();
            query = query.Where(e =>
                lowerNames.Any(n =>
                    (e.EvaluatedCustomerPersonnel != null && (e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName).ToLower().Contains(n)) ||
                    (e.EvaluatedUnknownPersonnel != null && e.EvaluatedUnknownPersonnel.ToLower().Contains(n))));
        }

        // Organization filter
        if (organizationIds?.Any() == true)
        {
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));
        }

        // CallId filter
        if (callIds?.Any() == true)
        {
            var lowerCallIds = callIds.Select(c => c.ToLower()).ToList();
            query = query.Where(e => e.CallId != null && lowerCallIds.Any(c => e.CallId.ToLower().Contains(c)));
        }

        // General search filter (legacy support)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(e =>
                (e.Project.Name != null && e.Project.Name.ToLower().Contains(searchLower)) ||
                (e.EvaluatorCustomerPersonnel != null && (e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName).ToLower().Contains(searchLower)) ||
                (e.EvaluatedCustomerPersonnel != null && (e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName).ToLower().Contains(searchLower)) ||
                (e.EvaluatedUnknownPersonnel != null && e.EvaluatedUnknownPersonnel.ToLower().Contains(searchLower)) ||
                (e.EvaluatedOrganization != null && e.EvaluatedOrganization.Name.ToLower().Contains(searchLower)) ||
                (e.CallId != null && e.CallId.ToLower().Contains(searchLower)));
        }

        var total = await query.CountAsync();
        var averageScore = await query.Where(e => e.ScorePercentage.HasValue).AverageAsync(e => (double?)e.ScorePercentage) ?? 0;

        var evaluations = await query
            .OrderByDescending(e => e.CallDate)
            .Skip(((page ?? 1) - 1) * (pageSize ?? 20))
            .Take(pageSize ?? 20)
            .Select(e => new
            {
                e.Id,
                evaluationDate = e.CallDate,
                projectName = e.Project.Name,
                projectCode = e.Project.Code,
                projectTypeId = e.Project.ProjectTypeId,
                evaluatorName = e.EvaluatorCustomerPersonnel != null
                    ? e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName
                    : null,
                e.EvaluatorCustomerPersonnelId,
                evaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName : e.EvaluatedUnknownPersonnel,
                dealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : (string?)null,
                organizationName = e.EvaluatedOrganization != null
                    ? e.EvaluatedOrganization.Name
                    : (e.EvaluatedCustomerPersonnel != null
                        ? e.EvaluatedCustomerPersonnel.OrganizationAssignments
                            .Select(oa => oa.CustomerOrganization.Name)
                            .FirstOrDefault()
                        : null),
                e.TotalScore,
                e.ScorePercentage,
                e.YellowCardCount,
                e.RedCardCount,
                e.CallId,
                e.CallDate,
                e.CallTime,
                e.Duration,
                e.StatusId
            })
            .ToListAsync();

        // Soft-deleted personel isimlerini IgnoreQueryFilters ile al (global filter bypass)
        var nullEvaluatorIds = evaluations
            .Where(e => e.evaluatorName == null && e.EvaluatorCustomerPersonnelId.HasValue)
            .Select(e => e.EvaluatorCustomerPersonnelId!.Value)
            .Distinct()
            .ToList();

        var deletedPersonnelNames = new Dictionary<int, string>();
        if (nullEvaluatorIds.Any())
        {
            deletedPersonnelNames = await _context.CustomerPersonnel
                .IgnoreQueryFilters()
                .Where(cp => nullEvaluatorIds.Contains(cp.Id))
                .ToDictionaryAsync(cp => cp.Id, cp => cp.FirstName + " " + cp.LastName);
        }

        var items = evaluations.Select(e => new
        {
            e.Id,
            e.evaluationDate,
            e.projectName,
            e.projectCode,
            e.projectTypeId,
            evaluatorName = e.evaluatorName
                ?? (e.EvaluatorCustomerPersonnelId.HasValue && deletedPersonnelNames.ContainsKey(e.EvaluatorCustomerPersonnelId.Value)
                    ? deletedPersonnelNames[e.EvaluatorCustomerPersonnelId.Value]
                    : null),
            e.evaluatedPersonnelName,
            e.dealerName,
            e.organizationName,
            e.TotalScore,
            e.ScorePercentage,
            e.YellowCardCount,
            e.RedCardCount,
            e.CallId,
            e.CallDate,
            e.CallTime,
            e.Duration,
            status = EvaluationStatuses.GetById(e.StatusId)?.SystemName ?? "Unknown"
        });

        return new { items, total, page = page ?? 1, pageSize = pageSize ?? 20, averageScore = Math.Round(averageScore, 2) };
    }

    // ==================== EXTERNAL EVALUATIONS ====================

    public async Task<object> GetExternalEvaluationsAsync(int customerId, string? role, int? personnelId,
        int? page, int? pageSize, string? search, DateTime? startDate, DateTime? endDate,
        List<int>? projectIds, List<string>? personnelNames, List<int>? organizationIds,
        List<string>? callIds, decimal? minScore, decimal? maxScore)
    {
        // Anket ve Enneagram checklist tipleri hariç (kendi raporları var)
        var excludedChecklistTypes = new[] { ChecklistTypes.Ids.Survey, ChecklistTypes.Ids.Enneagram };

        var query = _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Include(e => e.Evaluator)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Include(e => e.CustomerDealer)
            .Where(e => e.Project != null &&
                       e.Project.CustomerId == customerId &&
                       e.EvaluatorId != null &&
                       e.StatusId == EvaluationStatuses.Ids.Completed &&
                       !excludedChecklistTypes.Contains(e.Project!.Checklist!.ChecklistTypeId)); // Anket/Enneagram hariç

        // Rol bazlı filtreleme (Dış Dinlemeler)
        // Operator: Sadece kendisinin değerlendirildiği kayıtlar
        // Supervisor: Takımındaki personelin değerlendirildiği kayıtlar
        if (role == "CustomerOperator" && personnelId.HasValue)
        {
            query = query.Where(e => e.EvaluatedCustomerPersonnelId == personnelId.Value);
        }
        else if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            var allowedPersonnelIds = await ComputeAllowedPersonnelIdsAsync(role, personnelId);
            if (allowedPersonnelIds != null)
            {
                query = query.Where(e =>
                    e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
            }
        }
        // Manager ve Admin: Tüm kayıtları görür

        // Date filters (çağrı tarihi - CallDate)
        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.CallDate.HasValue && e.CallDate.Value >= start);
        }
        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            query = query.Where(e => e.CallDate.HasValue && e.CallDate.Value <= end);
        }

        // Project filter
        if (projectIds?.Any() == true)
        {
            query = query.Where(e => projectIds.Contains(e.ProjectId));
        }

        // Personnel name filter
        if (personnelNames?.Any() == true)
        {
            var lowerNames = personnelNames.Select(n => n.ToLower()).ToList();
            query = query.Where(e =>
                lowerNames.Any(n =>
                    (e.EvaluatedCustomerPersonnel != null && (e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName).ToLower().Contains(n)) ||
                    (e.EvaluatedUnknownPersonnel != null && e.EvaluatedUnknownPersonnel.ToLower().Contains(n))));
        }

        // Organization filter
        if (organizationIds?.Any() == true)
        {
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));
        }

        // CallId filter
        if (callIds?.Any() == true)
        {
            var lowerCallIds = callIds.Select(c => c.ToLower()).ToList();
            query = query.Where(e => e.CallId != null && lowerCallIds.Any(c => e.CallId.ToLower().Contains(c)));
        }

        // Score range filter
        if (minScore.HasValue)
        {
            query = query.Where(e => e.ScorePercentage.HasValue && e.ScorePercentage.Value >= minScore.Value);
        }
        if (maxScore.HasValue)
        {
            query = query.Where(e => e.ScorePercentage.HasValue && e.ScorePercentage.Value <= maxScore.Value);
        }

        // General search filter (legacy support)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(e =>
                (e.Project.Name != null && e.Project.Name.ToLower().Contains(searchLower)) ||
                (e.EvaluatedCustomerPersonnel != null && (e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName).ToLower().Contains(searchLower)) ||
                (e.EvaluatedUnknownPersonnel != null && e.EvaluatedUnknownPersonnel.ToLower().Contains(searchLower)) ||
                (e.EvaluatedOrganization != null && e.EvaluatedOrganization.Name.ToLower().Contains(searchLower)) ||
                (e.CallId != null && e.CallId.ToLower().Contains(searchLower)));
        }

        var total = await query.CountAsync();
        var averageScore = await query.Where(e => e.ScorePercentage.HasValue).AverageAsync(e => (double?)e.ScorePercentage) ?? 0;

        var evaluations = await query
            .OrderByDescending(e => e.CallDate ?? e.ControlDate)
            .Skip(((page ?? 1) - 1) * (pageSize ?? 20))
            .Take(pageSize ?? 20)
            .Select(e => new
            {
                e.Id,
                evaluationDate = e.CallDate ?? e.ControlDate,
                projectName = e.Project.Name,
                projectTypeId = e.Project.ProjectTypeId,
                evaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName : e.EvaluatedUnknownPersonnel,
                dealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : (string?)null,
                organizationName = e.EvaluatedOrganization != null
                    ? e.EvaluatedOrganization.Name
                    : (e.EvaluatedCustomerPersonnel != null
                        ? e.EvaluatedCustomerPersonnel.OrganizationAssignments
                            .Select(oa => oa.CustomerOrganization.Name)
                            .FirstOrDefault()
                        : null),
                e.TotalScore,
                e.ScorePercentage,
                e.YellowCardCount,
                e.RedCardCount,
                e.CallId,
                e.CallDate,
                e.CallTime,
                e.Duration,
                e.ControlDate,
                e.ControlTime
            })
            .ToListAsync();

        return new { items = evaluations, total, page = page ?? 1, pageSize = pageSize ?? 20, averageScore = Math.Round(averageScore, 2) };
    }

    // ==================== EVALUATION DETAIL ====================

    public async Task<(bool IsAuthorized, int? EvaluatedCustomerPersonnelId)?> CheckEvaluationAccessAsync(int evaluationId, int customerId, List<int>? allowedPersonnelIds, int? personnelId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == evaluationId);

        if (evaluation?.Project?.CustomerId != customerId)
            return null; // Evaluation not found or doesn't belong to this customer

        // Supervisor erişim kontrolü - evaluator da erişebilmeli
        var isEvaluator = personnelId.HasValue && evaluation.EvaluatorCustomerPersonnelId == personnelId.Value;
        if (allowedPersonnelIds != null && evaluation.EvaluatedCustomerPersonnelId.HasValue &&
            !allowedPersonnelIds.Contains(evaluation.EvaluatedCustomerPersonnelId.Value) &&
            !isEvaluator)
            return (false, evaluation.EvaluatedCustomerPersonnelId);

        return (true, evaluation.EvaluatedCustomerPersonnelId);
    }

    public async Task<object> GetEvaluationAttachmentsAsync(int evaluationId)
    {
        var attachments = await _context.EvaluationAttachments
            .Where(a => a.EvaluationId == evaluationId && !a.IsDeleted)
            .Select(a => new
            {
                id = a.Id,
                fileName = a.FileName,
                fileSize = a.FileSize,
                contentType = a.ContentType,
                uploadedAt = a.CreatedAt
            })
            .ToListAsync();

        return attachments;
    }

    // ==================== SAVED FILTERS ====================

    public async Task<object> GetSavedFiltersAsync(int customerId, string page)
    {
        var savedFilters = await _context.SavedFilters
            .Where(f => f.CustomerId == customerId && f.PageName == page && !f.IsDeleted)
            .OrderByDescending(f => f.IsDefault)
            .ThenByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.PageName,
                f.FilterData,
                f.IsDefault,
                f.CreatedAt
            })
            .ToListAsync();

        // Parse FilterData outside of LINQ expression
        var result = savedFilters.Select(f => new
        {
            f.Id,
            f.Name,
            f.PageName,
            f.IsDefault,
            f.CreatedAt,
            filters = JsonSerializer.Deserialize<List<object>>(f.FilterData)
        });

        return result;
    }

    public async Task<int> SaveFilterAsync(int customerId, string name, string pageName, string filterDataJson)
    {
        var filter = new SavedFilter
        {
            CustomerId = customerId,
            UserId = null, // CustomerPortal'dan kaydedildi
            PageName = pageName,
            Name = name,
            FilterData = filterDataJson,
            IsDefault = false,
            CreatedAt = TurkeyTime.Now
        };

        _context.SavedFilters.Add(filter);
        await _context.SaveChangesAsync();

        return filter.Id;
    }

    public async Task<bool> DeleteSavedFilterAsync(int customerId, int id)
    {
        var filter = await _context.SavedFilters
            .FirstOrDefaultAsync(f => f.Id == id && f.CustomerId == customerId);

        if (filter == null)
            return false;

        filter.IsDeleted = true;
        await _context.SaveChangesAsync();

        return true;
    }

    // ==================== ORGANIZATIONS MONTHLY TREND ====================

    public async Task<object> GetOrganizationsMonthlyTrendAsync(int customerId, List<int>? allowedOrgIds, List<int>? organizationIds, DateTime? startDate, DateTime? endDate)
    {
        var now = TurkeyTime.Now;

        // Default: Bu haftanın başı (Pazartesi) - Bugün
        DateTime start;
        if (startDate.HasValue)
        {
            start = startDate.Value.Date;
        }
        else
        {
            // Pazartesi'yi hesapla
            var daysFromMonday = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            start = now.Date.AddDays(-daysFromMonday);
        }

        var end = endDate?.Date ?? now.Date;

        // UTC'ye çevir
        start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        end = DateTime.SpecifyKind(end.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        // Organizasyonları al
        var organizationsQuery = _context.CustomerOrganizations
            .Where(o => o.CustomerId == customerId && o.IsActive && !o.IsDeleted);

        // Supervisor için sadece yetkili olduğu organizasyonları filtrele
        if (allowedOrgIds != null)
            organizationsQuery = organizationsQuery.Where(o => allowedOrgIds.Contains(o.Id));

        if (organizationIds?.Any() == true)
        {
            organizationsQuery = organizationsQuery.Where(o => organizationIds.Contains(o.Id));
        }

        var organizations = await organizationsQuery
            .OrderBy(o => o.Name)
            .Select(o => new { o.Id, o.Name })
            .ToListAsync();

        // Organizasyon ID'lerini al
        var orgIds = organizations.Select(o => o.Id).ToList();

        // Personel -> Organizasyon eşleştirmesini al (junction table)
        var personnelOrgMap = await _context.CustomerPersonnelOrganizations
            .Where(cpo => orgIds.Contains(cpo.CustomerOrganizationId))
            .Select(cpo => new { cpo.CustomerPersonnelId, cpo.CustomerOrganizationId })
            .ToListAsync();

        var personnelIds = personnelOrgMap.Select(x => x.CustomerPersonnelId).Distinct().ToList();

        // Değerlendirmeleri al: personel organizasyon ataması VEYA EvaluatedOrganizationId üzerinden
        var evaluationsQuery = _context.Evaluations
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.CallDate.HasValue &&
                        e.CallDate.Value >= start &&
                        e.CallDate.Value <= end &&
                        ((e.EvaluatedCustomerPersonnelId.HasValue && personnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value)) ||
                         (e.EvaluatedOrganizationId.HasValue && orgIds.Contains(e.EvaluatedOrganizationId.Value))));

        if (organizationIds?.Any() == true)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                (e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value)) ||
                (e.EvaluatedCustomerPersonnelId.HasValue &&
                 _context.CustomerPersonnelOrganizations.Any(cpo =>
                     cpo.CustomerPersonnelId == e.EvaluatedCustomerPersonnelId.Value &&
                     organizationIds.Contains(cpo.CustomerOrganizationId))));
        }

        var evaluations = await evaluationsQuery.ToListAsync();

        // Evaluation -> Org eşleştirme lookup'ı oluştur
        var personnelToOrgs = personnelOrgMap
            .GroupBy(x => x.CustomerPersonnelId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CustomerOrganizationId).ToList());

        // Tarih aralığına göre gruplama tipini belirle
        var totalDays = (end - start).TotalDays;
        var labels = new List<string>();
        var dateRanges = new List<(DateTime Start, DateTime End)>();

        if (totalDays <= 14)
        {
            // 2 hafta veya daha az: Günlük
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                labels.Add(date.ToString("dd MMM", new System.Globalization.CultureInfo("tr-TR")));
                dateRanges.Add((DateTime.SpecifyKind(date, DateTimeKind.Utc), DateTime.SpecifyKind(date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc)));
            }
        }
        else if (totalDays <= 90)
        {
            // 3 ay veya daha az: Haftalık
            var weekStart = start.Date;
            while (weekStart <= end.Date)
            {
                var weekEnd = weekStart.AddDays(6);
                if (weekEnd > end.Date) weekEnd = end.Date;

                labels.Add(weekStart.ToString("dd MMM", new System.Globalization.CultureInfo("tr-TR")));
                dateRanges.Add((DateTime.SpecifyKind(weekStart, DateTimeKind.Utc), DateTime.SpecifyKind(weekEnd.AddDays(1).AddSeconds(-1), DateTimeKind.Utc)));
                weekStart = weekStart.AddDays(7);
            }
        }
        else
        {
            // 3 aydan fazla: Aylık
            var monthStart = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            while (monthStart <= end)
            {
                var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);

                labels.Add(monthStart.ToString("MMM yy", new System.Globalization.CultureInfo("tr-TR")));
                dateRanges.Add((monthStart, monthEnd));
                monthStart = monthStart.AddMonths(1);
            }
        }

        // Genel trend (CallDate'e göre)
        var overallTrend = new List<object>();
        foreach (var (rangeStart, rangeEnd) in dateRanges)
        {
            var periodEvals = evaluations.Where(e => e.CallDate.HasValue && e.CallDate.Value >= rangeStart && e.CallDate.Value <= rangeEnd).ToList();
            var withScore = periodEvals.Where(e => e.ScorePercentage.HasValue).ToList();
            var avgScore = withScore.Any() ? withScore.Average(e => (double)e.ScorePercentage!.Value) : 0;

            overallTrend.Add(new
            {
                count = periodEvals.Count,
                averageScore = Math.Round(avgScore, 2)
            });
        }

        // Evaluation'ın hangi org'a ait olduğunu belirle (helper)
        Func<Evaluation, int, bool> evalBelongsToOrg = (e, orgId) =>
        {
            if (e.EvaluatedOrganizationId == orgId) return true;
            if (e.EvaluatedCustomerPersonnelId.HasValue &&
                personnelToOrgs.TryGetValue(e.EvaluatedCustomerPersonnelId.Value, out var orgs2) &&
                orgs2.Contains(orgId)) return true;
            return false;
        };

        // Organizasyon bazlı trend (en fazla 5 organizasyon)
        var topOrganizations = organizations
            .Select(o => new
            {
                o.Id,
                o.Name,
                EvaluationCount = evaluations.Count(e => evalBelongsToOrg(e, o.Id))
            })
            .Where(o => o.EvaluationCount > 0)
            .OrderByDescending(o => o.EvaluationCount)
            .Take(5)
            .ToList();

        var organizationTrends = new List<object>();
        foreach (var org in topOrganizations)
        {
            var orgData = new List<double>();
            foreach (var (rangeStart, rangeEnd) in dateRanges)
            {
                var orgEvals = evaluations
                    .Where(e => evalBelongsToOrg(e, org.Id) &&
                               e.CallDate.HasValue &&
                               e.CallDate.Value >= rangeStart &&
                               e.CallDate.Value <= rangeEnd &&
                               e.ScorePercentage.HasValue)
                    .ToList();

                var avgScore = orgEvals.Any() ? orgEvals.Average(e => (double)e.ScorePercentage!.Value) : 0;
                orgData.Add(Math.Round(avgScore, 2));
            }

            organizationTrends.Add(new
            {
                organizationId = org.Id,
                organizationName = org.Name,
                data = orgData
            });
        }

        return new
        {
            labels,
            overallTrend,
            organizationTrends,
            periodType = totalDays <= 14 ? "daily" : (totalDays <= 90 ? "weekly" : "monthly"),
            startDate = start,
            endDate = end.Date
        };
    }

    // ===== VALIDATION HELPERS =====

    public async Task<bool> ValidatePersonnelBelongsToCustomerAsync(int personnelId, int customerId)
    {
        var personnel = await _context.CustomerPersonnel.FindAsync(personnelId);
        return personnel != null && personnel.CustomerId == customerId;
    }

    public async Task<(string FirstName, string LastName)?> GetPersonnelNameAsync(int personnelId, int customerId)
    {
        var personnel = await _context.CustomerPersonnel.FindAsync(personnelId);
        if (personnel == null || personnel.CustomerId != customerId)
            return null;
        return (personnel.FirstName, personnel.LastName);
    }

    public async Task<bool> ValidateSurveyProjectBelongsToCustomerAsync(int projectId, int customerId)
    {
        return await _context.Projects.AnyAsync(p => p.Id == projectId &&
            p.CustomerId == customerId &&
            p.ProjectTypeId == Core.Enums.ProjectTypes.Ids.OnlineSurvey &&
            p.IsActive && !p.IsDeleted);
    }

    public async Task<bool> ValidateDealerBelongsToCustomerAsync(int dealerId, int customerId)
    {
        var dealer = await _context.CustomerDealers.FindAsync(dealerId);
        return dealer != null && dealer.CustomerId == customerId;
    }

    // ===== TRAINING VIDEO METHODS =====

    public async Task<object> GetMyTrainingsAsync(int personnelId)
    {
        var now = TurkeyTime.Now;

        var trainings = await _context.TrainingVideoParticipants
            .Include(p => p.Assignment)
                .ThenInclude(a => a.TrainingVideo)
            .Where(p => p.CustomerPersonnelId == personnelId && !p.IsDeleted)
            .Where(p => p.Assignment.IsActive && !p.Assignment.IsDeleted)
            .OrderByDescending(p => p.Assignment.DueDate)
            .Select(p => new
            {
                participantId = p.Id,
                assignmentId = p.Assignment.Id,
                assignmentTitle = p.Assignment.Title,
                videoId = p.Assignment.TrainingVideo.Id,
                videoTitle = p.Assignment.TrainingVideo.Title,
                videoDescription = p.Assignment.TrainingVideo.Description,
                videoDurationSeconds = p.Assignment.TrainingVideo.DurationSeconds,
                startDate = p.Assignment.StartDate,
                dueDate = p.Assignment.DueDate,
                statusId = p.StatusId,
                statusName = p.StatusId == 1 ? "Bekliyor" : p.StatusId == 2 ? "İzleniyor" : "Tamamlandı",
                startedAt = p.StartedAt,
                completedAt = p.CompletedAt,
                watchedSeconds = p.WatchedSeconds,
                isCompleted = p.IsCompleted,
                isOverdue = p.Assignment.DueDate < now && !p.IsCompleted,
                daysRemaining = p.IsCompleted ? 0 : (int)Math.Max(0, (p.Assignment.DueDate - now).TotalDays),
                // Video izleme kuralları (Video'dan)
                minWatchPercentage = p.Assignment.TrainingVideo.MinWatchPercentage,
                allowSkipping = p.Assignment.TrainingVideo.AllowSkipping,
                maxPlaybackSpeed = p.Assignment.TrainingVideo.MaxPlaybackSpeed,
                // Atama izleme kuralları (Assignment'tan)
                allowSeeking = p.Assignment.AllowSeeking,
                allowSpeedChange = p.Assignment.AllowSpeedChange,
                // İzleme sayısı bilgileri
                watchCount = p.WatchCount,
                minWatchCount = p.Assignment.MinWatchCount,
                maxWatchCount = p.Assignment.MaxWatchCount,
                remainingWatches = p.Assignment.MaxWatchCount.HasValue
                    ? Math.Max(0, p.Assignment.MaxWatchCount.Value - p.WatchCount)
                    : (int?)null
            })
            .ToListAsync();

        return trainings;
    }

    public async Task<object?> UpdateMyTrainingProgressAsync(int participantId, int personnelId, int watchedSeconds, bool isCompleted)
    {
        var participant = await _context.TrainingVideoParticipants
            .Include(p => p.Assignment)
                .ThenInclude(a => a.TrainingVideo)
            .FirstOrDefaultAsync(p => p.Id == participantId && p.CustomerPersonnelId == personnelId && !p.IsDeleted);

        if (participant == null)
            return null;

        var now = TurkeyTime.Now;

        // İlk izlemeye başlama
        if (participant.StatusId == 1 && watchedSeconds > 0)
        {
            participant.StatusId = 2;
            participant.StartedAt = now;
        }

        participant.WatchedSeconds = watchedSeconds;

        // Tamamlama kontrolü
        if (isCompleted || participant.WatchedSeconds >= participant.Assignment.TrainingVideo.DurationSeconds)
        {
            participant.IsCompleted = true;
            participant.StatusId = 3;
            participant.CompletedAt ??= now;
        }

        participant.UpdatedAt = now;
        await _context.SaveChangesAsync();

        return new {
            success = true,
            watchCount = participant.WatchCount,
            maxWatchCount = participant.Assignment.MaxWatchCount,
            remainingWatches = participant.Assignment.MaxWatchCount.HasValue
                ? Math.Max(0, participant.Assignment.MaxWatchCount.Value - participant.WatchCount)
                : (int?)null
        };
    }

    public async Task<object?> StartWatchSessionAsync(int participantId, int personnelId)
    {
        var participant = await _context.TrainingVideoParticipants
            .Include(p => p.Assignment)
            .FirstOrDefaultAsync(p => p.Id == participantId && p.CustomerPersonnelId == personnelId && !p.IsDeleted);

        if (participant == null)
            return null;

        var now = TurkeyTime.Now;

        // MaxWatchCount kontrolü
        var maxWatches = participant.Assignment.MaxWatchCount;
        if (maxWatches.HasValue && participant.WatchCount >= maxWatches.Value)
        {
            return new {
                success = false,
                message = "Maksimum izleme hakkınızı doldurdunuz.",
                watchCount = participant.WatchCount,
                maxWatchCount = maxWatches.Value
            };
        }

        // İzleme hakkını kullan
        participant.WatchCount++;

        // İlk izlemeye başlama
        if (participant.StatusId == 1)
        {
            participant.StatusId = 2;
            participant.StartedAt = now;
        }

        participant.UpdatedAt = now;
        await _context.SaveChangesAsync();

        return new {
            success = true,
            watchCount = participant.WatchCount,
            maxWatchCount = participant.Assignment.MaxWatchCount,
            remainingWatches = participant.Assignment.MaxWatchCount.HasValue
                ? Math.Max(0, participant.Assignment.MaxWatchCount.Value - participant.WatchCount)
                : (int?)null
        };
    }

    public async Task<object> GetStaffTrainingsAsync(int customerId, List<int>? allowedPersonnelIds)
    {
        var now = TurkeyTime.Now;

        var query = _context.TrainingVideoParticipants
            .Include(p => p.CustomerPersonnel)
            .Include(p => p.Assignment)
                .ThenInclude(a => a.TrainingVideo)
            .Where(p => !p.IsDeleted && p.Assignment.IsActive && !p.Assignment.IsDeleted)
            .Where(p => p.CustomerPersonnel.CustomerId == customerId);

        // CustomerManager tüm personeli görebilir
        // CustomerSupervisor sadece altındakileri görebilir
        if (allowedPersonnelIds != null)
        {
            query = query.Where(p => allowedPersonnelIds.Contains(p.CustomerPersonnelId));
        }

        var trainings = await query
            .OrderByDescending(p => p.Assignment.DueDate)
            .ThenBy(p => p.CustomerPersonnel.FirstName)
            .Select(p => new
            {
                participantId = p.Id,
                personnelId = p.CustomerPersonnelId,
                personnelName = p.CustomerPersonnel.FirstName + " " + p.CustomerPersonnel.LastName,
                personnelEmail = p.CustomerPersonnel.Email,
                assignmentId = p.Assignment.Id,
                assignmentTitle = p.Assignment.Title,
                videoId = p.Assignment.TrainingVideo.Id,
                videoTitle = p.Assignment.TrainingVideo.Title,
                videoDurationSeconds = p.Assignment.TrainingVideo.DurationSeconds,
                startDate = p.Assignment.StartDate,
                dueDate = p.Assignment.DueDate,
                statusId = p.StatusId,
                startedAt = p.StartedAt,
                completedAt = p.CompletedAt,
                watchedSeconds = p.WatchedSeconds,
                isCompleted = p.IsCompleted,
                isOverdue = p.Assignment.DueDate < now && !p.IsCompleted,
                daysRemaining = p.IsCompleted ? 0 : (int)Math.Max(0, (p.Assignment.DueDate - now).TotalDays)
            })
            .ToListAsync();

        return new { trainings };
    }

    // ===== DRAFT DELETION METHODS =====

    public async Task<(bool Success, string? ErrorMessage, int? StatusCode)?> DeleteDraftAsync(int evaluationId, int customerId, string? role, int? personnelId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == evaluationId && !e.IsDeleted);

        if (evaluation == null)
            return (false, "Değerlendirme bulunamadı.", 404);

        if (evaluation.Project?.CustomerId != customerId)
            return (false, null, 403);

        if (evaluation.StatusId != Core.Enums.EvaluationStatuses.Ids.Draft)
            return (false, "Sadece taslak durumundaki değerlendirmeler silinebilir.", 400);

        var isManager = role == "CustomerManager";
        if (!isManager && personnelId.HasValue)
        {
            if (evaluation.EvaluatorCustomerPersonnelId != personnelId.Value)
                return (false, null, 403);
        }

        evaluation.IsDeleted = true;
        evaluation.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        return (true, null, null);
    }

    public async Task<(bool Success, string? ErrorMessage, int? StatusCode)?> DeleteInternalDraftAsync(int evaluationId, int customerId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == evaluationId && !e.IsDeleted && e.EvaluatorCustomerPersonnelId != null);

        if (evaluation == null)
            return (false, "Değerlendirme bulunamadı.", 404);

        if (evaluation.Project?.CustomerId != customerId)
            return (false, null, 403);

        if (evaluation.StatusId != Core.Enums.EvaluationStatuses.Ids.Draft)
            return (false, "Sadece taslak durumundaki değerlendirmeler silinebilir.", 400);

        evaluation.IsDeleted = true;
        evaluation.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        return (true, null, null);
    }

    // ===== ENNEAGRAM EXPORT HELPER =====

    public async Task<List<int>?> GetEnneagramProjectIdsForCustomerAsync(int customerId, int? projectId)
    {
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == Core.Enums.ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        var customerProjectsQuery = _context.Projects
            .Where(p => p.CustomerId == customerId &&
                   enneagramChecklistIds.Contains(p.ChecklistId) &&
                   p.IsActive && !p.IsDeleted);

        if (projectId.HasValue)
        {
            var exists = await customerProjectsQuery.AnyAsync(p => p.Id == projectId.Value);
            if (!exists)
                return null;
            return new List<int> { projectId.Value };
        }
        else
        {
            return await customerProjectsQuery.Select(p => p.Id).ToListAsync();
        }
    }

    // ===== PERFORMANCE BY PERIOD =====

    public async Task<object> GetPerformanceByPeriodAsync(int customerId, List<int>? allowedPersonnelIds, List<int>? allowedOrgIds, List<int>? projectIds, List<int>? organizationIds, DateTime? startDate, DateTime? endDate)
    {
        // Müşteriye ait projeleri al
        var projectsQuery = _context.Projects
            .Where(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted);

        if (projectIds?.Any() == true)
        {
            projectsQuery = projectsQuery.Where(p => projectIds.Contains(p.Id));
        }

        var filteredProjectIds = await projectsQuery.Select(p => p.Id).ToListAsync();

        if (!filteredProjectIds.Any())
        {
            return new { periods = new List<object>(), data = new List<object>() };
        }

        // Personelleri al (organizasyon filtresiyle)
        var personnelQuery = _context.CustomerPersonnel
            .Include(cp => cp.OrganizationAssignments)
                .ThenInclude(cpo => cpo.CustomerOrganization)
            .Where(cp => cp.CustomerId == customerId && cp.IsActive && !cp.IsDeleted);

        // Supervisor personel filtresi
        if (allowedPersonnelIds != null)
        {
            personnelQuery = personnelQuery.Where(cp => allowedPersonnelIds.Contains(cp.Id));
        }

        // Supervisor organizasyon filtresi
        if (allowedOrgIds != null)
        {
            personnelQuery = personnelQuery.Where(cp =>
                cp.OrganizationAssignments.Any(cpo => allowedOrgIds.Contains(cpo.CustomerOrganizationId)));
        }

        // Kullanıcının seçtiği organizasyon filtresi
        if (organizationIds?.Any() == true)
        {
            personnelQuery = personnelQuery.Where(cp =>
                cp.OrganizationAssignments.Any(cpo => organizationIds.Contains(cpo.CustomerOrganizationId)));
        }

        var personnel = await personnelQuery
            .OrderBy(cp => cp.FirstName).ThenBy(cp => cp.LastName)
            .Select(cp => new
            {
                cp.Id,
                FullName = cp.FirstName + " " + cp.LastName,
                OrganizationName = cp.OrganizationAssignments
                    .Select(cpo => cpo.CustomerOrganization.Name)
                    .FirstOrDefault() ?? "-"
            })
            .ToListAsync();

        var personnelIds = personnel.Select(p => p.Id).ToList();

        // Önce AssignmentPeriod'ları kontrol et
        var assignmentPeriodsQuery = _context.AssignmentPeriods
            .Include(ap => ap.Assignment)
                .ThenInclude(a => a.Project)
            .Where(ap => filteredProjectIds.Contains(ap.Assignment.ProjectId) && !ap.IsDeleted);

        // Tarih filtresi (AssignmentPeriod'lar için dönem tarihlerine göre)
        if (startDate.HasValue)
        {
            assignmentPeriodsQuery = assignmentPeriodsQuery.Where(ap => ap.EndDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            assignmentPeriodsQuery = assignmentPeriodsQuery.Where(ap => ap.StartDate <= endDate.Value);
        }

        var assignmentPeriods = await assignmentPeriodsQuery
            .OrderBy(ap => ap.StartDate)
            .Select(ap => new
            {
                ap.Id,
                ap.Name,
                ap.StartDate,
                ap.EndDate,
                ProjectName = ap.Assignment.Project.Code != null ? ap.Assignment.Project.Code + " - " + ap.Assignment.Project.Name : ap.Assignment.Project.Name
            })
            .ToListAsync();

        // AssignmentPeriod varsa onları kullan
        if (assignmentPeriods.Any())
        {
            var periodIds = assignmentPeriods.Select(p => p.Id).ToList();

            var evaluations = await _context.Evaluations
                .Where(e => e.AssignmentPeriodId.HasValue &&
                           periodIds.Contains(e.AssignmentPeriodId.Value) &&
                           e.EvaluatedCustomerPersonnelId.HasValue &&
                           personnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                           e.StatusId == EvaluationStatuses.Ids.Completed &&
                           e.ScorePercentage.HasValue)
                .Select(e => new
                {
                    e.AssignmentPeriodId,
                    e.EvaluatedCustomerPersonnelId,
                    e.ScorePercentage,
                    e.YellowCardCount,
                    e.RedCardCount
                })
                .ToListAsync();

            var data = personnel.Select(p => new
            {
                personnelId = p.Id,
                personnelName = p.FullName,
                organizationName = p.OrganizationName,
                periodScores = assignmentPeriods.Select(period =>
                {
                    var periodEvals = evaluations
                        .Where(e => e.AssignmentPeriodId == period.Id && e.EvaluatedCustomerPersonnelId == p.Id)
                        .ToList();

                    return new
                    {
                        periodId = period.Id,
                        periodName = period.Name,
                        evaluationCount = periodEvals.Count,
                        averageScore = periodEvals.Any() ? Math.Round(periodEvals.Average(e => (double)e.ScorePercentage!.Value), 2) : (double?)null,
                        yellowCardCount = periodEvals.Sum(e => e.YellowCardCount),
                        redCardCount = periodEvals.Sum(e => e.RedCardCount)
                    };
                }).ToList(),
                overallAverage = evaluations
                    .Where(e => e.EvaluatedCustomerPersonnelId == p.Id)
                    .Select(e => (double)e.ScorePercentage!.Value)
                    .DefaultIfEmpty()
                    .Average(),
                totalEvaluations = evaluations.Count(e => e.EvaluatedCustomerPersonnelId == p.Id)
            })
            .Where(p => p.totalEvaluations > 0)
            .OrderByDescending(p => p.overallAverage)
            .ToList();

            return new
            {
                periods = assignmentPeriods.Select(p => new { p.Id, p.Name, p.ProjectName, p.StartDate, p.EndDate }),
                data
            };
        }

        // AssignmentPeriod yoksa CallDate'e göre aylık dönemler oluştur
        var allEvaluationsQuery = _context.Evaluations
            .Include(e => e.Project)
            .Where(e => e.Project != null &&
                       filteredProjectIds.Contains(e.ProjectId) &&
                       e.EvaluatedCustomerPersonnelId.HasValue &&
                       personnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                       e.StatusId == EvaluationStatuses.Ids.Completed &&
                       e.ScorePercentage.HasValue &&
                       e.CallDate.HasValue);

        // Tarih filtresi (CallDate'e göre)
        if (startDate.HasValue)
        {
            allEvaluationsQuery = allEvaluationsQuery.Where(e => e.CallDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            allEvaluationsQuery = allEvaluationsQuery.Where(e => e.CallDate <= endDate.Value);
        }

        var allEvaluations = await allEvaluationsQuery
            .Select(e => new
            {
                e.EvaluatedCustomerPersonnelId,
                e.ScorePercentage,
                e.YellowCardCount,
                e.RedCardCount,
                e.CallDate,
                ProjectName = e.Project!.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name
            })
            .ToListAsync();

        if (!allEvaluations.Any())
        {
            return new { periods = new List<object>(), data = new List<object>() };
        }

        // CallDate'e göre aylık dönemler oluştur
        var monthlyPeriods = allEvaluations
            .GroupBy(e => new { Year = e.CallDate!.Value.Year, Month = e.CallDate!.Value.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select((g, idx) => new
            {
                Id = -(idx + 1), // Negatif ID (sanal dönem)
                Name = $"{g.Key.Year}-{g.Key.Month:D2}",
                StartDate = new DateTime(g.Key.Year, g.Key.Month, 1),
                EndDate = new DateTime(g.Key.Year, g.Key.Month, DateTime.DaysInMonth(g.Key.Year, g.Key.Month)),
                ProjectName = g.Select(e => e.ProjectName).FirstOrDefault() ?? "-",
                Evaluations = g.ToList()
            })
            .ToList();

        var monthlyData = personnel.Select(p => new
        {
            personnelId = p.Id,
            personnelName = p.FullName,
            organizationName = p.OrganizationName,
            periodScores = monthlyPeriods.Select(period =>
            {
                var periodEvals = period.Evaluations
                    .Where(e => e.EvaluatedCustomerPersonnelId == p.Id)
                    .ToList();

                return new
                {
                    periodId = period.Id,
                    periodName = period.Name,
                    evaluationCount = periodEvals.Count,
                    averageScore = periodEvals.Any() ? Math.Round(periodEvals.Average(e => (double)e.ScorePercentage!.Value), 2) : (double?)null,
                    yellowCardCount = periodEvals.Sum(e => e.YellowCardCount),
                    redCardCount = periodEvals.Sum(e => e.RedCardCount)
                };
            }).ToList(),
            overallAverage = allEvaluations
                .Where(e => e.EvaluatedCustomerPersonnelId == p.Id)
                .Select(e => (double)e.ScorePercentage!.Value)
                .DefaultIfEmpty()
                .Average(),
            totalEvaluations = allEvaluations.Count(e => e.EvaluatedCustomerPersonnelId == p.Id)
        })
        .Where(p => p.totalEvaluations > 0)
        .OrderByDescending(p => p.overallAverage)
        .ToList();

        return new
        {
            periods = monthlyPeriods.Select(p => new { p.Id, p.Name, p.ProjectName, p.StartDate, p.EndDate }),
            data = monthlyData
        };
    }

    public async Task<(byte[] FileContent, string FileName)?> ExportPerformanceByPeriodAsync(int customerId, List<int>? allowedPersonnelIds, List<int>? allowedOrgIds, List<int>? projectIds, List<int>? organizationIds, DateTime? startDate, DateTime? endDate)
    {
        // Get score thresholds for Excel color coding
        var excelThresholds = await _customerScoreThresholdService.GetAllAsync(customerId);
        var excelSuccessThreshold = excelThresholds.Any() ? (double)excelThresholds.Max(t => t.SuccessThreshold) : 80.0;
        var excelWarningThreshold = excelThresholds.Any() ? (double)excelThresholds.Max(t => t.WarningThreshold) : 60.0;

        // Proje filtresi
        var projectsQuery = _context.Projects
            .Where(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted);

        if (projectIds?.Any() == true)
        {
            projectsQuery = projectsQuery.Where(p => projectIds.Contains(p.Id));
        }

        var filteredProjectIds = await projectsQuery.Select(p => p.Id).ToListAsync();

        // Personel filtresi
        var personnelQuery = _context.CustomerPersonnel
            .Include(cp => cp.OrganizationAssignments)
                .ThenInclude(cpo => cpo.CustomerOrganization)
            .Where(cp => cp.CustomerId == customerId && cp.IsActive && !cp.IsDeleted);

        // Supervisor personel filtresi
        if (allowedPersonnelIds != null)
        {
            personnelQuery = personnelQuery.Where(cp => allowedPersonnelIds.Contains(cp.Id));
        }

        // Supervisor organizasyon filtresi
        if (allowedOrgIds != null)
        {
            personnelQuery = personnelQuery.Where(cp =>
                cp.OrganizationAssignments.Any(cpo => allowedOrgIds.Contains(cpo.CustomerOrganizationId)));
        }

        // Kullanıcının seçtiği organizasyon filtresi
        if (organizationIds?.Any() == true)
        {
            personnelQuery = personnelQuery.Where(cp =>
                cp.OrganizationAssignments.Any(cpo => organizationIds.Contains(cpo.CustomerOrganizationId)));
        }

        var personnel = await personnelQuery
            .OrderBy(cp => cp.FirstName).ThenBy(cp => cp.LastName)
            .Select(cp => new
            {
                cp.Id,
                FullName = cp.FirstName + " " + cp.LastName,
                OrganizationName = cp.OrganizationAssignments
                    .Select(cpo => cpo.CustomerOrganization.Name)
                    .FirstOrDefault() ?? "-"
            })
            .ToListAsync();

        var personnelIds = personnel.Select(p => p.Id).ToList();

        // AssignmentPeriod kontrolü
        var assignmentPeriodsQuery = _context.AssignmentPeriods
            .Include(ap => ap.Assignment)
                .ThenInclude(a => a.Project)
            .Where(ap => filteredProjectIds.Contains(ap.Assignment.ProjectId) && !ap.IsDeleted);

        // Tarih filtresi (AssignmentPeriod'lar için dönem tarihlerine göre)
        if (startDate.HasValue)
        {
            assignmentPeriodsQuery = assignmentPeriodsQuery.Where(ap => ap.EndDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            assignmentPeriodsQuery = assignmentPeriodsQuery.Where(ap => ap.StartDate <= endDate.Value);
        }

        var assignmentPeriods = await assignmentPeriodsQuery
            .OrderBy(ap => ap.StartDate)
            .Select(ap => new { ap.Id, ap.Name })
            .ToListAsync();

        // Excel oluştur
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var sheet = workbook.Worksheets.Add("Dönem Bazlı Başarı");

        // AssignmentPeriod varsa onu kullan
        if (assignmentPeriods.Any())
        {
            var periodIds = assignmentPeriods.Select(p => p.Id).ToList();

            var evaluations = await _context.Evaluations
                .Include(e => e.Project)
                .Where(e => e.AssignmentPeriodId.HasValue &&
                           periodIds.Contains(e.AssignmentPeriodId.Value) &&
                           e.EvaluatedCustomerPersonnelId.HasValue &&
                           personnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                           e.StatusId == EvaluationStatuses.Ids.Completed &&
                           e.ScorePercentage.HasValue)
                .Select(e => new
                {
                    e.AssignmentPeriodId,
                    e.EvaluatedCustomerPersonnelId,
                    e.ScorePercentage,
                    IsInternal = e.EvaluatorCustomerPersonnelId != null,
                    ProjectCode = e.Project.Code ?? ""
                })
                .ToListAsync();

            // Headers - Row 1: Merge header for period groups, Row 2: Sub-headers
            sheet.Cell(1, 1).Value = "Personel";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Range(1, 1, 2, 1).Merge();
            sheet.Cell(1, 2).Value = "Organizasyon";
            sheet.Cell(1, 2).Style.Font.Bold = true;
            sheet.Range(1, 2, 2, 2).Merge();
            sheet.Cell(1, 3).Value = "Projeler";
            sheet.Cell(1, 3).Style.Font.Bold = true;
            sheet.Range(1, 3, 2, 3).Merge();

            int col = 4;
            foreach (var period in assignmentPeriods)
            {
                sheet.Cell(1, col).Value = period.Name;
                sheet.Cell(1, col).Style.Font.Bold = true;
                sheet.Range(1, col, 1, col + 2).Merge();
                sheet.Cell(2, col).Value = "Genel";
                sheet.Cell(2, col).Style.Font.Bold = true;
                sheet.Cell(2, col + 1).Value = "İç";
                sheet.Cell(2, col + 1).Style.Font.Bold = true;
                sheet.Cell(2, col + 2).Value = "Dış";
                sheet.Cell(2, col + 2).Style.Font.Bold = true;
                col += 3;
            }
            sheet.Cell(1, col).Value = "Genel Ortalama";
            sheet.Cell(1, col).Style.Font.Bold = true;
            sheet.Range(1, col, 2, col).Merge();
            sheet.Cell(1, col + 1).Value = "İç Ortalama";
            sheet.Cell(1, col + 1).Style.Font.Bold = true;
            sheet.Range(1, col + 1, 2, col + 1).Merge();
            sheet.Cell(1, col + 2).Value = "Dış Ortalama";
            sheet.Cell(1, col + 2).Style.Font.Bold = true;
            sheet.Range(1, col + 2, 2, col + 2).Merge();
            sheet.Cell(1, col + 3).Value = "Toplam Değerlendirme";
            sheet.Cell(1, col + 3).Style.Font.Bold = true;
            sheet.Range(1, col + 3, 2, col + 3).Merge();

            // Data rows
            int row = 3;
            foreach (var p in personnel)
            {
                var personEvals = evaluations.Where(e => e.EvaluatedCustomerPersonnelId == p.Id).ToList();
                if (!personEvals.Any()) continue;

                sheet.Cell(row, 1).Value = p.FullName;
                sheet.Cell(row, 2).Value = p.OrganizationName;

                var projectCodes = personEvals
                    .Where(e => !string.IsNullOrEmpty(e.ProjectCode))
                    .Select(e => e.ProjectCode)
                    .Distinct()
                    .OrderBy(c => c);
                sheet.Cell(row, 3).Value = string.Join(", ", projectCodes);

                col = 4;
                foreach (var period in assignmentPeriods)
                {
                    var periodEvals = personEvals.Where(e => e.AssignmentPeriodId == period.Id).ToList();
                    var internalEvals = periodEvals.Where(e => e.IsInternal).ToList();
                    var externalEvals = periodEvals.Where(e => !e.IsInternal).ToList();

                    // Genel
                    if (periodEvals.Any())
                    {
                        var avg = periodEvals.Average(e => (double)e.ScorePercentage!.Value);
                        sheet.Cell(row, col).Value = Math.Round(avg, 2);
                        if (avg >= excelSuccessThreshold)
                            sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGreen;
                        else if (avg >= excelWarningThreshold)
                            sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightYellow;
                        else
                            sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightCoral;
                    }
                    else
                    {
                        sheet.Cell(row, col).Value = "-";
                    }

                    // İç
                    if (internalEvals.Any())
                    {
                        var avg = internalEvals.Average(e => (double)e.ScorePercentage!.Value);
                        sheet.Cell(row, col + 1).Value = Math.Round(avg, 2);
                    }
                    else
                    {
                        sheet.Cell(row, col + 1).Value = "-";
                    }

                    // Dış
                    if (externalEvals.Any())
                    {
                        var avg = externalEvals.Average(e => (double)e.ScorePercentage!.Value);
                        sheet.Cell(row, col + 2).Value = Math.Round(avg, 2);
                    }
                    else
                    {
                        sheet.Cell(row, col + 2).Value = "-";
                    }

                    col += 3;
                }

                // Genel Ortalama
                var overallAvg = personEvals.Average(e => (double)e.ScorePercentage!.Value);
                sheet.Cell(row, col).Value = Math.Round(overallAvg, 2);
                sheet.Cell(row, col).Style.Font.Bold = true;

                // İç Ortalama
                var allInternal = personEvals.Where(e => e.IsInternal).ToList();
                if (allInternal.Any())
                {
                    var internalAvg = allInternal.Average(e => (double)e.ScorePercentage!.Value);
                    sheet.Cell(row, col + 1).Value = Math.Round(internalAvg, 2);
                    sheet.Cell(row, col + 1).Style.Font.Bold = true;
                }
                else
                {
                    sheet.Cell(row, col + 1).Value = "-";
                }

                // Dış Ortalama
                var allExternal = personEvals.Where(e => !e.IsInternal).ToList();
                if (allExternal.Any())
                {
                    var externalAvg = allExternal.Average(e => (double)e.ScorePercentage!.Value);
                    sheet.Cell(row, col + 2).Value = Math.Round(externalAvg, 2);
                    sheet.Cell(row, col + 2).Style.Font.Bold = true;
                }
                else
                {
                    sheet.Cell(row, col + 2).Value = "-";
                }

                sheet.Cell(row, col + 3).Value = personEvals.Count;

                row++;
            }
        }
        else
        {
            // AssignmentPeriod yoksa CallDate'e göre aylık dönemler oluştur
            var allEvaluationsQuery = _context.Evaluations
                .Include(e => e.Project)
                .Where(e => e.Project != null &&
                           filteredProjectIds.Contains(e.ProjectId) &&
                           e.EvaluatedCustomerPersonnelId.HasValue &&
                           personnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                           e.StatusId == EvaluationStatuses.Ids.Completed &&
                           e.ScorePercentage.HasValue &&
                           e.CallDate.HasValue);

            // Tarih filtresi (CallDate'e göre)
            if (startDate.HasValue)
            {
                allEvaluationsQuery = allEvaluationsQuery.Where(e => e.CallDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                allEvaluationsQuery = allEvaluationsQuery.Where(e => e.CallDate <= endDate.Value);
            }

            var allEvaluations = await allEvaluationsQuery
                .Select(e => new
                {
                    e.EvaluatedCustomerPersonnelId,
                    e.ScorePercentage,
                    e.CallDate,
                    IsInternal = e.EvaluatorCustomerPersonnelId != null,
                    ProjectCode = e.Project!.Code ?? ""
                })
                .ToListAsync();

            // CallDate'e göre aylık dönemler oluştur
            var monthlyPeriods = allEvaluations
                .GroupBy(e => new { Year = e.CallDate!.Value.Year, Month = e.CallDate!.Value.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Name = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Year = g.Key.Year,
                    Month = g.Key.Month
                })
                .ToList();

            // Headers - Row 1: Merge header for period groups, Row 2: Sub-headers
            sheet.Cell(1, 1).Value = "Personel";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Range(1, 1, 2, 1).Merge();
            sheet.Cell(1, 2).Value = "Organizasyon";
            sheet.Cell(1, 2).Style.Font.Bold = true;
            sheet.Range(1, 2, 2, 2).Merge();
            sheet.Cell(1, 3).Value = "Projeler";
            sheet.Cell(1, 3).Style.Font.Bold = true;
            sheet.Range(1, 3, 2, 3).Merge();

            int col = 4;
            foreach (var period in monthlyPeriods)
            {
                sheet.Cell(1, col).Value = period.Name;
                sheet.Cell(1, col).Style.Font.Bold = true;
                sheet.Range(1, col, 1, col + 2).Merge();
                sheet.Cell(2, col).Value = "Genel";
                sheet.Cell(2, col).Style.Font.Bold = true;
                sheet.Cell(2, col + 1).Value = "İç";
                sheet.Cell(2, col + 1).Style.Font.Bold = true;
                sheet.Cell(2, col + 2).Value = "Dış";
                sheet.Cell(2, col + 2).Style.Font.Bold = true;
                col += 3;
            }
            sheet.Cell(1, col).Value = "Genel Ortalama";
            sheet.Cell(1, col).Style.Font.Bold = true;
            sheet.Range(1, col, 2, col).Merge();
            sheet.Cell(1, col + 1).Value = "İç Ortalama";
            sheet.Cell(1, col + 1).Style.Font.Bold = true;
            sheet.Range(1, col + 1, 2, col + 1).Merge();
            sheet.Cell(1, col + 2).Value = "Dış Ortalama";
            sheet.Cell(1, col + 2).Style.Font.Bold = true;
            sheet.Range(1, col + 2, 2, col + 2).Merge();
            sheet.Cell(1, col + 3).Value = "Toplam Değerlendirme";
            sheet.Cell(1, col + 3).Style.Font.Bold = true;
            sheet.Range(1, col + 3, 2, col + 3).Merge();

            // Data rows
            int row = 3;
            foreach (var p in personnel)
            {
                var personEvals = allEvaluations.Where(e => e.EvaluatedCustomerPersonnelId == p.Id).ToList();
                if (!personEvals.Any()) continue;

                sheet.Cell(row, 1).Value = p.FullName;
                sheet.Cell(row, 2).Value = p.OrganizationName;

                var projectCodes = personEvals
                    .Where(e => !string.IsNullOrEmpty(e.ProjectCode))
                    .Select(e => e.ProjectCode)
                    .Distinct()
                    .OrderBy(c => c);
                sheet.Cell(row, 3).Value = string.Join(", ", projectCodes);

                col = 4;
                foreach (var period in monthlyPeriods)
                {
                    var periodEvals = personEvals
                        .Where(e => e.CallDate!.Value.Year == period.Year && e.CallDate!.Value.Month == period.Month)
                        .ToList();
                    var internalEvals = periodEvals.Where(e => e.IsInternal).ToList();
                    var externalEvals = periodEvals.Where(e => !e.IsInternal).ToList();

                    // Genel
                    if (periodEvals.Any())
                    {
                        var avg = periodEvals.Average(e => (double)e.ScorePercentage!.Value);
                        sheet.Cell(row, col).Value = Math.Round(avg, 2);
                        if (avg >= excelSuccessThreshold)
                            sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGreen;
                        else if (avg >= excelWarningThreshold)
                            sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightYellow;
                        else
                            sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightCoral;
                    }
                    else
                    {
                        sheet.Cell(row, col).Value = "-";
                    }

                    // İç
                    if (internalEvals.Any())
                    {
                        var avg = internalEvals.Average(e => (double)e.ScorePercentage!.Value);
                        sheet.Cell(row, col + 1).Value = Math.Round(avg, 2);
                    }
                    else
                    {
                        sheet.Cell(row, col + 1).Value = "-";
                    }

                    // Dış
                    if (externalEvals.Any())
                    {
                        var avg = externalEvals.Average(e => (double)e.ScorePercentage!.Value);
                        sheet.Cell(row, col + 2).Value = Math.Round(avg, 2);
                    }
                    else
                    {
                        sheet.Cell(row, col + 2).Value = "-";
                    }

                    col += 3;
                }

                // Genel Ortalama
                var overallAvg = personEvals.Average(e => (double)e.ScorePercentage!.Value);
                sheet.Cell(row, col).Value = Math.Round(overallAvg, 2);
                sheet.Cell(row, col).Style.Font.Bold = true;

                // İç Ortalama
                var allInternal = personEvals.Where(e => e.IsInternal).ToList();
                if (allInternal.Any())
                {
                    var internalAvg = allInternal.Average(e => (double)e.ScorePercentage!.Value);
                    sheet.Cell(row, col + 1).Value = Math.Round(internalAvg, 2);
                    sheet.Cell(row, col + 1).Style.Font.Bold = true;
                }
                else
                {
                    sheet.Cell(row, col + 1).Value = "-";
                }

                // Dış Ortalama
                var allExternal = personEvals.Where(e => !e.IsInternal).ToList();
                if (allExternal.Any())
                {
                    var externalAvg = allExternal.Average(e => (double)e.ScorePercentage!.Value);
                    sheet.Cell(row, col + 2).Value = Math.Round(externalAvg, 2);
                    sheet.Cell(row, col + 2).Style.Font.Bold = true;
                }
                else
                {
                    sheet.Cell(row, col + 2).Value = "-";
                }

                sheet.Cell(row, col + 3).Value = personEvals.Count;

                row++;
            }
        }

        sheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(sheet);

        // ===== GENEL RAPOR SHEET =====
        var genelRaporQuery = _context.Answers
            .Include(a => a.Question)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Project)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.AssignmentPeriod)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedCustomerPersonnel)
                    .ThenInclude(cp => cp!.OrganizationAssignments)
                        .ThenInclude(oa => oa.CustomerOrganization)
            .Where(a => a.Evaluation.Project != null &&
                       a.Evaluation.Project.CustomerId == customerId &&
                       filteredProjectIds.Contains(a.Evaluation.ProjectId) &&
                       a.Evaluation.EvaluatedCustomerPersonnelId.HasValue &&
                       personnelIds.Contains(a.Evaluation.EvaluatedCustomerPersonnelId.Value) &&
                       a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed &&
                       a.Question.GroupName != null &&
                       a.Question.WeightPoints > 0);

        if (startDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            genelRaporQuery = genelRaporQuery.Where(a =>
                (a.Evaluation.AssignmentPeriod != null && a.Evaluation.AssignmentPeriod.EndDate >= startUtc) ||
                (a.Evaluation.AssignmentPeriod == null && (a.Evaluation.CallDate ?? a.Evaluation.ControlDate) >= startUtc));
        }
        if (endDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            genelRaporQuery = genelRaporQuery.Where(a =>
                (a.Evaluation.AssignmentPeriod != null && a.Evaluation.AssignmentPeriod.StartDate <= endUtc) ||
                (a.Evaluation.AssignmentPeriod == null && (a.Evaluation.CallDate ?? a.Evaluation.ControlDate) <= endUtc));
        }

        var genelRaporAnswers = await genelRaporQuery
            .Select(a => new
            {
                ProjectName = a.Evaluation.Project.Code != null ? a.Evaluation.Project.Code + " - " + a.Evaluation.Project.Name : a.Evaluation.Project.Name,
                PersonnelId = a.Evaluation.EvaluatedCustomerPersonnelId,
                PersonnelName = a.Evaluation.EvaluatedCustomerPersonnel != null
                    ? a.Evaluation.EvaluatedCustomerPersonnel.FirstName + " " + a.Evaluation.EvaluatedCustomerPersonnel.LastName
                    : "-",
                OrgName = a.Evaluation.EvaluatedCustomerPersonnel != null
                    ? a.Evaluation.EvaluatedCustomerPersonnel.OrganizationAssignments
                        .Select(oa => oa.CustomerOrganization.Name)
                        .FirstOrDefault() ?? "-"
                    : "-",
                GroupName = a.Question.GroupName!,
                EarnedPoints = a.EarnedPoints ?? 0,
                WeightPoints = a.Question.WeightPoints,
                PeriodName = a.Evaluation.AssignmentPeriod != null ? a.Evaluation.AssignmentPeriod.Name : null,
                PeriodStartDate = a.Evaluation.AssignmentPeriod != null ? a.Evaluation.AssignmentPeriod.StartDate : (DateTime?)null,
                EvalDate = a.Evaluation.CallDate ?? a.Evaluation.ControlDate
            })
            .ToListAsync();

        // Genel Rapor sheet'i - PIVOT TABLO FORMATI
        var genelSheet = workbook.Worksheets.Add("Genel Rapor");

        if (genelRaporAnswers.Any())
        {
            var pivotData = genelRaporAnswers
                .GroupBy(a => new { a.GroupName, a.PersonnelId, a.PersonnelName })
                .Select(g =>
                {
                    var answers = g.ToList();
                    var sumWeight = answers.Sum(a => a.WeightPoints);
                    var sumEarned = answers.Sum(a => a.EarnedPoints);
                    return new
                    {
                        g.Key.GroupName,
                        g.Key.PersonnelId,
                        g.Key.PersonnelName,
                        AvgScore = sumWeight > 0 ? Math.Round(sumEarned / sumWeight * 100, 2) : 0,
                        ErrorCount = answers.Count(a => a.EarnedPoints < a.WeightPoints)
                    };
                })
                .ToList();

            var groupNames = pivotData.Select(p => p.GroupName).Distinct().OrderBy(g => g).ToList();
            var personnelListPivot = pivotData
                .Select(p => new { p.PersonnelId, p.PersonnelName })
                .Distinct()
                .OrderBy(p => p.PersonnelName)
                .ToList();

            genelSheet.Cell(1, 1).Value = "Kontrol Sorusu";
            genelSheet.Cell(1, 1).Style.Font.Bold = true;
            int colG = 2;
            foreach (var person in personnelListPivot)
            {
                genelSheet.Cell(1, colG).Value = person.PersonnelName;
                genelSheet.Cell(1, colG).Style.Font.Bold = true;
                genelSheet.Range(1, colG, 1, colG + 1).Merge();
                genelSheet.Cell(1, colG).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                colG += 2;
            }
            int totalColStart = colG;
            genelSheet.Cell(1, colG).Value = "Ortalama Puan Toplamı";
            genelSheet.Cell(1, colG).Style.Font.Bold = true;
            genelSheet.Cell(1, colG + 1).Value = "Hata Sayısı Toplamı";
            genelSheet.Cell(1, colG + 1).Style.Font.Bold = true;

            genelSheet.Cell(2, 1).Value = "";
            colG = 2;
            foreach (var _ in personnelListPivot)
            {
                genelSheet.Cell(2, colG).Value = "Ortalama Puan";
                genelSheet.Cell(2, colG).Style.Font.Bold = true;
                genelSheet.Cell(2, colG + 1).Value = "Hata Sayısı";
                genelSheet.Cell(2, colG + 1).Style.Font.Bold = true;
                colG += 2;
            }
            genelSheet.Cell(2, totalColStart).Value = "";
            genelSheet.Cell(2, totalColStart + 1).Value = "";

            int rowG = 3;
            foreach (var groupName in groupNames)
            {
                genelSheet.Cell(rowG, 1).Value = groupName;
                colG = 2;
                var rowScores = new List<decimal>();
                var rowErrors = 0;
                foreach (var person in personnelListPivot)
                {
                    var data = pivotData.FirstOrDefault(p => p.GroupName == groupName && p.PersonnelId == person.PersonnelId);
                    if (data != null)
                    {
                        genelSheet.Cell(rowG, colG).Value = (double)data.AvgScore;
                        genelSheet.Cell(rowG, colG + 1).Value = data.ErrorCount;
                        rowScores.Add(data.AvgScore);
                        rowErrors += data.ErrorCount;
                    }
                    else
                    {
                        genelSheet.Cell(rowG, colG).Value = "-";
                        genelSheet.Cell(rowG, colG + 1).Value = "-";
                    }
                    colG += 2;
                }
                if (rowScores.Any())
                {
                    genelSheet.Cell(rowG, totalColStart).Value = (double)Math.Round(rowScores.Average(), 2);
                    genelSheet.Cell(rowG, totalColStart).Style.Font.Bold = true;
                }
                else
                {
                    genelSheet.Cell(rowG, totalColStart).Value = "-";
                }
                genelSheet.Cell(rowG, totalColStart + 1).Value = rowErrors;
                genelSheet.Cell(rowG, totalColStart + 1).Style.Font.Bold = true;
                rowG++;
            }
        }
        else
        {
            genelSheet.Cell(1, 1).Value = "Veri bulunamadı";
        }

        genelSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(genelSheet);

        // ===== SÜREÇ ANALİZİ SHEET (Flat Data Format) =====
        var surecSheet = workbook.Worksheets.Add("Süreç Analizi");

        var surecData = genelRaporAnswers
            .GroupBy(a => new
            {
                ProjectPeriod = a.PeriodName != null
                    ? $"{a.ProjectName} {a.PeriodName}"
                    : $"{a.ProjectName} {a.EvalDate?.Year}-{a.EvalDate?.Month:D2}",
                a.PersonnelId,
                a.PersonnelName,
                a.OrgName,
                a.GroupName,
                Year = a.PeriodStartDate?.Year ?? a.EvalDate?.Year ?? 0,
                YearMonth = a.PeriodStartDate != null
                    ? $"{a.PeriodStartDate.Value.Year}{a.PeriodStartDate.Value.Month:D2}"
                    : $"{a.EvalDate?.Year}{a.EvalDate?.Month:D2}"
            })
            .Select(g =>
            {
                var answers = g.ToList();
                var sumWeight = answers.Sum(a => a.WeightPoints);
                var sumEarned = answers.Sum(a => a.EarnedPoints);
                return new
                {
                    g.Key.ProjectPeriod,
                    g.Key.PersonnelName,
                    Departman = g.Key.OrgName,
                    KontrolSorusu = g.Key.GroupName,
                    Periyot = g.Key.Year,
                    PeriyotAy = g.Key.YearMonth,
                    OrtalamaPuan = sumWeight > 0 ? Math.Round(sumEarned / sumWeight * 100, 2) : 0,
                    HataSayisi = answers.Count(a => a.EarnedPoints < a.WeightPoints)
                };
            })
            .OrderBy(x => x.ProjectPeriod)
            .ThenBy(x => x.PersonnelName)
            .ThenBy(x => x.KontrolSorusu)
            .ToList();

        surecSheet.Cell(1, 1).Value = "Proje";
        surecSheet.Cell(1, 2).Value = "Müşteri Temsilcisi";
        surecSheet.Cell(1, 3).Value = "Departman";
        surecSheet.Cell(1, 4).Value = "Kontrol Sorusu";
        surecSheet.Cell(1, 5).Value = "Periyot";
        surecSheet.Cell(1, 6).Value = "Periyot (Ay)";
        surecSheet.Cell(1, 7).Value = "Ortalama Puan";
        surecSheet.Cell(1, 8).Value = "Hata Sayısı";

        var surecHeaderRange = surecSheet.Range(1, 1, 1, 8);
        surecHeaderRange.Style.Font.Bold = true;
        surecHeaderRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

        int surecRow = 2;
        foreach (var item in surecData)
        {
            surecSheet.Cell(surecRow, 1).Value = item.ProjectPeriod;
            surecSheet.Cell(surecRow, 2).Value = item.PersonnelName;
            surecSheet.Cell(surecRow, 3).Value = item.Departman;
            surecSheet.Cell(surecRow, 4).Value = item.KontrolSorusu;
            surecSheet.Cell(surecRow, 5).Value = item.Periyot;
            surecSheet.Cell(surecRow, 6).Value = item.PeriyotAy;
            surecSheet.Cell(surecRow, 7).Value = (double)item.OrtalamaPuan;
            surecSheet.Cell(surecRow, 8).Value = item.HataSayisi;
            surecRow++;
        }

        surecSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(surecSheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (stream.ToArray(), $"SurecAnalizi_{TurkeyTime.Now:dd.MM.yyyyHHmmss}.xlsx");
    }

    // ===== ADMIN CUSTOMER SELECTION =====

    public async Task<object> GetCustomersForAdminAsync(string? search, bool includeInactive)
    {
        var query = _context.Customers.Where(c => !c.IsDeleted);

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c =>
                c.CompanyName.ToLower().Contains(searchLower) ||
                (c.Code != null && c.Code.ToLower().Contains(searchLower)));
        }

        var customers = await query
            .OrderBy(c => c.CompanyName)
            .Select(c => new
            {
                c.Id,
                c.CompanyName,
                c.Code,
                c.IsActive
            })
            .ToListAsync();

        return customers;
    }

    public async Task<(int Id, string CompanyName, string? Code)?> GetCustomerByIdAsync(int customerId)
    {
        var customer = await _context.Customers
            .Where(c => c.Id == customerId && !c.IsDeleted)
            .Select(c => new { c.Id, c.CompanyName, c.Code })
            .FirstOrDefaultAsync();

        if (customer == null)
            return null;

        return (customer.Id, customer.CompanyName, customer.Code);
    }

    // ===== ROLE-BASED ACCESS HELPERS =====

    public async Task<List<int>?> GetAllowedPersonnelIdsAsync(string? role, int? personnelId)
    {
        // CustomerManager tüm personeli görebilir
        if (role == "Admin" || role == "CustomerManager")
            return null;

        // CustomerSupervisor - Organizasyon bazında hibrit kontrol
        if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            // 1. Süpervizörün atandığı organizasyonları bul
            var myOrgIds = await _context.CustomerPersonnelOrganizations
                .Where(cpo => cpo.CustomerPersonnelId == personnelId.Value && !cpo.IsDeleted)
                .Select(cpo => cpo.CustomerOrganizationId)
                .Distinct()
                .ToListAsync();

            // Hiçbir organizasyona atanmamış → TÜM veriyi görebilir (null = filtre yok)
            if (!myOrgIds.Any())
                return null;

            // 2. Bu organizasyonlarda süpervizör olduğu personeller
            var supervisedPersonnel = await _context.CustomerPersonnelOrganizations
                .Where(cpo => myOrgIds.Contains(cpo.CustomerOrganizationId) &&
                             cpo.SupervisorId == personnelId.Value &&
                             !cpo.IsDeleted)
                .Select(cpo => new { cpo.CustomerOrganizationId, cpo.CustomerPersonnelId })
                .ToListAsync();

            // 3. Hangi organizasyonlarda altında personel var?
            var orgsWithTeam = supervisedPersonnel
                .Select(x => x.CustomerOrganizationId)
                .Distinct()
                .ToHashSet();

            // 4. Altında personel olmayan organizasyonlar
            var orgsWithoutTeam = myOrgIds.Except(orgsWithTeam).ToList();

            var result = new HashSet<int>();

            // Altında personel olan org'lardan sadece o personeller
            foreach (var p in supervisedPersonnel)
                result.Add(p.CustomerPersonnelId);

            // Altında personel olmayan org'lardan TÜM personeller
            if (orgsWithoutTeam.Any())
            {
                var allPersonnelInEmptyOrgs = await _context.CustomerPersonnelOrganizations
                    .Where(cpo => orgsWithoutTeam.Contains(cpo.CustomerOrganizationId) && !cpo.IsDeleted)
                    .Select(cpo => cpo.CustomerPersonnelId)
                    .Distinct()
                    .ToListAsync();

                foreach (var id in allPersonnelInEmptyOrgs)
                    result.Add(id);
            }

            // Kendisini de ekle
            result.Add(personnelId.Value);

            return result.ToList();
        }

        // CustomerOperator sadece kendini görebilir
        if (personnelId.HasValue)
            return new List<int> { personnelId.Value };

        return new List<int>(); // Hiçbir şey göremez
    }

    public async Task<List<int>?> GetAllowedOrganizationIdsAsync(string? role, int? personnelId)
    {
        // Admin ve CustomerManager tüm organizasyonları görebilir
        if (role == "Admin" || role == "CustomerManager")
            return null;

        // CustomerSupervisor - Organizasyon bazında kontrol
        if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            // Süpervizörün atandığı organizasyonları bul
            var myOrgIds = await _context.CustomerPersonnelOrganizations
                .Where(cpo => cpo.CustomerPersonnelId == personnelId.Value && !cpo.IsDeleted)
                .Select(cpo => cpo.CustomerOrganizationId)
                .Distinct()
                .ToListAsync();

            // Hiçbir organizasyona atanmamış → TÜM organizasyonları görebilir
            if (!myOrgIds.Any())
                return null;

            // Atandığı organizasyonları döndür
            return myOrgIds;
        }

        // CustomerOperator - Kendi organizasyonlarını görebilir
        if (role == "CustomerOperator" && personnelId.HasValue)
        {
            var myOrgIds = await _context.CustomerPersonnelOrganizations
                .Where(cpo => cpo.CustomerPersonnelId == personnelId.Value && !cpo.IsDeleted)
                .Select(cpo => cpo.CustomerOrganizationId)
                .Distinct()
                .ToListAsync();

            return myOrgIds.Any() ? myOrgIds : new List<int>();
        }

        return new List<int>(); // Hiçbir şey göremez
    }

    // ===== GÖLGE MÜŞTERİ =====

    public async Task<object> GetGmAramalarAsync(int customerId, int? donemId)
    {
        var query = _context.GmAtamalar
            .Include(a => a.GmDonemSoru)
                .ThenInclude(ds => ds!.GmHedefFirma)
            .Include(a => a.GmDonem)
            .Where(a => !a.IsDeleted
                && a.DurumId == GmAtamaDurumlari.Ids.Tamamlandi
                && a.GmDonemSoru != null
                && a.GmDonemSoru.CustomerId == customerId)
            .AsQueryable();

        if (donemId.HasValue)
            query = query.Where(a => a.GmDonemId == donemId.Value);

        var result = await query
            .OrderByDescending(a => a.GerceklesmeTarihi)
            .Select(a => new
            {
                a.Id,
                a.GerceklesmeTarihi,
                a.AramaSaati,
                SoruMetni = a.GmDonemSoru!.SoruMetni,
                BeklenenCevap = a.GmDonemSoru.BeklenenCevap,
                HedefFirmaAdi = a.GmDonemSoru.GmHedefFirma != null ? a.GmDonemSoru.GmHedefFirma.FirmaAdi : null,
                Not = a.Not,
                DonemAdi = a.GmDonem != null ? a.GmDonem.Ad : null,
                KuponKodu = a.KuponKodu,
                IsKuponlu = a.GmDonemSoru.IsKuponlu
            })
            .ToListAsync();

        return result;
    }

    public async Task<object> GetGmDonemlerAsync(int customerId)
    {
        // Müşterinin sorularının olduğu dönemler
        var donemler = await _context.GmDonemler
            .Where(d => !d.IsDeleted
                && d.Sorular.Any(ds => ds.CustomerId == customerId && !ds.IsDeleted))
            .OrderByDescending(d => d.BaslangicTarihi)
            .Select(d => new { d.Id, d.Ad })
            .ToListAsync();

        return donemler;
    }
}
