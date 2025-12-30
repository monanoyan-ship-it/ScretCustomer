using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Müşteri", Description = "Müşteri firmaları için Excel import/export", IsAvailable = true)]
public class Customer : BaseEntity
{
    [ExcelColumn("Firma Adı", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Müşteri firmanın adı", SampleValue = "ABC Perakende A.Ş.")]
    public string CompanyName { get; set; } = string.Empty;

    [ExcelColumn("Vergi Numarası", 2, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Firmanın vergi numarası", SampleValue = "1234567890")]
    public string TaxNumber { get; set; } = string.Empty;

    [ExcelColumn("Telefon", 3, ColumnType = ExcelColumnType.Phone,
        Description = "Firma telefonu", SampleValue = "0212 123 4567")]
    public string? Phone { get; set; }

    [ExcelColumn("E-posta", 4, ColumnType = ExcelColumnType.Email,
        Description = "Firma e-posta adresi", SampleValue = "info@abc.com")]
    public string? Email { get; set; }

    [ExcelColumn("Adres", 5, ColumnType = ExcelColumnType.Text,
        Description = "Firma adresi", SampleValue = "Maslak, İstanbul")]
    public string? Address { get; set; }

    [ExcelColumn("Şehir", 6, ColumnType = ExcelColumnType.Text,
        Description = "Firma şehri", SampleValue = "İstanbul")]
    public string? City { get; set; }

    [ExcelColumn("Aktif", 7, ColumnType = ExcelColumnType.Boolean,
        Description = "Müşterinin aktif olup olmadığı", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    [ExcelColumn("Sözleşme Başlangıç", 8, ColumnType = ExcelColumnType.Date,
        Description = "Müşteri sözleşme başlangıç tarihi")]
    public DateTime? ContractStartDate { get; set; }

    [ExcelColumn("Sözleşme Bitiş", 9, ColumnType = ExcelColumnType.Date,
        Description = "Müşteri sözleşme bitiş tarihi")]
    public DateTime? ContractEndDate { get; set; }

    [ExcelColumn("Notlar", 10, ColumnType = ExcelColumnType.Text,
        Description = "Müşteri hakkında notlar")]
    public string? Notes { get; set; }

    // Navigation Properties
    public ICollection<CustomerPersonnel> Personnel { get; set; } = new List<CustomerPersonnel>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<CustomerTaskList> TaskLists { get; set; } = new List<CustomerTaskList>();

    /// <summary>
    /// Firma altındaki organizasyonlar
    /// </summary>
    public ICollection<CustomerOrganization> Organizations { get; set; } = new List<CustomerOrganization>();
}