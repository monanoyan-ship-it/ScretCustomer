using SecretCustomer.Core.DTOs.BankVisit;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

/// <summary>
/// Banka Gizli Müşteri Ziyareti Servisi (GBF - Gizli Banka Formu)
/// </summary>
public interface IBankVisitService
{
    /// <summary>
    /// ID'ye göre banka ziyaret detaylarını getirir
    /// </summary>
    Task<BankVisitDetailsDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// CustomerVisit ID'ye göre banka ziyaret detaylarını getirir
    /// </summary>
    Task<BankVisitDetailsDto?> GetByCustomerVisitIdAsync(Guid customerVisitId);

    /// <summary>
    /// Tüm banka ziyaretlerini getirir
    /// </summary>
    Task<IEnumerable<BankVisitSummaryDto>> GetAllAsync();

    /// <summary>
    /// Filtreli banka ziyaretlerini getirir
    /// </summary>
    Task<IEnumerable<BankVisitSummaryDto>> GetFilteredAsync(BankVisitFilterDto filter);

    /// <summary>
    /// Müşteriye ait banka ziyaretlerini getirir
    /// </summary>
    Task<IEnumerable<BankVisitSummaryDto>> GetByCustomerIdAsync(Guid customerId);

    /// <summary>
    /// Şubeye ait banka ziyaretlerini getirir
    /// </summary>
    Task<IEnumerable<BankVisitSummaryDto>> GetByBranchIdAsync(Guid branchId);

    /// <summary>
    /// Yeni banka ziyaret detayı oluşturur
    /// </summary>
    Task<BankVisitDetailsDto> CreateAsync(CreateBankVisitDetailsDto dto);

    /// <summary>
    /// Mevcut banka ziyaret detayını günceller
    /// </summary>
    Task<BankVisitDetailsDto> UpdateAsync(Guid id, UpdateBankVisitDetailsDto dto);

    /// <summary>
    /// Banka ziyaret detayını siler
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Banka ziyaret istatistiklerini getirir
    /// </summary>
    Task<BankVisitStatisticsDto> GetStatisticsAsync(BankVisitFilterDto? filter = null);

    /// <summary>
    /// Şube bazlı istatistikleri getirir
    /// </summary>
    Task<Dictionary<Guid, BankVisitStatisticsDto>> GetBranchStatisticsAsync(Guid customerId, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Excel export için veri getirir
    /// </summary>
    Task<IEnumerable<BankVisitDetailsDto>> GetForExportAsync(BankVisitFilterDto? filter = null);
}
