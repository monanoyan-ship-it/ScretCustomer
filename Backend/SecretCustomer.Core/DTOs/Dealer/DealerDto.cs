using System.ComponentModel.DataAnnotations;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.Dealer;

/// <summary>
/// Bayi listesi için DTO
/// </summary>
public class DealerDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
    public int? ContactPersonnelId { get; set; }
    public string? ContactPersonnelName { get; set; }
    public string? WorkingHoursJson { get; set; }
    public int DealerTypeId { get; set; }
    public string DealerTypeText => CustomerDealerTypes.GetById(DealerTypeId)?.NameResourceKey ?? "Bilinmiyor";
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    // Customer info
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }

    // Stats
    public int EvaluationCount { get; set; }
    public decimal? AverageScore { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Bayi oluşturma DTO
/// </summary>
public class CreateDealerDto
{
    [Required(ErrorMessage = "Bayi adı zorunludur")]
    [StringLength(200, ErrorMessage = "Bayi adı en fazla 200 karakter olabilir")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir")]
    public string? Address { get; set; }

    [StringLength(100, ErrorMessage = "Şehir en fazla 100 karakter olabilir")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "İlçe en fazla 100 karakter olabilir")]
    public string? District { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir")]
    public string? Email { get; set; }

    [StringLength(100, ErrorMessage = "Yetkili kişi en fazla 100 karakter olabilir")]
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Yetkili kişi - CustomerPersonnel seçimi (opsiyonel)
    /// </summary>
    public int? ContactPersonnelId { get; set; }

    public string? WorkingHoursJson { get; set; }

    public int DealerTypeId { get; set; } = CustomerDealerTypes.Ids.Retail;

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    [Required(ErrorMessage = "Müşteri seçimi zorunludur")]
    public int CustomerId { get; set; }
}

/// <summary>
/// Bayi güncelleme DTO
/// </summary>
public class UpdateDealerDto
{
    [Required(ErrorMessage = "Bayi adı zorunludur")]
    [StringLength(200, ErrorMessage = "Bayi adı en fazla 200 karakter olabilir")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir")]
    public string? Address { get; set; }

    [StringLength(100, ErrorMessage = "Şehir en fazla 100 karakter olabilir")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "İlçe en fazla 100 karakter olabilir")]
    public string? District { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir")]
    public string? Email { get; set; }

    [StringLength(100, ErrorMessage = "Yetkili kişi en fazla 100 karakter olabilir")]
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Yetkili kişi - CustomerPersonnel seçimi (opsiyonel)
    /// </summary>
    public int? ContactPersonnelId { get; set; }

    public string? WorkingHoursJson { get; set; }

    public int DealerTypeId { get; set; } = CustomerDealerTypes.Ids.Retail;

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}

/// <summary>
/// Bayi filtre DTO
/// </summary>
public class DealerFilterDto
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }

    // Çoklu parametreler
    public List<int>? CustomerIds { get; set; }
    public List<int>? DealerTypeIds { get; set; }
    public List<string>? Cities { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Sayfalı bayi sonucu
/// </summary>
public class PagedDealerResult
{
    public List<DealerDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
