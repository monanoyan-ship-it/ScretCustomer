using SecretCustomer.Core.DTOs.Checklist;
using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IChecklistService
{
    Task<ChecklistDto?> GetByIdAsync(int id);
    Task<IEnumerable<ChecklistDto>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<ChecklistDto>> GetFilteredAsync(string? searchText = null, int? customerId = null, int? customerOrganizationId = null, bool includeInactive = false);
    /// <summary>
    /// Liste görünümü için optimize edilmiş method (Questions yüklemez)
    /// </summary>
    Task<IEnumerable<ChecklistListDto>> GetListAsync(string? searchText = null, int? customerId = null, int? customerOrganizationId = null, bool includeInactive = false);
    /// <summary>
    /// Liste görünümü için - Filter DTO ile (çoklu filtre destekli)
    /// </summary>
    Task<IEnumerable<ChecklistListDto>> GetListAsync(ChecklistFilterDto filter);
    Task<ChecklistDto> CreateAsync(CreateChecklistDto dto);
    Task<ChecklistDto> UpdateAsync(UpdateChecklistDto dto);
    Task<bool> DeleteAsync(int id);
    Task<ChecklistDto> CloneChecklistAsync(int id, string newName);
    Task<ExcelExportDto?> ExportChecklistToExcelAsync(int id);
    Task<List<string>> GetQuestionGroupsAsync(int checklistId);
}
