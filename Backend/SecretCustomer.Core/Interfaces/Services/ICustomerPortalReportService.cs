using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ICustomerPortalReportService
{
    // ===== SURVEY =====

    Task<List<SurveyProjectListItemDto>> GetSurveyProjectsAsync(int customerId);

    Task<List<RecentSurveyResponseDto>> GetRecentSurveyResponsesAsync(
        int customerId, int count = 20, int? projectId = null,
        DateTime? startDate = null, DateTime? endDate = null);

    Task<SurveyProjectDetailDto?> GetSurveyProjectDetailAsync(int customerId, int projectId);

    Task<SurveyQuestionScoreDistributionResultDto> GetSurveyQuestionScoreDistributionAsync(
        int customerId, int? projectId = null);

    Task<SurveyQuestionScoreDetailResultDto?> GetSurveyQuestionScoreDetailAsync(int customerId, int projectId);

    // ===== ENNEAGRAM =====

    Task<List<EnneagramProjectListItemDto>> GetEnneagramProjectsAsync(int customerId);

    Task<EnneagramResultsPagedDto> GetEnneagramResultsAsync(
        int customerId, int? projectId = null, string? searchTerm = null,
        int page = 1, int pageSize = 50);

    Task<EnneagramResultDetailDto?> GetEnneagramResultDetailAsync(int customerId, int evaluationId);

    Task<EnneagramDistributionResultDto?> GetEnneagramDistributionAsync(int customerId, int projectId);
}
