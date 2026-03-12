using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IAssessmentService
{
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
}
