using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Şube Personeli - Değerlendirilen personel bilgileri
/// </summary>
[ExcelTemplate("Personel", Description = "Şube personeli için Excel import/export", IsAvailable = true)]
public class Personnel : BaseEntity
{
    [ExcelColumn("Ad", 1, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Personelin adı", SampleValue = "Ahmet")]
    public string FirstName { get; set; } = string.Empty;

    [ExcelColumn("Soyad", 2, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Personelin soyadı", SampleValue = "Yılmaz")]
    public string LastName { get; set; } = string.Empty;

    [ExcelColumn("TC Kimlik No", 3, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "TC Kimlik Numarası", SampleValue = "12345678901")]
    public string? TcKimlikNo { get; set; }

    [ExcelColumn("ERP No", 4, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "ERP Numarası", SampleValue = "ERP001")]
    public string? ErpNo { get; set; }

    [ExcelColumn("Sicil No", 5, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Sicil Numarası", SampleValue = "SCL001")]
    public string? SicilNo { get; set; }

    [ExcelColumn("Unvan", 6, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Personelin unvanı", SampleValue = "Müşteri Temsilcisi")]
    public string? Title { get; set; }

    [ExcelColumn("Cinsiyet", 7, ColumnType = ExcelColumnTypes.Ids.Dropdown,
        Description = "Cinsiyet",
        DropdownOptions = "[\"Erkek\", \"Kadın\", \"Belirtilmemiş\"]",
        SampleValue = "Erkek")]
    public int GenderId { get; set; } = Genders.Ids.Unspecified;

    [ExcelColumn("Doğum Tarihi", 8, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Doğum tarihi")]
    public DateTime? BirthDate { get; set; }

    [ExcelColumn("İşe Başlama Tarihi", 9, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "İşe başlama tarihi")]
    public DateTime? HireDate { get; set; }

    [ExcelColumn("E-posta", 10, ColumnType = ExcelColumnTypes.Ids.Email,
        Description = "E-posta adresi", SampleValue = "ahmet.yilmaz@example.com")]
    public string? Email { get; set; }

    [ExcelColumn("Telefon", 11, ColumnType = ExcelColumnTypes.Ids.Phone,
        Description = "Telefon numarası", SampleValue = "0532 123 4567")]
    public string? PhoneNumber { get; set; }

    [ExcelColumn("Departman", 12, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Departman", SampleValue = "Müşteri Hizmetleri")]
    public string? Department { get; set; }

    [ExcelColumn("Aktif", 13, ColumnType = ExcelColumnTypes.Ids.Boolean,
        Description = "Personelin aktif olup olmadığı", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    [ExcelColumn("Notlar", 14, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Personel hakkında notlar")]
    public string? Notes { get; set; }

    // Foreign Keys
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // Computed Property
    public string FullName => $"{FirstName} {LastName}";

    // Navigation Properties
    public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
}
