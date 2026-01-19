using SecretCustomer.Core.DTOs.AI;

namespace SecretCustomer.Core.Interfaces.Services;

/// <summary>
/// AI destekli rapor oluşturma servisi
/// </summary>
public interface IAIReportService
{
    /// <summary>
    /// Müşteri için AI raporu oluşturur
    /// </summary>
    Task<AIReportResponseDto> GenerateReportAsync(AIReportRequestDto request);

    /// <summary>
    /// Rapor verilerini toplar (AI'ya gönderilmeden önce)
    /// </summary>
    Task<AIReportDataDto> CollectReportDataAsync(AIReportRequestDto request);
}
