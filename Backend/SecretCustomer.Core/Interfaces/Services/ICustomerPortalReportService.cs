using SecretCustomer.Core.DTOs.Auth;
using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ICustomerPortalReportService
{
    // ===== EVALUATION DETAIL =====
    Task<EvaluationDetailReportDto?> GetEvaluationDetailAsync(int evaluationId);
    Task<ExcelExportDto?> ExportEvaluationDetailToExcelAsync(int evaluationId, bool excludeEvaluatorInfo = false);

    // ===== PENALTIES =====
    Task<PenaltyReportResultDto> GetPenaltiesReportAsync(PenaltyFilterDto filter);
    Task<ExcelExportDto> ExportPenaltiesToExcelAsync(PenaltyFilterDto filter, bool excludeEvaluator = false);

    // ===== PERSONNEL =====
    Task<IEnumerable<PersonnelListItemDto>> GetEvaluatedPersonnelListAsync(int? customerId = null, int? organizationId = null);
    Task<PersonnelReportCardDto?> GetPersonnelReportCardAsync(PersonnelReportCardFilterDto filter);
    Task<ExcelExportDto> ExportPersonnelReportCardToExcelAsync(PersonnelReportCardFilterDto filter);
    Task<ExcelExportDto> ExportPersonnelReportCardToWordAsync(PersonnelReportCardFilterDto filter);

    // ===== SUGGESTIONS =====
    Task<SuggestionsReportResultDto> GetSuggestionsReportAsync(SuggestionsFilterDto filter);
    Task<IEnumerable<QuestionSuggestionSummaryDto>> GetTopSuggestedQuestionsAsync(SuggestionsFilterDto filter, int top = 10);
    Task<IEnumerable<SubCriteriaSummaryDto>> GetTopSubCriteriaAsync(SuggestionsFilterDto filter, int top = 10);
    Task<ExcelExportDto> ExportSuggestionsToExcelAsync(SuggestionsFilterDto filter, bool excludeEvaluator = false);

    // ===== EXCEL REPORTS =====
    Task<ExcelExportDto> ExportQuestionGroupAverageReportAsync(ReportFilterDto filter);
    Task<ExcelExportDto> ExportCustomerEvaluationReportAsync(ReportFilterDto filter);
    Task<ExcelExportDto> ExportInternalEvaluationReportAsync(ReportFilterDto filter);
    Task<ExcelExportDto> ExportProjectPerformanceReportAsync(ReportFilterDto filter);
    Task<ExcelExportDto> ExportUnscoredQuestionsReportAsync(ReportFilterDto filter);

    // ===== SURVEY EXPORTS =====
    Task<ExcelExportDto?> ExportSurveyResponsesToExcelAsync(int? projectId = null);
    Task<ExcelExportDto?> ExportSurveyGroupScoresToExcelAsync(int projectId);
    Task<ExcelExportDto?> ExportSurveyQuestionStatsToExcelAsync(int projectId);
    Task<ExcelExportDto?> ExportSurveyDetailReportToExcelAsync(int projectId, bool includeComments);
    Task<ExcelExportDto?> ExportSurveyQuestionDistributionToExcelAsync(int projectId);
    Task<ExcelExportDto> ExportSurveyQuestionScoreDetailAsync(int projectId);

    // ===== ENNEAGRAM EXPORT =====
    Task<ExcelExportDto> ExportEnneagramResultsToExcelAsync(EnneagramFilterDto filter);

    // ===== DEALER =====
    Task<IEnumerable<DealerListItemDto>> GetDealerListAsync(int? customerId = null);
    Task<DealerReportCardDto?> GetDealerReportCardAsync(DealerReportCardFilterDto filter);
    Task<ExcelExportDto> ExportDealerReportCardToExcelAsync(DealerReportCardFilterDto filter);
    Task<ExcelExportDto> ExportDealerReportCardToWordAsync(DealerReportCardFilterDto filter);

    // ===== SURVEY (Customer Portal specific) =====
    Task<List<SurveyProjectListItemDto>> GetSurveyProjectsAsync(int customerId);

    Task<List<RecentSurveyResponseDto>> GetRecentSurveyResponsesAsync(
        int customerId, int count = 20, int? projectId = null,
        DateTime? startDate = null, DateTime? endDate = null);

    Task<SurveyProjectDetailDto?> GetSurveyProjectDetailAsync(int customerId, int projectId);

    Task<SurveyQuestionScoreDistributionResultDto> GetSurveyQuestionScoreDistributionAsync(
        int customerId, int? projectId = null);

    Task<SurveyQuestionScoreDetailResultDto?> GetSurveyQuestionScoreDetailAsync(int customerId, int projectId);

    // ===== ENNEAGRAM (Customer Portal specific) =====
    Task<List<EnneagramProjectListItemDto>> GetEnneagramProjectsAsync(int customerId);

    Task<EnneagramResultsPagedDto> GetEnneagramResultsAsync(
        int customerId, int? projectId = null, string? searchTerm = null,
        int page = 1, int pageSize = 50);

    Task<EnneagramResultDetailDto?> GetEnneagramResultDetailAsync(int customerId, int evaluationId);

    Task<EnneagramDistributionResultDto?> GetEnneagramDistributionAsync(int customerId, int projectId);
}
