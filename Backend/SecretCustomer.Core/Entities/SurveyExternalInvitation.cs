using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Dış katılımcılara (email listesi) gönderilen anket davetiyesi
/// CustomerPersonnel dışındaki email adreslerine anket göndermek için kullanılır
/// </summary>
public class SurveyExternalInvitation : BaseEntity
{
    public int ProjectId { get; set; }

    /// <summary>
    /// Alıcı email adresi
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Alıcı adı (opsiyonel)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Alıcı soyadı (opsiyonel)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Benzersiz token (GUID) - anket linkinde kullanılır
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gönderim durumu: Pending, Sent, Failed
    /// </summary>
    public int StatusId { get; set; } = SurveyInvitationStatuses.Ids.Pending;

    /// <summary>
    /// Hata mesajı (başarısız olursa)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gönderim zamanı
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Kaçıncı deneme
    /// </summary>
    public int AttemptCount { get; set; } = 1;

    /// <summary>
    /// Anket linki açıldı mı?
    /// </summary>
    public bool IsOpened { get; set; }

    /// <summary>
    /// Açılma zamanı
    /// </summary>
    public DateTime? OpenedAt { get; set; }

    /// <summary>
    /// Anket tamamlandı mı?
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Tamamlanma zamanı
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Tamamlanan anketin Evaluation ID'si
    /// </summary>
    public int? EvaluationId { get; set; }

    // Navigation
    public virtual Project? Project { get; set; }
    public virtual Evaluation? Evaluation { get; set; }

    /// <summary>
    /// Alıcının tam adını döndürür
    /// </summary>
    public string? FullName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
        ? null
        : $"{FirstName} {LastName}".Trim();
}
