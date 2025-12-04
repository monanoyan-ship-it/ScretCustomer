namespace SecretCustomer.Core.Entities;

public class ExcelTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;  // Örn: "Müşteri Listesi Import"
    public string Description { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;  // Hangi entity için (User, Branch, vb.)
    public bool IsActive { get; set; } = true;

    // Sheet adı (Excel'de)
    public string SheetName { get; set; } = "Sheet1";

    // Başlık satırı var mı?
    public bool HasHeader { get; set; } = true;

    // Navigation properties
    public ICollection<ExcelColumn> Columns { get; set; } = new List<ExcelColumn>();
}
