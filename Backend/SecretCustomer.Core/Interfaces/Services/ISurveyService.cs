using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ISurveyService
{
    /// <summary>
    /// Projeyi ilişkileriyle birlikte getir (Customer, Organization, Checklist, EmailTemplate)
    /// </summary>
    Task<Project?> GetProjectWithDetailsAsync(int projectId);

    /// <summary>
    /// Projeyi sadece temel bilgileriyle getir
    /// </summary>
    Task<Project?> GetProjectAsync(int projectId);

    /// <summary>
    /// Projeyi Checklist ile birlikte getir
    /// </summary>
    Task<Project?> GetProjectWithChecklistAsync(int projectId);

    /// <summary>
    /// Email şablonunu getir
    /// </summary>
    Task<EmailTemplate?> GetEmailTemplateAsync(int emailTemplateId);

    /// <summary>
    /// Proje kapsamındaki aktif personelleri getir (email'i olanlar)
    /// </summary>
    Task<List<CustomerPersonnel>> GetProjectPersonnelWithEmailAsync(int? customerId, int? organizationId);

    /// <summary>
    /// Proje kapsamındaki tüm aktif personelleri getir
    /// </summary>
    Task<List<CustomerPersonnel>> GetProjectPersonnelAsync(int? customerId, int? organizationId);

    /// <summary>
    /// Proje kapsamındaki aktif personelleri getir (Customer dahil, email'i olanlar)
    /// </summary>
    Task<List<CustomerPersonnel>> GetProjectPersonnelWithCustomerAsync(int? customerId, int? organizationId);

    /// <summary>
    /// Anketi tamamlamış personel ID'lerini getir
    /// </summary>
    Task<List<int>> GetCompletedPersonnelIdsAsync(int projectId);

    /// <summary>
    /// SurveyInvitation kaydı oluştur ve kaydet
    /// </summary>
    Task<SurveyInvitation> CreateSurveyInvitationAsync(int projectId, int customerPersonnelId, string email, bool isReminder);

    /// <summary>
    /// SurveyInvitation durumunu güncelle (gönderim sonrası)
    /// </summary>
    Task UpdateSurveyInvitationStatusAsync(SurveyInvitation invitation, int statusId, string? errorMessage = null);

    /// <summary>
    /// External invitation token ile getir
    /// </summary>
    Task<SurveyExternalInvitation?> GetExternalInvitationByTokenAsync(string token);

    /// <summary>
    /// Projeyi Customer, Organization, Checklist ile birlikte getir (validate için)
    /// </summary>
    Task<SurveyExternalInvitation?> GetExternalInvitationWithProjectAsync(string token);

    /// <summary>
    /// External invitation'ı tamamlandı olarak işaretle (submit için)
    /// </summary>
    Task<SurveyExternalInvitation?> GetExternalInvitationWithChecklistAsync(string token);

    /// <summary>
    /// Personel getir (ID ile)
    /// </summary>
    Task<CustomerPersonnel?> GetPersonnelAsync(int personnelId);

    /// <summary>
    /// Personel için tamamlanmış değerlendirme var mı? (Assignments tablosu)
    /// </summary>
    Task<bool> HasCompletedAssignmentAsync(int projectId, int personnelId);

    /// <summary>
    /// Personel için tamamlanmış değerlendirme var mı? (Evaluations tablosu)
    /// </summary>
    Task<Evaluation?> GetCompletedEvaluationAsync(int projectId, int personnelId);

    /// <summary>
    /// SurveyInvitation'ı açıldı olarak işaretle
    /// </summary>
    Task MarkInvitationOpenedAsync(int projectId, int personnelId);

    /// <summary>
    /// External invitation'ı açıldı olarak işaretle
    /// </summary>
    Task MarkExternalInvitationOpenedAsync(SurveyExternalInvitation invitation);

    /// <summary>
    /// Checklist sorularını getir
    /// </summary>
    Task<object> GetSurveyQuestionsAsync(int? checklistId);

    /// <summary>
    /// Evaluation oluştur
    /// </summary>
    Task<Evaluation> CreateEvaluationAsync(Evaluation evaluation);

    /// <summary>
    /// Cevapları kaydet ve puanı hesapla
    /// </summary>
    Task<(decimal? TotalScore, decimal? MaxScore, decimal? ScorePercentage)> SaveAnswersAndCalculateScoreAsync(int evaluationId, int? checklistId, List<SurveyAnswerDto> answers);

    /// <summary>
    /// SurveyInvitation'ı tamamlandı olarak işaretle (submit sonrası)
    /// </summary>
    Task MarkSurveyInvitationCompletedAsync(int projectId, int personnelId);

    /// <summary>
    /// External invitation'ı tamamlandı olarak işaretle
    /// </summary>
    Task MarkExternalInvitationCompletedAsync(SurveyExternalInvitation invitation, int evaluationId);

    /// <summary>
    /// Evaluation score güncelle
    /// </summary>
    Task UpdateEvaluationScoreAsync(int evaluationId, decimal? totalScore, decimal? maxScore, decimal? scorePercentage);

    /// <summary>
    /// Tamamlayan personelleri ve puanlarını getir (status için)
    /// </summary>
    Task<Dictionary<int, (DateTime? CompletedAt, decimal? Score)>> GetCompletedAssignmentsWithScoresAsync(int projectId);

    /// <summary>
    /// Proje için davetiye istatistiklerini getir
    /// </summary>
    Task<List<SurveyInvitation>> GetInvitationsRawAsync(int projectId);

    /// <summary>
    /// ID listesine göre CustomerPersonnel getir
    /// </summary>
    Task<List<CustomerPersonnel>> GetCustomerPersonnelByIdsAsync(List<int> ids);

    /// <summary>
    /// Proje için davetiye listesini getir
    /// </summary>
    Task<object> GetInvitationsAsync(int projectId, int? statusId);

    /// <summary>
    /// Başarısız davetiyeleri getir (retry için)
    /// </summary>
    Task<List<SurveyInvitation>> GetFailedInvitationsAsync(int projectId);

    /// <summary>
    /// Davetiye retry sonrası güncelle
    /// </summary>
    Task UpdateInvitationRetryAsync(SurveyInvitation invitation, int statusId, string? errorMessage = null);

    /// <summary>
    /// Aynı proje+email için dış davetiye var mı?
    /// </summary>
    Task<bool> ExternalInvitationExistsAsync(int projectId, string email);

    /// <summary>
    /// Dış davetiye oluştur
    /// </summary>
    Task<SurveyExternalInvitation> CreateExternalInvitationAsync(int projectId, string email, string? firstName, string? lastName, string token);

    /// <summary>
    /// Dış davetiye durumunu güncelle
    /// </summary>
    Task UpdateExternalInvitationStatusAsync(SurveyExternalInvitation invitation, int statusId, string? errorMessage = null);

    /// <summary>
    /// Dış davetiye listesini getir
    /// </summary>
    Task<object> GetExternalInvitationsAsync(int projectId, int? statusId);

    /// <summary>
    /// Dış davetiye istatistikleri için ham listeyi getir
    /// </summary>
    Task<List<SurveyExternalInvitation>> GetExternalInvitationsRawAsync(int projectId);

    /// <summary>
    /// Başarısız dış davetiyeleri getir
    /// </summary>
    Task<List<SurveyExternalInvitation>> GetFailedExternalInvitationsAsync(int projectId);

    /// <summary>
    /// Dış davetiye retry sonrası güncelle
    /// </summary>
    Task UpdateExternalInvitationRetryAsync(SurveyExternalInvitation invitation, int statusId, string? errorMessage = null);

    /// <summary>
    /// Hatırlatma gönderilecek dış davetiyeleri getir
    /// </summary>
    Task<List<SurveyExternalInvitation>> GetExternalInvitationsForReminderAsync(int projectId, string? filter);

    /// <summary>
    /// Dış davetiye hatırlatma sonrası güncelle
    /// </summary>
    Task UpdateExternalInvitationReminderAsync(SurveyExternalInvitation invitation, bool success);

    /// <summary>
    /// Değişiklikleri kaydet
    /// </summary>
    Task SaveChangesAsync();
}

/// <summary>
/// Anket soru cevabı DTO (service'e taşındı)
/// </summary>
public class SurveyAnswerDto
{
    public int QuestionId { get; set; }
    public int? Score { get; set; }
    public string? Comment { get; set; }
    public List<int>? SelectedSubCriteriaIds { get; set; }
}
