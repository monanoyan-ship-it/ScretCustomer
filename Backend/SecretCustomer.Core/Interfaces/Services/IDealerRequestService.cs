using SecretCustomer.Core.DTOs.DealerRequest;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IDealerRequestService
{
    /// <summary>
    /// Tüm talepleri getir (Admin için)
    /// </summary>
    Task<PagedDealerRequestResult> GetAllAsync(DealerRequestFilterDto filter);

    /// <summary>
    /// Talep detayı
    /// </summary>
    Task<DealerRequestDto?> GetByIdAsync(int id);

    /// <summary>
    /// FieldWorker'ın kendi taleplerini getir
    /// </summary>
    Task<List<DealerRequestDto>> GetMyRequestsAsync(int userId);

    /// <summary>
    /// Yeni talep oluştur (FieldWorker için)
    /// </summary>
    Task<DealerRequestDto> CreateAsync(CreateDealerRequestDto dto, int requestedByUserId);

    /// <summary>
    /// Talebi onayla veya reddet (Admin için)
    /// </summary>
    Task<DealerRequestDto?> ProcessAsync(int id, ProcessDealerRequestDto dto, int processedByUserId);

    /// <summary>
    /// Talebi sil (soft delete)
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Bekleyen talep sayısı
    /// </summary>
    Task<int> GetPendingCountAsync();
}
