using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

/// <summary>
/// Değerlendirme bildirim servisi
/// </summary>
public interface IEvaluationNotificationService
{
    /// <summary>
    /// Tekil değerlendirme bildirimi gönderir (Her Kayıtta seçeneği için)
    /// </summary>
    Task SendSingleEvaluationNotificationAsync(Evaluation evaluation);

    /// <summary>
    /// Günlük özet bildirimi gönderir (dün yapılan değerlendirmeler)
    /// </summary>
    Task SendDailyNotificationsAsync();

    /// <summary>
    /// Haftalık özet bildirimi gönderir (bu hafta yapılan değerlendirmeler)
    /// </summary>
    Task SendWeeklyNotificationsAsync();

    /// <summary>
    /// Aylık özet bildirimi gönderir (bu ay yapılan değerlendirmeler)
    /// </summary>
    Task SendMonthlyNotificationsAsync();
}
