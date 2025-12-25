using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

public class ExcelColumn : BaseEntity
{
    public int ExcelTemplateId { get; set; }
    public ExcelTemplate ExcelTemplate { get; set; } = null!;

    public string ColumnName { get; set; } = string.Empty;  // Excel'de görünecek başlık
    public string PropertyName { get; set; } = string.Empty;  // Database field adı (örn: "FirstName")
    public ExcelColumnType ColumnType { get; set; }
    public int Order { get; set; }  // Kolon sırası
    public bool IsRequired { get; set; } = false;

    // Validation kuralları (JSON)
    public string? ValidationRules { get; set; }  // {"minLength": 3, "maxLength": 50}

    // Dropdown için seçenekler (JSON)
    public string? DropdownOptions { get; set; }  // ["Option1", "Option2"]

    // Örnek değer
    public string? SampleValue { get; set; }

    // Açıklama (kullanıcıya gösterilecek)
    public string? Description { get; set; }
}
