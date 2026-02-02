using System.ComponentModel.DataAnnotations;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.DealerRequest;

/// <summary>
/// Bayi talebi listesi için DTO
/// </summary>
public class DealerRequestDto
{
    public int Id { get; set; }

    // Müşteri bilgisi
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }

    // Talep eden FieldWorker
    public int RequestedByUserId { get; set; }
    public string? RequestedByUserName { get; set; }

    // Talep tipi ve durumu
    public int RequestTypeId { get; set; }
    public string RequestTypeName => CustomerDealerRequestTypes.GetById(RequestTypeId)?.NameResourceKey ?? "Bilinmiyor";
    public int StatusId { get; set; }
    public string StatusName => CustomerDealerRequestStatuses.GetById(StatusId)?.NameResourceKey ?? "Bilinmiyor";

    // Mevcut bayi (güncelleme talebi için)
    public int? CustomerDealerId { get; set; }
    public string? CustomerDealerName { get; set; }

    // Talep detayları
    public string RequestDataJson { get; set; } = "{}";

    // Admin yanıtı
    public string? AdminResponse { get; set; }
    public int? ProcessedByUserId { get; set; }
    public string? ProcessedByUserName { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Yeni bayi talebi oluşturma DTO (FieldWorker için)
/// </summary>
public class CreateDealerRequestDto
{
    [Required(ErrorMessage = "Müşteri seçimi zorunludur")]
    public int CustomerId { get; set; }

    public int RequestTypeId { get; set; } = CustomerDealerRequestTypes.Ids.NewDealer;

    /// <summary>
    /// Güncelleme talebi için mevcut bayi ID
    /// </summary>
    public int? CustomerDealerId { get; set; }

    /// <summary>
    /// Talep edilen bilgiler (JSON)
    /// </summary>
    [Required(ErrorMessage = "Talep bilgileri zorunludur")]
    public string RequestDataJson { get; set; } = "{}";
}

/// <summary>
/// Talep onaylama/reddetme DTO (Admin için)
/// </summary>
public class ProcessDealerRequestDto
{
    /// <summary>
    /// true = Onayla, false = Reddet
    /// </summary>
    [Required]
    public bool Approve { get; set; }

    /// <summary>
    /// Admin yanıtı/açıklaması
    /// </summary>
    public string? AdminResponse { get; set; }
}

/// <summary>
/// Talep filtre DTO
/// </summary>
public class DealerRequestFilterDto
{
    public string? SearchTerm { get; set; }
    public int? CustomerId { get; set; }
    public int? RequestedByUserId { get; set; }
    public int? RequestTypeId { get; set; }
    public int? StatusId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Sayfalı talep sonucu
/// </summary>
public class PagedDealerRequestResult
{
    public List<DealerRequestDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    // İstatistikler
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
}

/// <summary>
/// Bayi talebi için gönderilen veri yapısı
/// </summary>
public class DealerRequestData
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
    public string? WorkingHoursJson { get; set; }
    public int DealerTypeId { get; set; } = CustomerDealerTypes.Ids.Retail;
    public string? Notes { get; set; }
}
