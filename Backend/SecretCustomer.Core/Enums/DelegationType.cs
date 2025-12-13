namespace SecretCustomer.Core.Enums;

/// <summary>
/// Vekalet tipleri
/// </summary>
public enum DelegationType
{
    /// <summary>
    /// Tam yetki
    /// </summary>
    Full = 0,

    /// <summary>
    /// Sadece okuma yetkisi
    /// </summary>
    ReadOnly = 1,

    /// <summary>
    /// Sadece onay yetkisi
    /// </summary>
    ApprovalOnly = 2
}
