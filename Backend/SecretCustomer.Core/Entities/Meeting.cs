using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Toplantı entity'si
/// </summary>
public class Meeting : BaseEntity
{
    /// <summary>
    /// Toplantı başlığı
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Toplantı açıklaması
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Toplantı tipi (MeetingTypes.Ids kullanılır)
    /// </summary>
    public int MeetingTypeId { get; set; } = MeetingTypes.Ids.General;

    /// <summary>
    /// Toplantı durumu (MeetingStatuses.Ids kullanılır)
    /// </summary>
    public int StatusId { get; set; } = MeetingStatuses.Ids.Planned;

    /// <summary>
    /// Planlanan tarih/saat
    /// </summary>
    public DateTime PlannedDate { get; set; }

    /// <summary>
    /// Planlanan bitiş tarihi/saati
    /// </summary>
    public DateTime? PlannedEndDate { get; set; }

    /// <summary>
    /// Gerçekleşen tarih/saat
    /// </summary>
    public DateTime? ActualDate { get; set; }

    /// <summary>
    /// Gerçekleşen bitiş tarihi/saati
    /// </summary>
    public DateTime? ActualEndDate { get; set; }

    /// <summary>
    /// Toplantı süresi (dakika)
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Fiziksel lokasyon
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Online mı?
    /// </summary>
    public bool IsOnline { get; set; } = false;

    /// <summary>
    /// Online toplantı linki
    /// </summary>
    public string? OnlineLink { get; set; }

    /// <summary>
    /// Online platform (Zoom, Teams, Meet vb.)
    /// </summary>
    public string? OnlinePlatform { get; set; }

    /// <summary>
    /// Toplantı notları
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gündem
    /// </summary>
    public string? Agenda { get; set; }

    /// <summary>
    /// Toplantı tutanağı
    /// </summary>
    public string? Minutes { get; set; }

    /// <summary>
    /// Kararlar
    /// </summary>
    public string? Decisions { get; set; }

    /// <summary>
    /// Organizatör kullanıcı ID
    /// </summary>
    public int OrganizerId { get; set; }

    /// <summary>
    /// Organizatör
    /// </summary>
    public User? Organizer { get; set; }

    /// <summary>
    /// İlişkili proje ID
    /// </summary>
    public int? ProjectId { get; set; }

    /// <summary>
    /// İlişkili proje
    /// </summary>
    public Project? Project { get; set; }

    /// <summary>
    /// İlişkili müşteri ID
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// İlişkili müşteri
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Hatırlatma gönderildi mi?
    /// </summary>
    public bool ReminderSent { get; set; } = false;

    /// <summary>
    /// Hatırlatma zamanı (saat önce)
    /// </summary>
    public int? ReminderHoursBefore { get; set; } = 24;

    /// <summary>
    /// Toplantı katılımcıları
    /// </summary>
    public ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();

    /// <summary>
    /// Toplantı ekleri
    /// </summary>
    public ICollection<MeetingAttachment> Attachments { get; set; } = new List<MeetingAttachment>();
}

/// <summary>
/// Toplantı katılımcısı
/// </summary>
public class MeetingParticipant : BaseEntity
{
    /// <summary>
    /// Toplantı ID
    /// </summary>
    public int MeetingId { get; set; }

    /// <summary>
    /// Toplantı
    /// </summary>
    public Meeting Meeting { get; set; } = null!;

    /// <summary>
    /// Kullanıcı ID (sistem kullanıcısı ise)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Kullanıcı
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Harici katılımcı adı
    /// </summary>
    public string? ExternalName { get; set; }

    /// <summary>
    /// Harici katılımcı email
    /// </summary>
    public string? ExternalEmail { get; set; }

    /// <summary>
    /// Katılımcı durumu (ParticipantStatuses.Ids kullanılır)
    /// </summary>
    public int StatusId { get; set; } = ParticipantStatuses.Ids.Invited;

    /// <summary>
    /// Zorunlu katılımcı mı?
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Yanıt notu
    /// </summary>
    public string? ResponseNote { get; set; }

    /// <summary>
    /// Yanıt tarihi
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// Katılım notu
    /// </summary>
    public string? AttendanceNote { get; set; }
}

/// <summary>
/// Toplantı eki
/// </summary>
public class MeetingAttachment : BaseEntity
{
    /// <summary>
    /// Toplantı ID
    /// </summary>
    public int MeetingId { get; set; }

    /// <summary>
    /// Toplantı
    /// </summary>
    public Meeting Meeting { get; set; } = null!;

    /// <summary>
    /// Dosya adı
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Dosya yolu
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Dosya boyutu (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME tipi
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Yükleyen kullanıcı ID
    /// </summary>
    public int? UploadedByUserId { get; set; }

    /// <summary>
    /// Yükleyen kullanıcı
    /// </summary>
    public User? UploadedByUser { get; set; }
}
