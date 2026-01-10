using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Müşteri Personeli", Description = "Müşteri personelleri için Excel import/export", IsAvailable = true)]
public class CustomerPersonnel : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [ExcelColumn("Kullanıcı Adı", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Personelin sisteme giriş yapacağı kullanıcı adı", SampleValue = "ali.veli")]
    public string Username { get; set; } = string.Empty;

    [ExcelColumn("E-posta", 2, IsRequired = true, ColumnType = ExcelColumnType.Email,
        Description = "Personelin e-posta adresi", SampleValue = "ali.veli@abc.com")]
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    [ExcelColumn("Ad", 3, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Personelin adı", SampleValue = "Ali")]
    public string FirstName { get; set; } = string.Empty;

    [ExcelColumn("Soyad", 4, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Personelin soyadı", SampleValue = "Veli")]
    public string LastName { get; set; } = string.Empty;

    [ExcelColumn("Telefon", 5, ColumnType = ExcelColumnType.Phone,
        Description = "Personelin telefon numarası", SampleValue = "0555 123 4567")]
    public string? PhoneNumber { get; set; }

    [ExcelColumn("Departman", 6, ColumnType = ExcelColumnType.Text,
        Description = "Personelin departmanı", SampleValue = "Kalite Kontrol")]
    public string? Department { get; set; }

    [ExcelColumn("Ünvan", 7, ColumnType = ExcelColumnType.Text,
        Description = "Personelin ünvanı", SampleValue = "Kalite Müdürü")]
    public string? Title { get; set; }

    [ExcelColumn("Rol", 8, IsRequired = true, ColumnType = ExcelColumnType.Dropdown,
        Description = "Personelin sistem rolü",
        DropdownOptions = "[\"CustomerManager\", \"CustomerSupervisor\", \"CustomerOperator\"]",
        SampleValue = "CustomerManager")]
    public CustomerPersonnelRole Role { get; set; }

    [ExcelColumn("Aktif", 9, ColumnType = ExcelColumnType.Boolean,
        Description = "Personelin aktif olup olmadığı", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    [ExcelColumn("Notlar", 10, ColumnType = ExcelColumnType.Text,
        Description = "Personel hakkında notlar")]
    public string? Notes { get; set; }

    /// <summary>
    /// İlk giriş veya şifre sıfırlama sonrası şifre değiştirmeye zorla
    /// </summary>
    public bool MustChangePassword { get; set; } = false;

    // Language Preference
    public int? PreferredLanguageId { get; set; }
    public Language? PreferredLanguage { get; set; }

    // Computed Property
    public string FullName => $"{FirstName} {LastName}";

    // Navigation Properties
    public ICollection<CustomerPersonnelTaskAssignment> TaskAssignments { get; set; } = new List<CustomerPersonnelTaskAssignment>();
    public ICollection<CustomerPersonnelPermission> Permissions { get; set; } = new List<CustomerPersonnelPermission>();

    /// <summary>
    /// Bu personelin gorev aldigi organizasyonlar (many-to-many, her atamada farkli supervisor olabilir)
    /// </summary>
    public ICollection<CustomerPersonnelOrganization> OrganizationAssignments { get; set; } = new List<CustomerPersonnelOrganization>();
}