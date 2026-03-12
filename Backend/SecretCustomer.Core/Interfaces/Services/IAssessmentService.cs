using SecretCustomer.Core.DTOs.AssignmentPeriod;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IAssessmentService
{
    // ===== Proje Bazlı Dönem Yönetimi (PersonnelAssessment) =====

    /// <summary>
    /// Projeye ait dönemleri getirir (assignment gerektirmez)
    /// </summary>
    Task<List<AssignmentPeriodDto>> GetPeriodsByProjectAsync(int projectId);

    /// <summary>
    /// Proje bazlı dönem oluşturur (assignment gerektirmez)
    /// </summary>
    Task<AssignmentPeriodDto> CreatePeriodForProjectAsync(int projectId, CreateAssignmentPeriodDto dto);

    /// <summary>
    /// Dönem güncelle
    /// </summary>
    Task<AssignmentPeriodDto?> UpdatePeriodAsync(int periodId, UpdateAssignmentPeriodDto dto);

    /// <summary>
    /// Dönem sil (soft delete)
    /// </summary>
    Task<bool> DeletePeriodAsync(int periodId);

    // ===== Hiyerarşi Yönetimi =====

    /// <summary>
    /// Dönemdeki tüm katılımcıları hiyerarşi ağacı olarak getirir
    /// </summary>
    Task<List<AssessmentParticipant>> GetParticipantsAsync(int assignmentPeriodId);

    /// <summary>
    /// Katılımcı ekle
    /// </summary>
    Task<AssessmentParticipant> AddParticipantAsync(int assignmentPeriodId, int customerPersonnelId, int? parentId);

    /// <summary>
    /// Katılımcı çıkar (alt katılımcıları da siler)
    /// </summary>
    Task RemoveParticipantAsync(int participantId);

    /// <summary>
    /// Katılımcının üstünü değiştir (taşı)
    /// </summary>
    Task MoveParticipantAsync(int participantId, int? newParentId);

    /// <summary>
    /// Mevcut CustomerPersonnelOrganization'dan hiyerarşiyi otomatik aktar
    /// </summary>
    Task<int> ImportFromOrganizationAsync(int assignmentPeriodId, int projectId);

    /// <summary>
    /// Dönemdeki katılımcı sayısını getirir
    /// </summary>
    Task<int> GetParticipantCountAsync(int assignmentPeriodId);

    // ===== AssessmentTask Zincir Oluşturma =====

    /// <summary>
    /// Tüm katılımcılar için AssessmentTask zincirlerini oluştur ve davetiye gönderimini başlat
    /// Mod (180/360) ve anonimlik ayarına göre task'lar oluşturulur
    /// </summary>
    Task<int> GenerateAssessmentTasksAsync(int assignmentPeriodId, int projectId);

    /// <summary>
    /// Belirli bir davetiyenin task zincirini getirir
    /// </summary>
    Task<List<AssessmentTask>> GetTaskChainAsync(int surveyInvitationId);

    /// <summary>
    /// Belirli bir davetiyenin sıradaki tamamlanmamış task'ını getirir
    /// </summary>
    Task<AssessmentTask?> GetNextPendingTaskAsync(int surveyInvitationId);

    // ===== Tek Link Doldurma Akışı =====

    /// <summary>
    /// Token ile davetiyeyi ve projeyi getir (doldurma sayfası için)
    /// </summary>
    Task<AssessmentFillContext?> GetFillContextByTokenAsync(string token);

    /// <summary>
    /// Bir task'ı tamamla: Evaluation oluştur, task'ı güncelle, sonraki task'ı döndür
    /// </summary>
    Task<AssessmentTaskCompletionResult> CompleteTaskAsync(int assessmentTaskId, int evaluationId);
}

/// <summary>
/// Doldurma sayfası context'i — token'dan çözümlenir
/// </summary>
public class AssessmentFillContext
{
    public SurveyInvitation Invitation { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public AssessmentTask CurrentTask { get; set; } = null!;
    public int TotalTaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public bool IsAllCompleted { get; set; }

    /// <summary>
    /// Gösterilecek soru metni: Self ise SelfText (fallback Text), diğerleri Text
    /// </summary>
    public bool UseSelfText => CurrentTask.FeedbackRoleId == SecretCustomer.Core.Enums.FeedbackRoles.Ids.Self;
}

/// <summary>
/// Task tamamlama sonucu
/// </summary>
public class AssessmentTaskCompletionResult
{
    public bool HasNextTask { get; set; }
    public AssessmentTask? NextTask { get; set; }
    public bool IsAllCompleted { get; set; }
}
