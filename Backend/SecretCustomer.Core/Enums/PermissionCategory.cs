namespace SecretCustomer.Core.Enums;

/// <summary>
/// Permission kategorileri - Yetkilerin hangi modüle ait olduğunu belirtir
/// </summary>
public enum PermissionCategory
{
    /// <summary>
    /// Kullanıcı yönetimi
    /// </summary>
    Users = 1,

    /// <summary>
    /// Rol yönetimi
    /// </summary>
    Roles = 2,

    /// <summary>
    /// Proje yönetimi
    /// </summary>
    Projects = 3,

    /// <summary>
    /// Görev/Atama yönetimi
    /// </summary>
    Assignments = 4,

    /// <summary>
    /// Kontrol listesi yönetimi
    /// </summary>
    Checklists = 5,

    /// <summary>
    /// Değerlendirme yönetimi
    /// </summary>
    Evaluations = 6,

    /// <summary>
    /// Şube yönetimi
    /// </summary>
    Branches = 7,

    /// <summary>
    /// Saha çalışanı yönetimi
    /// </summary>
    FieldWorkers = 8,

    /// <summary>
    /// Rapor yönetimi
    /// </summary>
    Reports = 9,

    /// <summary>
    /// Dashboard erişimi
    /// </summary>
    Dashboard = 10,

    /// <summary>
    /// Excel template yönetimi
    /// </summary>
    ExcelTemplates = 11,

    /// <summary>
    /// Müşteri yönetimi
    /// </summary>
    Customers = 12,

    /// <summary>
    /// Müşteri personeli yönetimi
    /// </summary>
    CustomerPersonnel = 13,

    /// <summary>
    /// Sistem ayarları
    /// </summary>
    Settings = 14
}
