using SecretCustomer.Core.DTOs.SupportRequest;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ISupportRequestService
{
    /// <summary>
    /// Tüm destek taleplerini filtreli getirir (Admin için)
    /// </summary>
    Task<(List<SupportRequestListDto> Items, int TotalCount)> GetAllAsync(SupportRequestFilterDto filter);

    /// <summary>
    /// ID ile destek talebini getirir
    /// </summary>
    Task<SupportRequestDetailDto?> GetByIdAsync(int id);

    /// <summary>
    /// Kullanıcının kendi taleplerini getirir
    /// </summary>
    Task<List<SupportRequestListDto>> GetByUserAsync(int? userId, int? customerPersonnelId);

    /// <summary>
    /// Kullanıcının talep özetini getirir
    /// </summary>
    Task<MySupportRequestSummaryDto> GetMySummaryAsync(int? userId, int? customerPersonnelId);

    /// <summary>
    /// Yeni destek talebi oluşturur
    /// </summary>
    Task<SupportRequestDetailDto> CreateAsync(CreateSupportRequestDto dto, int? userId, int? customerPersonnelId);

    /// <summary>
    /// Admin destek talebine cevap verir
    /// </summary>
    Task<SupportRequestDetailDto> RespondAsync(RespondSupportRequestDto dto, int adminUserId);

    /// <summary>
    /// Destek talebi durumunu günceller
    /// </summary>
    Task<SupportRequestDetailDto> UpdateStatusAsync(UpdateSupportRequestStatusDto dto);

    /// <summary>
    /// Kullanıcı cevabı okudu olarak işaretler
    /// </summary>
    Task MarkAsReadAsync(int id, int? userId, int? customerPersonnelId);

    /// <summary>
    /// Destek talebini siler (soft delete)
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Durum sayılarını getirir
    /// </summary>
    Task<(int Pending, int InProgress, int Resolved, int Closed)> GetStatusCountsAsync();
}
