using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Saha Çalışanı", Description = "Saha çalışanları için Excel import/export", IsAvailable = true)]
public class FieldWorker : BaseEntity
{
    [ExcelColumn("Ad", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Saha çalışanının adı", SampleValue = "Mehmet")]
    public string FirstName { get; set; } = string.Empty;

    [ExcelColumn("Soyad", 2, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Saha çalışanının soyadı", SampleValue = "Demir")]
    public string LastName { get; set; } = string.Empty;

    [ExcelColumn("Telefon", 3, IsRequired = true, ColumnType = ExcelColumnType.Phone,
        Description = "Saha çalışanının telefon numarası", SampleValue = "0555 123 4567")]
    public string PhoneNumber { get; set; } = string.Empty;

    [ExcelColumn("Adres", 4, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Saha çalışanının adresi", SampleValue = "Kadıköy, İstanbul")]
    public string Address { get; set; } = string.Empty;

    [ExcelColumn("E-posta", 5, ColumnType = ExcelColumnType.Email,
        Description = "Saha çalışanının e-posta adresi", SampleValue = "mehmet@example.com")]
    public string? Email { get; set; }

    [ExcelColumn("Aktif", 6, ColumnType = ExcelColumnType.Boolean,
        Description = "Saha çalışanının aktif olup olmadığı", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    [ExcelColumn("Notlar", 7, ColumnType = ExcelColumnType.Text,
        Description = "Saha çalışanı hakkında ek notlar", SampleValue = "Tecrübeli, güvenilir")]
    public string? Notes { get; set; }

    // User relationship - saha çalışanı aynı zamanda sistem kullanıcısıdır
    public int? UserId { get; set; }
    public User? User { get; set; }

    // Navigation properties
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
