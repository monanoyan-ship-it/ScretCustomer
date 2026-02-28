using SecretCustomer.Core.DTOs.EvaluationImport;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IEvaluationImportService
{
    Task<EvaluationImportSessionDto> UploadAndProcessAsync(byte[] fileContent, string fileName, int uploadedByUserId, int? customerId = null);
    Task<List<EvaluationImportSessionDto>> GetSessionsAsync();
    Task<EvaluationImportSessionDetailDto> GetSessionDetailAsync(int sessionId);
    Task<PagedUnmatchedItemResult> GetUnmatchedItemsAsync(int sessionId, int? itemTypeId = null, int page = 1, int pageSize = 50);
    Task<EvaluationImportUnmatchedItemDto> ResolveUnmatchedItemAsync(int itemId, ResolveUnmatchedItemDto dto, int userId);
    Task<PagedPendingRowResult> GetPendingRowsAsync(int sessionId, int? statusId = null, int page = 1, int pageSize = 50);
    Task<ImportResultDto> ImportResolvedRowsAsync(int sessionId, int userId);
    Task<List<CustomerPersonnelSearchDto>> SearchCustomerPersonnelAsync(int customerId, string query);
    Task<List<UserSearchDto>> SearchUsersAsync(string query);
    Task<List<ProjectSearchDto>> SearchProjectsAsync(int? customerId, string query);
    Task<List<CustomerSearchDto>> GetCustomersAsync();
    Task<List<ChecklistSearchDto>> GetChecklistsAsync();
}
