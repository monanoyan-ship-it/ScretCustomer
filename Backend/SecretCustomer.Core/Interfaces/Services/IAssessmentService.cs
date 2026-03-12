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
}
