using System.Linq.Expressions;
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

public class CustomerPortalReportService : ICustomerPortalReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILocalizationService _localizationService;
    private readonly IPerformanceSettingsService _performanceSettingsService;
    private readonly ICustomerScoreThresholdService _customerScoreThresholdService;

    public CustomerPortalReportService(ApplicationDbContext context, ILocalizationService localizationService, IPerformanceSettingsService performanceSettingsService, ICustomerScoreThresholdService customerScoreThresholdService)
    {
        _context = context;
        _localizationService = localizationService;
        _performanceSettingsService = performanceSettingsService;
        _customerScoreThresholdService = customerScoreThresholdService;
    }

    /// <summary>
    /// DateRange listesini OR mantığıyla uygular. Her range ayrı bir predicate olarak OR ile birleştirilir.
    /// navigationPath: Entity'den Evaluation'a ulaşmak için property yolu.
    ///   IQueryable&lt;Evaluation&gt; → boş
    ///   IQueryable&lt;Answer&gt; → "Evaluation"
    ///   IQueryable&lt;AnswerSubCriteriaSelection&gt; → "Answer", "Evaluation"
    /// FilterType: "callDate" veya boş → CallDate, "createdAt" → CreatedAt
    /// </summary>
    private static IQueryable<T> ApplyDateRangeOrFilter<T>(
        IQueryable<T> query, List<DateRangeFilter>? dateRanges, params string[] navigationPath)
    {
        if (dateRanges == null || !dateRanges.Any()) return query;

        var param = Expression.Parameter(typeof(T), "x");

        // Navigate to Evaluation entity
        Expression evalExpr = param;
        foreach (var prop in navigationPath)
            evalExpr = Expression.Property(evalExpr, prop);

        Expression? orBody = null;

        foreach (var dr in dateRanges)
        {
            var startUtc = dr.StartDate.HasValue
                ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc) : (DateTime?)null;
            var endUtc = dr.EndDate.HasValue
                ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : (DateTime?)null;

            // FilterType'a göre date alanını seç
            Expression dateProp;
            bool isNullable;
            if (dr.FilterType == "createdAt")
            {
                dateProp = Expression.Property(evalExpr, nameof(Evaluation.CreatedAt));
                isNullable = false;
            }
            else
            {
                // CustomerPortal varsayılan: CallDate
                dateProp = Expression.Property(evalExpr, nameof(Evaluation.CallDate));
                isNullable = true;
            }

            Expression? rangeExpr = null;
            if (startUtc.HasValue)
            {
                var startConst = isNullable
                    ? Expression.Constant((DateTime?)startUtc.Value, typeof(DateTime?))
                    : Expression.Constant(startUtc.Value, typeof(DateTime));
                rangeExpr = Expression.GreaterThanOrEqual(dateProp, startConst);
            }
            if (endUtc.HasValue)
            {
                var endConst = isNullable
                    ? Expression.Constant((DateTime?)endUtc.Value, typeof(DateTime?))
                    : Expression.Constant(endUtc.Value, typeof(DateTime));
                var leExpr = Expression.LessThanOrEqual(dateProp, endConst);
                rangeExpr = rangeExpr != null ? Expression.AndAlso(rangeExpr, leExpr) : leExpr;
            }
            if (rangeExpr != null)
            {
                if (isNullable)
                {
                    var notNull = Expression.NotEqual(dateProp, Expression.Constant(null, typeof(DateTime?)));
                    rangeExpr = Expression.AndAlso(notNull, rangeExpr);
                }
                orBody = orBody != null ? Expression.OrElse(orBody, rangeExpr) : rangeExpr;
            }
        }

        if (orBody != null)
            query = query.Where(Expression.Lambda<Func<T, bool>>(orBody, param));

        return query;
    }

    public async Task<EvaluationDetailReportDto?> GetEvaluationDetailAsync(int evaluationId)
    {
        var evaluation = await _context.Evaluations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(e => e.Project)
                    .ThenInclude(p => p!.Customer)
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Include(e => e.Evaluator)
            .Include(e => e.EvaluatorCustomerPersonnel)
            .Include(e => e.EvaluatedPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Include(e => e.CustomerDealer)
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
            ProjectId = evaluation.ProjectId,
            ProjectName = evaluation.Project?.Name ?? "",
            ProjectCode = evaluation.Project?.Code,
            ChecklistName = evaluation.Project?.Checklist?.Name ?? "",
            EvaluatorName = evaluation.Evaluator != null
                ? $"{evaluation.Evaluator.FirstName} {evaluation.Evaluator.LastName}"
                : (evaluation.EvaluatorCustomerPersonnel != null
                    ? $"{evaluation.EvaluatorCustomerPersonnel.FirstName} {evaluation.EvaluatorCustomerPersonnel.LastName}"
                    : null),
            EvaluatedPersonnelName = evaluation.EvaluatedCustomerPersonnel != null
                ? $"{evaluation.EvaluatedCustomerPersonnel.FirstName} {evaluation.EvaluatedCustomerPersonnel.LastName}"
                : (evaluation.EvaluatedPersonnel != null
                    ? $"{evaluation.EvaluatedPersonnel.FirstName} {evaluation.EvaluatedPersonnel.LastName}"
                    : evaluation.EvaluatedUnknownPersonnel
                        ?? evaluation.EvaluatedOrganization?.Name),
            CustomerName = evaluation.EvaluatedCustomerPersonnel?.Customer?.CompanyName
                ?? evaluation.Project?.Customer?.CompanyName,
            OrganizationName = evaluation.EvaluatedOrganization?.Name
                ?? (evaluation.EvaluatedCustomerPersonnel?.OrganizationAssignments?.Any() == true
                    ? string.Join(", ", evaluation.EvaluatedCustomerPersonnel.OrganizationAssignments
                        .Where(oa => oa.CustomerOrganization != null)
                        .Select(oa => oa.CustomerOrganization!.Name))
                    : null),
            DealerName = evaluation.CustomerDealer?.Name,
            SupervisorName = evaluation.EvaluatedCustomerPersonnel?.OrganizationAssignments?.Any() == true
                ? string.Join(", ", evaluation.EvaluatedCustomerPersonnel.OrganizationAssignments
                    .Where(oa => oa.Supervisor != null)
                    .Select(oa => $"{oa.Supervisor!.FirstName} {oa.Supervisor.LastName}")
                    .Distinct())
                : null,
            EvaluationDate = evaluation.ControlDate ?? evaluation.CreatedAt,
            CompletedAt = evaluation.CompletedAt,
            CreatedAt = evaluation.CreatedAt,
            TotalScore = evaluation.TotalScore,
            MaxScore = evaluation.MaxScore,
            ScorePercentage = evaluation.ScorePercentage,
            YellowCardCount = evaluation.YellowCardCount,
            RedCardCount = evaluation.RedCardCount,
            Status = EvaluationStatuses.GetById(evaluation.StatusId)?.SystemName ?? "",
            ProjectTypeId = evaluation.Project?.ProjectTypeId,
            CallId = evaluation.CallId,
            CallDate = evaluation.CallDate,
            CallTime = evaluation.CallTime,
            Duration = evaluation.Duration,
            VisitId = evaluation.VisitId,
            ControlDate = evaluation.ControlDate,
            ControlTime = evaluation.ControlTime,
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
                        IsNotApplicable = a.IsNotApplicable,
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
                    Comment = a.Notes,
                    IsNotApplicable = a.IsNotApplicable
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
                    IsPenaltyApplied = a.IsPenaltyApplied,
                    Notes = a.Notes,
                    RecommendationNotes = a.RecommendationNotes,
                    // View uyumluluğu için ek alanlar
                    AnswerNumeric = a.AnswerNumeric,
                    AnswerText = a.AnswerText,
                    EarnedPoints = a.EarnedPoints,
                    QuestionMaxPoints = a.Question.WeightPoints,
                    WeightPoints = a.Question.WeightPoints,
                    PenaltyType = PenaltyTypes.GetById(a.AppliedPenaltyTypeId)?.SystemName,
                    ScoringType = ScoringTypes.GetById(a.Question.ScoringTypeId)?.SystemName,
                    IsNotApplicable = a.IsNotApplicable,
                    SelectedSubCriteria = a.SubCriteriaSelections
                        .Select(s => s.SubCriteria.Description)
                        .ToList()
                }).ToList(),

            // Değerlendirme yorumu
            EvaluationComment = evaluation.EvaluationComment,

            // Genel notlar (şimdilik null - entity'de yoksa)
            Notes = null,

            // Açıklamalar (DescriptionsJson'dan)
            Descriptions = DeserializeDescriptions(evaluation.DescriptionsJson)
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
            worksheet.Cell(row, 6).Value = detail.ScorePercentage.HasValue ? $"%{detail.ScorePercentage:F2}" : "-";
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
            worksheet.Cell(row, 6).Value = detail.ScorePercentage.HasValue ? $"%{detail.ScorePercentage:F2}" : "-";
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
                answerDisplay = $"{earnedPts:F2}/{maxPts:F2}";
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

        // ===== AÇIKLAMALAR =====
        if (detail.Descriptions?.Any() == true)
        {
            row += 2; // Boş satır
            worksheet.Cell(row, 1).Value = "Açıklamalar";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            row++;
            worksheet.Cell(row, 1).Value = string.Join("\n", detail.Descriptions);
            worksheet.Range(row, 1, row, 6).Merge();
            worksheet.Cell(row, 1).Style.Alignment.WrapText = true;
        }

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
            FileName = $"Degerlendirme_Detay_{evaluationId}_{TurkeyTime.Now:yyyyMMddHHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }


    public async Task<PenaltyReportResultDto> GetPenaltiesReportAsync(PenaltyFilterDto filter)
    {
        var query = _context.Answers
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Project)
                    .ThenInclude(p => p!.Customer)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Evaluator)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedCustomerPersonnel)
                    .ThenInclude(cp => cp!.OrganizationAssignments)
                        .ThenInclude(oa => oa.CustomerOrganization)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedOrganization)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.AssignmentPeriod)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.CustomerDealer)
            .Include(a => a.Question)
                .ThenInclude(q => q.Checklist)
            .Include(a => a.SubCriteriaSelections)
                .ThenInclude(s => s.SubCriteria)
            .Where(a => a.AppliedPenaltyTypeId != PenaltyTypes.Ids.None)
            .AsQueryable();

        // Varsayılan proje tipi filtresi: Çağrı Denetimi (proje filtresi yoksa)
        if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(a => a.Evaluation.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(a => filter.ProjectIds.Contains(a.Evaluation.ProjectId));

        if (filter.CustomerIds?.Any() == true)
            query = query.Where(a => a.Evaluation.Project.CustomerId.HasValue && filter.CustomerIds.Contains(a.Evaluation.Project.CustomerId.Value));

        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatedOrganizationId.HasValue && filter.OrganizationIds.Contains(a.Evaluation.EvaluatedOrganizationId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(a => filter.ChecklistIds.Contains(a.Question.ChecklistId));

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(a.Evaluation.EvaluatorId.Value));

        if (filter.PersonnelIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatedCustomerPersonnelId.HasValue && filter.PersonnelIds.Contains(a.Evaluation.EvaluatedCustomerPersonnelId.Value));

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
            query = ApplyDateRangeOrFilter(query, filter.DateRanges, "Evaluation");
        }

        var penaltyAnswers = await query.ToListAsync();

        // Organizasyon adı helper: EvaluatedOrganization yoksa personelin organizasyon atamasından al
        string GetOrgName(Evaluation eval)
        {
            if (eval.EvaluatedOrganization != null)
                return eval.EvaluatedOrganization.Name;
            return eval.EvaluatedCustomerPersonnel?.OrganizationAssignments
                ?.Select(oa => oa.CustomerOrganization?.Name)
                .FirstOrDefault() ?? "";
        }

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
            .OrderByDescending(a => a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt)
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
                ProjectName = a.Evaluation.Project != null ? (a.Evaluation.Project.Code != null ? a.Evaluation.Project.Code + " - " + a.Evaluation.Project.Name : a.Evaluation.Project.Name) ?? "" : "",
                CustomerName = a.Evaluation.Project?.Customer?.CompanyName,
                OrganizationName = GetOrgName(a.Evaluation),
                ChecklistName = a.Question?.Checklist?.Name,
                EvaluatorName = a.Evaluation.Evaluator != null
                    ? $"{a.Evaluation.Evaluator.FirstName} {a.Evaluation.Evaluator.LastName}"
                    : null,
                EvaluatedPersonnelName = a.Evaluation.EvaluatedCustomerPersonnel != null
                    ? $"{a.Evaluation.EvaluatedCustomerPersonnel.FirstName} {a.Evaluation.EvaluatedCustomerPersonnel.LastName}"
                    : (a.Evaluation.EvaluatedPersonnel != null
                        ? $"{a.Evaluation.EvaluatedPersonnel.FirstName} {a.Evaluation.EvaluatedPersonnel.LastName}"
                        : a.Evaluation.EvaluatedUnknownPersonnel),
                DealerName = a.Evaluation.CustomerDealer != null ? a.Evaluation.CustomerDealer.Name : null,
                EvaluationDate = a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt,
                Notes = a.Notes,
                PeriodName = a.Evaluation.AssignmentPeriod != null
                    ? a.Evaluation.AssignmentPeriod.Name
                    : (a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt).ToString("yyyyMM"),
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
                CustomerName = a.Evaluation.Project?.Customer?.CompanyName ?? ""
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
                OrgName = GetOrgName(a.Evaluation)
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
            .GroupBy(a => new
            {
                Year = (a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt).Year,
                Month = (a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt).Month
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
                .ThenInclude(e => e.Project)
                    .ThenInclude(p => p!.Customer)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Evaluator)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedCustomerPersonnel)
                    .ThenInclude(cp => cp!.OrganizationAssignments)
                        .ThenInclude(oa => oa.CustomerOrganization)
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
            query = query.Where(a => a.Evaluation.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(a => filter.ProjectIds.Contains(a.Evaluation.ProjectId));

        if (filter.CustomerIds?.Any() == true)
            query = query.Where(a => a.Evaluation.Project.CustomerId.HasValue && filter.CustomerIds.Contains(a.Evaluation.Project.CustomerId.Value));

        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatedOrganizationId.HasValue && filter.OrganizationIds.Contains(a.Evaluation.EvaluatedOrganizationId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(a => filter.ChecklistIds.Contains(a.Question.ChecklistId));

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(a.Evaluation.EvaluatorId.Value));

        if (filter.PersonnelIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatedCustomerPersonnelId.HasValue && filter.PersonnelIds.Contains(a.Evaluation.EvaluatedCustomerPersonnelId.Value));

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
            query = ApplyDateRangeOrFilter(query, filter.DateRanges, "Evaluation");
        }

        var penaltyAnswers = await query.ToListAsync();

        // Organizasyon adı helper: EvaluatedOrganization yoksa personelin organizasyon atamasından al
        string GetExportOrgName(Evaluation eval)
        {
            if (eval.EvaluatedOrganization != null)
                return eval.EvaluatedOrganization.Name;
            return eval.EvaluatedCustomerPersonnel?.OrganizationAssignments
                ?.Select(oa => oa.CustomerOrganization?.Name)
                .FirstOrDefault() ?? "";
        }

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
        summarySheet.Cell(5, 2).Value = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");
        summarySheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(summarySheet);

        // Penalties detail sheet (tüm veriler - pagination yok)
        var penaltiesSheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.PenaltyEvaluations", defaultValue: "Cezalı Değerlendirmeler"));

        var headersList = new List<string>
        {
            await _localizationService.GetResourceAsync("Report.Date", defaultValue: "Tarih"),
            await _localizationService.GetResourceAsync("Report.Period", defaultValue: "Periyot"),
            await _localizationService.GetResourceAsync("Common.CallId", defaultValue: "Çağrı ID"),
            await _localizationService.GetResourceAsync("Report.CallTime", defaultValue: "Çağrı Saati"),
            await _localizationService.GetResourceAsync("Report.Duration", defaultValue: "Süre"),
            await _localizationService.GetResourceAsync("Common.Project", defaultValue: "Proje"),
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
        headersList.Add(await _localizationService.GetResourceAsync("Common.Note", defaultValue: "Not"));
        headersList.Add(await _localizationService.GetResourceAsync("Evaluation.EvaluationComment", defaultValue: "Denetim Yorumu"));

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
        foreach (var a in penaltyAnswers.OrderByDescending(a => a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt))
        {
            int col = 1;
            var evalDate = a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt;
            penaltiesSheet.Cell(row, col++).Value = evalDate.ToString("dd.MM.yyyy");
            // Periyot: AssignmentPeriod varsa adı, yoksa YYYYMM formatında
            var periodName = a.Evaluation.AssignmentPeriod != null
                ? a.Evaluation.AssignmentPeriod.Name
                : evalDate.ToString("yyyyMM");
            penaltiesSheet.Cell(row, col++).Value = periodName;
            penaltiesSheet.Cell(row, col++).Value = a.Evaluation.CallId ?? "";
            penaltiesSheet.Cell(row, col++).Value = a.Evaluation.CallTime ?? "";
            penaltiesSheet.Cell(row, col++).Value = a.Evaluation.Duration ?? "";
            penaltiesSheet.Cell(row, col++).Value = a.Evaluation.Project?.Name ?? "";
            penaltiesSheet.Cell(row, col++).Value = GetExportOrgName(a.Evaluation);
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
            // Denetim Yorumu: DescriptionsJson + EvaluationComment birleşik
            var allDescriptions = new List<string>();
            if (!string.IsNullOrWhiteSpace(a.Evaluation.DescriptionsJson))
            {
                var descriptions = DeserializeDescriptions(a.Evaluation.DescriptionsJson);
                if (descriptions?.Any() == true)
                    allDescriptions.AddRange(descriptions);
            }
            if (!string.IsNullOrWhiteSpace(a.Evaluation.EvaluationComment))
            {
                allDescriptions.Add(a.Evaluation.EvaluationComment);
            }
            penaltiesSheet.Cell(row, col++).Value = allDescriptions.Count > 0 ? string.Join(", ", allDescriptions) : "";
            row++;
        }
        penaltiesSheet.Columns().AdjustToContents();
        // CallId: 3, SubCriteria: 11, Not: excludeEvaluator'a göre 14 veya 15, Denetim Yorumu: +1
        var noteCol = excludeEvaluator ? 14 : 15;
        var commentCol = noteCol + 1;
        ExcelHelper.ApplyLongTextColumnStyles(penaltiesSheet, callIdColumns: new[] { 3 }, noteColumns: new[] { noteCol, commentCol }, subCriteriaColumns: new[] { 11 });

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
                var evalDate = a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt;
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
            FileName = $"CezaliKL_Raporu_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
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

    /// <summary>
    /// Cevaplardaki yorumları + genel yorumu satır satır birleştirir.
    /// </summary>
    private static string? BuildCombinedNotes(Evaluation evaluation)
    {
        var parts = new List<string>();

        // Cevaplardaki yorumlar (Notes doluysa)
        if (evaluation.Answers != null)
        {
            foreach (var answer in evaluation.Answers.OrderBy(a => a.Question?.Order ?? 0))
            {
                if (!string.IsNullOrWhiteSpace(answer.Notes))
                {
                    parts.Add(answer.Notes.Trim());
                }
            }
        }

        // Genel yorum
        if (!string.IsNullOrWhiteSpace(evaluation.Notes))
        {
            parts.Add(evaluation.Notes.Trim());
        }

        // Denetim Yorumu
        if (!string.IsNullOrWhiteSpace(evaluation.EvaluationComment))
        {
            parts.Add(evaluation.EvaluationComment.Trim());
        }

        return parts.Count > 0 ? string.Join("\n", parts) : null;
    }

    private static List<string>? DeserializeDescriptions(string? descriptionsJson)
    {
        if (string.IsNullOrWhiteSpace(descriptionsJson))
            return null;
        try
        {
            var descriptions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(descriptionsJson);
            return descriptions?.Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
        }
        catch { return null; }
    }

    public async Task<IEnumerable<PersonnelListItemDto>> GetEvaluatedPersonnelListAsync(int? customerId = null, int? organizationId = null)
    {
        // Değerlendirmede bulunan personelleri getir (EvaluatedCustomerPersonnel = CustomerPersonnel entity)
        var query = _context.Evaluations
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Include(e => e.Project)
                    .ThenInclude(p => p.Customer)
            .Where(e => e.EvaluatedCustomerPersonnelId != null && e.StatusId == EvaluationStatuses.Ids.Completed)
            // Sadece Çağrı Denetimi projeleri
            .Where(e => e.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);

        // Müşteriye göre filtrele
        if (customerId.HasValue)
        {
            query = query.Where(e => e.Project.CustomerId == customerId.Value);
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
                CustomerId = e.Project.CustomerId,
                CustomerName = e.Project.Customer != null ? e.Project.Customer.CompanyName : "",
                OrganizationId = e.EvaluatedOrganizationId,
                OrganizationName = e.EvaluatedOrganization != null ? e.EvaluatedOrganization.Name : ""
            })
            .ToListAsync();

        // Evaluation'da organizasyon olmayan personeller için CustomerPersonnelOrganization'dan fallback
        var personnelIds = personnelFromEvaluations
            .Select(p => p.EvaluatedCustomerPersonnelId!.Value)
            .Distinct()
            .ToList();

        var personnelOrgMap = await _context.CustomerPersonnelOrganizations
            .Where(cpo => personnelIds.Contains(cpo.CustomerPersonnelId))
            .Select(cpo => new
            {
                cpo.CustomerPersonnelId,
                cpo.CustomerOrganizationId,
                OrganizationName = cpo.CustomerOrganization.Name
            })
            .GroupBy(x => x.CustomerPersonnelId)
            .ToDictionaryAsync(g => g.Key, g => g.First());

        return personnelFromEvaluations
            .GroupBy(p => p.EvaluatedCustomerPersonnelId)
            .Select(g =>
            {
                var withOrg = g.FirstOrDefault(x => x.OrganizationId.HasValue) ?? g.First();
                var orgId = withOrg.OrganizationId;
                var orgName = withOrg.OrganizationName;

                // Evaluation'da organizasyon yoksa mevcut atamadan al
                if (!orgId.HasValue && personnelOrgMap.TryGetValue(g.Key!.Value, out var cpoOrg))
                {
                    orgId = cpoOrg.CustomerOrganizationId;
                    orgName = cpoOrg.OrganizationName;
                }

                return new PersonnelListItemDto
                {
                    Id = g.Key!.Value,
                    Name = $"{withOrg.FirstName} {withOrg.LastName}",
                    Title = null,
                    CustomerId = withOrg.CustomerId,
                    CustomerName = withOrg.CustomerName,
                    OrganizationId = orgId,
                    OrganizationName = orgName
                };
            })
            .OrderBy(p => p.Name)
            .ToList();
    }

    /// <summary>
    /// Personelin değerlendirildiği projeleri getirir (karne filtresi için)
    /// </summary>

    public async Task<PersonnelReportCardDto?> GetPersonnelReportCardAsync(PersonnelReportCardFilterDto filter)
    {
        // CustomerPersonnel tablosundan personeli bul
        var personnel = await _context.CustomerPersonnel
            .FirstOrDefaultAsync(p => p.Id == filter.PersonnelId);

        if (personnel == null)
            return null;

        var query = _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Include(e => e.Evaluator)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => e.EvaluatedCustomerPersonnelId == filter.PersonnelId && e.StatusId == EvaluationStatuses.Ids.Completed);

        // İç/Dış Değerlendirme filtresi
        if (string.Equals(filter.EvaluationType, "internal", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(e => e.EvaluatorCustomerPersonnelId != null);
        }
        else if (string.Equals(filter.EvaluationType, "external", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(e => e.EvaluatorId != null);
        }

        // Proje filtresi: Çoğul ProjectIds veya varsayılan Çağrı Denetimi
        if (filter.ProjectIds?.Any() != true)
        {
            // Varsayılan: Çağrı Denetimi projeleri
            query = query.Where(e => e.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }
        else
        {
            query = query.Where(e => filter.ProjectIds.Contains(e.ProjectId));
        }

        // DateRanges pattern - CallDate ?? ControlDate, çoklu aralık OR ile birleştirilir
        if (filter.DateRanges?.Any() == true)
        {
            var validRanges = filter.DateRanges.Where(dr => dr.StartDate.HasValue || dr.EndDate.HasValue).ToList();
            if (validRanges.Any())
            {
                var rangePairs = validRanges.Select(dr => new
                {
                    Start = dr.StartDate.HasValue ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc) : (DateTime?)null,
                    End = dr.EndDate.HasValue ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1), DateTimeKind.Utc) : (DateTime?)null
                }).ToList();

                var param = Expression.Parameter(typeof(Evaluation), "e");
                var callDate = Expression.Property(param, nameof(Evaluation.CallDate));
                var controlDate = Expression.Property(param, nameof(Evaluation.ControlDate));
                var dateExpr = Expression.Coalesce(callDate, controlDate);

                Expression? orBody = null;
                foreach (var rp in rangePairs)
                {
                    Expression? rangeExpr = null;
                    if (rp.Start.HasValue)
                    {
                        var startConst = Expression.Constant((DateTime?)rp.Start.Value, typeof(DateTime?));
                        rangeExpr = Expression.GreaterThanOrEqual(dateExpr, startConst);
                    }
                    if (rp.End.HasValue)
                    {
                        var endConst = Expression.Constant((DateTime?)rp.End.Value, typeof(DateTime?));
                        var ltExpr = Expression.LessThan(dateExpr, endConst);
                        rangeExpr = rangeExpr != null ? Expression.AndAlso(rangeExpr, ltExpr) : ltExpr;
                    }
                    if (rangeExpr != null)
                        orBody = orBody != null ? Expression.OrElse(orBody, rangeExpr) : rangeExpr;
                }

                if (orBody != null)
                {
                    var lambda = Expression.Lambda<Func<Evaluation, bool>>(orBody, param);
                    query = query.Where(lambda);
                }
            }
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

            // Önce firmaya özel eşik kontrol et, yoksa global'e düş
            var customerThreshold = await _customerScoreThresholdService.GetByCustomerAndProjectTypeAsync(personnel.CustomerId, defaultProjectTypeId);
            decimal emptySuccessThreshold = 80;
            decimal emptyWarningThreshold = 60;
            if (customerThreshold != null)
            {
                emptySuccessThreshold = customerThreshold.SuccessThreshold;
                emptyWarningThreshold = customerThreshold.WarningThreshold;
            }
            else
            {
                var defaultSettings = await _performanceSettingsService.GetByProjectTypeIdAsync(defaultProjectTypeId);
                emptySuccessThreshold = defaultSettings?.SuccessThreshold ?? 80;
                emptyWarningThreshold = defaultSettings?.WarningThreshold ?? 60;
            }

            return new PersonnelReportCardDto
            {
                PersonnelId = personnel.Id,
                PersonnelName = $"{personnel.FirstName} {personnel.LastName}",
                Title = personnel.Title ?? "",
                Department = personnel.Department,
                SuccessThreshold = emptySuccessThreshold,
                WarningThreshold = emptyWarningThreshold
            };
        }

        // Özet istatistikler
        var completedScores = evaluations.Where(e => e.ScorePercentage.HasValue).Select(e => e.ScorePercentage!.Value).ToList();

        // Aylık trend (son 12 ay)
        var monthlyTrend = evaluations
            .GroupBy(e => new { Year = (e.CallDate ?? e.ControlDate ?? e.CreatedAt).Year, Month = (e.CallDate ?? e.ControlDate ?? e.CreatedAt).Month })
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
                    : 0,
                ErrorCount = g.Count(a => a.Question!.ScoringTypeId == ScoringTypes.Ids.Scored
                    ? (a.EarnedPoints ?? 0) < a.Question.WeightPoints
                    : a.IsPenaltyApplied)
            })
            .OrderByDescending(s => s.PercentageScore)
            .ToList();

        // Değerlendirmeler (filtreye göre tümü)
        var recentEvaluations = evaluations
            .OrderByDescending(e => e.CallDate ?? e.ControlDate ?? e.CreatedAt)
            .Select(e => new PersonnelEvaluationSummaryDto
            {
                EvaluationId = e.Id,
                EvaluationDate = e.CallDate ?? e.ControlDate ?? e.CreatedAt,
                ProjectName = e.Project != null ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name) ?? "" : "",
                ChecklistName = e.Project.Checklist?.Name ?? "",
                EvaluatorName = e.Evaluator != null ? $"{e.Evaluator.FirstName} {e.Evaluator.LastName}" : null,
                ScorePercentage = e.ScorePercentage ?? 0,
                YellowCards = e.YellowCardCount,
                RedCards = e.RedCardCount,
                Status = EvaluationStatuses.GetById(e.StatusId)?.SystemName ?? "",
                CallId = e.CallId,
                CallTime = e.CallTime,
                Duration = e.Duration,
                // Cevaplardaki yorumlar + genel yorum birleştirilir
                Notes = BuildCombinedNotes(e)
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

        // Proje tipine göre threshold değerlerini al: önce firmaya özel, yoksa global
        var projectTypeId = evaluations
            .Select(e => e.Project?.ProjectTypeId)
            .FirstOrDefault(pt => pt.HasValue) ?? ProjectTypes.Ids.CallAuditing;

        decimal successThreshold = 80;
        decimal warningThreshold = 60;
        var customerThresholdResult = await _customerScoreThresholdService.GetByCustomerAndProjectTypeAsync(personnel.CustomerId, projectTypeId);
        if (customerThresholdResult != null)
        {
            successThreshold = customerThresholdResult.SuccessThreshold;
            warningThreshold = customerThresholdResult.WarningThreshold;
        }
        else
        {
            var performanceSettings = await _performanceSettingsService.GetByProjectTypeIdAsync(projectTypeId);
            successThreshold = performanceSettings?.SuccessThreshold ?? 80;
            warningThreshold = performanceSettings?.WarningThreshold ?? 60;
        }

        // Süreç analizi: Proje + Soru + Periyot bazlı ortalama puan ve hata sayısı
        var processAnalysis = evaluations
            .SelectMany(e => e.Answers
                .Where(a => a.Question != null && !string.IsNullOrEmpty(a.Question.Text))
                .Select(a => new
                {
                    ProjectName = e.Project != null ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name) ?? "" : "",
                    QuestionText = a.Question!.Text,
                    EvalDate = e.CallDate ?? e.ControlDate ?? e.CreatedAt,
                    EarnedPoints = a.EarnedPoints ?? 0,
                    WeightPoints = a.Question.WeightPoints,
                    IsError = a.Question.ScoringTypeId == ScoringTypes.Ids.Scored
                        ? (a.EarnedPoints ?? 0) < a.Question.WeightPoints
                        : a.IsPenaltyApplied
                }))
            .GroupBy(x => new { x.ProjectName, x.QuestionText, Year = x.EvalDate.Year, Month = x.EvalDate.Month })
            .Select(g => new PersonnelProcessAnalysisDto
            {
                ProjectName = g.Key.ProjectName,
                Department = personnel.Department ?? "",
                QuestionText = g.Key.QuestionText,
                Year = g.Key.Year,
                PeriodMonth = $"{g.Key.Year}{g.Key.Month:D2}",
                AverageScore = g.Sum(x => x.WeightPoints) > 0
                    ? Math.Round(g.Sum(x => x.EarnedPoints) / g.Sum(x => x.WeightPoints) * 100, 2)
                    : 0,
                ErrorCount = g.Count(x => x.IsError)
            })
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.QuestionText)
            .ThenBy(x => x.PeriodMonth)
            .ToList();

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
            ProcessAnalysis = processAnalysis,
            SuccessThreshold = successThreshold,
            WarningThreshold = warningThreshold
        };
    }


    public async Task<ExcelExportDto> ExportPersonnelReportCardToExcelAsync(PersonnelReportCardFilterDto filter)
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
        infoSheet.Cell(8, 2).Value = $"{report.AverageScore:F2}%";
        infoSheet.Cell(9, 1).Value = "En Yüksek Puan:";
        infoSheet.Cell(9, 2).Value = $"{report.BestScore:F2}%";
        infoSheet.Cell(10, 1).Value = "En Düşük Puan:";
        infoSheet.Cell(10, 2).Value = $"{report.WorstScore:F2}%";
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
            trendSheet.Cell(row, 3).Value = $"{trend.AverageScore:F2}%";
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
        groupSheet.Cell(1, 4).Value = "Hata Sayısı";
        groupSheet.Cell(1, 4).Style.Font.Bold = true;

        row = 2;
        foreach (var group in report.GroupPerformances)
        {
            groupSheet.Cell(row, 1).Value = group.GroupName;
            groupSheet.Cell(row, 2).Value = group.EvaluationCount;
            groupSheet.Cell(row, 3).Value = $"{group.PercentageScore:F2}%";
            groupSheet.Cell(row, 4).Value = group.ErrorCount;
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
        evalSheet.Cell(1, 10).Value = "Yorum";
        evalSheet.Cell(1, 10).Style.Font.Bold = true;

        row = 2;
        foreach (var eval in report.RecentEvaluations)
        {
            evalSheet.Cell(row, 1).Value = eval.EvaluationDate?.ToString("dd.MM.yyyy") ?? "-";
            evalSheet.Cell(row, 2).Value = eval.CallId ?? "-";
            evalSheet.Cell(row, 3).Value = eval.CallTime ?? "-";
            evalSheet.Cell(row, 4).Value = eval.Duration ?? "-";
            evalSheet.Cell(row, 5).Value = eval.ProjectName;
            evalSheet.Cell(row, 6).Value = eval.ChecklistName;
            evalSheet.Cell(row, 7).Value = $"{eval.ScorePercentage:F2}%";
            evalSheet.Cell(row, 8).Value = eval.YellowCards;
            evalSheet.Cell(row, 9).Value = eval.RedCards;
            evalSheet.Cell(row, 10).Value = eval.Notes ?? "-";
            row++;
        }
        evalSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(evalSheet, callIdColumns: new[] { 2 }, noteColumns: new[] { 10 });

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
            analysisSheet.Cell(row, 3).Value = $"{strength.PercentageScore:F2}%";
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
            analysisSheet.Cell(row, 3).Value = $"{weakness.PercentageScore:F2}%";
            row++;
        }
        analysisSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(analysisSheet);

        // Süreç Analizi
        if (report.ProcessAnalysis.Any())
        {
            var processSheet = workbook.Worksheets.Add("Süreç Analizi");
            processSheet.Cell(1, 1).Value = "Proje";
            processSheet.Cell(1, 2).Value = "Müşteri Temsilcisi";
            processSheet.Cell(1, 3).Value = "Departman";
            processSheet.Cell(1, 4).Value = "Kontrol Sorusu";
            processSheet.Cell(1, 5).Value = "Periyot";
            processSheet.Cell(1, 6).Value = "Periyot (Ay)";
            processSheet.Cell(1, 7).Value = "Ortalama Puan";
            processSheet.Cell(1, 8).Value = "Hata Sayısı";

            var headerRange = processSheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            row = 2;
            foreach (var item in report.ProcessAnalysis)
            {
                processSheet.Cell(row, 1).Value = item.ProjectName;
                processSheet.Cell(row, 2).Value = report.PersonnelName;
                processSheet.Cell(row, 3).Value = item.Department;
                processSheet.Cell(row, 4).Value = item.QuestionText;
                processSheet.Cell(row, 5).Value = item.Year;
                processSheet.Cell(row, 6).Value = item.PeriodMonth;
                processSheet.Cell(row, 7).Value = item.AverageScore;
                processSheet.Cell(row, 8).Value = item.ErrorCount;
                row++;
            }
            processSheet.Columns().AdjustToContents();
            ExcelHelper.ApplyLongTextColumnStyles(processSheet);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"TemsilciKarnesi_{report.PersonnelName.Replace(" ", "_")}_{TurkeyTime.Now:yyyyMMdd}.xlsx",
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

        // Helper: Hücreye gri arka plan uygula
        void SetCellGrayBackground(XWPFTableCell cell)
        {
            var ctTc = cell.GetCTTc();
            var tcPr = ctTc.IsSetTcPr() ? ctTc.tcPr : ctTc.AddNewTcPr();
            var shd = tcPr.AddNewShd();
            shd.fill = "C0C0C0";
            shd.val = NPOI.OpenXmlFormats.Wordprocessing.ST_Shd.clear;
        }

        // Helper: Hücre run'ına font boyutu ayarla
        void SetCellFontSize(XWPFTableCell cell, int fontSize)
        {
            if (cell.Paragraphs.Count > 0 && cell.Paragraphs[0].Runs.Count > 0)
            {
                cell.Paragraphs[0].Runs[0].FontSize = fontSize;
            }
        }

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
        dateValueCell.SetText(TurkeyTime.Now.ToString("dd.MM.yyyy"));

        // Boş paragraf
        doc.CreateParagraph();

        // ===== PROJE + BAŞARI ORTALAMASI (Tablo olarak) =====
        var avgTable = doc.CreateTable(1, 2);
        avgTable.Width = 5000;

        // Sol: Proje adı (gri arka plan)
        var projectCell = avgTable.GetRow(0).GetCell(0);
        projectCell.SetText(projectName);
        SetCellGrayBackground(projectCell);
        if (projectCell.Paragraphs.Count > 0 && projectCell.Paragraphs[0].Runs.Count > 0)
        {
            projectCell.Paragraphs[0].Runs[0].IsBold = true;
            projectCell.Paragraphs[0].Runs[0].FontSize = 11;
        }

        // Sağ: Ortalama puan
        var scoreCell = avgTable.GetRow(0).GetCell(1);
        scoreCell.SetText($"  {report.AverageScore:F2}");
        if (scoreCell.Paragraphs.Count > 0 && scoreCell.Paragraphs[0].Runs.Count > 0)
        {
            scoreCell.Paragraphs[0].Runs[0].IsBold = true;
            scoreCell.Paragraphs[0].Runs[0].FontSize = 11;
        }

        doc.CreateParagraph();

        var scoreLabelPara = doc.CreateParagraph();
        var scoreLabelRun = scoreLabelPara.CreateRun();
        scoreLabelRun.SetText("BAŞARI ORTALAMASI");
        scoreLabelRun.IsBold = true;
        scoreLabelRun.FontSize = 10;

        doc.CreateParagraph();

        // ===== KONTROL SORULARI TABLOSU =====
        var questionTable = doc.CreateTable(report.GroupPerformances.Count + 1, 2);
        questionTable.Width = 5000;

        // Başlık satırı (gri arka plan)
        var qHeaderRow = questionTable.GetRow(0);
        qHeaderRow.GetCell(0).SetText("Kontrol Sorusu");
        qHeaderRow.GetCell(1).SetText("Puan");
        foreach (var cell in qHeaderRow.GetTableCells())
        {
            SetCellGrayBackground(cell);
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
            dataRow.GetCell(1).SetText($"{group.PercentageScore:F2}");
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

        // Başlık satırı (gri arka plan)
        var evalHeaderRow = evalTable.GetRow(0);
        string[] headers = { "Görüşme Tarihi", "Değerlendirilen Çağrı", "Denetim Yorumu", "Toplam Puan" };
        for (int c = 0; c < 4; c++)
        {
            var cell = evalHeaderRow.GetCell(c);
            cell.SetText(headers[c]);
            SetCellGrayBackground(cell);
            // Hücre genişliği ayarla (SetCellGrayBackground zaten tcPr oluşturmuş olabilir)
            var ctTcH = cell.GetCTTc();
            var tcPr = ctTcH.IsSetTcPr() ? ctTcH.tcPr : ctTcH.AddNewTcPr();
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
                $"{eval.ScorePercentage:F2}"
            };

            for (int c = 0; c < 4; c++)
            {
                var cell = dataRow.GetCell(c);

                // Denetim Yorumu sütunu (index 2): \n karakterlerini Word satır kırılmasına çevir
                if (c == 2 && !string.IsNullOrEmpty(values[c]) && values[c].Contains('\n'))
                {
                    // Mevcut boş paragrafı kullan
                    var para = cell.Paragraphs[0];
                    var lines = values[c].Split('\n');
                    for (int j = 0; j < lines.Length; j++)
                    {
                        var run = para.CreateRun();
                        run.SetText(lines[j]);
                        if (j < lines.Length - 1)
                            run.AddBreak(NPOI.XWPF.UserModel.BreakType.TEXTWRAPPING);
                    }
                }
                else
                {
                    cell.SetText(values[c]);
                }

                // Hücre genişliği ayarla (word wrap otomatik olur)
                var tcPr = cell.GetCTTc().AddNewTcPr();
                tcPr.AddNewTcW().w = colWidths[c].ToString();
                tcPr.tcW.type = NPOI.OpenXmlFormats.Wordprocessing.ST_TblWidth.dxa;

                // CallId sütunu (index 1): küçük font - uzun ID'ler tabloyu bozmasın
                if (c == 1)
                {
                    SetCellFontSize(cell, 8);
                }
            }
        }

        // Word dosyasını kaydet
        using var stream = new MemoryStream();
        doc.Write(stream);

        var safePersonnelName = string.Join("_", report.PersonnelName.Split(Path.GetInvalidFileNameChars()));

        return new ExcelExportDto
        {
            FileName = $"MT_Karne_{safePersonnelName}_{TurkeyTime.Now:yyyyMMdd}.docx",
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
                .ThenInclude(e => e.Project)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Project)
                    .ThenInclude(p => p.Checklist)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Evaluator)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedPersonnel)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedCustomerPersonnel)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.CustomerDealer)
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
            query = query.Where(a => a.Evaluation.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(a => filter.ProjectIds.Contains(a.Evaluation.ProjectId));

        if (filter.CustomerIds?.Any() == true)
            query = query.Where(a => a.Evaluation.Project.CustomerId.HasValue && filter.CustomerIds.Contains(a.Evaluation.Project.CustomerId.Value));

        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatedOrganizationId.HasValue && filter.OrganizationIds.Contains(a.Evaluation.EvaluatedOrganizationId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(a => filter.ChecklistIds.Contains(a.Evaluation.Project.ChecklistId));

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(a.Evaluation.EvaluatorId.Value));

        if (filter.PersonnelIds?.Any() == true)
            query = query.Where(a => a.Evaluation.EvaluatedCustomerPersonnelId.HasValue && filter.PersonnelIds.Contains(a.Evaluation.EvaluatedCustomerPersonnelId.Value));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            query = ApplyDateRangeOrFilter(query, filter.DateRanges, "Evaluation");
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
            .OrderByDescending(a => a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt)
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
                ChecklistName = a.Evaluation.Project.Checklist?.Name ?? "",
                Notes = a.Notes,
                RecommendationNotes = a.RecommendationNotes,
                GivenPoints = a.EarnedPoints,
                MaxPoints = a.Question?.WeightPoints ?? 0,
                PercentageScore = a.Question?.WeightPoints > 0 && a.EarnedPoints.HasValue
                    ? Math.Round((a.EarnedPoints.Value / a.Question.WeightPoints) * 100, 2)
                    : null,
                ProjectName = a.Evaluation.Project != null ? (a.Evaluation.Project.Code != null ? a.Evaluation.Project.Code + " - " + a.Evaluation.Project.Name : a.Evaluation.Project.Name) ?? "" : "",
                EvaluatorName = a.Evaluation.Evaluator != null
                    ? $"{a.Evaluation.Evaluator.FirstName} {a.Evaluation.Evaluator.LastName}"
                    : null,
                EvaluatedPersonnelName = a.Evaluation.EvaluatedCustomerPersonnel != null
                    ? $"{a.Evaluation.EvaluatedCustomerPersonnel.FirstName} {a.Evaluation.EvaluatedCustomerPersonnel.LastName}"
                    : (a.Evaluation.EvaluatedPersonnel != null
                        ? $"{a.Evaluation.EvaluatedPersonnel.FirstName} {a.Evaluation.EvaluatedPersonnel.LastName}"
                        : a.Evaluation.EvaluatedUnknownPersonnel),
                DealerName = a.Evaluation.CustomerDealer != null ? a.Evaluation.CustomerDealer.Name : null,
                EvaluationDate = a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt,
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
            .Include(e => e.Project)
            .Include(e => e.EvaluatedPersonnel)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.CustomerDealer)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed)
            .Where(e => !string.IsNullOrEmpty(e.Notes) || !string.IsNullOrEmpty(e.EvaluationComment))
            .AsQueryable();

        // Aynı filtreleri uygula
        if (filter.ProjectIds?.Any() == true)
            evaluationNotesQuery = evaluationNotesQuery.Where(e => filter.ProjectIds.Contains(e.ProjectId));
        else
            evaluationNotesQuery = evaluationNotesQuery.Where(e => e.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);

        if (filter.CustomerIds?.Any() == true)
            evaluationNotesQuery = evaluationNotesQuery.Where(e => e.Project.CustomerId.HasValue && filter.CustomerIds.Contains(e.Project.CustomerId.Value));

        // Organization filter (Supervisor için gerekli)
        if (filter.OrganizationIds?.Any() == true)
            evaluationNotesQuery = evaluationNotesQuery.Where(e => e.EvaluatedOrganizationId.HasValue && filter.OrganizationIds.Contains(e.EvaluatedOrganizationId.Value));

        if (filter.DateRanges?.Any() == true)
        {
            evaluationNotesQuery = ApplyDateRangeOrFilter(evaluationNotesQuery, filter.DateRanges);
        }

        var evaluationNotes = await evaluationNotesQuery
            .OrderByDescending(e => e.CallDate ?? e.ControlDate ?? e.CreatedAt)
            .Select(e => new EvaluationNoteDto
            {
                EvaluationId = e.Id,
                ProjectName = e.Project != null ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name) ?? "" : "",
                EvaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : (e.EvaluatedPersonnel != null
                        ? e.EvaluatedPersonnel.FirstName + " " + e.EvaluatedPersonnel.LastName
                        : e.EvaluatedUnknownPersonnel),
                DealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : null,
                EvaluationDate = e.CallDate ?? e.ControlDate ?? e.CreatedAt,
                CallId = e.CallId,
                ScorePercentage = e.ScorePercentage,
                Notes = e.Notes,
                EvaluationComment = e.EvaluationComment
            })
            .ToListAsync();

        // Populate Descriptions from DescriptionsJson
        if (evaluationNotes.Any())
        {
            var evalIds = evaluationNotes.Select(n => n.EvaluationId).ToList();
            var descriptionsRaw = await _context.Evaluations
                .Where(e => evalIds.Contains(e.Id) && e.DescriptionsJson != null)
                .Select(e => new { e.Id, e.DescriptionsJson })
                .ToListAsync();
            foreach (var note in evaluationNotes)
            {
                var raw = descriptionsRaw.FirstOrDefault(d => d.Id == note.EvaluationId);
                if (raw != null)
                    note.Descriptions = DeserializeDescriptions(raw.DescriptionsJson);
            }
        }

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
                .ThenInclude(e => e.Project)
            .Include(a => a.Question)
                .ThenInclude(q => q.Checklist)
            .Where(a => !string.IsNullOrEmpty(a.Notes) || !string.IsNullOrEmpty(a.RecommendationNotes))
            .Where(a => a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        // Varsayılan proje tipi filtresi: Çağrı Denetimi (proje filtresi yoksa)
        if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(a => a.Evaluation.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(a => filter.ProjectIds.Contains(a.Evaluation.ProjectId));

        if (filter.CustomerIds?.Any() == true)
            query = query.Where(a => a.Evaluation.Project.CustomerId.HasValue && filter.CustomerIds.Contains(a.Evaluation.Project.CustomerId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(a => filter.ChecklistIds.Contains(a.Evaluation.Project.ChecklistId));

        // DateRanges pattern (UTC dönüşümü Service'de)
        if (filter.DateRanges?.Any() == true)
        {
            query = ApplyDateRangeOrFilter(query, filter.DateRanges, "Evaluation");
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
                ? a.Evaluation.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing
                : filter.ProjectIds.Contains(a.Evaluation.ProjectId))
            .Where(a => filter.CustomerIds == null || !filter.CustomerIds.Any()
                || (a.Evaluation.Project.CustomerId.HasValue && filter.CustomerIds.Contains(a.Evaluation.Project.CustomerId.Value)))
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
                    .ThenInclude(e => e.Project)
            .Where(s => s.Answer.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            .Where(s => s.SubCriteria != null && !string.IsNullOrEmpty(s.SubCriteria.Description))
            .AsQueryable();

        // Varsayılan proje tipi filtresi: Çağrı Denetimi (proje filtresi yoksa)
        if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(s => s.Answer.Evaluation.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(s => filter.ProjectIds.Contains(s.Answer.Evaluation.ProjectId));

        if (filter.CustomerIds?.Any() == true)
            query = query.Where(s => s.Answer.Evaluation.Project.CustomerId.HasValue &&
                filter.CustomerIds.Contains(s.Answer.Evaluation.Project.CustomerId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(s => filter.ChecklistIds.Contains(s.Answer.Evaluation.Project.ChecklistId));

        // DateRanges filter
        if (filter.DateRanges?.Any() == true)
        {
            query = ApplyDateRangeOrFilter(query, filter.DateRanges, "Answer", "Evaluation");
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
                    .ThenInclude(e => e.Project)
                .Where(a => questionIds.Contains(a.QuestionId))
                .Where(a => a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
                .AsQueryable();

            // Aynı filtreleri uygula
            if (filter.ProjectIds?.Any() == true)
                answerQuery = answerQuery.Where(a => filter.ProjectIds.Contains(a.Evaluation.ProjectId));
            else
                answerQuery = answerQuery.Where(a => a.Evaluation.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);

            if (filter.CustomerIds?.Any() == true)
                answerQuery = answerQuery.Where(a => a.Evaluation.Project.CustomerId.HasValue &&
                    filter.CustomerIds.Contains(a.Evaluation.Project.CustomerId.Value));

            if (filter.ChecklistIds?.Any() == true)
                answerQuery = answerQuery.Where(a => filter.ChecklistIds.Contains(a.Evaluation.Project.ChecklistId));

            // Tarih filtreleri
            if (filter.DateRanges?.Any() == true)
            {
                answerQuery = ApplyDateRangeOrFilter(answerQuery, filter.DateRanges, "Evaluation");
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
        var summarySheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Summary", defaultValue: "Özet"));
        summarySheet.Cell(1, 1).Value = await _localizationService.GetResourceAsync("Report.Suggestions", defaultValue: "ÖNERİLER RAPORU");
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
        summarySheet.Cell(summaryRow, 2).Value = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");

        summarySheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(summarySheet);

        // Details sheet - Değerlendirici kolonu excludeEvaluator true ise eklenmez
        var detailsSheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.SuggestionsList", defaultValue: "Öneriler Listesi"));

        var headersList = new List<string>
        {
            await _localizationService.GetResourceAsync("Report.Date", defaultValue: "Tarih"),
            await _localizationService.GetResourceAsync("Common.Project", defaultValue: "Proje"),
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
        headersList.Add(await _localizationService.GetResourceAsync("Common.CallId", defaultValue: "Çağrı ID"));
        headersList.Add(await _localizationService.GetResourceAsync("Report.Penalty", defaultValue: "Ceza"));
        headersList.Add(await _localizationService.GetResourceAsync("Evaluation.EvaluationComment", defaultValue: "Denetim Yorumu"));

        var headers = headersList.ToArray();

        for (int i = 0; i < headers.Length; i++)
        {
            detailsSheet.Cell(1, i + 1).Value = headers[i];
            detailsSheet.Cell(1, i + 1).Style.Font.Bold = true;
            detailsSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Build evaluation comment lookup (DescriptionsJson + EvaluationComment)
        var suggestionEvalIds = report.Suggestions.Select(s => s.EvaluationId).Distinct().ToList();
        var evalCommentLookup = new Dictionary<int, string>();
        if (suggestionEvalIds.Any())
        {
            var evalCommentData = await _context.Evaluations
                .Where(e => suggestionEvalIds.Contains(e.Id))
                .Select(e => new { e.Id, e.DescriptionsJson, e.EvaluationComment })
                .ToListAsync();
            foreach (var ec in evalCommentData)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(ec.DescriptionsJson))
                {
                    var descriptions = DeserializeDescriptions(ec.DescriptionsJson);
                    if (descriptions?.Any() == true)
                        parts.AddRange(descriptions);
                }
                if (!string.IsNullOrWhiteSpace(ec.EvaluationComment))
                    parts.Add(ec.EvaluationComment);
                if (parts.Count > 0)
                    evalCommentLookup[ec.Id] = string.Join(", ", parts);
            }
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
            detailsSheet.Cell(row, col++).Value = item.PercentageScore.HasValue ? $"{item.PercentageScore:F2}%" : "";

            if (!excludeEvaluator)
            {
                detailsSheet.Cell(row, col++).Value = item.EvaluatorName ?? "";
            }

            detailsSheet.Cell(row, col++).Value = item.EvaluatedPersonnelName ?? "";
            detailsSheet.Cell(row, col++).Value = item.CallId ?? "";
            detailsSheet.Cell(row, col++).Value = item.PenaltyType ?? "";
            detailsSheet.Cell(row, col++).Value = evalCommentLookup.TryGetValue(item.EvaluationId, out var comment) ? comment : "";
            row++;
        }

        detailsSheet.Columns().AdjustToContents();
        // Notlar: 6, Öneri: 7, CallId: excludeEvaluator'a göre 12 veya 13, Denetim Yorumu: son kolon
        var callIdCol = excludeEvaluator ? 12 : 13;
        var evalCommentCol = callIdCol + 2;
        ExcelHelper.ApplyLongTextColumnStyles(detailsSheet, callIdColumns: new[] { callIdCol }, noteColumns: new[] { 6, 7, evalCommentCol });

        // Top Questions sheet
        var questionsSheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.TopSuggestedQuestions", defaultValue: "Top Önerilen Sorular"));
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
            questionsSheet.Cell(row, 5).Value = $"{q.AverageScore:F2}%";
            row++;
        }

        questionsSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(questionsSheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Oneriler_Raporu_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== ÇAĞRI DENETLEME RAPORU =====


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

    public async Task<ExcelExportDto> ExportQuestionGroupAverageReportAsync(ReportFilterDto filter)
    {
        var query = _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.AssignmentPeriod)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.ProjectId));

        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Project.ProjectTypeId));
        }
        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(e => filter.ChecklistIds.Contains(e.Project.ChecklistId));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            query = ApplyDateRangeOrFilter(query, filter.DateRanges);
        }

        // Customer filter (çoklu)
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                filter.CustomerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerIds?.Any() == true)
            query = query.Where(e => e.Project.CustomerId.HasValue && filter.ProjectCustomerIds.Contains(e.Project.CustomerId.Value));

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
                query = query.Where(e => e.EvaluatorCustomerPersonnelId != null);
            else if (hasOurs && !hasInternal)
                query = query.Where(e => e.EvaluatorCustomerPersonnelId == null);
        }

        var evaluations = await query.Take(10000).ToListAsync();

        // Flatten to answer level and calculate group averages
        var groupData = evaluations
            .SelectMany(e => e.Answers
                .Where(a => a.Question != null && !string.IsNullOrEmpty(a.Question.GroupName))
                .Select(a => new
                {
                    ProjectName = e.Project != null ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name) ?? "" : "",
                    PeriodName = e.AssignmentPeriod?.Name ?? FormatMonthYear(e.CallDate ?? e.ControlDate ?? e.CreatedAt),
                    Year = (e.CallDate ?? e.ControlDate ?? e.CreatedAt).Year,
                    GroupOrder = a.Question!.GroupName!.Split(' ').FirstOrDefault() ?? "",
                    GroupName = a.Question.GroupName,
                    EarnedPoints = a.EarnedPoints ?? 0,
                    MaxPoints = a.Question.WeightPoints,
                    EvaluationId = e.Id,
                    IsError = a.Question.ScoringTypeId == ScoringTypes.Ids.Scored
                        ? (a.EarnedPoints ?? 0) < a.Question.WeightPoints
                        : a.IsPenaltyApplied
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
                    : 0,
                ErrorCount = g.Count(x => x.IsError)
            })
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.GroupName)
            .ToList();

        // ===== SHEET 2: Soru Bazlı Ortalama =====
        // Soru bazlı gruplama (soruya göre)
        var questionData = evaluations
            .SelectMany(e => e.Answers
                .Where(a => a.Question != null)
                .Select(a => new
                {
                    ProjectName = e.Project != null ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name) ?? "" : "",
                    PeriodName = e.AssignmentPeriod?.Name ?? FormatMonthYear(e.CallDate ?? e.ControlDate ?? e.CreatedAt),
                    Year = (e.CallDate ?? e.ControlDate ?? e.CreatedAt).Year,
                    GroupName = a.Question!.GroupName ?? "",
                    QuestionOrder = a.Question!.Order,
                    QuestionText = a.Question.Text,
                    QuestionId = a.QuestionId,
                    EarnedPoints = a.EarnedPoints ?? 0,
                    MaxPoints = a.Question.WeightPoints,
                    EvaluationId = e.Id,
                    IsError = a.Question.ScoringTypeId == ScoringTypes.Ids.Scored
                        ? (a.EarnedPoints ?? 0) < a.Question.WeightPoints
                        : a.IsPenaltyApplied
                }))
            .GroupBy(x => new { x.ProjectName, x.PeriodName, x.Year, x.GroupName, x.QuestionId, x.QuestionText, x.QuestionOrder })
            .Select(g => new
            {
                ProjectName = $"{g.Key.ProjectName} {g.Key.PeriodName}",
                GroupName = g.Key.GroupName,
                QuestionText = g.Key.QuestionText,
                QuestionOrder = g.Key.QuestionOrder,
                Year = g.Key.Year,
                EvaluationCount = g.Select(x => x.EvaluationId).Distinct().Count(),
                AverageScore = g.Sum(x => x.MaxPoints) > 0
                    ? Math.Round(g.Sum(x => x.EarnedPoints) / g.Sum(x => x.MaxPoints) * 100, 2)
                    : 0,
                ErrorCount = g.Count(x => x.IsError)
            })
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.GroupName)
            .ThenBy(x => x.QuestionOrder)
            .ToList();

        // Excel oluştur
        using var workbook = new XLWorkbook();

        // ===== SHEET 1: Soru Grubu Ortalama =====
        var worksheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.QuestionGroupAverage", defaultValue: "Soru Grubu Ortalama"));

        // Headers
        var headers = new[] {
            await _localizationService.GetResourceAsync("Common.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.QuestionGroup", defaultValue: "Kontrol Grubu"),
            await _localizationService.GetResourceAsync("Report.Period", defaultValue: "Periyot"),
            await _localizationService.GetResourceAsync("Report.ListeningCount", defaultValue: "Dinleme Sayısı"),
            await _localizationService.GetResourceAsync("Report.AverageScore", defaultValue: "Ortalama Puan"),
            await _localizationService.GetResourceAsync("Report.ErrorCount", defaultValue: "Hata Sayısı")
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
            worksheet.Cell(row, 6).Value = item.ErrorCount;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet);

        // ===== SHEET 2: Soru Bazlı Ortalama =====
        var worksheet2 = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.QuestionAverage", defaultValue: "Soru Bazlı Ortalama"));

        // Headers for Sheet 2
        var headers2 = new[] {
            await _localizationService.GetResourceAsync("Common.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.QuestionGroup", defaultValue: "Soru Grubu"),
            await _localizationService.GetResourceAsync("Report.Question", defaultValue: "Soru"),
            await _localizationService.GetResourceAsync("Report.Period", defaultValue: "Periyot"),
            await _localizationService.GetResourceAsync("Report.ListeningCount", defaultValue: "Dinleme Sayısı"),
            await _localizationService.GetResourceAsync("Report.AverageScore", defaultValue: "Ortalama Puan"),
            await _localizationService.GetResourceAsync("Report.ErrorCount", defaultValue: "Hata Sayısı")
        };

        for (int i = 0; i < headers2.Length; i++)
        {
            worksheet2.Cell(1, i + 1).Value = headers2[i];
            worksheet2.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet2.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data for Sheet 2
        row = 2;
        foreach (var item in questionData)
        {
            worksheet2.Cell(row, 1).Value = item.ProjectName;
            worksheet2.Cell(row, 2).Value = item.GroupName;
            worksheet2.Cell(row, 3).Value = item.QuestionText;
            worksheet2.Cell(row, 4).Value = item.Year;
            worksheet2.Cell(row, 5).Value = item.EvaluationCount;
            worksheet2.Cell(row, 6).Value = item.AverageScore;
            worksheet2.Cell(row, 6).Style.NumberFormat.Format = "0.00";
            worksheet2.Cell(row, 7).Value = item.ErrorCount;
            row++;
        }

        // Auto-fit columns for Sheet 2
        worksheet2.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet2);

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Soru_Grubu_Ortalama_Raporu_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== MÜŞTERİ DEĞERLENDİRME RAPORU =====


    public async Task<ExcelExportDto> ExportCustomerEvaluationReportAsync(ReportFilterDto filter)
    {
        // PRENSIP: Taslaklar rapora dahil edilmez
        var query = _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.AssignmentPeriod)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.Customer)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.OrganizationAssignments)
                    .ThenInclude(oa => oa.CustomerOrganization)
            .Include(e => e.EvaluatedPersonnel)
            .Include(e => e.CustomerDealer)
                .ThenInclude(d => d!.Customer)
            .Include(e => e.Answers)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.ProjectId));

        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Project.ProjectTypeId));
        }
        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(e => filter.ChecklistIds.Contains(e.Project.ChecklistId));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            query = ApplyDateRangeOrFilter(query, filter.DateRanges);
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
                query = query.Where(e => e.EvaluatorCustomerPersonnelId != null);
            else if (hasOurs && !hasInternal)
                query = query.Where(e => e.EvaluatorCustomerPersonnelId == null);
        }

        // Customer filter (çoklu)
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                filter.CustomerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerIds?.Any() == true)
            query = query.Where(e => e.Project.CustomerId.HasValue && filter.ProjectCustomerIds.Contains(e.Project.CustomerId.Value));

        // Organization filter (çoklu)
        if (filter.OrganizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa =>
                    filter.OrganizationIds.Contains(oa.CustomerOrganizationId)));

        // Period filter (çoklu)
        if (filter.PeriodIds?.Any() == true)
            query = query.Where(e => e.AssignmentPeriodId.HasValue && filter.PeriodIds.Contains(e.AssignmentPeriodId.Value));

        // Evaluated Personnel name search (çoklu - OR mantığı) - dealer adı da aransın
        if (filter.PersonnelNames?.Any() == true)
        {
            query = query.Where(e =>
                filter.PersonnelNames.Any(name =>
                    (e.EvaluatedCustomerPersonnel != null &&
                        (EF.Functions.ILike(e.EvaluatedCustomerPersonnel.FirstName, $"%{name}%") ||
                         EF.Functions.ILike(e.EvaluatedCustomerPersonnel.LastName, $"%{name}%"))) ||
                    (e.EvaluatedUnknownPersonnel != null && EF.Functions.ILike(e.EvaluatedUnknownPersonnel, $"%{name}%")) ||
                    (e.CustomerDealer != null && EF.Functions.ILike(e.CustomerDealer.Name, $"%{name}%"))));
        }

        // CallId search (çoklu - OR mantığı)
        if (filter.CallIds?.Any() == true)
            query = query.Where(e => e.CallId != null &&
                filter.CallIds.Any(callId => EF.Functions.ILike(e.CallId, $"%{callId}%")));

        // Personnel ID filter (supervisor erişim kontrolü için)
        if (filter.PersonnelIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && filter.PersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));

        var evaluations = await query
            .OrderByDescending(e => e.CallDate ?? e.ControlDate ?? e.CreatedAt)
            .Take(10000)
            .ToListAsync();

        // Excel oluştur
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.CustomerEvaluation", defaultValue: "Müşteri Değerlendirme"));

        // Headers
        var headers = new[]
        {
            await _localizationService.GetResourceAsync("Report.Company", defaultValue: "Firma"),
            await _localizationService.GetResourceAsync("Common.Project", defaultValue: "Proje"),
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
            // Firma - personel yoksa dealer'ın müşterisinden al
            var customerName = e.EvaluatedCustomerPersonnel?.Customer?.CompanyName
                ?? e.CustomerDealer?.Customer?.CompanyName
                ?? "";

            // Değerlendirilen: Period varsa period adı, yoksa AyYıl + Company
            var evalDate = e.CallDate ?? e.ControlDate ?? e.CreatedAt;
            var degerlendirilenmStr = e.AssignmentPeriod != null
                ? e.AssignmentPeriod.Name
                : $"{FormatMonthYear(evalDate)} - {customerName}";

            // Kişi - personel yoksa dealer adı veya "Şube Denetlemesi"
            string personnelName;
            if (e.EvaluatedCustomerPersonnel != null)
                personnelName = $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}";
            else if (e.EvaluatedPersonnel != null)
                personnelName = $"{e.EvaluatedPersonnel.FirstName} {e.EvaluatedPersonnel.LastName}";
            else if (!string.IsNullOrWhiteSpace(e.EvaluatedUnknownPersonnel))
                personnelName = e.EvaluatedUnknownPersonnel;
            else if (e.CustomerDealer != null)
                personnelName = $"{e.CustomerDealer.Name} (Şube Denetlemesi)";
            else
                personnelName = "";

            // Departman - personel yoksa dealer şehir/ilçe bilgisi
            string departmentName;
            if (e.EvaluatedCustomerPersonnel?.OrganizationAssignments != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa => oa.CustomerOrganization != null))
            {
                departmentName = string.Join(", ", e.EvaluatedCustomerPersonnel.OrganizationAssignments
                    .Where(oa => oa.CustomerOrganization != null)
                    .Select(oa => oa.CustomerOrganization!.Name));
            }
            else if (e.CustomerDealer != null)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(e.CustomerDealer.City)) parts.Add(e.CustomerDealer.City);
                if (!string.IsNullOrWhiteSpace(e.CustomerDealer.District)) parts.Add(e.CustomerDealer.District);
                departmentName = parts.Count > 0 ? string.Join(" / ", parts) : e.CustomerDealer.Name;
            }
            else
            {
                departmentName = "";
            }

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
            var projectCode = e.Project?.Code;
            var projectName = e.Project?.Name ?? "";
            worksheet.Cell(row, 2).Value = !string.IsNullOrEmpty(projectCode) ? $"{projectCode} - {projectName}" : projectName;
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
            FileName = $"Musteri_Degerlendirme_Raporu_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== İÇ DİNLEME RAPORU (CustomerPortal - Dinleyen kolonu dahil) =====


    public async Task<ExcelExportDto> ExportInternalEvaluationReportAsync(ReportFilterDto filter)
    {
        // PRENSIP: Taslaklar rapora dahil edilmez
        var query = _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.AssignmentPeriod)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.Customer)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.OrganizationAssignments)
                    .ThenInclude(oa => oa.CustomerOrganization)
            .Include(e => e.EvaluatedPersonnel)
            .Include(e => e.EvaluatorCustomerPersonnel)
            .Include(e => e.Answers)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.ProjectId));

        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Project.ProjectTypeId));
        }
        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(e => filter.ChecklistIds.Contains(e.Project.ChecklistId));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            query = ApplyDateRangeOrFilter(query, filter.DateRanges);
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
                query = query.Where(e => e.EvaluatorCustomerPersonnelId != null);
            else if (hasOurs && !hasInternal)
                query = query.Where(e => e.EvaluatorCustomerPersonnelId == null);
        }

        // Customer filter (çoklu)
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                filter.CustomerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerIds?.Any() == true)
            query = query.Where(e => e.Project.CustomerId.HasValue && filter.ProjectCustomerIds.Contains(e.Project.CustomerId.Value));

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

        // Personnel ID filter (supervisor erişim kontrolü için)
        if (filter.PersonnelIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && filter.PersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));

        var evaluations = await query
            .OrderByDescending(e => e.CallDate ?? e.ControlDate ?? e.CreatedAt)
            .Take(10000)
            .ToListAsync();

        // Excel oluştur
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Sheet.CustomerEvaluation", defaultValue: "Müşteri Değerlendirme"));

        // Headers - Dinleyen kolonu dahil
        var headers = new[]
        {
            await _localizationService.GetResourceAsync("Report.Company", defaultValue: "Firma"),
            await _localizationService.GetResourceAsync("Common.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.Evaluated", defaultValue: "Değerlendirilen"),
            await _localizationService.GetResourceAsync("Report.Person", defaultValue: "Kişi"),
            await _localizationService.GetResourceAsync("Report.Listener", defaultValue: "Dinleyen"),
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
            var evalDate = e.CallDate ?? e.ControlDate ?? e.CreatedAt;
            var degerlendirilenmStr = e.AssignmentPeriod != null
                ? e.AssignmentPeriod.Name
                : $"{FormatMonthYear(evalDate)} - {customerName}";

            // Kişi
            var personnelName = e.EvaluatedCustomerPersonnel != null
                ? $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}"
                : (e.EvaluatedPersonnel != null
                    ? $"{e.EvaluatedPersonnel.FirstName} {e.EvaluatedPersonnel.LastName}"
                    : e.EvaluatedUnknownPersonnel ?? "");

            // Dinleyen (iç dinleme - müşterinin kendi personeli)
            var evaluatorName = e.EvaluatorCustomerPersonnel != null
                ? $"{e.EvaluatorCustomerPersonnel.FirstName} {e.EvaluatorCustomerPersonnel.LastName}"
                : "";

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
            var projectCode = e.Project?.Code;
            var projectName = e.Project?.Name ?? "";
            worksheet.Cell(row, 2).Value = !string.IsNullOrEmpty(projectCode) ? $"{projectCode} - {projectName}" : projectName;
            worksheet.Cell(row, 3).Value = degerlendirilenmStr;
            worksheet.Cell(row, 4).Value = personnelName;
            worksheet.Cell(row, 5).Value = evaluatorName;
            worksheet.Cell(row, 6).Value = departmentName;
            worksheet.Cell(row, 7).Value = e.CallId ?? "";
            worksheet.Cell(row, 8).Value = (e.ControlDate ?? e.CallDate)?.ToString("dd.MM.yyyy") ?? "";
            worksheet.Cell(row, 9).Value = e.CallTime ?? (e.CallDate?.ToString("HH:mm") ?? "");
            worksheet.Cell(row, 10).Value = e.Duration ?? "";
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

            worksheet.Cell(row, 11).Value = combinedComment;
            worksheet.Cell(row, 12).Value = evalDate.Year;
            worksheet.Cell(row, 13).Value = periodMonth;
            worksheet.Cell(row, 14).Value = e.ScorePercentage ?? 0;
            worksheet.Cell(row, 14).Style.NumberFormat.Format = "0.00";
            worksheet.Cell(row, 15).Value = combinedDescription;

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet, callIdColumns: new[] { 7 }, noteColumns: new[] { 11, 15 });

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Ic_Dinleme_Raporu_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== PROJE PERFORMANS RAPORU =====


    public async Task<ExcelExportDto> ExportProjectPerformanceReportAsync(ReportFilterDto filter)
    {
        var query = _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.AssignmentPeriod)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed && e.ScorePercentage.HasValue)
            .AsQueryable();

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.ProjectId));

        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Project.ProjectTypeId));
        }
        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(e => filter.ChecklistIds.Contains(e.Project.ChecklistId));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            query = ApplyDateRangeOrFilter(query, filter.DateRanges);
        }

        // Customer filter (çoklu)
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                filter.CustomerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerIds?.Any() == true)
            query = query.Where(e => e.Project.CustomerId.HasValue && filter.ProjectCustomerIds.Contains(e.Project.CustomerId.Value));

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
                query = query.Where(e => e.EvaluatorCustomerPersonnelId != null);
            else if (hasOurs && !hasInternal)
                query = query.Where(e => e.EvaluatorCustomerPersonnelId == null);
        }

        var evaluations = await query.Take(50000).ToListAsync();

        // Group by Period (Month) + Project and calculate averages
        var projectData = evaluations
            .Select(e => new
            {
                EvalDate = e.CallDate ?? e.ControlDate ?? e.CreatedAt,
                ProjectName = e.AssignmentPeriod?.Name ?? (e.Project != null ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name) ?? "" : ""),
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
            await _localizationService.GetResourceAsync("Common.Project", defaultValue: "Proje"),
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
            FileName = $"Proje_Performans_Raporu_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== MT RAPORU (4 Sheet) =====


    public async Task<ExcelExportDto?> ExportSurveyResponsesToExcelAsync(int? projectId = null)
    {
        // Enneagram checklist'lerini hariç tut - sadece Survey tipi
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        // Yanıtları al (max 500)
        var query = _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => e.Project.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey &&
                   e.StatusId == EvaluationStatuses.Ids.Completed &&
                   !e.Project.IsDeleted &&
                   !enneagramChecklistIds.Contains(e.Project.ChecklistId))
            .AsQueryable();

        // Filter by project
        if (projectId.HasValue)
        {
            query = query.Where(e => e.ProjectId == projectId.Value);
        }

        var evaluations = await query
            .OrderByDescending(e => e.CallDate ?? e.ControlDate ?? e.CreatedAt)
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
            ? evaluations.FirstOrDefault()?.Project?.Name ?? "Anket"
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

            sheet1.Cell(row1, 1).Value = e.Project?.Name ?? "";
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

                sheet2.Cell(row2, 1).Value = e.Project?.Name ?? "";
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
            FileName = $"Anket_Yanitlari_{safeProjectName}_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
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
        sheet.Cell(4, 2).Value = detail.AverageScore.HasValue ? $"{detail.AverageScore:F2}%" : "-";

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
            sheet.Cell(row, 3).Value = group.AverageScore.HasValue ? $"{group.AverageScore:F2}%" : "-";
            row++;
        }

        sheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(sheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Grup_Puanlari_{detail.ProjectName}_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
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
            ? $"%{scoreDetail.OverallAverageScore:F2}"
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
                ? $"%{avgScore:F2}"
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
                ? $"%{scoreDetail.OverallAverageScore:F2}"
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
                        sheet2.Cell(row, 7).Value = $"%{dist.Percentage:F2}";
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
                        ? $"%{question.AverageScorePercentage:F2}"
                        : "-";
                }
                else
                {
                    // Alt kriter yoksa tek satır
                    sheet2.Cell(row, 1).Value = question.GroupName ?? "-";
                    sheet2.Cell(row, 2).Value = question.QuestionText;
                    sheet2.Cell(row, 3).Value = question.ResponseCount;
                    sheet2.Cell(row, 4).Value = question.AverageScorePercentage.HasValue
                        ? $"%{question.AverageScorePercentage:F2}"
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
            FileName = $"Soru_Istatistikleri_{safeProjectName}_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
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
            .Where(e => e.ProjectId == projectId && e.StatusId == EvaluationStatuses.Ids.Completed)
            .OrderByDescending(e => e.CallDate ?? e.ControlDate ?? e.CreatedAt)
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
            sheet.Cell(row, col++).Value = eval.ScorePercentage.HasValue ? $"{eval.ScorePercentage:F2}%" : "-";

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
            FileName = $"{filePrefix}_{project.Name}_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== PERFORMANS TAKİBİ =====


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

        worksheet.Cell(2, 1).Value = $"Toplam Yanıt: {data.TotalResponses} | Genel Ortalama: %{data.OverallAverageScore:F2}";
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
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "0.00";

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
                        answerSheet.Cell(ansRow, 5).Style.NumberFormat.Format = "0.00";
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

        var fileName = $"SoruPuanDagilimi_{project.Name.Replace(" ", "_")}_{TurkeyTime.Now:yyyyMMdd}.xlsx";

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
        worksheet.Cell(3, 2).Value = data.OverallAverageScore.HasValue ? $"%{data.OverallAverageScore:F2}" : "-";

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
                        ? $"%{question.AverageScorePercentage:F2}"
                        : "-";
                    worksheet.Cell(row, 5).Value = answer.AnswerText;
                    worksheet.Cell(row, 6).Value = answer.Points;
                    worksheet.Cell(row, 7).Value = answer.SelectionCount;
                    worksheet.Cell(row, 8).Value = $"%{answer.Percentage:F2}";
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
                    ? $"%{question.AverageScorePercentage:F2}"
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
            FileName = $"PuanDetayi_{safeProjectName}_{TurkeyTime.Now:yyyyMMdd}.xlsx",
            FileContent = stream.ToArray()
        };
    }

    // ===== PERSONEL SORU BAZLI PERFORMANS RAPORU =====

    /// <summary>
    /// Personel Soru Bazlı Performans Raporu - Tablo görünümü için
    /// Personel + GroupName bazında ortalama puan (pivot tablo yapısı)
    /// </summary>

    public async Task<ExcelExportDto> ExportEnneagramResultsToExcelAsync(EnneagramFilterDto filter)
    {
        // Enneagram checklist'leri
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        if (!enneagramChecklistIds.Any())
            return new ExcelExportDto
            {
                FileName = $"Enneagram_Sonuclari_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
                FileContent = Array.Empty<byte>(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };

        // Evaluations sorgusu (Cevap Detayları sheet'i için Answers dahil)
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
                        enneagramChecklistIds.Contains(e.Project.ChecklistId));

        // Proje filtresi
        if (filter.ProjectIds?.Any() == true)
        {
            query = query.Where(e => filter.ProjectIds.Contains(e.ProjectId));
        }

        // Arama filtresi
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(e =>
                (e.EvaluatedCustomerPersonnel != null &&
                    ((e.EvaluatedCustomerPersonnel.FirstName != null && e.EvaluatedCustomerPersonnel.FirstName.ToLower().Contains(term)) ||
                     (e.EvaluatedCustomerPersonnel.LastName != null && e.EvaluatedCustomerPersonnel.LastName.ToLower().Contains(term)) ||
                     (e.EvaluatedCustomerPersonnel.Email != null && e.EvaluatedCustomerPersonnel.Email.ToLower().Contains(term)))));
        }

        // Tarih filtresi (çoklu aralık OR)
        if (filter.DateRanges?.Any() == true)
        {
            var param = Expression.Parameter(typeof(Evaluation), "e");
            var createdAtProp = Expression.Property(param, nameof(Evaluation.CreatedAt));
            Expression? orBody = null;
            foreach (var dr in filter.DateRanges)
            {
                Expression? rangeExpr = null;
                if (dr.StartDate.HasValue)
                {
                    var startUtc = DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc);
                    rangeExpr = Expression.GreaterThanOrEqual(createdAtProp, Expression.Constant(startUtc, typeof(DateTime)));
                }
                if (dr.EndDate.HasValue)
                {
                    var endUtc = DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    var leExpr = Expression.LessThanOrEqual(createdAtProp, Expression.Constant(endUtc, typeof(DateTime)));
                    rangeExpr = rangeExpr != null ? Expression.AndAlso(rangeExpr, leExpr) : leExpr;
                }
                if (rangeExpr != null)
                    orBody = orBody != null ? Expression.OrElse(orBody, rangeExpr) : rangeExpr;
            }
            if (orBody != null)
                query = query.Where(Expression.Lambda<Func<Evaluation, bool>>(orBody, param));
        }

        var evaluations = await query
            .OrderByDescending(e => e.CreatedAt)
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

        // Helper: get respondent info
        (string? Name, string? Email) GetRespondentInfo(Evaluation eval)
        {
            if (eval.EvaluatedCustomerPersonnel != null)
            {
                var name = $"{eval.EvaluatedCustomerPersonnel.FirstName} {eval.EvaluatedCustomerPersonnel.LastName}".Trim();
                return (string.IsNullOrWhiteSpace(name) ? null : name, eval.EvaluatedCustomerPersonnel.Email);
            }
            if (externalInvitations.TryGetValue(eval.Id, out var ext))
            {
                var name = $"{ext.FirstName} {ext.LastName}".Trim();
                return (string.IsNullOrWhiteSpace(name) ? null : name, ext.Email);
            }
            return (null, null);
        }

        string GetProjectDisplayName(Project? project)
        {
            if (project == null) return "";
            return !string.IsNullOrEmpty(project.Code) ? $"{project.Code} - {project.Name}" : project.Name;
        }

        using var workbook = new XLWorkbook();

        // ===== Sheet 1: Enneagram Sonuçları =====
        var sheet1 = workbook.Worksheets.Add("Enneagram Sonuçları");
        var headers1 = new[] { "Katılımcı", "E-posta", "Proje", "Baskın Tip", "Baskın Yüzde", "Toplam Puan", "Tamamlanma Tarihi" };
        for (int i = 0; i < headers1.Length; i++)
        {
            sheet1.Cell(1, i + 1).Value = headers1[i];
            sheet1.Cell(1, i + 1).Style.Font.Bold = true;
            sheet1.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row1 = 2;
        foreach (var eval in evaluations)
        {
            var (respondentName, respondentEmail) = GetRespondentInfo(eval);
            var scores = CalculateEnneagramScores(eval);
            var dominantScore = scores.OrderByDescending(s => s.Percentage).FirstOrDefault();

            sheet1.Cell(row1, 1).Value = respondentName ?? "Anonim";
            sheet1.Cell(row1, 2).Value = respondentEmail ?? "";
            sheet1.Cell(row1, 3).Value = GetProjectDisplayName(eval.Project);
            sheet1.Cell(row1, 4).Value = dominantScore?.PersonalityType ?? "-";
            sheet1.Cell(row1, 5).Value = dominantScore?.Percentage != null ? $"%{dominantScore.Percentage:F2}" : "-";
            sheet1.Cell(row1, 6).Value = scores.Sum(s => s.TotalPoints);
            sheet1.Cell(row1, 7).Value = eval.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
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
        foreach (var eval in evaluations)
        {
            var (respondentName, respondentEmail) = GetRespondentInfo(eval);
            var projectName = GetProjectDisplayName(eval.Project);

            var answers = eval.Answers
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

                sheet2.Cell(row2, 1).Value = projectName;
                sheet2.Cell(row2, 2).Value = respondentName ?? "-";
                sheet2.Cell(row2, 3).Value = respondentEmail ?? "-";
                // Enneagram puanlama: SubCriteriaSelections üzerinden (CalculateEnneagramScores ile aynı)
                var earnedPoints = a.SubCriteriaSelections
                    .Select(sc => sc.SubCriteria?.WeightPoints ?? 0)
                    .DefaultIfEmpty(0)
                    .Max();

                sheet2.Cell(row2, 4).Value = a.Question!.GroupName ?? "Genel";
                sheet2.Cell(row2, 5).Value = a.Question.Text;
                sheet2.Cell(row2, 6).Value = (int)earnedPoints;
                sheet2.Cell(row2, 7).Value = a.Question.MaxPoints;
                sheet2.Cell(row2, 8).Value = selectedCriteria.Any() ? string.Join(", ", selectedCriteria) : "-";
                sheet2.Cell(row2, 9).Value = a.Notes ?? "-";
                row2++;
            }
        }

        sheet2.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(sheet2, subCriteriaColumns: new[] { 8 }, noteColumns: new[] { 9 });

        // ===== Sheet 3: Kişilik Tipi Dağılımı (proje bazlı) =====
        var sheet3 = workbook.Worksheets.Add("Kişilik Tipi Dağılımı");
        var headers3 = new[] { "Proje", "Yanıt Sayısı", "Kişilik Tipi", "Ortalama Puan", "Maks Puan", "Ortalama Yüzde (%)" };
        for (int i = 0; i < headers3.Length; i++)
        {
            sheet3.Cell(1, i + 1).Value = headers3[i];
            sheet3.Cell(1, i + 1).Style.Font.Bold = true;
            sheet3.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Proje bazlı dağılım hesapla
        var projectGroups = evaluations.GroupBy(e => e.ProjectId);
        int row3 = 2;

        foreach (var projectGroup in projectGroups)
        {
            var projectEvals = projectGroup.ToList();
            var projectDisplayName = GetProjectDisplayName(projectEvals.First().Project);
            var responseCount = projectEvals.Count;

            // Tüm kişilik tiplerini ve puanlarını topla
            var personalityScores = new Dictionary<string, List<decimal>>();
            var personalityPoints = new Dictionary<string, List<int>>();

            foreach (var eval in projectEvals)
            {
                var scores = CalculateEnneagramScores(eval);
                foreach (var score in scores)
                {
                    if (!personalityScores.ContainsKey(score.PersonalityType))
                    {
                        personalityScores[score.PersonalityType] = new List<decimal>();
                        personalityPoints[score.PersonalityType] = new List<int>();
                    }
                    personalityScores[score.PersonalityType].Add(score.Percentage);
                    personalityPoints[score.PersonalityType].Add(score.TotalPoints);
                }
            }

            // Yüzdeye göre sırala
            var distribution = personalityScores
                .Select(kvp => new
                {
                    PersonalityType = kvp.Key,
                    AveragePoints = personalityPoints[kvp.Key].Any() ? (int)Math.Round(personalityPoints[kvp.Key].Average()) : 0,
                    MaxPoints = 50,
                    AveragePercentage = kvp.Value.Any() ? kvp.Value.Average() : 0
                })
                .OrderByDescending(d => d.AveragePercentage)
                .ToList();

            foreach (var d in distribution)
            {
                sheet3.Cell(row3, 1).Value = projectDisplayName;
                sheet3.Cell(row3, 2).Value = responseCount;
                sheet3.Cell(row3, 3).Value = d.PersonalityType;
                sheet3.Cell(row3, 4).Value = d.AveragePoints;
                sheet3.Cell(row3, 5).Value = d.MaxPoints;
                sheet3.Cell(row3, 6).Value = Math.Round(d.AveragePercentage, 2);
                sheet3.Cell(row3, 6).Style.NumberFormat.Format = "0.00";
                row3++;
            }
        }

        sheet3.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(sheet3);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Enneagram_Sonuclari_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
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

    public async Task<IEnumerable<DealerListItemDto>> GetDealerListAsync(int? customerId = null)
    {
        var query = _context.CustomerDealers
            .Include(d => d.Customer)
            .Where(d => d.IsActive);

        if (customerId.HasValue)
        {
            query = query.Where(d => d.CustomerId == customerId.Value);
        }

        return await query
            .Select(d => new DealerListItemDto
            {
                Id = d.Id,
                Name = d.Name,
                Code = d.Code,
                City = d.City,
                CustomerId = d.CustomerId,
                CustomerName = d.Customer != null ? d.Customer.CompanyName : ""
            })
            .OrderBy(d => d.Name)
            .ToListAsync();
    }


    public async Task<DealerReportCardDto?> GetDealerReportCardAsync(DealerReportCardFilterDto filter)
    {
        var dealer = await _context.CustomerDealers
            .FirstOrDefaultAsync(d => d.Id == filter.DealerId);

        if (dealer == null)
            return null;

        var query = _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Include(e => e.Evaluator)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => e.CustomerDealerId == filter.DealerId && e.StatusId == EvaluationStatuses.Ids.Completed);

        // Proje filtresi
        if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Project.ProjectTypeId == ProjectTypes.Ids.MysteryShopping
                                  || e.Project.ProjectTypeId == ProjectTypes.Ids.PhysicalAudit);
        }
        else
        {
            query = query.Where(e => filter.ProjectIds.Contains(e.ProjectId));
        }

        // DateRanges pattern
        if (filter.DateRanges?.Any() == true)
        {
            query = ApplyDateRangeOrFilter(query, filter.DateRanges);
        }

        var evaluations = await query.ToListAsync();

        if (!evaluations.Any())
        {
            var defaultProjectTypeId = ProjectTypes.Ids.MysteryShopping;
            if (filter.ProjectIds?.Any() == true)
            {
                var projectType = await _context.Projects
                    .Where(p => filter.ProjectIds.Contains(p.Id))
                    .Select(p => p.ProjectTypeId)
                    .FirstOrDefaultAsync();
                if (projectType > 0) defaultProjectTypeId = projectType;
            }

            var customerThreshold = await _customerScoreThresholdService.GetByCustomerAndProjectTypeAsync(dealer.CustomerId, defaultProjectTypeId);
            decimal emptySuccessThreshold = 80;
            decimal emptyWarningThreshold = 60;
            if (customerThreshold != null)
            {
                emptySuccessThreshold = customerThreshold.SuccessThreshold;
                emptyWarningThreshold = customerThreshold.WarningThreshold;
            }
            else
            {
                var defaultSettings = await _performanceSettingsService.GetByProjectTypeIdAsync(defaultProjectTypeId);
                emptySuccessThreshold = defaultSettings?.SuccessThreshold ?? 80;
                emptyWarningThreshold = defaultSettings?.WarningThreshold ?? 60;
            }

            return new DealerReportCardDto
            {
                DealerId = dealer.Id,
                DealerName = dealer.Name,
                DealerCode = dealer.Code,
                City = dealer.City,
                District = dealer.District,
                SuccessThreshold = emptySuccessThreshold,
                WarningThreshold = emptyWarningThreshold
            };
        }

        var completedScores = evaluations.Where(e => e.ScorePercentage.HasValue).Select(e => e.ScorePercentage!.Value).ToList();

        // Aylık trend
        var monthlyTrend = evaluations
            .GroupBy(e => new { (e.CallDate ?? e.ControlDate ?? e.CreatedAt).Year, (e.CallDate ?? e.ControlDate ?? e.CreatedAt).Month })
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

        // Grup bazlı performans
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
                    : 0,
                ErrorCount = g.Count(a => a.Question!.ScoringTypeId == ScoringTypes.Ids.Scored
                    ? (a.EarnedPoints ?? 0) < a.Question.WeightPoints
                    : a.IsPenaltyApplied)
            })
            .OrderByDescending(s => s.PercentageScore)
            .ToList();

        // Değerlendirmeler
        var recentEvaluations = evaluations
            .OrderByDescending(e => e.CallDate ?? e.ControlDate ?? e.CreatedAt)
            .Select(e => new DealerEvaluationSummaryDto
            {
                EvaluationId = e.Id,
                EvaluationDate = e.CallDate ?? e.ControlDate ?? e.CreatedAt,
                ProjectName = e.Project != null ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name) ?? "" : "",
                ChecklistName = e.Project.Checklist?.Name ?? "",
                EvaluatorName = e.Evaluator != null ? $"{e.Evaluator.FirstName} {e.Evaluator.LastName}" : null,
                PersonnelName = e.EvaluatedCustomerPersonnel != null
                    ? $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}"
                    : null,
                ScorePercentage = e.ScorePercentage ?? 0,
                YellowCards = e.YellowCardCount,
                RedCards = e.RedCardCount,
                Status = EvaluationStatuses.GetById(e.StatusId)?.SystemName ?? "",
                ControlDate = e.ControlDate?.ToString("dd.MM.yyyy"),
                ControlTime = e.ControlTime,
                Notes = e.Notes
            })
            .ToList();

        // Güçlü ve zayıf yönler
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
            .Where(q => q.EvaluationCount >= 2)
            .ToList();

        var strengths = questionPerformance.OrderByDescending(q => q.PercentageScore).Take(5).ToList();

        var scoredWeaknesses = questionPerformance.OrderBy(q => q.PercentageScore).Take(5).ToList();

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
                PercentageScore = 0,
                EvaluationCount = g.Count()
            })
            .Where(q => q.EvaluationCount >= 1)
            .ToList();

        var weaknesses = penaltyWeaknesses
            .Concat(scoredWeaknesses)
            .Take(5)
            .ToList();

        // Threshold
        var projectTypeId = evaluations
            .Select(e => e.Project?.ProjectTypeId)
            .FirstOrDefault(pt => pt.HasValue) ?? ProjectTypes.Ids.MysteryShopping;

        decimal successThreshold = 80;
        decimal warningThreshold = 60;
        var customerThresholdResult = await _customerScoreThresholdService.GetByCustomerAndProjectTypeAsync(dealer.CustomerId, projectTypeId);
        if (customerThresholdResult != null)
        {
            successThreshold = customerThresholdResult.SuccessThreshold;
            warningThreshold = customerThresholdResult.WarningThreshold;
        }
        else
        {
            var performanceSettings = await _performanceSettingsService.GetByProjectTypeIdAsync(projectTypeId);
            successThreshold = performanceSettings?.SuccessThreshold ?? 80;
            warningThreshold = performanceSettings?.WarningThreshold ?? 60;
        }

        // Süreç analizi
        var processAnalysis = evaluations
            .SelectMany(e => e.Answers
                .Where(a => a.Question != null && !string.IsNullOrEmpty(a.Question.Text))
                .Select(a => new
                {
                    ProjectName = e.Project != null ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name) ?? "" : "",
                    QuestionText = a.Question!.Text,
                    EvalDate = e.CallDate ?? e.ControlDate ?? e.CreatedAt,
                    EarnedPoints = a.EarnedPoints ?? 0,
                    WeightPoints = a.Question.WeightPoints,
                    IsError = a.Question.ScoringTypeId == ScoringTypes.Ids.Scored
                        ? (a.EarnedPoints ?? 0) < a.Question.WeightPoints
                        : a.IsPenaltyApplied
                }))
            .GroupBy(x => new { x.ProjectName, x.QuestionText, Year = x.EvalDate.Year, Month = x.EvalDate.Month })
            .Select(g => new PersonnelProcessAnalysisDto
            {
                ProjectName = g.Key.ProjectName,
                Department = dealer.City ?? "",
                QuestionText = g.Key.QuestionText,
                Year = g.Key.Year,
                PeriodMonth = $"{g.Key.Year}{g.Key.Month:D2}",
                AverageScore = g.Sum(x => x.WeightPoints) > 0
                    ? Math.Round(g.Sum(x => x.EarnedPoints) / g.Sum(x => x.WeightPoints) * 100, 2)
                    : 0,
                ErrorCount = g.Count(x => x.IsError)
            })
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.QuestionText)
            .ThenBy(x => x.PeriodMonth)
            .ToList();

        return new DealerReportCardDto
        {
            DealerId = dealer.Id,
            DealerName = dealer.Name,
            DealerCode = dealer.Code,
            City = dealer.City,
            District = dealer.District,
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
            ProcessAnalysis = processAnalysis,
            SuccessThreshold = successThreshold,
            WarningThreshold = warningThreshold
        };
    }


    public async Task<ExcelExportDto> ExportDealerReportCardToExcelAsync(DealerReportCardFilterDto filter)
    {
        var report = await GetDealerReportCardAsync(filter);
        if (report == null)
        {
            return new ExcelExportDto
            {
                FileName = "SubeKarnesi_Bulunamadi.xlsx",
                FileContent = Array.Empty<byte>(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        using var workbook = new XLWorkbook();

        // Genel Bilgiler
        var infoSheet = workbook.Worksheets.Add("Genel Bilgiler");
        infoSheet.Cell(1, 1).Value = "ŞUBE KARNESİ";
        infoSheet.Cell(1, 1).Style.Font.Bold = true;
        infoSheet.Cell(1, 1).Style.Font.FontSize = 16;

        infoSheet.Cell(3, 1).Value = "Şube Adı:";
        infoSheet.Cell(3, 2).Value = report.DealerName;
        infoSheet.Cell(4, 1).Value = "Şube Kodu:";
        infoSheet.Cell(4, 2).Value = report.DealerCode ?? "-";
        infoSheet.Cell(5, 1).Value = "İl/İlçe:";
        infoSheet.Cell(5, 2).Value = $"{report.City ?? "-"} / {report.District ?? "-"}";

        infoSheet.Cell(7, 1).Value = "PERFORMANS ÖZETİ";
        infoSheet.Cell(7, 1).Style.Font.Bold = true;

        infoSheet.Cell(8, 1).Value = "Toplam Değerlendirme:";
        infoSheet.Cell(8, 2).Value = report.TotalEvaluations;
        infoSheet.Cell(9, 1).Value = "Ortalama Puan:";
        infoSheet.Cell(9, 2).Value = $"{report.AverageScore:F2}%";
        infoSheet.Cell(10, 1).Value = "En Yüksek Puan:";
        infoSheet.Cell(10, 2).Value = $"{report.BestScore:F2}%";
        infoSheet.Cell(11, 1).Value = "En Düşük Puan:";
        infoSheet.Cell(11, 2).Value = $"{report.WorstScore:F2}%";
        infoSheet.Cell(12, 1).Value = "Toplam Sarı Kart:";
        infoSheet.Cell(12, 2).Value = report.TotalYellowCards;
        infoSheet.Cell(13, 1).Value = "Toplam Kırmızı Kart:";
        infoSheet.Cell(13, 2).Value = report.TotalRedCards;

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
            trendSheet.Cell(row, 3).Value = $"{trend.AverageScore:F2}%";
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
        groupSheet.Cell(1, 4).Value = "Hata Sayısı";
        groupSheet.Cell(1, 4).Style.Font.Bold = true;

        row = 2;
        foreach (var group in report.GroupPerformances)
        {
            groupSheet.Cell(row, 1).Value = group.GroupName;
            groupSheet.Cell(row, 2).Value = group.EvaluationCount;
            groupSheet.Cell(row, 3).Value = $"{group.PercentageScore:F2}%";
            groupSheet.Cell(row, 4).Value = group.ErrorCount;
            row++;
        }
        groupSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(groupSheet);

        // Değerlendirmeler
        var evalSheet = workbook.Worksheets.Add("Değerlendirmeler");
        evalSheet.Cell(1, 1).Value = "Tarih";
        evalSheet.Cell(1, 1).Style.Font.Bold = true;
        evalSheet.Cell(1, 2).Value = "Denetlenen";
        evalSheet.Cell(1, 2).Style.Font.Bold = true;
        evalSheet.Cell(1, 3).Value = "Kontrol Tarihi";
        evalSheet.Cell(1, 3).Style.Font.Bold = true;
        evalSheet.Cell(1, 4).Value = "Kontrol Saati";
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
            evalSheet.Cell(row, 2).Value = eval.PersonnelName ?? "-";
            evalSheet.Cell(row, 3).Value = eval.ControlDate ?? "-";
            evalSheet.Cell(row, 4).Value = eval.ControlTime ?? "-";
            evalSheet.Cell(row, 5).Value = eval.ProjectName;
            evalSheet.Cell(row, 6).Value = eval.ChecklistName;
            evalSheet.Cell(row, 7).Value = $"{eval.ScorePercentage:F2}%";
            evalSheet.Cell(row, 8).Value = eval.YellowCards;
            evalSheet.Cell(row, 9).Value = eval.RedCards;
            row++;
        }
        evalSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(evalSheet);

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
            analysisSheet.Cell(row, 3).Value = $"{strength.PercentageScore:F2}%";
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
            analysisSheet.Cell(row, 3).Value = $"{weakness.PercentageScore:F2}%";
            row++;
        }
        analysisSheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(analysisSheet);

        // Süreç Analizi
        if (report.ProcessAnalysis.Any())
        {
            var processSheet = workbook.Worksheets.Add("Süreç Analizi");
            processSheet.Cell(1, 1).Value = "Proje";
            processSheet.Cell(1, 2).Value = "Şube";
            processSheet.Cell(1, 3).Value = "İl";
            processSheet.Cell(1, 4).Value = "Kontrol Sorusu";
            processSheet.Cell(1, 5).Value = "Periyot";
            processSheet.Cell(1, 6).Value = "Periyot (Ay)";
            processSheet.Cell(1, 7).Value = "Ortalama Puan";
            processSheet.Cell(1, 8).Value = "Hata Sayısı";

            var headerRange = processSheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            row = 2;
            foreach (var item in report.ProcessAnalysis)
            {
                processSheet.Cell(row, 1).Value = item.ProjectName;
                processSheet.Cell(row, 2).Value = report.DealerName;
                processSheet.Cell(row, 3).Value = item.Department;
                processSheet.Cell(row, 4).Value = item.QuestionText;
                processSheet.Cell(row, 5).Value = item.Year;
                processSheet.Cell(row, 6).Value = item.PeriodMonth;
                processSheet.Cell(row, 7).Value = item.AverageScore;
                processSheet.Cell(row, 8).Value = item.ErrorCount;
                row++;
            }
            processSheet.Columns().AdjustToContents();
            ExcelHelper.ApplyLongTextColumnStyles(processSheet);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"SubeKarnesi_{report.DealerName.Replace(" ", "_")}_{TurkeyTime.Now:yyyyMMdd}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }


    public async Task<ExcelExportDto> ExportDealerReportCardToWordAsync(DealerReportCardFilterDto filter)
    {
        var report = await GetDealerReportCardAsync(filter);
        if (report == null)
        {
            return new ExcelExportDto
            {
                FileName = "SubeKarnesi_Bulunamadi.docx",
                FileContent = Array.Empty<byte>(),
                ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };
        }

        var projectName = report.RecentEvaluations.FirstOrDefault()?.ProjectName ?? "Proje";

        using var doc = new XWPFDocument();

        // Başlık tablosu
        var headerTable = doc.CreateTable(2, 2);
        headerTable.Width = 5000;

        var nameCell = headerTable.GetRow(0).GetCell(0);
        nameCell.SetText(report.DealerName);
        if (nameCell.Paragraphs.Count > 0 && nameCell.Paragraphs[0].Runs.Count > 0)
        {
            nameCell.Paragraphs[0].Runs[0].IsBold = true;
            nameCell.Paragraphs[0].Runs[0].FontSize = 14;
        }

        var dateLabelCell = headerTable.GetRow(0).GetCell(1);
        dateLabelCell.SetText("Doküman Tarihi");
        if (dateLabelCell.Paragraphs.Count > 0 && dateLabelCell.Paragraphs[0].Runs.Count > 0)
        {
            dateLabelCell.Paragraphs[0].Runs[0].IsBold = true;
        }

        headerTable.GetRow(1).GetCell(0).SetText($"Kod: {report.DealerCode ?? "-"} | İl: {report.City ?? "-"} / {report.District ?? "-"}");
        var dateValueCell = headerTable.GetRow(1).GetCell(1);
        dateValueCell.SetText(TurkeyTime.Now.ToString("dd.MM.yyyy"));

        doc.CreateParagraph();

        // Proje + başarı ortalaması
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

        // Kontrol soruları tablosu
        var questionTable = doc.CreateTable(report.GroupPerformances.Count + 1, 2);
        questionTable.Width = 5000;

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

        for (int i = 0; i < report.GroupPerformances.Count; i++)
        {
            var group = report.GroupPerformances[i];
            var dataRow = questionTable.GetRow(i + 1);
            dataRow.GetCell(0).SetText(group.GroupName);
            dataRow.GetCell(1).SetText($"{group.PercentageScore:F2}");
        }

        doc.CreateParagraph();

        // Genel analiz tablosu
        var analysisHeader = doc.CreateParagraph();
        var analysisHeaderRun = analysisHeader.CreateRun();
        analysisHeaderRun.SetText("GENEL ANALİZ");
        analysisHeaderRun.IsBold = true;
        analysisHeaderRun.FontSize = 12;

        var evalTable = doc.CreateTable(report.RecentEvaluations.Count + 1, 4);

        evalTable.GetCTTbl().AddNewTblPr().AddNewTblW().w = "9000";
        evalTable.GetCTTbl().tblPr.tblW.type = NPOI.OpenXmlFormats.Wordprocessing.ST_TblWidth.dxa;

        int[] colWidths = { 1200, 3000, 3800, 1000 };

        var evalHeaderRow = evalTable.GetRow(0);
        string[] headers = { "Ziyaret Tarihi", "Denetlenen", "Denetim Yorumu", "Toplam Puan" };
        for (int c = 0; c < 4; c++)
        {
            var cell = evalHeaderRow.GetCell(c);
            cell.SetText(headers[c]);
            var tcPr = cell.GetCTTc().AddNewTcPr();
            tcPr.AddNewTcW().w = colWidths[c].ToString();
            tcPr.tcW.type = NPOI.OpenXmlFormats.Wordprocessing.ST_TblWidth.dxa;
            if (cell.Paragraphs.Count > 0 && cell.Paragraphs[0].Runs.Count > 0)
            {
                cell.Paragraphs[0].Runs[0].IsBold = true;
            }
        }

        for (int i = 0; i < report.RecentEvaluations.Count; i++)
        {
            var eval = report.RecentEvaluations[i];
            var dataRow = evalTable.GetRow(i + 1);
            string[] values = {
                eval.EvaluationDate?.ToString("dd.MM.yyyy") ?? "-",
                eval.PersonnelName ?? "-",
                eval.Notes ?? "",
                $"{eval.ScorePercentage:F2}"
            };

            for (int c = 0; c < 4; c++)
            {
                var cell = dataRow.GetCell(c);
                cell.SetText(values[c]);
                var tcPr = cell.GetCTTc().AddNewTcPr();
                tcPr.AddNewTcW().w = colWidths[c].ToString();
                tcPr.tcW.type = NPOI.OpenXmlFormats.Wordprocessing.ST_TblWidth.dxa;
            }
        }

        using var stream = new MemoryStream();
        doc.Write(stream);

        var safeDealerName = string.Join("_", report.DealerName.Split(Path.GetInvalidFileNameChars()));

        return new ExcelExportDto
        {
            FileName = $"Sube_Karne_{safeDealerName}_{TurkeyTime.Now:yyyyMMdd}.docx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };
    }

    // ===== PUANSIZ SORU RAPORU =====


    public async Task<ExcelExportDto> ExportUnscoredQuestionsReportAsync(ReportFilterDto filter)
    {
        var query = _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.AssignmentPeriod)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedPersonnel)
            .Include(e => e.CustomerDealer)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        // Apply filters - Çoklu değer desteği (OR mantığı)
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(e => filter.ProjectIds.Contains(e.ProjectId));

        if (filter.ProjectTypes?.Any() == true)
        {
            var projectTypeIds = filter.ProjectTypes
                .Select(pt => ProjectTypes.GetBySystemName(pt))
                .Where(pt => pt != null)
                .Select(pt => pt!.Id)
                .ToList();
            if (projectTypeIds.Any())
                query = query.Where(e => projectTypeIds.Contains(e.Project.ProjectTypeId));
        }
        else if (filter.ProjectIds?.Any() != true)
        {
            query = query.Where(e => e.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorIds?.Any() == true)
            query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(e => filter.ChecklistIds.Contains(e.Project.ChecklistId));

        // Date Range filter (çoklu - OR mantığı)
        if (filter.DateRanges?.Any() == true)
        {
            query = ApplyDateRangeOrFilter(query, filter.DateRanges);
        }

        // Customer filter (çoklu)
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                filter.CustomerIds.Contains(e.EvaluatedCustomerPersonnel.CustomerId));

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerIds?.Any() == true)
            query = query.Where(e => e.Project.CustomerId.HasValue && filter.ProjectCustomerIds.Contains(e.Project.CustomerId.Value));

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
                query = query.Where(e => e.EvaluatorCustomerPersonnelId != null);
            else if (hasOurs && !hasInternal)
                query = query.Where(e => e.EvaluatorCustomerPersonnelId == null);
        }

        // Personnel filter (çoklu)
        if (filter.PersonnelIds?.Any() == true)
            query = query.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && filter.PersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));

        var evaluations = await query.Take(10000).ToListAsync();

        // ===== SHEET 1: Detaylı (her değerlendirme-cevap satırı) =====
        var detailData = evaluations
            .SelectMany(e => e.Answers
                .Where(a => a.Question != null && a.Question.ScoringTypeId == ScoringTypes.Ids.Unscored)
                .Select(a => new
                {
                    ProjectName = e.Project != null ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name) ?? "" : "",
                    PeriodName = e.AssignmentPeriod?.Name ?? "",
                    Date = e.CallDate ?? e.ControlDate,
                    CallId = e.CallId ?? "",
                    EvaluatedName = e.EvaluatedCustomerPersonnel != null
                        ? e.EvaluatedCustomerPersonnel.FullName
                        : (e.EvaluatedPersonnel != null
                            ? $"{e.EvaluatedPersonnel.FirstName} {e.EvaluatedPersonnel.LastName}"
                            : e.EvaluatedUnknownPersonnel
                                ?? e.CustomerDealer?.Name
                                ?? "-"),
                    GroupName = a.Question!.GroupName ?? "",
                    QuestionText = a.Question.Text,
                    SelectedSubCriteria = a.SubCriteriaSelections.Any()
                        ? string.Join(", ", a.SubCriteriaSelections
                            .OrderBy(s => s.SubCriteria?.Order ?? 0)
                            .Select(s => s.SubCriteria?.Description ?? ""))
                        : "",
                    Notes = a.Notes ?? ""
                }))
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.GroupName)
            .ThenBy(x => x.QuestionText)
            .ToList();

        // ===== SHEET 2: Gruplu (alt kriter dağılımı) =====
        var groupedData = evaluations
            .SelectMany(e => e.Answers
                .Where(a => a.Question != null && a.Question.ScoringTypeId == ScoringTypes.Ids.Unscored)
                .SelectMany(a => a.SubCriteriaSelections
                    .Where(s => s.SubCriteria != null)
                    .Select(s => new
                    {
                        ProjectName = e.Project != null ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name) ?? "" : "",
                        GroupName = a.Question!.GroupName ?? "",
                        QuestionText = a.Question.Text,
                        QuestionId = a.QuestionId,
                        SubCriteriaDescription = s.SubCriteria!.Description,
                        SubCriteriaId = s.SubCriteriaId,
                        EvaluationId = e.Id
                    })))
            .GroupBy(x => new { x.ProjectName, x.GroupName, x.QuestionId, x.QuestionText, x.SubCriteriaId, x.SubCriteriaDescription })
            .Select(g => new
            {
                g.Key.ProjectName,
                g.Key.GroupName,
                g.Key.QuestionId,
                g.Key.QuestionText,
                g.Key.SubCriteriaDescription,
                SelectionCount = g.Count(),
            })
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.GroupName)
            .ThenBy(x => x.QuestionText)
            .ThenByDescending(x => x.SelectionCount)
            .ToList();

        // Soru başına toplam değerlendirme sayısı (oran hesabı için)
        var questionEvalCounts = evaluations
            .SelectMany(e => e.Answers
                .Where(a => a.Question != null && a.Question.ScoringTypeId == ScoringTypes.Ids.Unscored)
                .Select(a => new { ProjectName = e.Project != null ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name) ?? "" : "", a.QuestionId, EvaluationId = e.Id }))
            .GroupBy(x => new { x.ProjectName, x.QuestionId })
            .ToDictionary(g => (g.Key.ProjectName, g.Key.QuestionId), g => g.Select(x => x.EvaluationId).Distinct().Count());

        // Excel oluştur
        using var workbook = new XLWorkbook();

        // ===== SHEET 1: Puansız Soru Detay =====
        var ws1 = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.UnscoredQuestionsDetail", defaultValue: "Puansız Soru Detay"));

        var headers1 = new[] {
            await _localizationService.GetResourceAsync("Common.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.Period", defaultValue: "Dönem"),
            await _localizationService.GetResourceAsync("Common.Date", defaultValue: "Tarih"),
            "CallId",
            await _localizationService.GetResourceAsync("Evaluation.EvaluatedPerson", defaultValue: "Değerlendirilen"),
            await _localizationService.GetResourceAsync("Report.QuestionGroup", defaultValue: "Soru Grubu"),
            await _localizationService.GetResourceAsync("Report.Question", defaultValue: "Soru"),
            await _localizationService.GetResourceAsync("Report.SelectedSubCriteria", defaultValue: "Seçilen Alt Kriterler"),
            await _localizationService.GetResourceAsync("Common.Notes", defaultValue: "Not")
        };

        for (int i = 0; i < headers1.Length; i++)
        {
            ws1.Cell(1, i + 1).Value = headers1[i];
            ws1.Cell(1, i + 1).Style.Font.Bold = true;
            ws1.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var item in detailData)
        {
            ws1.Cell(row, 1).Value = item.ProjectName;
            ws1.Cell(row, 2).Value = item.PeriodName;
            ws1.Cell(row, 3).Value = item.Date?.ToString("dd.MM.yyyy") ?? "";
            ws1.Cell(row, 4).Value = item.CallId;
            ws1.Cell(row, 5).Value = item.EvaluatedName;
            ws1.Cell(row, 6).Value = item.GroupName;
            ws1.Cell(row, 7).Value = item.QuestionText;
            ws1.Cell(row, 8).Value = item.SelectedSubCriteria;
            ws1.Cell(row, 9).Value = item.Notes;
            row++;
        }

        ws1.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(ws1);

        // ===== SHEET 2: Alt Kriter Dağılımı =====
        var ws2 = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.UnscoredQuestionsGrouped", defaultValue: "Alt Kriter Dağılımı"));

        var headers2 = new[] {
            await _localizationService.GetResourceAsync("Common.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.QuestionGroup", defaultValue: "Soru Grubu"),
            await _localizationService.GetResourceAsync("Report.Question", defaultValue: "Soru"),
            await _localizationService.GetResourceAsync("Report.SubCriteria", defaultValue: "Alt Kriter"),
            await _localizationService.GetResourceAsync("Report.SelectionCount", defaultValue: "Seçilme Sayısı"),
            await _localizationService.GetResourceAsync("Report.TotalEvaluations", defaultValue: "Toplam Değerlendirme"),
            await _localizationService.GetResourceAsync("Report.SelectionRate", defaultValue: "Oran (%)")
        };

        for (int i = 0; i < headers2.Length; i++)
        {
            ws2.Cell(1, i + 1).Value = headers2[i];
            ws2.Cell(1, i + 1).Style.Font.Bold = true;
            ws2.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        row = 2;
        foreach (var item in groupedData)
        {
            var totalForQuestion = questionEvalCounts.TryGetValue((item.ProjectName, item.QuestionId), out var cnt)
                ? cnt : 0;

            ws2.Cell(row, 1).Value = item.ProjectName;
            ws2.Cell(row, 2).Value = item.GroupName;
            ws2.Cell(row, 3).Value = item.QuestionText;
            ws2.Cell(row, 4).Value = item.SubCriteriaDescription;
            ws2.Cell(row, 5).Value = item.SelectionCount;
            ws2.Cell(row, 6).Value = totalForQuestion;
            ws2.Cell(row, 7).Value = totalForQuestion > 0
                ? Math.Round((decimal)item.SelectionCount / totalForQuestion * 100, 2)
                : 0;
            ws2.Cell(row, 7).Style.NumberFormat.Format = "0.00";
            row++;
        }

        ws2.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(ws2);

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Puansiz_Soru_Raporu_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ==================== CUSTOMER PORTAL SPECIFIC (Survey + Enneagram) ====================

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
                   p.IsActive && !p.IsDeleted &&
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
                .OrderByDescending(e => e.CallDate ?? e.ControlDate ?? e.CreatedAt)
                .Select(e => (DateTime?)e.CreatedAt)
                .FirstOrDefaultAsync();

            result.Add(new SurveyProjectListItemDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                ProjectCode = project.Code,
                TotalInvitations = invitationCount,
                TotalResponses = completedCount,
                ResponseRate = invitationCount > 0 ? Math.Round((decimal)completedCount / invitationCount * 100, 2) : 0,
                AverageScore = completedCount > 0 ? Math.Round(avgScore, 2) : null,
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
            query = query.Where(e => e.CreatedAt >= startDateUtc);
        }

        if (endDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => e.CreatedAt <= endDateUtc);
        }

        var evaluations = await query
            .OrderByDescending(e => e.CallDate ?? e.ControlDate ?? e.CreatedAt)
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
                ProjectName = !string.IsNullOrEmpty(e.Project.Code) ? $"{e.Project.Code} - {e.Project.Name}" : e.Project.Name,
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
            .OrderByDescending(e => e.CallDate ?? e.ControlDate ?? e.CreatedAt)
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
                    AverageScore = totalMaxScore > 0 ? Math.Round(totalScore / totalMaxScore * 100, 2) : null
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
            ProjectName = !string.IsNullOrEmpty(project.Code) ? $"{project.Code} - {project.Name}" : project.Name,
            OrganizationName = project.Organization?.Name,
            TotalInvitations = invitationCount > 0 ? invitationCount : evaluations.Count,
            TotalResponses = evaluations.Count,
            ResponseRate = invitationCount > 0 ? Math.Round((decimal)evaluations.Count / invitationCount * 100, 2) : 100,
            AverageScore = evaluations.Any(e => e.ScorePercentage.HasValue)
                ? Math.Round((decimal)evaluations.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage!.Value), 2)
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
                    ? (decimal?)Math.Round(g.Where(a => a.EarnedPoints.HasValue).Average(a => a.EarnedPoints!.Value) / g.Key.WeightPoints * 100, 2)
                    : null
            })
            .OrderBy(q => q.GroupName)
            .ThenBy(q => q.Order)
            .ToList();

        var overallAverage = questionStats.Where(q => q.AverageScore.HasValue).Any()
            ? Math.Round(questionStats.Where(q => q.AverageScore.HasValue).Average(q => q.AverageScore!.Value), 2)
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
                ProjectName = !string.IsNullOrEmpty(project.Code) ? $"{project.Code} - {project.Name}" : project.Name,
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
                    ? Math.Round((decimal)penaltyAppliedCount / responseCount * 100, 2)
                    : 0;
            }
            // Normal puanlı sorular
            else if (question.WeightPoints > 0 && responseCount > 0)
            {
                var answersWithEarned = group.Where(a => a.EarnedPoints.HasValue).ToList();
                if (answersWithEarned.Any())
                {
                    var avgEarned = answersWithEarned.Average(a => a.EarnedPoints!.Value);
                    avgScorePercentage = Math.Round(avgEarned / question.WeightPoints * 100, 2);
                }
                else
                {
                    var answerScores = group.Select(a =>
                        a.SubCriteriaSelections.Sum(s => s.SubCriteria?.WeightPoints ?? 0)
                    ).ToList();

                    if (answerScores.Any())
                    {
                        var avgScore = answerScores.Average();
                        avgScorePercentage = Math.Round((decimal)avgScore / question.WeightPoints * 100, 2);
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
                    ? Math.Round((decimal)selectionCount / responseCount * 100, 2)
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
            ProjectName = !string.IsNullOrEmpty(project.Code) ? $"{project.Code} - {project.Name}" : project.Name,
            TotalResponses = evaluationIds.Count,
            OverallAverageScore = Math.Round(overallAverage, 2),
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
                   p.IsActive && !p.IsDeleted)
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
                .OrderByDescending(e => e.CallDate ?? e.ControlDate ?? e.CreatedAt)
                .Select(e => (DateTime?)e.CreatedAt)
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
            .OrderByDescending(e => e.CallDate ?? e.ControlDate ?? e.CreatedAt)
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
                ProjectName = !string.IsNullOrEmpty(eval.Project?.Code) ? $"{eval.Project.Code} - {eval.Project.Name}" : (eval.Project?.Name ?? ""),
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
            ProjectName = !string.IsNullOrEmpty(evaluation.Project?.Code) ? $"{evaluation.Project.Code} - {evaluation.Project.Name}" : (evaluation.Project?.Name ?? ""),
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
                ProjectName = !string.IsNullOrEmpty(project.Code) ? $"{project.Code} - {project.Name}" : project.Name,
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
            ProjectName = !string.IsNullOrEmpty(project.Code) ? $"{project.Code} - {project.Name}" : project.Name,
            TotalResponses = evaluations.Count,
            Distribution = distribution
        };
    }


    // ===== INTERNAL HELPER METHODS (called by export methods above) =====

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
            .Include(e => e.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.OrganizationAssignments)
                    .ThenInclude(oa => oa.CustomerOrganization)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
            .Where(e => e.ProjectId == projectId && e.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        if (startDate.HasValue)
        {
            var startDateUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.CreatedAt >= startDateUtc);
        }

        if (endDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => e.CreatedAt <= endDateUtc);
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
                    avgScore = Math.Round((decimal)scoredAnswers.Average(a => (a.AnswerNumeric!.Value / (decimal)question.MaxPoints) * 100), 2);

                    // Puan dağılımı
                    scoreDist = new List<ScoreDistributionDto>();
                    for (int i = 0; i <= question.MaxPoints; i++)
                    {
                        var count = scoredAnswers.Count(a => a.AnswerNumeric == i);
                        scoreDist.Add(new ScoreDistributionDto
                        {
                            Score = i,
                            Count = count,
                            Percentage = responseCount > 0 ? Math.Round((decimal)count / responseCount * 100, 2) : 0
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
                            SelectionPercentage = responseCount > 0 ? Math.Round((decimal)selectionCount / responseCount * 100, 2) : 0
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
            ProjectName = !string.IsNullOrEmpty(project.Code) ? $"{project.Code} - {project.Name}" : project.Name,
            CustomerName = project.Customer?.CompanyName,
            OrganizationName = project.Organization?.Name,
            TotalResponses = evaluations.Count,
            TotalInvited = invitedCount > 0 ? invitedCount : evaluations.Count,
            CompletionRate = invitedCount > 0 ? Math.Round((decimal)evaluations.Count / invitedCount * 100, 2) : 100,
            AverageScore = evaluations.Any(e => e.ScorePercentage.HasValue)
                ? Math.Round((decimal)evaluations.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage!.Value), 2)
                : 0,
            TotalQuestions = questions.Count,
            QuestionResults = questionResults,
            Respondents = respondents
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
            .Where(e => e.ProjectId == projectId && e.StatusId == EvaluationStatuses.Ids.Completed)
            .OrderByDescending(e => e.CreatedAt)
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
                    AverageScore = totalMaxScore > 0 ? Math.Round(totalScore / totalMaxScore * 100, 2) : null
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
            ProjectName = !string.IsNullOrEmpty(project.Code) ? $"{project.Code} - {project.Name}" : project.Name,
            CustomerName = project.Customer?.CompanyName,
            OrganizationName = project.Organization?.Name,
            TotalInvitations = invitationCount > 0 ? invitationCount : evaluations.Count,
            TotalResponses = evaluations.Count,
            ResponseRate = invitationCount > 0 ? Math.Round((decimal)evaluations.Count / invitationCount * 100, 2) : 100,
            AverageScore = evaluations.Any(e => e.ScorePercentage.HasValue)
                ? Math.Round((decimal)evaluations.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage!.Value), 2)
                : null,
            TotalQuestions = questions.Count,
            GroupScores = groupScores.OrderBy(g => g.GroupName).ToList(),
            RecentRespondents = recentRespondents
        };
    }


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
                        e.Project.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey &&
                        e.ProjectId == projectId.Value);

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
                    ? (decimal?)Math.Round(g.Where(a => a.EarnedPoints.HasValue).Average(a => a.EarnedPoints!.Value) / g.Key.WeightPoints * 100, 2)
                    : null
            })
            .OrderBy(q => q.GroupName)
            .ThenBy(q => q.Order)
            .ToList();

        // Genel ortalama hesapla
        var overallAverage = questionStats.Where(q => q.AverageScore.HasValue).Any()
            ? Math.Round(questionStats.Where(q => q.AverageScore.HasValue).Average(q => q.AverageScore!.Value), 2)
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
                        e.ProjectId == projectId)
            .Select(e => e.Id)
            .ToListAsync();

        if (!evaluationIds.Any())
        {
            return new SurveyQuestionScoreDetailResultDto
            {
                ProjectId = projectId,
                ProjectName = !string.IsNullOrEmpty(project.Code) ? $"{project.Code} - {project.Name}" : project.Name,
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
                    ? Math.Round((decimal)penaltyAppliedCount / responseCount * 100, 2)
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
                    avgScorePercentage = Math.Round(avgEarned / question.WeightPoints * 100, 2);
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
                        avgScorePercentage = Math.Round((decimal)avgScore / question.WeightPoints * 100, 2);
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
                    ? Math.Round((decimal)selectionCount / responseCount * 100, 2)
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
            ProjectName = !string.IsNullOrEmpty(project.Code) ? $"{project.Code} - {project.Name}" : project.Name,
            TotalResponses = evaluationIds.Count,
            OverallAverageScore = Math.Round(overallAverage, 2),
            Questions = questionDetails
        };
    }

    /// <summary>
    /// Proje bazlı soru puan detayı Excel export
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
                        enneagramChecklistIds.Contains(e.Project.ChecklistId));

        // Proje filtresi
        if (filter.ProjectIds?.Any() == true)
        {
            query = query.Where(e => filter.ProjectIds.Contains(e.ProjectId));
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

        // Tarih filtresi (çoklu aralık OR ile birleştirilir)
        if (filter.DateRanges?.Any() == true)
        {
            var param = Expression.Parameter(typeof(Evaluation), "e");
            var createdAtProp = Expression.Property(param, nameof(Evaluation.CreatedAt));
            Expression? orBody = null;
            foreach (var dr in filter.DateRanges)
            {
                Expression? rangeExpr = null;
                if (dr.StartDate.HasValue)
                {
                    var startUtc = DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc);
                    var startConst = Expression.Constant(startUtc, typeof(DateTime));
                    rangeExpr = Expression.GreaterThanOrEqual(createdAtProp, startConst);
                }
                if (dr.EndDate.HasValue)
                {
                    var endUtc = DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    var endConst = Expression.Constant(endUtc, typeof(DateTime));
                    var leExpr = Expression.LessThanOrEqual(createdAtProp, endConst);
                    rangeExpr = rangeExpr != null ? Expression.AndAlso(rangeExpr, leExpr) : leExpr;
                }
                if (rangeExpr != null)
                    orBody = orBody != null ? Expression.OrElse(orBody, rangeExpr) : rangeExpr;
            }
            if (orBody != null)
                query = query.Where(Expression.Lambda<Func<Evaluation, bool>>(orBody, param));
        }

        var totalCount = await query.CountAsync();

        // Sayfalama için sonuçları al
        var evaluations = await query
            .OrderByDescending(e => e.CreatedAt)
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
                ProjectId = eval.ProjectId,
                ProjectName = !string.IsNullOrEmpty(eval.Project?.Code) ? $"{eval.Project.Code} - {eval.Project.Name}" : (eval.Project?.Name ?? ""),
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

}
