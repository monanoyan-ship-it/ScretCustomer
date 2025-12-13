using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IReportService
{
    // Değerlendirme listesi (sayfalı)
    Task<PagedReportResult<EvaluationReportDto>> GetEvaluationsAsync(ReportFilterDto filter);

    // Değerlendirme detayı
    Task<EvaluationDetailReportDto?> GetEvaluationDetailAsync(Guid evaluationId);

    // Özet rapor
    Task<SummaryReportDto> GetSummaryReportAsync(ReportFilterDto filter);

    // Excel export
    Task<ExcelExportDto> ExportEvaluationsToExcelAsync(ReportFilterDto filter);

    // Detaylı Excel export (soru-cevaplarla)
    Task<ExcelExportDto> ExportDetailedEvaluationsToExcelAsync(ReportFilterDto filter);

    // Cezalı KL Raporu
    Task<PenaltyReportResultDto> GetPenaltiesReportAsync(PenaltyFilterDto filter);

    // Cezalı KL Excel export
    Task<ExcelExportDto> ExportPenaltiesToExcelAsync(PenaltyFilterDto filter);

    // ===== TEMSİLCİ KARNESİ (Video 4) =====

    // Değerlendirilen personel listesi
    Task<IEnumerable<PersonnelListItemDto>> GetEvaluatedPersonnelListAsync();

    // Temsilci Karnesi raporu
    Task<PersonnelReportCardDto?> GetPersonnelReportCardAsync(PersonnelReportCardFilterDto filter);

    // Temsilci Karnesi PDF export
    Task<ExcelExportDto> ExportPersonnelReportCardToPdfAsync(PersonnelReportCardFilterDto filter);

    // ===== ÖNERİLER RAPORU (Video 5-6) =====

    // Öneriler raporu
    Task<SuggestionsReportResultDto> GetSuggestionsReportAsync(SuggestionsFilterDto filter);

    // En çok öneri yazılan sorular
    Task<IEnumerable<QuestionSuggestionSummaryDto>> GetTopSuggestedQuestionsAsync(SuggestionsFilterDto filter, int top = 10);

    // Öneriler Excel export
    Task<ExcelExportDto> ExportSuggestionsToExcelAsync(SuggestionsFilterDto filter);
}
