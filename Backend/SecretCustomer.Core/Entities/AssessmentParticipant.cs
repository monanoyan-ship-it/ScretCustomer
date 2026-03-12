namespace SecretCustomer.Core.Entities;

/// <summary>
/// Personel Değerlendirme - Dönem bazlı hiyerarşi katılımcısı
/// ParentId ile ağaç yapısı: Yönetici > Takım Lideri > Operatör
/// Rol tespiti ağaçtan otomatik türer:
///   Self = kendisi, Manager = ParentId, Peer = aynı ParentId kardeşler, Subordinate = çocuklar
/// </summary>
public class AssessmentParticipant : BaseEntity
{
    /// <summary>
    /// Hangi döneme ait
    /// </summary>
    public int AssignmentPeriodId { get; set; }
    public AssignmentPeriod AssignmentPeriod { get; set; } = null!;

    /// <summary>
    /// Katılımcı personel
    /// </summary>
    public int CustomerPersonnelId { get; set; }
    public CustomerPersonnel CustomerPersonnel { get; set; } = null!;

    /// <summary>
    /// Üst katılımcı (null = en tepedeki yönetici)
    /// </summary>
    public int? ParentId { get; set; }
    public AssessmentParticipant? Parent { get; set; }

    /// <summary>
    /// Sıralama
    /// </summary>
    public int Order { get; set; }

    // Navigation - Alt katılımcılar
    public ICollection<AssessmentParticipant> Children { get; set; } = new List<AssessmentParticipant>();
}
