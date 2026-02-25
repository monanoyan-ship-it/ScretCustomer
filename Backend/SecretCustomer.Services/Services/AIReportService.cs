using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.DTOs.AI;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

/// <summary>
/// Google Gemini API ile rapor oluşturma servisi
/// </summary>
public class AIReportService : IAIReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AIReportService> _logger;

    private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    public AIReportService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AIReportService> logger)
    {
        _context = context;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AIReportResponseDto> GenerateReportAsync(AIReportRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Veri topla
            var reportData = await CollectReportDataAsync(request);

            // 2. Prompt oluştur
            var prompt = BuildPrompt(reportData, request);

            // 3. Gemini API'ye gönder
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return new AIReportResponseDto
                {
                    Success = false,
                    ErrorMessage = "Gemini API key yapılandırılmamış.",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            var response = await CallGeminiApiAsync(prompt, apiKey);

            stopwatch.Stop();
            response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI rapor oluşturma hatası: CustomerId={CustomerId}", request.CustomerId);
            stopwatch.Stop();

            return new AIReportResponseDto
            {
                Success = false,
                ErrorMessage = $"Rapor oluşturma hatası: {ex.Message}",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<AIReportDataDto> CollectReportDataAsync(AIReportRequestDto request)
    {
        var currentYear = request.Year;
        var compareYear = request.CompareYear;

        // Müşteri bilgisi
        var customer = await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == request.CustomerId)
            .Select(c => new AICustomerInfoDto
            {
                Id = c.Id,
                Name = c.CompanyName,
                Code = c.Code
            })
            .FirstOrDefaultAsync();

        if (customer == null)
            throw new ArgumentException($"Müşteri bulunamadı: {request.CustomerId}");

        // Müşterinin tüm proje ID'leri
        var projectIds = await _context.Projects
            .AsNoTracking()
            .Where(p => p.CustomerId == request.CustomerId && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync();

        if (!projectIds.Any())
            throw new ArgumentException("Müşteriye ait proje bulunamadı.");

        // Mevcut yıl değerlendirmeleri
        var currentYearStart = DateTime.SpecifyKind(new DateTime(currentYear, 1, 1), DateTimeKind.Utc);
        var currentYearEnd = DateTime.SpecifyKind(new DateTime(currentYear, 12, 31, 23, 59, 59), DateTimeKind.Utc);

        var currentYearEvaluations = await GetEvaluationsAsync(projectIds, currentYearStart, currentYearEnd);

        // Karşılaştırma yılı değerlendirmeleri (varsa)
        List<EvaluationData> compareYearEvaluations = new();
        if (compareYear.HasValue)
        {
            var compareYearStart = DateTime.SpecifyKind(new DateTime(compareYear.Value, 1, 1), DateTimeKind.Utc);
            var compareYearEnd = DateTime.SpecifyKind(new DateTime(compareYear.Value, 12, 31, 23, 59, 59), DateTimeKind.Utc);
            compareYearEvaluations = await GetEvaluationsAsync(projectIds, compareYearStart, compareYearEnd);
        }

        // Soru bazlı ortalamalar
        var questionAverages = await GetQuestionAveragesAsync(projectIds, currentYearStart, currentYearEnd);

        // Özet
        var summary = new AIExecutiveSummaryDto
        {
            CurrentYear = currentYear,
            CompareYear = compareYear,
            CurrentYearAverage = currentYearEvaluations.Any()
                ? Math.Round(currentYearEvaluations.Average(e => e.ScorePercentage), 2)
                : 0,
            CompareYearAverage = compareYearEvaluations.Any()
                ? Math.Round(compareYearEvaluations.Average(e => e.ScorePercentage), 2)
                : null,
            TotalEvaluations = currentYearEvaluations.Count
        };

        // Proje bazlı özet
        summary.ProjectSummaries = currentYearEvaluations
            .GroupBy(e => e.ProjectName)
            .Select(g => new AIProjectYearlySummaryDto
            {
                ProjectName = g.Key,
                ChannelType = DetermineChannelType(g.Key),
                TotalEvaluations = g.Count(),
                CurrentYearAverage = Math.Round(g.Average(e => e.ScorePercentage), 2)
            })
            .OrderByDescending(p => p.TotalEvaluations)
            .ToList();

        // Aylık puanlar
        var monthlyScores = BuildMonthlyScores(currentYearEvaluations);

        // Top 5 personel
        var topPersonnel = BuildTopPersonnel(currentYearEvaluations, 5);

        // Bottom 3 personel
        var bottomPersonnel = BuildBottomPersonnel(currentYearEvaluations, 3);

        return new AIReportDataDto
        {
            Customer = customer,
            TargetScores = request.TargetScores,
            ExecutiveSummary = summary,
            InboundProjects = new List<AIProjectMonthlyDto>
            {
                new AIProjectMonthlyDto
                {
                    ProjectName = "Tüm Projeler",
                    TotalEvaluations = currentYearEvaluations.Count,
                    CurrentYearAverage = summary.CurrentYearAverage,
                    PreviousYearAverage = summary.CompareYearAverage,
                    MonthlyScores = monthlyScores
                }
            },
            OutboundProjects = new List<AIProjectMonthlyDto>(),
            CriteriaComparison = questionAverages.Select(q => new AICriteriaComparisonDto
            {
                CriteriaName = q.QuestionText,
                InboundScore = q.AverageScore, // Genel ortalama olarak kullan
                OutboundScore = 0
            }).ToList(),
            PersonnelRanking = topPersonnel.Concat(bottomPersonnel).ToList(),
            PersonnelMonthlyScores = new List<AIPersonnelMonthlyDto>()
        };
    }

    private async Task<List<EvaluationData>> GetEvaluationsAsync(List<int> projectIds, DateTime startDate, DateTime endDate)
    {
        return await _context.Evaluations
            .AsNoTracking()
            .Include(e => e.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Where(e => projectIds.Contains(e.ProjectId))
            .Where(e => e.StatusId == EvaluationStatuses.Ids.Completed)
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .Select(e => new EvaluationData
            {
                EvaluationId = e.Id,
                ProjectName = e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name,
                PersonnelId = e.EvaluatedCustomerPersonnelId,
                PersonnelName = e.EvaluatedCustomerPersonnel != null ? e.EvaluatedCustomerPersonnel.FullName : null,
                CreatedAt = e.CreatedAt,
                ScorePercentage = e.ScorePercentage ?? 0
            })
            .ToListAsync();
    }

    private async Task<List<QuestionAverage>> GetQuestionAveragesAsync(List<int> projectIds, DateTime startDate, DateTime endDate)
    {
        // Soru bazlı ortalamalar - aggregate edilmiş
        var answers = await _context.Answers
            .AsNoTracking()
            .Include(a => a.Question)
            .Include(a => a.Evaluation)
            .Where(a => projectIds.Contains(a.Evaluation.ProjectId))
            .Where(a => a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            .Where(a => a.Evaluation.CreatedAt >= startDate && a.Evaluation.CreatedAt <= endDate)
            .Where(a => a.Question != null && a.EarnedPoints.HasValue && a.Question.WeightPoints > 0)
            .GroupBy(a => a.Question!.Text)
            .Select(g => new QuestionAverage
            {
                QuestionText = g.Key,
                AverageScore = Math.Round(g.Average(a => (a.EarnedPoints!.Value / a.Question!.WeightPoints) * 100), 2),
                AnswerCount = g.Count()
            })
            .Where(q => q.AnswerCount >= 10) // En az 10 cevap
            .OrderBy(q => q.AverageScore) // En düşükten en yükseğe (gelişim alanları önce)
            .Take(15) // En önemli 15 soru
            .ToListAsync();

        return answers;
    }

    private Dictionary<int, decimal?> BuildMonthlyScores(List<EvaluationData> evaluations)
    {
        var monthlyScores = new Dictionary<int, decimal?>();
        for (int month = 1; month <= 12; month++)
        {
            var monthEvals = evaluations.Where(e => e.CreatedAt.Month == month).ToList();
            if (monthEvals.Any())
            {
                monthlyScores[month] = Math.Round(monthEvals.Average(e => e.ScorePercentage), 2);
            }
        }
        return monthlyScores;
    }

    private List<AIPersonnelRankingDto> BuildTopPersonnel(List<EvaluationData> evaluations, int count)
    {
        return evaluations
            .Where(e => e.PersonnelId.HasValue && !string.IsNullOrEmpty(e.PersonnelName))
            .GroupBy(e => new { e.PersonnelId, e.PersonnelName })
            .Where(g => g.Count() >= 3)
            .Select(g => new AIPersonnelRankingDto
            {
                PersonnelName = g.Key.PersonnelName!,
                AverageScore = Math.Round(g.Average(e => e.ScorePercentage), 2),
                EvaluationCount = g.Count()
            })
            .OrderByDescending(p => p.AverageScore)
            .Take(count)
            .ToList();
    }

    private List<AIPersonnelRankingDto> BuildBottomPersonnel(List<EvaluationData> evaluations, int count)
    {
        return evaluations
            .Where(e => e.PersonnelId.HasValue && !string.IsNullOrEmpty(e.PersonnelName))
            .GroupBy(e => new { e.PersonnelId, e.PersonnelName })
            .Where(g => g.Count() >= 3)
            .Select(g => new AIPersonnelRankingDto
            {
                PersonnelName = g.Key.PersonnelName!,
                AverageScore = Math.Round(g.Average(e => e.ScorePercentage), 2),
                EvaluationCount = g.Count()
            })
            .OrderBy(p => p.AverageScore)
            .Take(count)
            .ToList();
    }

    private static string DetermineChannelType(string projectName)
    {
        var nameLower = projectName.ToLowerInvariant();
        if (nameLower.Contains("inbound")) return "Inbound";
        if (nameLower.Contains("outbound")) return "Outbound";
        return "Genel";
    }

    private static string BuildPrompt(AIReportDataDto data, AIReportRequestDto request)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Çağrı merkezi performans analisti olarak görev yapıyorsun.");
        sb.AppendLine("Aşağıdaki verileri analiz ederek KISA ve ÖZ bir performans raporu oluştur.");
        sb.AppendLine();

        // Temel bilgiler
        sb.AppendLine($"# {data.Customer.Name} - {request.Year} Performans Raporu");
        sb.AppendLine();

        // Hedefler
        sb.AppendLine("## Hedefler");
        sb.AppendLine($"- Yıl Hedefi: %{request.TargetScores.CurrentYearTarget}");
        sb.AppendLine($"- Üst Hedef: %{request.TargetScores.StretchTarget}");
        sb.AppendLine();

        // Özet
        sb.AppendLine("## Genel Durum");
        sb.AppendLine($"- Toplam Değerlendirme: {data.ExecutiveSummary.TotalEvaluations}");
        sb.AppendLine($"- {request.Year} Ortalaması: %{data.ExecutiveSummary.CurrentYearAverage}");
        if (data.ExecutiveSummary.CompareYearAverage.HasValue)
            sb.AppendLine($"- {request.CompareYear} Ortalaması: %{data.ExecutiveSummary.CompareYearAverage}");
        sb.AppendLine();

        // Proje bazlı
        if (data.ExecutiveSummary.ProjectSummaries.Any())
        {
            sb.AppendLine("## Proje Bazlı");
            foreach (var p in data.ExecutiveSummary.ProjectSummaries.Take(5))
            {
                sb.AppendLine($"- {p.ProjectName}: %{p.CurrentYearAverage} ({p.TotalEvaluations} değ.)");
            }
            sb.AppendLine();
        }

        // Aylık
        var project = data.InboundProjects.FirstOrDefault();
        if (project != null && project.MonthlyScores.Any())
        {
            sb.AppendLine("## Aylık Trend");
            var monthNames = new[] { "", "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };
            foreach (var ms in project.MonthlyScores.OrderBy(m => m.Key))
            {
                sb.AppendLine($"- {monthNames[ms.Key]}: %{ms.Value}");
            }
            sb.AppendLine();
        }

        // Soru bazlı (gelişim alanları)
        if (data.CriteriaComparison.Any())
        {
            sb.AppendLine("## Kriter/Soru Ortalamaları (Gelişim Alanları)");
            foreach (var q in data.CriteriaComparison.Take(10))
            {
                sb.AppendLine($"- {q.CriteriaName}: %{q.InboundScore}");
            }
            sb.AppendLine();
        }

        // Personel
        if (data.PersonnelRanking.Any())
        {
            var top = data.PersonnelRanking.OrderByDescending(p => p.AverageScore).Take(5).ToList();
            var bottom = data.PersonnelRanking.OrderBy(p => p.AverageScore).Take(3).ToList();

            sb.AppendLine("## En İyi Personel");
            foreach (var p in top)
                sb.AppendLine($"- {p.PersonnelName}: %{p.AverageScore}");
            sb.AppendLine();

            sb.AppendLine("## Gelişim Gerektiren Personel");
            foreach (var p in bottom)
                sb.AppendLine($"- {p.PersonnelName}: %{p.AverageScore}");
            sb.AppendLine();
        }

        // Talimatlar
        sb.AppendLine("---");
        sb.AppendLine("Markdown formatında rapor yaz. Bölümler:");
        sb.AppendLine("1. Özet (3 cümle)");
        sb.AppendLine("2. Hedef Değerlendirmesi");
        sb.AppendLine("3. Güçlü ve Zayıf Yönler");
        sb.AppendLine("4. Öneriler (3-5 madde)");
        sb.AppendLine();
        sb.AppendLine("KISA TUT, gereksiz detay verme.");

        return sb.ToString();
    }

    private async Task<AIReportResponseDto> CallGeminiApiAsync(string prompt, string apiKey)
    {
        var client = _httpClientFactory.CreateClient();

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 2048
            }
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var url = $"{GEMINI_API_URL}?key={apiKey}";
        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Gemini API hatası: {StatusCode} - {Error}", response.StatusCode, errorContent);

            return new AIReportResponseDto
            {
                Success = false,
                ErrorMessage = $"Gemini API hatası: {response.StatusCode}"
            };
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        var reportContent = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        int? tokensUsed = null;
        if (doc.RootElement.TryGetProperty("usageMetadata", out var usageMetadata))
        {
            if (usageMetadata.TryGetProperty("totalTokenCount", out var tokenCount))
            {
                tokensUsed = tokenCount.GetInt32();
            }
        }

        return new AIReportResponseDto
        {
            Success = true,
            ReportContent = reportContent,
            TokensUsed = tokensUsed
        };
    }

    // İç sınıflar
    private class EvaluationData
    {
        public int EvaluationId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int? PersonnelId { get; set; }
        public string? PersonnelName { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal ScorePercentage { get; set; }
    }

    private class QuestionAverage
    {
        public string QuestionText { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public int AnswerCount { get; set; }
    }
}
