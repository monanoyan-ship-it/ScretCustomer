using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Müşteri", Description = "Müşteri firmaları için Excel import/export", IsAvailable = true)]
public class Customer : BaseEntity
{
    [ExcelColumn("Müşteri Kodu", 1, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Müşteri takip kodu", SampleValue = "MUS-001")]
    public string? Code { get; set; }

    [ExcelColumn("Firma Adı", 2, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Müşteri firmanın adı", SampleValue = "ABC Perakende A.Ş.")]
    public string CompanyName { get; set; } = string.Empty;

    [ExcelColumn("Vergi Numarası", 3, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Firmanın vergi numarası", SampleValue = "1234567890")]
    public string TaxNumber { get; set; } = string.Empty;

    [ExcelColumn("Telefon", 4, ColumnType = ExcelColumnTypes.Ids.Phone,
        Description = "Firma telefonu", SampleValue = "0212 123 4567")]
    public string? Phone { get; set; }

    [ExcelColumn("E-posta", 5, ColumnType = ExcelColumnTypes.Ids.Email,
        Description = "Firma e-posta adresi", SampleValue = "info@abc.com")]
    public string? Email { get; set; }

    [ExcelColumn("Adres", 6, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Firma adresi", SampleValue = "Maslak, İstanbul")]
    public string? Address { get; set; }

    [ExcelColumn("Şehir", 7, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Firma şehri", SampleValue = "İstanbul")]
    public string? City { get; set; }

    [ExcelColumn("Aktif", 8, ColumnType = ExcelColumnTypes.Ids.Boolean,
        Description = "Müşterinin aktif olup olmadığı", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    [ExcelColumn("Sözleşme Başlangıç", 9, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Müşteri sözleşme başlangıç tarihi")]
    public DateTime? ContractStartDate { get; set; }

    [ExcelColumn("Sözleşme Bitiş", 10, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Müşteri sözleşme bitiş tarihi")]
    public DateTime? ContractEndDate { get; set; }

    [ExcelColumn("Notlar", 11, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Müşteri hakkında notlar")]
    public string? Notes { get; set; }

    /// <summary>
    /// Hedef değerlendirme sayısı
    /// </summary>
    [ExcelColumn("Hedef Sayı", 12, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Hedef değerlendirme sayısı", SampleValue = "100")]
    public int? TargetCount { get; set; }

    /// <summary>
    /// Günlük kota
    /// </summary>
    [ExcelColumn("Günlük Kota", 13, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Günlük maksimum değerlendirme sayısı")]
    public int? DailyQuota { get; set; }

    /// <summary>
    /// Haftalık kota
    /// </summary>
    [ExcelColumn("Haftalık Kota", 14, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Haftalık maksimum değerlendirme sayısı")]
    public int? WeeklyQuota { get; set; }

    /// <summary>
    /// Aylık kota
    /// </summary>
    [ExcelColumn("Aylık Kota", 15, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Aylık maksimum değerlendirme sayısı")]
    public int? MonthlyQuota { get; set; }

    // Navigation Properties
    public ICollection<CustomerPersonnel> Personnel { get; set; } = new List<CustomerPersonnel>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<CustomerTaskList> TaskLists { get; set; } = new List<CustomerTaskList>();

    /// <summary>
    /// Firma altındaki organizasyonlar
    /// </summary>
    public ICollection<CustomerOrganization> Organizations { get; set; } = new List<CustomerOrganization>();

    /// <summary>
    /// Firma altındaki bayiler (FieldWorker ziyaretleri için)
    /// </summary>
    public ICollection<Dealer> Dealers { get; set; } = new List<Dealer>();
}