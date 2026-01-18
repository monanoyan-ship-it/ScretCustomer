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
    public int TotalParticipants { get; set; }
    public int CompletedParticipants { get; set; }
    public int InProgressParticipants { get; set; }
    public decimal CompletionPercentage { get; set; }
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

    // Otomatik atama kriterleri
    public int? ProjectId { get; set; }
    public decimal? ScoreThreshold { get; set; }
    public DateTime? SourceStartDate { get; set; }
    public DateTime? SourceEndDate { get; set; }

    // Manuel katılımcı listesi
    public List<int>? ManualUserIds { get; set; }
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
}

/// <summary>
/// Video izleme ilerleme güncelleme DTO
/// </summary>
public class UpdateWatchProgressDto
{
    public int WatchedSeconds { get; set; }
    public bool IsCompleted { get; set; }
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
/// Atama filtreleme DTO
/// </summary>
public class TrainingVideoAssignmentFilterDto
{
    public string? SearchTerm { get; set; }
    public List<int>? VideoIds { get; set; }
    public List<int>? ProjectIds { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
}
