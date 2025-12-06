namespace SecretCustomer.Core.Enums;

/// <summary>
/// Personelin görevdeki rolü
/// </summary>
public enum TaskAssignmentRole
{
    /// <summary>
    /// Görev sahibi - Görevi yürütür
    /// </summary>
    Owner = 1,

    /// <summary>
    /// Görev yardımcısı - Desteğe katkıda bulunur
    /// </summary>
    Assistant = 2,

    /// <summary>
    /// Gözlemci - Sadece takip eder
    /// </summary>
    Observer = 3,

    /// <summary>
    /// Onaylayıcı - Görevi onaylar
    /// </summary>
    Approver = 4
}