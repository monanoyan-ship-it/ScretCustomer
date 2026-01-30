namespace SecretCustomer.Core.Entities;

/// <summary>
/// Eğitim Videosu Anketi - Video tamamlandıktan sonra gösterilecek quiz
/// Evaluation/Checklist sisteminden AYRI bir yapı
/// </summary>
public class TrainingQuiz : BaseEntity
{
    /// <summary>
    /// Video ID (hangi videoya bağlı - Video tarafından seçilir)
    /// </summary>
    public int? TrainingVideoId { get; set; }

    /// <summary>
    /// Video
    /// </summary>
    public TrainingVideo? TrainingVideo { get; set; }

    /// <summary>
    /// Anket başlığı
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Geçme notu (% olarak, opsiyonel). Örn: 70 = %70 başarı gerekli
    /// </summary>
    public int? PassingScore { get; set; }

    /// <summary>
    /// Anket zorunlu mu? Video bazlı ayar
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Soruları karıştır
    /// </summary>
    public bool ShuffleQuestions { get; set; } = false;

    /// <summary>
    /// Seçenekleri karıştır
    /// </summary>
    public bool ShuffleOptions { get; set; } = false;

    /// <summary>
    /// Sonuçları göster (cevap sonrası doğru/yanlış)
    /// </summary>
    public bool ShowResults { get; set; } = true;

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Anket soruları
    /// </summary>
    public ICollection<TrainingQuizQuestion> Questions { get; set; } = new List<TrainingQuizQuestion>();

    /// <summary>
    /// Anket cevapları (katılımcı yanıtları)
    /// </summary>
    public ICollection<TrainingQuizResponse> Responses { get; set; } = new List<TrainingQuizResponse>();
}

/// <summary>
/// Anket Sorusu
/// </summary>
public class TrainingQuizQuestion : BaseEntity
{
    /// <summary>
    /// Anket ID
    /// </summary>
    public int TrainingQuizId { get; set; }

    /// <summary>
    /// Anket
    /// </summary>
    public TrainingQuiz TrainingQuiz { get; set; } = null!;

    /// <summary>
    /// Soru metni
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Yardım metni (opsiyonel açıklama)
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Sıra numarası
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Soru tipi: 1=SingleChoice (Tekli seçim), 2=MultipleChoice (Çoklu seçim)
    /// </summary>
    public int QuestionTypeId { get; set; } = 1;

    /// <summary>
    /// Seçenekler
    /// </summary>
    public ICollection<TrainingQuizOption> Options { get; set; } = new List<TrainingQuizOption>();

    /// <summary>
    /// Bu soruya verilen cevaplar
    /// </summary>
    public ICollection<TrainingQuizAnswer> Answers { get; set; } = new List<TrainingQuizAnswer>();
}

/// <summary>
/// Anket Soru Seçeneği
/// </summary>
public class TrainingQuizOption : BaseEntity
{
    /// <summary>
    /// Soru ID
    /// </summary>
    public int TrainingQuizQuestionId { get; set; }

    /// <summary>
    /// Soru
    /// </summary>
    public TrainingQuizQuestion Question { get; set; } = null!;

    /// <summary>
    /// Seçenek metni
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Sıra numarası
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Seçenek ağırlık puanı - Puan buradan gelecek
    /// Doğru cevap için pozitif, yanlış için 0 veya negatif olabilir
    /// </summary>
    public decimal WeightPoints { get; set; }

    /// <summary>
    /// Doğru cevap mı?
    /// </summary>
    public bool IsCorrect { get; set; }
}

/// <summary>
/// Anket Yanıtı (Header) - Katılımcının ankete verdiği cevapların özeti
/// </summary>
public class TrainingQuizResponse : BaseEntity
{
    /// <summary>
    /// Anket ID
    /// </summary>
    public int TrainingQuizId { get; set; }

    /// <summary>
    /// Anket
    /// </summary>
    public TrainingQuiz TrainingQuiz { get; set; } = null!;

    /// <summary>
    /// Dahili katılımcı ID (varsa)
    /// </summary>
    public int? TrainingVideoParticipantId { get; set; }

    /// <summary>
    /// Dahili katılımcı
    /// </summary>
    public TrainingVideoParticipant? Participant { get; set; }

    /// <summary>
    /// Dış katılımcı ID (varsa)
    /// </summary>
    public int? TrainingVideoExternalParticipantId { get; set; }

    /// <summary>
    /// Dış katılımcı
    /// </summary>
    public TrainingVideoExternalParticipant? ExternalParticipant { get; set; }

    /// <summary>
    /// Toplam alınan puan
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// Maksimum alınabilecek puan
    /// </summary>
    public decimal MaxPossibleScore { get; set; }

    /// <summary>
    /// Başarı yüzdesi
    /// </summary>
    public decimal ScorePercentage { get; set; }

    /// <summary>
    /// Geçti mi? (PassingScore'u geçti mi)
    /// </summary>
    public bool IsPassed { get; set; }

    /// <summary>
    /// Başlangıç zamanı
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Tamamlanma zamanı
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Durum: 1=Started, 2=InProgress, 3=Completed
    /// </summary>
    public int StatusId { get; set; } = 1;

    /// <summary>
    /// Deneme numarası (kaçıncı deneme)
    /// </summary>
    public int AttemptNumber { get; set; } = 1;

    /// <summary>
    /// Cevap detayları
    /// </summary>
    public ICollection<TrainingQuizAnswer> Answers { get; set; } = new List<TrainingQuizAnswer>();
}

/// <summary>
/// Anket Cevap Detayı - Her soruya verilen cevap
/// </summary>
public class TrainingQuizAnswer : BaseEntity
{
    /// <summary>
    /// Yanıt (Response) ID
    /// </summary>
    public int TrainingQuizResponseId { get; set; }

    /// <summary>
    /// Yanıt
    /// </summary>
    public TrainingQuizResponse Response { get; set; } = null!;

    /// <summary>
    /// Soru ID
    /// </summary>
    public int TrainingQuizQuestionId { get; set; }

    /// <summary>
    /// Soru
    /// </summary>
    public TrainingQuizQuestion Question { get; set; } = null!;

    /// <summary>
    /// Seçilen seçenek ID (tekli seçim için)
    /// </summary>
    public int? SelectedOptionId { get; set; }

    /// <summary>
    /// Seçilen seçenek
    /// </summary>
    public TrainingQuizOption? SelectedOption { get; set; }

    /// <summary>
    /// Seçilen seçenek ID'leri (çoklu seçim için, virgülle ayrılmış)
    /// </summary>
    public string? SelectedOptionIds { get; set; }

    /// <summary>
    /// Kazanılan puan
    /// </summary>
    public decimal EarnedPoints { get; set; }

    /// <summary>
    /// Doğru mu?
    /// </summary>
    public bool IsCorrect { get; set; }
}
