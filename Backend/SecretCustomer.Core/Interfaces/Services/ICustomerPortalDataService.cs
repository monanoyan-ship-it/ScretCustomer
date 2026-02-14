namespace SecretCustomer.Core.Interfaces.Services;

public interface ICustomerPortalDataService
{
    // ===== PROJECTS =====
    Task<object> GetProjectsAsync(int customerId, int? projectTypeId, List<int>? allowedPersonnelIds);

    // ===== ORGANIZATIONS =====
    Task<object> GetOrganizationsAsync(int customerId, List<int>? allowedOrgIds);
    Task<object> GetOrganizationsMonthlyTrendAsync(int customerId, List<int>? allowedOrgIds, List<int>? organizationIds, DateTime? startDate, DateTime? endDate);

    // ===== SUPERVISORS =====
    Task<object> GetSupervisorsAsync(int customerId, List<int>? allowedOrgIds, List<int>? organizationIds, string? searchText);
    Task<object?> GetSupervisorMonthlyTrendAsync(int customerId, int supervisorId);

    // ===== DASHBOARD =====
    Task<object> GetDashboardStatsAsync(int customerId, List<int>? allowedPersonnelIds);
    Task<object> GetMonthlyTrendAsync(int customerId, List<int>? allowedPersonnelIds, int? projectId, int? projectTypeId, DateTime? startDate, DateTime? endDate);
    Task<object> GetMonthlyTrendByTypeAsync(int customerId, List<int>? allowedPersonnelIds, DateTime? startDate, DateTime? endDate);
    Task<object> GetQuestionGroupTrendAsync(int customerId, List<int>? allowedPersonnelIds, List<int>? projectIds, DateTime? startDate, DateTime? endDate);
    Task<object> GetQuestionTrendAsync(int customerId, List<int>? allowedPersonnelIds, List<int>? projectIds, string? groupName, DateTime? startDate, DateTime? endDate);
    Task<object> GetScoreDistributionAsync(int customerId, List<int>? allowedPersonnelIds, int? projectId, DateTime? startDate, DateTime? endDate);
    Task<object?> GetScoreDistributionEvaluationsAsync(int customerId, List<int>? allowedPersonnelIds, string category, int projectTypeId, DateTime? startDate, DateTime? endDate, int page, int pageSize);
    Task<(byte[] FileContent, string FileName)?> ExportScoreDistributionEvaluationsAsync(int customerId, List<int>? allowedPersonnelIds, string category, int projectTypeId, DateTime? startDate, DateTime? endDate);

    // ===== EVALUATIONS =====
    Task<object> GetRecentEvaluationsAsync(int customerId, List<int>? allowedPersonnelIds, int count);
    Task<object> GetEvaluationsAsync(int customerId, string? role, int? personnelId, List<int>? allowedPersonnelIds, int page, int pageSize, int? projectId = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<(byte[] FileContent, string FileName)> ExportAllEvaluationsToExcelAsync(int customerId, string? role, int? personnelId, List<int>? allowedPersonnelIds, int? projectId, DateTime? startDate, DateTime? endDate);
    Task<object> GetInternalEvaluationsAsync(int customerId, string? role, int? personnelId,
        int? page, int? pageSize, string? search, DateTime? startDate, DateTime? endDate,
        List<int>? projectIds, List<string>? evaluatorNames, List<string>? personnelNames,
        List<int>? organizationIds, List<string>? callIds);
    Task<object> GetExternalEvaluationsAsync(int customerId, string? role, int? personnelId,
        int? page, int? pageSize, string? search, DateTime? startDate, DateTime? endDate,
        List<int>? projectIds, List<string>? personnelNames, List<int>? organizationIds,
        List<string>? callIds, decimal? minScore, decimal? maxScore);

    // ===== EVALUATION DETAIL =====
    Task<(bool IsAuthorized, int? EvaluatedCustomerPersonnelId)?> CheckEvaluationAccessAsync(int evaluationId, int customerId, List<int>? allowedPersonnelIds, int? personnelId);
    Task<object> GetEvaluationAttachmentsAsync(int evaluationId);

    // ===== SAVED FILTERS =====
    Task<object> GetSavedFiltersAsync(int customerId, string page);
    Task<int> SaveFilterAsync(int customerId, string name, string pageName, string filterDataJson);
    Task<bool> DeleteSavedFilterAsync(int customerId, int id);

    // ===== REPORTS =====
    Task<object> GetProjectPerformanceAsync(int customerId, List<int>? allowedPersonnelIds, DateTime? startDate, DateTime? endDate, List<int>? projectIds, List<int>? organizationIds, bool? isInternal);
    Task<object> GetReportSummaryAsync(int customerId, List<int>? allowedPersonnelIds, DateTime? startDate, DateTime? endDate, List<int>? projectIds, List<int>? organizationIds, bool? isInternal);
    Task<object> GetEvaluationsByScoreRangeAsync(int customerId, List<int>? allowedPersonnelIds, DateTime? startDate, DateTime? endDate, List<int>? projectIds, List<int>? organizationIds, decimal minScore, decimal maxScore, bool? isInternal);
    Task<object> GetReportMonthlyTrendAsync(int customerId, List<int>? allowedPersonnelIds, DateTime? startDate, DateTime? endDate, List<int>? projectIds, List<int>? organizationIds, bool? isInternal);

    // ===== VALIDATION HELPERS =====
    Task<bool> ValidatePersonnelBelongsToCustomerAsync(int personnelId, int customerId);
    Task<(string FirstName, string LastName)?> GetPersonnelNameAsync(int personnelId, int customerId);
    Task<bool> ValidateSurveyProjectBelongsToCustomerAsync(int projectId, int customerId);
    Task<bool> ValidateDealerBelongsToCustomerAsync(int dealerId, int customerId);

    // ===== PERFORMANCE BY PERIOD =====
    Task<object> GetPerformanceByPeriodAsync(int customerId, List<int>? allowedPersonnelIds, List<int>? allowedOrgIds, List<int>? projectIds, List<int>? organizationIds, DateTime? startDate, DateTime? endDate);
    Task<(byte[] FileContent, string FileName)?> ExportPerformanceByPeriodAsync(int customerId, List<int>? allowedPersonnelIds, List<int>? allowedOrgIds, List<int>? projectIds, List<int>? organizationIds, DateTime? startDate, DateTime? endDate);

    // ===== TRAINING VIDEOS =====
    Task<object> GetMyTrainingsAsync(int personnelId);
    Task<object?> UpdateMyTrainingProgressAsync(int participantId, int personnelId, int watchedSeconds, bool isCompleted);
    Task<object?> StartWatchSessionAsync(int participantId, int personnelId);
    Task<object> GetStaffTrainingsAsync(int customerId, List<int>? allowedPersonnelIds);

    // ===== DRAFT DELETION =====
    Task<(bool Success, string? ErrorMessage, int? StatusCode)?> DeleteDraftAsync(int evaluationId, int customerId, string? role, int? personnelId);
    Task<(bool Success, string? ErrorMessage, int? StatusCode)?> DeleteInternalDraftAsync(int evaluationId, int customerId);

    // ===== ENNEAGRAM EXPORT =====
    Task<List<int>?> GetEnneagramProjectIdsForCustomerAsync(int customerId, int? projectId);

    // ===== ADMIN CUSTOMER SELECTION =====
    Task<object> GetCustomersForAdminAsync(string? search, bool includeInactive);
    Task<(int Id, string CompanyName, string? Code)?> GetCustomerByIdAsync(int customerId);

    // ===== ROLE-BASED ACCESS HELPERS =====
    Task<List<int>?> GetAllowedPersonnelIdsAsync(string? role, int? personnelId);
    Task<List<int>?> GetAllowedOrganizationIdsAsync(string? role, int? personnelId);

    // ===== GÖLGE MÜŞTERİ =====
    Task<object> GetGmAramalarAsync(int customerId, int? donemId);
    Task<object> GetGmDonemlerAsync(int customerId);
}
