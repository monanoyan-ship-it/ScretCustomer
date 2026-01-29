using SecretCustomer.Core.DTOs.Auth;
using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IReportService
{
    // Değerlendirme listesi (sayfalı)
    Task<PagedReportResult<EvaluationReportDto>> GetEvaluationsAsync(ReportFilterDto filter);

    // Değerlendirme sayısı (ayrı endpoint - hızlı ilk yükleme için)
    Task<int> GetEvaluationsCountAsync(ReportFilterDto filter);

    // Değerlendirme detayı
    Task<EvaluationDetailReportDto?> GetEvaluationDetailAsync(int evaluationId);

    // Değerlendirme detayı Excel export
    Task<ExcelExportDto?> ExportEvaluationDetailToExcelAsync(int evaluationId, bool excludeEvaluatorInfo = false);

    // Özet rapor
    Task<SummaryReportDto> GetSummaryReportAsync(ReportFilterDto filter);

    // Excel export
    Task<ExcelExportDto> ExportEvaluationsToExcelAsync(ReportFilterDto filter);

    // Detaylı Excel export (soru-cevaplarla)
    Task<ExcelExportDto> ExportDetailedEvaluationsToExcelAsync(ReportFilterDto filter);

    // Cezalı KL Raporu
    Task<PenaltyReportResultDto> GetPenaltiesReportAsync(PenaltyFilterDto filter);

    // Cezalı KL Excel export
    Task<ExcelExportDto> ExportPenaltiesToExcelAsync(PenaltyFilterDto filter, bool excludeEvaluator = false);

    // ===== TEMSİLCİ KARNESİ (Video 4) =====

    // Değerlendirmesi olan müşteri listesi
    Task<IEnumerable<CustomerListItemDto>> GetCustomersWithEvaluationsAsync();

    // Değerlendirmesi olan organizasyon listesi
    Task<IEnumerable<OrganizationListItemDto>> GetOrganizationsWithEvaluationsAsync(int? customerId);

    // Değerlendirilen personel listesi (müşteri ve organizasyona göre filtrelenebilir)
    Task<IEnumerable<PersonnelListItemDto>> GetEvaluatedPersonnelListAsync(int? customerId = null, int? organizationId = null);

    // Personelin değerlendirildiği projeler (karne filtresi için)
    Task<IEnumerable<ProjectListItemDto>> GetPersonnelProjectsAsync(int personnelId);

    // Temsilci Karnesi raporu
    Task<PersonnelReportCardDto?> GetPersonnelReportCardAsync(PersonnelReportCardFilterDto filter);

    // Temsilci Karnesi PDF export
    Task<ExcelExportDto> ExportPersonnelReportCardToPdfAsync(PersonnelReportCardFilterDto filter);

    // Temsilci Karnesi Word export
    Task<ExcelExportDto> ExportPersonnelReportCardToWordAsync(PersonnelReportCardFilterDto filter);

    // ===== ÖNERİLER RAPORU (Video 5-6) =====

    // Öneriler raporu
    Task<SuggestionsReportResultDto> GetSuggestionsReportAsync(SuggestionsFilterDto filter);

    // En çok öneri yazılan sorular
    Task<IEnumerable<QuestionSuggestionSummaryDto>> GetTopSuggestedQuestionsAsync(SuggestionsFilterDto filter, int top = 10);

    // En çok seçilen alt kriterler
    Task<IEnumerable<SubCriteriaSummaryDto>> GetTopSubCriteriaAsync(SuggestionsFilterDto filter, int top = 10);

    // Öneriler Excel export
    Task<ExcelExportDto> ExportSuggestionsToExcelAsync(SuggestionsFilterDto filter, bool excludeEvaluator = false);

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

    // ===== ANKET SONUÇLARI RAPORU =====

    // Anket sonuçları raporu (Online Survey projeleri için)
    Task<SurveyResultsDto?> GetSurveyResultsAsync(int projectId, DateTime? startDate, DateTime? endDate);

    // Anket sonuçları Excel export
    Task<ExcelExportDto?> ExportSurveyResultsToExcelAsync(int projectId, DateTime? startDate, DateTime? endDate);

    // Online anket projeleri listesi (dashboard için)
    Task<List<SurveyProjectListItemDto>> GetSurveyProjectsAsync();

    // Son anket yanıtları (dashboard sol panel için)
    Task<List<RecentSurveyResponseDto>> GetRecentSurveyResponsesAsync(int count = 20, int? projectId = null, DateTime? startDate = null, DateTime? endDate = null);

    // Tüm anket yanıtları Excel export (2 sheet: Yanıtlar + Cevap Detayları)
    Task<ExcelExportDto?> ExportSurveyResponsesToExcelAsync(int? projectId = null);

    // Anket proje detayı (modal için - grup bazlı puanlarla)
    Task<SurveyProjectDetailDto?> GetSurveyProjectDetailAsync(int projectId);

    // Grup bazlı puan raporu Excel export
    Task<ExcelExportDto?> ExportSurveyGroupScoresToExcelAsync(int projectId);

    // Soru istatistik raporu Excel export (alt seçenek kaç kere seçilmiş)
    Task<ExcelExportDto?> ExportSurveyQuestionStatsToExcelAsync(int projectId);

    // Detay raporu Excel export (puan + seçenekler + opsiyonel yorumlar)
    Task<ExcelExportDto?> ExportSurveyDetailReportToExcelAsync(int projectId, bool includeComments);

    // Genel soru puan dağılımı Excel export
    Task<ExcelExportDto?> ExportSurveyQuestionDistributionToExcelAsync(int projectId);

    // ===== PERFORMANS TAKİBİ =====

    // Performans Takibi raporu (Dinleyici performansı + Firma kotaları)
    Task<PerformanceTrackingResultDto> GetPerformanceTrackingAsync(
        List<int>? customerIds = null,
        List<int>? evaluatorIds = null,
        List<int>? projectIds = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    // ===== GENEL SORU PUAN DAĞILIMI =====

    // Online anket projelerindeki soruların genel puan dağılımı
    Task<SurveyQuestionScoreDistributionResultDto> GetSurveyQuestionScoreDistributionAsync(int? projectId = null, DateTime? startDate = null, DateTime? endDate = null);

    // Proje bazlı soru puan detayı ve cevap dağılımları (Puan Detayı modalı için)
    Task<SurveyQuestionScoreDetailResultDto?> GetSurveyQuestionScoreDetailAsync(int projectId);

    // Proje bazlı soru puan detayı Excel export
    Task<ExcelExportDto> ExportSurveyQuestionScoreDetailAsync(int projectId);

    // ===== PERSONEL SORU BAZLI PERFORMANS RAPORU =====

    // Personel Soru Bazlı Performans Raporu - Tablo görünümü için
    Task<PersonnelQuestionPerformanceReportDto> GetPersonnelQuestionPerformanceAsync(PersonnelQuestionPerformanceFilterDto filter);

    // Personel Soru Bazlı Performans Raporu Excel export
    Task<ExcelExportDto> ExportPersonnelQuestionPerformanceReportAsync(PersonnelQuestionPerformanceFilterDto filter);

    // ===== ENNEAGRAM SONUÇLARI RAPORU =====

    // Enneagram projeleri listesi (checklistType=Enneagram olan projeler)
    Task<List<EnneagramProjectListItemDto>> GetEnneagramProjectsAsync();

    // Enneagram sonuçları listesi (filtrelenebilir)
    Task<(List<EnneagramResultListDto> Results, EnneagramSummaryDto Summary, int TotalCount)> GetEnneagramResultsAsync(EnneagramFilterDto filter);

    // Enneagram sonuç detayı (kişilik tipi puanlarıyla)
    Task<EnneagramResultDetailDto?> GetEnneagramResultDetailAsync(int evaluationId);

    // Enneagram sonuçları Excel export
    Task<ExcelExportDto> ExportEnneagramResultsToExcelAsync(EnneagramFilterDto filter);

    // Enneagram proje bazlı kişilik tipi dağılımı
    Task<EnneagramDistributionResultDto?> GetEnneagramDistributionAsync(int projectId);
}
