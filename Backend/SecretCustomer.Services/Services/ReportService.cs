using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using NPOI.XWPF.UserModel;
using SecretCustomer.Core.DTOs.Auth;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILocalizationService _localizationService;
    private readonly IPerformanceSettingsService _performanceSettingsService;

    public ReportService(ApplicationDbContext context, ILocalizationService localizationService, IPerformanceSettingsService performanceSettingsService)
    {
        _context = context;
        _localizationService = localizationService;
        _performanceSettingsService = performanceSettingsService;
    }

    public async Task<PagedReportResult<EvaluationReportDto>> GetEvaluationsAsync(ReportFilterDto filter)
    {

        // Liste görünümü için Answers dahil EDİLMEZ - sadece detay sayfasında lazım
        // AsNoTracking: Read-only sorgu, performans için change tracking kapalı
        // AsSplitQuery: Çok fazla Include olduğunda tek büyük sorgu yerine ayrı sorgular
        var query = _context.Evaluations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Include(e => e.Evaluator)
            .Include(e => e.EvaluatorCustomerPersonnel)
            .Include(e => e.EvaluatedPersonnel)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.OrganizationAssignments)
                    .ThenInclude(oa => oa.Supervisor)
            .Include(e => e.AssignmentPeriod)
            .AsQueryable();

        // ===== ÇOKLU DEĞER DESTEKLİ FİLTRELER (OR mantığı) =====

        // Project filter (çoklu)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));

        // Project Type filter (çoklu)
        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Assignment.Project.ProjectTypeId));
        }
        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Evaluator filter (çoklu)
        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(e => filter.ChecklistIds.Contains(e.Assignment.ChecklistId));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            // Her tarih aralığı için OR koşulu oluştur
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            // En geniş aralığı bul (OR mantığı için)
            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(e => e.CompletedAt >= minStart || e.CreatedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(e => e.CompletedAt <= maxEnd || e.CreatedAt <= maxEnd);
        }

        // Status filter (çoklu)
        if (filter.Statuses?.Any() == true)
        {
            var statusIds = filter.Statuses
                .Select(s => EvaluationStatuses.GetBySystemName(s))
                .Where(s => s != null)
                .Select(s => s!.Id)
                .ToList();
            if (statusIds.Any())
                query = query.Where(e => statusIds.Contains(e.StatusId));
        }
        // PRENSIP: Varsayılan olarak sadece Completed değerlendirmeler dahil edilir (taslaklar hariç)
        else
        {
            query = query.Where(e => e.StatusId == EvaluationStatuses.Ids.Completed);
        }

        // Evaluation source filter (çoklu)
        if (filter.EvaluationSources?.Any() == true)
        {
            var hasInternal = filter.EvaluationSources.Contains("internal");
            var hasOurs = filter.EvaluationSources.Contains("ours");

            if (hasInternal && !hasOurs)
                query = query.Where(e => e.Assignment.TypeId == AssignmentTypes.Ids.CustomerPersonnel);
            else if (hasOurs && !hasInternal)
                query = query.Where(e => e.Assignment.TypeId != AssignmentTypes.Ids.CustomerPersonnel);
            // Eğer ikisi de seçiliyse filtre uygulanmaz (tümü gelir)
        }

        // Customer filter (çoklu)
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                filter.CustomerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerIds?.Any() == true)
            query = query.Where(e => e.Assignment.Project.CustomerId.HasValue && filter.ProjectCustomerIds.Contains(e.Assignment.Project.CustomerId.Value));

        // Organization filter (çoklu)
        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa =>
                    filter.OrganizationIds.Contains(oa.CustomerOrganizationId)));

        // Period filter (çoklu)
        if (filter.PeriodIds?.Any() == true)
            query = query.Where(e => e.AssignmentPeriodId.HasValue && filter.PeriodIds.Contains(e.AssignmentPeriodId.Value));

        // Evaluated Personnel name search (çoklu - OR mantığı)
        if (filter.PersonnelNames?.Any() == true)
        {
            query = query.Where(e =>
                filter.PersonnelNames.Any(name =>
                    (e.EvaluatedCustomerPersonnel != null &&
                        (EF.Functions.ILike(e.EvaluatedCustomerPersonnel.FirstName, $"%{name}%") ||
                         EF.Functions.ILike(e.EvaluatedCustomerPersonnel.LastName, $"%{name}%"))) ||
                    (e.EvaluatedUnknownPersonnel != null && EF.Functions.ILike(e.EvaluatedUnknownPersonnel, $"%{name}%"))));
        }

        // Supervisor name search (çoklu - OR mantığı)
        if (filter.SupervisorNames?.Any() == true)
        {
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa =>
                    oa.Supervisor != null &&
                    filter.SupervisorNames.Any(name =>
                        EF.Functions.ILike(oa.Supervisor.FirstName, $"%{name}%") ||
                        EF.Functions.ILike(oa.Supervisor.LastName, $"%{name}%"))));
        }

        // CallId search (çoklu - OR mantığı)
        if (filter.CallIds?.Any() == true)
            query = query.Where(e => e.CallId != null &&
                filter.CallIds.Any(callId => EF.Functions.ILike(e.CallId, $"%{callId}%")));

        // Get total count (skip if requested for faster initial load)
        var totalCount = filter.SkipCount ? -1 : await query.CountAsync();

        // Apply sorting
        var isAsc = filter.SortDirection?.ToLower() == "asc";
        query = filter.SortField?.ToLower() switch
        {
            // ID = Primary Key, en hızlı sıralama (varsayılan)
            "id" => isAsc
                ? query.OrderBy(e => e.Id)
                : query.OrderByDescending(e => e.Id),
            "projectname" => isAsc
                ? query.OrderBy(e => e.Assignment.Project!.Name)
                : query.OrderByDescending(e => e.Assignment.Project!.Name),
            "periodname" => isAsc
                ? query.OrderBy(e => e.AssignmentPeriod != null ? e.AssignmentPeriod.StartDate : (DateTime?)null)
                : query.OrderByDescending(e => e.AssignmentPeriod != null ? e.AssignmentPeriod.StartDate : (DateTime?)null),
            "evaluatedpersonnelname" => isAsc
                ? query.OrderBy(e => e.EvaluatedCustomerPersonnel != null ? e.EvaluatedCustomerPersonnel.FirstName : e.EvaluatedUnknownPersonnel)
                : query.OrderByDescending(e => e.EvaluatedCustomerPersonnel != null ? e.EvaluatedCustomerPersonnel.FirstName : e.EvaluatedUnknownPersonnel),
            // supervisorname sorting is complex, fallback to id
            "supervisorname" => query.OrderByDescending(e => e.Id),
            "callid" => isAsc
                ? query.OrderBy(e => e.CallId)
                : query.OrderByDescending(e => e.CallId),
            "evaluatorname" => isAsc
                ? query.OrderBy(e => e.Evaluator != null ? e.Evaluator.FirstName : "")
                : query.OrderByDescending(e => e.Evaluator != null ? e.Evaluator.FirstName : ""),
            "calldate" => isAsc
                ? query.OrderBy(e => e.CallDate ?? e.CompletedAt)
                : query.OrderByDescending(e => e.CallDate ?? e.CompletedAt),
            "duration" => isAsc
                ? query.OrderBy(e => e.Duration)
                : query.OrderByDescending(e => e.Duration),
            "scorepercentage" => isAsc
                ? query.OrderBy(e => e.ScorePercentage)
                : query.OrderByDescending(e => e.ScorePercentage),
            "status" => isAsc
                ? query.OrderBy(e => e.StatusId)
                : query.OrderByDescending(e => e.StatusId),
            // Varsayılan: ID DESC (Primary Key, en hızlı)
            _ => query.OrderByDescending(e => e.Id)
        };

        // Apply pagination
        var evaluations = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        // Map to DTOs
        var items = evaluations.Select(e => MapToReportDto(e)).ToList();

        return new PagedReportResult<EvaluationReportDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<int> GetEvaluationsCountAsync(ReportFilterDto filter)
    {
        // Count için Include'lar gereksiz - sadece filtreleme yeterli
        // AsNoTracking: Count için tracking gereksiz
        var query = _context.Evaluations.AsNoTracking().AsQueryable();

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));

        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Assignment.Project.ProjectTypeId));
        }
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(e => filter.ChecklistIds.Contains(e.Assignment.ChecklistId));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(e => e.CompletedAt >= minStart || e.CreatedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(e => e.CompletedAt <= maxEnd || e.CreatedAt <= maxEnd);
        }

        // Status filter (çoklu)
        if (filter.Statuses?.Any() == true)
        {
            var statusIds = filter.Statuses
                .Select(s => EvaluationStatuses.GetBySystemName(s))
                .Where(s => s != null)
                .Select(s => s!.Id)
                .ToList();
            if (statusIds.Any())
                query = query.Where(e => statusIds.Contains(e.StatusId));
        }

        // Evaluation source filter (çoklu)
        if (filter.EvaluationSources?.Any() == true)
        {
            var hasInternal = filter.EvaluationSources.Contains("internal");
            var hasOurs = filter.EvaluationSources.Contains("ours");

            if (hasInternal && !hasOurs)
                query = query.Where(e => e.Assignment.TypeId == AssignmentTypes.Ids.CustomerPersonnel);
            else if (hasOurs && !hasInternal)
                query = query.Where(e => e.Assignment.TypeId != AssignmentTypes.Ids.CustomerPersonnel);
        }

        if (filter.CustomerIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                filter.CustomerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

        if (filter.ProjectCustomerIds?.Any() == true)
            query = query.Where(e => e.Assignment.Project.CustomerId.HasValue &&
                filter.ProjectCustomerIds.Contains(e.Assignment.Project.CustomerId.Value));

        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa =>
                    filter.OrganizationIds.Contains(oa.CustomerOrganizationId)));

        if (filter.PeriodIds?.Any() == true)
            query = query.Where(e => e.AssignmentPeriodId.HasValue && filter.PeriodIds.Contains(e.AssignmentPeriodId.Value));

        // Personnel name search (çoklu - OR mantığı)
        if (filter.PersonnelNames?.Any() == true)
        {
            query = query.Where(e =>
                filter.PersonnelNames.Any(name =>
                    (e.EvaluatedCustomerPersonnel != null &&
                        (EF.Functions.ILike(e.EvaluatedCustomerPersonnel.FirstName, $"%{name}%") ||
                         EF.Functions.ILike(e.EvaluatedCustomerPersonnel.LastName, $"%{name}%"))) ||
                    (e.EvaluatedUnknownPersonnel != null && EF.Functions.ILike(e.EvaluatedUnknownPersonnel, $"%{name}%"))));
        }

        // Supervisor name search (çoklu - OR mantığı)
        if (filter.SupervisorNames?.Any() == true)
        {
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa =>
                    oa.Supervisor != null &&
                    filter.SupervisorNames.Any(name =>
                        EF.Functions.ILike(oa.Supervisor.FirstName, $"%{name}%") ||
                        EF.Functions.ILike(oa.Supervisor.LastName, $"%{name}%"))));
        }

        // CallId search (çoklu - OR mantığı)
        if (filter.CallIds?.Any() == true)
            query = query.Where(e => e.CallId != null &&
                filter.CallIds.Any(callId => EF.Functions.ILike(e.CallId, $"%{callId}%")));

        return await query.CountAsync();
    }

    public async Task<EvaluationDetailReportDto?> GetEvaluationDetailAsync(int evaluationId)
    {
        var evaluation = await _context.Evaluations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Include(e => e.Evaluator)
            .Include(e => e.EvaluatorCustomerPersonnel)
            .Include(e => e.EvaluatedPersonnel)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.Customer)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.OrganizationAssignments)
                    .ThenInclude(oa => oa.CustomerOrganization)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.OrganizationAssignments)
                    .ThenInclude(oa => oa.Supervisor)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .FirstOrDefaultAsync(e => e.Id == evaluationId);

        if (evaluation == null)
            return null;

        var dto = new EvaluationDetailReportDto
        {
            EvaluationId = evaluation.Id,
            AssignmentId = evaluation.AssignmentId,
            ProjectName = evaluation.Assignment.Project?.Name ?? "",
            ProjectCode = evaluation.Assignment.Project?.Code,
            ChecklistName = evaluation.Assignment.Checklist?.Name ?? "",
            EvaluatorName = evaluation.Evaluator != null
                ? $"{evaluation.Evaluator.FirstName} {evaluation.Evaluator.LastName}"
                : (evaluation.EvaluatorCustomerPersonnel != null
                    ? $"{evaluation.EvaluatorCustomerPersonnel.FirstName} {evaluation.EvaluatorCustomerPersonnel.LastName}"
                    : null),
            EvaluatedPersonnelName = evaluation.EvaluatedCustomerPersonnel != null
                ? $"{evaluation.EvaluatedCustomerPersonnel.FirstName} {evaluation.EvaluatedCustomerPersonnel.LastName}"
                : (evaluation.EvaluatedPersonnel != null
                    ? $"{evaluation.EvaluatedPersonnel.FirstName} {evaluation.EvaluatedPersonnel.LastName}"
                    : evaluation.EvaluatedUnknownPersonnel),
            CustomerName = evaluation.EvaluatedCustomerPersonnel?.Customer?.CompanyName,
            OrganizationName = evaluation.EvaluatedCustomerPersonnel?.OrganizationAssignments != null
                ? string.Join(", ", evaluation.EvaluatedCustomerPersonnel.OrganizationAssignments
                    .Where(oa => oa.CustomerOrganization != null)
                    .Select(oa => oa.CustomerOrganization!.Name))
                : null,
            SupervisorName = evaluation.EvaluatedCustomerPersonnel?.OrganizationAssignments != null
                ? string.Join(", ", evaluation.EvaluatedCustomerPersonnel.OrganizationAssignments
                    .Where(oa => oa.Supervisor != null)
                    .Select(oa => $"{oa.Supervisor!.FirstName} {oa.Supervisor.LastName}")
                    .Distinct())
                : null,
            EvaluationDate = evaluation.ControlDate ?? evaluation.CompletedAt,
            CompletedAt = evaluation.CompletedAt,
            CreatedAt = evaluation.CreatedAt,
            DueDate = evaluation.Assignment.DueDate,
            TotalScore = evaluation.TotalScore,
            MaxScore = evaluation.MaxScore,
            ScorePercentage = evaluation.ScorePercentage,
            YellowCardCount = evaluation.YellowCardCount,
            RedCardCount = evaluation.RedCardCount,
            Status = EvaluationStatuses.GetById(evaluation.StatusId)?.SystemName ?? "",
            CallId = evaluation.CallId,
            CallDate = evaluation.CallDate,
            Duration = evaluation.Duration,
            Comment = evaluation.EvaluationComment,
            Groups = evaluation.Answers
                .Where(a => a.Question?.GroupName != null)
                .GroupBy(a => a.Question!.GroupName!)
                .OrderBy(g => g.Key)
                .Select((g, index) => new QuestionGroupReportDto
                {
                    GroupName = g.Key,
                    Order = index + 1,
                    GroupScore = g.Sum(a => a.EarnedPoints ?? 0),
                    GroupMaxScore = g.Sum(a => a.Question!.WeightPoints),
                    Questions = g.OrderBy(a => a.Question!.Order).Select(a => new QuestionAnswerReportDto
                    {
                        QuestionText = a.Question!.Text,
                        Order = a.Question.Order,
                        AnswerText = a.AnswerText,
                        AnswerNumeric = a.AnswerNumeric,
                        GivenPoints = a.EarnedPoints,
                        MaxPoints = a.Question.WeightPoints,
                        PenaltyType = PenaltyTypes.GetById(a.AppliedPenaltyTypeId)?.SystemName ?? "None",
                        Notes = a.Notes,
                        SelectedSubCriteria = a.SubCriteriaSelections
                            .Select(s => s.SubCriteria.Description)
                            .ToList()
                    }).ToList()
                }).ToList(),

            // Düz liste halinde tüm soru-cevaplar (Online Anket detayı için)
            // Score: Her zaman hesaplanmış ağırlıklı puan (EarnedPoints)
            // MaxPoints: Sorunun ağırlık puanı (WeightPoints)
            QuestionAnswers = evaluation.Answers
                .Where(a => a.Question != null)
                .OrderBy(a => a.Question!.GroupName ?? "")
                .ThenBy(a => a.Question!.Order)
                .Select(a => new FlatQuestionAnswerDto
                {
                    QuestionText = a.Question!.Text,
                    GroupName = a.Question.GroupName,
                    Order = a.Question.Order,
                    Score = a.Question.ShowScoreInput ? a.AnswerNumeric : a.EarnedPoints,
                    MaxPoints = a.Question.ShowScoreInput ? a.Question.MaxPoints : a.Question.WeightPoints,
                    SelectedSubCriteria = a.SubCriteriaSelections
                        .Select(s => s.SubCriteria.Description)
                        .ToList(),
                    Comment = a.Notes
                }).ToList(),

            // Dinleme detay modalı için düz cevap listesi
            Answers = evaluation.Answers
                .Where(a => a.Question != null)
                .OrderBy(a => a.Question!.GroupName ?? "")
                .ThenBy(a => a.Question!.Order)
                .Select(a => new EvaluationAnswerDto
                {
                    GroupName = a.Question!.GroupName,
                    QuestionText = a.Question.Text,
                    GivenPoints = a.EarnedPoints,
                    MaxPoints = a.Question.WeightPoints,
                    AppliedPenaltyType = PenaltyTypes.GetById(a.AppliedPenaltyTypeId)?.SystemName,
                    Notes = a.Notes,
                    // View uyumluluğu için ek alanlar
                    AnswerNumeric = a.AnswerNumeric,
                    AnswerText = a.AnswerText,
                    EarnedPoints = a.EarnedPoints,
                    QuestionMaxPoints = a.Question.WeightPoints,
                    PenaltyType = PenaltyTypes.GetById(a.AppliedPenaltyTypeId)?.SystemName
                }).ToList(),

            // Değerlendirme yorumu
            EvaluationComment = evaluation.EvaluationComment,

            // Genel notlar (şimdilik null - entity'de yoksa)
            Notes = null
        };

        return dto;
    }

    public async Task<ExcelExportDto?> ExportEvaluationDetailToExcelAsync(int evaluationId, bool excludeEvaluatorInfo = false)
    {
        var detail = await GetEvaluationDetailAsync(evaluationId);
        if (detail == null)
            return null;

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Değerlendirme Detayı");

        int row = 1;

        // ===== BÖLÜMLER YAN YANA =====
        // Başlık satırı
        worksheet.Cell(row, 1).Value = "Değerlendirilen Bilgileri";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.Range(row, 1, row, 2).Merge();

        worksheet.Cell(row, 3).Value = "Çağrı Bilgileri";
        worksheet.Cell(row, 3).Style.Font.Bold = true;
        worksheet.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.Range(row, 3, row, 4).Merge();

        // Değerlendirme Bilgileri başlığı - sadece Listenings için göster
        if (!excludeEvaluatorInfo)
        {
            worksheet.Cell(row, 5).Value = "Değerlendirme Bilgileri";
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Range(row, 5, row, 6).Merge();
        }
        row++;

        // Satır 1
        worksheet.Cell(row, 1).Value = "Müşteri";
        worksheet.Cell(row, 2).Value = detail.CustomerName ?? "-";
        worksheet.Cell(row, 3).Value = "Çağrı ID";
        worksheet.Cell(row, 4).Value = detail.CallId ?? "-";
        if (!excludeEvaluatorInfo)
        {
            worksheet.Cell(row, 5).Value = "Değerlendiren";
            worksheet.Cell(row, 6).Value = detail.EvaluatorName ?? "-";
        }
        else
        {
            // CustomerPortal için sadece Puan göster (bold label)
            worksheet.Cell(row, 5).Value = "Puan:";
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = detail.ScorePercentage.HasValue ? $"%{detail.ScorePercentage:F1}" : "-";
        }
        row++;

        // Satır 2
        worksheet.Cell(row, 1).Value = "Organizasyon";
        worksheet.Cell(row, 2).Value = detail.OrganizationName ?? "-";
        worksheet.Cell(row, 3).Value = "Çağrı Tarihi";
        worksheet.Cell(row, 4).Value = detail.CallDate?.ToString("dd.MM.yyyy") ?? "-";
        if (!excludeEvaluatorInfo)
        {
            worksheet.Cell(row, 5).Value = "Değerlendirme Tarihi";
            worksheet.Cell(row, 6).Value = detail.EvaluationDate?.ToString("dd.MM.yyyy") ?? "-";
        }
        row++;

        // Satır 3
        worksheet.Cell(row, 1).Value = "Değerlendirilen";
        worksheet.Cell(row, 2).Value = detail.EvaluatedPersonnelName ?? "-";
        worksheet.Cell(row, 3).Value = "Süre";
        worksheet.Cell(row, 4).Value = detail.Duration ?? "-";
        if (!excludeEvaluatorInfo)
        {
            worksheet.Cell(row, 5).Value = "Puan";
            worksheet.Cell(row, 6).Value = detail.ScorePercentage.HasValue ? $"%{detail.ScorePercentage:F1}" : "-";
        }
        row++;

        row++; // Boş satır

        // ===== SORU DETAYLARI TABLOSU =====
        var tableHeaders = new[] { "Grup", "Soru", "Cevap", "Ağırlık", "Kazanılan", "Not" };
        for (int i = 0; i < tableHeaders.Length; i++)
        {
            worksheet.Cell(row, i + 1).Value = tableHeaders[i];
            worksheet.Cell(row, i + 1).Style.Font.Bold = true;
            worksheet.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            worksheet.Cell(row, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        row++;

        // Tüm soruları düzleştir (flatten)
        var allQuestions = new List<(string GroupName, QuestionAnswerReportDto Question)>();
        foreach (var group in detail.Groups)
        {
            foreach (var question in group.Questions)
            {
                allQuestions.Add((group.GroupName, question));
            }
        }

        // Soruları listele
        string previousGroup = "";
        for (int i = 0; i < allQuestions.Count; i++)
        {
            var (groupName, question) = allQuestions[i];

            // Grup: Önceki satırla aynıysa boş, farklıysa göster
            var showGroup = groupName != previousGroup;
            worksheet.Cell(row, 1).Value = showGroup ? groupName : "";
            previousGroup = groupName;

            // Soru (alt kriterler varsa altına ekle)
            var questionText = question.QuestionText ?? "";
            if (question.SelectedSubCriteria != null && question.SelectedSubCriteria.Count > 0)
            {
                questionText += "\n  → " + string.Join("\n  → ", question.SelectedSubCriteria);
            }
            worksheet.Cell(row, 2).Value = questionText;
            worksheet.Cell(row, 2).Style.Alignment.WrapText = true;

            // Cevap: Kazanılan/Ağırlık formatında
            var maxPts = question.MaxPoints ?? 0;
            var earnedPts = question.GivenPoints ?? 0;
            string answerDisplay;
            if (maxPts > 0)
                answerDisplay = $"{earnedPts:F0}/{maxPts:F0}";
            else
                answerDisplay = "-";
            worksheet.Cell(row, 3).Value = answerDisplay;

            // Ağırlık
            worksheet.Cell(row, 4).Value = maxPts > 0 ? maxPts : (decimal?)null;

            // Kazanılan
            worksheet.Cell(row, 5).Value = maxPts > 0 ? earnedPts : (decimal?)null;

            // Not (ceza varsa başına emoji ekle)
            var noteText = question.Notes ?? "";
            if (!string.IsNullOrEmpty(question.PenaltyType) && question.PenaltyType != "None")
            {
                var penaltyLabel = question.PenaltyType == "YellowCard" ? "[Sarı Kart] " :
                                   question.PenaltyType == "RedCard" ? "[Kırmızı Kart] " : "";
                noteText = penaltyLabel + noteText;
            }
            worksheet.Cell(row, 6).Value = noteText;

            // Satır border
            for (int c = 1; c <= 6; c++)
                worksheet.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            row++;
        }

        row++; // Boş satır

        // ===== GENEL YORUM =====
        worksheet.Cell(row, 1).Value = "Genel Yorum";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        row++;
        worksheet.Cell(row, 1).Value = detail.Comment ?? "-";
        worksheet.Range(row, 1, row, 6).Merge();

        // Sütun genişlikleri
        worksheet.Column(1).Width = 25; // Grup
        worksheet.Column(2).Width = 40; // Soru
        worksheet.Column(3).Width = 12; // Cevap
        worksheet.Column(4).Width = 10; // Ağırlık
        worksheet.Column(5).Width = 12; // Kazanılan
        worksheet.Column(6).Width = 35; // Not

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Degerlendirme_Detay_{evaluationId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<SummaryReportDto> GetSummaryReportAsync(ReportFilterDto filter)
    {
        // Base query - Include kullanmadan, sadece filtreleme için
        var query = _context.Evaluations
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));

        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Assignment.Project.ProjectTypeId));
        }
        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(e => e.CompletedAt >= minStart || e.CreatedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(e => e.CompletedAt <= maxEnd || e.CreatedAt <= maxEnd);
        }

        // Veritabanında aggregate - memory'ye çekmeden
        var totalCount = await query.CountAsync();
        var completedCount = await query.CountAsync(e => e.StatusId == EvaluationStatuses.Ids.Completed);
        var pendingCount = await query.CountAsync(e =>
            e.StatusId == EvaluationStatuses.Ids.Draft ||
            e.StatusId == EvaluationStatuses.Ids.InProgress ||
            e.StatusId == EvaluationStatuses.Ids.Pending);

        // Completed evaluations için aggregate sorgular
        var completedQuery = query.Where(e => e.StatusId == EvaluationStatuses.Ids.Completed && e.ScorePercentage.HasValue);

        var avgScore = await completedQuery.AnyAsync()
            ? Math.Round(await completedQuery.AverageAsync(e => e.ScorePercentage ?? 0), 2)
            : 0;
        var minScore = await completedQuery.AnyAsync()
            ? await completedQuery.MinAsync(e => e.ScorePercentage ?? 0)
            : 0;
        var maxScore = await completedQuery.AnyAsync()
            ? await completedQuery.MaxAsync(e => e.ScorePercentage ?? 0)
            : 0;

        var totalYellowCards = await query.SumAsync(e => e.YellowCardCount);
        var totalRedCards = await query.SumAsync(e => e.RedCardCount);

        // Project summaries - veritabanında group by
        var projectSummaries = await query
            .Where(e => e.Assignment.Project != null)
            .GroupBy(e => new { e.Assignment.Project!.Id, e.Assignment.Project.Name })
            .Select(g => new ProjectSummaryReportDto
            {
                ProjectId = g.Key.Id,
                ProjectName = g.Key.Name,
                EvaluationCount = g.Count(),
                AverageScore = g.Where(e => e.ScorePercentage.HasValue).Any()
                    ? Math.Round(g.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage!.Value), 2)
                    : 0
            })
            .OrderByDescending(p => p.EvaluationCount)
            .Take(20) // En çok değerlendirilen 20 proje
            .ToListAsync();

        // Evaluator summaries - veritabanında group by
        var evaluatorSummaries = await query
            .Where(e => e.EvaluatorId != null)
            .GroupBy(e => new { e.Evaluator!.Id, e.Evaluator.FirstName, e.Evaluator.LastName })
            .Select(g => new EvaluatorSummaryReportDto
            {
                EvaluatorId = g.Key.Id,
                EvaluatorName = g.Key.FirstName + " " + g.Key.LastName,
                EvaluationCount = g.Count(),
                AverageScore = g.Where(e => e.ScorePercentage.HasValue).Any()
                    ? Math.Round(g.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage!.Value), 2)
                    : 0
            })
            .OrderByDescending(ev => ev.EvaluationCount)
            .Take(20) // En çok değerlendirme yapan 20 kişi
            .ToListAsync();

        return new SummaryReportDto
        {
            TotalEvaluations = totalCount,
            CompletedEvaluations = completedCount,
            PendingEvaluations = pendingCount,
            AverageScore = avgScore,
            MinScore = minScore,
            MaxScore = maxScore,
            TotalYellowCards = totalYellowCards,
            TotalRedCards = totalRedCards,
            ProjectSummaries = projectSummaries,
            EvaluatorSummaries = evaluatorSummaries
        };
    }

    public async Task<ExcelExportDto> ExportEvaluationsToExcelAsync(ReportFilterDto filter)
    {
        // Remove pagination for export
        filter.Page = 1;
        filter.PageSize = 10000;
        filter.ForExport = true; // PRENSIP: Taslaklar rapora dahil edilmez

        var result = await GetEvaluationsAsync(filter);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.Evaluations", defaultValue: "Değerlendirmeler"));

        // Headers
        var headers = new[]
        {
            await _localizationService.GetResourceAsync("Report.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.ProjectCode", defaultValue: "Proje Kodu"),
            await _localizationService.GetResourceAsync("Report.Checklist", defaultValue: "Kontrol Listesi"),
            await _localizationService.GetResourceAsync("Report.Evaluator", defaultValue: "Değerlendirici"),
            await _localizationService.GetResourceAsync("Report.EvaluatedPersonnel", defaultValue: "Değerlendirilen Personel"),
            await _localizationService.GetResourceAsync("Report.EvaluationDate", defaultValue: "Değerlendirme Tarihi"),
            await _localizationService.GetResourceAsync("Report.CompletedDate", defaultValue: "Tamamlanma Tarihi"),
            await _localizationService.GetResourceAsync("Report.DueDate", defaultValue: "Son Tarih"),
            await _localizationService.GetResourceAsync("Report.Score", defaultValue: "Puan"),
            await _localizationService.GetResourceAsync("Report.MaxScore", defaultValue: "Maks Puan"),
            await _localizationService.GetResourceAsync("Report.Percentage", defaultValue: "Yüzde"),
            await _localizationService.GetResourceAsync("Report.YellowCard", defaultValue: "Sarı Kart"),
            await _localizationService.GetResourceAsync("Report.RedCard", defaultValue: "Kırmızı Kart"),
            await _localizationService.GetResourceAsync("Report.Status", defaultValue: "Durum"),
            await _localizationService.GetResourceAsync("Report.CallId", defaultValue: "Çağrı ID"),
            await _localizationService.GetResourceAsync("Report.CallDate", defaultValue: "Çağrı Tarihi"),
            await _localizationService.GetResourceAsync("Report.Duration", defaultValue: "Süre (dk)"),
            await _localizationService.GetResourceAsync("Report.Comment", defaultValue: "Yorum")
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        int row = 2;
        foreach (var item in result.Items)
        {
            worksheet.Cell(row, 1).Value = item.ProjectName;
            worksheet.Cell(row, 2).Value = item.ProjectCode ?? "";
            worksheet.Cell(row, 3).Value = item.ChecklistName;
            worksheet.Cell(row, 4).Value = item.EvaluatorName ?? "";
            worksheet.Cell(row, 5).Value = item.EvaluatedPersonnelName ?? "";
            worksheet.Cell(row, 6).Value = item.EvaluationDate?.ToString("dd.MM.yyyy HH:mm") ?? "";
            worksheet.Cell(row, 7).Value = item.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "";
            worksheet.Cell(row, 8).Value = item.DueDate.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 9).Value = item.TotalScore ?? 0;
            worksheet.Cell(row, 10).Value = item.MaxScore ?? 0;
            worksheet.Cell(row, 11).Value = item.ScorePercentage ?? 0;
            worksheet.Cell(row, 12).Value = item.YellowCardCount;
            worksheet.Cell(row, 13).Value = item.RedCardCount;
            worksheet.Cell(row, 14).Value = item.Status;
            worksheet.Cell(row, 15).Value = item.CallId ?? "";
            worksheet.Cell(row, 16).Value = item.CallDate?.ToString("dd.MM.yyyy HH:mm") ?? "";
            worksheet.Cell(row, 17).Value = item.Duration ?? "";
            worksheet.Cell(row, 18).Value = item.Comment ?? "";
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet, callIdColumns: new[] { 15 }, noteColumns: new[] { 18 });

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Degerlendirmeler_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<ExcelExportDto> ExportDetailedEvaluationsToExcelAsync(ReportFilterDto filter)
    {
        // Get evaluations with details
        // PRENSIP: Taslaklar rapora dahil edilmez
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Include(e => e.Evaluator)
            .Include(e => e.EvaluatedPersonnel)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));

        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Assignment.Project.ProjectTypeId));
        }
        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(e => e.CompletedAt >= minStart || e.CreatedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(e => e.CompletedAt <= maxEnd || e.CreatedAt <= maxEnd);
        }

        var evaluations = await query.Take(1000).ToListAsync();

        using var workbook = new XLWorkbook();

        // Summary sheet
        var summarySheet = workbook.Worksheets.Add("Özet");
        summarySheet.Cell(1, 1).Value = "Toplam Değerlendirme";
        summarySheet.Cell(1, 2).Value = evaluations.Count;
        summarySheet.Cell(2, 1).Value = "Rapor Tarihi";
        summarySheet.Cell(2, 2).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        // Detailed answers sheet
        var detailSheet = workbook.Worksheets.Add("Detaylı Cevaplar");

        var detailHeaders = new[]
        {
            "Değerlendirme ID", "Proje", "Şube", "Bölüm", "Soru No", "Soru",
            "Cevap", "Sayısal Cevap", "Verilen Puan", "Maks Puan",
            "Ceza Tipi", "Not"
        };

        for (int i = 0; i < detailHeaders.Length; i++)
        {
            detailSheet.Cell(1, i + 1).Value = detailHeaders[i];
            detailSheet.Cell(1, i + 1).Style.Font.Bold = true;
            detailSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int detailRow = 2;
        foreach (var evaluation in evaluations)
        {
            foreach (var answer in evaluation.Answers.OrderBy(a => a.Question?.GroupName).ThenBy(a => a.Question?.Order))
            {
                if (answer.Question == null) continue;

                detailSheet.Cell(detailRow, 1).Value = evaluation.Id.ToString();
                detailSheet.Cell(detailRow, 2).Value = evaluation.Assignment.Project?.Name ?? "";
                detailSheet.Cell(detailRow, 3).Value = "";
                detailSheet.Cell(detailRow, 4).Value = answer.Question.GroupName ?? "";
                detailSheet.Cell(detailRow, 5).Value = answer.Question.Order;
                detailSheet.Cell(detailRow, 6).Value = answer.Question.Text;
                detailSheet.Cell(detailRow, 7).Value = answer.AnswerText ?? "";
                detailSheet.Cell(detailRow, 8).Value = answer.AnswerNumeric ?? 0;
                detailSheet.Cell(detailRow, 9).Value = answer.EarnedPoints ?? 0;
                detailSheet.Cell(detailRow, 10).Value = answer.Question.WeightPoints;
                detailSheet.Cell(detailRow, 11).Value = PenaltyTypes.GetById(answer.AppliedPenaltyTypeId)?.SystemName ?? "None";
                detailSheet.Cell(detailRow, 12).Value = answer.Notes ?? "";
                detailRow++;
            }
        }

        detailSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(detailSheet, noteColumns: new[] { 12 });

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Detayli_Degerlendirmeler_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    private EvaluationReportDto MapToReportDto(Core.Entities.Evaluation evaluation)
    {
        // Tüm yorumları topla: soru notları + genel değerlendirme yorumu
        var allComments = new List<string>();

        // Soru notlarını ekle (Notes alanı dolu olanlar)
        if (evaluation.Answers != null)
        {
            var answerNotes = evaluation.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Notes))
                .Select(a => a.Notes!)
                .ToList();
            allComments.AddRange(answerNotes);
        }

        // Genel değerlendirme yorumunu ekle
        if (!string.IsNullOrWhiteSpace(evaluation.EvaluationComment))
        {
            allComments.Add(evaluation.EvaluationComment);
        }

        var combinedComment = allComments.Count > 0 ? string.Join(", ", allComments) : "-";

        return new EvaluationReportDto
        {
            EvaluationId = evaluation.Id,
            AssignmentId = evaluation.AssignmentId,
            ProjectName = evaluation.Assignment.Project?.Name ?? "",
            ProjectCode = evaluation.Assignment.Project?.Code,
            ChecklistName = evaluation.Assignment.Checklist?.Name ?? "",
            EvaluatorName = evaluation.Evaluator != null
                ? $"{evaluation.Evaluator.FirstName} {evaluation.Evaluator.LastName}"
                : (evaluation.EvaluatorCustomerPersonnel != null
                    ? $"{evaluation.EvaluatorCustomerPersonnel.FirstName} {evaluation.EvaluatorCustomerPersonnel.LastName}"
                    : null),
            EvaluatedPersonnelName = evaluation.EvaluatedCustomerPersonnel != null
                ? $"{evaluation.EvaluatedCustomerPersonnel.FirstName} {evaluation.EvaluatedCustomerPersonnel.LastName}"
                : (evaluation.EvaluatedPersonnel != null
                    ? $"{evaluation.EvaluatedPersonnel.FirstName} {evaluation.EvaluatedPersonnel.LastName}"
                    : evaluation.EvaluatedUnknownPersonnel),
            SupervisorName = evaluation.EvaluatedCustomerPersonnel?.OrganizationAssignments != null
                ? string.Join(", ", evaluation.EvaluatedCustomerPersonnel.OrganizationAssignments
                    .Where(oa => oa.Supervisor != null)
                    .Select(oa => $"{oa.Supervisor!.FirstName} {oa.Supervisor.LastName}")
                    .Distinct())
                : null,
            PeriodName = evaluation.AssignmentPeriod != null
                ? evaluation.AssignmentPeriod.StartDate.ToString("yyyyMM")
                : (evaluation.CallDate.HasValue ? FormatMonthYear(evaluation.CallDate.Value) : null),
            EvaluationDate = evaluation.ControlDate ?? evaluation.CompletedAt,
            CompletedAt = evaluation.CompletedAt,
            CreatedAt = evaluation.CreatedAt,
            DueDate = evaluation.Assignment.DueDate,
            TotalScore = evaluation.TotalScore,
            MaxScore = evaluation.MaxScore,
            ScorePercentage = evaluation.ScorePercentage,
            YellowCardCount = evaluation.YellowCardCount,
            RedCardCount = evaluation.RedCardCount,
            Status = EvaluationStatuses.GetById(evaluation.StatusId)?.SystemName ?? "",
            CallId = evaluation.CallId,
            CallDate = evaluation.CallDate,
            CallTime = evaluation.CallTime,
            Duration = evaluation.Duration,
            Comment = combinedComment
        };
    }

    // ===== CEZALI KL RAPORU =====

    public async Task<PenaltyReportResultDto> GetPenaltiesReportAsync(PenaltyFilterDto filter)
    {
        var query = _context.Answers
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Assignment)
                    .ThenInclude(a => a.Project)
                        .ThenInclude(p => p!.Customer)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Evaluator)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedCustomerPersonnel)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedOrganization)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.AssignmentPeriod)
            .Include(a => a.Question)
                .ThenInclude(q => q.Checklist)
            .Include(a => a.SubCriteriaSelections)
                .ThenInclude(s => s.SubCriteria)
            .Where(a => a.AppliedPenaltyTypeId != PenaltyTypes.Ids.None)
            .AsQueryable();

        // Varsayılan proje tipi filtresi: Çağrı Denetimi (proje filtresi yoksa)
        if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(a => a.Evaluation.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(a => filter.ProjectIds.Contains(a.Evaluation.Assignment.ProjectId));

        if (filter.CustomerIds?.Any() == true)
            query = query.Where(a => a.Evaluation.Assignment.Project.CustomerId.HasValue && filter.CustomerIds.Contains(a.Evaluation.Assignment.Project.CustomerId.Value));

        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatedOrganizationId.HasValue && filter.OrganizationIds.Contains(a.Evaluation.EvaluatedOrganizationId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(a => filter.ChecklistIds.Contains(a.Question.ChecklistId));

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(a.Evaluation.EvaluatorId.Value));

        // PenaltyType - çoklu değer desteği
        if (filter.PenaltyTypes?.Any() == true)
        {
            var penaltyTypeIds = filter.PenaltyTypes
                .Select(pt => PenaltyTypes.GetBySystemName(pt)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (penaltyTypeIds.Any())
                query = query.Where(a => penaltyTypeIds.Contains(a.AppliedPenaltyTypeId));
        }

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(a => (a.Evaluation.CompletedAt ?? a.Evaluation.ControlDate) >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(a => (a.Evaluation.CompletedAt ?? a.Evaluation.ControlDate) <= maxEnd);
        }

        var penaltyAnswers = await query.ToListAsync();

        // Summary
        var summary = new PenaltySummaryDto
        {
            TotalPenalties = penaltyAnswers.Count,
            TotalYellowCards = penaltyAnswers.Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.YellowCard),
            TotalRedCards = penaltyAnswers.Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.RedCard),
            AffectedEvaluations = penaltyAnswers.Select(a => a.EvaluationId).Distinct().Count()
        };

        // Total count before pagination
        var totalCount = penaltyAnswers.Count;

        // Detailed penalties with pagination
        var penalties = penaltyAnswers
            .OrderByDescending(a => a.Evaluation.ControlDate ?? a.Evaluation.CompletedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new PenaltyDetailDto
            {
                EvaluationId = a.EvaluationId,
                AnswerId = a.Id,
                QuestionId = a.QuestionId,
                CallId = a.Evaluation.CallId,
                CallTime = a.Evaluation.CallTime,
                Duration = a.Evaluation.Duration,
                QuestionText = a.Question?.Text ?? "",
                GroupName = a.Question?.GroupName ?? "",
                PenaltyType = PenaltyTypes.GetById(a.AppliedPenaltyTypeId)?.SystemName ?? "None",
                ProjectName = a.Evaluation.Assignment.Project?.Name ?? "",
                CustomerName = a.Evaluation.Assignment.Project?.Customer?.CompanyName,
                OrganizationName = a.Evaluation.EvaluatedOrganization?.Name,
                ChecklistName = a.Question?.Checklist?.Name,
                EvaluatorName = a.Evaluation.Evaluator != null
                    ? $"{a.Evaluation.Evaluator.FirstName} {a.Evaluation.Evaluator.LastName}"
                    : null,
                EvaluatedPersonnelName = a.Evaluation.EvaluatedCustomerPersonnel != null
                    ? $"{a.Evaluation.EvaluatedCustomerPersonnel.FirstName} {a.Evaluation.EvaluatedCustomerPersonnel.LastName}"
                    : (a.Evaluation.EvaluatedPersonnel != null
                        ? $"{a.Evaluation.EvaluatedPersonnel.FirstName} {a.Evaluation.EvaluatedPersonnel.LastName}"
                        : a.Evaluation.EvaluatedUnknownPersonnel),
                EvaluationDate = a.Evaluation.ControlDate ?? a.Evaluation.CompletedAt,
                Notes = a.Notes,
                PeriodName = a.Evaluation.AssignmentPeriod != null
                    ? a.Evaluation.AssignmentPeriod.Name
                    : ((a.Evaluation.ControlDate ?? a.Evaluation.CompletedAt) != null
                        ? (a.Evaluation.ControlDate ?? a.Evaluation.CompletedAt)!.Value.ToString("yyyyMM")
                        : null),
                SelectedSubCriteria = a.SubCriteriaSelections
                    .Where(s => s.SubCriteria != null)
                    .Select(s => s.SubCriteria.Description)
                    .ToList()
            })
            .ToList();

        // Top penalty questions
        var topQuestions = penaltyAnswers
            .Where(a => a.Question != null)
            .GroupBy(a => new { a.QuestionId, a.Question!.Text, GroupName = a.Question.GroupName ?? "", ChecklistName = a.Question.Checklist?.Name ?? "" })
            .Select(g => new PenaltyQuestionDto
            {
                QuestionId = g.Key.QuestionId,
                QuestionText = g.Key.Text,
                GroupName = g.Key.GroupName,
                ChecklistName = g.Key.ChecklistName,
                YellowCardCount = g.Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.YellowCard),
                RedCardCount = g.Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.RedCard),
                TotalPenalties = g.Count()
            })
            .OrderByDescending(q => q.TotalPenalties)
            .Take(10)
            .ToList();

        // Top penalty organizations
        var topOrganizations = penaltyAnswers
            .Where(a => a.Evaluation.EvaluatedOrganization != null)
            .GroupBy(a => new
            {
                OrgId = a.Evaluation.EvaluatedOrganizationId ?? 0,
                OrgName = a.Evaluation.EvaluatedOrganization?.Name ?? "",
                CustomerName = a.Evaluation.Assignment.Project?.Customer?.CompanyName ?? ""
            })
            .Where(g => g.Key.OrgId > 0)
            .Select(g => new PenaltyOrganizationDto
            {
                OrganizationId = g.Key.OrgId,
                OrganizationName = g.Key.OrgName,
                CustomerName = g.Key.CustomerName,
                YellowCardCount = g.Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.YellowCard),
                RedCardCount = g.Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.RedCard),
                TotalPenalties = g.Count()
            })
            .OrderByDescending(o => o.TotalPenalties)
            .Take(10)
            .ToList();

        // Top penalty personnel
        var topPersonnel = penaltyAnswers
            .Where(a => a.Evaluation.EvaluatedCustomerPersonnel != null)
            .GroupBy(a => new
            {
                PersonnelId = a.Evaluation.EvaluatedCustomerPersonnelId ?? 0,
                PersonnelName = a.Evaluation.EvaluatedCustomerPersonnel != null
                    ? $"{a.Evaluation.EvaluatedCustomerPersonnel.FirstName} {a.Evaluation.EvaluatedCustomerPersonnel.LastName}"
                    : "",
                OrgName = a.Evaluation.EvaluatedOrganization?.Name ?? ""
            })
            .Where(g => g.Key.PersonnelId > 0)
            .Select(g => new PenaltyPersonnelDto
            {
                PersonnelId = g.Key.PersonnelId,
                PersonnelName = g.Key.PersonnelName,
                OrganizationName = g.Key.OrgName,
                YellowCardCount = g.Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.YellowCard),
                RedCardCount = g.Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.RedCard),
                TotalPenalties = g.Count(),
                EvaluationCount = g.Select(a => a.EvaluationId).Distinct().Count()
            })
            .OrderByDescending(p => p.TotalPenalties)
            .Take(10)
            .ToList();

        // Monthly trend (last 12 months)
        var monthlyTrend = penaltyAnswers
            .Where(a => a.Evaluation.ControlDate.HasValue || a.Evaluation.CompletedAt.HasValue)
            .GroupBy(a => new
            {
                Year = (a.Evaluation.ControlDate ?? a.Evaluation.CompletedAt!.Value).Year,
                Month = (a.Evaluation.ControlDate ?? a.Evaluation.CompletedAt!.Value).Month
            })
            .Select(g => new PenaltyMonthlyTrendDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = GetTurkishMonthName(g.Key.Month) + " " + g.Key.Year,
                YellowCardCount = g.Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.YellowCard),
                RedCardCount = g.Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.RedCard),
                TotalPenalties = g.Count()
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .Take(12)
            .ToList();

        return new PenaltyReportResultDto
        {
            Summary = summary,
            Penalties = penalties,
            TopPenaltyQuestions = topQuestions,
            TopPenaltyOrganizations = topOrganizations,
            TopPenaltyPersonnel = topPersonnel,
            MonthlyTrend = monthlyTrend,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ExcelExportDto> ExportPenaltiesToExcelAsync(PenaltyFilterDto filter, bool excludeEvaluator = false)
    {
        // Export için pagination olmadan tüm veriyi çek
        var query = _context.Answers
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Assignment)
                    .ThenInclude(a => a.Project)
                        .ThenInclude(p => p!.Customer)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Evaluator)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedCustomerPersonnel)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedPersonnel)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedOrganization)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.AssignmentPeriod)
            .Include(a => a.Question)
                .ThenInclude(q => q.Checklist)
            .Include(a => a.SubCriteriaSelections)
                .ThenInclude(s => s.SubCriteria)
            .Where(a => a.AppliedPenaltyTypeId != PenaltyTypes.Ids.None)
            .AsQueryable();

        // Varsayılan proje tipi filtresi
        if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(a => a.Evaluation.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(a => filter.ProjectIds.Contains(a.Evaluation.Assignment.ProjectId));

        if (filter.CustomerIds?.Any() == true)
            query = query.Where(a => a.Evaluation.Assignment.Project.CustomerId.HasValue && filter.CustomerIds.Contains(a.Evaluation.Assignment.Project.CustomerId.Value));

        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatedOrganizationId.HasValue && filter.OrganizationIds.Contains(a.Evaluation.EvaluatedOrganizationId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(a => filter.ChecklistIds.Contains(a.Question.ChecklistId));

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(a.Evaluation.EvaluatorId.Value));

        if (filter.PenaltyTypes?.Any() == true)
        {
            var penaltyTypeIds = filter.PenaltyTypes
                .Select(pt => PenaltyTypes.GetBySystemName(pt)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (penaltyTypeIds.Any())
                query = query.Where(a => penaltyTypeIds.Contains(a.AppliedPenaltyTypeId));
        }

        // Date Range filter
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(a => (a.Evaluation.CompletedAt ?? a.Evaluation.ControlDate) >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(a => (a.Evaluation.CompletedAt ?? a.Evaluation.ControlDate) <= maxEnd);
        }

        var penaltyAnswers = await query.ToListAsync();

        using var workbook = new XLWorkbook();

        // Summary sheet
        var summarySheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Summary", defaultValue: "Özet"));
        summarySheet.Cell(1, 1).Value = await _localizationService.GetResourceAsync("Report.TotalPenalties", defaultValue: "Toplam Cezalı");
        summarySheet.Cell(1, 2).Value = penaltyAnswers.Count;
        summarySheet.Cell(2, 1).Value = await _localizationService.GetResourceAsync("Report.YellowCard", defaultValue: "Sarı Kart");
        summarySheet.Cell(2, 2).Value = penaltyAnswers.Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.YellowCard);
        summarySheet.Cell(3, 1).Value = await _localizationService.GetResourceAsync("Report.RedCard", defaultValue: "Kırmızı Kart");
        summarySheet.Cell(3, 2).Value = penaltyAnswers.Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.RedCard);
        summarySheet.Cell(4, 1).Value = await _localizationService.GetResourceAsync("Report.AffectedEvaluations", defaultValue: "Etkilenen Değerlendirme");
        summarySheet.Cell(4, 2).Value = penaltyAnswers.Select(a => a.EvaluationId).Distinct().Count();
        summarySheet.Cell(5, 1).Value = await _localizationService.GetResourceAsync("Report.ReportDate", defaultValue: "Rapor Tarihi");
        summarySheet.Cell(5, 2).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        summarySheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(summarySheet);

        // Penalties detail sheet (tüm veriler - pagination yok)
        var penaltiesSheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.PenaltyEvaluations", defaultValue: "Cezalı Değerlendirmeler"));

        var headersList = new List<string>
        {
            await _localizationService.GetResourceAsync("Report.Date", defaultValue: "Tarih"),
            await _localizationService.GetResourceAsync("Report.Period", defaultValue: "Periyot"),
            await _localizationService.GetResourceAsync("Report.CallId", defaultValue: "Çağrı ID"),
            await _localizationService.GetResourceAsync("Report.CallTime", defaultValue: "Çağrı Saati"),
            await _localizationService.GetResourceAsync("Report.Duration", defaultValue: "Süre"),
            await _localizationService.GetResourceAsync("Report.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.Organization", defaultValue: "Organizasyon"),
            await _localizationService.GetResourceAsync("Report.Checklist", defaultValue: "Kontrol Listesi"),
            await _localizationService.GetResourceAsync("Report.Section", defaultValue: "Bölüm"),
            await _localizationService.GetResourceAsync("Report.Question", defaultValue: "Soru"),
            await _localizationService.GetResourceAsync("Report.SubCriteria", defaultValue: "Alt Kriterler"),
            await _localizationService.GetResourceAsync("Report.PenaltyType", defaultValue: "Ceza Tipi")
        };

        if (!excludeEvaluator)
        {
            headersList.Add(await _localizationService.GetResourceAsync("Report.Evaluator", defaultValue: "Değerlendirici"));
        }

        headersList.Add(await _localizationService.GetResourceAsync("Report.Evaluated", defaultValue: "Denetlenen"));
        headersList.Add(await _localizationService.GetResourceAsync("Report.Note", defaultValue: "Not"));

        var headers = headersList.ToArray();

        for (int i = 0; i < headers.Length; i++)
        {
            penaltiesSheet.Cell(1, i + 1).Value = headers[i];
            penaltiesSheet.Cell(1, i + 1).Style.Font.Bold = true;
            penaltiesSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var yellowCardText = await _localizationService.GetResourceAsync("Report.YellowCard", defaultValue: "Sarı Kart");
        var redCardText = await _localizationService.GetResourceAsync("Report.RedCard", defaultValue: "Kırmızı Kart");

        int row = 2;
        foreach (var a in penaltyAnswers.OrderByDescending(a => a.Evaluation.ControlDate ?? a.Evaluation.CompletedAt))
        {
            int col = 1;
            var evalDate = a.Evaluation.ControlDate ?? a.Evaluation.CompletedAt;
            penaltiesSheet.Cell(row, col++).Value = evalDate?.ToString("dd.MM.yyyy") ?? "";
            // Periyot: AssignmentPeriod varsa adı, yoksa YYYYMM formatında
            var periodName = a.Evaluation.AssignmentPeriod != null
                ? a.Evaluation.AssignmentPeriod.Name
                : (evalDate.HasValue ? evalDate.Value.ToString("yyyyMM") : "");
            penaltiesSheet.Cell(row, col++).Value = periodName;
            penaltiesSheet.Cell(row, col++).Value = a.Evaluation.CallId ?? "";
            penaltiesSheet.Cell(row, col++).Value = a.Evaluation.CallTime ?? "";
            penaltiesSheet.Cell(row, col++).Value = a.Evaluation.Duration ?? "";
            penaltiesSheet.Cell(row, col++).Value = a.Evaluation.Assignment.Project?.Name ?? "";
            penaltiesSheet.Cell(row, col++).Value = a.Evaluation.EvaluatedOrganization?.Name ?? "";
            penaltiesSheet.Cell(row, col++).Value = a.Question?.Checklist?.Name ?? "";
            penaltiesSheet.Cell(row, col++).Value = a.Question?.GroupName ?? "";
            penaltiesSheet.Cell(row, col++).Value = a.Question?.Text ?? "";
            // Alt Kriterler - virgülle ayrılmış liste
            var subCriteriaText = a.SubCriteriaSelections != null && a.SubCriteriaSelections.Any()
                ? string.Join(", ", a.SubCriteriaSelections.Where(s => s.SubCriteria != null).Select(s => s.SubCriteria.Description))
                : "";
            penaltiesSheet.Cell(row, col++).Value = subCriteriaText;
            penaltiesSheet.Cell(row, col++).Value = a.AppliedPenaltyTypeId == PenaltyTypes.Ids.YellowCard ? yellowCardText : redCardText;

            if (!excludeEvaluator)
            {
                penaltiesSheet.Cell(row, col++).Value = a.Evaluation.Evaluator != null
                    ? $"{a.Evaluation.Evaluator.FirstName} {a.Evaluation.Evaluator.LastName}"
                    : "";
            }

            var evaluatedName = a.Evaluation.EvaluatedCustomerPersonnel != null
                ? $"{a.Evaluation.EvaluatedCustomerPersonnel.FirstName} {a.Evaluation.EvaluatedCustomerPersonnel.LastName}"
                : (a.Evaluation.EvaluatedPersonnel != null
                    ? $"{a.Evaluation.EvaluatedPersonnel.FirstName} {a.Evaluation.EvaluatedPersonnel.LastName}"
                    : a.Evaluation.EvaluatedUnknownPersonnel ?? "");
            penaltiesSheet.Cell(row, col++).Value = evaluatedName;
            penaltiesSheet.Cell(row, col++).Value = a.Notes ?? "";
            row++;
        }
        penaltiesSheet.Columns().AdjustToContents();
        // CallId: 3, SubCriteria: 11, Not: son sütun (excludeEvaluator'a göre 14 veya 15)
        var noteCol = excludeEvaluator ? 14 : 15;
        ExcelHelper.ApplyLongTextColumnStyles(penaltiesSheet, callIdColumns: new[] { 3 }, noteColumns: new[] { noteCol }, subCriteriaColumns: new[] { 11 });

        // Top questions sheet - Seçilen alt kriterler ile birlikte
        var questionsSheet = workbook.Worksheets.Add("En Çok Ceza Alan Sorular");

        // Kolonlar: Ceza Tipi, Bölüm, Soru, Seçilen Alt Kriter, Dönem, Adet
        var qHeaders = new List<string>
        {
            await _localizationService.GetResourceAsync("Report.PenaltyType", defaultValue: "Ceza Tipi"),
            await _localizationService.GetResourceAsync("Report.Section", defaultValue: "Bölüm"),
            await _localizationService.GetResourceAsync("Report.Question", defaultValue: "Soru"),
            await _localizationService.GetResourceAsync("Report.SubCriteria", defaultValue: "Seçilen Alt Kriter"),
            await _localizationService.GetResourceAsync("Report.Period", defaultValue: "Dönem"),
            await _localizationService.GetResourceAsync("Common.Count", defaultValue: "Adet")
        };

        for (int i = 0; i < qHeaders.Count; i++)
        {
            questionsSheet.Cell(1, i + 1).Value = qHeaders[i];
            questionsSheet.Cell(1, i + 1).Style.Font.Bold = true;
            questionsSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Her cezalı cevap için seçilen alt kriterleri çıkar
        // Alt kriter varsa her biri için ayrı satır, yoksa boş alt kriter ile tek satır
        var questionDataWithSubCriteria = penaltyAnswers
            .Where(a => a.Question != null)
            .SelectMany(a =>
            {
                var evalDate = a.Evaluation.ControlDate ?? a.Evaluation.CompletedAt ?? a.Evaluation.CreatedAt;
                var periodName = a.Evaluation.AssignmentPeriod?.Name ?? evalDate.ToString("yyyyMM");
                var penaltyType = a.AppliedPenaltyTypeId == PenaltyTypes.Ids.YellowCard ? "YellowCard" : "RedCard";

                var subCriteriaList = a.SubCriteriaSelections?.Where(s => s.SubCriteria != null).ToList() ?? new List<AnswerSubCriteriaSelection>();

                // Alt kriter yoksa boş string ile tek kayıt
                if (!subCriteriaList.Any())
                {
                    return new[]
                    {
                        new
                        {
                            PenaltyType = penaltyType,
                            GroupName = a.Question!.GroupName ?? "",
                            QuestionText = a.Question.Text,
                            SubCriteriaDescription = "",
                            PeriodName = periodName
                        }
                    };
                }

                // Her alt kriter için ayrı kayıt
                return subCriteriaList.Select(sc => new
                {
                    PenaltyType = penaltyType,
                    GroupName = a.Question!.GroupName ?? "",
                    QuestionText = a.Question.Text,
                    SubCriteriaDescription = sc.SubCriteria?.Description ?? "",
                    PeriodName = periodName
                });
            })
            .ToList();

        // Ceza Tipi + Bölüm + Soru + Alt Kriter + Dönem bazında gruplama
        var detailedQuestionStats = questionDataWithSubCriteria
            .GroupBy(x => new { x.PenaltyType, x.GroupName, x.QuestionText, x.SubCriteriaDescription, x.PeriodName })
            .Select(g => new
            {
                g.Key.PenaltyType,
                g.Key.GroupName,
                g.Key.QuestionText,
                g.Key.SubCriteriaDescription,
                g.Key.PeriodName,
                Count = g.Count()
            })
            .OrderBy(x => x.PenaltyType)
            .ThenBy(x => x.GroupName)
            .ThenBy(x => x.QuestionText)
            .ThenBy(x => x.SubCriteriaDescription)
            .ThenBy(x => x.PeriodName)
            .ToList();

        row = 2;
        foreach (var q in detailedQuestionStats)
        {
            questionsSheet.Cell(row, 1).Value = q.PenaltyType == "YellowCard" ? yellowCardText : redCardText;
            questionsSheet.Cell(row, 2).Value = q.GroupName;
            questionsSheet.Cell(row, 3).Value = q.QuestionText;
            questionsSheet.Cell(row, 4).Value = q.SubCriteriaDescription;
            questionsSheet.Cell(row, 5).Value = q.PeriodName;
            questionsSheet.Cell(row, 6).Value = q.Count;
            row++;
        }
        questionsSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(questionsSheet, subCriteriaColumns: new[] { 4 });

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"CezaliKL_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    private static string GetTurkishMonthName(int month)
    {
        return month switch
        {
            1 => "Ocak",
            2 => "Şubat",
            3 => "Mart",
            4 => "Nisan",
            5 => "Mayıs",
            6 => "Haziran",
            7 => "Temmuz",
            8 => "Ağustos",
            9 => "Eylül",
            10 => "Ekim",
            11 => "Kasım",
            12 => "Aralık",
            _ => ""
        };
    }

    // ===== TEMSİLCİ KARNESİ (Video 4) =====

    public async Task<IEnumerable<CustomerListItemDto>> GetCustomersWithEvaluationsAsync()
    {
        // Değerlendirmesi olan müşterileri getir (Project üzerinden Customer)
        // Sadece Çağrı Denetimi projeleri
        var customersFromEvaluations = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
                    .ThenInclude(p => p.Customer)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.Assignment.Project.CustomerId != null &&
                        e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing)
            .Select(e => new
            {
                e.Assignment.Project.Customer!.Id,
                e.Assignment.Project.Customer.CompanyName,
                e.Assignment.Project.Customer.TaxNumber,
                e.Assignment.Project.Customer.IsActive
            })
            .Distinct()
            .ToListAsync();

        return customersFromEvaluations
            .GroupBy(c => c.Id)
            .Select(g => new CustomerListItemDto
            {
                Id = g.Key,
                CompanyName = g.First().CompanyName,
                TaxNumber = g.First().TaxNumber,
                IsActive = g.First().IsActive
            })
            .OrderBy(c => c.CompanyName)
            .ToList();
    }

    public async Task<IEnumerable<OrganizationListItemDto>> GetOrganizationsWithEvaluationsAsync(int? customerId)
    {
        // Değerlendirmesi olan organizasyonları getir
        // Sadece Çağrı Denetimi projeleri
        var query = _context.Evaluations
            .Include(e => e.EvaluatedOrganization)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
                    .ThenInclude(p => p.Customer)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.EvaluatedOrganizationId != null &&
                        e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);

        if (customerId.HasValue)
        {
            query = query.Where(e => e.Assignment.Project.CustomerId == customerId.Value);
        }

        var orgsFromEvaluations = await query
            .Select(e => new
            {
                e.EvaluatedOrganization!.Id,
                e.EvaluatedOrganization.Name,
                e.EvaluatedOrganization.CustomerId,
                CustomerName = e.Assignment.Project.Customer != null ? e.Assignment.Project.Customer.CompanyName : ""
            })
            .ToListAsync();

        return orgsFromEvaluations
            .GroupBy(o => o.Id)
            .Select(g => new OrganizationListItemDto
            {
                Id = g.Key,
                Name = g.First().Name,
                CustomerId = g.First().CustomerId,
                CustomerName = g.First().CustomerName,
                EvaluationCount = g.Count()
            })
            .OrderBy(o => o.Name)
            .ToList();
    }

    public async Task<IEnumerable<PersonnelListItemDto>> GetEvaluatedPersonnelListAsync(int? customerId = null, int? organizationId = null)
    {
        // Değerlendirmede bulunan personelleri getir (EvaluatedCustomerPersonnel = CustomerPersonnel entity)
        var query = _context.Evaluations
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
                    .ThenInclude(p => p.Customer)
            .Where(e => e.EvaluatedCustomerPersonnelId != null && e.StatusId == EvaluationStatuses.Ids.Completed)
            // Sadece Çağrı Denetimi projeleri
            .Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);

        // Müşteriye göre filtrele
        if (customerId.HasValue)
        {
            query = query.Where(e => e.Assignment.Project.CustomerId == customerId.Value);
        }

        // Organizasyona göre filtrele
        if (organizationId.HasValue)
        {
            query = query.Where(e => e.EvaluatedOrganizationId == organizationId.Value);
        }

        var personnelFromEvaluations = await query
            .Select(e => new
            {
                e.EvaluatedCustomerPersonnelId,
                e.EvaluatedCustomerPersonnel!.FirstName,
                e.EvaluatedCustomerPersonnel.LastName,
                CustomerId = e.Assignment.Project.CustomerId,
                CustomerName = e.Assignment.Project.Customer != null ? e.Assignment.Project.Customer.CompanyName : "",
                OrganizationId = e.EvaluatedOrganizationId,
                OrganizationName = e.EvaluatedOrganization != null ? e.EvaluatedOrganization.Name : ""
            })
            .ToListAsync();

        return personnelFromEvaluations
            .GroupBy(p => p.EvaluatedCustomerPersonnelId)
            .Select(g => new PersonnelListItemDto
            {
                Id = g.Key!.Value,
                Name = $"{g.First().FirstName} {g.First().LastName}",
                Title = null,
                CustomerId = g.First().CustomerId,
                CustomerName = g.First().CustomerName,
                OrganizationId = g.First().OrganizationId,
                OrganizationName = g.First().OrganizationName
            })
            .OrderBy(p => p.Name)
            .ToList();
    }

    /// <summary>
    /// Personelin değerlendirildiği projeleri getirir (karne filtresi için)
    /// </summary>
    public async Task<IEnumerable<ProjectListItemDto>> GetPersonnelProjectsAsync(int personnelId)
    {
        var projects = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.EvaluatedCustomerPersonnelId == personnelId && e.StatusId == EvaluationStatuses.Ids.Completed)
            .Select(e => new { e.Assignment.ProjectId, e.Assignment.Project.Name })
            .Distinct()
            .ToListAsync();

        return projects.Select(p => new ProjectListItemDto
        {
            Id = p.ProjectId,
            Name = p.Name
        }).OrderBy(p => p.Name).ToList();
    }

    public async Task<PersonnelReportCardDto?> GetPersonnelReportCardAsync(PersonnelReportCardFilterDto filter)
    {
        // CustomerPersonnel tablosundan personeli bul
        var personnel = await _context.CustomerPersonnel
            .FirstOrDefaultAsync(p => p.Id == filter.PersonnelId);

        if (personnel == null)
            return null;

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Include(e => e.Evaluator)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => e.EvaluatedCustomerPersonnelId == filter.PersonnelId && e.StatusId == EvaluationStatuses.Ids.Completed);

        // Proje filtresi: Çoğul ProjectIds veya varsayılan Çağrı Denetimi
        if (filter.ProjectIds?.Any() != true)
        {
            // Varsayılan: Çağrı Denetimi projeleri
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }
        else
        {
            query = query.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));
        }

        // DateRanges pattern (UTC dönüşümü Service'de)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(e => e.CompletedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(e => e.CompletedAt <= maxEnd);
        }

        var evaluations = await query.ToListAsync();

        if (!evaluations.Any())
        {
            // Değerlendirme yoksa da threshold değerlerini getir (varsayılan proje tipine göre)
            var defaultProjectTypeId = ProjectTypes.Ids.CallAuditing;
            if (filter.ProjectIds?.Any() == true)
            {
                var projectType = await _context.Projects
                    .Where(p => filter.ProjectIds.Contains(p.Id))
                    .Select(p => p.ProjectTypeId)
                    .FirstOrDefaultAsync();
                if (projectType > 0) defaultProjectTypeId = projectType;
            }
            var defaultSettings = await _performanceSettingsService.GetByProjectTypeIdAsync(defaultProjectTypeId);

            return new PersonnelReportCardDto
            {
                PersonnelId = personnel.Id,
                PersonnelName = $"{personnel.FirstName} {personnel.LastName}",
                Title = personnel.Title ?? "",
                Department = personnel.Department,
                SuccessThreshold = defaultSettings?.SuccessThreshold ?? 80,
                WarningThreshold = defaultSettings?.WarningThreshold ?? 60
            };
        }

        // Özet istatistikler
        var completedScores = evaluations.Where(e => e.ScorePercentage.HasValue).Select(e => e.ScorePercentage!.Value).ToList();

        // Aylık trend (son 12 ay)
        var monthlyTrend = evaluations
            .Where(e => e.CompletedAt.HasValue)
            .GroupBy(e => new { e.CompletedAt!.Value.Year, e.CompletedAt.Value.Month })
            .Select(g => new PersonnelMonthlyTrendDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = GetTurkishMonthName(g.Key.Month) + " " + g.Key.Year,
                EvaluationCount = g.Count(),
                AverageScore = g.Where(e => e.ScorePercentage.HasValue).Any()
                    ? Math.Round(g.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage!.Value), 2)
                    : 0,
                YellowCards = g.Sum(e => e.YellowCardCount),
                RedCards = g.Sum(e => e.RedCardCount)
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .Take(12)
            .ToList();

        // Bölüm bazlı performans
        var allAnswers = evaluations.SelectMany(e => e.Answers).Where(a => !string.IsNullOrEmpty(a.Question?.GroupName)).ToList();
        var groupPerformances = allAnswers
            .GroupBy(a => a.Question!.GroupName!)
            .Select(g => new PersonnelGroupPerformanceDto
            {
                GroupName = g.Key,
                EvaluationCount = g.Select(a => a.EvaluationId).Distinct().Count(),
                AverageScore = g.Sum(a => a.EarnedPoints ?? 0),
                MaxPossibleScore = g.Sum(a => a.Question!.WeightPoints),
                PercentageScore = g.Sum(a => a.Question!.WeightPoints) > 0
                    ? Math.Round(g.Sum(a => a.EarnedPoints ?? 0) / g.Sum(a => a.Question!.WeightPoints) * 100, 2)
                    : 0
            })
            .OrderByDescending(s => s.PercentageScore)
            .ToList();

        // Değerlendirmeler (filtreye göre tümü)
        var recentEvaluations = evaluations
            .OrderByDescending(e => e.CompletedAt)
            .Select(e => new PersonnelEvaluationSummaryDto
            {
                EvaluationId = e.Id,
                EvaluationDate = e.CompletedAt,
                ProjectName = e.Assignment.Project?.Name ?? "",
                ChecklistName = e.Assignment.Checklist?.Name ?? "",
                EvaluatorName = e.Evaluator != null ? $"{e.Evaluator.FirstName} {e.Evaluator.LastName}" : null,
                ScorePercentage = e.ScorePercentage ?? 0,
                YellowCards = e.YellowCardCount,
                RedCards = e.RedCardCount,
                Status = EvaluationStatuses.GetById(e.StatusId)?.SystemName ?? "",
                CallId = e.CallId,
                CallTime = e.CallTime,
                Duration = e.Duration,
                Notes = e.Notes
            })
            .ToList();

        // Güçlü ve zayıf yönler (soru bazlı analiz)
        // 1. Sadece Scored (puanlı) sorulardan güçlü/zayıf yönler
        var scoredAnswers = allAnswers
            .Where(a => a.Question != null && a.Question.ScoringTypeId == ScoringTypes.Ids.Scored)
            .ToList();

        var questionPerformance = scoredAnswers
            .GroupBy(a => new { a.Question!.Id, a.Question.Text, GroupName = a.Question.GroupName ?? "" })
            .Select(g => new PersonnelStrengthWeaknessDto
            {
                QuestionText = g.Key.Text,
                GroupName = g.Key.GroupName,
                AverageScore = g.Sum(a => a.EarnedPoints ?? 0),
                MaxScore = g.Sum(a => a.Question!.WeightPoints),
                PercentageScore = g.Sum(a => a.Question!.WeightPoints) > 0
                    ? Math.Round(g.Sum(a => a.EarnedPoints ?? 0) / g.Sum(a => a.Question!.WeightPoints) * 100, 2)
                    : 0,
                EvaluationCount = g.Count()
            })
            .Where(q => q.EvaluationCount >= 2) // En az 2 kez değerlendirilmiş sorular
            .ToList();

        var strengths = questionPerformance.OrderByDescending(q => q.PercentageScore).Take(5).ToList();

        // 2. Zayıf yönler: Scored sorular + Penalty sorular (ceza uygulanmışsa)
        var scoredWeaknesses = questionPerformance.OrderBy(q => q.PercentageScore).Take(5).ToList();

        // 3. Penalty (cezalı) sorulardan zayıf yönler - ceza uygulanmışsa
        var penaltyWeaknesses = allAnswers
            .Where(a => a.Question != null &&
                        a.Question.ScoringTypeId == ScoringTypes.Ids.Penalty &&
                        a.IsPenaltyApplied)
            .GroupBy(a => new { a.Question!.Id, a.Question.Text, GroupName = a.Question.GroupName ?? "" })
            .Select(g => new PersonnelStrengthWeaknessDto
            {
                QuestionText = g.Key.Text + (g.SelectMany(a => a.SubCriteriaSelections).Any()
                    ? " (" + string.Join(", ", g.SelectMany(a => a.SubCriteriaSelections).Select(s => s.SubCriteria?.Description).Where(n => n != null).Distinct().Take(3)) + ")"
                    : ""),
                GroupName = g.Key.GroupName,
                PercentageScore = 0, // Cezalı sorular için 0%
                EvaluationCount = g.Count()
            })
            .Where(q => q.EvaluationCount >= 1) // Cezalılar için en az 1 kez
            .ToList();

        // Zayıf yönleri birleştir: önce cezalılar, sonra düşük puanlı scored sorular
        var weaknesses = penaltyWeaknesses
            .Concat(scoredWeaknesses)
            .Take(5)
            .ToList();

        // Proje tipine göre PerformanceSettings'ten threshold değerlerini al
        var projectTypeId = evaluations
            .Select(e => e.Assignment?.Project?.ProjectTypeId)
            .FirstOrDefault(pt => pt.HasValue) ?? ProjectTypes.Ids.CallAuditing;

        var performanceSettings = await _performanceSettingsService.GetByProjectTypeIdAsync(projectTypeId);
        var successThreshold = performanceSettings?.SuccessThreshold ?? 80;
        var warningThreshold = performanceSettings?.WarningThreshold ?? 60;

        return new PersonnelReportCardDto
        {
            PersonnelId = personnel.Id,
            PersonnelName = $"{personnel.FirstName} {personnel.LastName}",
            Title = personnel.Title ?? "",
            Department = personnel.Department,
            TotalEvaluations = evaluations.Count,
            AverageScore = completedScores.Any() ? Math.Round(completedScores.Average(), 2) : 0,
            BestScore = completedScores.Any() ? completedScores.Max() : 0,
            WorstScore = completedScores.Any() ? completedScores.Min() : 0,
            TotalYellowCards = evaluations.Sum(e => e.YellowCardCount),
            TotalRedCards = evaluations.Sum(e => e.RedCardCount),
            MonthlyTrend = monthlyTrend,
            GroupPerformances = groupPerformances,
            RecentEvaluations = recentEvaluations,
            Strengths = strengths,
            Weaknesses = weaknesses,
            SuccessThreshold = successThreshold,
            WarningThreshold = warningThreshold
        };
    }

    public async Task<ExcelExportDto> ExportPersonnelReportCardToPdfAsync(PersonnelReportCardFilterDto filter)
    {
        var report = await GetPersonnelReportCardAsync(filter);
        if (report == null)
        {
            return new ExcelExportDto
            {
                FileName = "TemsilciKarnesi_Bulunamadi.xlsx",
                FileContent = Array.Empty<byte>(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        // PDF yerine Excel export (browser-based PDF export için HTML kullanılacak)
        using var workbook = new XLWorkbook();

        // Genel Bilgiler
        var infoSheet = workbook.Worksheets.Add("Genel Bilgiler");
        infoSheet.Cell(1, 1).Value = "TEMSİLCİ KARNESİ";
        infoSheet.Cell(1, 1).Style.Font.Bold = true;
        infoSheet.Cell(1, 1).Style.Font.FontSize = 16;

        infoSheet.Cell(3, 1).Value = "Temsilci Adı:";
        infoSheet.Cell(3, 2).Value = report.PersonnelName;
        infoSheet.Cell(4, 1).Value = "Unvan:";
        infoSheet.Cell(4, 2).Value = report.Title ?? "-";

        infoSheet.Cell(6, 1).Value = "PERFORMANS ÖZETİ";
        infoSheet.Cell(6, 1).Style.Font.Bold = true;

        infoSheet.Cell(7, 1).Value = "Toplam Değerlendirme:";
        infoSheet.Cell(7, 2).Value = report.TotalEvaluations;
        infoSheet.Cell(8, 1).Value = "Ortalama Puan:";
        infoSheet.Cell(8, 2).Value = $"{report.AverageScore:F1}%";
        infoSheet.Cell(9, 1).Value = "En Yüksek Puan:";
        infoSheet.Cell(9, 2).Value = $"{report.BestScore:F1}%";
        infoSheet.Cell(10, 1).Value = "En Düşük Puan:";
        infoSheet.Cell(10, 2).Value = $"{report.WorstScore:F1}%";
        infoSheet.Cell(11, 1).Value = "Toplam Sarı Kart:";
        infoSheet.Cell(11, 2).Value = report.TotalYellowCards;
        infoSheet.Cell(12, 1).Value = "Toplam Kırmızı Kart:";
        infoSheet.Cell(12, 2).Value = report.TotalRedCards;

        infoSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(infoSheet);

        // Aylık Trend
        var trendSheet = workbook.Worksheets.Add("Aylık Trend");
        trendSheet.Cell(1, 1).Value = "Dönem";
        trendSheet.Cell(1, 1).Style.Font.Bold = true;
        trendSheet.Cell(1, 2).Value = "Değerlendirme Sayısı";
        trendSheet.Cell(1, 2).Style.Font.Bold = true;
        trendSheet.Cell(1, 3).Value = "Ortalama Puan";
        trendSheet.Cell(1, 3).Style.Font.Bold = true;
        trendSheet.Cell(1, 4).Value = "Sarı Kart";
        trendSheet.Cell(1, 4).Style.Font.Bold = true;
        trendSheet.Cell(1, 5).Value = "Kırmızı Kart";
        trendSheet.Cell(1, 5).Style.Font.Bold = true;

        int row = 2;
        foreach (var trend in report.MonthlyTrend)
        {
            trendSheet.Cell(row, 1).Value = trend.MonthName;
            trendSheet.Cell(row, 2).Value = trend.EvaluationCount;
            trendSheet.Cell(row, 3).Value = $"{trend.AverageScore:F1}%";
            trendSheet.Cell(row, 4).Value = trend.YellowCards;
            trendSheet.Cell(row, 5).Value = trend.RedCards;
            row++;
        }
        trendSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(trendSheet);

        // Grup Performansı
        var groupSheet = workbook.Worksheets.Add("Grup Performansı");
        groupSheet.Cell(1, 1).Value = "Grup";
        groupSheet.Cell(1, 1).Style.Font.Bold = true;
        groupSheet.Cell(1, 2).Value = "Değerlendirme";
        groupSheet.Cell(1, 2).Style.Font.Bold = true;
        groupSheet.Cell(1, 3).Value = "Başarı Yüzdesi";
        groupSheet.Cell(1, 3).Style.Font.Bold = true;

        row = 2;
        foreach (var group in report.GroupPerformances)
        {
            groupSheet.Cell(row, 1).Value = group.GroupName;
            groupSheet.Cell(row, 2).Value = group.EvaluationCount;
            groupSheet.Cell(row, 3).Value = $"{group.PercentageScore:F1}%";
            row++;
        }
        groupSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(groupSheet);

        // Değerlendirmeler
        var evalSheet = workbook.Worksheets.Add("Değerlendirmeler");
        evalSheet.Cell(1, 1).Value = "Tarih";
        evalSheet.Cell(1, 1).Style.Font.Bold = true;
        evalSheet.Cell(1, 2).Value = "Çağrı ID";
        evalSheet.Cell(1, 2).Style.Font.Bold = true;
        evalSheet.Cell(1, 3).Value = "Çağrı Saati";
        evalSheet.Cell(1, 3).Style.Font.Bold = true;
        evalSheet.Cell(1, 4).Value = "Süre";
        evalSheet.Cell(1, 4).Style.Font.Bold = true;
        evalSheet.Cell(1, 5).Value = "Proje";
        evalSheet.Cell(1, 5).Style.Font.Bold = true;
        evalSheet.Cell(1, 6).Value = "Kontrol Listesi";
        evalSheet.Cell(1, 6).Style.Font.Bold = true;
        evalSheet.Cell(1, 7).Value = "Puan";
        evalSheet.Cell(1, 7).Style.Font.Bold = true;
        evalSheet.Cell(1, 8).Value = "Sarı Kart";
        evalSheet.Cell(1, 8).Style.Font.Bold = true;
        evalSheet.Cell(1, 9).Value = "Kırmızı Kart";
        evalSheet.Cell(1, 9).Style.Font.Bold = true;

        row = 2;
        foreach (var eval in report.RecentEvaluations)
        {
            evalSheet.Cell(row, 1).Value = eval.EvaluationDate?.ToString("dd.MM.yyyy") ?? "-";
            evalSheet.Cell(row, 2).Value = eval.CallId ?? "-";
            evalSheet.Cell(row, 3).Value = eval.CallTime ?? "-";
            evalSheet.Cell(row, 4).Value = eval.Duration ?? "-";
            evalSheet.Cell(row, 5).Value = eval.ProjectName;
            evalSheet.Cell(row, 6).Value = eval.ChecklistName;
            evalSheet.Cell(row, 7).Value = $"{eval.ScorePercentage:F1}%";
            evalSheet.Cell(row, 8).Value = eval.YellowCards;
            evalSheet.Cell(row, 9).Value = eval.RedCards;
            row++;
        }
        evalSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(evalSheet, callIdColumns: new[] { 2 });

        // Güçlü/Zayıf Yönler
        var analysisSheet = workbook.Worksheets.Add("Güçlü ve Zayıf Yönler");

        analysisSheet.Cell(1, 1).Value = "GÜÇLÜ YÖNLER";
        analysisSheet.Cell(1, 1).Style.Font.Bold = true;
        analysisSheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;

        row = 2;
        foreach (var strength in report.Strengths)
        {
            analysisSheet.Cell(row, 1).Value = strength.GroupName;
            analysisSheet.Cell(row, 2).Value = strength.QuestionText;
            analysisSheet.Cell(row, 3).Value = $"{strength.PercentageScore:F1}%";
            row++;
        }

        row += 2;
        analysisSheet.Cell(row, 1).Value = "ZAYIF YÖNLER";
        analysisSheet.Cell(row, 1).Style.Font.Bold = true;
        analysisSheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightCoral;
        row++;

        foreach (var weakness in report.Weaknesses)
        {
            analysisSheet.Cell(row, 1).Value = weakness.GroupName;
            analysisSheet.Cell(row, 2).Value = weakness.QuestionText;
            analysisSheet.Cell(row, 3).Value = $"{weakness.PercentageScore:F1}%";
            row++;
        }
        analysisSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(analysisSheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"TemsilciKarnesi_{report.PersonnelName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    /// <summary>
    /// Temsilci Karnesi Word export
    /// </summary>
    public async Task<ExcelExportDto> ExportPersonnelReportCardToWordAsync(PersonnelReportCardFilterDto filter)
    {
        var report = await GetPersonnelReportCardAsync(filter);
        if (report == null)
        {
            return new ExcelExportDto
            {
                FileName = "TemsilciKarnesi_Bulunamadi.docx",
                FileContent = Array.Empty<byte>(),
                ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };
        }

        // Proje adı (ilk değerlendirmeden al)
        var projectName = report.RecentEvaluations.FirstOrDefault()?.ProjectName ?? "Proje";

        using var doc = new XWPFDocument();

        // ===== BAŞLIK TABLOSU =====
        var headerTable = doc.CreateTable(2, 2);
        headerTable.Width = 5000;

        // Sol üst: Personel adı
        var nameCell = headerTable.GetRow(0).GetCell(0);
        nameCell.SetText(report.PersonnelName);
        if (nameCell.Paragraphs.Count > 0 && nameCell.Paragraphs[0].Runs.Count > 0)
        {
            nameCell.Paragraphs[0].Runs[0].IsBold = true;
            nameCell.Paragraphs[0].Runs[0].FontSize = 14;
        }

        // Sağ üst: "Doküman Tarihi" etiketi
        var dateLabelCell = headerTable.GetRow(0).GetCell(1);
        dateLabelCell.SetText("Doküman Tarihi");
        if (dateLabelCell.Paragraphs.Count > 0 && dateLabelCell.Paragraphs[0].Runs.Count > 0)
        {
            dateLabelCell.Paragraphs[0].Runs[0].IsBold = true;
        }

        // Sol alt: boş (personel adı devamı gibi düşünülebilir)
        headerTable.GetRow(1).GetCell(0).SetText("");

        // Sağ alt: Tarih değeri
        var dateValueCell = headerTable.GetRow(1).GetCell(1);
        dateValueCell.SetText(DateTime.Now.ToString("dd.MM.yyyy"));

        // Boş paragraf
        doc.CreateParagraph();

        // ===== PROJE + BAŞARI ORTALAMASI =====
        var avgPara = doc.CreateParagraph();
        var avgRun = avgPara.CreateRun();
        avgRun.SetText($"{projectName}");
        avgRun.IsBold = true;
        avgRun.FontSize = 11;

        var scorePara = doc.CreateParagraph();
        var scoreRun = scorePara.CreateRun();
        scoreRun.SetText($"  {report.AverageScore:F2}");
        scoreRun.IsBold = true;
        scoreRun.FontSize = 24;

        var scoreLabelPara = doc.CreateParagraph();
        var scoreLabelRun = scoreLabelPara.CreateRun();
        scoreLabelRun.SetText("BAŞARI ORTALAMASI");
        scoreLabelRun.IsBold = true;
        scoreLabelRun.FontSize = 10;

        doc.CreateParagraph();

        // ===== KONTROL SORULARI TABLOSU =====
        var questionTable = doc.CreateTable(report.GroupPerformances.Count + 1, 2);
        questionTable.Width = 5000;

        // Başlık satırı
        var qHeaderRow = questionTable.GetRow(0);
        qHeaderRow.GetCell(0).SetText("Kontrol Sorusu");
        qHeaderRow.GetCell(1).SetText("Puan");
        foreach (var cell in qHeaderRow.GetTableCells())
        {
            if (cell.Paragraphs.Count > 0 && cell.Paragraphs[0].Runs.Count > 0)
            {
                cell.Paragraphs[0].Runs[0].IsBold = true;
            }
        }

        // Veri satırları
        for (int i = 0; i < report.GroupPerformances.Count; i++)
        {
            var group = report.GroupPerformances[i];
            var dataRow = questionTable.GetRow(i + 1);
            dataRow.GetCell(0).SetText(group.GroupName);
            dataRow.GetCell(1).SetText($"{group.PercentageScore:F0}");
        }

        doc.CreateParagraph();

        // ===== GENEL ANALİZ TABLOSU =====
        var analysisHeader = doc.CreateParagraph();
        var analysisHeaderRun = analysisHeader.CreateRun();
        analysisHeaderRun.SetText("GENEL ANALİZ");
        analysisHeaderRun.IsBold = true;
        analysisHeaderRun.FontSize = 12;

        var evalTable = doc.CreateTable(report.RecentEvaluations.Count + 1, 4);

        // Tablo genişliğini sayfa genişliğine ayarla
        evalTable.GetCTTbl().AddNewTblPr().AddNewTblW().w = "9000";
        evalTable.GetCTTbl().tblPr.tblW.type = NPOI.OpenXmlFormats.Wordprocessing.ST_TblWidth.dxa;

        // Sütun genişlikleri (twips): Tarih:1200, Çağrı:3000, Yorum:3800, Puan:1000
        int[] colWidths = { 1200, 3000, 3800, 1000 };

        // Başlık satırı
        var evalHeaderRow = evalTable.GetRow(0);
        string[] headers = { "Görüşme Tarihi", "Değerlendirilen Çağrı", "Denetim Yorumu", "Toplam Puan" };
        for (int c = 0; c < 4; c++)
        {
            var cell = evalHeaderRow.GetCell(c);
            cell.SetText(headers[c]);
            // Hücre genişliği ayarla
            var tcPr = cell.GetCTTc().AddNewTcPr();
            tcPr.AddNewTcW().w = colWidths[c].ToString();
            tcPr.tcW.type = NPOI.OpenXmlFormats.Wordprocessing.ST_TblWidth.dxa;
            // Başlık kalın
            if (cell.Paragraphs.Count > 0 && cell.Paragraphs[0].Runs.Count > 0)
            {
                cell.Paragraphs[0].Runs[0].IsBold = true;
            }
        }

        // Veri satırları
        for (int i = 0; i < report.RecentEvaluations.Count; i++)
        {
            var eval = report.RecentEvaluations[i];
            var dataRow = evalTable.GetRow(i + 1);
            string[] values = {
                eval.EvaluationDate?.ToString("dd.MM.yyyy") ?? "-",
                eval.CallId ?? "-",
                eval.Notes ?? "",
                $"{eval.ScorePercentage:F0}"
            };

            for (int c = 0; c < 4; c++)
            {
                var cell = dataRow.GetCell(c);
                cell.SetText(values[c]);
                // Hücre genişliği ayarla (word wrap otomatik olur)
                var tcPr = cell.GetCTTc().AddNewTcPr();
                tcPr.AddNewTcW().w = colWidths[c].ToString();
                tcPr.tcW.type = NPOI.OpenXmlFormats.Wordprocessing.ST_TblWidth.dxa;
            }
        }

        // Word dosyasını kaydet
        using var stream = new MemoryStream();
        doc.Write(stream);

        var safePersonnelName = string.Join("_", report.PersonnelName.Split(Path.GetInvalidFileNameChars()));

        return new ExcelExportDto
        {
            FileName = $"MT_Karne_{safePersonnelName}_{DateTime.Now:yyyyMMdd}.docx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };
    }

    // ===== ÖNERİLER RAPORU (Video 5-6) =====

    public async Task<SuggestionsReportResultDto> GetSuggestionsReportAsync(SuggestionsFilterDto filter)
    {
        // Düşük puan eşiği: %50
        const decimal lowScoreThreshold = 0.5m;

        var query = _context.Answers
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Assignment)
                    .ThenInclude(a => a.Project)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Assignment)
                    .ThenInclude(a => a.Checklist)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Evaluator)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedPersonnel)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedCustomerPersonnel)
            .Include(a => a.Question)
                .ThenInclude(q => q.Checklist)
            .Include(a => a.SubCriteriaSelections)
                .ThenInclude(s => s.SubCriteria)
            .Where(a => a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            // Hibrit filtre: Not/Öneri VEYA Kart VEYA Düşük puan
            .Where(a =>
                // Not veya öneri yazılmış
                !string.IsNullOrEmpty(a.Notes) ||
                !string.IsNullOrEmpty(a.RecommendationNotes) ||
                // Sarı veya kırmızı kart uygulanmış
                a.AppliedPenaltyTypeId == PenaltyTypes.Ids.YellowCard ||
                a.AppliedPenaltyTypeId == PenaltyTypes.Ids.RedCard ||
                // Düşük puan (%50 altı) - sadece puanlı sorular için
                (a.Question.WeightPoints > 0 && a.EarnedPoints.HasValue &&
                 (a.EarnedPoints.Value / a.Question.WeightPoints) < lowScoreThreshold)
            )
            .AsQueryable();

        // Varsayılan proje tipi filtresi: Çağrı Denetimi (proje filtresi yoksa)
        if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(a => a.Evaluation.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(a => filter.ProjectIds.Contains(a.Evaluation.Assignment.ProjectId));

        if (filter.CustomerIds?.Any() == true)
            query = query.Where(a => a.Evaluation.Assignment.Project.CustomerId.HasValue && filter.CustomerIds.Contains(a.Evaluation.Assignment.Project.CustomerId.Value));

        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatedOrganizationId.HasValue && filter.OrganizationIds.Contains(a.Evaluation.EvaluatedOrganizationId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(a => filter.ChecklistIds.Contains(a.Evaluation.Assignment.ChecklistId));

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(a.Evaluation.EvaluatorId.Value));

        if (filter.PersonnelIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatedCustomerPersonnelId.HasValue && filter.PersonnelIds.Contains(a.Evaluation.EvaluatedCustomerPersonnelId.Value));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(a => a.Evaluation.CompletedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(a => a.Evaluation.CompletedAt <= maxEnd);
        }

        if (!string.IsNullOrEmpty(filter.SearchText))
        {
            var searchLower = filter.SearchText.ToLower();
            query = query.Where(a =>
                (a.Notes != null && a.Notes.ToLower().Contains(searchLower)) ||
                (a.RecommendationNotes != null && a.RecommendationNotes.ToLower().Contains(searchLower)) ||
                a.Question!.Text.ToLower().Contains(searchLower));
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Get paginated results
        var suggestions = await query
            .OrderByDescending(a => a.Evaluation.CompletedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        // Calculate summary
        var allSuggestionAnswers = await query.ToListAsync();

        var summary = new SuggestionsSummaryDto
        {
            TotalSuggestions = totalCount,
            TotalEvaluationsWithSuggestions = allSuggestionAnswers.Select(a => a.EvaluationId).Distinct().Count(),
            UniqueEvaluators = allSuggestionAnswers
                .Where(a => a.Evaluation.EvaluatorId.HasValue)
                .Select(a => a.Evaluation.EvaluatorId)
                .Distinct()
                .Count(),
            UniquePersonnel = allSuggestionAnswers
                .Where(a => a.Evaluation.EvaluatedCustomerPersonnelId.HasValue)
                .Select(a => a.Evaluation.EvaluatedCustomerPersonnelId)
                .Distinct()
                .Count(),
            RedCardCount = allSuggestionAnswers
                .Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.RedCard),
            YellowCardCount = allSuggestionAnswers
                .Count(a => a.AppliedPenaltyTypeId == PenaltyTypes.Ids.YellowCard),
            LowScoreCount = allSuggestionAnswers
                .Count(a => a.Question?.WeightPoints > 0 && a.EarnedPoints.HasValue &&
                           (a.EarnedPoints.Value / a.Question.WeightPoints) < lowScoreThreshold &&
                           a.AppliedPenaltyTypeId != PenaltyTypes.Ids.RedCard &&
                           a.AppliedPenaltyTypeId != PenaltyTypes.Ids.YellowCard)
        };

        // Map to DTOs with ReasonType
        var suggestionDtos = suggestions.Select(a =>
        {
            // ReasonType belirleme (öncelik sırası: RedCard > YellowCard > LowScore > Note)
            string reasonType = "Note";
            if (a.AppliedPenaltyTypeId == PenaltyTypes.Ids.RedCard)
                reasonType = "RedCard";
            else if (a.AppliedPenaltyTypeId == PenaltyTypes.Ids.YellowCard)
                reasonType = "YellowCard";
            else if (a.Question?.WeightPoints > 0 && a.EarnedPoints.HasValue &&
                     (a.EarnedPoints.Value / a.Question.WeightPoints) < lowScoreThreshold)
                reasonType = "LowScore";

            return new SuggestionDetailDto
            {
                EvaluationId = a.EvaluationId,
                AnswerId = a.Id,
                QuestionId = a.QuestionId,
                QuestionText = a.Question?.Text ?? "",
                GroupName = a.Question?.GroupName ?? "",
                ChecklistName = a.Evaluation.Assignment.Checklist?.Name ?? "",
                Notes = a.Notes,
                RecommendationNotes = a.RecommendationNotes,
                GivenPoints = a.EarnedPoints,
                MaxPoints = a.Question?.WeightPoints ?? 0,
                PercentageScore = a.Question?.WeightPoints > 0 && a.EarnedPoints.HasValue
                    ? Math.Round((a.EarnedPoints.Value / a.Question.WeightPoints) * 100, 1)
                    : null,
                ProjectName = a.Evaluation.Assignment.Project?.Name ?? "",
                EvaluatorName = a.Evaluation.Evaluator != null
                    ? $"{a.Evaluation.Evaluator.FirstName} {a.Evaluation.Evaluator.LastName}"
                    : null,
                EvaluatedPersonnelName = a.Evaluation.EvaluatedCustomerPersonnel != null
                    ? $"{a.Evaluation.EvaluatedCustomerPersonnel.FirstName} {a.Evaluation.EvaluatedCustomerPersonnel.LastName}"
                    : (a.Evaluation.EvaluatedPersonnel != null
                        ? $"{a.Evaluation.EvaluatedPersonnel.FirstName} {a.Evaluation.EvaluatedPersonnel.LastName}"
                        : a.Evaluation.EvaluatedUnknownPersonnel),
                EvaluationDate = a.Evaluation.CompletedAt ?? a.Evaluation.ControlDate,
                CallId = a.Evaluation.CallId,
                IsPenaltyApplied = a.IsPenaltyApplied,
                PenaltyType = a.AppliedPenaltyTypeId != PenaltyTypes.Ids.None ? PenaltyTypes.GetById(a.AppliedPenaltyTypeId)?.SystemName : null,
                ReasonType = reasonType,
                SelectedSubCriteria = a.SubCriteriaSelections
                    .Where(s => s.SubCriteria != null)
                    .Select(s => s.SubCriteria.Description)
                    .ToList()
            };
        }).ToList();

        // Evaluation seviyesindeki notları getir (genel notlar ve denetim yorumları)
        var evaluationNotesQuery = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatedPersonnel)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed)
            .Where(e => !string.IsNullOrEmpty(e.Notes) || !string.IsNullOrEmpty(e.EvaluationComment))
            .AsQueryable();

        // Aynı filtreleri uygula
        if (filter.ProjectIds?.Any() == true)
            evaluationNotesQuery = evaluationNotesQuery.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));
        else
            evaluationNotesQuery = evaluationNotesQuery.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);

        if (filter.CustomerIds?.Any() == true)
            evaluationNotesQuery = evaluationNotesQuery.Where(e => e.Assignment.Project.CustomerId.HasValue && filter.CustomerIds.Contains(e.Assignment.Project.CustomerId.Value));

        // Organization filter (Supervisor için gerekli)
        if (filter.OrganizationIds?.Any() == true)
            evaluationNotesQuery = evaluationNotesQuery.Where(e => e.EvaluatedOrganizationId.HasValue && filter.OrganizationIds.Contains(e.EvaluatedOrganizationId.Value));

        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return new { Start = startUtc, End = endUtc };
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                evaluationNotesQuery = evaluationNotesQuery.Where(e => e.CompletedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                evaluationNotesQuery = evaluationNotesQuery.Where(e => e.CompletedAt <= maxEnd);
        }

        var evaluationNotes = await evaluationNotesQuery
            .OrderByDescending(e => e.CompletedAt)
            .Select(e => new EvaluationNoteDto
            {
                EvaluationId = e.Id,
                ProjectName = e.Assignment.Project != null ? e.Assignment.Project.Name : "",
                EvaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : (e.EvaluatedPersonnel != null
                        ? e.EvaluatedPersonnel.FirstName + " " + e.EvaluatedPersonnel.LastName
                        : e.EvaluatedUnknownPersonnel),
                EvaluationDate = e.CompletedAt ?? e.ControlDate,
                CallId = e.CallId,
                ScorePercentage = e.ScorePercentage,
                Notes = e.Notes,
                EvaluationComment = e.EvaluationComment
            })
            .ToListAsync();

        summary.EvaluationNotesCount = evaluationNotes.Count;

        return new SuggestionsReportResultDto
        {
            Summary = summary,
            Suggestions = suggestionDtos,
            EvaluationNotes = evaluationNotes,
            TotalCount = totalCount,
            EvaluationNotesCount = evaluationNotes.Count,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<QuestionSuggestionSummaryDto>> GetTopSuggestedQuestionsAsync(SuggestionsFilterDto filter, int top = 10)
    {
        var query = _context.Answers
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Assignment)
                    .ThenInclude(a => a.Project)
            .Include(a => a.Question)
                .ThenInclude(q => q.Checklist)
            .Where(a => !string.IsNullOrEmpty(a.Notes) || !string.IsNullOrEmpty(a.RecommendationNotes))
            .Where(a => a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        // Varsayılan proje tipi filtresi: Çağrı Denetimi (proje filtresi yoksa)
        if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(a => a.Evaluation.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(a => filter.ProjectIds.Contains(a.Evaluation.Assignment.ProjectId));

        if (filter.CustomerIds?.Any() == true)
            query = query.Where(a => a.Evaluation.Assignment.Project.CustomerId.HasValue && filter.CustomerIds.Contains(a.Evaluation.Assignment.Project.CustomerId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(a => filter.ChecklistIds.Contains(a.Evaluation.Assignment.ChecklistId));

        // DateRanges pattern (UTC dönüşümü Service'de)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(a => a.Evaluation.CompletedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(a => a.Evaluation.CompletedAt <= maxEnd);
        }

        var answers = await query.ToListAsync();

        // Önce not/öneri yazılmış cevapları grupla
        var suggestionsGrouped = answers
            .Where(a => a.Question != null)
            .GroupBy(a => new
            {
                a.QuestionId,
                QuestionText = a.Question!.Text,
                GroupName = a.Question.GroupName ?? "",
                ChecklistName = a.Question.Checklist?.Name ?? ""
            })
            .Select(g => new
            {
                g.Key.QuestionId,
                g.Key.QuestionText,
                g.Key.GroupName,
                g.Key.ChecklistName,
                SuggestionCount = g.Count(),
                AverageScore = g.Where(a => a.EarnedPoints.HasValue && a.Question?.WeightPoints > 0).Any()
                    ? Math.Round(g.Where(a => a.EarnedPoints.HasValue && a.Question?.WeightPoints > 0)
                        .Average(a => (a.EarnedPoints!.Value / a.Question!.WeightPoints) * 100), 1)
                    : 0
            })
            .OrderByDescending(q => q.SuggestionCount)
            .Take(top)
            .ToList();

        // Bu soruların sorulduğu toplam değerlendirme sayısını al (not yazılmış olsun/olmasın)
        var questionIds = suggestionsGrouped.Select(q => q.QuestionId).ToList();

        // Tüm değerlendirmeler üzerinden bu soruların kaç kez cevaplandığını hesapla
        var totalEvaluationCounts = await _context.Answers
            .Where(a => questionIds.Contains(a.QuestionId))
            .Where(a => a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            // Aynı filtreleri uygula
            .Where(a => filter.ProjectIds == null || !filter.ProjectIds.Any()
                ? a.Evaluation.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing
                : filter.ProjectIds.Contains(a.Evaluation.Assignment.ProjectId))
            .Where(a => filter.CustomerIds == null || !filter.CustomerIds.Any()
                || (a.Evaluation.Assignment.Project.CustomerId.HasValue && filter.CustomerIds.Contains(a.Evaluation.Assignment.Project.CustomerId.Value)))
            .GroupBy(a => a.QuestionId)
            .Select(g => new { QuestionId = g.Key, TotalEvaluationCount = g.Select(a => a.EvaluationId).Distinct().Count() })
            .ToListAsync();

        var evalCountDict = totalEvaluationCounts.ToDictionary(x => x.QuestionId, x => x.TotalEvaluationCount);

        return suggestionsGrouped.Select(q => new QuestionSuggestionSummaryDto
        {
            QuestionId = q.QuestionId,
            QuestionText = q.QuestionText,
            GroupName = q.GroupName,
            ChecklistName = q.ChecklistName,
            SuggestionCount = q.SuggestionCount,
            EvaluationCount = evalCountDict.GetValueOrDefault(q.QuestionId, q.SuggestionCount),
            AverageScore = q.AverageScore
        }).ToList();
    }

    /// <summary>
    /// En çok seçilen alt kriterler (SubCriteria)
    /// </summary>
    public async Task<IEnumerable<SubCriteriaSummaryDto>> GetTopSubCriteriaAsync(SuggestionsFilterDto filter, int top = 10)
    {
        var query = _context.AnswerSubCriteriaSelections
            .Include(s => s.SubCriteria)
                .ThenInclude(sc => sc.Question)
                    .ThenInclude(q => q.Checklist)
            .Include(s => s.Answer)
                .ThenInclude(a => a.Evaluation)
                    .ThenInclude(e => e.Assignment)
                        .ThenInclude(a => a.Project)
            .Where(s => s.Answer.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            .Where(s => s.SubCriteria != null && !string.IsNullOrEmpty(s.SubCriteria.Description))
            .AsQueryable();

        // Varsayılan proje tipi filtresi: Çağrı Denetimi (proje filtresi yoksa)
        if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(s => s.Answer.Evaluation.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(s => filter.ProjectIds.Contains(s.Answer.Evaluation.Assignment.ProjectId));

        if (filter.CustomerIds?.Any() == true)
            query = query.Where(s => s.Answer.Evaluation.Assignment.Project.CustomerId.HasValue &&
                filter.CustomerIds.Contains(s.Answer.Evaluation.Assignment.Project.CustomerId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(s => filter.ChecklistIds.Contains(s.Answer.Evaluation.Assignment.ChecklistId));

        // DateRanges filter
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(s => s.Answer.Evaluation.CompletedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(s => s.Answer.Evaluation.CompletedAt <= maxEnd);
        }

        var selections = await query.ToListAsync();

        // SubCriteria bazında grupla
        var grouped = selections
            .Where(s => s.SubCriteria?.Question != null)
            .GroupBy(s => new
            {
                s.SubCriteriaId,
                QuestionId = s.SubCriteria!.QuestionId,
                Description = s.SubCriteria!.Description,
                QuestionText = s.SubCriteria.Question!.Text,
                GroupName = s.SubCriteria.Question.GroupName ?? "",
                ChecklistName = s.SubCriteria.Question.Checklist?.Name ?? ""
            })
            .Select(g => new SubCriteriaSummaryDto
            {
                SubCriteriaId = g.Key.SubCriteriaId,
                QuestionId = g.Key.QuestionId,
                Description = g.Key.Description,
                QuestionText = g.Key.QuestionText,
                GroupName = g.Key.GroupName,
                ChecklistName = g.Key.ChecklistName,
                SelectionCount = g.Count(),
                EvaluationCount = g.Select(s => s.Answer.EvaluationId).Distinct().Count()
            })
            .OrderByDescending(x => x.SelectionCount)
            .Take(top)
            .ToList();

        // Her soru için toplam değerlendirme sayısını hesapla (aynı filtrelerle)
        var questionIds = grouped.Select(g => g.QuestionId).Distinct().ToList();
        if (questionIds.Any())
        {
            // Answer tablosundan bu soruların kaç değerlendirmede sorulduğunu hesapla
            var answerQuery = _context.Answers
                .Include(a => a.Evaluation)
                    .ThenInclude(e => e.Assignment)
                        .ThenInclude(a => a.Project)
                .Where(a => questionIds.Contains(a.QuestionId))
                .Where(a => a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
                .AsQueryable();

            // Aynı filtreleri uygula
            if (filter.ProjectIds?.Any() == true)
                answerQuery = answerQuery.Where(a => filter.ProjectIds.Contains(a.Evaluation.Assignment.ProjectId));
            else
                answerQuery = answerQuery.Where(a => a.Evaluation.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);

            if (filter.CustomerIds?.Any() == true)
                answerQuery = answerQuery.Where(a => a.Evaluation.Assignment.Project.CustomerId.HasValue &&
                    filter.CustomerIds.Contains(a.Evaluation.Assignment.Project.CustomerId.Value));

            if (filter.ChecklistIds?.Any() == true)
                answerQuery = answerQuery.Where(a => filter.ChecklistIds.Contains(a.Evaluation.Assignment.ChecklistId));

            // Tarih filtreleri
            if (filter.DateRanges?.Any() == true)
            {
                var datePredicates = filter.DateRanges.Select(dr =>
                {
                    DateTime? startUtc = dr.StartDate.HasValue
                        ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                        : null;
                    DateTime? endUtc = dr.EndDate.HasValue
                        ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                        : null;
                    return (Start: startUtc, End: endUtc);
                }).ToList();

                var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
                var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

                if (minStart != DateTime.MinValue)
                    answerQuery = answerQuery.Where(a => a.Evaluation.CompletedAt >= minStart);
                if (maxEnd != DateTime.MaxValue)
                    answerQuery = answerQuery.Where(a => a.Evaluation.CompletedAt <= maxEnd);
            }

            var questionEvalCounts = await answerQuery
                .GroupBy(a => a.QuestionId)
                .Select(g => new { QuestionId = g.Key, Count = g.Select(a => a.EvaluationId).Distinct().Count() })
                .ToDictionaryAsync(x => x.QuestionId, x => x.Count);

            // Sonuçları birleştir
            foreach (var item in grouped)
            {
                item.TotalQuestionEvaluations = questionEvalCounts.GetValueOrDefault(item.QuestionId, 0);
            }
        }

        return grouped;
    }

    public async Task<ExcelExportDto> ExportSuggestionsToExcelAsync(SuggestionsFilterDto filter, bool excludeEvaluator = false)
    {
        // Remove pagination for export
        filter.Page = 1;
        filter.PageSize = 10000;

        var report = await GetSuggestionsReportAsync(filter);
        var topQuestions = await GetTopSuggestedQuestionsAsync(filter, 20);

        using var workbook = new XLWorkbook();

        // Summary sheet
        var summarySheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.Summary", defaultValue: "Özet"));
        summarySheet.Cell(1, 1).Value = await _localizationService.GetResourceAsync("Report.SuggestionsReport", defaultValue: "ÖNERİLER RAPORU");
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 16;

        summarySheet.Cell(3, 1).Value = await _localizationService.GetResourceAsync("Report.TotalSuggestions", defaultValue: "Toplam Öneri/Not:");
        summarySheet.Cell(3, 2).Value = report.Summary.TotalSuggestions;
        summarySheet.Cell(4, 1).Value = await _localizationService.GetResourceAsync("Report.EvaluationsWithSuggestions", defaultValue: "Önerili Değerlendirme Sayısı:");
        summarySheet.Cell(4, 2).Value = report.Summary.TotalEvaluationsWithSuggestions;

        int summaryRow = 5;
        if (!excludeEvaluator)
        {
            summarySheet.Cell(summaryRow, 1).Value = await _localizationService.GetResourceAsync("Report.UniqueEvaluators", defaultValue: "Değerlendirici Sayısı:");
            summarySheet.Cell(summaryRow, 2).Value = report.Summary.UniqueEvaluators;
            summaryRow++;
        }

        summarySheet.Cell(summaryRow, 1).Value = await _localizationService.GetResourceAsync("Report.UniquePersonnel", defaultValue: "Personel Sayısı:");
        summarySheet.Cell(summaryRow, 2).Value = report.Summary.UniquePersonnel;
        summaryRow++;
        summarySheet.Cell(summaryRow, 1).Value = await _localizationService.GetResourceAsync("Report.ReportDate", defaultValue: "Rapor Tarihi:");
        summarySheet.Cell(summaryRow, 2).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        summarySheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(summarySheet);

        // Details sheet - Değerlendirici kolonu excludeEvaluator true ise eklenmez
        var detailsSheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.SuggestionsList", defaultValue: "Öneriler Listesi"));

        var headersList = new List<string>
        {
            await _localizationService.GetResourceAsync("Report.Date", defaultValue: "Tarih"),
            await _localizationService.GetResourceAsync("Report.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.Checklist", defaultValue: "Kontrol Listesi"),
            await _localizationService.GetResourceAsync("Report.Section", defaultValue: "Bölüm"),
            await _localizationService.GetResourceAsync("Report.Question", defaultValue: "Soru"),
            await _localizationService.GetResourceAsync("Report.Notes", defaultValue: "Notlar"),
            await _localizationService.GetResourceAsync("Report.Suggestion", defaultValue: "Öneri"),
            await _localizationService.GetResourceAsync("Report.GivenPoints", defaultValue: "Verilen Puan"),
            await _localizationService.GetResourceAsync("Report.MaxPoints", defaultValue: "Maks Puan"),
            await _localizationService.GetResourceAsync("Report.Percentage", defaultValue: "Yüzde")
        };

        if (!excludeEvaluator)
        {
            headersList.Add(await _localizationService.GetResourceAsync("Report.Evaluator", defaultValue: "Değerlendirici"));
        }

        headersList.Add(await _localizationService.GetResourceAsync("Report.Personnel", defaultValue: "Personel"));
        headersList.Add(await _localizationService.GetResourceAsync("Report.CallId", defaultValue: "Çağrı ID"));
        headersList.Add(await _localizationService.GetResourceAsync("Report.Penalty", defaultValue: "Ceza"));

        var headers = headersList.ToArray();

        for (int i = 0; i < headers.Length; i++)
        {
            detailsSheet.Cell(1, i + 1).Value = headers[i];
            detailsSheet.Cell(1, i + 1).Style.Font.Bold = true;
            detailsSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var item in report.Suggestions)
        {
            int col = 1;
            detailsSheet.Cell(row, col++).Value = item.EvaluationDate?.ToString("dd.MM.yyyy") ?? "";
            detailsSheet.Cell(row, col++).Value = item.ProjectName;
            detailsSheet.Cell(row, col++).Value = item.ChecklistName;
            detailsSheet.Cell(row, col++).Value = item.GroupName;
            detailsSheet.Cell(row, col++).Value = item.QuestionText;
            detailsSheet.Cell(row, col++).Value = item.Notes ?? "";
            detailsSheet.Cell(row, col++).Value = item.RecommendationNotes ?? "";
            detailsSheet.Cell(row, col++).Value = item.GivenPoints ?? 0;
            detailsSheet.Cell(row, col++).Value = item.MaxPoints ?? 0;
            detailsSheet.Cell(row, col++).Value = item.PercentageScore.HasValue ? $"{item.PercentageScore:F1}%" : "";

            if (!excludeEvaluator)
            {
                detailsSheet.Cell(row, col++).Value = item.EvaluatorName ?? "";
            }

            detailsSheet.Cell(row, col++).Value = item.EvaluatedPersonnelName ?? "";
            detailsSheet.Cell(row, col++).Value = item.CallId ?? "";
            detailsSheet.Cell(row, col++).Value = item.PenaltyType ?? "";
            row++;
        }

        detailsSheet.Columns().AdjustToContents();
        // Notlar: 6, Öneri: 7, CallId: excludeEvaluator'a göre 12 veya 13
        var callIdCol = excludeEvaluator ? 12 : 13;
        ExcelHelper.ApplyLongTextColumnStyles(detailsSheet, callIdColumns: new[] { callIdCol }, noteColumns: new[] { 6, 7 });

        // Top Questions sheet
        var questionsSheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.TopSuggestedQuestions", defaultValue: "Top Önerilen Sorular"));
        questionsSheet.Cell(1, 1).Value = await _localizationService.GetResourceAsync("Report.Question", defaultValue: "Soru");
        questionsSheet.Cell(1, 1).Style.Font.Bold = true;
        questionsSheet.Cell(1, 2).Value = await _localizationService.GetResourceAsync("Report.Checklist", defaultValue: "Kontrol Listesi");
        questionsSheet.Cell(1, 2).Style.Font.Bold = true;
        questionsSheet.Cell(1, 3).Value = await _localizationService.GetResourceAsync("Report.Section", defaultValue: "Bölüm");
        questionsSheet.Cell(1, 3).Style.Font.Bold = true;
        questionsSheet.Cell(1, 4).Value = await _localizationService.GetResourceAsync("Report.SuggestionCount", defaultValue: "Öneri Sayısı");
        questionsSheet.Cell(1, 4).Style.Font.Bold = true;
        questionsSheet.Cell(1, 5).Value = await _localizationService.GetResourceAsync("Report.AverageScore", defaultValue: "Ort. Puan");
        questionsSheet.Cell(1, 5).Style.Font.Bold = true;

        row = 2;
        foreach (var q in topQuestions)
        {
            questionsSheet.Cell(row, 1).Value = q.QuestionText;
            questionsSheet.Cell(row, 2).Value = q.ChecklistName;
            questionsSheet.Cell(row, 3).Value = q.GroupName;
            questionsSheet.Cell(row, 4).Value = q.SuggestionCount;
            questionsSheet.Cell(row, 5).Value = $"{q.AverageScore:F1}%";
            row++;
        }

        questionsSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(questionsSheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Oneriler_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== ÇAĞRI DENETLEME RAPORU =====

    public async Task<ExcelExportDto> ExportCallAuditReportAsync(ReportFilterDto filter)
    {
        // Remove pagination for export
        filter.Page = 1;
        filter.PageSize = 10000;
        filter.ForExport = true; // PRENSIP: Taslaklar rapora dahil edilmez

        var result = await GetEvaluationsAsync(filter);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.CallAudit", defaultValue: "Çağrı Denetleme"));

        // Headers - Kullanıcının istediği sütunlar
        var headers = new[]
        {
            await _localizationService.GetResourceAsync("Report.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.EvaluatorName", defaultValue: "Değerlendirme Yapan"),
            await _localizationService.GetResourceAsync("Report.Person", defaultValue: "Kişi"),
            await _localizationService.GetResourceAsync("Report.CallNo", defaultValue: "Çağrı No"),
            await _localizationService.GetResourceAsync("Report.ControlDate", defaultValue: "Kontrol Tarihi"),
            await _localizationService.GetResourceAsync("Report.Time", defaultValue: "Saat"),
            await _localizationService.GetResourceAsync("Report.ListeningDate", defaultValue: "Dinleme Tarihi"),
            await _localizationService.GetResourceAsync("Report.ListeningTime", defaultValue: "Dinleme Saati"),
            await _localizationService.GetResourceAsync("Report.Duration", defaultValue: "Süre"),
            await _localizationService.GetResourceAsync("Report.Comment", defaultValue: "Yorum"),
            await _localizationService.GetResourceAsync("Report.PeriodMonth", defaultValue: "Periyot (Ay)"),
            await _localizationService.GetResourceAsync("Report.AverageScore", defaultValue: "Ortalama Puan")
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        int row = 2;
        foreach (var item in result.Items)
        {
            // Periyot hesaplama (YYYYMM formatında)
            var period = item.CallDate?.ToString("yyyyMM") ?? item.EvaluationDate?.ToString("yyyyMM") ?? "";

            worksheet.Cell(row, 1).Value = item.ProjectName ?? "";
            worksheet.Cell(row, 2).Value = item.EvaluatorName ?? "";
            worksheet.Cell(row, 3).Value = item.EvaluatedPersonnelName ?? "";
            worksheet.Cell(row, 4).Value = item.CallId ?? "";
            worksheet.Cell(row, 5).Value = item.CallDate?.ToString("dd.MM.yyyy") ?? item.EvaluationDate?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 6).Value = item.CallTime ?? (item.CallDate?.ToString("HH:mm") ?? "");
            worksheet.Cell(row, 7).Value = item.CreatedAt.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 8).Value = item.CreatedAt.ToString("HH:mm");
            worksheet.Cell(row, 9).Value = item.Duration ?? "";
            worksheet.Cell(row, 10).Value = item.Comment ?? "";
            worksheet.Cell(row, 11).Value = period;
            worksheet.Cell(row, 12).Value = item.ScorePercentage ?? 0;

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet, callIdColumns: new[] { 4 }, noteColumns: new[] { 10 });

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Cagri_Denetleme_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    /// <summary>
    /// Tarihten ay/yıl formatında string döndürür (Ocak 2026)
    /// Months TypeDefinition ve ILocalizationService kullanarak yerelleştirilmiş ay adı döner
    /// </summary>
    private string FormatMonthYear(DateTime date)
    {
        var month = Months.GetByDate(date);
        if (month == null)
            return date.ToString("MMMM yyyy", System.Globalization.CultureInfo.CurrentCulture);

        var monthName = _localizationService.GetResourceAsync(month.NameResourceKey, defaultValue: month.SystemName).Result;
        return $"{monthName} {date.Year}";
    }

    // ===== SORU GRUBU ORTALAMA RAPORU =====

    public async Task<ExcelExportDto> ExportQuestionGroupAverageReportAsync(ReportFilterDto filter)
    {
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.AssignmentPeriod)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));

        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Assignment.Project.ProjectTypeId));
        }
        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(e => filter.ChecklistIds.Contains(e.Assignment.ChecklistId));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(e => e.CompletedAt >= minStart || e.CreatedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(e => e.CompletedAt <= maxEnd || e.CreatedAt <= maxEnd);
        }

        // Customer filter (çoklu)
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                filter.CustomerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerIds?.Any() == true)
            query = query.Where(e => e.Assignment.Project.CustomerId.HasValue && filter.ProjectCustomerIds.Contains(e.Assignment.Project.CustomerId.Value));

        // Organization filter (çoklu)
        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa =>
                    filter.OrganizationIds.Contains(oa.CustomerOrganizationId)));

        // Period filter (çoklu)
        if (filter.PeriodIds?.Any() == true)
            query = query.Where(e => e.AssignmentPeriodId.HasValue && filter.PeriodIds.Contains(e.AssignmentPeriodId.Value));

        // Evaluation source filter (çoklu)
        if (filter.EvaluationSources?.Any() == true)
        {
            var hasInternal = filter.EvaluationSources.Contains("internal");
            var hasOurs = filter.EvaluationSources.Contains("ours");

            if (hasInternal && !hasOurs)
                query = query.Where(e => e.Assignment.TypeId == AssignmentTypes.Ids.CustomerPersonnel);
            else if (hasOurs && !hasInternal)
                query = query.Where(e => e.Assignment.TypeId != AssignmentTypes.Ids.CustomerPersonnel);
        }

        var evaluations = await query.Take(10000).ToListAsync();

        // Flatten to answer level and calculate group averages
        var groupData = evaluations
            .SelectMany(e => e.Answers
                .Where(a => a.Question != null && !string.IsNullOrEmpty(a.Question.GroupName))
                .Select(a => new
                {
                    ProjectName = e.Assignment.Project?.Name ?? "",
                    PeriodName = e.AssignmentPeriod?.Name ?? FormatMonthYear(e.CallDate ?? e.CompletedAt ?? e.CreatedAt),
                    Year = (e.CallDate ?? e.CompletedAt ?? e.CreatedAt).Year,
                    GroupOrder = a.Question!.GroupName!.Split(' ').FirstOrDefault() ?? "",
                    GroupName = a.Question.GroupName,
                    EarnedPoints = a.EarnedPoints ?? 0,
                    MaxPoints = a.Question.WeightPoints,
                    EvaluationId = e.Id
                }))
            .GroupBy(x => new { x.ProjectName, x.PeriodName, x.Year, x.GroupName })
            .Select(g => new QuestionGroupAverageReportDto
            {
                ProjectName = $"{g.Key.ProjectName} {g.Key.PeriodName}",
                GroupName = g.Key.GroupName,
                Year = g.Key.Year,
                EvaluationCount = g.Select(x => x.EvaluationId).Distinct().Count(),
                AverageScore = g.Sum(x => x.MaxPoints) > 0
                    ? Math.Round(g.Sum(x => x.EarnedPoints) / g.Sum(x => x.MaxPoints) * 100, 2)
                    : 0
            })
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.GroupName)
            .ToList();

        // Excel oluştur
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.QuestionGroupAverage", defaultValue: "Soru Grubu Ortalama"));

        // Headers
        var headers = new[] {
            await _localizationService.GetResourceAsync("Report.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.QuestionGroup", defaultValue: "Kontrol Grubu"),
            await _localizationService.GetResourceAsync("Report.Period", defaultValue: "Periyot"),
            await _localizationService.GetResourceAsync("Report.ListeningCount", defaultValue: "Dinleme Sayısı"),
            await _localizationService.GetResourceAsync("Report.AverageScore", defaultValue: "Ortalama Puan")
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        int row = 2;
        foreach (var item in groupData)
        {
            worksheet.Cell(row, 1).Value = item.ProjectName;
            worksheet.Cell(row, 2).Value = item.GroupName;
            worksheet.Cell(row, 3).Value = item.Year;
            worksheet.Cell(row, 4).Value = item.EvaluationCount;
            worksheet.Cell(row, 5).Value = item.AverageScore;
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "0.00";
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet);

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Soru_Grubu_Ortalama_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== MÜŞTERİ DEĞERLENDİRME RAPORU =====

    public async Task<ExcelExportDto> ExportCustomerEvaluationReportAsync(ReportFilterDto filter)
    {
        // PRENSIP: Taslaklar rapora dahil edilmez
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.AssignmentPeriod)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.Customer)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.OrganizationAssignments)
                    .ThenInclude(oa => oa.CustomerOrganization)
            .Include(e => e.EvaluatedPersonnel)
            .Include(e => e.Answers)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));

        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Assignment.Project.ProjectTypeId));
        }
        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(e => filter.ChecklistIds.Contains(e.Assignment.ChecklistId));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(e => e.CompletedAt >= minStart || e.CreatedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(e => e.CompletedAt <= maxEnd || e.CreatedAt <= maxEnd);
        }

        // Status filter (çoklu)
        if (filter.Statuses?.Any() == true)
        {
            var statusIds = filter.Statuses
                .Select(s => EvaluationStatuses.GetBySystemName(s))
                .Where(s => s != null)
                .Select(s => s!.Id)
                .ToList();
            if (statusIds.Any())
                query = query.Where(e => statusIds.Contains(e.StatusId));
        }
        // PRENSIP: Varsayılan olarak sadece Completed değerlendirmeler dahil edilir (taslaklar hariç)
        else
        {
            query = query.Where(e => e.StatusId == EvaluationStatuses.Ids.Completed);
        }

        // Evaluation source filter (çoklu)
        if (filter.EvaluationSources?.Any() == true)
        {
            var hasInternal = filter.EvaluationSources.Contains("internal");
            var hasOurs = filter.EvaluationSources.Contains("ours");

            if (hasInternal && !hasOurs)
                query = query.Where(e => e.Assignment.TypeId == AssignmentTypes.Ids.CustomerPersonnel);
            else if (hasOurs && !hasInternal)
                query = query.Where(e => e.Assignment.TypeId != AssignmentTypes.Ids.CustomerPersonnel);
        }

        // Customer filter (çoklu)
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                filter.CustomerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerIds?.Any() == true)
            query = query.Where(e => e.Assignment.Project.CustomerId.HasValue && filter.ProjectCustomerIds.Contains(e.Assignment.Project.CustomerId.Value));

        // Organization filter (çoklu)
        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa =>
                    filter.OrganizationIds.Contains(oa.CustomerOrganizationId)));

        // Period filter (çoklu)
        if (filter.PeriodIds?.Any() == true)
            query = query.Where(e => e.AssignmentPeriodId.HasValue && filter.PeriodIds.Contains(e.AssignmentPeriodId.Value));

        // Evaluated Personnel name search (çoklu - OR mantığı)
        if (filter.PersonnelNames?.Any() == true)
        {
            query = query.Where(e =>
                filter.PersonnelNames.Any(name =>
                    (e.EvaluatedCustomerPersonnel != null &&
                        (EF.Functions.ILike(e.EvaluatedCustomerPersonnel.FirstName, $"%{name}%") ||
                         EF.Functions.ILike(e.EvaluatedCustomerPersonnel.LastName, $"%{name}%"))) ||
                    (e.EvaluatedUnknownPersonnel != null && EF.Functions.ILike(e.EvaluatedUnknownPersonnel, $"%{name}%"))));
        }

        // CallId search (çoklu - OR mantığı)
        if (filter.CallIds?.Any() == true)
            query = query.Where(e => e.CallId != null &&
                filter.CallIds.Any(callId => EF.Functions.ILike(e.CallId, $"%{callId}%")));

        var evaluations = await query
            .OrderByDescending(e => e.CallDate ?? e.CompletedAt ?? e.CreatedAt)
            .Take(10000)
            .ToListAsync();

        // Excel oluştur
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.CustomerEvaluation", defaultValue: "Müşteri Değerlendirme"));

        // Headers
        var headers = new[]
        {
            await _localizationService.GetResourceAsync("Report.Company", defaultValue: "Firma"),
            await _localizationService.GetResourceAsync("Report.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.Evaluated", defaultValue: "Değerlendirilen"),
            await _localizationService.GetResourceAsync("Report.Person", defaultValue: "Kişi"),
            await _localizationService.GetResourceAsync("Report.Department", defaultValue: "Departman"),
            await _localizationService.GetResourceAsync("Report.CallNo", defaultValue: "Çağrı No"),
            await _localizationService.GetResourceAsync("Report.ControlDate", defaultValue: "Kontrol Tarihi"),
            await _localizationService.GetResourceAsync("Report.Time", defaultValue: "Saat"),
            await _localizationService.GetResourceAsync("Report.Duration", defaultValue: "Süre"),
            await _localizationService.GetResourceAsync("Report.Comment", defaultValue: "Yorum"),
            await _localizationService.GetResourceAsync("Report.Period", defaultValue: "Periyot"),
            await _localizationService.GetResourceAsync("Report.PeriodMonth", defaultValue: "Periyot (Ay)"),
            await _localizationService.GetResourceAsync("Report.TotalScore", defaultValue: "Toplam Puan"),
            await _localizationService.GetResourceAsync("Report.Description", defaultValue: "Açıklama")
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        int row = 2;
        foreach (var e in evaluations)
        {
            // Firma
            var customerName = e.EvaluatedCustomerPersonnel?.Customer?.CompanyName ?? "";

            // Değerlendirilen: Period varsa period adı, yoksa AyYıl + Company
            var evalDate = e.CallDate ?? e.CompletedAt ?? e.CreatedAt;
            var degerlendirilenmStr = e.AssignmentPeriod != null
                ? e.AssignmentPeriod.Name
                : $"{FormatMonthYear(evalDate)} - {customerName}";

            // Kişi
            var personnelName = e.EvaluatedCustomerPersonnel != null
                ? $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}"
                : (e.EvaluatedPersonnel != null
                    ? $"{e.EvaluatedPersonnel.FirstName} {e.EvaluatedPersonnel.LastName}"
                    : e.EvaluatedUnknownPersonnel ?? "");

            // Departman
            var departmentName = e.EvaluatedCustomerPersonnel?.OrganizationAssignments != null
                ? string.Join(", ", e.EvaluatedCustomerPersonnel.OrganizationAssignments
                    .Where(oa => oa.CustomerOrganization != null)
                    .Select(oa => oa.CustomerOrganization!.Name))
                : "";

            // Yorum: Tüm answer notes + genel yorum (virgülle birleşik)
            var allComments = new List<string>();
            if (e.Answers != null)
            {
                var answerNotes = e.Answers
                    .Where(a => !string.IsNullOrWhiteSpace(a.Notes))
                    .Select(a => a.Notes!)
                    .ToList();
                allComments.AddRange(answerNotes);
            }
            if (!string.IsNullOrWhiteSpace(e.EvaluationComment))
            {
                allComments.Add(e.EvaluationComment);
            }
            var combinedComment = allComments.Count > 0 ? string.Join(", ", allComments) : "-";

            // Periyot (Ay) - YYYYMM formatı
            var periodMonth = evalDate.ToString("yyyyMM");

            worksheet.Cell(row, 1).Value = customerName;
            worksheet.Cell(row, 2).Value = e.Assignment.Project?.Name ?? "";
            worksheet.Cell(row, 3).Value = degerlendirilenmStr;
            worksheet.Cell(row, 4).Value = personnelName;
            worksheet.Cell(row, 5).Value = departmentName;
            worksheet.Cell(row, 6).Value = e.CallId ?? "";
            worksheet.Cell(row, 7).Value = (e.ControlDate ?? e.CallDate)?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 8).Value = e.CallTime ?? (e.CallDate?.ToString("HH:mm") ?? "");
            worksheet.Cell(row, 9).Value = e.Duration ?? "";
            // Açıklama: DescriptionsJson (+ ile eklenen açıklamalar) + EvaluationComment
            var allDescriptions = new List<string>();
            if (!string.IsNullOrWhiteSpace(e.DescriptionsJson))
            {
                try
                {
                    var descriptions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(e.DescriptionsJson);
                    if (descriptions != null)
                    {
                        allDescriptions.AddRange(descriptions.Where(d => !string.IsNullOrWhiteSpace(d)));
                    }
                }
                catch { /* JSON parse hatası - devam et */ }
            }
            if (!string.IsNullOrWhiteSpace(e.EvaluationComment))
            {
                allDescriptions.Add(e.EvaluationComment);
            }
            var combinedDescription = allDescriptions.Count > 0 ? string.Join(", ", allDescriptions) : "-";

            worksheet.Cell(row, 10).Value = combinedComment;
            worksheet.Cell(row, 11).Value = evalDate.Year;
            worksheet.Cell(row, 12).Value = periodMonth;
            worksheet.Cell(row, 13).Value = e.ScorePercentage ?? 0;
            worksheet.Cell(row, 13).Style.NumberFormat.Format = "0.00";
            worksheet.Cell(row, 14).Value = combinedDescription;

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet, callIdColumns: new[] { 6 }, noteColumns: new[] { 10, 14 });

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Musteri_Degerlendirme_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== PROJE PERFORMANS RAPORU =====

    public async Task<ExcelExportDto> ExportProjectPerformanceReportAsync(ReportFilterDto filter)
    {
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.AssignmentPeriod)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed && e.ScorePercentage.HasValue)
            .AsQueryable();

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));

        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Assignment.Project.ProjectTypeId));
        }
        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(e => filter.ChecklistIds.Contains(e.Assignment.ChecklistId));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(e => e.CompletedAt >= minStart || e.CreatedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(e => e.CompletedAt <= maxEnd || e.CreatedAt <= maxEnd);
        }

        // Customer filter (çoklu)
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                filter.CustomerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerIds?.Any() == true)
            query = query.Where(e => e.Assignment.Project.CustomerId.HasValue && filter.ProjectCustomerIds.Contains(e.Assignment.Project.CustomerId.Value));

        // Organization filter (çoklu)
        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa =>
                    filter.OrganizationIds.Contains(oa.CustomerOrganizationId)));

        // Period filter (çoklu)
        if (filter.PeriodIds?.Any() == true)
            query = query.Where(e => e.AssignmentPeriodId.HasValue && filter.PeriodIds.Contains(e.AssignmentPeriodId.Value));

        // Evaluation source filter (çoklu)
        if (filter.EvaluationSources?.Any() == true)
        {
            var hasInternal = filter.EvaluationSources.Contains("internal");
            var hasOurs = filter.EvaluationSources.Contains("ours");

            if (hasInternal && !hasOurs)
                query = query.Where(e => e.Assignment.TypeId == AssignmentTypes.Ids.CustomerPersonnel);
            else if (hasOurs && !hasInternal)
                query = query.Where(e => e.Assignment.TypeId != AssignmentTypes.Ids.CustomerPersonnel);
        }

        var evaluations = await query.Take(50000).ToListAsync();

        // Group by Period (Month) + Project and calculate averages
        var projectData = evaluations
            .Select(e => new
            {
                EvalDate = e.CallDate ?? e.CompletedAt ?? e.CreatedAt,
                ProjectName = e.AssignmentPeriod?.Name ?? e.Assignment.Project?.Name ?? "",
                ScorePercentage = e.ScorePercentage!.Value
            })
            .GroupBy(x => new
            {
                PeriodMonth = x.EvalDate.ToString("yyyyMM"),
                Year = x.EvalDate.Year,
                x.ProjectName
            })
            .Select(g => new
            {
                PeriodMonth = g.Key.PeriodMonth,
                ProjectName = g.Key.ProjectName,
                Year = g.Key.Year,
                AverageScore = Math.Round(g.Average(x => x.ScorePercentage), 2),
                EvaluationCount = g.Count()
            })
            .OrderByDescending(x => x.PeriodMonth)
            .ThenByDescending(x => x.AverageScore)
            .ToList();

        // Excel oluştur
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.ProjectPerformance", defaultValue: "Proje Performans"));

        // Headers
        var headers = new[] {
            await _localizationService.GetResourceAsync("Report.PeriodMonth", defaultValue: "Periyot (Ay)"),
            await _localizationService.GetResourceAsync("Report.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.Period", defaultValue: "Periyot"),
            await _localizationService.GetResourceAsync("Report.ListeningCount", defaultValue: "Dinleme Sayısı"),
            await _localizationService.GetResourceAsync("Report.AverageScore", defaultValue: "Ortalama Puan")
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        int row = 2;
        foreach (var item in projectData)
        {
            worksheet.Cell(row, 1).Value = item.PeriodMonth;
            worksheet.Cell(row, 2).Value = item.ProjectName;
            worksheet.Cell(row, 3).Value = item.Year;
            worksheet.Cell(row, 4).Value = item.EvaluationCount;
            worksheet.Cell(row, 5).Value = item.AverageScore;
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "0.00";
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet);

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Proje_Performans_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== MT RAPORU (4 Sheet) =====

    public async Task<ExcelExportDto> ExportMTReportAsync(ReportFilterDto filter)
    {
        // Get evaluations with all necessary includes
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
                    .ThenInclude(p => p!.Customer)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Include(e => e.AssignmentPeriod)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));

        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Assignment.Project.ProjectTypeId));
        }
        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(e => e.CompletedAt >= minStart || e.CreatedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(e => e.CompletedAt <= maxEnd || e.CreatedAt <= maxEnd);
        }

        // Customer filter (çoklu)
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                filter.CustomerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

        // Organization filter (çoklu)
        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue &&
                filter.OrganizationIds.Contains(e.EvaluatedOrganizationId.Value));

        // Period filter (çoklu)
        if (filter.PeriodIds?.Any() == true)
            query = query.Where(e => e.AssignmentPeriodId.HasValue && filter.PeriodIds.Contains(e.AssignmentPeriodId.Value));

        // Evaluation source filter (çoklu)
        if (filter.EvaluationSources?.Any() == true)
        {
            var hasInternal = filter.EvaluationSources.Contains("internal");
            var hasOurs = filter.EvaluationSources.Contains("ours");

            if (hasInternal && !hasOurs)
                query = query.Where(e => e.Assignment.TypeId == AssignmentTypes.Ids.CustomerPersonnel);
            else if (hasOurs && !hasInternal)
                query = query.Where(e => e.Assignment.TypeId != AssignmentTypes.Ids.CustomerPersonnel);
        }

        var evaluations = await query.Take(50000).ToListAsync();

        using var workbook = new XLWorkbook();

        // ===== SHEET 1: MT Başarı =====
        await CreateMTBasariSheet(workbook, evaluations);

        // ===== SHEET 2: MT Gelişim Alanı =====
        await CreateMTGelisimAlaniSheet(workbook, evaluations);

        // ===== SHEET 3: MT Süreç Analizi =====
        await CreateMTSurecAnaliziSheet(workbook, evaluations);

        // ===== SHEET 4: MT Endeks Başarı Analizi =====
        await CreateMTEndeksBasariSheet(workbook, evaluations);

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"MT_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    private async Task CreateMTBasariSheet(XLWorkbook workbook, List<Core.Entities.Evaluation> evaluations)
    {
        var sheetName = await _localizationService.GetResourceAsync("Report.Sheet.MTBasari", defaultValue: "MT Başarı");
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Headers
        var headers = new[]
        {
            await _localizationService.GetResourceAsync("Report.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.Representative", defaultValue: "Müşteri Temsilcisi"),
            await _localizationService.GetResourceAsync("Report.Department", defaultValue: "Departman"),
            await _localizationService.GetResourceAsync("Report.Period", defaultValue: "Periyot"),
            await _localizationService.GetResourceAsync("Report.AverageScore", defaultValue: "Ortalama Puan"),
            await _localizationService.GetResourceAsync("Report.TotalCallCount", defaultValue: "Toplam Çağrı Sayısı")
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Group by Project, Personnel, Department, Year
        var groupedData = evaluations
            .Where(e => e.ScorePercentage.HasValue)
            .Select(e => new
            {
                ProjectName = e.Assignment.Project?.Name ?? "",
                PersonnelName = e.EvaluatedCustomerPersonnel != null
                    ? $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}"
                    : e.EvaluatedUnknownPersonnel ?? "-",
                Department = e.EvaluatedOrganization?.Name ?? e.Assignment.Project?.Customer?.CompanyName ?? "-",
                Year = (e.CallDate ?? e.CompletedAt ?? e.CreatedAt).Year,
                Score = e.ScorePercentage!.Value
            })
            .GroupBy(x => new { x.ProjectName, x.PersonnelName, x.Department, x.Year })
            .Select(g => new
            {
                g.Key.ProjectName,
                g.Key.PersonnelName,
                g.Key.Department,
                g.Key.Year,
                AverageScore = Math.Round(g.Average(x => x.Score), 2),
                CallCount = g.Count()
            })
            .OrderByDescending(x => x.AverageScore)
            .ToList();

        int row = 2;
        foreach (var item in groupedData)
        {
            worksheet.Cell(row, 1).Value = item.ProjectName;
            worksheet.Cell(row, 2).Value = item.PersonnelName;
            worksheet.Cell(row, 3).Value = item.Department;
            worksheet.Cell(row, 4).Value = item.Year;
            worksheet.Cell(row, 5).Value = item.AverageScore;
            worksheet.Cell(row, 6).Value = item.CallCount;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet);
    }

    private async Task CreateMTGelisimAlaniSheet(XLWorkbook workbook, List<Core.Entities.Evaluation> evaluations)
    {
        var sheetName = await _localizationService.GetResourceAsync("Report.Sheet.MTGelisimAlani", defaultValue: "MT Gelişim Alanı");
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Headers
        var headers = new[]
        {
            await _localizationService.GetResourceAsync("Report.Representative", defaultValue: "Müşteri Temsilcisi"),
            await _localizationService.GetResourceAsync("Report.Department", defaultValue: "Departman"),
            await _localizationService.GetResourceAsync("Report.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.Period", defaultValue: "Periyot"),
            await _localizationService.GetResourceAsync("Report.PeriodMonth", defaultValue: "Periyot (Ay)"),
            await _localizationService.GetResourceAsync("Report.Suggestion", defaultValue: "Öneri"),
            await _localizationService.GetResourceAsync("Report.CallCount", defaultValue: "Çağrı Sayısı")
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Get evaluation IDs for filtering
        var evaluationIds = evaluations.Select(e => e.Id).ToList();

        // Query suggestions directly from database
        // 1. SubCriteriaSelections
        var subCriteriaSuggestions = await _context.AnswerSubCriteriaSelections
            .Include(s => s.SubCriteria)
            .Include(s => s.Answer)
                .ThenInclude(a => a.Evaluation)
                    .ThenInclude(e => e.Assignment)
                        .ThenInclude(a => a.Project)
                            .ThenInclude(p => p!.Customer)
            .Include(s => s.Answer)
                .ThenInclude(a => a.Evaluation)
                    .ThenInclude(e => e.EvaluatedCustomerPersonnel)
            .Include(s => s.Answer)
                .ThenInclude(a => a.Evaluation)
                    .ThenInclude(e => e.EvaluatedOrganization)
            .Where(s => evaluationIds.Contains(s.Answer.EvaluationId))
            .Where(s => s.SubCriteria != null && s.SubCriteria.Description != null && s.SubCriteria.Description != "")
            .Select(s => new
            {
                PersonnelName = s.Answer.Evaluation.EvaluatedCustomerPersonnel != null
                    ? s.Answer.Evaluation.EvaluatedCustomerPersonnel.FirstName + " " + s.Answer.Evaluation.EvaluatedCustomerPersonnel.LastName
                    : s.Answer.Evaluation.EvaluatedUnknownPersonnel ?? "-",
                Department = s.Answer.Evaluation.EvaluatedOrganization != null
                    ? s.Answer.Evaluation.EvaluatedOrganization.Name
                    : (s.Answer.Evaluation.Assignment.Project != null && s.Answer.Evaluation.Assignment.Project.Customer != null
                        ? s.Answer.Evaluation.Assignment.Project.Customer.CompanyName
                        : "-"),
                ProjectName = s.Answer.Evaluation.Assignment.Project != null ? s.Answer.Evaluation.Assignment.Project.Name : "",
                EvalDate = s.Answer.Evaluation.CallDate ?? s.Answer.Evaluation.CompletedAt ?? s.Answer.Evaluation.CreatedAt,
                Suggestion = s.SubCriteria!.Description
            })
            .ToListAsync();

        // 2. RecommendationNotes and Notes from Answers
        var answerSuggestions = await _context.Answers
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Assignment)
                    .ThenInclude(a => a.Project)
                        .ThenInclude(p => p!.Customer)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedCustomerPersonnel)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedOrganization)
            .Where(a => evaluationIds.Contains(a.EvaluationId))
            .Where(a => (a.RecommendationNotes != null && a.RecommendationNotes != "") || (a.Notes != null && a.Notes != ""))
            .ToListAsync();

        // Combine all suggestions
        var suggestionList = new List<(string PersonnelName, string Department, string ProjectName, DateTime EvalDate, string Suggestion)>();

        // Add SubCriteria suggestions
        foreach (var s in subCriteriaSuggestions)
        {
            suggestionList.Add((s.PersonnelName, s.Department, s.ProjectName, s.EvalDate, s.Suggestion));
        }

        // Add Answer suggestions (RecommendationNotes and Notes)
        foreach (var a in answerSuggestions)
        {
            var personnelName = a.Evaluation.EvaluatedCustomerPersonnel != null
                ? $"{a.Evaluation.EvaluatedCustomerPersonnel.FirstName} {a.Evaluation.EvaluatedCustomerPersonnel.LastName}"
                : a.Evaluation.EvaluatedUnknownPersonnel ?? "-";
            var department = a.Evaluation.EvaluatedOrganization?.Name ?? a.Evaluation.Assignment.Project?.Customer?.CompanyName ?? "-";
            var projectName = a.Evaluation.Assignment.Project?.Name ?? "";
            var evalDate = a.Evaluation.CallDate ?? a.Evaluation.CompletedAt ?? a.Evaluation.CreatedAt;

            if (!string.IsNullOrWhiteSpace(a.RecommendationNotes))
                suggestionList.Add((personnelName, department, projectName, evalDate, a.RecommendationNotes));

            if (!string.IsNullOrWhiteSpace(a.Notes))
                suggestionList.Add((personnelName, department, projectName, evalDate, a.Notes));
        }

        // Group and aggregate
        var suggestionData = suggestionList
            .GroupBy(x => new
            {
                x.PersonnelName,
                x.Department,
                x.ProjectName,
                Year = x.EvalDate.Year,
                PeriodMonth = x.EvalDate.ToString("yyyyMM"),
                x.Suggestion
            })
            .Select(g => new
            {
                g.Key.PersonnelName,
                g.Key.Department,
                g.Key.ProjectName,
                g.Key.Year,
                g.Key.PeriodMonth,
                g.Key.Suggestion,
                CallCount = g.Count()
            })
            .OrderBy(x => x.PersonnelName)
            .ThenBy(x => x.PeriodMonth)
            .ToList();

        int row = 2;
        foreach (var item in suggestionData)
        {
            worksheet.Cell(row, 1).Value = item.PersonnelName;
            worksheet.Cell(row, 2).Value = item.Department;
            worksheet.Cell(row, 3).Value = item.ProjectName;
            worksheet.Cell(row, 4).Value = item.Year;
            worksheet.Cell(row, 5).Value = item.PeriodMonth;
            worksheet.Cell(row, 6).Value = item.Suggestion;
            worksheet.Cell(row, 7).Value = item.CallCount;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet, noteColumns: new[] { 6 });
    }

    private async Task CreateMTSurecAnaliziSheet(XLWorkbook workbook, List<Core.Entities.Evaluation> evaluations)
    {
        var sheetName = await _localizationService.GetResourceAsync("Report.Sheet.MTSurecAnalizi", defaultValue: "MT Süreç Analizi");
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Headers
        var headers = new[]
        {
            await _localizationService.GetResourceAsync("Report.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.Representative", defaultValue: "Müşteri Temsilcisi"),
            await _localizationService.GetResourceAsync("Report.ControlQuestion", defaultValue: "Kontrol Sorusu"),
            await _localizationService.GetResourceAsync("Report.Period", defaultValue: "Periyot"),
            await _localizationService.GetResourceAsync("Report.PeriodMonth", defaultValue: "Periyot (Ay)"),
            await _localizationService.GetResourceAsync("Report.AverageScore", defaultValue: "Ortalama Puan"),
            await _localizationService.GetResourceAsync("Report.ErrorCount", defaultValue: "Hata Sayısı")
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Flatten to answer level and group by Project, Personnel, Question
        var processData = evaluations
            .SelectMany(e => e.Answers
                .Where(a => a.Question != null && a.Question.MaxPoints > 0)
                .Select(a => new
                {
                    ProjectName = e.Assignment.Project?.Name ?? "",
                    PersonnelName = e.EvaluatedCustomerPersonnel != null
                        ? $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}"
                        : e.EvaluatedUnknownPersonnel ?? "-",
                    QuestionText = a.Question!.Text,
                    EvalDate = e.CallDate ?? e.CompletedAt ?? e.CreatedAt,
                    ScorePercentage = (a.GivenPoints ?? 0) / a.Question.MaxPoints * 100,
                    IsError = (a.GivenPoints ?? 0) < a.Question.MaxPoints
                }))
            .GroupBy(x => new
            {
                x.ProjectName,
                x.PersonnelName,
                x.QuestionText,
                Year = x.EvalDate.Year,
                PeriodMonth = x.EvalDate.ToString("yyyyMM")
            })
            .Select(g => new
            {
                g.Key.ProjectName,
                g.Key.PersonnelName,
                g.Key.QuestionText,
                g.Key.Year,
                g.Key.PeriodMonth,
                AverageScore = Math.Round(g.Average(x => x.ScorePercentage), 0),
                ErrorCount = g.Count(x => x.IsError)
            })
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.PersonnelName)
            .ThenBy(x => x.QuestionText)
            .ToList();

        int row = 2;
        foreach (var item in processData)
        {
            worksheet.Cell(row, 1).Value = item.ProjectName;
            worksheet.Cell(row, 2).Value = item.PersonnelName;
            worksheet.Cell(row, 3).Value = item.QuestionText;
            worksheet.Cell(row, 4).Value = item.Year;
            worksheet.Cell(row, 5).Value = item.PeriodMonth;
            worksheet.Cell(row, 6).Value = item.AverageScore;
            worksheet.Cell(row, 7).Value = item.ErrorCount > 0 ? item.ErrorCount : (int?)null;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet);
    }

    private async Task CreateMTEndeksBasariSheet(XLWorkbook workbook, List<Core.Entities.Evaluation> evaluations)
    {
        var sheetName = await _localizationService.GetResourceAsync("Report.Sheet.MTEndeksBasari", defaultValue: "MT Endeks Başarı Analizi");
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Headers
        var headers = new[]
        {
            await _localizationService.GetResourceAsync("Report.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.QuestionGroup", defaultValue: "Kontrol Grubu"),
            await _localizationService.GetResourceAsync("Report.ControlQuestion", defaultValue: "Kontrol Sorusu"),
            await _localizationService.GetResourceAsync("Report.Representative", defaultValue: "Müşteri Temsilcisi"),
            await _localizationService.GetResourceAsync("Report.Department", defaultValue: "Departman"),
            await _localizationService.GetResourceAsync("Report.CallNo", defaultValue: "Çağrı No"),
            await _localizationService.GetResourceAsync("Report.PeriodMonth", defaultValue: "Periyot (Ay)"),
            await _localizationService.GetResourceAsync("Report.CriteriaSuccessScore", defaultValue: "Kriter Başarı Puanı"),
            await _localizationService.GetResourceAsync("Report.AuditComment", defaultValue: "Denetim Yorumu"),
            await _localizationService.GetResourceAsync("Report.Suggestion", defaultValue: "Öneriler"),
            await _localizationService.GetResourceAsync("Report.AverageScore", defaultValue: "Ortalama Puan")
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Flatten to answer level with full details
        var detailData = evaluations
            .SelectMany(e => e.Answers
                .Where(a => a.Question != null && a.Question.MaxPoints > 0)
                .Select(a => {
                    // Collect suggestions from multiple sources
                    var suggestions = new List<string>();

                    // SubCriteriaSelections
                    if (a.SubCriteriaSelections != null)
                    {
                        suggestions.AddRange(a.SubCriteriaSelections
                            .Where(s => s.SubCriteria != null && !string.IsNullOrWhiteSpace(s.SubCriteria.Description))
                            .Select(s => s.SubCriteria!.Description));
                    }

                    // RecommendationNotes
                    if (!string.IsNullOrWhiteSpace(a.RecommendationNotes))
                        suggestions.Add(a.RecommendationNotes);

                    // Notes
                    if (!string.IsNullOrWhiteSpace(a.Notes))
                        suggestions.Add(a.Notes);

                    return new
                    {
                        ProjectName = e.Assignment.Project?.Name ?? "",
                        GroupName = a.Question!.GroupName ?? "-",
                        QuestionText = a.Question.Text,
                        PersonnelName = e.EvaluatedCustomerPersonnel != null
                            ? $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}"
                            : e.EvaluatedUnknownPersonnel ?? "-",
                        Department = e.EvaluatedOrganization?.Name ?? e.Assignment.Project?.Customer?.CompanyName ?? "-",
                        CallId = e.CallId ?? "-",
                        PeriodMonth = (e.CallDate ?? e.CompletedAt ?? e.CreatedAt).ToString("yyyyMM"),
                        CriteriaScore = a.Question.MaxPoints > 0
                            ? Math.Round((a.GivenPoints ?? 0) / a.Question.MaxPoints * 100, 0)
                            : 0,
                        AuditComment = e.EvaluationComment ?? "",
                        Suggestions = string.Join(", ", suggestions),
                        AverageScore = e.ScorePercentage ?? 0
                    };
                }))
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.GroupName)
            .ThenBy(x => x.QuestionText)
            .ToList();

        int row = 2;
        foreach (var item in detailData)
        {
            worksheet.Cell(row, 1).Value = item.ProjectName;
            worksheet.Cell(row, 2).Value = item.GroupName;
            worksheet.Cell(row, 3).Value = item.QuestionText;
            worksheet.Cell(row, 4).Value = item.PersonnelName;
            worksheet.Cell(row, 5).Value = item.Department;
            worksheet.Cell(row, 6).Value = item.CallId;
            worksheet.Cell(row, 7).Value = item.PeriodMonth;
            worksheet.Cell(row, 8).Value = item.CriteriaScore;
            worksheet.Cell(row, 9).Value = item.AuditComment;
            worksheet.Cell(row, 10).Value = item.Suggestions;
            worksheet.Cell(row, 11).Value = Math.Round(item.AverageScore, 0);
            row++;
        }

        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet, callIdColumns: new[] { 6 }, noteColumns: new[] { 9, 10 });
    }

    // ===== ANKET SONUÇLARI RAPORU =====

    public async Task<SurveyResultsDto?> GetSurveyResultsAsync(int projectId, DateTime? startDate, DateTime? endDate)
    {
        // Proje kontrolü
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.Checklist)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null || project.ProjectTypeId != ProjectTypes.Ids.OnlineSurvey)
            return null;

        // Sorular
        var questions = await _context.Questions
            .Include(q => q.SubCriteria)
            .Where(q => q.ChecklistId == project.ChecklistId && !q.IsDeleted)
            .OrderBy(q => q.GroupName)
            .ThenBy(q => q.Order)
            .ToListAsync();

        // Değerlendirmeler
        var query = _context.Evaluations
            .Include(e => e.Assignment)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.OrganizationAssignments)
                    .ThenInclude(oa => oa.CustomerOrganization)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
            .Where(e => e.Assignment.ProjectId == projectId && e.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

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

        var evaluations = await query.ToListAsync();

        // Davetiye sayısı
        var invitedCount = await _context.SurveyInvitations
            .Where(si => si.ProjectId == projectId && si.StatusId == SurveyInvitationStatuses.Ids.Sent)
            .Select(si => si.CustomerPersonnelId)
            .Distinct()
            .CountAsync();

        // Soru bazlı sonuçlar
        var questionResults = new List<SurveyQuestionResultDto>();
        foreach (var question in questions)
        {
            var answers = evaluations
                .SelectMany(e => e.Answers.Where(a => a.QuestionId == question.Id))
                .ToList();

            var responseCount = answers.Count;
            decimal? avgScore = null;
            List<ScoreDistributionDto>? scoreDist = null;
            List<SubCriteriaResultDto>? subCriteriaResults = null;
            List<SurveyCommentDto>? comments = null;

            // Puan istatistikleri (showScoreInput true ise)
            if (question.ShowScoreInput && answers.Any(a => a.AnswerNumeric.HasValue))
            {
                var scoredAnswers = answers.Where(a => a.AnswerNumeric.HasValue).ToList();
                if (scoredAnswers.Any())
                {
                    avgScore = Math.Round((decimal)scoredAnswers.Average(a => (a.AnswerNumeric!.Value / (decimal)question.MaxPoints) * 100), 1);

                    // Puan dağılımı
                    scoreDist = new List<ScoreDistributionDto>();
                    for (int i = 0; i <= question.MaxPoints; i++)
                    {
                        var count = scoredAnswers.Count(a => a.AnswerNumeric == i);
                        scoreDist.Add(new ScoreDistributionDto
                        {
                            Score = i,
                            Count = count,
                            Percentage = responseCount > 0 ? Math.Round((decimal)count / responseCount * 100, 1) : 0
                        });
                    }
                }
            }

            // SubCriteria sonuçları
            if (question.SubCriteria.Any())
            {
                subCriteriaResults = question.SubCriteria
                    .Where(sc => sc.IsActive && !sc.IsDeleted)
                    .OrderBy(sc => sc.Order)
                    .Select(sc => {
                        var selectionCount = answers.Sum(a => a.SubCriteriaSelections.Count(s => s.SubCriteriaId == sc.Id));
                        return new SubCriteriaResultDto
                        {
                            SubCriteriaId = sc.Id,
                            Description = sc.Description,
                            SelectionCount = selectionCount,
                            SelectionPercentage = responseCount > 0 ? Math.Round((decimal)selectionCount / responseCount * 100, 1) : 0
                        };
                    })
                    .ToList();
            }

            // Yorumlar
            var answerComments = answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Notes))
                .Select(a => {
                    var eval = evaluations.First(e => e.Answers.Contains(a));
                    return new SurveyCommentDto
                    {
                        EvaluationId = eval.Id,
                        RespondentName = eval.EvaluatedCustomerPersonnel != null
                            ? $"{eval.EvaluatedCustomerPersonnel.FirstName} {eval.EvaluatedCustomerPersonnel.LastName}"
                            : null,
                        Comment = a.Notes!,
                        Date = eval.CompletedAt
                    };
                })
                .ToList();

            if (answerComments.Any())
                comments = answerComments;

            questionResults.Add(new SurveyQuestionResultDto
            {
                QuestionId = question.Id,
                QuestionText = question.Text,
                GroupName = question.GroupName ?? "Genel",
                Order = question.Order,
                ShowScoreInput = question.ShowScoreInput,
                ResponseCount = responseCount,
                AverageScore = avgScore,
                ScoreDistribution = scoreDist,
                SubCriteriaResults = subCriteriaResults,
                Comments = comments
            });
        }

        // Katılımcılar - External invitation'ları da al
        var extEvalIds = evaluations.Where(e => e.EvaluatedCustomerPersonnelId == null).Select(e => e.Id).ToList();
        var extInvitations = new Dictionary<int, (string? FirstName, string? LastName, string? Email)>();
        if (extEvalIds.Any())
        {
            var extList = await _context.SurveyExternalInvitations
                .Where(sei => sei.EvaluationId != null && extEvalIds.Contains(sei.EvaluationId.Value))
                .Select(sei => new { EvalId = sei.EvaluationId!.Value, sei.FirstName, sei.LastName, sei.Email })
                .ToListAsync();
            foreach (var item in extList)
                extInvitations[item.EvalId] = (item.FirstName, item.LastName, item.Email);
        }

        var respondents = evaluations.Select(e =>
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
            else if (extInvitations.TryGetValue(e.Id, out var ext))
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
        })
        .OrderByDescending(r => r.CompletedAt)
        .ToList();

        return new SurveyResultsDto
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            CustomerName = project.Customer?.CompanyName,
            OrganizationName = project.Organization?.Name,
            TotalResponses = evaluations.Count,
            TotalInvited = invitedCount > 0 ? invitedCount : evaluations.Count,
            CompletionRate = invitedCount > 0 ? Math.Round((decimal)evaluations.Count / invitedCount * 100, 1) : 100,
            AverageScore = evaluations.Any(e => e.ScorePercentage.HasValue)
                ? Math.Round((decimal)evaluations.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage!.Value), 1)
                : 0,
            TotalQuestions = questions.Count,
            QuestionResults = questionResults,
            Respondents = respondents
        };
    }

    public async Task<ExcelExportDto?> ExportSurveyResultsToExcelAsync(int projectId, DateTime? startDate, DateTime? endDate)
    {
        var results = await GetSurveyResultsAsync(projectId, startDate, endDate);
        if (results == null)
            return null;

        using var workbook = new XLWorkbook();

        // Summary sheet
        var summarySheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.Summary", defaultValue: "Özet"));
        summarySheet.Cell(1, 1).Value = results.ProjectName;
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 16;

        summarySheet.Cell(3, 1).Value = "Toplam Yanıt:";
        summarySheet.Cell(3, 2).Value = results.TotalResponses;
        summarySheet.Cell(4, 1).Value = "Tamamlanma Oranı:";
        summarySheet.Cell(4, 2).Value = $"{results.CompletionRate}%";
        summarySheet.Cell(5, 1).Value = "Ortalama Puan:";
        summarySheet.Cell(5, 2).Value = results.AverageScore;
        summarySheet.Cell(6, 1).Value = "Toplam Soru:";
        summarySheet.Cell(6, 2).Value = results.TotalQuestions;
        summarySheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(summarySheet);

        // Questions sheet
        var questionsSheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.Questions", defaultValue: "Sorular"));
        questionsSheet.Cell(1, 1).Value = "Sıra";
        questionsSheet.Cell(1, 2).Value = "Grup";
        questionsSheet.Cell(1, 3).Value = "Soru";
        questionsSheet.Cell(1, 4).Value = "Yanıt Sayısı";
        questionsSheet.Cell(1, 5).Value = "Ortalama Puan";
        for (int i = 1; i <= 5; i++)
        {
            questionsSheet.Cell(1, i).Style.Font.Bold = true;
            questionsSheet.Cell(1, i).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var q in results.QuestionResults)
        {
            questionsSheet.Cell(row, 1).Value = q.Order;
            questionsSheet.Cell(row, 2).Value = q.GroupName;
            questionsSheet.Cell(row, 3).Value = q.QuestionText;
            questionsSheet.Cell(row, 4).Value = q.ResponseCount;
            questionsSheet.Cell(row, 5).Value = q.AverageScore.HasValue ? $"{q.AverageScore}%" : "-";
            row++;
        }
        questionsSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(questionsSheet);

        // SubCriteria Results sheet
        var subCriteriaSheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.SubCriteria", defaultValue: "Alt Kriter Sonuçları"));
        subCriteriaSheet.Cell(1, 1).Value = "Soru";
        subCriteriaSheet.Cell(1, 2).Value = "Alt Kriter";
        subCriteriaSheet.Cell(1, 3).Value = "Seçim Sayısı";
        subCriteriaSheet.Cell(1, 4).Value = "Seçim Oranı";
        for (int i = 1; i <= 4; i++)
        {
            subCriteriaSheet.Cell(1, i).Style.Font.Bold = true;
            subCriteriaSheet.Cell(1, i).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        row = 2;
        foreach (var q in results.QuestionResults.Where(q => q.SubCriteriaResults != null && q.SubCriteriaResults.Any()))
        {
            foreach (var sc in q.SubCriteriaResults!)
            {
                subCriteriaSheet.Cell(row, 1).Value = q.QuestionText;
                subCriteriaSheet.Cell(row, 2).Value = sc.Description;
                subCriteriaSheet.Cell(row, 3).Value = sc.SelectionCount;
                subCriteriaSheet.Cell(row, 4).Value = $"{sc.SelectionPercentage}%";
                row++;
            }
        }
        subCriteriaSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(subCriteriaSheet, subCriteriaColumns: new[] { 2 });

        // Respondents sheet
        var respondentsSheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.Respondents", defaultValue: "Katılımcılar"));
        respondentsSheet.Cell(1, 1).Value = "Ad Soyad";
        respondentsSheet.Cell(1, 2).Value = "Email";
        respondentsSheet.Cell(1, 3).Value = "Organizasyon";
        respondentsSheet.Cell(1, 4).Value = "Puan";
        respondentsSheet.Cell(1, 5).Value = "Tamamlanma Tarihi";
        for (int i = 1; i <= 5; i++)
        {
            respondentsSheet.Cell(1, i).Style.Font.Bold = true;
            respondentsSheet.Cell(1, i).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        row = 2;
        foreach (var r in results.Respondents)
        {
            respondentsSheet.Cell(row, 1).Value = r.FullName ?? "Anonim";
            respondentsSheet.Cell(row, 2).Value = r.Email ?? "-";
            respondentsSheet.Cell(row, 3).Value = r.OrganizationName ?? "-";
            respondentsSheet.Cell(row, 4).Value = r.Score.HasValue ? $"{r.Score}%" : "-";
            respondentsSheet.Cell(row, 5).Value = r.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
            row++;
        }
        respondentsSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(respondentsSheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Anket_Sonuclari_{results.ProjectName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<List<SurveyProjectListItemDto>> GetSurveyProjectsAsync()
    {
        // Enneagram checklist'lerini hariç tut - sadece Survey (ChecklistTypeId=5) olanlar gelsin
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        var projects = await _context.Projects
            .Include(p => p.Customer)
            .Where(p => p.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey &&
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
                .Where(e => e.Assignment.ProjectId == project.Id && e.StatusId == EvaluationStatuses.Ids.Completed)
                .CountAsync();

            // Ortalama puan
            var avgScore = await _context.Evaluations
                .Where(e => e.Assignment.ProjectId == project.Id &&
                       e.StatusId == EvaluationStatuses.Ids.Completed &&
                       e.ScorePercentage.HasValue)
                .Select(e => e.ScorePercentage)
                .AverageAsync() ?? 0;

            // Son yanıt tarihi
            var lastResponse = await _context.Evaluations
                .Where(e => e.Assignment.ProjectId == project.Id && e.StatusId == EvaluationStatuses.Ids.Completed)
                .OrderByDescending(e => e.CompletedAt)
                .Select(e => e.CompletedAt)
                .FirstOrDefaultAsync();

            result.Add(new SurveyProjectListItemDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                CustomerName = project.Customer?.CompanyName,
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

    public async Task<List<RecentSurveyResponseDto>> GetRecentSurveyResponsesAsync(int count = 20, int? projectId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Enneagram checklist'lerini hariç tut - sadece Survey tipi
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey &&
                   e.StatusId == EvaluationStatuses.Ids.Completed &&
                   !e.Assignment.Project.IsDeleted &&
                   !enneagramChecklistIds.Contains(e.Assignment.Project.ChecklistId))
            .AsQueryable();

        // Filter by project
        if (projectId.HasValue)
        {
            query = query.Where(e => e.Assignment.ProjectId == projectId.Value);
        }

        // Filter by date range
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

        // Get external invitation names for evaluations without CustomerPersonnel
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
                ProjectId = e.Assignment.ProjectId,
                ProjectName = e.Assignment.Project.Name,
                RespondentName = string.IsNullOrWhiteSpace(respondentName) ? null : respondentName,
                RespondentEmail = respondentEmail,
                Score = e.ScorePercentage,
                CompletedAt = e.CompletedAt
            };
        }).ToList();

        return responses;
    }

    public async Task<ExcelExportDto?> ExportSurveyResponsesToExcelAsync(int? projectId = null)
    {
        // Yanıtları al (max 500)
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey &&
                   e.StatusId == EvaluationStatuses.Ids.Completed &&
                   !e.Assignment.Project.IsDeleted)
            .AsQueryable();

        // Filter by project
        if (projectId.HasValue)
        {
            query = query.Where(e => e.Assignment.ProjectId == projectId.Value);
        }

        var evaluations = await query
            .OrderByDescending(e => e.CompletedAt)
            .Take(500)
            .ToListAsync();

        if (!evaluations.Any())
            return null;

        // Get external invitation names for evaluations without CustomerPersonnel
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

        // Proje adı (dosya adı için)
        var projectName = projectId.HasValue
            ? evaluations.FirstOrDefault()?.Assignment.Project?.Name ?? "Anket"
            : "Tum_Anketler";

        // Excel oluştur
        using var workbook = new XLWorkbook();

        // ===== Sheet 1: Yanıtlar =====
        var sheet1 = workbook.Worksheets.Add("Yanıtlar");
        var headers1 = new[] { "Proje", "Katılımcı Adı", "E-posta", "Puan (%)", "Tamamlanma Tarihi" };

        for (int i = 0; i < headers1.Length; i++)
        {
            sheet1.Cell(1, i + 1).Value = headers1[i];
            sheet1.Cell(1, i + 1).Style.Font.Bold = true;
            sheet1.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row1 = 2;
        foreach (var e in evaluations)
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

            sheet1.Cell(row1, 1).Value = e.Assignment.Project?.Name ?? "";
            sheet1.Cell(row1, 2).Value = respondentName ?? "-";
            sheet1.Cell(row1, 3).Value = respondentEmail ?? "-";
            sheet1.Cell(row1, 4).Value = e.ScorePercentage ?? 0;
            sheet1.Cell(row1, 4).Style.NumberFormat.Format = "0.00";
            sheet1.Cell(row1, 5).Value = e.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
            row1++;
        }

        sheet1.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(sheet1);

        // ===== Sheet 2: Cevap Detayları =====
        var sheet2 = workbook.Worksheets.Add("Cevap Detayları");
        var headers2 = new[] { "Proje", "Katılımcı", "E-posta", "Grup", "Soru", "Puan", "Max Puan", "Seçilen Kriterler", "Yorum" };

        for (int i = 0; i < headers2.Length; i++)
        {
            sheet2.Cell(1, i + 1).Value = headers2[i];
            sheet2.Cell(1, i + 1).Style.Font.Bold = true;
            sheet2.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row2 = 2;
        foreach (var e in evaluations)
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

            var answers = e.Answers
                .Where(a => a.Question != null)
                .OrderBy(a => a.Question!.GroupName ?? "")
                .ThenBy(a => a.Question!.Order)
                .ToList();

            foreach (var a in answers)
            {
                var selectedCriteria = a.SubCriteriaSelections
                    .Select(s => s.SubCriteria?.Description)
                    .Where(d => !string.IsNullOrEmpty(d))
                    .ToList();

                sheet2.Cell(row2, 1).Value = e.Assignment.Project?.Name ?? "";
                sheet2.Cell(row2, 2).Value = respondentName ?? "-";
                sheet2.Cell(row2, 3).Value = respondentEmail ?? "-";
                sheet2.Cell(row2, 4).Value = a.Question!.GroupName ?? "Genel";
                sheet2.Cell(row2, 5).Value = a.Question.Text;
                sheet2.Cell(row2, 6).Value = a.Question.ShowScoreInput ? (a.AnswerNumeric ?? 0) : (a.EarnedPoints ?? 0);
                sheet2.Cell(row2, 7).Value = a.Question.ShowScoreInput ? a.Question.MaxPoints : a.Question.WeightPoints;
                sheet2.Cell(row2, 8).Value = selectedCriteria.Any() ? string.Join(", ", selectedCriteria) : "-";
                sheet2.Cell(row2, 9).Value = a.Notes ?? "-";
                row2++;
            }
        }

        sheet2.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(sheet2, subCriteriaColumns: new[] { 8 }, noteColumns: new[] { 9 });

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var safeProjectName = string.Join("_", projectName.Split(Path.GetInvalidFileNameChars()));

        return new ExcelExportDto
        {
            FileName = $"Anket_Yanitlari_{safeProjectName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<SurveyProjectDetailDto?> GetSurveyProjectDetailAsync(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.Checklist)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

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
            .Where(e => e.Assignment.ProjectId == projectId && e.StatusId == EvaluationStatuses.Ids.Completed)
            .OrderByDescending(e => e.CompletedAt)
            .ToListAsync();

        // Davetiye sayısı
        var invitationCount = await _context.SurveyInvitations
            .Where(si => si.ProjectId == projectId && si.StatusId == SurveyInvitationStatuses.Ids.Sent)
            .CountAsync();

        // Grup bazlı puan hesaplaması
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
                // Her soru için max puan al
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
            CustomerName = project.Customer?.CompanyName,
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

    public async Task<ExcelExportDto?> ExportSurveyGroupScoresToExcelAsync(int projectId)
    {
        var detail = await GetSurveyProjectDetailAsync(projectId);
        if (detail == null) return null;

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Grup Puanları");

        // Header
        sheet.Cell(1, 1).Value = detail.ProjectName;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 3).Merge();

        // Summary
        sheet.Cell(3, 1).Value = "Toplam Yanıt:";
        sheet.Cell(3, 2).Value = detail.TotalResponses;
        sheet.Cell(4, 1).Value = "Ortalama Puan:";
        sheet.Cell(4, 2).Value = detail.AverageScore.HasValue ? $"{detail.AverageScore:F1}%" : "-";

        // Table header
        var row = 6;
        sheet.Cell(row, 1).Value = "Grup Adı";
        sheet.Cell(row, 2).Value = "Soru Sayısı";
        sheet.Cell(row, 3).Value = "Ortalama Puan";
        sheet.Range(row, 1, row, 3).Style.Font.Bold = true;
        sheet.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data
        row++;
        foreach (var group in detail.GroupScores)
        {
            sheet.Cell(row, 1).Value = group.GroupName;
            sheet.Cell(row, 2).Value = group.QuestionCount;
            sheet.Cell(row, 3).Value = group.AverageScore.HasValue ? $"{group.AverageScore:F1}%" : "-";
            row++;
        }

        sheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(sheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Grup_Puanlari_{detail.ProjectName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<ExcelExportDto?> ExportSurveyQuestionStatsToExcelAsync(int projectId)
    {
        var results = await GetSurveyResultsAsync(projectId, null, null);
        if (results == null) return null;

        // Puan detayı verilerini de al
        var scoreDetail = await GetSurveyQuestionScoreDetailAsync(projectId);

        using var workbook = new XLWorkbook();

        // ===== SHEET 1: GENEL DURUM =====
        var sheet1 = workbook.Worksheets.Add("Genel Durum");

        // Header
        sheet1.Cell(1, 1).Value = results.ProjectName;
        sheet1.Cell(1, 1).Style.Font.Bold = true;
        sheet1.Cell(1, 1).Style.Font.FontSize = 14;
        sheet1.Range(1, 1, 1, 5).Merge();

        // Özet bilgiler
        sheet1.Cell(2, 1).Value = "Toplam Yanıt:";
        sheet1.Cell(2, 2).Value = results.TotalResponses;
        sheet1.Cell(2, 3).Value = "Genel Ortalama:";
        sheet1.Cell(2, 4).Value = scoreDetail?.OverallAverageScore.HasValue == true
            ? $"%{scoreDetail.OverallAverageScore:F1}"
            : "-";
        sheet1.Range(2, 1, 2, 4).Style.Font.Bold = true;

        // Soru listesi başlıkları
        var row = 4;
        sheet1.Cell(row, 1).Value = "Soru Grubu";
        sheet1.Cell(row, 2).Value = "Soru";
        sheet1.Cell(row, 3).Value = "Yanıt Sayısı";
        sheet1.Cell(row, 4).Value = "Ortalama Puan (%)";
        sheet1.Range(row, 1, row, 4).Style.Font.Bold = true;
        row++;

        // scoreDetail'den soru puanlarını dictionary'e al
        var scoreByQuestionId = scoreDetail?.Questions
            .ToDictionary(q => q.QuestionId, q => q.AverageScorePercentage)
            ?? new Dictionary<int, decimal?>();

        // Soruların özet listesi
        foreach (var question in results.QuestionResults)
        {
            // scoreDetail'den doğru ortalamayı al
            var avgScore = scoreByQuestionId.GetValueOrDefault(question.QuestionId);

            sheet1.Cell(row, 1).Value = question.GroupName ?? "-";
            sheet1.Cell(row, 2).Value = question.QuestionText;
            sheet1.Cell(row, 3).Value = question.ResponseCount;
            sheet1.Cell(row, 4).Value = avgScore.HasValue
                ? $"%{avgScore:F1}"
                : "-";
            row++;
        }

        // Kolon genişlikleri
        sheet1.Column(1).Width = 20;  // Soru Grubu
        sheet1.Column(2).Width = 60;  // Soru (geniş)
        sheet1.Column(3).Width = 15;  // Yanıt Sayısı
        sheet1.Column(4).Width = 18;  // Ortalama Puan

        // Soru kolonuna text wrap uygula
        sheet1.Column(2).Style.Alignment.WrapText = true;
        sheet1.Column(2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

        // ===== SHEET 2: PUAN DETAYI =====
        var sheet2 = workbook.Worksheets.Add("Puan Detayı");

        if (scoreDetail != null && scoreDetail.Questions.Any())
        {
            // Header
            sheet2.Cell(1, 1).Value = results.ProjectName + " - Puan Detayı";
            sheet2.Cell(1, 1).Style.Font.Bold = true;
            sheet2.Cell(1, 1).Style.Font.FontSize = 14;
            sheet2.Range(1, 1, 1, 7).Merge();

            sheet2.Cell(2, 1).Value = "Toplam Yanıt:";
            sheet2.Cell(2, 2).Value = scoreDetail.TotalResponses;
            sheet2.Cell(2, 3).Value = "Genel Ortalama:";
            sheet2.Cell(2, 4).Value = scoreDetail.OverallAverageScore.HasValue
                ? $"%{scoreDetail.OverallAverageScore:F1}"
                : "-";

            // Tablo başlıkları
            var headerRow = 4;
            sheet2.Cell(headerRow, 1).Value = "Soru Grubu";
            sheet2.Cell(headerRow, 2).Value = "Soru";
            sheet2.Cell(headerRow, 3).Value = "Yanıt";
            sheet2.Cell(headerRow, 4).Value = "Ort. (%)";
            sheet2.Cell(headerRow, 5).Value = "Cevap";
            sheet2.Cell(headerRow, 6).Value = "Seçim";
            sheet2.Cell(headerRow, 7).Value = "Yüzde";
            sheet2.Range(headerRow, 1, headerRow, 7).Style.Font.Bold = true;

            row = headerRow + 1;
            foreach (var question in scoreDetail.Questions)
            {
                var questionStartRow = row;
                var hasDistributions = question.AnswerDistributions.Any();

                if (hasDistributions)
                {
                    // Her alt kriter için bir satır
                    foreach (var dist in question.AnswerDistributions)
                    {
                        sheet2.Cell(row, 5).Value = dist.AnswerText;
                        sheet2.Cell(row, 6).Value = dist.SelectionCount;
                        sheet2.Cell(row, 7).Value = $"%{dist.Percentage:F1}";
                        row++;
                    }

                    // Soru bilgilerini merge et
                    var questionEndRow = row - 1;
                    if (questionEndRow > questionStartRow)
                    {
                        sheet2.Range(questionStartRow, 1, questionEndRow, 1).Merge();
                        sheet2.Range(questionStartRow, 2, questionEndRow, 2).Merge();
                        sheet2.Range(questionStartRow, 3, questionEndRow, 3).Merge();
                        sheet2.Range(questionStartRow, 4, questionEndRow, 4).Merge();
                    }

                    sheet2.Cell(questionStartRow, 1).Value = question.GroupName ?? "-";
                    sheet2.Cell(questionStartRow, 2).Value = question.QuestionText;
                    sheet2.Cell(questionStartRow, 3).Value = question.ResponseCount;
                    sheet2.Cell(questionStartRow, 4).Value = question.AverageScorePercentage.HasValue
                        ? $"%{question.AverageScorePercentage:F1}"
                        : "-";
                }
                else
                {
                    // Alt kriter yoksa tek satır
                    sheet2.Cell(row, 1).Value = question.GroupName ?? "-";
                    sheet2.Cell(row, 2).Value = question.QuestionText;
                    sheet2.Cell(row, 3).Value = question.ResponseCount;
                    sheet2.Cell(row, 4).Value = question.AverageScorePercentage.HasValue
                        ? $"%{question.AverageScorePercentage:F1}"
                        : "-";
                    sheet2.Cell(row, 5).Value = "-";
                    sheet2.Cell(row, 6).Value = "-";
                    sheet2.Cell(row, 7).Value = "-";
                    row++;
                }
            }

            // Kolon genişlikleri
            sheet2.Column(1).Width = 20;  // Soru Grubu
            sheet2.Column(2).Width = 60;  // Soru (geniş)
            sheet2.Column(3).Width = 10;  // Yanıt
            sheet2.Column(4).Width = 12;  // Ort. (%)
            sheet2.Column(5).Width = 40;  // Cevap
            sheet2.Column(6).Width = 10;  // Seçim
            sheet2.Column(7).Width = 10;  // Yüzde

            // Soru kolonuna text wrap uygula
            sheet2.Column(2).Style.Alignment.WrapText = true;
            sheet2.Column(5).Style.Alignment.WrapText = true;

            // Dikey hizalama (üste)
            sheet2.Column(1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            sheet2.Column(2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            sheet2.Column(3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            sheet2.Column(4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        }
        else
        {
            sheet2.Cell(1, 1).Value = "Puan detayı verisi bulunamadı.";
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var safeProjectName = string.Join("_", results.ProjectName.Split(Path.GetInvalidFileNameChars()));
        return new ExcelExportDto
        {
            FileName = $"Soru_Istatistikleri_{safeProjectName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<ExcelExportDto?> ExportSurveyDetailReportToExcelAsync(int projectId, bool includeComments)
    {
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Checklist)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null || project.ProjectTypeId != ProjectTypes.Ids.OnlineSurvey)
            return null;

        // Sorular
        var questions = await _context.Questions
            .Include(q => q.SubCriteria.Where(sc => sc.IsActive && !sc.IsDeleted))
            .Where(q => q.ChecklistId == project.ChecklistId && !q.IsDeleted)
            .OrderBy(q => q.GroupName)
            .ThenBy(q => q.Order)
            .ToListAsync();

        // Değerlendirmeler
        var evaluations = await _context.Evaluations
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => e.Assignment.ProjectId == projectId && e.StatusId == EvaluationStatuses.Ids.Completed)
            .OrderByDescending(e => e.CompletedAt)
            .ToListAsync();

        // External invitation'ları al
        var extEvalIds = evaluations.Where(e => e.EvaluatedCustomerPersonnelId == null).Select(e => e.Id).ToList();
        var extInvitations = new Dictionary<int, (string? FirstName, string? LastName, string? Email)>();
        if (extEvalIds.Any())
        {
            var extList = await _context.SurveyExternalInvitations
                .Where(sei => sei.EvaluationId != null && extEvalIds.Contains(sei.EvaluationId.Value))
                .Select(sei => new { EvalId = sei.EvaluationId!.Value, sei.FirstName, sei.LastName, sei.Email })
                .ToListAsync();
            foreach (var item in extList)
                extInvitations[item.EvalId] = (item.FirstName, item.LastName, item.Email);
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(includeComments ? "Tam Detay" : "Detay");

        // Build dynamic columns: Katılımcı, Email, Puan, Tarih, [Soru1], [Soru1_Seçenekler], [Soru1_Yorum?], ...
        var headers = new List<string> { "Katılımcı", "Email", "Genel Puan", "Tarih" };
        var subCriteriaColIndices = new List<int>();
        var noteColIndices = new List<int>();
        var colIndex = 5; // Base columns end at 4

        foreach (var q in questions)
        {
            headers.Add(q.Text); // Puan
            colIndex++;
            if (q.SubCriteria.Any())
            {
                headers.Add($"{q.Text} - Seçenekler");
                subCriteriaColIndices.Add(colIndex);
                colIndex++;
            }
            if (includeComments)
            {
                headers.Add($"{q.Text} - Yorum");
                noteColIndices.Add(colIndex);
                colIndex++;
            }
        }

        // Write headers
        for (int i = 0; i < headers.Count; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }
        sheet.Range(1, 1, 1, headers.Count).Style.Font.Bold = true;
        sheet.Range(1, 1, 1, headers.Count).Style.Fill.BackgroundColor = XLColor.LightGray;

        // Write data
        var row = 2;
        foreach (var eval in evaluations)
        {
            var col = 1;

            // Katılımcı ve Email - External invitation'dan da bak
            string participantName = "Anonim";
            string participantEmail = "";

            if (eval.EvaluatedCustomerPersonnel != null)
            {
                participantName = $"{eval.EvaluatedCustomerPersonnel.FirstName} {eval.EvaluatedCustomerPersonnel.LastName}".Trim();
                participantEmail = eval.EvaluatedCustomerPersonnel.Email ?? "";
            }
            else if (extInvitations.TryGetValue(eval.Id, out var ext))
            {
                var name = $"{ext.FirstName} {ext.LastName}".Trim();
                participantName = string.IsNullOrWhiteSpace(name) ? "Anonim" : name;
                participantEmail = ext.Email ?? "";
            }

            sheet.Cell(row, col++).Value = participantName;
            sheet.Cell(row, col++).Value = participantEmail;

            // Genel Puan
            sheet.Cell(row, col++).Value = eval.ScorePercentage.HasValue ? $"{eval.ScorePercentage:F1}%" : "-";

            // Tarih
            sheet.Cell(row, col++).Value = eval.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "";

            // Her soru için
            foreach (var q in questions)
            {
                var answer = eval.Answers.FirstOrDefault(a => a.QuestionId == q.Id);

                // Puan
                if (answer?.AnswerNumeric.HasValue == true)
                {
                    sheet.Cell(row, col++).Value = $"{answer.AnswerNumeric}/{q.MaxPoints}";
                }
                else
                {
                    sheet.Cell(row, col++).Value = "-";
                }

                // Seçenekler
                if (q.SubCriteria.Any())
                {
                    var selections = answer?.SubCriteriaSelections
                        .Select(s => s.SubCriteria?.Description ?? "")
                        .Where(d => !string.IsNullOrEmpty(d))
                        .ToList() ?? new List<string>();
                    sheet.Cell(row, col++).Value = string.Join(", ", selections);
                }

                // Yorum
                if (includeComments)
                {
                    sheet.Cell(row, col++).Value = answer?.Notes ?? "";
                }
            }

            row++;
        }

        sheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(sheet,
            subCriteriaColumns: subCriteriaColIndices.Any() ? subCriteriaColIndices.ToArray() : null,
            noteColumns: noteColIndices.Any() ? noteColIndices.ToArray() : null);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var filePrefix = includeComments ? "Tam_Detay" : "Detay";
        return new ExcelExportDto
        {
            FileName = $"{filePrefix}_{project.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== PERFORMANS TAKİBİ =====

    public async Task<PerformanceTrackingResultDto> GetPerformanceTrackingAsync(
        List<int>? customerIds = null,
        List<int>? evaluatorIds = null,
        List<int>? projectIds = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek + (int)DayOfWeek.Monday);
        if (todayStart.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = new PerformanceTrackingResultDto();

        // Base query with filters
        var baseQuery = _context.Evaluations
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed);

        // Apply filters
        if (customerIds?.Any() == true)
            baseQuery = baseQuery.Where(e => e.Assignment.Project.CustomerId.HasValue && customerIds.Contains(e.Assignment.Project.CustomerId.Value));
        if (evaluatorIds?.Any() == true)
            baseQuery = baseQuery.Where(e => e.EvaluatorId.HasValue && evaluatorIds.Contains(e.EvaluatorId.Value));
        if (projectIds?.Any() == true)
            baseQuery = baseQuery.Where(e => projectIds.Contains(e.Assignment.ProjectId));
        if (startDate.HasValue)
            baseQuery = baseQuery.Where(e => e.CompletedAt >= startDate.Value);
        if (endDate.HasValue)
            baseQuery = baseQuery.Where(e => e.CompletedAt <= endDate.Value.Date.AddDays(1));

        // 1. Değerlendirici (Evaluator) Performansları
        var evaluatorStats = await baseQuery
            .Where(e => e.EvaluatorId != null)
            .GroupBy(e => new { e.EvaluatorId, e.Evaluator!.FirstName, e.Evaluator.LastName })
            .Select(g => new
            {
                EvaluatorId = g.Key.EvaluatorId!.Value,
                FirstName = g.Key.FirstName,
                LastName = g.Key.LastName,
                TodayCount = g.Count(e => e.CompletedAt >= todayStart),
                WeekCount = g.Count(e => e.CompletedAt >= weekStart),
                MonthCount = g.Count(e => e.CompletedAt >= monthStart),
                YearCount = g.Count(e => e.CompletedAt >= yearStart),
                TotalCount = g.Count(),
                TotalScore = g.Sum(e => e.ScorePercentage ?? 0),
                ScoredCount = g.Count(e => e.ScorePercentage != null)
            })
            .OrderByDescending(e => e.MonthCount)
            .ToListAsync();

        result.EvaluatorPerformances = evaluatorStats.Select(e => new EvaluatorPerformanceDto
        {
            EvaluatorId = e.EvaluatorId,
            EvaluatorName = $"{e.FirstName} {e.LastName}".Trim(),
            TodayCount = e.TodayCount,
            WeekCount = e.WeekCount,
            MonthCount = e.MonthCount,
            YearCount = e.YearCount,
            TotalCount = e.TotalCount,
            AverageScore = e.ScoredCount > 0 ? Math.Round(e.TotalScore / e.ScoredCount, 2) : 0
        }).ToList();

        // 2. Firma Kota Durumları
        var customerStats = await _context.Customers
            .Where(c => !c.IsDeleted && c.IsActive)
            .Select(c => new
            {
                c.Id,
                c.CompanyName,
                c.Code,
                c.TargetCount,
                c.DailyQuota,
                c.WeeklyQuota,
                c.MonthlyQuota,
                TodayCount = _context.Evaluations.Count(e =>
                    !e.IsDeleted &&
                    e.StatusId == EvaluationStatuses.Ids.Completed &&
                    e.Assignment.Project.CustomerId == c.Id &&
                    e.CompletedAt >= todayStart),
                WeekCount = _context.Evaluations.Count(e =>
                    !e.IsDeleted &&
                    e.StatusId == EvaluationStatuses.Ids.Completed &&
                    e.Assignment.Project.CustomerId == c.Id &&
                    e.CompletedAt >= weekStart),
                MonthCount = _context.Evaluations.Count(e =>
                    !e.IsDeleted &&
                    e.StatusId == EvaluationStatuses.Ids.Completed &&
                    e.Assignment.Project.CustomerId == c.Id &&
                    e.CompletedAt >= monthStart),
                TotalCount = _context.Evaluations.Count(e =>
                    !e.IsDeleted &&
                    e.StatusId == EvaluationStatuses.Ids.Completed &&
                    e.Assignment.Project.CustomerId == c.Id)
            })
            .OrderByDescending(c => c.MonthCount)
            .ToListAsync();

        result.CustomerQuotaStatuses = customerStats.Select(c => new CustomerQuotaStatusDto
        {
            CustomerId = c.Id,
            CustomerName = c.CompanyName,
            CustomerCode = c.Code,
            TargetCount = c.TargetCount,
            DailyQuota = c.DailyQuota,
            WeeklyQuota = c.WeeklyQuota,
            MonthlyQuota = c.MonthlyQuota,
            TodayCount = c.TodayCount,
            WeekCount = c.WeekCount,
            MonthCount = c.MonthCount,
            TotalCount = c.TotalCount,
            TargetCompletionPercentage = c.TargetCount.HasValue && c.TargetCount > 0
                ? Math.Round((decimal)c.TotalCount / c.TargetCount.Value * 100, 2)
                : null,
            DailyQuotaUsagePercentage = c.DailyQuota.HasValue && c.DailyQuota > 0
                ? Math.Round((decimal)c.TodayCount / c.DailyQuota.Value * 100, 2)
                : null,
            WeeklyQuotaUsagePercentage = c.WeeklyQuota.HasValue && c.WeeklyQuota > 0
                ? Math.Round((decimal)c.WeekCount / c.WeeklyQuota.Value * 100, 2)
                : null,
            MonthlyQuotaUsagePercentage = c.MonthlyQuota.HasValue && c.MonthlyQuota > 0
                ? Math.Round((decimal)c.MonthCount / c.MonthlyQuota.Value * 100, 2)
                : null
        }).ToList();

        // 3. Proje Tipi Bazlı Performans Karşılaştırması
        var performanceSettings = await _context.Set<PerformanceSettings>()
            .Where(ps => !ps.IsDeleted && ps.IsActive)
            .ToListAsync();

        var projectTypeStats = await _context.Evaluations
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed)
            .GroupBy(e => e.Assignment.Project.ProjectTypeId)
            .Select(g => new
            {
                ProjectTypeId = g.Key,
                TodayCount = g.Count(e => e.CompletedAt >= todayStart),
                WeekCount = g.Count(e => e.CompletedAt >= weekStart),
                MonthCount = g.Count(e => e.CompletedAt >= monthStart),
                YearCount = g.Count(e => e.CompletedAt >= yearStart),
                TotalScore = g.Sum(e => e.ScorePercentage ?? 0),
                ScoredCount = g.Count(e => e.ScorePercentage != null)
            })
            .ToListAsync();

        result.ProjectTypePerformances = ProjectTypes.All
            .Select(pt =>
            {
                var settings = performanceSettings.FirstOrDefault(ps => ps.ProjectTypeId == pt.Id);
                var stats = projectTypeStats.FirstOrDefault(s => s.ProjectTypeId == pt.Id);

                var dto = new ProjectTypePerformanceDto
                {
                    ProjectTypeId = pt.Id,
                    ProjectTypeName = pt.NameResourceKey,
                    DailyTarget = settings?.DailyTarget,
                    WeeklyTarget = settings?.WeeklyTarget,
                    MonthlyTarget = settings?.MonthlyTarget,
                    YearlyTarget = settings?.YearlyTarget,
                    SuccessThreshold = settings?.SuccessThreshold,
                    WarningThreshold = settings?.WarningThreshold,
                    TodayCount = stats?.TodayCount ?? 0,
                    WeekCount = stats?.WeekCount ?? 0,
                    MonthCount = stats?.MonthCount ?? 0,
                    YearCount = stats?.YearCount ?? 0,
                    AverageScore = stats != null && stats.ScoredCount > 0
                        ? Math.Round(stats.TotalScore / stats.ScoredCount, 2)
                        : 0
                };

                // Hedef yüzdeleri hesapla
                if (dto.DailyTarget.HasValue && dto.DailyTarget > 0)
                    dto.DailyPercentage = Math.Round((decimal)dto.TodayCount / dto.DailyTarget.Value * 100, 1);
                if (dto.WeeklyTarget.HasValue && dto.WeeklyTarget > 0)
                    dto.WeeklyPercentage = Math.Round((decimal)dto.WeekCount / dto.WeeklyTarget.Value * 100, 1);
                if (dto.MonthlyTarget.HasValue && dto.MonthlyTarget > 0)
                    dto.MonthlyPercentage = Math.Round((decimal)dto.MonthCount / dto.MonthlyTarget.Value * 100, 1);
                if (dto.YearlyTarget.HasValue && dto.YearlyTarget > 0)
                    dto.YearlyPercentage = Math.Round((decimal)dto.YearCount / dto.YearlyTarget.Value * 100, 1);

                return dto;
            })
            .Where(dto => dto.DailyTarget.HasValue || dto.WeeklyTarget.HasValue || dto.MonthlyTarget.HasValue || dto.YearlyTarget.HasValue ||
                          dto.TodayCount > 0 || dto.WeekCount > 0 || dto.MonthCount > 0 || dto.YearCount > 0)
            .ToList();

        // 4. Genel Özet
        result.Summary = new PerformanceSummaryDto
        {
            TotalEvaluators = result.EvaluatorPerformances.Count,
            TotalActiveCustomers = result.CustomerQuotaStatuses.Count,
            TotalTodayEvaluations = result.EvaluatorPerformances.Sum(e => e.TodayCount),
            TotalWeekEvaluations = result.EvaluatorPerformances.Sum(e => e.WeekCount),
            TotalMonthEvaluations = result.EvaluatorPerformances.Sum(e => e.MonthCount),
            TotalYearEvaluations = result.EvaluatorPerformances.Sum(e => e.YearCount)
        };

        return result;
    }

    // ===== GENEL SORU PUAN DAĞILIMI =====

    /// <summary>
    /// Online anket projelerindeki soruların genel puan dağılımını getirir
    /// </summary>
    public async Task<SurveyQuestionScoreDistributionResultDto> GetSurveyQuestionScoreDistributionAsync(
        int? projectId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        // Proje zorunlu - tarih filtresi kaldırıldı
        if (!projectId.HasValue)
        {
            return new SurveyQuestionScoreDistributionResultDto
            {
                Questions = new List<SurveyQuestionScoreDistributionDto>(),
                TotalResponses = 0,
                OverallAverageScore = 0
            };
        }

        // Online anket projesindeki tamamlanmış değerlendirmeler
        var evalQuery = _context.Evaluations
            .Where(e => !e.IsDeleted &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey &&
                        e.Assignment.ProjectId == projectId.Value);

        var evaluationIds = await evalQuery.Select(e => e.Id).ToListAsync();

        if (!evaluationIds.Any())
        {
            return new SurveyQuestionScoreDistributionResultDto
            {
                Questions = new List<SurveyQuestionScoreDistributionDto>(),
                TotalResponses = 0,
                OverallAverageScore = 0
            };
        }

        // Cevapları ve soruları getir
        var answers = await _context.Answers
            .Include(a => a.Question)
            .Where(a => evaluationIds.Contains(a.EvaluationId) && !a.Question.IsDeleted)
            .ToListAsync();

        // Soru bazlı gruplama - EarnedPoints kullan (doğru hesaplanmış puan)
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

        // Genel ortalama hesapla
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
    /// Genel Soru Puan Dağılımı Excel Export
    /// </summary>
    public async Task<ExcelExportDto?> ExportSurveyQuestionDistributionToExcelAsync(int projectId)
    {
        // Proje bilgisi
        var project = await _context.Projects
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
            return null;

        // Veriyi al
        var data = await GetSurveyQuestionScoreDistributionAsync(projectId, null, null);

        if (data.Questions.Count == 0)
            return null;

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Soru Puan Dağılımı");

        // Başlık
        worksheet.Cell(1, 1).Value = "Proje: " + project.Name;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 5).Merge();

        worksheet.Cell(2, 1).Value = $"Toplam Yanıt: {data.TotalResponses} | Genel Ortalama: %{data.OverallAverageScore:F1}";
        worksheet.Range(2, 1, 2, 5).Merge();

        // Headers
        var headerRow = 4;
        worksheet.Cell(headerRow, 1).Value = "Soru";
        worksheet.Cell(headerRow, 2).Value = "Grup";
        worksheet.Cell(headerRow, 3).Value = "Yanıt Sayısı";
        worksheet.Cell(headerRow, 4).Value = "Ort. Puan";
        worksheet.Cell(headerRow, 5).Value = "Yüzde (%)";

        var headerRange = worksheet.Range(headerRow, 1, headerRow, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Data
        var row = headerRow + 1;
        foreach (var q in data.Questions)
        {
            worksheet.Cell(row, 1).Value = q.QuestionText;
            worksheet.Cell(row, 2).Value = q.GroupName ?? "-";
            worksheet.Cell(row, 3).Value = q.ResponseCount;
            worksheet.Cell(row, 4).Value = q.AverageRawScore.HasValue ? $"{q.AverageRawScore:F2} / {q.MaxPoints}" : "-";
            worksheet.Cell(row, 5).Value = q.AverageScore.HasValue ? q.AverageScore.Value : 0;
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "0.0";

            // Renklendirme
            if (q.AverageScore.HasValue)
            {
                var cell = worksheet.Cell(row, 5);
                if (q.AverageScore >= 70)
                    cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                else if (q.AverageScore >= 40)
                    cell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                else
                    cell.Style.Fill.BackgroundColor = XLColor.LightPink;
            }

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet);
        worksheet.Column(1).Width = 60; // Soru sütunu geniş

        // ===== Cevap Dağılımı Sheet'i =====
        var scoreDetailData = await GetSurveyQuestionScoreDetailAsync(projectId);
        if (scoreDetailData != null && scoreDetailData.Questions.Any())
        {
            var answerSheet = workbook.Worksheets.Add("Cevap Dağılımı");

            // Başlık
            answerSheet.Cell(1, 1).Value = "Proje: " + project.Name;
            answerSheet.Cell(1, 1).Style.Font.Bold = true;
            answerSheet.Cell(1, 1).Style.Font.FontSize = 14;
            answerSheet.Range(1, 1, 1, 5).Merge();

            answerSheet.Cell(2, 1).Value = $"Toplam Yanıt: {scoreDetailData.TotalResponses}";
            answerSheet.Range(2, 1, 2, 5).Merge();

            // Headers
            var ansHeaderRow = 4;
            answerSheet.Cell(ansHeaderRow, 1).Value = "Soru";
            answerSheet.Cell(ansHeaderRow, 2).Value = "Cevap";
            answerSheet.Cell(ansHeaderRow, 3).Value = "Seçim";
            answerSheet.Cell(ansHeaderRow, 4).Value = "Toplam";
            answerSheet.Cell(ansHeaderRow, 5).Value = "Oran (%)";

            var ansHeaderRange = answerSheet.Range(ansHeaderRow, 1, ansHeaderRow, 5);
            ansHeaderRange.Style.Font.Bold = true;
            ansHeaderRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            ansHeaderRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Data - Flat tablo (Soru | Cevap | Seçim | Toplam | Oran)
            var ansRow = ansHeaderRow + 1;
            foreach (var q in scoreDetailData.Questions)
            {
                if (q.AnswerDistributions != null && q.AnswerDistributions.Any())
                {
                    foreach (var ans in q.AnswerDistributions)
                    {
                        answerSheet.Cell(ansRow, 1).Value = q.QuestionText;
                        answerSheet.Cell(ansRow, 2).Value = ans.AnswerText;
                        answerSheet.Cell(ansRow, 3).Value = ans.SelectionCount;
                        answerSheet.Cell(ansRow, 4).Value = q.ResponseCount;
                        answerSheet.Cell(ansRow, 5).Value = ans.Percentage;
                        answerSheet.Cell(ansRow, 5).Style.NumberFormat.Format = "0.0";
                        ansRow++;
                    }
                }
            }

            // Auto-fit columns
            answerSheet.Columns().AdjustToContents();
            ExcelHelper.ApplyLongTextColumnStyles(answerSheet);
            answerSheet.Column(1).Width = 50; // Soru sütunu geniş
            answerSheet.Column(2).Width = 30; // Cevap sütunu
        }

        // Export
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"SoruPuanDagilimi_{project.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.xlsx";

        return new ExcelExportDto
        {
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = fileName
        };
    }

    /// <summary>
    /// Proje bazlı soru puan detayı ve cevap dağılımları (Puan Detayı modalı için)
    /// Her soru için: soru metni, ortalama puan %, alt kriterlerin seçilme sayısı ve yüzdesi
    /// </summary>
    public async Task<SurveyQuestionScoreDetailResultDto?> GetSurveyQuestionScoreDetailAsync(int projectId)
    {
        // Proje bilgisi
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
            return null;

        // Bu projedeki tamamlanmış değerlendirmeler
        var evaluationIds = await _context.Evaluations
            .Where(e => !e.IsDeleted &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.Assignment.ProjectId == projectId)
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

        // Cevapları ve soruları getir (alt kriterlerle birlikte)
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

            // Cezalı sorular için: Penalty uygulanma oranı (negatif etki)
            if (question.ScoringTypeId == ScoringTypes.Ids.Penalty)
            {
                // Penalty uygulanan cevap sayısı
                var penaltyAppliedCount = group.Count(a => a.IsPenaltyApplied);
                // Yüzde olarak göster (ne kadar ceza uygulandı)
                avgScorePercentage = responseCount > 0
                    ? Math.Round((decimal)penaltyAppliedCount / responseCount * 100, 1)
                    : 0;
            }
            // Normal puanlı sorular
            else if (question.WeightPoints > 0 && responseCount > 0)
            {
                // Önce EarnedPoints'i kontrol et (zaten hesaplanmış olabilir)
                var answersWithEarned = group.Where(a => a.EarnedPoints.HasValue).ToList();
                if (answersWithEarned.Any())
                {
                    // EarnedPoints varsa onu kullan
                    var avgEarned = answersWithEarned.Average(a => a.EarnedPoints!.Value);
                    avgScorePercentage = Math.Round(avgEarned / question.WeightPoints * 100, 1);
                }
                else
                {
                    // Yoksa SubCriteria'lardan hesapla
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
                // Bu alt kriteri seçen cevap sayısı
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

    /// <summary>
    /// Proje bazlı soru puan detayı Excel export
    /// </summary>
    public async Task<ExcelExportDto> ExportSurveyQuestionScoreDetailAsync(int projectId)
    {
        var data = await GetSurveyQuestionScoreDetailAsync(projectId);

        if (data == null)
        {
            return new ExcelExportDto
            {
                FileName = "PuanDetayi_Bulunamadi.xlsx",
                FileContent = Array.Empty<byte>()
            };
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Puan Detayı");

        // Başlık bilgileri
        worksheet.Cell(1, 1).Value = "Proje:";
        worksheet.Cell(1, 2).Value = data.ProjectName;
        worksheet.Cell(2, 1).Value = "Toplam Yanıt:";
        worksheet.Cell(2, 2).Value = data.TotalResponses;
        worksheet.Cell(3, 1).Value = "Genel Ortalama:";
        worksheet.Cell(3, 2).Value = data.OverallAverageScore.HasValue ? $"%{data.OverallAverageScore:F1}" : "-";

        worksheet.Range(1, 1, 3, 1).Style.Font.Bold = true;

        // Tablo başlıkları
        var headerRow = 5;
        worksheet.Cell(headerRow, 1).Value = "Soru Grubu";
        worksheet.Cell(headerRow, 2).Value = "Soru";
        worksheet.Cell(headerRow, 3).Value = "Yanıt Sayısı";
        worksheet.Cell(headerRow, 4).Value = "Ortalama Puan (%)";
        worksheet.Cell(headerRow, 5).Value = "Cevap";
        worksheet.Cell(headerRow, 6).Value = "Puan";
        worksheet.Cell(headerRow, 7).Value = "Seçilme Sayısı";
        worksheet.Cell(headerRow, 8).Value = "Yüzde (%)";

        var headerRange = worksheet.Range(headerRow, 1, headerRow, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Veri satırları
        var row = headerRow + 1;
        foreach (var question in data.Questions)
        {
            var questionStartRow = row;

            if (question.AnswerDistributions.Any())
            {
                foreach (var answer in question.AnswerDistributions)
                {
                    worksheet.Cell(row, 1).Value = question.GroupName ?? "-";
                    worksheet.Cell(row, 2).Value = question.QuestionText;
                    worksheet.Cell(row, 3).Value = question.ResponseCount;
                    worksheet.Cell(row, 4).Value = question.AverageScorePercentage.HasValue
                        ? $"%{question.AverageScorePercentage:F1}"
                        : "-";
                    worksheet.Cell(row, 5).Value = answer.AnswerText;
                    worksheet.Cell(row, 6).Value = answer.Points;
                    worksheet.Cell(row, 7).Value = answer.SelectionCount;
                    worksheet.Cell(row, 8).Value = $"%{answer.Percentage:F1}";
                    row++;
                }

                // Soru bilgilerini birleştir (merge cells)
                if (row > questionStartRow + 1)
                {
                    worksheet.Range(questionStartRow, 1, row - 1, 1).Merge();
                    worksheet.Range(questionStartRow, 2, row - 1, 2).Merge();
                    worksheet.Range(questionStartRow, 3, row - 1, 3).Merge();
                    worksheet.Range(questionStartRow, 4, row - 1, 4).Merge();
                }
            }
            else
            {
                // Alt kriter yoksa sadece soru bilgisi
                worksheet.Cell(row, 1).Value = question.GroupName ?? "-";
                worksheet.Cell(row, 2).Value = question.QuestionText;
                worksheet.Cell(row, 3).Value = question.ResponseCount;
                worksheet.Cell(row, 4).Value = question.AverageScorePercentage.HasValue
                    ? $"%{question.AverageScorePercentage:F1}"
                    : "-";
                worksheet.Cell(row, 5).Value = "-";
                worksheet.Cell(row, 6).Value = "-";
                worksheet.Cell(row, 7).Value = "-";
                worksheet.Cell(row, 8).Value = "-";
                row++;
            }
        }

        // Sütun genişlikleri
        worksheet.Column(1).Width = 20;
        worksheet.Column(2).Width = 50;
        worksheet.Column(3).Width = 12;
        worksheet.Column(4).Width = 15;
        worksheet.Column(5).Width = 40;
        worksheet.Column(6).Width = 10;
        worksheet.Column(7).Width = 15;
        worksheet.Column(8).Width = 12;

        // Kenarlıklar
        var dataRange = worksheet.Range(headerRow, 1, row - 1, 8);
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var safeProjectName = string.Join("_", data.ProjectName.Split(Path.GetInvalidFileNameChars()));

        return new ExcelExportDto
        {
            FileName = $"PuanDetayi_{safeProjectName}_{DateTime.Now:yyyyMMdd}.xlsx",
            FileContent = stream.ToArray()
        };
    }

    // ===== PERSONEL SORU BAZLI PERFORMANS RAPORU =====

    /// <summary>
    /// Personel Soru Bazlı Performans Raporu - Tablo görünümü için
    /// Personel + GroupName bazında ortalama puan (pivot tablo yapısı)
    /// </summary>
    public async Task<PersonnelQuestionPerformanceReportDto> GetPersonnelQuestionPerformanceAsync(PersonnelQuestionPerformanceFilterDto filter)
    {
        // Temel sorgu - tamamlanmış değerlendirmeler
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Where(e => !e.IsDeleted &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.EvaluatedCustomerPersonnelId != null);

        // Filtreler - çoğul parametreler (KURALLAR.md Bölüm 20)
        if (filter.CustomerIds?.Any() == true)
        {
            query = query.Where(e => e.Assignment.Project.CustomerId.HasValue && filter.CustomerIds.Contains(e.Assignment.Project.CustomerId.Value));
        }

        if (filter.ProjectIds?.Any() == true)
        {
            query = query.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));
        }

        if (filter.OrganizationIds?.Any() == true)
        {
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && filter.OrganizationIds.Contains(e.EvaluatedOrganizationId.Value));
        }

        if (filter.PersonnelIds?.Any() == true)
        {
            query = query.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && filter.PersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        if (filter.PeriodIds?.Any() == true)
        {
            query = query.Where(e => e.AssignmentPeriodId.HasValue && filter.PeriodIds.Contains(e.AssignmentPeriodId.Value));
        }

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(e => (e.CallDate ?? e.CompletedAt ?? e.CreatedAt) >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(e => (e.CallDate ?? e.CompletedAt ?? e.CreatedAt) <= maxEnd);
        }

        // Verileri çek
        var evaluations = await query.ToListAsync();

        // Tüm benzersiz GroupName'leri bul
        var allGroupNames = evaluations
            .SelectMany(e => e.Answers.Where(a => !a.Question.IsDeleted && !string.IsNullOrEmpty(a.Question.GroupName)))
            .Select(a => a.Question.GroupName!)
            .Distinct()
            .OrderBy(g => g)
            .ToList();

        // Personel + GroupName bazında gruplama
        var rawData = evaluations
            .SelectMany(e => e.Answers
                .Where(a => !a.Question.IsDeleted && !string.IsNullOrEmpty(a.Question.GroupName))
                .Select(a => new
                {
                    PersonnelId = e.EvaluatedCustomerPersonnelId!.Value,
                    PersonnelName = e.EvaluatedCustomerPersonnel != null
                        ? $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}".Trim()
                        : "",
                    Department = e.EvaluatedCustomerPersonnel?.Department,
                    OrganizationName = e.EvaluatedOrganization?.Name,
                    GroupName = a.Question.GroupName ?? "",
                    EvalId = e.Id,
                    Score = a.AnswerNumeric,
                    MaxPoints = a.Question.MaxPoints,
                    IsError = a.AnswerNumeric.HasValue && a.Question.MaxPoints > 0 && a.AnswerNumeric.Value < a.Question.MaxPoints
                }))
            .ToList();

        // Personel bazında gruplama
        var personnelGroups = rawData
            .GroupBy(x => new { x.PersonnelId, x.PersonnelName, x.Department, x.OrganizationName })
            .ToList();

        var rows = new List<PersonnelQuestionPerformanceRowDto>();

        foreach (var personnelGroup in personnelGroups)
        {
            var row = new PersonnelQuestionPerformanceRowDto
            {
                PersonnelId = personnelGroup.Key.PersonnelId,
                PersonnelName = personnelGroup.Key.PersonnelName,
                Department = personnelGroup.Key.Department,
                OrganizationName = personnelGroup.Key.OrganizationName,
                TotalEvaluations = personnelGroup.Select(x => x.EvalId).Distinct().Count(),
                GroupScores = new List<GroupScoreDto>()
            };

            // Her GroupName için puan hesapla
            decimal totalScore = 0;
            int totalCount = 0;

            foreach (var groupName in allGroupNames)
            {
                var groupData = personnelGroup.Where(x => x.GroupName == groupName).ToList();

                if (groupData.Any())
                {
                    var validScores = groupData.Where(x => x.Score.HasValue && x.MaxPoints > 0).ToList();
                    var avgScore = validScores.Any()
                        ? Math.Round((decimal)validScores.Average(x => (double)(x.Score!.Value / x.MaxPoints * 100)), 1)
                        : (decimal?)null;

                    row.GroupScores.Add(new GroupScoreDto
                    {
                        GroupName = groupName,
                        AverageScore = avgScore,
                        EvaluationCount = groupData.Select(x => x.EvalId).Distinct().Count(),
                        ErrorCount = groupData.Count(x => x.IsError)
                    });

                    if (avgScore.HasValue)
                    {
                        totalScore += avgScore.Value;
                        totalCount++;
                    }
                }
                else
                {
                    // Bu personel için bu grup verisi yok
                    row.GroupScores.Add(new GroupScoreDto
                    {
                        GroupName = groupName,
                        AverageScore = null,
                        EvaluationCount = 0,
                        ErrorCount = 0
                    });
                }
            }

            // Genel ortalama
            row.OverallAverage = totalCount > 0 ? Math.Round(totalScore / totalCount, 1) : 0;

            rows.Add(row);
        }

        return new PersonnelQuestionPerformanceReportDto
        {
            GroupNames = allGroupNames,
            Rows = rows.OrderBy(r => r.PersonnelName).ToList(),
            TotalEvaluations = evaluations.Count
        };
    }

    /// <summary>
    /// Personel Soru Bazlı Performans Raporu - Excel Export
    /// Proje + Personel + GroupName + Periyot bazında ortalama puan ve hata sayısı
    /// </summary>
    public async Task<ExcelExportDto> ExportPersonnelQuestionPerformanceReportAsync(PersonnelQuestionPerformanceFilterDto filter)
    {
        // Temel sorgu - tamamlanmış değerlendirmeler
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Where(e => !e.IsDeleted &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.EvaluatedCustomerPersonnelId != null);

        // Filtreler - çoğul parametreler (KURALLAR.md Bölüm 20)
        if (filter.CustomerIds?.Any() == true)
        {
            query = query.Where(e => e.Assignment.Project.CustomerId.HasValue && filter.CustomerIds.Contains(e.Assignment.Project.CustomerId.Value));
        }

        if (filter.ProjectIds?.Any() == true)
        {
            query = query.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));
        }

        if (filter.OrganizationIds?.Any() == true)
        {
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && filter.OrganizationIds.Contains(e.EvaluatedOrganizationId.Value));
        }

        if (filter.PersonnelIds?.Any() == true)
        {
            query = query.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && filter.PersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        if (filter.PeriodIds?.Any() == true)
        {
            query = query.Where(e => e.AssignmentPeriodId.HasValue && filter.PeriodIds.Contains(e.AssignmentPeriodId.Value));
        }

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(e => (e.CallDate ?? e.CompletedAt ?? e.CreatedAt) >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(e => (e.CallDate ?? e.CompletedAt ?? e.CreatedAt) <= maxEnd);
        }

        // Verileri çek
        var evaluations = await query.ToListAsync();

        // Gruplama için veri hazırla
        var reportData = new List<PersonnelQuestionPerformanceDto>();

        // Her değerlendirme için cevapları grupla
        var groupedData = evaluations
            .SelectMany(e => e.Answers
                .Where(a => !a.Question.IsDeleted && !string.IsNullOrEmpty(a.Question.GroupName))
                .Select(a => new
                {
                    ProjectName = e.Assignment?.Project?.Name ?? "",
                    PersonnelId = e.EvaluatedCustomerPersonnelId!.Value,
                    PersonnelName = e.EvaluatedCustomerPersonnel != null
                        ? $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}".Trim()
                        : "",
                    Department = e.EvaluatedCustomerPersonnel?.Department,
                    GroupName = a.Question.GroupName ?? "",
                    EvalDate = e.CallDate ?? e.CompletedAt ?? e.CreatedAt,
                    Score = a.AnswerNumeric,
                    MaxPoints = a.Question.MaxPoints,
                    IsError = a.AnswerNumeric.HasValue && a.Question.MaxPoints > 0 && a.AnswerNumeric.Value < a.Question.MaxPoints
                }))
            .GroupBy(x => new
            {
                x.ProjectName,
                x.PersonnelId,
                x.PersonnelName,
                x.Department,
                x.GroupName,
                Year = x.EvalDate.Year,
                Month = x.EvalDate.Month
            })
            .Select(g => new PersonnelQuestionPerformanceDto
            {
                ProjectName = g.Key.ProjectName,
                PersonnelId = g.Key.PersonnelId,
                PersonnelName = g.Key.PersonnelName,
                Department = g.Key.Department,
                GroupName = g.Key.GroupName,
                Year = g.Key.Year,
                PeriodMonth = $"{g.Key.Year}{g.Key.Month:D2}",
                AverageScore = g.Where(x => x.Score.HasValue && x.MaxPoints > 0).Any()
                    ? Math.Round((decimal)g.Where(x => x.Score.HasValue && x.MaxPoints > 0)
                        .Average(x => (double)(x.Score!.Value / x.MaxPoints * 100)), 2)
                    : 0,
                ErrorCount = g.Count(x => x.IsError),
                EvaluationCount = g.Select(x => x.EvalDate).Distinct().Count()
            })
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.PersonnelName)
            .ThenBy(x => x.GroupName)
            .ThenBy(x => x.PeriodMonth)
            .ToList();

        // Excel oluştur
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var sheet = workbook.Worksheets.Add("Personel Soru Performansı");

        // Başlıklar
        var headers = new[] { "Proje", "Müşteri Temsilcisi", "Departman", "Kontrol Sorusu", "Periyot", "Periyot (Ay)", "Ortalama Puan", "Hata Sayısı", "Değerlendirme Sayısı" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }
        sheet.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        sheet.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

        // Veriler
        var row = 2;
        foreach (var item in groupedData)
        {
            sheet.Cell(row, 1).Value = item.ProjectName;
            sheet.Cell(row, 2).Value = item.PersonnelName;
            sheet.Cell(row, 3).Value = item.Department ?? "";
            sheet.Cell(row, 4).Value = item.GroupName;
            sheet.Cell(row, 5).Value = item.Year;
            sheet.Cell(row, 6).Value = item.PeriodMonth;
            sheet.Cell(row, 7).Value = item.AverageScore;
            sheet.Cell(row, 8).Value = item.ErrorCount;
            sheet.Cell(row, 9).Value = item.EvaluationCount;
            row++;
        }

        // Kolon genişlikleri
        sheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(sheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Personel_Soru_Performans_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== ENNEAGRAM SONUÇLARI RAPORU =====

    /// <summary>
    /// Enneagram tipindeki checklist'leri kullanan projeleri listele
    /// </summary>
    public async Task<List<EnneagramProjectListItemDto>> GetEnneagramProjectsAsync()
    {
        // Enneagram checklist'leri olan projeleri bul
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        if (!enneagramChecklistIds.Any())
            return new List<EnneagramProjectListItemDto>();

        // Project.ChecklistId üzerinden projeleri bul (direkt ilişki)
        var projects = await _context.Projects
            .Include(p => p.Customer)
            .Where(p => enneagramChecklistIds.Contains(p.ChecklistId) && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var result = new List<EnneagramProjectListItemDto>();

        foreach (var project in projects)
        {
            // Tamamlanan anket sayısı
            var completedCount = await _context.Evaluations
                .Where(e => e.Assignment.ProjectId == project.Id &&
                           e.StatusId == EvaluationStatuses.Ids.Completed)
                .CountAsync();

            // Son yanıt tarihi
            var lastResponse = await _context.Evaluations
                .Where(e => e.Assignment.ProjectId == project.Id &&
                           e.StatusId == EvaluationStatuses.Ids.Completed)
                .OrderByDescending(e => e.CompletedAt)
                .Select(e => e.CompletedAt)
                .FirstOrDefaultAsync();

            result.Add(new EnneagramProjectListItemDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                CustomerName = project.Customer?.CompanyName,
                ProjectCode = project.Code,
                TotalResponses = completedCount,
                LastResponseAt = lastResponse,
                IsActive = project.IsActive
            });
        }

        return result;
    }

    /// <summary>
    /// Enneagram sonuçlarını listele (filtrelenebilir)
    /// </summary>
    public async Task<(List<EnneagramResultListDto> Results, EnneagramSummaryDto Summary, int TotalCount)> GetEnneagramResultsAsync(EnneagramFilterDto filter)
    {
        // Enneagram checklist'leri
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        if (!enneagramChecklistIds.Any())
            return (new List<EnneagramResultListDto>(), new EnneagramSummaryDto(), 0);

        // Temel sorgu - Project.ChecklistId üzerinden filtrele
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => !e.IsDeleted &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.Assignment.Project != null &&
                        enneagramChecklistIds.Contains(e.Assignment.Project.ChecklistId));

        // Proje filtresi
        if (filter.ProjectIds?.Any() == true)
        {
            query = query.Where(e => filter.ProjectIds.Contains(e.Assignment.ProjectId));
        }

        // Arama filtresi (isim veya e-posta) - EvaluatedCustomerPersonnel üzerinden
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(e =>
                (e.EvaluatedCustomerPersonnel != null &&
                    ((e.EvaluatedCustomerPersonnel.FirstName != null && e.EvaluatedCustomerPersonnel.FirstName.ToLower().Contains(term)) ||
                     (e.EvaluatedCustomerPersonnel.LastName != null && e.EvaluatedCustomerPersonnel.LastName.ToLower().Contains(term)) ||
                     (e.EvaluatedCustomerPersonnel.Email != null && e.EvaluatedCustomerPersonnel.Email.ToLower().Contains(term)))));
        }

        // Tarih filtresi
        if (filter.DateRanges?.Any() == true)
        {
            var dateRange = filter.DateRanges.First();
            if (dateRange.StartDate.HasValue)
            {
                var startUtc = DateTime.SpecifyKind(dateRange.StartDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(e => e.CompletedAt >= startUtc);
            }
            if (dateRange.EndDate.HasValue)
            {
                var endUtc = DateTime.SpecifyKind(dateRange.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(e => e.CompletedAt <= endUtc);
            }
        }

        var totalCount = await query.CountAsync();

        // Sayfalama için sonuçları al
        var evaluations = await query
            .OrderByDescending(e => e.CompletedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        // External invitations for respondent info
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

        // Kişilik tipi istatistikleri için
        var dominantTypes = new Dictionary<string, int>();

        foreach (var eval in evaluations)
        {
            var scores = CalculateEnneagramScores(eval);
            var dominantScore = scores.OrderByDescending(s => s.Percentage).FirstOrDefault();

            // Track dominant types
            if (dominantScore != null && !string.IsNullOrEmpty(dominantScore.PersonalityType))
            {
                if (!dominantTypes.ContainsKey(dominantScore.PersonalityType))
                    dominantTypes[dominantScore.PersonalityType] = 0;
                dominantTypes[dominantScore.PersonalityType]++;
            }

            // Get respondent info
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
                ProjectId = eval.Assignment.ProjectId,
                ProjectName = eval.Assignment.Project?.Name ?? "",
                RespondentName = string.IsNullOrWhiteSpace(respondentName) ? null : respondentName,
                RespondentEmail = respondentEmail,
                DominantType = dominantScore?.PersonalityType,
                DominantPercentage = dominantScore?.Percentage,
                TotalScore = scores.Sum(s => s.TotalPoints),
                CompletedAt = eval.CompletedAt
            });
        }

        var mostCommonType = dominantTypes.OrderByDescending(x => x.Value).FirstOrDefault().Key;

        var projectCount = evaluations.Select(e => e.Assignment.ProjectId).Distinct().Count();

        var summary = new EnneagramSummaryDto
        {
            TotalResponses = totalCount,
            DominantType = mostCommonType,
            ProjectCount = projectCount,
            AverageCompletionRate = totalCount > 0 ? 100m : 0m // Her kayıt zaten tamamlanmış
        };

        return (results, summary, totalCount);
    }

    /// <summary>
    /// Enneagram sonuç detayı (kişilik tipi puanlarıyla)
    /// </summary>
    public async Task<EnneagramResultDetailDto?> GetEnneagramResultDetailAsync(int evaluationId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .FirstOrDefaultAsync(e => e.Id == evaluationId && !e.IsDeleted);

        if (evaluation == null)
            return null;

        // Get respondent info
        string? respondentName = null;
        string? respondentEmail = null;
        if (evaluation.EvaluatedCustomerPersonnel != null)
        {
            respondentName = $"{evaluation.EvaluatedCustomerPersonnel.FirstName} {evaluation.EvaluatedCustomerPersonnel.LastName}".Trim();
            respondentEmail = evaluation.EvaluatedCustomerPersonnel.Email;
        }
        else
        {
            // Check external invitation
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

        var scores = CalculateEnneagramScores(evaluation);
        var dominantScore = scores.OrderByDescending(s => s.Percentage).FirstOrDefault();

        return new EnneagramResultDetailDto
        {
            EvaluationId = evaluation.Id,
            RespondentName = string.IsNullOrWhiteSpace(respondentName) ? null : respondentName,
            RespondentEmail = respondentEmail,
            ProjectName = evaluation.Assignment.Project?.Name ?? "",
            DominantType = dominantScore?.PersonalityType,
            DominantPercentage = dominantScore?.Percentage,
            CompletedAt = evaluation.CompletedAt,
            Scores = scores
        };
    }

    /// <summary>
    /// Enneagram sonuçları Excel export
    /// </summary>
    public async Task<ExcelExportDto> ExportEnneagramResultsToExcelAsync(EnneagramFilterDto filter)
    {
        // Tüm sonuçları al (sayfalama yok)
        filter.PageSize = int.MaxValue;
        var (results, summary, _) = await GetEnneagramResultsAsync(filter);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Enneagram Sonuçları");

        // Header
        var headers = new[] { "Katılımcı", "E-posta", "Proje", "Baskın Tip", "Baskın Yüzde", "Toplam Puan", "Tamamlanma Tarihi" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Veri
        var row = 2;
        foreach (var result in results)
        {
            worksheet.Cell(row, 1).Value = result.RespondentName ?? "Anonim";
            worksheet.Cell(row, 2).Value = result.RespondentEmail ?? "";
            worksheet.Cell(row, 3).Value = result.ProjectName;
            worksheet.Cell(row, 4).Value = result.DominantType ?? "-";
            worksheet.Cell(row, 5).Value = result.DominantPercentage.HasValue ? $"%{result.DominantPercentage:F0}" : "-";
            worksheet.Cell(row, 6).Value = result.TotalScore ?? 0;
            worksheet.Cell(row, 7).Value = result.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
            row++;
        }

        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Enneagram_Sonuclari_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    /// <summary>
    /// Enneagram puanlarını hesapla (GroupName bazında)
    /// Her grup için: Toplam puan / Maksimum puan (her grupta 10 soru x 5 puan = 50)
    /// </summary>
    private List<EnneagramPersonalityScoreDto> CalculateEnneagramScores(Evaluation evaluation)
    {
        var scores = new List<EnneagramPersonalityScoreDto>();

        // Cevapları GroupName'e göre grupla
        var groupedAnswers = evaluation.Answers
            .Where(a => a.Question != null && !string.IsNullOrEmpty(a.Question.GroupName))
            .GroupBy(a => a.Question.GroupName!);

        foreach (var group in groupedAnswers)
        {
            // Gruptaki her cevabın puanını topla
            var totalPoints = 0;
            var questionCount = 0;

            foreach (var answer in group)
            {
                // En yüksek puanlı seçilen alt kriteri al (SubCriteriaSelections üzerinden)
                var selectedPoints = answer.SubCriteriaSelections
                    .Select(sc => sc.SubCriteria?.WeightPoints ?? 0)
                    .DefaultIfEmpty(0)
                    .Max();

                totalPoints += (int)selectedPoints;
                questionCount++;
            }

            // Maksimum puan: soru sayısı x 5 (en yüksek puan)
            var maxPoints = questionCount * 5;
            if (maxPoints == 0) maxPoints = 50; // Varsayılan 10 soru x 5 puan

            var percentage = maxPoints > 0 ? (decimal)totalPoints / maxPoints * 100 : 0;

            scores.Add(new EnneagramPersonalityScoreDto
            {
                PersonalityType = group.Key,
                TotalPoints = totalPoints,
                MaxPoints = maxPoints,
                Percentage = percentage
            });
        }

        // Yüzdeye göre sırala (yüksekten düşüğe)
        return scores.OrderByDescending(s => s.Percentage).ToList();
    }

    /// <summary>
    /// Enneagram proje bazlı kişilik tipi dağılımı (tüm yanıtların ortalaması)
    /// </summary>
    public async Task<EnneagramDistributionResultDto?> GetEnneagramDistributionAsync(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Checklist)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
            return null;

        // Checklist Enneagram tipinde mi kontrol et
        if (project.Checklist?.ChecklistTypeId != ChecklistTypes.Ids.Enneagram)
            return null;

        // Bu projedeki tamamlanmış değerlendirmeleri al
        var evaluations = await _context.Evaluations
            .Include(e => e.Assignment)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => e.Assignment.ProjectId == projectId &&
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

        // Tüm kişilik tiplerini ve puanlarını topla
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

        // Ortalamaları hesapla ve sırala
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

}
