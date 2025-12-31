using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Attributes;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Değerlendirme (Kontrol Listesi Doldurma) - Bir atama için yapılan değerlendirmeyi temsil eder
/// </summary>
[ExcelTemplate("Değerlendirme", Description = "Değerlendirmeler için Excel import/export", IsAvailable = true)]
public class Evaluation : BaseEntity
{
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    /// <summary>
    /// Hangi döneme ait (opsiyonel - dönem olmadan da değerlendirme yapılabilir)
    /// </summary>
    public int? AssignmentPeriodId { get; set; }
    public AssignmentPeriod? AssignmentPeriod { get; set; }

    public int? EvaluatorId { get; set; }
    public User? Evaluator { get; set; }

    [ExcelColumn("Durum", 1, ColumnType = ExcelColumnType.Dropdown,
        Description = "Değerlendirme durumu",
        DropdownOptions = "[\"Pending\", \"InProgress\", \"Completed\", \"Draft\", \"Cancelled\"]",
        SampleValue = "Pending")]
    public EvaluationStatus Status { get; set; } = EvaluationStatus.Pending;

    [ExcelColumn("Toplam Puan", 2, ColumnType = ExcelColumnType.Number,
        Description = "Alınan toplam puan", SampleValue = "85")]
    public decimal? TotalScore { get; set; }

    [ExcelColumn("Maksimum Puan", 3, ColumnType = ExcelColumnType.Number,
        Description = "Alınabilecek maksimum puan", SampleValue = "100")]
    public decimal? MaxScore { get; set; }

    [ExcelColumn("Yüzde", 4, ColumnType = ExcelColumnType.Number,
        Description = "Puan yüzdesi", SampleValue = "85")]
    public decimal? ScorePercentage { get; set; }

    [ExcelColumn("Başlangıç Zamanı", 5, ColumnType = ExcelColumnType.Date,
        Description = "Değerlendirmenin başlangıç zamanı")]
    public DateTime? StartedAt { get; set; }

    [ExcelColumn("Tamamlanma Zamanı", 6, ColumnType = ExcelColumnType.Date,
        Description = "Değerlendirmenin tamamlanma zamanı")]
    public DateTime? CompletedAt { get; set; }

    [ExcelColumn("Notlar", 7, ColumnType = ExcelColumnType.Text,
        Description = "Değerlendirme ile ilgili notlar", SampleValue = "Genel olarak başarılı")]
    public string? Notes { get; set; }

    // ===== YENİ ALANLAR - Çağrı Denetleme Geliştirmeleri =====

    /// <summary>
    /// Denetim Yorumu - Genel değerlendirme yorumu
    /// </summary>
    [ExcelColumn("Denetim Yorumu", 8, ColumnType = ExcelColumnType.Text,
        Description = "Denetim yorumu / genel değerlendirme")]
    public string? EvaluationComment { get; set; }

    /// <summary>
    /// Denetlenen çağrı ID/numarası
    /// </summary>
    [ExcelColumn("Çağrı ID", 9, ColumnType = ExcelColumnType.Text,
        Description = "Denetlenen çağrı numarası")]
    public string? CallId { get; set; }

    /// <summary>
    /// Denetlenen çağrı tarihi
    /// </summary>
    [ExcelColumn("Çağrı Tarihi", 10, ColumnType = ExcelColumnType.Date,
        Description = "Denetlenen çağrının tarihi")]
    public DateTime? CallDate { get; set; }

    /// <summary>
    /// Değerlendirme süresi (dakika)
    /// </summary>
    [ExcelColumn("Süre (dk)", 11, ColumnType = ExcelColumnType.Number,
        Description = "Değerlendirme süresi (dakika)")]
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Değerlendirilen tanımlı personel ID (User - bizim şirket personeli)
    /// </summary>
    public int? EvaluatedPersonnelId { get; set; }

    /// <summary>
    /// Değerlendirilen personel (Navigation Property)
    /// </summary>
    public User? EvaluatedPersonnel { get; set; }

    // ===== YENİ: Müşteri Personeli Değerlendirmesi =====

    /// <summary>
    /// Değerlendirilen müşteri personeli ID (CustomerPersonnel - firma personeli)
    /// </summary>
    public int? EvaluatedCustomerPersonnelId { get; set; }

    /// <summary>
    /// Değerlendirilen müşteri personeli
    /// </summary>
    public CustomerPersonnel? EvaluatedCustomerPersonnel { get; set; }

    /// <summary>
    /// Değerlendirilen organizasyon ID
    /// </summary>
    public int? EvaluatedOrganizationId { get; set; }

    /// <summary>
    /// Değerlendirilen organizasyon
    /// </summary>
    public CustomerOrganization? EvaluatedOrganization { get; set; }

    /// <summary>
    /// Değerlendirilen tanımsız personel adı
    /// </summary>
    [ExcelColumn("Tanımsız Personel", 12, ColumnType = ExcelColumnType.Text,
        Description = "Tanımsız personel adı")]
    public string? EvaluatedUnknownPersonnel { get; set; }

    /// <summary>
    /// Sarı kart sayısı
    /// </summary>
    [ExcelColumn("Sarı Kart", 13, ColumnType = ExcelColumnType.Number,
        Description = "Sarı kart sayısı", SampleValue = "0")]
    public int YellowCardCount { get; set; } = 0;

    /// <summary>
    /// Kırmızı kart sayısı
    /// </summary>
    [ExcelColumn("Kırmızı Kart", 14, ColumnType = ExcelColumnType.Number,
        Description = "Kırmızı kart sayısı", SampleValue = "0")]
    public int RedCardCount { get; set; } = 0;

    /// <summary>
    /// Form açılma tarihi
    /// </summary>
    public DateTime? FormOpenedAt { get; set; }

    /// <summary>
    /// Kontrol tarihi
    /// </summary>
    [ExcelColumn("Kontrol Tarihi", 15, ColumnType = ExcelColumnType.Date,
        Description = "Kontrol listesinin doldurulduğu tarih")]
    public DateTime? ControlDate { get; set; }

    /// <summary>
    /// Kontrol saati
    /// </summary>
    [ExcelColumn("Kontrol Saati", 16, ColumnType = ExcelColumnType.Text,
        Description = "Kontrol saati")]
    public string? ControlTime { get; set; }

    // Navigation properties
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
