using SecretCustomer.Core.DTOs.Visit;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Interfaces.Services;

/// <summary>
/// Ziyaret detay servisi arayüzü
/// Sektör, alan tanımı ve değer yönetimi
/// </summary>
public interface IVisitDetailService
{
    // ===== SEKTÖR İŞLEMLERİ =====

    Task<IEnumerable<VisitSectorDto>> GetAllSectorsAsync();
    Task<VisitSectorDto?> GetSectorByIdAsync(Guid id);
    Task<VisitSectorDto?> GetSectorByCodeAsync(string code);
    Task<VisitSectorDto> CreateSectorAsync(SaveVisitSectorDto dto);
    Task<VisitSectorDto> UpdateSectorAsync(Guid id, SaveVisitSectorDto dto);
    Task DeleteSectorAsync(Guid id);

    // ===== ALAN TANIMI İŞLEMLERİ =====

    Task<IEnumerable<VisitFieldDefinitionDto>> GetAllFieldDefinitionsAsync();
    Task<IEnumerable<VisitFieldDefinitionDto>> GetFieldDefinitionsBySectorAsync(Guid? sectorId);
    Task<IEnumerable<VisitFieldDefinitionDto>> GetFieldDefinitionsForVisitAsync(Guid? sectorId);
    Task<Dictionary<VisitFieldCategory, List<VisitFieldDefinitionDto>>> GetFieldDefinitionsGroupedAsync(Guid? sectorId);
    Task<VisitFieldDefinitionDto?> GetFieldDefinitionByIdAsync(Guid id);
    Task<VisitFieldDefinitionDto> CreateFieldDefinitionAsync(SaveVisitFieldDefinitionDto dto);
    Task<VisitFieldDefinitionDto> UpdateFieldDefinitionAsync(Guid id, SaveVisitFieldDefinitionDto dto);
    Task DeleteFieldDefinitionAsync(Guid id);

    // ===== DEĞER İŞLEMLERİ =====

    Task<IEnumerable<VisitDetailValueDto>> GetVisitDetailsAsync(Guid customerVisitId);
    Task SaveVisitDetailsAsync(SaveVisitDetailsDto dto);
    Task UpdateFieldValueAsync(Guid customerVisitId, Guid fieldDefinitionId, object? value);
    Task<VisitDetailSummaryDto> GetVisitSummaryAsync(Guid customerVisitId);

    // ===== SORGULAMA VE İSTATİSTİK =====

    Task<IEnumerable<Guid>> FilterVisitsByFieldValueAsync(Guid fieldDefinitionId, object? value, string @operator = "eq");
    Task<FieldStatisticsDto> GetFieldStatisticsAsync(Guid fieldDefinitionId, Guid? projectId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<VisitComparisonDto>> CompareVisitsAsync(IEnumerable<Guid> customerVisitIds);
}
