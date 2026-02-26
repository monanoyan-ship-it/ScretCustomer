using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Gölge Müşteri - Dinleme Değerlendirme (tamamlanmış aramayı dinleyerek puanlama)
/// </summary>
public class GmDinlemeEvaluation : BaseEntity
{
    /// <summary>
    /// Hangi tamamlanmış arama
    /// </summary>
    public int GmAtamaId { get; set; }
    public GmAtama? GmAtama { get; set; }

    /// <summary>
    /// Hangi kontrol listesi
    /// </summary>
    public int ChecklistId { get; set; }
    public Checklist? Checklist { get; set; }

    /// <summary>
    /// Kim dinliyor
    /// </summary>
    public int DinleyenUserId { get; set; }
    public User? DinleyenUser { get; set; }

    /// <summary>
    /// Dinleme durumu (GmDinlemeDurumlari)
    /// </summary>
    public int DurumId { get; set; } = GmDinlemeDurumlari.Ids.Beklemede;

    /// <summary>
    /// Dinleme tarihi
    /// </summary>
    public DateTime? DinlemeTarihi { get; set; }

    /// <summary>
    /// Puanlama
    /// </summary>
    public decimal TotalScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percentage { get; set; }

    /// <summary>
    /// Kart sayıları
    /// </summary>
    public int YellowCardCount { get; set; }
    public int RedCardCount { get; set; }

    /// <summary>
    /// Genel yorum
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Cevaplar
    /// </summary>
    public ICollection<GmDinlemeAnswer> Answers { get; set; } = new List<GmDinlemeAnswer>();
}
