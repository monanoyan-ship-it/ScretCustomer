using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.DTOs.Meeting;

/// <summary>
/// Toplantı DTO
/// </summary>
public class MeetingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MeetingType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PlannedDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Location { get; set; }
    public bool IsOnline { get; set; }
    public string? OnlineLink { get; set; }
    public string? OnlinePlatform { get; set; }
    public string? Notes { get; set; }
    public string? Agenda { get; set; }
    public string? Minutes { get; set; }
    public string? Decisions { get; set; }
    public int OrganizerId { get; set; }
    public string? OrganizerName { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? ReminderHoursBefore { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MeetingParticipantDto> Participants { get; set; } = new();
    public List<MeetingAttachmentDto> Attachments { get; set; } = new();
    public int ParticipantCount { get; set; }
    public int AcceptedCount { get; set; }
    public int DeclinedCount { get; set; }
}

/// <summary>
/// Toplantı listesi için özet DTO
/// </summary>
public class MeetingListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MeetingType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PlannedDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public string? Location { get; set; }
    public bool IsOnline { get; set; }
    public string? OrganizerName { get; set; }
    public string? ProjectName { get; set; }
    public string? CustomerName { get; set; }
    public int ParticipantCount { get; set; }
    public int AcceptedCount { get; set; }
}

/// <summary>
/// Toplantı oluşturma DTO
/// </summary>
public class CreateMeetingDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MeetingType { get; set; } = "General";
    public DateTime PlannedDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Location { get; set; }
    public bool IsOnline { get; set; }
    public string? OnlineLink { get; set; }
    public string? OnlinePlatform { get; set; }
    public string? Agenda { get; set; }
    public int? ProjectId { get; set; }
    public int? CustomerId { get; set; }
    public int? ReminderHoursBefore { get; set; } = 24;
    public List<CreateMeetingParticipantDto> Participants { get; set; } = new();
}

/// <summary>
/// Toplantı güncelleme DTO
/// </summary>
public class UpdateMeetingDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MeetingType { get; set; } = "General";
    public string Status { get; set; } = "Planned";
    public DateTime PlannedDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Location { get; set; }
    public bool IsOnline { get; set; }
    public string? OnlineLink { get; set; }
    public string? OnlinePlatform { get; set; }
    public string? Notes { get; set; }
    public string? Agenda { get; set; }
    public string? Minutes { get; set; }
    public string? Decisions { get; set; }
    public int? ProjectId { get; set; }
    public int? CustomerId { get; set; }
    public int? ReminderHoursBefore { get; set; }
}

/// <summary>
/// Toplantı tamamlama DTO
/// </summary>
public class CompleteMeetingDto
{
    public DateTime? ActualDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public string? Minutes { get; set; }
    public string? Decisions { get; set; }
    public string? Notes { get; set; }
    public List<UpdateParticipantAttendanceDto> Attendances { get; set; } = new();
}

/// <summary>
/// Katılımcı DTO
/// </summary>
public class MeetingParticipantDto
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? ExternalName { get; set; }
    public string? ExternalEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? ResponseNote { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? AttendanceNote { get; set; }
    public string DisplayName => !string.IsNullOrEmpty(UserName) ? UserName : ExternalName ?? "";
    public string DisplayEmail => !string.IsNullOrEmpty(UserEmail) ? UserEmail : ExternalEmail ?? "";
}

/// <summary>
/// Katılımcı ekleme DTO
/// </summary>
public class CreateMeetingParticipantDto
{
    public int? UserId { get; set; }
    public string? ExternalName { get; set; }
    public string? ExternalEmail { get; set; }
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// Katılımcı yanıtı DTO
/// </summary>
public class RespondToMeetingDto
{
    public string Status { get; set; } = "Accepted";
    public string? ResponseNote { get; set; }
}

/// <summary>
/// Katılım durumu güncelleme DTO
/// </summary>
public class UpdateParticipantAttendanceDto
{
    public int ParticipantId { get; set; }
    public string Status { get; set; } = "Attended";
    public string? AttendanceNote { get; set; }
}

/// <summary>
/// Toplantı eki DTO
/// </summary>
public class MeetingAttachmentDto
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? Description { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DownloadUrl => $"/api/meetings/{MeetingId}/attachments/{Id}/download";
}

/// <summary>
/// Toplantı filtre DTO
/// </summary>
public class MeetingFilterDto
{
    // Çoklu filtreler (OR mantığı)
    public List<int>? ProjectIds { get; set; }
    public List<int>? CustomerIds { get; set; }
    public List<int>? OrganizerIds { get; set; }
    public List<string>? MeetingTypes { get; set; }
    public List<string>? Statuses { get; set; }

    // Tarih filtresi (çoklu)
    public List<DateRangeFilter>? DateRanges { get; set; }
    public bool? IsOnline { get; set; }
    public bool? MyMeetingsOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // Sorting
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Dashboard için toplantı özeti
/// </summary>
public class MeetingSummaryDto
{
    public int TotalMeetings { get; set; }
    public int UpcomingMeetings { get; set; }
    public int TodayMeetings { get; set; }
    public int CompletedMeetings { get; set; }
    public int CancelledMeetings { get; set; }
    public List<MeetingListDto> UpcomingList { get; set; } = new();
}
