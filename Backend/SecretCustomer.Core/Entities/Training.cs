using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Eğitim entity'si
/// </summary>
public class Training : BaseEntity
{
    /// <summary>
    /// Eğitim kodu
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Eğitim başlığı
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Eğitim tipi
    /// </summary>
    public TrainingType TrainingType { get; set; } = TrainingType.InPerson;

    /// <summary>
    /// Durum
    /// </summary>
    public TrainingStatus Status { get; set; } = TrainingStatus.Draft;

    /// <summary>
    /// Kategori
    /// </summary>
    public TrainingCategory Category { get; set; } = TrainingCategory.General;

    /// <summary>
    /// Başlangıç tarihi
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Bitiş tarihi
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Süre (dakika)
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Konum
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Online link
    /// </summary>
    public string? OnlineLink { get; set; }

    /// <summary>
    /// Maksimum katılımcı sayısı
    /// </summary>
    public int? MaxParticipants { get; set; }

    /// <summary>
    /// Zorunlu eğitim mi?
    /// </summary>
    public bool IsMandatory { get; set; } = false;

    /// <summary>
    /// Sertifika veriliyor mu?
    /// </summary>
    public bool HasCertificate { get; set; } = false;

    /// <summary>
    /// Geçme notu (yüzde)
    /// </summary>
    public int? PassingScore { get; set; }

    /// <summary>
    /// İçerik / Müfredat
    /// </summary>
    public string? Curriculum { get; set; }

    /// <summary>
    /// Ön koşullar
    /// </summary>
    public string? Prerequisites { get; set; }

    /// <summary>
    /// Hedefler
    /// </summary>
    public string? Objectives { get; set; }

    /// <summary>
    /// Eğitmen ID
    /// </summary>
    public Guid? TrainerId { get; set; }

    /// <summary>
    /// Eğitmen
    /// </summary>
    public User? Trainer { get; set; }

    /// <summary>
    /// Dış eğitmen adı
    /// </summary>
    public string? ExternalTrainerName { get; set; }

    /// <summary>
    /// Dış eğitmen kurum
    /// </summary>
    public string? ExternalTrainerCompany { get; set; }

    /// <summary>
    /// Oluşturan kullanıcı ID
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// Oluşturan kullanıcı
    /// </summary>
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// İlişkili proje ID
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// İlişkili proje
    /// </summary>
    public Project? Project { get; set; }

    /// <summary>
    /// İlişkili müşteri ID
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// İlişkili müşteri
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Maliyet
    /// </summary>
    public decimal? Cost { get; set; }

    /// <summary>
    /// Para birimi
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Etiketler
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Katılımcılar
    /// </summary>
    public ICollection<TrainingParticipant> Participants { get; set; } = new List<TrainingParticipant>();

    /// <summary>
    /// Materyaller
    /// </summary>
    public ICollection<TrainingMaterial> Materials { get; set; } = new List<TrainingMaterial>();
}

/// <summary>
/// Eğitim katılımcısı
/// </summary>
public class TrainingParticipant : BaseEntity
{
    /// <summary>
    /// Eğitim ID
    /// </summary>
    public Guid TrainingId { get; set; }

    /// <summary>
    /// Eğitim
    /// </summary>
    public Training Training { get; set; } = null!;

    /// <summary>
    /// Kullanıcı ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Kullanıcı
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Dış katılımcı adı
    /// </summary>
    public string? ExternalName { get; set; }

    /// <summary>
    /// Dış katılımcı email
    /// </summary>
    public string? ExternalEmail { get; set; }

    /// <summary>
    /// Dış katılımcı telefon
    /// </summary>
    public string? ExternalPhone { get; set; }

    /// <summary>
    /// Durum
    /// </summary>
    public TrainingParticipantStatus Status { get; set; } = TrainingParticipantStatus.Invited;

    /// <summary>
    /// Katılım tarihi
    /// </summary>
    public DateTime? AttendanceDate { get; set; }

    /// <summary>
    /// Katılım süresi (dakika)
    /// </summary>
    public int? AttendanceMinutes { get; set; }

    /// <summary>
    /// Test puanı
    /// </summary>
    public int? Score { get; set; }

    /// <summary>
    /// Sertifika verildi mi?
    /// </summary>
    public bool CertificateIssued { get; set; } = false;

    /// <summary>
    /// Sertifika tarihi
    /// </summary>
    public DateTime? CertificateDate { get; set; }

    /// <summary>
    /// Geri bildirim
    /// </summary>
    public string? Feedback { get; set; }

    /// <summary>
    /// Değerlendirme puanı (1-5)
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// Notlar
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Eğitim materyali
/// </summary>
public class TrainingMaterial : BaseEntity
{
    /// <summary>
    /// Eğitim ID
    /// </summary>
    public Guid TrainingId { get; set; }

    /// <summary>
    /// Eğitim
    /// </summary>
    public Training Training { get; set; } = null!;

    /// <summary>
    /// Materyal adı
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Dosya adı
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Dosya yolu
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Dosya boyutu
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// İçerik tipi
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Harici link
    /// </summary>
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// Sıralama
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Zorunlu mu?
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Yükleyen kullanıcı ID
    /// </summary>
    public Guid? UploadedByUserId { get; set; }

    /// <summary>
    /// Yükleyen kullanıcı
    /// </summary>
    public User? UploadedByUser { get; set; }
}
