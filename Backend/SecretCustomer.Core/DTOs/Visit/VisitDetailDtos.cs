using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.Visit;

// ===== SEKTÖR DTO'LARI =====

/// <summary>
/// Sektör listesi DTO
/// </summary>
public class VisitSectorDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int FieldCount { get; set; }
}

/// <summary>
/// Sektör oluşturma/güncelleme DTO
/// </summary>
public class SaveVisitSectorDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// ===== ALAN TANIMI DTO'LARI =====

/// <summary>
/// Alan tanımı listesi DTO
/// </summary>
public class VisitFieldDefinitionDto
{
    public int Id { get; set; }
    public int? SectorId { get; set; }
    public string? SectorName { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public VisitFieldType FieldType { get; set; }
    public string FieldTypeName { get; set; } = string.Empty;
    public VisitFieldCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int? MaxRating { get; set; }
    public int? MaxLength { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsCommon => SectorId == null;
}

/// <summary>
/// Alan tanımı oluşturma/güncelleme DTO
/// </summary>
public class SaveVisitFieldDefinitionDto
{
    public int? SectorId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public VisitFieldType FieldType { get; set; }
    public VisitFieldCategory Category { get; set; }
    public bool IsRequired { get; set; }
    public int? MaxRating { get; set; }
    public int? MaxLength { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// ===== DEĞER DTO'LARI =====

/// <summary>
/// Ziyaret detay değeri DTO
/// </summary>
public class VisitDetailValueDto
{
    public int Id { get; set; }
    public int CustomerVisitId { get; set; }
    public int FieldDefinitionId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public VisitFieldCategory Category { get; set; }
    public VisitFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public int? MaxRating { get; set; }

    // Değer
    public object? Value { get; set; }

    // Typed değerler
    public int? IntValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public bool? BoolValue { get; set; }
    public string? StringValue { get; set; }
    public DateTime? DateTimeValue { get; set; }
}

/// <summary>
/// Ziyaret detayları kaydetme DTO
/// </summary>
public class SaveVisitDetailsDto
{
    public int CustomerVisitId { get; set; }
    public List<VisitDetailValueInputDto> Values { get; set; } = new();
}

/// <summary>
/// Ziyaret detay değeri input DTO
/// </summary>
public class VisitDetailValueInputDto
{
    public int FieldDefinitionId { get; set; }
    public object? Value { get; set; }
}

// ===== ÖZET VE İSTATİSTİK DTO'LARI =====

/// <summary>
/// Ziyaret detay özeti DTO
/// </summary>
public class VisitDetailSummaryDto
{
    public int CustomerVisitId { get; set; }
    public int? SectorId { get; set; }
    public string SectorName { get; set; } = string.Empty;
    public int TotalFields { get; set; }
    public int FilledFields { get; set; }
    public double CompletionPercentage { get; set; }
    public Dictionary<VisitFieldCategory, CategorySummaryDto> Categories { get; set; } = new();
}

/// <summary>
/// Kategori özeti DTO
/// </summary>
public class CategorySummaryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int TotalFields { get; set; }
    public int FilledFields { get; set; }
    public double? AverageRating { get; set; }
}

/// <summary>
/// Alan istatistikleri DTO
/// </summary>
public class FieldStatisticsDto
{
    public int FieldDefinitionId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public VisitFieldType FieldType { get; set; }
    public int TotalRecords { get; set; }

    // Sayısal alanlar için
    public double? Average { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public double? StandardDeviation { get; set; }

    // Boolean alanlar için
    public int? TrueCount { get; set; }
    public int? FalseCount { get; set; }
    public double? TruePercentage { get; set; }

    // Distribution (rating alanları için)
    public Dictionary<int, int>? Distribution { get; set; }
}

/// <summary>
/// Ziyaret karşılaştırma DTO
/// </summary>
public class VisitComparisonDto
{
    public int FieldDefinitionId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public List<VisitComparisonValueDto> Values { get; set; } = new();
    public double? Average { get; set; }
    public object? Min { get; set; }
    public object? Max { get; set; }
}

/// <summary>
/// Karşılaştırma değeri DTO
/// </summary>
public class VisitComparisonValueDto
{
    public int CustomerVisitId { get; set; }
    public object? Value { get; set; }
}
