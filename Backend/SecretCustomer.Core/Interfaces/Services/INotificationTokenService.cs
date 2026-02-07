namespace SecretCustomer.Core.Interfaces.Services;

/// <summary>
/// Değerlendirme bildirim e-postalarındaki public linkler için token encrypt/decrypt servisi
/// </summary>
public interface INotificationTokenService
{
    /// <summary>
    /// Tekil değerlendirme linki için token oluşturur
    /// </summary>
    string GenerateSingleToken(int evaluationId, DateTime expiresAt);

    /// <summary>
    /// Toplu değerlendirme linki için token oluşturur (CustomerId + tarih aralığı)
    /// </summary>
    string GenerateBulkToken(int customerId, DateTime startDate, DateTime endDate, DateTime expiresAt);

    /// <summary>
    /// Personel bazlı link için token oluşturur (CustomerPersonnelId + tarih aralığı)
    /// </summary>
    string GeneratePersonnelToken(int customerPersonnelId, DateTime startDate, DateTime endDate, DateTime expiresAt);

    /// <summary>
    /// Token'ı decrypt edip payload'ı döndürür. Süresi dolmuşsa null döner.
    /// </summary>
    NotificationTokenPayload? DecryptToken(string token);
}

/// <summary>
/// Token payload - decrypt sonucu
/// </summary>
public class NotificationTokenPayload
{
    /// <summary>
    /// Token tipi: "single", "bulk", "personnel"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public int? EvaluationId { get; set; }
    public int? CustomerId { get; set; }
    public int? CustomerPersonnelId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime ExpiresAt { get; set; }
}
