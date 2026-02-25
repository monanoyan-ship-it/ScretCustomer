using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Kullanıcı", Description = "Sistem kullanıcıları için Excel import/export", IsAvailable = true)]
public class User : BaseEntity
{
    [ExcelColumn("Kullanıcı Adı", 1, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Kullanıcının sisteme giriş yapacağı kullanıcı adı", SampleValue = "ahmet.yilmaz")]
    public string Username { get; set; } = string.Empty;

    [ExcelColumn("E-posta", 2, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Email,
        Description = "Kullanıcının e-posta adresi", SampleValue = "ahmet@example.com")]
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    [ExcelColumn("Ad", 3, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Kullanıcının adı", SampleValue = "Ahmet")]
    public string FirstName { get; set; } = string.Empty;

    [ExcelColumn("Soyad", 4, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Kullanıcının soyadı", SampleValue = "Yılmaz")]
    public string LastName { get; set; } = string.Empty;

    [ExcelColumn("Rol", 5, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Dropdown,
        Description = "Kullanıcının sistem rolü",
        DropdownOptions = "[\"Admin\", \"QualitySpecialist\", \"FieldWorker\", \"Inspector\"]",
        SampleValue = "QualitySpecialist")]
    public int RoleId { get; set; }

    [ExcelColumn("Aktif", 6, ColumnType = ExcelColumnTypes.Ids.Boolean,
        Description = "Kullanıcının aktif olup olmadığı", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    [ExcelColumn("Telefon", 7, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Kullanıcının telefon numarası", SampleValue = "0532 123 45 67")]
    public string? PhoneNumber { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Language Preference
    public int? PreferredLanguageId { get; set; }
    public Language? PreferredLanguage { get; set; }

    // Password Reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    /// <summary>
    /// İlk giriş veya şifre sıfırlama sonrası şifre değiştirmeye zorla
    /// </summary>
    public bool MustChangePassword { get; set; } = false;

    // Navigation properties
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
