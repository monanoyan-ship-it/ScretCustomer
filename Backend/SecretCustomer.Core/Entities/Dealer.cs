using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Bayi - Müşterilere ait bayiler (FieldWorker ziyaretleri için)
/// </summary>
[ExcelTemplate("Bayi", Description = "Bayiler için Excel import/export", IsAvailable = true)]
public class Dealer : BaseEntity
{
    /// <summary>
    /// Bayi kodu - Otomatik üretilir (BAY-001, BAY-002, ...)
    /// </summary>
    [ExcelColumn("Bayi Kodu", 1, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Bayi takip kodu", SampleValue = "BAY-001")]
    public string Code { get; set; } = string.Empty;

    [ExcelColumn("Bayi Adı", 2, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Bayi adı", SampleValue = "ABC Bayi")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Adres", 3, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Bayi adresi", SampleValue = "Atatürk Cad. No:1")]
    public string? Address { get; set; }

    [ExcelColumn("Şehir", 4, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Bayi şehri", SampleValue = "İstanbul")]
    public string? City { get; set; }

    [ExcelColumn("İlçe", 5, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Bayi ilçesi", SampleValue = "Kadıköy")]
    public string? District { get; set; }

    /// <summary>
    /// GPS Enlem koordinatı
    /// </summary>
    [ExcelColumn("Enlem", 6, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "GPS Enlem koordinatı", SampleValue = "41.0082")]
    public decimal? Latitude { get; set; }

    /// <summary>
    /// GPS Boylam koordinatı
    /// </summary>
    [ExcelColumn("Boylam", 7, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "GPS Boylam koordinatı", SampleValue = "28.9784")]
    public decimal? Longitude { get; set; }

    [ExcelColumn("Telefon", 8, ColumnType = ExcelColumnTypes.Ids.Phone,
        Description = "Bayi telefonu", SampleValue = "0216 123 4567")]
    public string? Phone { get; set; }

    [ExcelColumn("E-posta", 9, ColumnType = ExcelColumnTypes.Ids.Email,
        Description = "Bayi e-posta adresi", SampleValue = "info@bayi.com")]
    public string? Email { get; set; }

    /// <summary>
    /// Yetkili kişi adı
    /// </summary>
    [ExcelColumn("Yetkili Kişi", 10, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Bayi yetkili kişisi", SampleValue = "Ahmet Yılmaz")]
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Çalışma saatleri (JSON formatında)
    /// Örnek: {"mon": "09:00-18:00", "tue": "09:00-18:00", ...}
    /// </summary>
    public string? WorkingHoursJson { get; set; }

    /// <summary>
    /// Bayi tipi (Perakende, Toptan, Franchise, Yetkili Bayi)
    /// </summary>
    [ExcelColumn("Bayi Tipi", 11, ColumnType = ExcelColumnTypes.Ids.Dropdown,
        Description = "Bayi tipi",
        DropdownOptions = "[\"Perakende\", \"Toptan\", \"Franchise\", \"Yetkili Bayi\"]",
        SampleValue = "Perakende")]
    public int DealerTypeId { get; set; } = DealerTypes.Ids.Retail;

    [ExcelColumn("Aktif", 12, ColumnType = ExcelColumnTypes.Ids.Boolean,
        Description = "Bayinin aktif olup olmadığı", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    [ExcelColumn("Notlar", 13, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Bayi hakkında notlar")]
    public string? Notes { get; set; }

    // Foreign Keys
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Navigation Properties
    public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
}
