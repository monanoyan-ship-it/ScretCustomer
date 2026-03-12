namespace SecretCustomer.Core.Entities;

/// <summary>
/// Personel Değerlendirme - Tek link altındaki değerlendirme zincirinin her adımı
/// Bir kişiye TEK link gider, bu link altında sıralı AssessmentTask'lar bulunur
/// Her task tamamlandığında sonraki task'a geçilir
/// </summary>
public class AssessmentTask : BaseEntity
{
    /// <summary>
    /// Tek link = tek davetiye
    /// </summary>
    public int SurveyInvitationId { get; set; }
    public SurveyInvitation SurveyInvitation { get; set; } = null!;

    /// <summary>
    /// Değerlendirilen kişi
    /// İsimli modda: spesifik kişi (her peer/subordinate için ayrı task)
    /// Anonim modda Peer/Subordinate: null (grup değerlendirmesi, toplu tek form)
    /// </summary>
    public int? EvaluatedCustomerPersonnelId { get; set; }
    public CustomerPersonnel? EvaluatedCustomerPersonnel { get; set; }

    /// <summary>
    /// Geri bildirim rolü (Self=1, Manager=2, Peer=3, Subordinate=4)
    /// </summary>
    public int FeedbackRoleId { get; set; }

    /// <summary>
    /// Sıra numarası (1=Self, 2=Manager, 3=Peer, 4=Subordinate...)
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Tamamlandığında oluşan Evaluation kaydı
    /// </summary>
    public int? EvaluationId { get; set; }
    public Evaluation? Evaluation { get; set; }

    /// <summary>
    /// Tamamlandı mı?
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Tamamlanma zamanı
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
