namespace SecretCustomer.Core.Enums;

/// <summary>
/// İzin kapsamları
/// </summary>
public enum PermissionScope
{
    /// <summary>
    /// Tüm kaynaklara erişim
    /// </summary>
    All = 1,

    /// <summary>
    /// Sadece kendi oluşturduğu kaynaklara erişim
    /// </summary>
    Own = 2,

    /// <summary>
    /// Kendi şubesine ait kaynaklara erişim
    /// </summary>
    Branch = 3,

    /// <summary>
    /// Kendi departmanına ait kaynaklara erişim
    /// </summary>
    Department = 4,

    /// <summary>
    /// Kendi müşterisine ait kaynaklara erişim
    /// </summary>
    Customer = 5
}