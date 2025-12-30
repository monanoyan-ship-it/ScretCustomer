namespace SecretCustomer.Core.Enums;

/// <summary>
/// Müşteri personeli rolleri
/// </summary>
public enum CustomerPersonnelRole
{
    /// <summary>
    /// Müşteri Yöneticisi - Tüm raporları görür (Firma Yetkilisi)
    /// </summary>
    CustomerManager = 1,

    /// <summary>
    /// Müşteri Süpervizörü - Kendi takımının raporlarını görür (Takım Lideri)
    /// </summary>
    CustomerSupervisor = 2,

    /// <summary>
    /// Müşteri Operatörü - Sadece kendi değerlerini görür (Değerlendirilecek personel)
    /// </summary>
    CustomerOperator = 3
}
