namespace SecretCustomer.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ExcelColumnAttribute : Attribute
{
    /// <summary>
    /// Excel'de görünecek sütun başlığı
    /// </summary>
    public string ColumnName { get; set; }

    /// <summary>
    /// Sütun sırası
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Zorunlu alan mı?
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Sütun tipi (ExcelColumnTypes.Ids kullanın)
    /// </summary>
    public int ColumnType { get; set; } = 1; // ExcelColumnTypes.Ids.Text

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Örnek değer
    /// </summary>
    public string? SampleValue { get; set; }

    /// <summary>
    /// Dropdown seçenekleri (virgülle ayrılmış)
    /// </summary>
    public string? DropdownOptions { get; set; }

    /// <summary>
    /// Validation kuralları (JSON string)
    /// </summary>
    public string? ValidationRules { get; set; }

    public ExcelColumnAttribute(string columnName, int order)
    {
        ColumnName = columnName;
        Order = order;
    }
}
