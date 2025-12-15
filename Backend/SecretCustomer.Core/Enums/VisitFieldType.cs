namespace SecretCustomer.Core.Enums;

/// <summary>
/// Ziyaret alanı veri tipleri
/// </summary>
public enum VisitFieldType
{
    /// <summary>
    /// Tam sayı
    /// </summary>
    Int = 1,

    /// <summary>
    /// Ondalıklı sayı
    /// </summary>
    Decimal = 2,

    /// <summary>
    /// Evet/Hayır
    /// </summary>
    Bool = 3,

    /// <summary>
    /// Metin
    /// </summary>
    String = 4,

    /// <summary>
    /// Tarih/Saat
    /// </summary>
    DateTime = 5,

    /// <summary>
    /// Puan (1-5 veya 1-10)
    /// </summary>
    Rating = 6
}
