using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

/// <summary>
/// Kural bazlı değerlendirme bildirim servisi.
/// Her müşterinin N tane CustomerNotificationRule kaydı olabilir.
/// FrequencyId=1 → anında, 2=günlük, 3=haftalık, 4=aylık
/// </summary>
public class EvaluationNotificationService : IEvaluationNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationTokenService _tokenService;
    private readonly IAuditLogService _auditLog;
    private readonly IConfiguration _configuration;

    private const string LogCategory = "EvaluationNotification";

    private static readonly TimeZoneInfo TurkeyTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

    public EvaluationNotificationService(
        ApplicationDbContext context,
        IEmailService emailService,
        INotificationTokenService tokenService,
        IAuditLogService auditLogService,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _tokenService = tokenService;
        _auditLog = auditLogService;
        _configuration = configuration;
    }

    // ───────────────────────────────────────────────
    // 1) TEKİL BİLDİRİM (FrequencyId = 1, Her Kayıtta)
    // ───────────────────────────────────────────────

    public async Task SendSingleEvaluationNotificationAsync(Evaluation evaluation, string baseUrl)
    {
        // baseUrl boşsa config'ten al (fallback)
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = await GetBaseUrlAsync();

        try
        {
            // Müşteri ID'sini bul
            var customerId = evaluation.Project?.CustomerId;
            if (customerId == null)
            {
                var ev = await _context.Evaluations
                    .Include(e => e.Project)
                    .Include(e => e.EvaluatedCustomerPersonnel)
                    .Include(e => e.EvaluatedOrganization)
                    .Include(e => e.CustomerDealer)
                    .FirstOrDefaultAsync(e => e.Id == evaluation.Id);

                if (ev?.Project?.CustomerId == null)
                {
                    await _auditLog.LogWarningAsync($"Değerlendirme #{evaluation.Id} için müşteri bulunamadı", LogCategory);
                    return;
                }

                evaluation = ev;
                customerId = ev.Project!.CustomerId;
            }

            // FrequencyId=1, aktif kuralları bul
            var rules = await _context.CustomerNotificationRules
                .Include(r => r.EmailTemplate)
                .Where(r => r.CustomerId == customerId
                         && r.FrequencyId == EvaluationNotificationFrequencies.Ids.PerEvaluation
                         && r.IsActive
                         && !r.IsDeleted)
                .ToListAsync();

            if (!rules.Any())
            {
                return;
            }

            // Personel adı
            var personnelName = evaluation.EvaluatedCustomerPersonnel != null
                ? $"{evaluation.EvaluatedCustomerPersonnel.FirstName} {evaluation.EvaluatedCustomerPersonnel.LastName}"
                : evaluation.EvaluatedUnknownPersonnel ?? "-";

            // Müşteri adı
            var customer = await _context.Customers.FindAsync(customerId);
            var companyName = customer?.CompanyName ?? "-";

            foreach (var rule in rules)
            {
                try
                {
                    var template = await GetTemplateAsync(rule.EmailTemplateId);
                    if (template == null)
                    {
                        await _auditLog.LogWarningAsync($"Kural #{rule.Id} için email şablonu bulunamadı (templateId={rule.EmailTemplateId})", LogCategory);
                        continue;
                    }

                    // Kural bazında token oluştur
                    var token = _tokenService.GenerateSingleToken(evaluation.Id, DateTime.UtcNow.AddDays(rule.TokenExpirationDays));
                    var reportLink = $"{baseUrl}/report/view/{token}";
                    var linkHtml = $"<a href=\"{reportLink}\" style=\"color: #007bff; text-decoration: underline;\">Değerlendirme Raporu</a>";

                    // Alıcı listesini belirle
                    var recipients = new List<string>();

                    if (!string.IsNullOrWhiteSpace(rule.Emails))
                    {
                        recipients.AddRange(
                            rule.Emails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Where(e => !string.IsNullOrWhiteSpace(e)));
                    }

                    if (rule.SendToPersonnel && evaluation.EvaluatedCustomerPersonnel != null
                        && !string.IsNullOrWhiteSpace(evaluation.EvaluatedCustomerPersonnel.Email))
                    {
                        var personnelEmail = evaluation.EvaluatedCustomerPersonnel.Email.Trim();
                        if (!recipients.Contains(personnelEmail, StringComparer.OrdinalIgnoreCase))
                            recipients.Add(personnelEmail);
                    }

                    if (!recipients.Any())
                    {
                        await _auditLog.LogWarningAsync($"Kural #{rule.Id} için alıcı bulunamadı (Emails boş, SendToPersonnel={rule.SendToPersonnel})", LogCategory);
                        continue;
                    }

                    // Placeholder'ları değiştir
                    var subject = ReplacePlaceholders(template.Subject, companyName, evaluation, personnelName, null, null, 1, null, linkHtml, reportLink);
                    var body = ReplacePlaceholders(template.Body, companyName, evaluation, personnelName, null, null, 1, null, linkHtml, reportLink);

                    // Her alıcıya gönder
                    foreach (var email in recipients)
                    {
                        var result = await _emailService.SendEmailAsync(email, subject, body);

                        var log = new CustomerPersonnelNotificationLog
                        {
                            CustomerPersonnelId = evaluation.EvaluatedCustomerPersonnelId ?? 0,
                            CustomerId = customerId.Value,
                            SentAt = DateTime.UtcNow,
                            NotificationType = EvaluationNotificationTypes.PerEvaluation,
                            Email = email,
                            Subject = subject,
                            EvaluationIdsJson = System.Text.Json.JsonSerializer.Serialize(new[] { evaluation.Id }),
                            EvaluationCount = 1,
                            IsSuccess = result.Success,
                            ErrorMessage = result.Success ? null : result.ErrorMessage
                        };
                        _context.CustomerPersonnelNotificationLogs.Add(log);

                        if (!result.Success)
                            await _auditLog.LogErrorAsync($"Değerlendirme #{evaluation.Id} bildirimi gönderilemedi → {email}: {result.ErrorMessage}", LogCategory);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    await _auditLog.LogErrorAsync($"Kural #{rule.Id} işlenirken hata oluştu", LogCategory, ex);
                }
            }
        }
        catch (Exception ex)
        {
            await _auditLog.LogErrorAsync($"Değerlendirme #{evaluation.Id} bildirimi genel hata", LogCategory, ex);
        }
    }

    // ───────────────────────────────────────────────
    // 2) ZAMANLI BİLDİRİMLER (Günlük/Haftalık/Aylık)
    // ───────────────────────────────────────────────

    public async Task ProcessScheduledNotificationsAsync()
    {
        var now = DateTime.UtcNow;
        var turkeyNow = TimeZoneInfo.ConvertTimeFromUtc(now, TurkeyTimeZone);
        var turkeyToday = turkeyNow.Date;

        var todayDayOfWeek = (int)turkeyNow.DayOfWeek;
        var todayDayOfMonth = turkeyNow.Day;

        var rules = await _context.CustomerNotificationRules
            .Include(r => r.Customer)
            .Include(r => r.EmailTemplate)
            .Where(r => r.IsActive
                     && !r.IsDeleted
                     && r.FrequencyId >= EvaluationNotificationFrequencies.Ids.Daily
                     && r.FrequencyId <= EvaluationNotificationFrequencies.Ids.Monthly
                     && r.Customer.IsActive && !r.Customer.IsDeleted)
            .ToListAsync();

        foreach (var rule in rules)
        {
            try
            {
                if (!ShouldFireToday(rule, todayDayOfWeek, todayDayOfMonth))
                    continue;

                var (periodStart, periodEnd) = CalculatePeriod(rule, turkeyToday);
                var periodStartUtc = TimeZoneInfo.ConvertTimeToUtc(periodStart, TurkeyTimeZone);
                var periodEndUtc = TimeZoneInfo.ConvertTimeToUtc(periodEnd, TurkeyTimeZone);

                var recipients = new List<string>();
                if (!string.IsNullOrWhiteSpace(rule.Emails))
                {
                    recipients.AddRange(
                        rule.Emails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Where(e => !string.IsNullOrWhiteSpace(e)));
                }

                if (!recipients.Any())
                {
                    continue;
                }

                var evaluations = await _context.Evaluations
                    .Include(e => e.Project)
                    .Include(e => e.EvaluatedCustomerPersonnel)
                    .Include(e => e.EvaluatedOrganization)
                    .Include(e => e.CustomerDealer)
                    .Where(e => !e.IsDeleted
                        && e.Project.CustomerId == rule.CustomerId
                        && e.StatusId == EvaluationStatuses.Ids.Completed
                        && e.CompletedAt >= periodStartUtc
                        && e.CompletedAt < periodEndUtc)
                    .OrderByDescending(e => e.CompletedAt)
                    .ToListAsync();

                if (!evaluations.Any())
                    continue;

                var token = _tokenService.GenerateBulkToken(
                    rule.CustomerId,
                    periodStartUtc,
                    periodEndUtc,
                    DateTime.UtcNow.AddDays(rule.TokenExpirationDays));
                var reportLink = $"{await GetBaseUrlAsync()}/report/view/{token}";
                var linkHtml = $"<a href=\"{reportLink}\" style=\"color: #007bff; text-decoration: underline;\">Değerlendirme Raporu</a>";

                var summaryTable = BuildSummaryTable(evaluations);

                var template = await GetTemplateAsync(rule.EmailTemplateId);
                if (template == null)
                {
                    await _auditLog.LogWarningAsync($"Zamanlı kural #{rule.Id} için email şablonu bulunamadı", LogCategory);
                    continue;
                }

                var periodName = GetPeriodName(rule.FrequencyId);
                var companyName = rule.Customer.CompanyName;

                var subject = ReplacePlaceholders(template.Subject, companyName, null, null, periodStart, periodEnd, evaluations.Count, summaryTable, linkHtml, reportLink);
                var body = ReplacePlaceholders(template.Body, companyName, null, null, periodStart, periodEnd, evaluations.Count, summaryTable, linkHtml, reportLink);

                foreach (var email in recipients)
                {
                    var result = await _emailService.SendEmailAsync(email, subject, body);

                    var log = new CustomerPersonnelNotificationLog
                    {
                        CustomerPersonnelId = 0,
                        CustomerId = rule.CustomerId,
                        SentAt = DateTime.UtcNow,
                        NotificationType = GetNotificationType(rule.FrequencyId),
                        PeriodStart = periodStartUtc,
                        PeriodEnd = periodEndUtc,
                        Email = email,
                        Subject = subject,
                        EvaluationIdsJson = System.Text.Json.JsonSerializer.Serialize(evaluations.Select(e => e.Id).ToArray()),
                        EvaluationCount = evaluations.Count,
                        IsSuccess = result.Success,
                        ErrorMessage = result.Success ? null : result.ErrorMessage
                    };
                    _context.CustomerPersonnelNotificationLogs.Add(log);

                    if (!result.Success)
                        await _auditLog.LogErrorAsync($"{periodName} bildirim gönderilemedi → {email}: {result.ErrorMessage}", LogCategory);
                }

                rule.LastSentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _auditLog.LogErrorAsync($"Zamanlı kural #{rule.Id} işlenirken hata", LogCategory, ex);
            }
        }

    }

    // ───────────────────────────────────────────────
    // PRIVATE HELPERS
    // ───────────────────────────────────────────────

    private bool ShouldFireToday(CustomerNotificationRule rule, int todayDayOfWeek, int todayDayOfMonth)
    {
        return rule.FrequencyId switch
        {
            EvaluationNotificationFrequencies.Ids.Daily => true,
            EvaluationNotificationFrequencies.Ids.Weekly => rule.DayOfWeek == todayDayOfWeek,
            EvaluationNotificationFrequencies.Ids.Monthly => rule.DayOfMonth == todayDayOfMonth,
            _ => false
        };
    }

    private (DateTime start, DateTime end) CalculatePeriod(CustomerNotificationRule rule, DateTime turkeyToday)
    {
        var periodEnd = turkeyToday.AddDays(1);

        return rule.FrequencyId switch
        {
            EvaluationNotificationFrequencies.Ids.Daily => (turkeyToday, periodEnd),
            EvaluationNotificationFrequencies.Ids.Weekly => (turkeyToday.AddDays(-7), periodEnd),
            EvaluationNotificationFrequencies.Ids.Monthly => (turkeyToday.AddMonths(-1), periodEnd),
            _ => (turkeyToday, periodEnd)
        };
    }

    private async Task<EmailTemplate?> GetTemplateAsync(int? emailTemplateId)
    {
        EmailTemplate? template = null;

        if (emailTemplateId.HasValue)
        {
            template = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.Id == emailTemplateId.Value && !t.IsDeleted);
        }

        template ??= await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.TemplateTypeId == EmailTemplateTypes.Ids.EvaluationNotification
                                   && t.IsDefault && !t.IsDeleted);

        return template;
    }

    private async Task<string> GetBaseUrlAsync()
    {
        // Önce DB'den oku (dashboard açıldığında otomatik set edilir)
        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == SystemSettingKeys.AppUrl);

        if (!string.IsNullOrWhiteSpace(setting?.Value))
            return setting.Value.TrimEnd('/');

        // Fallback: config'den oku
        return _configuration["AppUrl"]?.TrimEnd('/') ?? "";
    }

    private static string GetPeriodName(int frequencyId) => frequencyId switch
    {
        EvaluationNotificationFrequencies.Ids.Daily => "Günlük",
        EvaluationNotificationFrequencies.Ids.Weekly => "Haftalık",
        EvaluationNotificationFrequencies.Ids.Monthly => "Aylık",
        _ => "Bilinmeyen"
    };

    private static string GetNotificationType(int frequencyId) => frequencyId switch
    {
        EvaluationNotificationFrequencies.Ids.Daily => EvaluationNotificationTypes.Daily,
        EvaluationNotificationFrequencies.Ids.Weekly => EvaluationNotificationTypes.Weekly,
        EvaluationNotificationFrequencies.Ids.Monthly => EvaluationNotificationTypes.Monthly,
        _ => "Unknown"
    };

    private string BuildSummaryTable(List<Evaluation> evaluations)
    {
        var avgScore = evaluations.Where(e => e.ScorePercentage.HasValue).Average(e => (double?)e.ScorePercentage) ?? 0;

        var html = $@"
<table style='border-collapse: collapse; width: 100%; font-family: Arial, sans-serif;'>
    <thead>
        <tr style='background-color: #f8f9fa;'>
            <th style='border: 1px solid #dee2e6; padding: 8px; text-align: left;'>Tarih</th>
            <th style='border: 1px solid #dee2e6; padding: 8px; text-align: left;'>Proje</th>
            <th style='border: 1px solid #dee2e6; padding: 8px; text-align: left;'>Personel</th>
            <th style='border: 1px solid #dee2e6; padding: 8px; text-align: left;'>Organizasyon</th>
            <th style='border: 1px solid #dee2e6; padding: 8px; text-align: center;'>Puan</th>
        </tr>
    </thead>
    <tbody>";

        foreach (var eval in evaluations.Take(50))
        {
            var personnelName = eval.EvaluatedCustomerPersonnel != null
                ? $"{eval.EvaluatedCustomerPersonnel.FirstName} {eval.EvaluatedCustomerPersonnel.LastName}"
                : eval.EvaluatedUnknownPersonnel ?? "-";

            var scoreColor = eval.ScorePercentage >= 80 ? "#28a745" :
                            eval.ScorePercentage >= 60 ? "#ffc107" : "#dc3545";

            html += $@"
        <tr>
            <td style='border: 1px solid #dee2e6; padding: 8px;'>{eval.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-"}</td>
            <td style='border: 1px solid #dee2e6; padding: 8px;'>{eval.Project?.Name ?? "-"}</td>
            <td style='border: 1px solid #dee2e6; padding: 8px;'>{personnelName}</td>
            <td style='border: 1px solid #dee2e6; padding: 8px;'>{eval.EvaluatedOrganization?.Name ?? "-"}</td>
            <td style='border: 1px solid #dee2e6; padding: 8px; text-align: center; color: {scoreColor}; font-weight: bold;'>{eval.ScorePercentage?.ToString("F2") ?? "-"}%</td>
        </tr>";
        }

        html += $@"
    </tbody>
    <tfoot>
        <tr style='background-color: #e9ecef; font-weight: bold;'>
            <td colspan='4' style='border: 1px solid #dee2e6; padding: 8px; text-align: right;'>Toplam: {evaluations.Count} değerlendirme | Ortalama:</td>
            <td style='border: 1px solid #dee2e6; padding: 8px; text-align: center;'>{avgScore:F2}%</td>
        </tr>
    </tfoot>
</table>";

        if (evaluations.Count > 50)
        {
            html += $"<p style='color: #6c757d; font-size: 12px;'>* Tabloda ilk 50 kayıt gösterilmektedir. Toplam {evaluations.Count} değerlendirme yapılmıştır.</p>";
        }

        return html;
    }

    private string ReplacePlaceholders(
        string content,
        string companyName,
        Evaluation? evaluation,
        string? evaluatedPersonnel,
        DateTime? periodStart,
        DateTime? periodEnd,
        int evaluationCount,
        string? summaryTable,
        string? evaluationLinkHtml,
        string? evaluationUrl)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        content = content.Replace(EmailPlaceholders.CompanyName, companyName);

        if (evaluation != null)
        {
            content = content.Replace(EmailPlaceholders.EvaluationScore, evaluation.TotalScore?.ToString("F2") ?? "-");
            content = content.Replace(EmailPlaceholders.EvaluationScorePercentage, evaluation.ScorePercentage?.ToString("F2") ?? "-");
            content = content.Replace(EmailPlaceholders.EvaluatedPersonnel, evaluatedPersonnel ?? "-");
            content = content.Replace(EmailPlaceholders.EvaluationDate, evaluation.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-");
            content = content.Replace(EmailPlaceholders.ProjectName, evaluation.Project?.Name ?? "-");
            content = content.Replace(EmailPlaceholders.OrganizationName, evaluation.EvaluatedOrganization?.Name ?? "-");
        }

        content = content.Replace(EmailPlaceholders.EvaluationCount, evaluationCount.ToString());
        content = content.Replace(EmailPlaceholders.PeriodStartDate, periodStart?.ToString("dd.MM.yyyy") ?? "-");
        content = content.Replace(EmailPlaceholders.PeriodEndDate, periodEnd?.AddDays(-1).ToString("dd.MM.yyyy") ?? "-");
        content = content.Replace(EmailPlaceholders.EvaluationSummaryTable, summaryTable ?? "");
        content = content.Replace(EmailPlaceholders.EvaluationLink, evaluationLinkHtml ?? "");
        content = content.Replace(EmailPlaceholders.EvaluationUrl, evaluationUrl ?? "");

        content = content.Replace(EmailPlaceholders.CurrentDate, DateTime.Now.ToString("dd.MM.yyyy"));
        content = content.Replace(EmailPlaceholders.CurrentYear, DateTime.Now.Year.ToString());
        content = content.Replace(EmailPlaceholders.SystemName, "Secret Customer");

        return content;
    }
}
