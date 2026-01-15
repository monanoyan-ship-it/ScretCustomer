using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Anket davetiye gönderim kaydı
/// </summary>
public class SurveyInvitation : BaseEntity
{
    public int ProjectId { get; set; }
    public int CustomerPersonnelId { get; set; }
    public string Email { get; set; } = string.Empty;

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
    /// Hatırlatma mı?
    /// </summary>
    public bool IsReminder { get; set; }

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

    // Navigation
    public virtual Project? Project { get; set; }
    public virtual CustomerPersonnel? CustomerPersonnel { get; set; }
}
