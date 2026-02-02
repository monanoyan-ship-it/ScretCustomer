using SecretCustomer.Core.DTOs.FieldWorker;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IFieldWorkerService
{
    /// <summary>
    /// Dashboard verilerini getir
    /// </summary>
    Task<FieldWorkerDashboardDto> GetDashboardAsync(int userId);

    /// <summary>
    /// Atanmış projeleri getir
    /// </summary>
    Task<List<AssignedProjectDto>> GetAssignedProjectsAsync(int userId);

    /// <summary>
    /// FieldWorker'ın erişebildiği bayileri getir
    /// </summary>
    Task<List<FieldWorkerDealerDto>> GetAccessibleDealersAsync(int userId, int? projectId = null);

    /// <summary>
    /// Atama için erişilebilir bayileri getir (ziyaret edilmemiş olanlar)
    /// </summary>
    Task<List<FieldWorkerDealerDto>> GetDealersForAssignmentAsync(int userId, int assignmentId);

    /// <summary>
    /// Tüm atamalardaki ziyaret bekleyen şubeleri getir (Dashboard için)
    /// </summary>
    Task<List<PendingDealerDto>> GetPendingDealersAsync(int userId);

    /// <summary>
    /// Ziyaretleri getir (sayfalı)
    /// </summary>
    Task<PagedVisitResult> GetVisitsAsync(int userId, VisitFilterDto filter);

    /// <summary>
    /// Ziyaret detayı getir
    /// </summary>
    Task<VisitSummaryDto?> GetVisitByIdAsync(int userId, int evaluationId);

    /// <summary>
    /// Yeni ziyaret oluştur
    /// </summary>
    Task<int> CreateVisitAsync(int userId, CreateVisitDto dto);

    /// <summary>
    /// Ziyareti güncelle (taslak)
    /// </summary>
    Task<bool> UpdateVisitAsync(int userId, int evaluationId, CreateVisitDto dto);

    /// <summary>
    /// VisitId üret (ZYR-{YIL}-{SIRA})
    /// </summary>
    Task<string> GenerateVisitIdAsync();
}
