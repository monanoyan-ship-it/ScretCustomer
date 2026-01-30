using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.DTOs.TrainingVideo;

/// <summary>
/// Video listesi için hafif DTO
/// </summary>
public class TrainingVideoListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationSeconds { get; set; }
    public long FileSize { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ScopeCount { get; set; }
    public int AssignmentCount { get; set; }
    public int TotalParticipants { get; set; }
    public int CompletedParticipants { get; set; }
    public string? ThumbnailUrl { get; set; }

    // İzleme kontrol ayarları
    public int MinWatchPercentage { get; set; }
    public bool AllowSkipping { get; set; }
    public decimal MaxPlaybackSpeed { get; set; }
}

/// <summary>
/// Video detay DTO
/// </summary>
public class TrainingVideoDto : TrainingVideoListDto
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public List<TrainingVideoScopeDto> Scopes { get; set; } = new();
}

/// <summary>
/// Video oluşturma DTO
/// </summary>
public class CreateTrainingVideoDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationSeconds { get; set; }
    public List<CreateTrainingVideoScopeDto> Scopes { get; set; } = new();

    // İzleme kontrol ayarları
    public int MinWatchPercentage { get; set; } = 90;
    public bool AllowSkipping { get; set; } = false;
    public decimal MaxPlaybackSpeed { get; set; } = 1.0m;
}

/// <summary>
/// Video güncelleme DTO
/// </summary>
public class UpdateTrainingVideoDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<CreateTrainingVideoScopeDto> Scopes { get; set; } = new();

    // İzleme kontrol ayarları
    public int MinWatchPercentage { get; set; } = 90;
    public bool AllowSkipping { get; set; } = false;
    public decimal MaxPlaybackSpeed { get; set; } = 1.0m;
}

/// <summary>
/// Video kapsam DTO
/// </summary>
public class TrainingVideoScopeDto
{
    public int Id { get; set; }
    public int ScopeTypeId { get; set; }
    public string ScopeTypeName { get; set; } = string.Empty;
    public int? ChecklistId { get; set; }
    public string? ChecklistName { get; set; }
    public string? QuestionGroupName { get; set; }
    public int? QuestionId { get; set; }
    public string? QuestionText { get; set; }
}

/// <summary>
/// Kapsam oluşturma DTO
/// </summary>
public class CreateTrainingVideoScopeDto
{
    public int ScopeTypeId { get; set; }
    public int? ChecklistId { get; set; }
    public string? QuestionGroupName { get; set; }
    public int? QuestionId { get; set; }
}

/// <summary>
/// Kapsam seçenekleri - Checklist, Soru Grupları, Sorular
/// </summary>
public class TrainingVideoScopeOptionsDto
{
    public List<ChecklistOptionDto> Checklists { get; set; } = new();
    public List<QuestionGroupOptionDto> QuestionGroups { get; set; } = new();
    public List<QuestionOptionDto> Questions { get; set; } = new();
}

public class ChecklistOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class QuestionGroupOptionDto
{
    public int ChecklistId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
}

public class QuestionOptionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public int ChecklistId { get; set; }
    public string? ChecklistName { get; set; }
}

/// <summary>
/// Video ataması listesi DTO
/// </summary>
public class TrainingVideoAssignmentListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TrainingVideoId { get; set; }
    public string TrainingVideoTitle { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsExternal { get; set; }
    public int TotalParticipants { get; set; }
    public int CompletedParticipants { get; set; }
    public int InProgressParticipants { get; set; }
    public decimal CompletionPercentage { get; set; }

    // Email istatistikleri
    public int EmailSentParticipants { get; set; }
    public int NotStartedParticipants { get; set; }
}

/// <summary>
/// Video ataması detay DTO
/// </summary>
public class TrainingVideoAssignmentDto : TrainingVideoAssignmentListDto
{
    public int? SourceProjectId { get; set; }
    public string? SourceProjectName { get; set; }
    public decimal? ScoreThreshold { get; set; }
    public DateTime? SourceStartDate { get; set; }
    public DateTime? SourceEndDate { get; set; }
    public List<TrainingVideoParticipantDto> Participants { get; set; } = new();

    // İzleme ayarları
    public int MinWatchCount { get; set; }
    public int? MaxWatchCount { get; set; }
    public bool AllowSpeedChange { get; set; }
    public bool AllowSeeking { get; set; }
    public int? EmailTemplateId { get; set; }
}

/// <summary>
/// Atama oluşturma DTO
/// </summary>
public class CreateTrainingVideoAssignmentDto
{
    public string Title { get; set; } = string.Empty;
    public int TrainingVideoId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsExternal { get; set; } = false;

    // Otomatik atama kriterleri - Filtreler
    public int? CustomerId { get; set; }
    public int? OrganizationId { get; set; }
    public int? ProjectId { get; set; }
    public decimal? ScoreThreshold { get; set; }
    public DateTime? SourceStartDate { get; set; }
    public DateTime? SourceEndDate { get; set; }

    // Manuel katılımcı listesi
    public List<int>? ManualUserIds { get; set; }

    // Email ayarları
    public int? EmailTemplateId { get; set; }
    public bool SendEmail { get; set; } = true;

    // İzleme ayarları
    public int MinWatchCount { get; set; } = 1;
    public int? MaxWatchCount { get; set; }
    public bool AllowSpeedChange { get; set; } = false;
    public bool AllowSeeking { get; set; } = false;
}

/// <summary>
/// Atama güncelleme DTO
/// </summary>
public class UpdateTrainingVideoAssignmentDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Email ayarları
    public int? EmailTemplateId { get; set; }

    // İzleme ayarları
    public int MinWatchCount { get; set; } = 1;
    public int? MaxWatchCount { get; set; }
    public bool AllowSpeedChange { get; set; } = false;
    public bool AllowSeeking { get; set; } = false;

    // Katılımcı ekleme/çıkarma (iç katılımcılar)
    public List<int>? AddParticipantIds { get; set; }
    public List<int>? RemoveParticipantIds { get; set; }

    // Dış katılımcı ekleme/çıkarma
    public List<ExternalParticipantItemDto>? AddExternalParticipants { get; set; }
    public List<int>? RemoveExternalParticipantIds { get; set; }
}

/// <summary>
/// Otomatik atama önizleme DTO
/// </summary>
public class AssignmentPreviewDto
{
    public int TotalUsers { get; set; }
    public List<AssignmentPreviewUserDto> Users { get; set; } = new();
}

public class AssignmentPreviewUserDto
{
    public int UserId { get; set; } // CustomerPersonnelId
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? CustomerName { get; set; }
    public decimal? ScopeScore { get; set; }
}

/// <summary>
/// Video katılımcı DTO (CustomerPersonnel)
/// </summary>
public class TrainingVideoParticipantDto
{
    public int Id { get; set; }
    public int UserId { get; set; } // CustomerPersonnelId
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int WatchedSeconds { get; set; }
    public bool IsCompleted { get; set; }

    // Email durumu
    public DateTime? FirstEmailSentAt { get; set; }
    public DateTime? LastEmailSentAt { get; set; }
    public int EmailSentCount { get; set; }
}

/// <summary>
/// Toplu email gönderim DTO
/// </summary>
public class SendTrainingEmailDto
{
    public int AssignmentId { get; set; }
    public List<int> ParticipantIds { get; set; } = new();
    public int? EmailTemplateId { get; set; }
    /// <summary>
    /// Email türü: 1=Initial (ilk atama), 2=Reminder (hatırlatma)
    /// </summary>
    public int EmailTypeId { get; set; } = 1;
}

/// <summary>
/// Katılımcı filtre DTO (email modal için)
/// </summary>
public class ParticipantFilterDto
{
    public bool? EmailSent { get; set; }      // null=tümü, true=email gitmiş, false=email gitmemiş
    public bool? HasStarted { get; set; }     // null=tümü, true=izlemeye başlamış, false=başlamamış
    public bool? IsCompleted { get; set; }    // null=tümü, true=tamamlamış, false=tamamlamamış
}

/// <summary>
/// Kullanıcının kendi eğitimleri için DTO
/// </summary>
public class MyTrainingDto
{
    public int ParticipantId { get; set; }
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public int VideoId { get; set; }
    public string VideoTitle { get; set; } = string.Empty;
    public string? VideoDescription { get; set; }
    public int VideoDurationSeconds { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int WatchedSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysRemaining { get; set; }

    // Video player kontrol ayarları
    public int MinWatchPercentage { get; set; }
    public bool AllowSkipping { get; set; }
    public decimal MaxPlaybackSpeed { get; set; }
}

/// <summary>
/// Video izleme ilerleme güncelleme DTO
/// </summary>
public class UpdateWatchProgressDto
{
    public int WatchedSeconds { get; set; }
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Video sonuna ulaşıldı mı? (WatchCount artırımı için)
    /// </summary>
    public bool IsVideoEnded { get; set; }
}

/// <summary>
/// Video filtreleme DTO
/// </summary>
public class TrainingVideoFilterDto
{
    public string? SearchTerm { get; set; }
    public List<int>? ChecklistIds { get; set; }
    public List<string>? QuestionGroupNames { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Tüm katılımcılar listesi DTO (participants tab için)
/// </summary>
public class AllParticipantDto
{
    public int Id { get; set; }
    public int UserId { get; set; } // CustomerPersonnelId
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? CustomerName { get; set; }
    public int VideoId { get; set; }
    public string VideoTitle { get; set; } = string.Empty;
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int WatchedSeconds { get; set; }
    public int VideoDurationSeconds { get; set; }
    public int WatchProgress { get; set; } // Yüzde olarak izleme ilerlemesi
}

/// <summary>
/// Atama filtreleme DTO (KURALLAR.md Section 20 Pattern)
/// </summary>
public class TrainingVideoAssignmentFilterDto
{
    public string? SearchTerm { get; set; }
    public List<int>? VideoIds { get; set; }
    public List<int>? ProjectIds { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>
    /// Tarih aralığı filtreleri (DateRanges pattern)
    /// StartDate için: DateRanges[0].StartDate, DateRanges[0].EndDate
    /// DueDate için: Ayrı bir filter tipi olarak yönetilir
    /// </summary>
    public List<DateRangeFilter>? DateRanges { get; set; }

    /// <summary>
    /// Tarih filtresi tipi: "start" = Başlangıç tarihi, "due" = Bitiş tarihi
    /// </summary>
    public string? DateFilterType { get; set; }
}

// ===== DIŞ KATILIMCI DTO'LARI =====

/// <summary>
/// Dış katılımcı listeleme DTO
/// </summary>
public class TrainingVideoExternalParticipantDto
{
    public int Id { get; set; }
    public int TrainingVideoAssignmentId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string Token { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsOpened { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int WatchedSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public int WatchCount { get; set; }
    public DateTime? FirstEmailSentAt { get; set; }
    public DateTime? LastEmailSentAt { get; set; }
    public int EmailSentCount { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Video izleme linki
    /// </summary>
    public string WatchUrl { get; set; } = string.Empty;
}

/// <summary>
/// Tekil dış katılımcı ekleme DTO
/// </summary>
public class ExternalParticipantItemDto
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

/// <summary>
/// Toplu dış katılımcı ekleme DTO
/// </summary>
public class AddExternalParticipantsDto
{
    public int AssignmentId { get; set; }

    /// <summary>
    /// Eklenecek katılımcılar
    /// </summary>
    public List<ExternalParticipantItemDto> Participants { get; set; } = new();

    /// <summary>
    /// Ekleme sonrası email gönderilsin mi?
    /// </summary>
    public bool SendEmail { get; set; } = true;

    /// <summary>
    /// Email şablon ID (opsiyonel - belirtilmezse assignment'ın şablonu kullanılır)
    /// </summary>
    public int? EmailTemplateId { get; set; }
}

/// <summary>
/// Toplu dış katılımcı ekleme sonuç DTO
/// </summary>
public class AddExternalParticipantsResultDto
{
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
    public int EmailSentCount { get; set; }
    public List<string> SkippedEmails { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Text parse sonucu DTO (email;ad;soyad formatından)
/// </summary>
public class ParsedExternalParticipantDto
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Dış katılımcılara email gönderme DTO
/// </summary>
public class SendExternalEmailsDto
{
    public int AssignmentId { get; set; }
    public List<int> ParticipantIds { get; set; } = new();

    /// <summary>
    /// Email türü: 1=Initial, 2=Reminder
    /// </summary>
    public int EmailTypeId { get; set; } = 1;

    /// <summary>
    /// Email şablon ID (opsiyonel)
    /// </summary>
    public int? EmailTemplateId { get; set; }
}

/// <summary>
/// Token ile video bilgisi DTO (halka açık izleme sayfası için)
/// </summary>
public class ExternalVideoWatchDto
{
    public int ParticipantId { get; set; }
    public string VideoTitle { get; set; } = string.Empty;
    public string? VideoDescription { get; set; }
    public int VideoDurationSeconds { get; set; }
    public string VideoStreamUrl { get; set; } = string.Empty;
    public string AssignmentTitle { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int MinWatchCount { get; set; }
    public int? MaxWatchCount { get; set; }
    public bool AllowSpeedChange { get; set; }
    public bool AllowSeeking { get; set; }
    public int CurrentWatchCount { get; set; }
    public int WatchedSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsExpired { get; set; }
    public string? ParticipantName { get; set; }
}

/// <summary>
/// Video scope'una göre personel arama sonucu DTO
/// </summary>
public class ScopePersonnelSearchResultDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? CustomerName { get; set; }
}
