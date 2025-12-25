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
    Task<VisitSectorDto?> GetSectorByIdAsync(int id);
    Task<VisitSectorDto?> GetSectorByCodeAsync(string code);
    Task<VisitSectorDto> CreateSectorAsync(SaveVisitSectorDto dto);
    Task<VisitSectorDto> UpdateSectorAsync(int id, SaveVisitSectorDto dto);
    Task DeleteSectorAsync(int id);

    // ===== ALAN TANIMI İŞLEMLERİ =====

    Task<IEnumerable<VisitFieldDefinitionDto>> GetAllFieldDefinitionsAsync();
    Task<IEnumerable<VisitFieldDefinitionDto>> GetFieldDefinitionsBySectorAsync(int? sectorId);
    Task<IEnumerable<VisitFieldDefinitionDto>> GetFieldDefinitionsForVisitAsync(int? sectorId);
    Task<Dictionary<VisitFieldCategory, List<VisitFieldDefinitionDto>>> GetFieldDefinitionsGroupedAsync(int? sectorId);
    Task<VisitFieldDefinitionDto?> GetFieldDefinitionByIdAsync(int id);
    Task<VisitFieldDefinitionDto> CreateFieldDefinitionAsync(SaveVisitFieldDefinitionDto dto);
    Task<VisitFieldDefinitionDto> UpdateFieldDefinitionAsync(int id, SaveVisitFieldDefinitionDto dto);
    Task DeleteFieldDefinitionAsync(int id);

    // ===== DEĞER İŞLEMLERİ =====

    Task<IEnumerable<VisitDetailValueDto>> GetVisitDetailsAsync(int customerVisitId);
    Task SaveVisitDetailsAsync(SaveVisitDetailsDto dto);
    Task UpdateFieldValueAsync(int customerVisitId, int fieldDefinitionId, object? value);
    Task<VisitDetailSummaryDto> GetVisitSummaryAsync(int customerVisitId);

    // ===== SORGULAMA VE İSTATİSTİK =====

    Task<IEnumerable<int>> FilterVisitsByFieldValueAsync(int fieldDefinitionId, object? value, string @operator = "eq");
    Task<FieldStatisticsDto> GetFieldStatisticsAsync(int fieldDefinitionId, int? projectId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<VisitComparisonDto>> CompareVisitsAsync(IEnumerable<int> customerVisitIds);
}
