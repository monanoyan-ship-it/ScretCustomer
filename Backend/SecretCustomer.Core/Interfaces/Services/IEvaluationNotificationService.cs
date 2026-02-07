using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

/// <summary>
/// Değerlendirme bildirim servisi
/// </summary>
public interface IEvaluationNotificationService
{
    /// <summary>
    /// Tekil değerlendirme bildirimi gönderir (FrequencyId=1 kuralları için).
    /// Değerlendirme tamamlanınca anında çağrılır.
    /// </summary>
    /// <param name="evaluation">Değerlendirme (Include'lar yüklenmiş olmalı)</param>
    /// <param name="baseUrl">Uygulamanın base URL'i (Request.Scheme + Request.Host)</param>
    Task SendSingleEvaluationNotificationAsync(Evaluation evaluation, string baseUrl);

    /// <summary>
    /// Zamanlı bildirim kurallarını işler (Günlük, Haftalık, Aylık).
    /// Job tarafından her gün 19:00'da çağrılır.
    /// </summary>
    Task ProcessScheduledNotificationsAsync();
}
