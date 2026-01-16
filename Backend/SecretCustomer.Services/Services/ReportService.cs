using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Auth;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILocalizationService _localizationService;

    public ReportService(ApplicationDbContext context, ILocalizationService localizationService)
    {
        _context = context;
        _localizationService = localizationService;
    }

    public async Task<PagedReportResult<EvaluationReportDto>> GetEvaluationsAsync(ReportFilterDto filter)
    {
        // Liste görünümü için Answers dahil EDİLMEZ - sadece detay sayfasında lazım
        // Bu Include optimizasyonu performansı önemli ölçüde artırır
        var query = _context.Evaluations
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
            // .Include(e => e.Answers) - KALDIRILDI: Liste için gereksiz, detay sayfasında ayrı yükleniyor
            .AsQueryable();

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (!string.IsNullOrEmpty(filter.ProjectType))
        {
            var projectTypeItem = ProjectTypes.GetBySystemName(filter.ProjectType);
            if (projectTypeItem != null)
                query = query.Where(e => e.Assignment.Project.ProjectTypeId == projectTypeItem.Id);
        }

        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        // /Listenings sayfası Çağrı Denetimi için
        if (string.IsNullOrEmpty(filter.ProjectType) && !filter.ProjectId.HasValue)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorId.HasValue)
            query = query.Where(e => e.EvaluatorId == filter.EvaluatorId.Value);

        if (filter.ChecklistId.HasValue)
            query = query.Where(e => e.Assignment.ChecklistId == filter.ChecklistId.Value);

        if (filter.StartDate.HasValue)
        {
            var startDateUtc = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt >= startDateUtc || e.CreatedAt >= startDateUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt <= endDateUtc || e.CreatedAt <= endDateUtc);
        }

        // PRENSIP: Export için sadece Completed değerlendirmeler dahil edilir (taslaklar hariç)
        if (filter.ForExport)
        {
            query = query.Where(e => e.StatusId == EvaluationStatuses.Ids.Completed);
        }
        else if (!string.IsNullOrEmpty(filter.Status))
        {
            var statusItem = EvaluationStatuses.GetBySystemName(filter.Status);
            if (statusItem != null)
                query = query.Where(e => e.StatusId == statusItem.Id);
        }

        // Evaluation source filter
        if (!string.IsNullOrEmpty(filter.EvaluationSource))
        {
            if (filter.EvaluationSource == "internal")
            {
                // Müşteri iç değerlendirmeleri (CustomerPersonnel = 4)
                query = query.Where(e => e.Assignment.TypeId == AssignmentTypes.Ids.CustomerPersonnel);
            }
            else if (filter.EvaluationSource == "ours")
            {
                // Bizim değerlendirmelerimiz (CustomerPersonnel dışındakiler)
                query = query.Where(e => e.Assignment.TypeId != AssignmentTypes.Ids.CustomerPersonnel);
            }
        }

        // Customer filter (evaluated personnel's customer)
        if (filter.CustomerId.HasValue)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null && e.EvaluatedCustomerPersonnel.CustomerId == filter.CustomerId.Value);

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerId.HasValue)
            query = query.Where(e => e.Assignment.Project.CustomerId == filter.ProjectCustomerId.Value);

        // Organization filter
        if (filter.OrganizationId.HasValue)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == filter.OrganizationId.Value));

        // Period filter
        if (filter.PeriodId.HasValue)
            query = query.Where(e => e.AssignmentPeriodId == filter.PeriodId.Value);

        // Evaluated Personnel name search (case-insensitive)
        if (!string.IsNullOrEmpty(filter.EvaluatedPersonnelName))
        {
            query = query.Where(e =>
                (e.EvaluatedCustomerPersonnel != null &&
                    (EF.Functions.ILike(e.EvaluatedCustomerPersonnel.FirstName, $"%{filter.EvaluatedPersonnelName}%") ||
                     EF.Functions.ILike(e.EvaluatedCustomerPersonnel.LastName, $"%{filter.EvaluatedPersonnelName}%"))) ||
                (e.EvaluatedUnknownPersonnel != null && EF.Functions.ILike(e.EvaluatedUnknownPersonnel, $"%{filter.EvaluatedPersonnelName}%")));
        }

        // Supervisor name search
        if (!string.IsNullOrEmpty(filter.SupervisorName))
        {
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa =>
                    oa.Supervisor != null &&
                    (EF.Functions.ILike(oa.Supervisor.FirstName, $"%{filter.SupervisorName}%") ||
                     EF.Functions.ILike(oa.Supervisor.LastName, $"%{filter.SupervisorName}%"))));
        }

        // CallId search
        if (!string.IsNullOrEmpty(filter.CallId))
            query = query.Where(e => e.CallId != null && EF.Functions.ILike(e.CallId, $"%{filter.CallId}%"));

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
        var query = _context.Evaluations.AsQueryable();

        // Apply filters (aynı filtreler)
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (!string.IsNullOrEmpty(filter.ProjectType))
        {
            var projectTypeItem = ProjectTypes.GetBySystemName(filter.ProjectType);
            if (projectTypeItem != null)
                query = query.Where(e => e.Assignment.Project.ProjectTypeId == projectTypeItem.Id);
        }

        if (string.IsNullOrEmpty(filter.ProjectType) && !filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);

        if (filter.EvaluatorId.HasValue)
            query = query.Where(e => e.EvaluatorId == filter.EvaluatorId.Value);

        if (filter.ChecklistId.HasValue)
            query = query.Where(e => e.Assignment.ChecklistId == filter.ChecklistId.Value);

        if (filter.StartDate.HasValue)
        {
            var startDateUtc = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt >= startDateUtc || e.CreatedAt >= startDateUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt <= endDateUtc || e.CreatedAt <= endDateUtc);
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            var statusItem = EvaluationStatuses.GetBySystemName(filter.Status);
            if (statusItem != null)
                query = query.Where(e => e.StatusId == statusItem.Id);
        }

        if (!string.IsNullOrEmpty(filter.EvaluationSource))
        {
            if (filter.EvaluationSource == "internal")
                query = query.Where(e => e.Assignment.TypeId == AssignmentTypes.Ids.CustomerPersonnel);
            else if (filter.EvaluationSource == "ours")
                query = query.Where(e => e.Assignment.TypeId != AssignmentTypes.Ids.CustomerPersonnel);
        }

        if (filter.CustomerId.HasValue)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null && e.EvaluatedCustomerPersonnel.CustomerId == filter.CustomerId.Value);

        if (filter.ProjectCustomerId.HasValue)
            query = query.Where(e => e.Assignment.Project.CustomerId == filter.ProjectCustomerId.Value);

        if (filter.OrganizationId.HasValue)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == filter.OrganizationId.Value));

        if (filter.PeriodId.HasValue)
            query = query.Where(e => e.AssignmentPeriodId == filter.PeriodId.Value);

        if (!string.IsNullOrEmpty(filter.EvaluatedPersonnelName))
        {
            query = query.Where(e =>
                (e.EvaluatedCustomerPersonnel != null &&
                    (EF.Functions.ILike(e.EvaluatedCustomerPersonnel.FirstName, $"%{filter.EvaluatedPersonnelName}%") ||
                     EF.Functions.ILike(e.EvaluatedCustomerPersonnel.LastName, $"%{filter.EvaluatedPersonnelName}%"))) ||
                (e.EvaluatedUnknownPersonnel != null && EF.Functions.ILike(e.EvaluatedUnknownPersonnel, $"%{filter.EvaluatedPersonnelName}%")));
        }

        if (!string.IsNullOrEmpty(filter.SupervisorName))
        {
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa =>
                    oa.Supervisor != null &&
                    (EF.Functions.ILike(oa.Supervisor.FirstName, $"%{filter.SupervisorName}%") ||
                     EF.Functions.ILike(oa.Supervisor.LastName, $"%{filter.SupervisorName}%"))));
        }

        if (!string.IsNullOrEmpty(filter.CallId))
            query = query.Where(e => e.CallId != null && EF.Functions.ILike(e.CallId, $"%{filter.CallId}%"));

        return await query.CountAsync();
    }

    public async Task<EvaluationDetailReportDto?> GetEvaluationDetailAsync(int evaluationId)
    {
        var evaluation = await _context.Evaluations
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
                }).ToList()
        };

        return dto;
    }

    public async Task<ExcelExportDto?> ExportEvaluationDetailToExcelAsync(int evaluationId)
    {
        var detail = await GetEvaluationDetailAsync(evaluationId);
        if (detail == null)
            return null;

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Değerlendirme Detayı");

        int row = 1;

        // ===== 3 BÖLÜM YAN YANA =====
        // Başlık satırı
        worksheet.Cell(row, 1).Value = "Değerlendirilen Bilgileri";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.Range(row, 1, row, 2).Merge();

        worksheet.Cell(row, 3).Value = "Çağrı Bilgileri";
        worksheet.Cell(row, 3).Style.Font.Bold = true;
        worksheet.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.Range(row, 3, row, 4).Merge();
        row++;

        // Satır 1
        worksheet.Cell(row, 1).Value = "Müşteri";
        worksheet.Cell(row, 2).Value = detail.CustomerName ?? "-";
        worksheet.Cell(row, 3).Value = "Çağrı ID";
        worksheet.Cell(row, 4).Value = detail.CallId ?? "-";
        row++;

        // Satır 2
        worksheet.Cell(row, 1).Value = "Organizasyon";
        worksheet.Cell(row, 2).Value = detail.OrganizationName ?? "-";
        worksheet.Cell(row, 3).Value = "Çağrı Tarihi";
        worksheet.Cell(row, 4).Value = detail.CallDate?.ToString("dd.MM.yyyy") ?? "-";
        row++;

        // Satır 3
        worksheet.Cell(row, 1).Value = "Değerlendirilen";
        worksheet.Cell(row, 2).Value = detail.EvaluatedPersonnelName ?? "-";
        worksheet.Cell(row, 3).Value = "Süre";
        worksheet.Cell(row, 4).Value = detail.Duration ?? "-";
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

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (!string.IsNullOrEmpty(filter.ProjectType))
        {
            var projectTypeItem = ProjectTypes.GetBySystemName(filter.ProjectType);
            if (projectTypeItem != null)
                query = query.Where(e => e.Assignment.Project.ProjectTypeId == projectTypeItem.Id);
        }

        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        if (string.IsNullOrEmpty(filter.ProjectType) && !filter.ProjectId.HasValue)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.StartDate.HasValue)
        {
            var startDateUtc = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt >= startDateUtc || e.CreatedAt >= startDateUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt <= endDateUtc || e.CreatedAt <= endDateUtc);
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

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (!string.IsNullOrEmpty(filter.ProjectType))
        {
            var projectTypeItem = ProjectTypes.GetBySystemName(filter.ProjectType);
            if (projectTypeItem != null)
                query = query.Where(e => e.Assignment.Project.ProjectTypeId == projectTypeItem.Id);
        }

        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        if (string.IsNullOrEmpty(filter.ProjectType) && !filter.ProjectId.HasValue)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

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
            .Include(a => a.Question)
                .ThenInclude(q => q.Checklist)
            .Where(a => a.AppliedPenaltyTypeId != PenaltyTypes.Ids.None)
            .AsQueryable();

        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        if (!filter.ProjectId.HasValue)
        {
            query = query.Where(a => a.Evaluation.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(a => a.Evaluation.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(a => a.Evaluation.Assignment.Project.CustomerId == filter.CustomerId.Value);

        if (filter.OrganizationId.HasValue)
            query = query.Where(a => a.Evaluation.EvaluatedOrganizationId == filter.OrganizationId.Value);

        if (filter.ChecklistId.HasValue)
            query = query.Where(a => a.Question.ChecklistId == filter.ChecklistId.Value);

        if (filter.EvaluatorId.HasValue)
            query = query.Where(a => a.Evaluation.EvaluatorId == filter.EvaluatorId.Value);

        if (!string.IsNullOrEmpty(filter.PenaltyType))
        {
            var penaltyTypeItem = PenaltyTypes.GetBySystemName(filter.PenaltyType);
            if (penaltyTypeItem != null)
                query = query.Where(a => a.AppliedPenaltyTypeId == penaltyTypeItem.Id);
        }

        if (filter.StartDate.HasValue)
            query = query.Where(a => a.Evaluation.CompletedAt >= filter.StartDate.Value || a.Evaluation.ControlDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(a => a.Evaluation.CompletedAt <= filter.EndDate.Value || a.Evaluation.ControlDate <= filter.EndDate.Value);

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
        var report = await GetPenaltiesReportAsync(filter);

        using var workbook = new XLWorkbook();

        // Summary sheet
        var summarySheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.Summary", defaultValue: "Özet"));
        summarySheet.Cell(1, 1).Value = await _localizationService.GetResourceAsync("Report.TotalPenalties", defaultValue: "Toplam Cezalı");
        summarySheet.Cell(1, 2).Value = report.Summary.TotalPenalties;
        summarySheet.Cell(2, 1).Value = await _localizationService.GetResourceAsync("Report.YellowCard", defaultValue: "Sarı Kart");
        summarySheet.Cell(2, 2).Value = report.Summary.TotalYellowCards;
        summarySheet.Cell(3, 1).Value = await _localizationService.GetResourceAsync("Report.RedCard", defaultValue: "Kırmızı Kart");
        summarySheet.Cell(3, 2).Value = report.Summary.TotalRedCards;
        summarySheet.Cell(4, 1).Value = await _localizationService.GetResourceAsync("Report.AffectedEvaluations", defaultValue: "Etkilenen Değerlendirme");
        summarySheet.Cell(4, 2).Value = report.Summary.AffectedEvaluations;
        summarySheet.Cell(5, 1).Value = await _localizationService.GetResourceAsync("Report.ReportDate", defaultValue: "Rapor Tarihi");
        summarySheet.Cell(5, 2).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        summarySheet.Columns().AdjustToContents();

        // Penalties detail sheet
        var penaltiesSheet = workbook.Worksheets.Add(await _localizationService.GetResourceAsync("Report.PenaltyEvaluations", defaultValue: "Cezalı Değerlendirmeler"));

        // Headers - Değerlendirici kolonu excludeEvaluator true ise eklenmez
        var headersList = new List<string>
        {
            await _localizationService.GetResourceAsync("Report.Date", defaultValue: "Tarih"),
            await _localizationService.GetResourceAsync("Report.Project", defaultValue: "Proje"),
            await _localizationService.GetResourceAsync("Report.Checklist", defaultValue: "Kontrol Listesi"),
            await _localizationService.GetResourceAsync("Report.Section", defaultValue: "Bölüm"),
            await _localizationService.GetResourceAsync("Report.Question", defaultValue: "Soru"),
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

        int row = 2;
        foreach (var penalty in report.Penalties)
        {
            int col = 1;
            penaltiesSheet.Cell(row, col++).Value = penalty.EvaluationDate?.ToString("dd.MM.yyyy") ?? "";
            penaltiesSheet.Cell(row, col++).Value = penalty.ProjectName;
            penaltiesSheet.Cell(row, col++).Value = penalty.ChecklistName ?? "";
            penaltiesSheet.Cell(row, col++).Value = penalty.GroupName;
            penaltiesSheet.Cell(row, col++).Value = penalty.QuestionText;
            penaltiesSheet.Cell(row, col++).Value = penalty.PenaltyType == "YellowCard"
                ? await _localizationService.GetResourceAsync("Report.YellowCard", defaultValue: "Sarı Kart")
                : await _localizationService.GetResourceAsync("Report.RedCard", defaultValue: "Kırmızı Kart");

            if (!excludeEvaluator)
            {
                penaltiesSheet.Cell(row, col++).Value = penalty.EvaluatorName ?? "";
            }

            penaltiesSheet.Cell(row, col++).Value = penalty.EvaluatedPersonnelName ?? "";
            penaltiesSheet.Cell(row, col++).Value = penalty.Notes ?? "";
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
            .Where(e => e.EvaluatedCustomerPersonnelId == filter.PersonnelId && e.StatusId == EvaluationStatuses.Ids.Completed);

        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        if (!filter.ProjectId.HasValue)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(e => e.Assignment.Project.CustomerId == filter.CustomerId.Value);

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
                Title = UserRoles.GetById(user.RoleId)?.SystemName ?? "",
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
                Status = EvaluationStatuses.GetById(e.StatusId)?.SystemName ?? ""
            })
            .ToList();

        // Güçlü ve zayıf yönler (soru bazlı analiz)
        var questionPerformance = allAnswers
            .Where(a => a.Question != null)
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
            Title = UserRoles.GetById(user.RoleId)?.SystemName ?? "",
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
            .Where(a => a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        if (!filter.ProjectId.HasValue)
        {
            query = query.Where(a => a.Evaluation.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(a => a.Evaluation.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(a => a.Evaluation.Assignment.Project.CustomerId == filter.CustomerId.Value);

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
            PenaltyType = a.AppliedPenaltyTypeId != PenaltyTypes.Ids.None ? PenaltyTypes.GetById(a.AppliedPenaltyTypeId)?.SystemName : null
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
                    .ThenInclude(a => a.Project)
            .Include(a => a.Question)
                .ThenInclude(q => q.Checklist)
            .Where(a => !string.IsNullOrEmpty(a.Notes) || !string.IsNullOrEmpty(a.RecommendationNotes))
            .Where(a => a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            .AsQueryable();

        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        if (!filter.ProjectId.HasValue)
        {
            query = query.Where(a => a.Evaluation.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(a => a.Evaluation.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(a => a.Evaluation.Assignment.Project.CustomerId == filter.CustomerId.Value);

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
            worksheet.Cell(row, 7).Value = item.Duration ?? "";
            worksheet.Cell(row, 8).Value = item.Comment ?? "";
            worksheet.Cell(row, 9).Value = period;
            worksheet.Cell(row, 10).Value = item.ScorePercentage ?? 0;

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

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

        // Apply filters (same as GetEvaluationsAsync)
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (!string.IsNullOrEmpty(filter.ProjectType))
        {
            var projectTypeItem = ProjectTypes.GetBySystemName(filter.ProjectType);
            if (projectTypeItem != null)
                query = query.Where(e => e.Assignment.Project.ProjectTypeId == projectTypeItem.Id);
        }

        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        if (string.IsNullOrEmpty(filter.ProjectType) && !filter.ProjectId.HasValue)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorId.HasValue)
            query = query.Where(e => e.EvaluatorId == filter.EvaluatorId.Value);

        if (filter.ChecklistId.HasValue)
            query = query.Where(e => e.Assignment.ChecklistId == filter.ChecklistId.Value);

        if (filter.StartDate.HasValue)
        {
            var startDateUtc = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt >= startDateUtc || e.CreatedAt >= startDateUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt <= endDateUtc || e.CreatedAt <= endDateUtc);
        }

        // Customer filter (evaluated personnel's customer)
        if (filter.CustomerId.HasValue)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null && e.EvaluatedCustomerPersonnel.CustomerId == filter.CustomerId.Value);

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerId.HasValue)
            query = query.Where(e => e.Assignment.Project.CustomerId == filter.ProjectCustomerId.Value);

        // Organization filter
        if (filter.OrganizationId.HasValue)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == filter.OrganizationId.Value));

        // Period filter
        if (filter.PeriodId.HasValue)
            query = query.Where(e => e.AssignmentPeriodId == filter.PeriodId.Value);

        // Evaluation source filter
        if (!string.IsNullOrEmpty(filter.EvaluationSource))
        {
            if (filter.EvaluationSource == "internal")
                query = query.Where(e => e.Assignment.TypeId == AssignmentTypes.Ids.CustomerPersonnel);
            else if (filter.EvaluationSource == "ours")
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
                    ? Math.Round(g.Sum(x => x.EarnedPoints) / g.Sum(x => x.MaxPoints) * 100, 0)
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
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

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

        // Apply filters (same as GetEvaluationsAsync)
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (!string.IsNullOrEmpty(filter.ProjectType))
        {
            var projectTypeItem = ProjectTypes.GetBySystemName(filter.ProjectType);
            if (projectTypeItem != null)
                query = query.Where(e => e.Assignment.Project.ProjectTypeId == projectTypeItem.Id);
        }

        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        if (string.IsNullOrEmpty(filter.ProjectType) && !filter.ProjectId.HasValue)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorId.HasValue)
            query = query.Where(e => e.EvaluatorId == filter.EvaluatorId.Value);

        if (filter.ChecklistId.HasValue)
            query = query.Where(e => e.Assignment.ChecklistId == filter.ChecklistId.Value);

        if (filter.StartDate.HasValue)
        {
            var startDateUtc = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt >= startDateUtc || e.CreatedAt >= startDateUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt <= endDateUtc || e.CreatedAt <= endDateUtc);
        }

        // PRENSIP: Export için sadece Completed değerlendirmeler dahil edilir (taslaklar hariç)
        if (filter.ForExport)
        {
            query = query.Where(e => e.StatusId == EvaluationStatuses.Ids.Completed);
        }
        else if (!string.IsNullOrEmpty(filter.Status))
        {
            var statusItem = EvaluationStatuses.GetBySystemName(filter.Status);
            if (statusItem != null)
                query = query.Where(e => e.StatusId == statusItem.Id);
        }

        // Evaluation source filter
        if (!string.IsNullOrEmpty(filter.EvaluationSource))
        {
            if (filter.EvaluationSource == "internal")
                query = query.Where(e => e.Assignment.TypeId == AssignmentTypes.Ids.CustomerPersonnel);
            else if (filter.EvaluationSource == "ours")
                query = query.Where(e => e.Assignment.TypeId != AssignmentTypes.Ids.CustomerPersonnel);
        }

        // Customer filter (evaluated personnel's customer)
        if (filter.CustomerId.HasValue)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null && e.EvaluatedCustomerPersonnel.CustomerId == filter.CustomerId.Value);

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerId.HasValue)
            query = query.Where(e => e.Assignment.Project.CustomerId == filter.ProjectCustomerId.Value);

        // Organization filter
        if (filter.OrganizationId.HasValue)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == filter.OrganizationId.Value));

        // Period filter
        if (filter.PeriodId.HasValue)
            query = query.Where(e => e.AssignmentPeriodId == filter.PeriodId.Value);

        // Evaluated Personnel name search
        if (!string.IsNullOrEmpty(filter.EvaluatedPersonnelName))
        {
            query = query.Where(e =>
                (e.EvaluatedCustomerPersonnel != null &&
                    (EF.Functions.ILike(e.EvaluatedCustomerPersonnel.FirstName, $"%{filter.EvaluatedPersonnelName}%") ||
                     EF.Functions.ILike(e.EvaluatedCustomerPersonnel.LastName, $"%{filter.EvaluatedPersonnelName}%"))) ||
                (e.EvaluatedUnknownPersonnel != null && EF.Functions.ILike(e.EvaluatedUnknownPersonnel, $"%{filter.EvaluatedPersonnelName}%")));
        }

        // CallId search
        if (!string.IsNullOrEmpty(filter.CallId))
            query = query.Where(e => e.CallId != null && EF.Functions.ILike(e.CallId, $"%{filter.CallId}%"));

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
            worksheet.Cell(row, 14).Value = combinedDescription;

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

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

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (!string.IsNullOrEmpty(filter.ProjectType))
        {
            var projectTypeItem = ProjectTypes.GetBySystemName(filter.ProjectType);
            if (projectTypeItem != null)
                query = query.Where(e => e.Assignment.Project.ProjectTypeId == projectTypeItem.Id);
        }

        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        if (string.IsNullOrEmpty(filter.ProjectType) && !filter.ProjectId.HasValue)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorId.HasValue)
            query = query.Where(e => e.EvaluatorId == filter.EvaluatorId.Value);

        if (filter.ChecklistId.HasValue)
            query = query.Where(e => e.Assignment.ChecklistId == filter.ChecklistId.Value);

        if (filter.StartDate.HasValue)
        {
            var startDateUtc = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt >= startDateUtc || e.CreatedAt >= startDateUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt <= endDateUtc || e.CreatedAt <= endDateUtc);
        }

        // Customer filter (evaluated personnel's customer)
        if (filter.CustomerId.HasValue)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null && e.EvaluatedCustomerPersonnel.CustomerId == filter.CustomerId.Value);

        // Project customer filter (for CustomerPortal - filter by project's customer)
        if (filter.ProjectCustomerId.HasValue)
            query = query.Where(e => e.Assignment.Project.CustomerId == filter.ProjectCustomerId.Value);

        // Organization filter
        if (filter.OrganizationId.HasValue)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null &&
                e.EvaluatedCustomerPersonnel.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == filter.OrganizationId.Value));

        // Period filter
        if (filter.PeriodId.HasValue)
            query = query.Where(e => e.AssignmentPeriodId == filter.PeriodId.Value);

        // Evaluation source filter
        if (!string.IsNullOrEmpty(filter.EvaluationSource))
        {
            if (filter.EvaluationSource == "internal")
                query = query.Where(e => e.Assignment.TypeId == AssignmentTypes.Ids.CustomerPersonnel);
            else if (filter.EvaluationSource == "ours")
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
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

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

        // Apply filters
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (!string.IsNullOrEmpty(filter.ProjectType))
        {
            var projectTypeItem = ProjectTypes.GetBySystemName(filter.ProjectType);
            if (projectTypeItem != null)
                query = query.Where(e => e.Assignment.Project.ProjectTypeId == projectTypeItem.Id);
        }

        // Varsayılan proje tipi filtresi: Çağrı Denetimi
        if (string.IsNullOrEmpty(filter.ProjectType) && !filter.ProjectId.HasValue)
        {
            query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
        }

        if (filter.EvaluatorId.HasValue)
            query = query.Where(e => e.EvaluatorId == filter.EvaluatorId.Value);

        if (filter.StartDate.HasValue)
        {
            var startDateUtc = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt >= startDateUtc || e.CreatedAt >= startDateUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt <= endDateUtc || e.CreatedAt <= endDateUtc);
        }

        if (filter.CustomerId.HasValue)
            query = query.Where(e => e.EvaluatedCustomerPersonnel != null && e.EvaluatedCustomerPersonnel.CustomerId == filter.CustomerId.Value);

        if (filter.OrganizationId.HasValue)
            query = query.Where(e => e.EvaluatedOrganizationId == filter.OrganizationId.Value);

        if (filter.PeriodId.HasValue)
            query = query.Where(e => e.AssignmentPeriodId == filter.PeriodId.Value);

        if (!string.IsNullOrEmpty(filter.EvaluationSource))
        {
            if (filter.EvaluationSource == "internal")
                query = query.Where(e => e.Assignment.TypeId == AssignmentTypes.Ids.CustomerPersonnel);
            else if (filter.EvaluationSource == "ours")
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

        // Katılımcılar
        var respondents = evaluations
            .Select(e => new SurveyRespondentDto
            {
                PersonnelId = e.EvaluatedCustomerPersonnelId ?? 0,
                EvaluationId = e.Id,
                FullName = e.EvaluatedCustomerPersonnel != null
                    ? $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}".Trim()
                    : null,
                Email = e.EvaluatedCustomerPersonnel?.Email,
                OrganizationName = e.EvaluatedCustomerPersonnel?.OrganizationAssignments
                    .FirstOrDefault()?.CustomerOrganization?.Name,
                Score = e.ScorePercentage,
                CompletedAt = e.CompletedAt
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

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ExcelExportDto
        {
            FileName = $"Anket_Sonuclari_{results.ProjectName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // ===== PERFORMANS TAKİBİ =====

    public async Task<PerformanceTrackingResultDto> GetPerformanceTrackingAsync()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek + (int)DayOfWeek.Monday);
        if (todayStart.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = new PerformanceTrackingResultDto();

        // 1. Değerlendirici (Evaluator) Performansları
        var evaluatorStats = await _context.Evaluations
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed && e.EvaluatorId != null)
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

        // 3. Genel Özet
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
}
