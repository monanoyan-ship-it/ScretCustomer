namespace SecretCustomer.Core.Enums;

public enum AssignmentType
{
    /// <summary>
    /// İç şube değerlendirmesi - Branch ile atama
    /// </summary>
    InternalBranch = 1,

    /// <summary>
    /// İç kullanıcı değerlendirmesi - User ile atama
    /// </summary>
    InternalUser = 2,

    /// <summary>
    /// Dış müşteri anketi - Email ile atama
    /// </summary>
    ExternalCustomer = 3,

    /// <summary>
    /// Müşteri personeli değerlendirmesi - CustomerPersonnel ile atama
    /// </summary>
    CustomerPersonnel = 4,

    /// <summary>
    /// Saha çalışanı değerlendirmesi - FieldWorker ile atama
    /// </summary>
    FieldWorker = 5
}
