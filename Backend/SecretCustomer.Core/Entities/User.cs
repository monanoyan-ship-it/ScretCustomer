using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Attributes;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Kullanıcı", Description = "Sistem kullanıcıları için Excel import/export", IsAvailable = true)]
public class User : BaseEntity
{
    [ExcelColumn("Kullanıcı Adı", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Kullanıcının sisteme giriş yapacağı kullanıcı adı", SampleValue = "ahmet.yilmaz")]
    public string Username { get; set; } = string.Empty;

    [ExcelColumn("E-posta", 2, IsRequired = true, ColumnType = ExcelColumnType.Email,
        Description = "Kullanıcının e-posta adresi", SampleValue = "ahmet@example.com")]
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    [ExcelColumn("Ad", 3, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Kullanıcının adı", SampleValue = "Ahmet")]
    public string FirstName { get; set; } = string.Empty;

    [ExcelColumn("Soyad", 4, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Kullanıcının soyadı", SampleValue = "Yılmaz")]
    public string LastName { get; set; } = string.Empty;

    [ExcelColumn("Rol", 5, IsRequired = true, ColumnType = ExcelColumnType.Dropdown,
        Description = "Kullanıcının sistem rolü",
        DropdownOptions = "[\"Admin\", \"Manager\", \"Evaluator\", \"FieldWorker\"]",
        SampleValue = "Manager")]
    public UserRole Role { get; set; }

    [ExcelColumn("Aktif", 6, ColumnType = ExcelColumnType.Boolean,
        Description = "Kullanıcının aktif olup olmadığı", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    // Navigation properties
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
