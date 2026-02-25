using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Attributes;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Değerlendirme (Kontrol Listesi Doldurma) - Bir atama için yapılan değerlendirmeyi temsil eder
/// </summary>
[ExcelTemplate("Değerlendirme", Description = "Değerlendirmeler için Excel import/export", IsAvailable = true)]
public class Evaluation : BaseEntity
{
    /// <summary>
    /// Değerlendirmenin ait olduğu proje (zorunlu)
    /// </summary>
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    /// <summary>
    /// Değerlendirmenin ait olduğu checklist (zorunlu)
    /// </summary>
    public int ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    /// <summary>
    /// Değerlendirmenin ait olduğu atama (nullable - survey/enneagram için boş kalabilir)
    /// </summary>
    public int? AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }

    /// <summary>
    /// Hangi döneme ait (opsiyonel - dönem olmadan da değerlendirme yapılabilir)
    /// </summary>
    public int? AssignmentPeriodId { get; set; }
    public AssignmentPeriod? AssignmentPeriod { get; set; }

    /// <summary>
    /// Değerlendirmeyi yapan User ID (Gizli müşteri / İç personel)
    /// </summary>
    public int? EvaluatorId { get; set; }
    public User? Evaluator { get; set; }

    /// <summary>
    /// Değerlendirmeyi yapan CustomerPersonnel ID (İç değerlendirme için)
    /// </summary>
    public int? EvaluatorCustomerPersonnelId { get; set; }
    public CustomerPersonnel? EvaluatorCustomerPersonnel { get; set; }

    [ExcelColumn("Durum", 1, ColumnType = ExcelColumnTypes.Ids.Dropdown,
        Description = "Değerlendirme durumu",
        DropdownOptions = "[\"Pending\", \"InProgress\", \"Completed\", \"Draft\", \"Cancelled\"]",
        SampleValue = "Pending")]
    public int StatusId { get; set; } = EvaluationStatuses.Ids.Pending;

    [ExcelColumn("Toplam Puan", 2, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Alınan toplam puan", SampleValue = "85")]
    public decimal? TotalScore { get; set; }

    [ExcelColumn("Maksimum Puan", 3, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Alınabilecek maksimum puan", SampleValue = "100")]
    public decimal? MaxScore { get; set; }

    [ExcelColumn("Yüzde", 4, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Puan yüzdesi", SampleValue = "85")]
    public decimal? ScorePercentage { get; set; }

    [ExcelColumn("Başlangıç Zamanı", 5, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Değerlendirmenin başlangıç zamanı")]
    public DateTime? StartedAt { get; set; }

    [ExcelColumn("Tamamlanma Zamanı", 6, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Değerlendirmenin tamamlanma zamanı")]
    public DateTime? CompletedAt { get; set; }

    [ExcelColumn("Notlar", 7, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Değerlendirme ile ilgili notlar", SampleValue = "Genel olarak başarılı")]
    public string? Notes { get; set; }

    // ===== YENİ ALANLAR - Çağrı Denetleme Geliştirmeleri =====

    /// <summary>
    /// Denetim Yorumu - Genel değerlendirme yorumu
    /// </summary>
    [ExcelColumn("Denetim Yorumu", 8, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Denetim yorumu / genel değerlendirme")]
    public string? EvaluationComment { get; set; }

    /// <summary>
    /// Denetlenen çağrı ID/numarası
    /// </summary>
    [ExcelColumn("Çağrı ID", 9, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Denetlenen çağrı numarası")]
    public string? CallId { get; set; }

    /// <summary>
    /// Denetlenen çağrı tarihi
    /// </summary>
    [ExcelColumn("Çağrı Tarihi", 10, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Denetlenen çağrının tarihi")]
    public DateTime? CallDate { get; set; }

    /// <summary>
    /// Çağrı saati (HH:mm formatında)
    /// </summary>
    [ExcelColumn("Çağrı Saati", 11, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Çağrı saati")]
    public string? CallTime { get; set; }

    /// <summary>
    /// Değerlendirme süresi (dakika:saniye formatında, örn: "12:26")
    /// </summary>
    [ExcelColumn("Süre", 12, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Değerlendirme süresi (dk:sn)")]
    public string? Duration { get; set; }

    /// <summary>
    /// Açıklamalar (JSON array olarak saklanır)
    /// </summary>
    public string? DescriptionsJson { get; set; }

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
    [ExcelColumn("Tanımsız Personel", 12, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Tanımsız personel adı")]
    public string? EvaluatedUnknownPersonnel { get; set; }

    /// <summary>
    /// Sarı kart sayısı
    /// </summary>
    [ExcelColumn("Sarı Kart", 13, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Sarı kart sayısı", SampleValue = "0")]
    public int YellowCardCount { get; set; } = 0;

    /// <summary>
    /// Kırmızı kart sayısı
    /// </summary>
    [ExcelColumn("Kırmızı Kart", 14, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Kırmızı kart sayısı", SampleValue = "0")]
    public int RedCardCount { get; set; } = 0;

    /// <summary>
    /// Form açılma tarihi
    /// </summary>
    public DateTime? FormOpenedAt { get; set; }

    /// <summary>
    /// Kontrol tarihi
    /// </summary>
    [ExcelColumn("Kontrol Tarihi", 15, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Kontrol listesinin doldurulduğu tarih")]
    public DateTime? ControlDate { get; set; }

    /// <summary>
    /// Kontrol saati
    /// </summary>
    [ExcelColumn("Kontrol Saati", 16, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Kontrol saati")]
    public string? ControlTime { get; set; }

    // ===== FIELDWORKER - BAYİ ZİYARETİ ALANLARI =====

    /// <summary>
    /// Ziyaret ID - FieldWorker değerlendirmeleri için otomatik üretilir
    /// Format: ZYR-{YIL}-{5 HANELI SIRA} (Örn: ZYR-2024-00001)
    /// </summary>
    [ExcelColumn("Ziyaret ID", 17, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Ziyaret numarası (FieldWorker için)")]
    public string? VisitId { get; set; }

    /// <summary>
    /// Ziyaret edilen bayi (FieldWorker değerlendirmeleri için)
    /// </summary>
    public int? CustomerDealerId { get; set; }
    public CustomerDealer? CustomerDealer { get; set; }

    /// <summary>
    /// Bildirim maili gönderildi mi?
    /// </summary>
    public bool IsNotificationSent { get; set; } = false;

    // Navigation properties
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<EvaluationAttachment> Attachments { get; set; } = new List<EvaluationAttachment>();
    public ICollection<AssignmentCustomerDealer> AssignmentCustomerDealers { get; set; } = new List<AssignmentCustomerDealer>();
}
