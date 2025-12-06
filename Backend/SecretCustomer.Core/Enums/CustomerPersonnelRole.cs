namespace SecretCustomer.Core.Enums;

/// <summary>
/// Müşteri personeli rolleri
/// </summary>
public enum CustomerPersonnelRole
{
    /// <summary>
    /// Müşteri yöneticisi - tüm yetkilere sahip
    /// </summary>
    CustomerManager = 1,

    /// <summary>
    /// Müşteri süpervizörü - görev atama ve izleme yetkisi
    /// </summary>
    CustomerSupervisor = 2,

    /// <summary>
    /// Müşteri operatörü - görev yapma yetkisi
    /// </summary>
    CustomerOperator = 3,

    /// <summary>
    /// Müşteri görüntüleyici - sadece okuma yetkisi
    /// </summary>
    CustomerViewer = 4
}
