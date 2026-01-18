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
    /// Katılımcılar
    /// </summary>
    public ICollection<TrainingVideoParticipant> Participants { get; set; } = new List<TrainingVideoParticipant>();
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
    /// Kaynak değerlendirme ID (varsa - bu değerlendirmeden dolayı atandı)
    /// </summary>
    public int? SourceEvaluationId { get; set; }

    /// <summary>
    /// Kaynak değerlendirme
    /// </summary>
    public Evaluation? SourceEvaluation { get; set; }
}
