using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Kullanıcı destek talepleri / hata bildirimleri
/// </summary>
public class SupportRequest : BaseEntity
{
    /// <summary>
    /// Talebi gönderen kullanıcı ID
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Müşteri personeli ise (CustomerPortal'dan gelen)
    /// </summary>
    public int? CustomerPersonnelId { get; set; }

    /// <summary>
    /// Kullanıcı mesajı
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Talebin gönderildiği sayfa URL'si
    /// </summary>
    public string? PageUrl { get; set; }

    /// <summary>
    /// Durum: Pending, InProgress, Resolved, Closed
    /// </summary>
    public int StatusId { get; set; } = SupportRequestStatuses.Ids.Pending;

    /// <summary>
    /// Admin cevabı
    /// </summary>
    public string? AdminResponse { get; set; }

    /// <summary>
    /// Cevaplayan admin ID
    /// </summary>
    public int? RespondedByUserId { get; set; }

    /// <summary>
    /// Cevap tarihi
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// Kullanıcı cevabı okudu mu?
    /// </summary>
    public bool IsReadByUser { get; set; }

    // Navigation
    public User? User { get; set; }
    public User? RespondedByUser { get; set; }
    public CustomerPersonnel? CustomerPersonnel { get; set; }
}
