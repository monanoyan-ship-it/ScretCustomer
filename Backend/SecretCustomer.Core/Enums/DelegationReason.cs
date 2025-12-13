namespace SecretCustomer.Core.Enums;

/// <summary>
/// Vekalet sebepleri
/// </summary>
public enum DelegationReason
{
    /// <summary>
    /// Yıllık izin
    /// </summary>
    AnnualLeave = 0,

    /// <summary>
    /// Hastalık izni
    /// </summary>
    SickLeave = 1,

    /// <summary>
    /// İş seyahati
    /// </summary>
    BusinessTrip = 2,

    /// <summary>
    /// Eğitim
    /// </summary>
    Training = 3,

    /// <summary>
    /// Diğer
    /// </summary>
    Other = 4
}
