using SecretCustomer.Core.DTOs.Auth;
using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IReportService
{
    // Değerlendirme listesi (sayfalı)
    Task<PagedReportResult<EvaluationReportDto>> GetEvaluationsAsync(ReportFilterDto filter);

    // Değerlendirme detayı
    Task<EvaluationDetailReportDto?> GetEvaluationDetailAsync(int evaluationId);

    // Değerlendirme detayı Excel export
    Task<ExcelExportDto?> ExportEvaluationDetailToExcelAsync(int evaluationId);

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

    // Değerlendirmesi olan müşteri listesi
    Task<IEnumerable<CustomerListItemDto>> GetCustomersWithEvaluationsAsync();

    // Değerlendirmesi olan organizasyon listesi
    Task<IEnumerable<OrganizationListItemDto>> GetOrganizationsWithEvaluationsAsync(int? customerId);

    // Değerlendirilen personel listesi (müşteri ve organizasyona göre filtrelenebilir)
    Task<IEnumerable<PersonnelListItemDto>> GetEvaluatedPersonnelListAsync(int? customerId = null, int? organizationId = null);

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

    // ===== ÇAĞRI DENETLEME RAPORU =====

    // Çağrı Denetleme Raporu Excel export
    Task<ExcelExportDto> ExportCallAuditReportAsync(ReportFilterDto filter);

    // ===== SORU GRUBU ORTALAMA RAPORU =====

    // Soru Grubu Ortalama Raporu Excel export
    Task<ExcelExportDto> ExportQuestionGroupAverageReportAsync(ReportFilterDto filter);

    // ===== MÜŞTERİ DEĞERLENDİRME RAPORU =====

    // Müşteri Değerlendirme Raporu Excel export
    Task<ExcelExportDto> ExportCustomerEvaluationReportAsync(ReportFilterDto filter);

    // ===== PROJE PERFORMANS RAPORU =====

    // Proje Performans Raporu Excel export
    Task<ExcelExportDto> ExportProjectPerformanceReportAsync(ReportFilterDto filter);

    // ===== MT RAPORU (4 Sheet) =====

    // MT Raporu Excel export (Başarı, Gelişim Alanı, Süreç Analizi, Endeks Başarı)
    Task<ExcelExportDto> ExportMTReportAsync(ReportFilterDto filter);
}
