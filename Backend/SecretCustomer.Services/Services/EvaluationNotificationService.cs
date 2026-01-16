using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

/// <summary>
/// Değerlendirme bildirim servisi implementasyonu
/// </summary>
public class EvaluationNotificationService : IEvaluationNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<EvaluationNotificationService> _logger;

    public EvaluationNotificationService(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<EvaluationNotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Tekil değerlendirme bildirimi gönderir (Her Kayıtta seçeneği için)
    /// </summary>
    public async Task SendSingleEvaluationNotificationAsync(Evaluation evaluation)
    {
        try
        {
            // Müşteri bilgilerini al
            var customer = await _context.Customers
                .Include(c => c.EvaluationNotificationTemplate)
                .FirstOrDefaultAsync(c => c.Id == evaluation.Assignment!.Project!.CustomerId);

            if (customer == null)
            {
                _logger.LogWarning("Customer not found for evaluation {EvaluationId}", evaluation.Id);
                return;
            }

            // Bildirim ayarları kontrol
            if (customer.EvaluationNotificationFrequencyId != EvaluationNotificationFrequencies.Ids.PerEvaluation)
                return;

            if (string.IsNullOrWhiteSpace(customer.NotificationEmails))
            {
                _logger.LogWarning("No notification emails configured for customer {CustomerId}", customer.Id);
                return;
            }

            // Email şablonunu al
            var template = customer.EvaluationNotificationTemplate;
            if (template == null)
            {
                // Varsayılan şablon kullan
                template = await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.TemplateTypeId == EmailTemplateTypes.Ids.EvaluationNotification &&
                                              t.IsDefault && !t.IsDeleted);
            }

            if (template == null)
            {
                _logger.LogWarning("No email template found for evaluation notification");
                return;
            }

            // Değerlendirilen personel adı
            var evaluatedPersonnel = evaluation.EvaluatedCustomerPersonnel != null
                ? $"{evaluation.EvaluatedCustomerPersonnel.FirstName} {evaluation.EvaluatedCustomerPersonnel.LastName}"
                : evaluation.EvaluatedUnknownPersonnel ?? "-";

            // Placeholder'ları değiştir
            var subject = ReplacePlaceholders(template.Subject, customer, evaluation, evaluatedPersonnel, null, null, 1);
            var body = ReplacePlaceholders(template.Body, customer, evaluation, evaluatedPersonnel, null, null, 1);

            // Email gönder
            var emails = customer.NotificationEmails.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();

            foreach (var email in emails)
            {
                var result = await _emailService.SendEmailAsync(email, subject, body);
                if (!result.Success)
                {
                    _logger.LogError("Failed to send evaluation notification to {Email}: {Error}", email, result.ErrorMessage);
                }
            }

            _logger.LogInformation("Sent single evaluation notification for evaluation {EvaluationId} to customer {CustomerId}",
                evaluation.Id, customer.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending single evaluation notification for evaluation {EvaluationId}", evaluation.Id);
        }
    }

    /// <summary>
    /// Günlük özet bildirimi gönderir (dün yapılan değerlendirmeler)
    /// </summary>
    public async Task SendDailyNotificationsAsync()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var today = DateTime.UtcNow.Date;

        await SendPeriodNotificationsAsync(
            EvaluationNotificationFrequencies.Ids.Daily,
            yesterday,
            today,
            "Günlük");
    }

    /// <summary>
    /// Haftalık özet bildirimi gönderir (bu hafta yapılan değerlendirmeler)
    /// </summary>
    public async Task SendWeeklyNotificationsAsync()
    {
        var today = DateTime.UtcNow.Date;
        // Pazartesi'yi hesapla (haftanın başı)
        var daysFromMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var weekStart = today.AddDays(-daysFromMonday);

        await SendPeriodNotificationsAsync(
            EvaluationNotificationFrequencies.Ids.Weekly,
            weekStart,
            today.AddDays(1),
            "Haftalık");
    }

    /// <summary>
    /// Aylık özet bildirimi gönderir (bu ay yapılan değerlendirmeler)
    /// </summary>
    public async Task SendMonthlyNotificationsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        await SendPeriodNotificationsAsync(
            EvaluationNotificationFrequencies.Ids.Monthly,
            monthStart,
            today.AddDays(1),
            "Aylık");
    }

    /// <summary>
    /// Dönem bazlı bildirim gönderir
    /// </summary>
    private async Task SendPeriodNotificationsAsync(int frequencyId, DateTime periodStart, DateTime periodEnd, string periodName)
    {
        try
        {
            // Bu sıklıkta bildirim isteyen müşterileri bul
            var customers = await _context.Customers
                .Include(c => c.EvaluationNotificationTemplate)
                .Where(c => !c.IsDeleted && c.IsActive &&
                           c.EvaluationNotificationFrequencyId == frequencyId &&
                           !string.IsNullOrEmpty(c.NotificationEmails))
                .ToListAsync();

            _logger.LogInformation("Found {Count} customers for {Period} notification", customers.Count, periodName);

            foreach (var customer in customers)
            {
                try
                {
                    await SendPeriodNotificationToCustomerAsync(customer, periodStart, periodEnd, periodName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending {Period} notification to customer {CustomerId}", periodName, customer.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Period} notifications", periodName);
        }
    }

    /// <summary>
    /// Belirli bir müşteriye dönem bildirimi gönderir
    /// </summary>
    private async Task SendPeriodNotificationToCustomerAsync(Customer customer, DateTime periodStart, DateTime periodEnd, string periodName)
    {
        // Bu dönemdeki değerlendirmeleri al
        var evaluations = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a!.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Where(e => e.Assignment!.Project!.CustomerId == customer.Id &&
                       e.StatusId == EvaluationStatuses.Ids.Completed &&
                       e.CompletedAt >= periodStart &&
                       e.CompletedAt < periodEnd)
            .OrderByDescending(e => e.CompletedAt)
            .ToListAsync();

        if (!evaluations.Any())
        {
            _logger.LogDebug("No evaluations found for customer {CustomerId} in period {PeriodStart} - {PeriodEnd}",
                customer.Id, periodStart, periodEnd);
            return;
        }

        // Email şablonunu al
        var template = customer.EvaluationNotificationTemplate;
        if (template == null)
        {
            template = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateTypeId == EmailTemplateTypes.Ids.EvaluationNotification &&
                                          t.IsDefault && !t.IsDeleted);
        }

        if (template == null)
        {
            _logger.LogWarning("No email template found for evaluation notification");
            return;
        }

        // Özet tablo oluştur
        var summaryTable = BuildSummaryTable(evaluations);

        // Placeholder'ları değiştir
        var subject = ReplacePlaceholders(template.Subject, customer, null, null, periodStart, periodEnd, evaluations.Count, summaryTable);
        var body = ReplacePlaceholders(template.Body, customer, null, null, periodStart, periodEnd, evaluations.Count, summaryTable);

        // Email gönder
        var emails = customer.NotificationEmails!.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrEmpty(e))
            .ToList();

        foreach (var email in emails)
        {
            var result = await _emailService.SendEmailAsync(email, subject, body);
            if (!result.Success)
            {
                _logger.LogError("Failed to send {Period} notification to {Email}: {Error}", periodName, email, result.ErrorMessage);
            }
        }

        // Son bildirim tarihini güncelle
        customer.LastNotificationSentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sent {Period} notification to customer {CustomerId} with {Count} evaluations",
            periodName, customer.Id, evaluations.Count);
    }

    /// <summary>
    /// Özet tablo HTML'i oluşturur
    /// </summary>
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

        foreach (var eval in evaluations.Take(50)) // Max 50 satır
        {
            var personnelName = eval.EvaluatedCustomerPersonnel != null
                ? $"{eval.EvaluatedCustomerPersonnel.FirstName} {eval.EvaluatedCustomerPersonnel.LastName}"
                : eval.EvaluatedUnknownPersonnel ?? "-";

            var scoreColor = eval.ScorePercentage >= 80 ? "#28a745" :
                            eval.ScorePercentage >= 60 ? "#ffc107" : "#dc3545";

            html += $@"
        <tr>
            <td style='border: 1px solid #dee2e6; padding: 8px;'>{eval.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-"}</td>
            <td style='border: 1px solid #dee2e6; padding: 8px;'>{eval.Assignment?.Project?.Name ?? "-"}</td>
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

    /// <summary>
    /// Placeholder'ları değiştirir
    /// </summary>
    private string ReplacePlaceholders(
        string content,
        Customer customer,
        Evaluation? evaluation,
        string? evaluatedPersonnel,
        DateTime? periodStart,
        DateTime? periodEnd,
        int evaluationCount,
        string? summaryTable = null)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // Firma bilgileri
        content = content.Replace(EmailPlaceholders.CompanyName, customer.CompanyName);

        // Değerlendirme bilgileri (tekil)
        if (evaluation != null)
        {
            content = content.Replace(EmailPlaceholders.EvaluationScore, evaluation.TotalScore?.ToString("F2") ?? "-");
            content = content.Replace(EmailPlaceholders.EvaluationScorePercentage, evaluation.ScorePercentage?.ToString("F2") ?? "-");
            content = content.Replace(EmailPlaceholders.EvaluatedPersonnel, evaluatedPersonnel ?? "-");
            content = content.Replace(EmailPlaceholders.EvaluationDate, evaluation.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-");
            content = content.Replace(EmailPlaceholders.ProjectName, evaluation.Assignment?.Project?.Name ?? "-");
            content = content.Replace(EmailPlaceholders.OrganizationName, evaluation.EvaluatedOrganization?.Name ?? "-");
        }

        // Dönem bilgileri (özet)
        content = content.Replace(EmailPlaceholders.EvaluationCount, evaluationCount.ToString());
        content = content.Replace(EmailPlaceholders.PeriodStartDate, periodStart?.ToString("dd.MM.yyyy") ?? "-");
        content = content.Replace(EmailPlaceholders.PeriodEndDate, periodEnd?.AddDays(-1).ToString("dd.MM.yyyy") ?? "-");
        content = content.Replace(EmailPlaceholders.EvaluationSummaryTable, summaryTable ?? "");

        // Sistem bilgileri
        content = content.Replace(EmailPlaceholders.CurrentDate, DateTime.Now.ToString("dd.MM.yyyy"));
        content = content.Replace(EmailPlaceholders.CurrentYear, DateTime.Now.Year.ToString());
        content = content.Replace(EmailPlaceholders.SystemName, "Secret Customer");

        return content;
    }
}
