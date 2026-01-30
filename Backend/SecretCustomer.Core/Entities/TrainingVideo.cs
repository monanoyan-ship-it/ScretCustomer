namespace SecretCustomer.Core.Entities;

/// <summary>
/// Eğitim Videosu - Admin tarafından yüklenen ve personele atanan videolar
/// </summary>
public class TrainingVideo : BaseEntity
{
    /// <summary>
    /// Video başlığı
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Dosya adı
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Depolama yolu
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Dosya boyutu (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Video süresi (saniye)
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Önizleme görseli yolu
    /// </summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Minimum izleme yüzdesi (tamamlanmış sayılması için). Örn: 90 = %90 izlenmeli
    /// </summary>
    public int MinWatchPercentage { get; set; } = 90;

    /// <summary>
    /// İleri/geri atlama izni var mı?
    /// </summary>
    public bool AllowSkipping { get; set; } = false;

    /// <summary>
    /// Maksimum oynatma hızı. Örn: 1.0 = normal hız, 1.5 = 1.5x hız
    /// </summary>
    public decimal MaxPlaybackSpeed { get; set; } = 1.0m;

    /// <summary>
    /// Video kapsamları - hangi checklist/soru grubu/soruları kapsıyor
    /// </summary>
    public ICollection<TrainingVideoScope> Scopes { get; set; } = new List<TrainingVideoScope>();

    /// <summary>
    /// Video atamaları
    /// </summary>
    public ICollection<TrainingVideoAssignment> Assignments { get; set; } = new List<TrainingVideoAssignment>();
}

/// <summary>
/// Video Kapsamı - Video hangi checklist/soru grubu/soruyu kapsıyor
/// </summary>
public class TrainingVideoScope : BaseEntity
{
    /// <summary>
    /// Video ID
    /// </summary>
    public int TrainingVideoId { get; set; }

    /// <summary>
    /// Video
    /// </summary>
    public TrainingVideo TrainingVideo { get; set; } = null!;

    /// <summary>
    /// Kapsam türü: 1=Checklist, 2=QuestionGroup, 3=Question
    /// </summary>
    public int ScopeTypeId { get; set; }

    /// <summary>
    /// Checklist ID (ScopeTypeId=1 ise)
    /// </summary>
    public int? ChecklistId { get; set; }

    /// <summary>
    /// Checklist
    /// </summary>
    public Checklist? Checklist { get; set; }

    /// <summary>
    /// Soru grubu adı (ScopeTypeId=2 ise) - Question.GroupName referansı
    /// </summary>
    public string? QuestionGroupName { get; set; }

    /// <summary>
    /// Soru ID (ScopeTypeId=3 ise)
    /// </summary>
    public int? QuestionId { get; set; }

    /// <summary>
    /// Soru
    /// </summary>
    public Question? Question { get; set; }
}

/// <summary>
/// Eğitim Video Ataması - Bir video için yapılan toplu atama
/// </summary>
public class TrainingVideoAssignment : BaseEntity
{
    /// <summary>
    /// Atama başlığı (örn: "Ocak 2026 Aktif Dinleme Eğitimi")
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Video ID
    /// </summary>
    public int TrainingVideoId { get; set; }

    /// <summary>
    /// Video
    /// </summary>
    public TrainingVideo TrainingVideo { get; set; } = null!;

    /// <summary>
    /// Başlangıç tarihi
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Bitiş tarihi
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Dış katılımcı ataması mı? (true = dış, false = iç)
    /// </summary>
    public bool IsExternal { get; set; } = false;

    /// <summary>
    /// Kaynak proje ID (otomatik atama için)
    /// </summary>
    public int? SourceProjectId { get; set; }

    /// <summary>
    /// Kaynak proje
    /// </summary>
    public Project? SourceProject { get; set; }

    /// <summary>
    /// Puan eşiği (bu puanın altındakilere atandı)
    /// </summary>
    public decimal? ScoreThreshold { get; set; }

    /// <summary>
    /// Kaynak tarih aralığı başlangıcı
    /// </summary>
    public DateTime? SourceStartDate { get; set; }

    /// <summary>
    /// Kaynak tarih aralığı bitişi
    /// </summary>
    public DateTime? SourceEndDate { get; set; }

    /// <summary>
    /// Katılımcılar (sistem kullanıcıları)
    /// </summary>
    public ICollection<TrainingVideoParticipant> Participants { get; set; } = new List<TrainingVideoParticipant>();

    /// <summary>
    /// Dış katılımcılar (sistemde kayıtlı olmayan email adresleri)
    /// </summary>
    public ICollection<TrainingVideoExternalParticipant> ExternalParticipants { get; set; } = new List<TrainingVideoExternalParticipant>();

    /// <summary>
    /// Minimum izleme sayısı (varsayılan: 1)
    /// </summary>
    public int MinWatchCount { get; set; } = 1;

    /// <summary>
    /// Maksimum izleme sayısı (null = sınırsız)
    /// </summary>
    public int? MaxWatchCount { get; set; }

    /// <summary>
    /// Video hızını değiştirmeye izin ver
    /// </summary>
    public bool AllowSpeedChange { get; set; } = false;

    /// <summary>
    /// Videoyu ileri/geri sarmaya izin ver
    /// </summary>
    public bool AllowSeeking { get; set; } = false;

    /// <summary>
    /// Email şablon ID
    /// </summary>
    public int? EmailTemplateId { get; set; }

    /// <summary>
    /// Email şablonu
    /// </summary>
    public EmailTemplate? EmailTemplate { get; set; }
}

/// <summary>
/// Eğitim Video Katılımcısı - Atamaya dahil edilen müşteri personeli
/// </summary>
public class TrainingVideoParticipant : BaseEntity
{
    /// <summary>
    /// Atama ID
    /// </summary>
    public int TrainingVideoAssignmentId { get; set; }

    /// <summary>
    /// Atama
    /// </summary>
    public TrainingVideoAssignment Assignment { get; set; } = null!;

    /// <summary>
    /// Müşteri Personeli ID - Eğitimi izleyecek kişi
    /// </summary>
    public int CustomerPersonnelId { get; set; }

    /// <summary>
    /// Müşteri Personeli
    /// </summary>
    public CustomerPersonnel CustomerPersonnel { get; set; } = null!;

    /// <summary>
    /// Durum: 1=Pending, 2=InProgress, 3=Completed
    /// </summary>
    public int StatusId { get; set; } = 1;

    /// <summary>
    /// İzlemeye başlama tarihi
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Tamamlama tarihi
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// İzlenen süre (saniye)
    /// </summary>
    public int WatchedSeconds { get; set; }

    /// <summary>
    /// Tamamen izlendi mi?
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Toplam izleme sayısı (video sonuna kadar izlendiğinde artar)
    /// </summary>
    public int WatchCount { get; set; }

    /// <summary>
    /// İlk email gönderim tarihi
    /// </summary>
    public DateTime? FirstEmailSentAt { get; set; }

    /// <summary>
    /// Son email gönderim tarihi
    /// </summary>
    public DateTime? LastEmailSentAt { get; set; }

    /// <summary>
    /// Toplam gönderilen email sayısı
    /// </summary>
    public int EmailSentCount { get; set; }

    /// <summary>
    /// Kaynak değerlendirme ID (varsa - bu değerlendirmeden dolayı atandı)
    /// </summary>
    public int? SourceEvaluationId { get; set; }

    /// <summary>
    /// Kaynak değerlendirme
    /// </summary>
    public Evaluation? SourceEvaluation { get; set; }

    /// <summary>
    /// Email gönderim geçmişi
    /// </summary>
    public ICollection<TrainingVideoEmailLog> EmailLogs { get; set; } = new List<TrainingVideoEmailLog>();
}

/// <summary>
/// Eğitim Video Email Logu - Her email gönderimini ayrı ayrı takip eder
/// </summary>
public class TrainingVideoEmailLog : BaseEntity
{
    /// <summary>
    /// Katılımcı ID
    /// </summary>
    public int TrainingVideoParticipantId { get; set; }

    /// <summary>
    /// Katılımcı
    /// </summary>
    public TrainingVideoParticipant Participant { get; set; } = null!;

    /// <summary>
    /// Email şablon ID
    /// </summary>
    public int? EmailTemplateId { get; set; }

    /// <summary>
    /// Email şablonu
    /// </summary>
    public EmailTemplate? EmailTemplate { get; set; }

    /// <summary>
    /// Email gönderim tarihi
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Gönderen kullanıcı ID (Admin)
    /// </summary>
    public int? SentByUserId { get; set; }

    /// <summary>
    /// Gönderen kullanıcı
    /// </summary>
    public User? SentByUser { get; set; }

    /// <summary>
    /// Email türü: 1=Initial (ilk atama), 2=Reminder (hatırlatma)
    /// </summary>
    public int EmailTypeId { get; set; } = 1;

    /// <summary>
    /// Email başarılı mı?
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Dış Eğitim Video Katılımcısı - Sistemde kayıtlı olmayan email adreslerine video atama
/// </summary>
public class TrainingVideoExternalParticipant : BaseEntity
{
    /// <summary>
    /// Atama ID
    /// </summary>
    public int TrainingVideoAssignmentId { get; set; }

    /// <summary>
    /// Atama
    /// </summary>
    public TrainingVideoAssignment Assignment { get; set; } = null!;

    /// <summary>
    /// Email adresi
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Ad (opsiyonel)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Soyad (opsiyonel)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Benzersiz token (GUID) - video izleme linkinde kullanılır
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Durum: 1=Pending, 2=InProgress, 3=Completed
    /// </summary>
    public int StatusId { get; set; } = 1;

    /// <summary>
    /// Video linki açıldı mı?
    /// </summary>
    public bool IsOpened { get; set; }

    /// <summary>
    /// Açılma zamanı
    /// </summary>
    public DateTime? OpenedAt { get; set; }

    /// <summary>
    /// İzlemeye başlama tarihi
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Tamamlama tarihi
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// İzlenen süre (saniye)
    /// </summary>
    public int WatchedSeconds { get; set; }

    /// <summary>
    /// Tamamen izlendi mi?
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Toplam izleme sayısı
    /// </summary>
    public int WatchCount { get; set; }

    /// <summary>
    /// İlk email gönderim tarihi
    /// </summary>
    public DateTime? FirstEmailSentAt { get; set; }

    /// <summary>
    /// Son email gönderim tarihi
    /// </summary>
    public DateTime? LastEmailSentAt { get; set; }

    /// <summary>
    /// Toplam gönderilen email sayısı
    /// </summary>
    public int EmailSentCount { get; set; }

    /// <summary>
    /// Email gönderim geçmişi
    /// </summary>
    public ICollection<TrainingVideoExternalEmailLog> EmailLogs { get; set; } = new List<TrainingVideoExternalEmailLog>();

    /// <summary>
    /// Tam adı döndürür
    /// </summary>
    public string? FullName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
        ? null
        : $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// Dış Katılımcı Email Logu
/// </summary>
public class TrainingVideoExternalEmailLog : BaseEntity
{
    /// <summary>
    /// Dış Katılımcı ID
    /// </summary>
    public int TrainingVideoExternalParticipantId { get; set; }

    /// <summary>
    /// Dış Katılımcı
    /// </summary>
    public TrainingVideoExternalParticipant ExternalParticipant { get; set; } = null!;

    /// <summary>
    /// Email şablon ID
    /// </summary>
    public int? EmailTemplateId { get; set; }

    /// <summary>
    /// Email şablonu
    /// </summary>
    public EmailTemplate? EmailTemplate { get; set; }

    /// <summary>
    /// Email gönderim tarihi
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Gönderen kullanıcı ID (Admin)
    /// </summary>
    public int? SentByUserId { get; set; }

    /// <summary>
    /// Gönderen kullanıcı
    /// </summary>
    public User? SentByUser { get; set; }

    /// <summary>
    /// Email türü: 1=Initial (ilk atama), 2=Reminder (hatırlatma)
    /// </summary>
    public int EmailTypeId { get; set; } = 1;

    /// <summary>
    /// Email başarılı mı?
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
