namespace SecretCustomer.Core.Enums;

/// <summary>
/// Ceza tipi (Sarı Kart / Kırmızı Kart)
/// </summary>
public enum PenaltyType
{
    /// <summary>
    /// Ceza yok
    /// </summary>
    None = 0,

    /// <summary>
    /// Sarı Kart - Uyarı niteliğinde
    /// </summary>
    YellowCard = 1,

    /// <summary>
    /// Kırmızı Kart - Kritik hata
    /// </summary>
    RedCard = 2
}
