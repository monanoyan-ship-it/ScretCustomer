namespace SecretCustomer.Core.Entities;

/// <summary>
/// Gölge Müşteri - Dönem Personel (döneme atanmış personel)
/// </summary>
public class GmDonemPersonel : BaseEntity
{
    /// <summary>
    /// Hangi döneme ait
    /// </summary>
    public int GmDonemId { get; set; }
    public GmDonem? GmDonem { get; set; }

    /// <summary>
    /// Atanan kullanıcı
    /// </summary>
    public int UserId { get; set; }
    public User? User { get; set; }
}
