namespace SecretCustomer.Core.Enums;

/// <summary>
/// Soru/Grup puanlama tipi
/// </summary>
public enum ScoringType
{
    /// <summary>
    /// Puanlı - Normal puanlama yapılır
    /// </summary>
    Scored = 1,

    /// <summary>
    /// Puansız - Puan hesaplanmaz
    /// </summary>
    Unscored = 2,

    /// <summary>
    /// Cezalı - Sarı/Kırmızı kart uygulanır
    /// </summary>
    Penalty = 3
}
