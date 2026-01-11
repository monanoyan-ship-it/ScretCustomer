using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedReportResult<EvaluationReportDto>> GetEvaluationsAsync(ReportFilterDto filter)
    {
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Include(e => e.Evaluator)
            .Include(e => e.EvaluatedPersonnel)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.OrganizationAssignments)
                    .ThenInclude(oa => oa.Supervisor)
            .Include(e => e.AssignmentPeriod)
            .AsQueryable();

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.EvaluatorId.HasValue)
            query = query.Where(e => e.EvaluatorId == filter.EvaluatorId.Value);

        if (filter.ChecklistId.HasValue)
            query = query.Where(e => e.Assignment.ChecklistId == filter.ChecklistId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(e => e.CompletedAt >= filter.StartDate.Value || e.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(e => e.CompletedAt <= filter.EndDate.Value || e.CreatedAt <= filter.EndDate.Value);

        if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<EvaluationStatus>(filter.Status, out var status))
            query = query.Where(e => e.Status == status);

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var evaluations = await query
            .OrderByDescending(e => e.CompletedAt ?? e.CreatedAt)
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

    public async Task<EvaluationDetailReportDto?> GetEvaluationDetailAsync(int evaluationId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Include(e => e.Evaluator)
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
                : null,
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
            DueDate = evaluation.Assignment.DueDate,
            TotalScore = evaluation.TotalScore,
            MaxScore = evaluation.MaxScore,
            ScorePercentage = evaluation.ScorePercentage,
            YellowCardCount = evaluation.YellowCardCount,
            RedCardCount = evaluation.RedCardCount,
            Status = evaluation.Status.ToString(),
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
                        IsNA = a.IsNA,
                        GivenPoints = a.EarnedPoints,
                        MaxPoints = a.Question.WeightPoints,
                        PenaltyType = a.AppliedPenaltyType.ToString(),
                        Notes = a.Notes,
                        SelectedSubCriteria = a.SubCriteriaSelections
                            .Select(s => s.SubCriteria.Description)
                            .ToList()
                    }).ToList()
                }).ToList()
        };

        return dto;
    }

    public async Task<SummaryReportDto> GetSummaryReportAsync(ReportFilterDto filter)
    {
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Evaluator)
            .AsQueryable();

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(e => e.CompletedAt >= filter.StartDate.Value || e.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(e => e.CompletedAt <= filter.EndDate.Value || e.CreatedAt <= filter.EndDate.Value);

        var evaluations = await query.ToListAsync();

        var completedEvaluations = evaluations.Where(e => e.Status == EvaluationStatus.Completed && e.ScorePercentage.HasValue).ToList();

        var summary = new SummaryReportDto
        {
            TotalEvaluations = evaluations.Count,
            CompletedEvaluations = completedEvaluations.Count,
            PendingEvaluations = evaluations.Count(e => e.Status == EvaluationStatus.Draft || e.Status == EvaluationStatus.InProgress || e.Status == EvaluationStatus.Pending),
            AverageScore = completedEvaluations.Any()
                ? Math.Round(completedEvaluations.Average(e => e.ScorePercentage ?? 0), 2)
                : 0,
            MinScore = completedEvaluations.Any()
                ? completedEvaluations.Min(e => e.ScorePercentage ?? 0)
                : 0,
            MaxScore = completedEvaluations.Any()
                ? completedEvaluations.Max(e => e.ScorePercentage ?? 0)
                : 0,
            TotalYellowCards = evaluations.Sum(e => e.YellowCardCount),
            TotalRedCards = evaluations.Sum(e => e.RedCardCount),
            ProjectSummaries = evaluations
                .Where(e => e.Assignment.Project != null)
                .GroupBy(e => e.Assignment.Project!)
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
                .ToList(),
            EvaluatorSummaries = evaluations
                .Where(e => e.Evaluator != null)
                .GroupBy(e => e.Evaluator!)
                .Select(g => new EvaluatorSummaryReportDto
                {
                    EvaluatorId = g.Key.Id,
                    EvaluatorName = $"{g.Key.FirstName} {g.Key.LastName}",
                    EvaluationCount = g.Count(),
                    AverageScore = g.Where(e => e.ScorePercentage.HasValue).Any()
                        ? Math.Round(g.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage!.Value), 2)
                        : 0
                })
                .OrderByDescending(ev => ev.EvaluationCount)
                .ToList()
        };

        return summary;
    }

    public async Task<ExcelExportDto> ExportEvaluationsToExcelAsync(ReportFilterDto filter)
    {
        // Remove pagination for export
        filter.Page = 1;
        filter.PageSize = 10000;

        var result = await GetEvaluationsAsync(filter);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Değerlendirmeler");

        // Headers
        var headers = new[]
        {
            "Proje", "Proje Kodu", "Kontrol Listesi",
            "Değerlendirici", "Değerlendirilen Personel", "Değerlendirme Tarihi", "Tamamlanma Tarihi",
            "Son Tarih", "Puan", "Maks Puan", "Yüzde", "Sarı Kart", "Kırmızı Kart",
            "Durum", "Çağrı ID", "Çağrı Tarihi", "Süre (dk)", "Yorum"
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
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Include(e => e.Evaluator)
            .Include(e => e.EvaluatedPersonnel)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .AsQueryable();

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(e => e.CompletedAt >= filter.StartDate.Value || e.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(e => e.CompletedAt <= filter.EndDate.Value || e.CreatedAt <= filter.EndDate.Value);

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
            "Cevap", "Sayısal Cevap", "N/A", "Verilen Puan", "Maks Puan",
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
                detailSheet.Cell(detailRow, 9).Value = answer.IsNA ? "Evet" : "Hayır";
                detailSheet.Cell(detailRow, 10).Value = answer.EarnedPoints ?? 0;
                detailSheet.Cell(detailRow, 11).Value = answer.Question.WeightPoints;
                detailSheet.Cell(detailRow, 12).Value = answer.AppliedPenaltyType.ToString();
                detailSheet.Cell(detailRow, 13).Value = answer.Notes ?? "";
                detailRow++;
            }
        }

        detailSheet.Columns().AdjustToContents();

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
        return new EvaluationReportDto
        {
            EvaluationId = evaluation.Id,
            AssignmentId = evaluation.AssignmentId,
            ProjectName = evaluation.Assignment.Project?.Name ?? "",
            ProjectCode = evaluation.Assignment.Project?.Code,
            ChecklistName = evaluation.Assignment.Checklist?.Name ?? "",
            EvaluatorName = evaluation.Evaluator != null
                ? $"{evaluation.Evaluator.FirstName} {evaluation.Evaluator.LastName}"
                : null,
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
                : null,
            EvaluationDate = evaluation.ControlDate ?? evaluation.CompletedAt,
            CompletedAt = evaluation.CompletedAt,
            DueDate = evaluation.Assignment.DueDate,
            TotalScore = evaluation.TotalScore,
            MaxScore = evaluation.MaxScore,
            ScorePercentage = evaluation.ScorePercentage,
            YellowCardCount = evaluation.YellowCardCount,
            RedCardCount = evaluation.RedCardCount,
            Status = evaluation.Status.ToString(),
            CallId = evaluation.CallId,
            CallDate = evaluation.CallDate,
            CallTime = evaluation.CallTime,
            Duration = evaluation.Duration,
            Comment = evaluation.EvaluationComment
        };
    }

    // ===== CEZALI KL RAPORU =====

    public async Task<PenaltyReportResultDto> GetPenaltiesReportAsync(PenaltyFilterDto filter)
    {
        var query = _context.Answers
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Assignment)
                    .ThenInclude(a => a.Project)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Evaluator)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedPersonnel)
            .Include(a => a.Question)
                .ThenInclude(q => q.Checklist)
            .Where(a => a.AppliedPenaltyType != PenaltyType.None)
            .AsQueryable();

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(a => a.Evaluation.Assignment.ProjectId == filter.ProjectId.Value);

        if (!string.IsNullOrEmpty(filter.PenaltyType) && Enum.TryParse<PenaltyType>(filter.PenaltyType, out var penaltyType))
            query = query.Where(a => a.AppliedPenaltyType == penaltyType);

        if (filter.StartDate.HasValue)
            query = query.Where(a => a.Evaluation.CompletedAt >= filter.StartDate.Value || a.Evaluation.ControlDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(a => a.Evaluation.CompletedAt <= filter.EndDate.Value || a.Evaluation.ControlDate <= filter.EndDate.Value);

        var penaltyAnswers = await query.ToListAsync();

        // Summary
        var summary = new PenaltySummaryDto
        {
            TotalPenalties = penaltyAnswers.Count,
            TotalYellowCards = penaltyAnswers.Count(a => a.AppliedPenaltyType == PenaltyType.YellowCard),
            TotalRedCards = penaltyAnswers.Count(a => a.AppliedPenaltyType == PenaltyType.RedCard),
            AffectedEvaluations = penaltyAnswers.Select(a => a.EvaluationId).Distinct().Count()
        };

        // Detailed penalties
        var penalties = penaltyAnswers
            .OrderByDescending(a => a.Evaluation.ControlDate ?? a.Evaluation.CompletedAt)
            .Select(a => new PenaltyDetailDto
            {
                EvaluationId = a.EvaluationId,
                AnswerId = a.Id,
                QuestionId = a.QuestionId,
                QuestionText = a.Question?.Text ?? "",
                GroupName = a.Question?.GroupName ?? "",
                PenaltyType = a.AppliedPenaltyType.ToString(),
                ProjectName = a.Evaluation.Assignment.Project?.Name ?? "",
                ChecklistName = a.Question?.Checklist?.Name,
                EvaluatorName = a.Evaluation.Evaluator != null
                    ? $"{a.Evaluation.Evaluator.FirstName} {a.Evaluation.Evaluator.LastName}"
                    : null,
                EvaluatedPersonnelName = a.Evaluation.EvaluatedPersonnel != null
                    ? $"{a.Evaluation.EvaluatedPersonnel.FirstName} {a.Evaluation.EvaluatedPersonnel.LastName}"
                    : a.Evaluation.EvaluatedUnknownPersonnel,
                EvaluationDate = a.Evaluation.ControlDate ?? a.Evaluation.CompletedAt,
                Notes = a.Notes
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
                YellowCardCount = g.Count(a => a.AppliedPenaltyType == PenaltyType.YellowCard),
                RedCardCount = g.Count(a => a.AppliedPenaltyType == PenaltyType.RedCard),
                TotalPenalties = g.Count()
            })
            .OrderByDescending(q => q.TotalPenalties)
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
                YellowCardCount = g.Count(a => a.AppliedPenaltyType == PenaltyType.YellowCard),
                RedCardCount = g.Count(a => a.AppliedPenaltyType == PenaltyType.RedCard),
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
            MonthlyTrend = monthlyTrend
        };
    }

    public async Task<ExcelExportDto> ExportPenaltiesToExcelAsync(PenaltyFilterDto filter)
    {
        var report = await GetPenaltiesReportAsync(filter);

        using var workbook = new XLWorkbook();

        // Summary sheet
        var summarySheet = workbook.Worksheets.Add("Özet");
        summarySheet.Cell(1, 1).Value = "Toplam Cezalı";
        summarySheet.Cell(1, 2).Value = report.Summary.TotalPenalties;
        summarySheet.Cell(2, 1).Value = "Sarı Kart";
        summarySheet.Cell(2, 2).Value = report.Summary.TotalYellowCards;
        summarySheet.Cell(3, 1).Value = "Kırmızı Kart";
        summarySheet.Cell(3, 2).Value = report.Summary.TotalRedCards;
        summarySheet.Cell(4, 1).Value = "Etkilenen Değerlendirme";
        summarySheet.Cell(4, 2).Value = report.Summary.AffectedEvaluations;
        summarySheet.Cell(5, 1).Value = "Rapor Tarihi";
        summarySheet.Cell(5, 2).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        summarySheet.Columns().AdjustToContents();

        // Penalties detail sheet
        var penaltiesSheet = workbook.Worksheets.Add("Cezalı Değerlendirmeler");
        var headers = new[]
        {
            "Tarih", "Proje", "Kontrol Listesi", "Bölüm", "Soru",
            "Ceza Tipi", "Değerlendirici", "Denetlenen", "Not"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            penaltiesSheet.Cell(1, i + 1).Value = headers[i];
            penaltiesSheet.Cell(1, i + 1).Style.Font.Bold = true;
            penaltiesSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var penalty in report.Penalties)
        {
            penaltiesSheet.Cell(row, 1).Value = penalty.EvaluationDate?.ToString("dd.MM.yyyy") ?? "";
            penaltiesSheet.Cell(row, 2).Value = penalty.ProjectName;
            penaltiesSheet.Cell(row, 3).Value = penalty.ChecklistName ?? "";
            penaltiesSheet.Cell(row, 4).Value = penalty.GroupName;
            penaltiesSheet.Cell(row, 5).Value = penalty.QuestionText;
            penaltiesSheet.Cell(row, 6).Value = penalty.PenaltyType == "YellowCard" ? "Sarı Kart" : "Kırmızı Kart";
            penaltiesSheet.Cell(row, 7).Value = penalty.EvaluatorName ?? "";
            penaltiesSheet.Cell(row, 8).Value = penalty.EvaluatedPersonnelName ?? "";
            penaltiesSheet.Cell(row, 9).Value = penalty.Notes ?? "";
            row++;
        }
        penaltiesSheet.Columns().AdjustToContents();

        // Top questions sheet
        var questionsSheet = workbook.Worksheets.Add("En Çok Ceza Alan Sorular");
        questionsSheet.Cell(1, 1).Value = "Soru";
        questionsSheet.Cell(1, 1).Style.Font.Bold = true;
        questionsSheet.Cell(1, 2).Value = "Kontrol Listesi";
        questionsSheet.Cell(1, 2).Style.Font.Bold = true;
        questionsSheet.Cell(1, 3).Value = "Bölüm";
        questionsSheet.Cell(1, 3).Style.Font.Bold = true;
        questionsSheet.Cell(1, 4).Value = "Sarı Kart";
        questionsSheet.Cell(1, 4).Style.Font.Bold = true;
        questionsSheet.Cell(1, 5).Value = "Kırmızı Kart";
        questionsSheet.Cell(1, 5).Style.Font.Bold = true;
        questionsSheet.Cell(1, 6).Value = "Toplam";
        questionsSheet.Cell(1, 6).Style.Font.Bold = true;

        row = 2;
        foreach (var q in report.TopPenaltyQuestions)
        {
            questionsSheet.Cell(row, 1).Value = q.QuestionText;
            questionsSheet.Cell(row, 2).Value = q.ChecklistName;
            questionsSheet.Cell(row, 3).Value = q.GroupName;
            questionsSheet.Cell(row, 4).Value = q.YellowCardCount;
            questionsSheet.Cell(row, 5).Value = q.RedCardCount;
            questionsSheet.Cell(row, 6).Value = q.TotalPenalties;
            row++;
        }
        questionsSheet.Columns().AdjustToContents();

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

    public async Task<IEnumerable<PersonnelListItemDto>> GetEvaluatedPersonnelListAsync()
    {
        // Değerlendirmede bulunan personelleri getir (EvaluatedCustomerPersonnel = CustomerPersonnel entity)
        var personnelFromEvaluations = await _context.Evaluations
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Where(e => e.EvaluatedCustomerPersonnelId != null && e.Status == EvaluationStatus.Completed)
            .Select(e => new
            {
                e.EvaluatedCustomerPersonnelId,
                e.EvaluatedCustomerPersonnel!.FirstName,
                e.EvaluatedCustomerPersonnel.LastName
            })
            .Distinct()
            .ToListAsync();

        return personnelFromEvaluations
            .GroupBy(p => p.EvaluatedCustomerPersonnelId)
            .Select(g => new PersonnelListItemDto
            {
                Id = g.Key!.Value,
                Name = $"{g.First().FirstName} {g.First().LastName}",
                Title = null
            })
            .OrderBy(p => p.Name)
            .ToList();
    }

    public async Task<PersonnelReportCardDto?> GetPersonnelReportCardAsync(PersonnelReportCardFilterDto filter)
    {
        // EvaluatedPersonnel aslında User entity'sine işaret ediyor
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == filter.PersonnelId);

        if (user == null)
            return null;

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Include(e => e.Evaluator)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Where(e => e.EvaluatedCustomerPersonnelId == filter.PersonnelId && e.Status == EvaluationStatus.Completed);

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(e => e.CompletedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(e => e.CompletedAt <= filter.EndDate.Value);

        var evaluations = await query.ToListAsync();

        if (!evaluations.Any())
        {
            return new PersonnelReportCardDto
            {
                PersonnelId = user.Id,
                PersonnelName = $"{user.FirstName} {user.LastName}",
                Title = user.Role.ToString(),
                Department = null
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

        // Son değerlendirmeler
        var recentEvaluations = evaluations
            .OrderByDescending(e => e.CompletedAt)
            .Take(10)
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
                Status = e.Status.ToString()
            })
            .ToList();

        // Güçlü ve zayıf yönler (soru bazlı analiz)
        var questionPerformance = allAnswers
            .Where(a => !a.IsNA && a.Question != null)
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
        var weaknesses = questionPerformance.OrderBy(q => q.PercentageScore).Take(5).ToList();

        return new PersonnelReportCardDto
        {
            PersonnelId = user.Id,
            PersonnelName = $"{user.FirstName} {user.LastName}",
            Title = user.Role.ToString(),
            Department = null,
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
            Weaknesses = weaknesses
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

        // Son Değerlendirmeler
        var evalSheet = workbook.Worksheets.Add("Son Değerlendirmeler");
        evalSheet.Cell(1, 1).Value = "Tarih";
        evalSheet.Cell(1, 1).Style.Font.Bold = true;
        evalSheet.Cell(1, 2).Value = "Proje";
        evalSheet.Cell(1, 2).Style.Font.Bold = true;
        evalSheet.Cell(1, 3).Value = "Kontrol Listesi";
        evalSheet.Cell(1, 3).Style.Font.Bold = true;
        evalSheet.Cell(1, 4).Value = "Puan";
        evalSheet.Cell(1, 4).Style.Font.Bold = true;
        evalSheet.Cell(1, 5).Value = "Sarı Kart";
        evalSheet.Cell(1, 5).Style.Font.Bold = true;
        evalSheet.Cell(1, 6).Value = "Kırmızı Kart";
        evalSheet.Cell(1, 6).Style.Font.Bold = true;

        row = 2;
        foreach (var eval in report.RecentEvaluations)
        {
            evalSheet.Cell(row, 1).Value = eval.EvaluationDate?.ToString("dd.MM.yyyy") ?? "-";
            evalSheet.Cell(row, 2).Value = eval.ProjectName;
            evalSheet.Cell(row, 3).Value = eval.ChecklistName;
            evalSheet.Cell(row, 4).Value = $"{eval.ScorePercentage:F1}%";
            evalSheet.Cell(row, 5).Value = eval.YellowCards;
            evalSheet.Cell(row, 6).Value = eval.RedCards;
            row++;
        }
        evalSheet.Columns().AdjustToContents();

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

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"TemsilciKarnesi_{report.PersonnelName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== ÖNERİLER RAPORU (Video 5-6) =====

    public async Task<SuggestionsReportResultDto> GetSuggestionsReportAsync(SuggestionsFilterDto filter)
    {
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
            .Include(a => a.Question)
                .ThenInclude(q => q.Checklist)
            .Where(a => !string.IsNullOrEmpty(a.Notes) || !string.IsNullOrEmpty(a.RecommendationNotes))
            .Where(a => a.Evaluation.Status == EvaluationStatus.Completed)
            .AsQueryable();

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(a => a.Evaluation.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.ChecklistId.HasValue)
            query = query.Where(a => a.Evaluation.Assignment.ChecklistId == filter.ChecklistId.Value);

        if (filter.EvaluatorId.HasValue)
            query = query.Where(a => a.Evaluation.EvaluatorId == filter.EvaluatorId.Value);

        if (filter.PersonnelId.HasValue)
            query = query.Where(a => a.Evaluation.EvaluatedCustomerPersonnelId == filter.PersonnelId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(a => a.Evaluation.CompletedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(a => a.Evaluation.CompletedAt <= filter.EndDate.Value);

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
                .Count()
        };

        // Map to DTOs
        var suggestionDtos = suggestions.Select(a => new SuggestionDetailDto
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
            EvaluatedPersonnelName = a.Evaluation.EvaluatedPersonnel != null
                ? $"{a.Evaluation.EvaluatedPersonnel.FirstName} {a.Evaluation.EvaluatedPersonnel.LastName}"
                : a.Evaluation.EvaluatedUnknownPersonnel,
            EvaluationDate = a.Evaluation.CompletedAt ?? a.Evaluation.ControlDate,
            CallId = a.Evaluation.CallId,
            IsPenaltyApplied = a.IsPenaltyApplied,
            PenaltyType = a.AppliedPenaltyType != PenaltyType.None ? a.AppliedPenaltyType.ToString() : null
        }).ToList();

        return new SuggestionsReportResultDto
        {
            Summary = summary,
            Suggestions = suggestionDtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<QuestionSuggestionSummaryDto>> GetTopSuggestedQuestionsAsync(SuggestionsFilterDto filter, int top = 10)
    {
        var query = _context.Answers
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Assignment)
            .Include(a => a.Question)
                .ThenInclude(q => q.Checklist)
            .Where(a => !string.IsNullOrEmpty(a.Notes) || !string.IsNullOrEmpty(a.RecommendationNotes))
            .Where(a => a.Evaluation.Status == EvaluationStatus.Completed)
            .AsQueryable();

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(a => a.Evaluation.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.ChecklistId.HasValue)
            query = query.Where(a => a.Evaluation.Assignment.ChecklistId == filter.ChecklistId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(a => a.Evaluation.CompletedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(a => a.Evaluation.CompletedAt <= filter.EndDate.Value);

        var answers = await query.ToListAsync();

        return answers
            .Where(a => a.Question != null)
            .GroupBy(a => new
            {
                a.QuestionId,
                QuestionText = a.Question!.Text,
                GroupName = a.Question.GroupName ?? "",
                ChecklistName = a.Question.Checklist?.Name ?? ""
            })
            .Select(g => new QuestionSuggestionSummaryDto
            {
                QuestionId = g.Key.QuestionId,
                QuestionText = g.Key.QuestionText,
                GroupName = g.Key.GroupName,
                ChecklistName = g.Key.ChecklistName,
                SuggestionCount = g.Count(),
                AverageScore = g.Where(a => a.EarnedPoints.HasValue && a.Question?.WeightPoints > 0).Any()
                    ? Math.Round(g.Where(a => a.EarnedPoints.HasValue && a.Question?.WeightPoints > 0)
                        .Average(a => (a.EarnedPoints!.Value / a.Question!.WeightPoints) * 100), 1)
                    : 0
            })
            .OrderByDescending(q => q.SuggestionCount)
            .Take(top)
            .ToList();
    }

    public async Task<ExcelExportDto> ExportSuggestionsToExcelAsync(SuggestionsFilterDto filter)
    {
        // Remove pagination for export
        filter.Page = 1;
        filter.PageSize = 10000;

        var report = await GetSuggestionsReportAsync(filter);
        var topQuestions = await GetTopSuggestedQuestionsAsync(filter, 20);

        using var workbook = new XLWorkbook();

        // Summary sheet
        var summarySheet = workbook.Worksheets.Add("Özet");
        summarySheet.Cell(1, 1).Value = "ÖNERİLER RAPORU";
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 16;

        summarySheet.Cell(3, 1).Value = "Toplam Öneri/Not:";
        summarySheet.Cell(3, 2).Value = report.Summary.TotalSuggestions;
        summarySheet.Cell(4, 1).Value = "Önerili Değerlendirme Sayısı:";
        summarySheet.Cell(4, 2).Value = report.Summary.TotalEvaluationsWithSuggestions;
        summarySheet.Cell(5, 1).Value = "Değerlendirici Sayısı:";
        summarySheet.Cell(5, 2).Value = report.Summary.UniqueEvaluators;
        summarySheet.Cell(6, 1).Value = "Personel Sayısı:";
        summarySheet.Cell(6, 2).Value = report.Summary.UniquePersonnel;
        summarySheet.Cell(7, 1).Value = "Rapor Tarihi:";
        summarySheet.Cell(7, 2).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        summarySheet.Columns().AdjustToContents();

        // Details sheet
        var detailsSheet = workbook.Worksheets.Add("Öneriler Listesi");
        var headers = new[]
        {
            "Tarih", "Proje", "Kontrol Listesi", "Bölüm", "Soru",
            "Notlar", "Öneri", "Verilen Puan", "Maks Puan", "Yüzde",
            "Değerlendirici", "Personel", "Çağrı ID", "Ceza"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            detailsSheet.Cell(1, i + 1).Value = headers[i];
            detailsSheet.Cell(1, i + 1).Style.Font.Bold = true;
            detailsSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var item in report.Suggestions)
        {
            detailsSheet.Cell(row, 1).Value = item.EvaluationDate?.ToString("dd.MM.yyyy") ?? "";
            detailsSheet.Cell(row, 2).Value = item.ProjectName;
            detailsSheet.Cell(row, 3).Value = item.ChecklistName;
            detailsSheet.Cell(row, 4).Value = item.GroupName;
            detailsSheet.Cell(row, 5).Value = item.QuestionText;
            detailsSheet.Cell(row, 6).Value = item.Notes ?? "";
            detailsSheet.Cell(row, 7).Value = item.RecommendationNotes ?? "";
            detailsSheet.Cell(row, 8).Value = item.GivenPoints ?? 0;
            detailsSheet.Cell(row, 9).Value = item.MaxPoints ?? 0;
            detailsSheet.Cell(row, 10).Value = item.PercentageScore.HasValue ? $"{item.PercentageScore:F1}%" : "";
            detailsSheet.Cell(row, 11).Value = item.EvaluatorName ?? "";
            detailsSheet.Cell(row, 12).Value = item.EvaluatedPersonnelName ?? "";
            detailsSheet.Cell(row, 13).Value = item.CallId ?? "";
            detailsSheet.Cell(row, 14).Value = item.PenaltyType ?? "";
            row++;
        }

        detailsSheet.Columns().AdjustToContents();

        // Top Questions sheet
        var questionsSheet = workbook.Worksheets.Add("En Çok Öneri Yazılan Sorular");
        questionsSheet.Cell(1, 1).Value = "Soru";
        questionsSheet.Cell(1, 1).Style.Font.Bold = true;
        questionsSheet.Cell(1, 2).Value = "Kontrol Listesi";
        questionsSheet.Cell(1, 2).Style.Font.Bold = true;
        questionsSheet.Cell(1, 3).Value = "Bölüm";
        questionsSheet.Cell(1, 3).Style.Font.Bold = true;
        questionsSheet.Cell(1, 4).Value = "Öneri Sayısı";
        questionsSheet.Cell(1, 4).Style.Font.Bold = true;
        questionsSheet.Cell(1, 5).Value = "Ort. Puan";
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

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Oneriler_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }
}
