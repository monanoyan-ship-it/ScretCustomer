namespace SecretCustomer.Core.Enums;

/// <summary>
/// Kontrol listesi tipi
/// </summary>
public enum ChecklistType
{
    /// <summary>
    /// Çağrı Performans değerlendirmesi
    /// </summary>
    CallPerformance = 1,

    /// <summary>
    /// Fiziksel denetim (şube ziyareti vb.)
    /// </summary>
    PhysicalAudit = 2,

    /// <summary>
    /// Mystery Shopping (Gizli Müşteri)
    /// </summary>
    MysteryShopping = 3,

    /// <summary>
    /// Online değerlendirme
    /// </summary>
    OnlineEvaluation = 4,

    /// <summary>
    /// Genel anket
    /// </summary>
    Survey = 5,

    /// <summary>
    /// Banka Gizli Müşteri (GBF - Gizli Banka Formu)
    /// </summary>
    BankMysteryShopping = 6
}
