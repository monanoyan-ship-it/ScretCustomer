namespace SecretCustomer.Core.Enums;

/// <summary>
/// Müşteri görev tipleri
/// </summary>
public enum CustomerTaskType
{
    /// <summary>
    /// Denetim görevi
    /// </summary>
    Inspection = 1,

    /// <summary>
    /// Audit görevi
    /// </summary>
    Audit = 2,

    /// <summary>
    /// Anket/Değerlendirme görevi
    /// </summary>
    Survey = 3,

    /// <summary>
    /// Saha çalışması
    /// </summary>
    FieldWork = 4,

    /// <summary>
    /// Raporlama görevi
    /// </summary>
    Reporting = 5
}