namespace SecretCustomer.Core.Attributes;

/// <summary>
/// Entity'lerin Excel template için kullanılabilir olduğunu ve görünen adını belirler
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ExcelTemplateAttribute : Attribute
{
    /// <summary>
    /// Excel template listesinde görünecek Türkçe ad
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Bu entity Excel template için kullanılabilir mi?
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    public ExcelTemplateAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}
