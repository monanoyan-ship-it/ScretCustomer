using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Şube", Description = "Şubeler için Excel import/export", IsAvailable = true)]
public class Branch : BaseEntity
{
    [ExcelColumn("Şube Adı", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Şubenin adı", SampleValue = "İstanbul Merkez Şube")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Şube Kodu", 2, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Şubenin benzersiz kodu", SampleValue = "IST001")]
    public string Code { get; set; } = string.Empty;

    [ExcelColumn("Adres", 3, ColumnType = ExcelColumnType.Text,
        Description = "Şubenin fiziksel adresi", SampleValue = "Atatürk Cad. No:123")]
    public string? Address { get; set; }

    [ExcelColumn("Şehir", 4, ColumnType = ExcelColumnType.Text,
        Description = "Şubenin bulunduğu şehir", SampleValue = "İstanbul")]
    public string? City { get; set; }

    [ExcelColumn("Bölge", 5, ColumnType = ExcelColumnType.Text,
        Description = "Şubenin bölgesi", SampleValue = "Marmara")]
    public string? Region { get; set; }

    [ExcelColumn("Aktif", 6, ColumnType = ExcelColumnType.Boolean,
        Description = "Şubenin aktif olup olmadığı", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}
