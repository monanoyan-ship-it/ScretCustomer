using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Evaluation;

public class SubmitEvaluationDto
{
    [Required]
    public int ProjectId { get; set; }

    /// <summary>
    /// Mevcut değerlendirme ID (taslak güncelleme için)
    /// </summary>
    public int? EvaluationId { get; set; }

    /// <summary>
    /// Değerlendirmenin ait olduğu dönem
    /// </summary>
    public int? AssignmentPeriodId { get; set; }

    /// <summary>
    /// Değerlendirmeyi yapan User ID (Gizli müşteri / Admin personel)
    /// </summary>
    public int? EvaluatorId { get; set; }

    /// <summary>
    /// Değerlendirmeyi yapan CustomerPersonnel ID (Müşteri portalı kullanıcıları için)
    /// </summary>
    public int? EvaluatorCustomerPersonnelId { get; set; }

    [Required]
    public List<SubmitAnswerDto> Answers { get; set; } = new();

    public string? Notes { get; set; }

    // ===== YENİ ALANLAR - Çağrı Denetleme =====

    /// <summary>
    /// Denetim Yorumu
    /// </summary>
    public string? EvaluationComment { get; set; }

    /// <summary>
    /// Çağrı ID/numarası
    /// </summary>
    public string? CallId { get; set; }

    /// <summary>
    /// Çağrı tarihi
    /// </summary>
    public DateTime? CallDate { get; set; }

    /// <summary>
    /// Çağrı saati (HH:mm formatında)
    /// </summary>
    public string? CallTime { get; set; }

    /// <summary>
    /// Süre (dk:sn formatında, örn: "12:26")
    /// </summary>
    public string? Duration { get; set; }

    /// <summary>
    /// Açıklamalar listesi (boş olanlar otomatik filtrelenir)
    /// </summary>
    public List<string>? Descriptions { get; set; }

    /// <summary>
    /// Değerlendirilen organizasyon ID (ZORUNLU)
    /// </summary>
    public int? EvaluatedOrganizationId { get; set; }

    /// <summary>
    /// Değerlendirilen personel ID (User - bizim şirket personeli)
    /// </summary>
    public int? EvaluatedPersonnelId { get; set; }

    /// <summary>
    /// Değerlendirilen müşteri personeli ID (CustomerPersonnel - firma personeli)
    /// </summary>
    public int? EvaluatedCustomerPersonnelId { get; set; }

    /// <summary>
    /// Tanımsız personel adı
    /// </summary>
    public string? EvaluatedUnknownPersonnel { get; set; }

    /// <summary>
    /// Kontrol tarihi
    /// </summary>
    public DateTime? ControlDate { get; set; }

    /// <summary>
    /// Ziyaret edilen bayi ID (FieldWorker ziyaretleri için)
    /// </summary>
    public int? CustomerDealerId { get; set; }

    /// <summary>
    /// Kontrol saati
    /// </summary>
    public string? ControlTime { get; set; }

    /// <summary>
    /// Form açılma zamanı
    /// </summary>
    public DateTime? FormOpenedAt { get; set; }

    /// <summary>
    /// Taslak olarak kaydet
    /// </summary>
    public bool SaveAsDraft { get; set; } = false;

    /// <summary>
    /// Yeni personel bilgisi (Listede Yok seçildiğinde)
    /// Doldurulursa PersonnelRequest oluşturulur
    /// </summary>
    public NewPersonnelDto? NewPersonnel { get; set; }
}

/// <summary>
/// Yeni personel talebi için DTO (Listede Yok)
/// </summary>
public class NewPersonnelDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Title { get; set; }
}

public class SubmitAnswerDto
{
    [Required]
    public int QuestionId { get; set; }

    public string? AnswerText { get; set; }

    public int? AnswerNumeric { get; set; }

    // ===== YENİ ALANLAR =====

    /// <summary>
    /// Verilen puan (ham puan)
    /// </summary>
    public decimal? GivenPoints { get; set; }

    /// <summary>
    /// Değerlendirici notu
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Öneri notu
    /// </summary>
    public string? RecommendationNotes { get; set; }

    /// <summary>
    /// Ceza uygulansın mı?
    /// </summary>
    public bool ApplyPenalty { get; set; } = false;

    /// <summary>
    /// Seçilen ceza tipi (None, YellowCard, RedCard)
    /// </summary>
    public string? SelectedPenaltyType { get; set; }

    /// <summary>
    /// Seçilen alt kriter ID'leri
    /// </summary>
    public List<int>? SelectedSubCriteriaIds { get; set; }

    /// <summary>
    /// Soru hesaplamaya dahil mi?
    /// Zorunlu olmayan sorular için kullanıcı tarafından belirlenir
    /// </summary>
    public bool IsIncluded { get; set; } = true;
}

/// <summary>
/// Değerlendirme başlatma için DTO
/// </summary>
public class StartEvaluationDto
{
    [Required]
    public int ProjectId { get; set; }

    /// <summary>
    /// Değerlendirmenin ait olduğu dönem
    /// </summary>
    public int? AssignmentPeriodId { get; set; }

    /// <summary>
    /// Değerlendirmeyi yapan User ID
    /// </summary>
    public int? EvaluatorId { get; set; }

    /// <summary>
    /// Değerlendirmeyi yapan CustomerPersonnel ID (Müşteri portalı için)
    /// </summary>
    public int? EvaluatorCustomerPersonnelId { get; set; }

    /// <summary>
    /// Çağrı ID/numarası
    /// </summary>
    public string? CallId { get; set; }

    /// <summary>
    /// Çağrı tarihi
    /// </summary>
    public DateTime? CallDate { get; set; }

    /// <summary>
    /// Çağrı saati (HH:mm formatında)
    /// </summary>
    public string? CallTime { get; set; }

    /// <summary>
    /// Değerlendirilen personel ID
    /// </summary>
    public int? EvaluatedPersonnelId { get; set; }

    /// <summary>
    /// Tanımsız personel adı
    /// </summary>
    public string? EvaluatedUnknownPersonnel { get; set; }
}

/// <summary>
/// Taslak güncelleme için DTO
/// </summary>
public class UpdateDraftDto
{
    [Required]
    public int EvaluationId { get; set; }

    public List<SubmitAnswerDto> Answers { get; set; } = new();

    public string? Notes { get; set; }

    public string? EvaluationComment { get; set; }
}
