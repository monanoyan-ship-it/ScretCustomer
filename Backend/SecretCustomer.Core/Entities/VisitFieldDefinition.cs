using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Ziyaret alan tanımları
/// Her sektör için dinamik olarak tanımlanır
/// </summary>
public class VisitFieldDefinition : BaseEntity
{
    /// <summary>
    /// Bağlı olduğu sektör (null ise tüm sektörler için geçerli - ortak alan)
    /// </summary>
    public Guid? SectorId { get; set; }
    public VisitSector? Sector { get; set; }

    /// <summary>
    /// Alan kodu (unique per sector, örn: "entry_time", "staff_rating")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Alan adı (Türkçe)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Alan açıklaması
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Veri tipi
    /// </summary>
    public VisitFieldType FieldType { get; set; }

    /// <summary>
    /// Kategori
    /// </summary>
    public VisitFieldCategory Category { get; set; }

    /// <summary>
    /// Zorunlu alan mı?
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Rating tipi için maksimum değer (5 veya 10)
    /// </summary>
    public int? MaxRating { get; set; }

    /// <summary>
    /// String tipi için maksimum karakter
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Int/Decimal tipi için minimum değer
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// Int/Decimal tipi için maksimum değer
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Placeholder metni
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Yardım metni
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Sıralama
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<VisitDetailValue> DetailValues { get; set; } = new List<VisitDetailValue>();
}
