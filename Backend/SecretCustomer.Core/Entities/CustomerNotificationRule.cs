namespace SecretCustomer.Core.Entities;

/// <summary>
/// Müşteri bazlı değerlendirme bildirim kuralı.
/// Her müşteri için birden fazla kural tanımlanabilir.
/// </summary>
public class CustomerNotificationRule : BaseEntity
{
    /// <summary>
    /// Hangi müşteriye ait
    /// </summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>
    /// Bildirim sıklığı: 1=Her Kayıtta, 2=Günlük, 3=Haftalık, 4=Aylık
    /// </summary>
    public int FrequencyId { get; set; }

    /// <summary>
    /// Haftalıksa: Haftanın günü (DaysOfWeek TypeDef: 0=Pazar, 1=Pazartesi ... 6=Cumartesi)
    /// </summary>
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// Aylıksa: Ayın günü (1-28)
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// Alıcı e-posta adresleri (virgülle ayrılmış)
    /// </summary>
    public string? Emails { get; set; }

    /// <summary>
    /// true ise değerlendirilen personelin kendi e-posta adresine de gönderilir
    /// (Sadece "Her Kayıtta" sıklığında personel maili kullanılır)
    /// </summary>
    public bool SendToPersonnel { get; set; }

    /// <summary>
    /// true ise değerlendirilen personelin takım liderinin (süpervizörün) e-posta adresine de gönderilir
    /// (Sadece "Her Kayıtta" sıklığında)
    /// </summary>
    public bool SendToSupervisor { get; set; }

    /// <summary>
    /// Bu kural için kullanılacak e-posta şablonu
    /// </summary>
    public int? EmailTemplateId { get; set; }
    public EmailTemplate? EmailTemplate { get; set; }

    /// <summary>
    /// Token geçerlilik süresi (gün). Oluşturulan rapor linkinin kaç gün geçerli olacağı.
    /// </summary>
    public int TokenExpirationDays { get; set; } = 30;

    /// <summary>
    /// Kural aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Son gönderim tarihi (Günlük/Haftalık/Aylık için)
    /// </summary>
    public DateTime? LastSentAt { get; set; }
}
