namespace SecretCustomer.Core.Entities;

/// <summary>
/// Personele gönderilen bildirim e-postalarının kaydı
/// Her mail gönderiminde bir kayıt oluşturulur
/// </summary>
public class CustomerPersonnelNotificationLog : BaseEntity
{
    /// <summary>
    /// Bildirimin gönderildiği personel
    /// </summary>
    public int CustomerPersonnelId { get; set; }
    public CustomerPersonnel CustomerPersonnel { get; set; } = null!;

    /// <summary>
    /// Müşteri (hızlı erişim için)
    /// </summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>
    /// Gönderilme tarihi
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Bildirim tipi: PerEvaluation, Daily, Weekly, Monthly
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Dönem başlangıç tarihi (özet bildirimleri için)
    /// </summary>
    public DateTime? PeriodStart { get; set; }

    /// <summary>
    /// Dönem bitiş tarihi (özet bildirimleri için)
    /// </summary>
    public DateTime? PeriodEnd { get; set; }

    /// <summary>
    /// Gönderilen e-posta adresi
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// E-posta konusu
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Dahil edilen değerlendirme ID'leri (JSON array)
    /// Örn: [1, 2, 3, 4]
    /// </summary>
    public string EvaluationIdsJson { get; set; } = "[]";

    /// <summary>
    /// Değerlendirme sayısı
    /// </summary>
    public int EvaluationCount { get; set; }

    /// <summary>
    /// Gönderim başarılı mı?
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Hata mesajı (başarısız gönderimler için)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Değerlendirme bildirim tipleri
/// </summary>
public static class EvaluationNotificationTypes
{
    public const string PerEvaluation = "PerEvaluation";
    public const string Daily = "Daily";
    public const string Weekly = "Weekly";
    public const string Monthly = "Monthly";
}
