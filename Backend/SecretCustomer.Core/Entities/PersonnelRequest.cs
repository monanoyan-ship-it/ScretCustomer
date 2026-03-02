namespace SecretCustomer.Core.Entities;

/// <summary>
/// Değerlendirme sırasında listede olmayan personel için talep
/// Kullanıcı talep oluşturur, Admin onaylar/reddeder
/// </summary>
public class PersonnelRequest : BaseEntity
{
    /// <summary>
    /// Hangi değerlendirmeden geldi
    /// </summary>
    public int EvaluationId { get; set; }
    public Evaluation Evaluation { get; set; } = null!;

    /// <summary>
    /// Hangi firma için
    /// </summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>
    /// Hangi organizasyon/departman için (opsiyonel - org bulunamadığında null olabilir)
    /// </summary>
    public int? CustomerOrganizationId { get; set; }
    public CustomerOrganization? CustomerOrganization { get; set; }

    /// <summary>
    /// Talep edilen personelin adı
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Talep edilen personelin soyadı
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Ünvan (opsiyonel)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Talep notu (opsiyonel)
    /// </summary>
    public string? Notes { get; set; }

    // ===== Talep Eden =====

    /// <summary>
    /// Talebi oluşturan kullanıcı
    /// </summary>
    public int RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;

    // ===== Durum =====

    /// <summary>
    /// Talep durumu (ApprovalStatuses: Pending=1, Approved=2, Rejected=3)
    /// </summary>
    public int Status { get; set; } = 1; // Pending

    // ===== Onay/Red Bilgileri =====

    /// <summary>
    /// Talebi değerlendiren admin
    /// </summary>
    public int? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }

    /// <summary>
    /// Değerlendirme tarihi
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Red sebebi (reddedilirse)
    /// </summary>
    public string? RejectReason { get; set; }

    // ===== Onay Sonucu =====

    /// <summary>
    /// Onaylanınca oluşturulan personel
    /// </summary>
    public int? CreatedPersonnelId { get; set; }
    public CustomerPersonnel? CreatedPersonnel { get; set; }

    // ===== Computed =====

    public string FullName => $"{FirstName} {LastName}";
}
