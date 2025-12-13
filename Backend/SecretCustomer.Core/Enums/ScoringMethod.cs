namespace SecretCustomer.Core.Enums;

/// <summary>
/// Puanlama yöntemi
/// </summary>
public enum ScoringMethod
{
    /// <summary>
    /// Maksimum puan üzerinden hesaplama
    /// </summary>
    Maximum = 1,

    /// <summary>
    /// Ortalama puan hesaplama
    /// </summary>
    Average = 2,

    /// <summary>
    /// Ağırlıklı ortalama
    /// </summary>
    WeightedAverage = 3,

    /// <summary>
    /// Toplam puan
    /// </summary>
    Sum = 4
}
