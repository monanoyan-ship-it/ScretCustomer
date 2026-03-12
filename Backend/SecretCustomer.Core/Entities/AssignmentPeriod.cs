using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Atama Dönemi - Bir atama altında aylık/dönemlik değerlendirme periyotları
/// Örnek: "Fort Global - Aralık 2025" dönemi, bu dönemde 25 çağrı değerlendirilecek
/// </summary>
public class AssignmentPeriod : BaseEntity
{
    /// <summary>
    /// Hangi atamaya ait (klasik atama akışı - PersonnelAssessment dışı)
    /// </summary>
    public int? AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }

    /// <summary>
    /// Hangi projeye ait (PersonnelAssessment doğrudan proje bazlı dönem yönetimi)
    /// </summary>
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    /// <summary>
    /// Dönem adı - Örnek: "Aralık 2025", "Ocak 2026"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Dönem başlangıç tarihi
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Dönem bitiş tarihi
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Dönem durumu: Open (Açık), Closed (Kapalı)
    /// </summary>
    public int StatusId { get; set; } = PeriodStatuses.Ids.Open;

    /// <summary>
    /// Hedef değerlendirme sayısı - Bu dönemde kaç dinleme yapılacak
    /// </summary>
    public int TargetCount { get; set; }

    /// <summary>
    /// Tamamlanan değerlendirme sayısı (hesaplanan)
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// Dönem ortalaması (hesaplanan)
    /// </summary>
    public decimal? AverageScore { get; set; }

    /// <summary>
    /// Dönem notu
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Dönemi açan kullanıcı
    /// </summary>
    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    // Navigation - Bu dönemdeki değerlendirmeler
    public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
}
