namespace SecretCustomer.Core.Entities;

/// <summary>
/// Ziyaret sektörü (Banka, Kafe, Otel, Market vb.)
/// Dinamik olarak yönetilir
/// </summary>
public class VisitSector : BaseEntity
{
    /// <summary>
    /// Sektör kodu (unique, örn: "bank", "cafe", "hotel")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Sektör adı (Türkçe)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Sektör açıklaması
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Sektör ikonu (FontAwesome veya Bootstrap icon class)
    /// </summary>
    public string? IconClass { get; set; }

    /// <summary>
    /// Sıralama
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<VisitFieldDefinition> FieldDefinitions { get; set; } = new List<VisitFieldDefinition>();
}
