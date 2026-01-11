using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Meeting;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/meetings")]
[Authorize]
public class MeetingsApiController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MeetingsApiController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly ILocalizationService _localizationService;

    public MeetingsApiController(
        ApplicationDbContext context,
        ILogger<MeetingsApiController> logger,
        IWebHostEnvironment env,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _context = context;
        _logger = logger;
        _env = env;
        _localizationService = localizationService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Toplantı listesi
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMeetings([FromQuery] MeetingFilterDto filter)
    {
        var currentUserId = GetCurrentUserId();
        var query = _context.Meetings
            .Include(m => m.Organizer)
            .Include(m => m.Project)
            .Include(m => m.Customer)
            .Include(m => m.Participants)
            .AsQueryable();

        // Filters
        if (filter.ProjectId.HasValue)
            query = query.Where(m => m.ProjectId == filter.ProjectId.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(m => m.CustomerId == filter.CustomerId.Value);

        if (filter.OrganizerId.HasValue)
            query = query.Where(m => m.OrganizerId == filter.OrganizerId.Value);

        if (!string.IsNullOrEmpty(filter.MeetingType))
        {
            var meetingTypeItem = MeetingTypes.GetBySystemName(filter.MeetingType);
            if (meetingTypeItem != null)
                query = query.Where(m => m.MeetingTypeId == meetingTypeItem.Id);
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            var statusItem = MeetingStatuses.GetBySystemName(filter.Status);
            if (statusItem != null)
                query = query.Where(m => m.StatusId == statusItem.Id);
        }

        if (filter.StartDate.HasValue)
            query = query.Where(m => m.PlannedDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(m => m.PlannedDate <= filter.EndDate.Value);

        if (filter.IsOnline.HasValue)
            query = query.Where(m => m.IsOnline == filter.IsOnline.Value);

        if (filter.MyMeetingsOnly == true)
            query = query.Where(m => m.OrganizerId == currentUserId || m.Participants.Any(p => p.UserId == currentUserId));

        var totalCount = await query.CountAsync();

        // Dynamic sorting
        var isAscending = filter.SortDirection?.ToLower() == "asc";
        IOrderedQueryable<Meeting> orderedQuery = filter.SortBy?.ToLower() switch
        {
            "title" => isAscending ? query.OrderBy(m => m.Title) : query.OrderByDescending(m => m.Title),
            "meetingtype" => isAscending ? query.OrderBy(m => m.MeetingTypeId) : query.OrderByDescending(m => m.MeetingTypeId),
            "status" => isAscending ? query.OrderBy(m => m.StatusId) : query.OrderByDescending(m => m.StatusId),
            "planneddate" => isAscending ? query.OrderBy(m => m.PlannedDate) : query.OrderByDescending(m => m.PlannedDate),
            "organizername" => isAscending
                ? query.OrderBy(m => m.Organizer != null ? m.Organizer.FirstName : null)
                : query.OrderByDescending(m => m.Organizer != null ? m.Organizer.FirstName : null),
            "participantcount" => isAscending
                ? query.OrderBy(m => m.Participants.Count)
                : query.OrderByDescending(m => m.Participants.Count),
            _ => query.OrderByDescending(m => m.PlannedDate) // Default
        };

        var meetings = await orderedQuery
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(m => new MeetingListDto
            {
                Id = m.Id,
                Title = m.Title,
                MeetingType = m.MeetingTypeId.ToString(),
                Status = m.StatusId.ToString(),
                PlannedDate = m.PlannedDate,
                PlannedEndDate = m.PlannedEndDate,
                Location = m.Location,
                IsOnline = m.IsOnline,
                OrganizerName = m.Organizer != null ? m.Organizer.FirstName + " " + m.Organizer.LastName : null,
                ProjectName = m.Project != null ? m.Project.Name : null,
                CustomerName = m.Customer != null ? m.Customer.CompanyName : null,
                ParticipantCount = m.Participants.Count,
                AcceptedCount = m.Participants.Count(p => p.StatusId == ParticipantStatuses.Ids.Accepted || p.StatusId == ParticipantStatuses.Ids.Attended)
            })
            .ToListAsync();

        return Ok(new
        {
            items = meetings,
            totalCount,
            page = filter.Page,
            pageSize = filter.PageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
        });
    }

    /// <summary>
    /// Toplantı detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMeeting(int id)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Organizer)
            .Include(m => m.Project)
            .Include(m => m.Customer)
            .Include(m => m.Participants)
                .ThenInclude(p => p.User)
            .Include(m => m.Attachments)
                .ThenInclude(a => a.UploadedByUser)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (meeting == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Meeting.NotFound")));

        var dto = MapToDto(meeting);
        return Ok(dto);
    }

    /// <summary>
    /// Toplantı oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingDto dto)
    {
        var currentUserId = GetCurrentUserId();

        var meeting = new Meeting
        {
            Title = dto.Title,
            Description = dto.Description,
            MeetingTypeId = MeetingTypes.GetBySystemName(dto.MeetingType)?.Id ?? MeetingTypes.Ids.General,
            StatusId = MeetingStatuses.Ids.Planned,
            PlannedDate = dto.PlannedDate,
            PlannedEndDate = dto.PlannedEndDate,
            DurationMinutes = dto.DurationMinutes,
            Location = dto.Location,
            IsOnline = dto.IsOnline,
            OnlineLink = dto.OnlineLink,
            OnlinePlatform = dto.OnlinePlatform,
            Agenda = dto.Agenda,
            OrganizerId = currentUserId,
            ProjectId = dto.ProjectId,
            CustomerId = dto.CustomerId,
            ReminderHoursBefore = dto.ReminderHoursBefore ?? 24
        };

        // Add participants
        foreach (var participant in dto.Participants)
        {
            meeting.Participants.Add(new MeetingParticipant
            {
                UserId = participant.UserId,
                ExternalName = participant.ExternalName,
                ExternalEmail = participant.ExternalEmail,
                IsRequired = participant.IsRequired,
                StatusId = ParticipantStatuses.Ids.Invited
            });
        }

        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Meeting created: {MeetingId} by user {UserId}", meeting.Id, currentUserId);

        return CreatedAtAction(nameof(GetMeeting), new { id = meeting.Id }, new { id = meeting.Id, message = await _localizationService.GetResourceAsync("Api.Meeting.CreateSuccess") });
    }

    /// <summary>
    /// Toplantı güncelle
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMeeting(int id, [FromBody] UpdateMeetingDto dto)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Meeting.NotFound")));

        meeting.Title = dto.Title;
        meeting.Description = dto.Description;
        meeting.MeetingTypeId = MeetingTypes.GetBySystemName(dto.MeetingType)?.Id ?? meeting.MeetingTypeId;
        meeting.StatusId = MeetingStatuses.GetBySystemName(dto.Status)?.Id ?? meeting.StatusId;
        meeting.PlannedDate = dto.PlannedDate;
        meeting.PlannedEndDate = dto.PlannedEndDate;
        meeting.DurationMinutes = dto.DurationMinutes;
        meeting.Location = dto.Location;
        meeting.IsOnline = dto.IsOnline;
        meeting.OnlineLink = dto.OnlineLink;
        meeting.OnlinePlatform = dto.OnlinePlatform;
        meeting.Notes = dto.Notes;
        meeting.Agenda = dto.Agenda;
        meeting.Minutes = dto.Minutes;
        meeting.Decisions = dto.Decisions;
        meeting.ProjectId = dto.ProjectId;
        meeting.CustomerId = dto.CustomerId;
        meeting.ReminderHoursBefore = dto.ReminderHoursBefore;

        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Meeting.UpdateSuccess") });
    }

    /// <summary>
    /// Toplantıyı sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMeeting(int id)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Meeting.NotFound")));

        meeting.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Meeting.DeleteSuccess") });
    }

    /// <summary>
    /// Toplantıyı başlat
    /// </summary>
    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartMeeting(int id)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Meeting.NotFound")));

        meeting.StatusId = MeetingStatuses.Ids.InProgress;
        meeting.ActualDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Meeting.StartSuccess") });
    }

    /// <summary>
    /// Toplantıyı tamamla
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteMeeting(int id, [FromBody] CompleteMeetingDto dto)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (meeting == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Meeting.NotFound")));

        meeting.StatusId = MeetingStatuses.Ids.Completed;
        meeting.ActualDate = dto.ActualDate ?? meeting.ActualDate ?? DateTime.UtcNow;
        meeting.ActualEndDate = dto.ActualEndDate ?? DateTime.UtcNow;
        meeting.Minutes = dto.Minutes;
        meeting.Decisions = dto.Decisions;
        meeting.Notes = dto.Notes;

        // Update attendance
        foreach (var attendance in dto.Attendances)
        {
            var participant = meeting.Participants.FirstOrDefault(p => p.Id == attendance.ParticipantId);
            if (participant != null)
            {
                participant.StatusId = ParticipantStatuses.GetBySystemName(attendance.Status)?.Id ?? ParticipantStatuses.Ids.NotAttended;
                participant.AttendanceNote = attendance.AttendanceNote;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Meeting.CompleteSuccess") });
    }

    /// <summary>
    /// Toplantıyı iptal et
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelMeeting(int id, [FromBody] CancelMeetingRequest? request)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Meeting.NotFound")));

        meeting.StatusId = MeetingStatuses.Ids.Cancelled;
        if (!string.IsNullOrEmpty(request?.Reason))
            meeting.Notes = (meeting.Notes ?? "") + "\n[İPTAL]: " + request.Reason;

        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Meeting.CancelSuccess") });
    }

    /// <summary>
    /// Toplantıyı ertele
    /// </summary>
    [HttpPost("{id}/postpone")]
    public async Task<IActionResult> PostponeMeeting(int id, [FromBody] PostponeMeetingRequest request)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Meeting.NotFound")));

        meeting.StatusId = MeetingStatuses.Ids.Postponed;
        meeting.PlannedDate = request.NewDate;
        meeting.PlannedEndDate = request.NewEndDate;
        if (!string.IsNullOrEmpty(request.Reason))
            meeting.Notes = (meeting.Notes ?? "") + "\n[ERTELEME]: " + request.Reason;

        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Meeting.PostponeSuccess") });
    }

    // ===== KATILIMCI İŞLEMLERİ =====

    /// <summary>
    /// Katılımcı ekle
    /// </summary>
    [HttpPost("{id}/participants")]
    public async Task<IActionResult> AddParticipant(int id, [FromBody] CreateMeetingParticipantDto dto)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Meeting.NotFound")));

        var participant = new MeetingParticipant
        {
            MeetingId = id,
            UserId = dto.UserId,
            ExternalName = dto.ExternalName,
            ExternalEmail = dto.ExternalEmail,
            IsRequired = dto.IsRequired,
            StatusId = ParticipantStatuses.Ids.Invited
        };

        _context.MeetingParticipants.Add(participant);
        await _context.SaveChangesAsync();

        return Ok(new { id = participant.Id, message = await _localizationService.GetResourceAsync("Api.Meeting.ParticipantAddSuccess") });
    }

    /// <summary>
    /// Katılımcı sil
    /// </summary>
    [HttpDelete("{id}/participants/{participantId}")]
    public async Task<IActionResult> RemoveParticipant(int id, int participantId)
    {
        var participant = await _context.MeetingParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.MeetingId == id);

        if (participant == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Meeting.ParticipantNotFound")));

        participant.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Meeting.ParticipantRemoveSuccess") });
    }

    /// <summary>
    /// Toplantıya yanıt ver (Kabul/Reddet)
    /// </summary>
    [HttpPost("{id}/respond")]
    public async Task<IActionResult> RespondToMeeting(int id, [FromBody] RespondToMeetingDto dto)
    {
        var currentUserId = GetCurrentUserId();

        var participant = await _context.MeetingParticipants
            .FirstOrDefaultAsync(p => p.MeetingId == id && p.UserId == currentUserId);

        if (participant == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Meeting.NotInvited")));

        participant.StatusId = ParticipantStatuses.GetBySystemName(dto.Status)?.Id ?? ParticipantStatuses.Ids.Accepted;
        participant.ResponseNote = dto.ResponseNote;
        participant.RespondedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Meeting.ResponseSaved") });
    }

    // ===== DOSYA İŞLEMLERİ =====

    /// <summary>
    /// Dosya yükle
    /// </summary>
    [HttpPost("{id}/attachments")]
    public async Task<IActionResult> UploadAttachment(int id, IFormFile file, [FromForm] string? description)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Meeting.NotFound")));

        if (file == null || file.Length == 0)
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotSelected")));

        // Max 10MB
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileSizeExceeded10MB")));

        var currentUserId = GetCurrentUserId();
        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "meetings", id.ToString());
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new MeetingAttachment
        {
            MeetingId = id,
            FileName = file.FileName,
            FilePath = filePath,
            FileSize = file.Length,
            ContentType = file.ContentType,
            Description = description,
            UploadedByUserId = currentUserId
        };

        _context.MeetingAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        return Ok(new { id = attachment.Id, message = await _localizationService.GetResourceAsync("Api.Common.FileUploadSuccess") });
    }

    /// <summary>
    /// Dosya indir
    /// </summary>
    [HttpGet("{id}/attachments/{attachmentId}/download")]
    public async Task<IActionResult> DownloadAttachment(int id, int attachmentId)
    {
        var attachment = await _context.MeetingAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.MeetingId == id);

        if (attachment == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));

        if (!System.IO.File.Exists(attachment.FilePath))
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));

        var fileBytes = await System.IO.File.ReadAllBytesAsync(attachment.FilePath);
        return File(fileBytes, attachment.ContentType ?? "application/octet-stream", attachment.FileName);
    }

    /// <summary>
    /// Dosya sil
    /// </summary>
    [HttpDelete("{id}/attachments/{attachmentId}")]
    public async Task<IActionResult> DeleteAttachment(int id, int attachmentId)
    {
        var attachment = await _context.MeetingAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.MeetingId == id);

        if (attachment == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));

        attachment.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Common.FileDeleteSuccess") });
    }

    // ===== ÖZET VE İSTATİSTİK =====

    /// <summary>
    /// Toplantı özeti (Dashboard için)
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetMeetingSummary()
    {
        var currentUserId = GetCurrentUserId();
        var today = DateTime.UtcNow.Date;

        var meetings = await _context.Meetings
            .Where(m => m.OrganizerId == currentUserId || m.Participants.Any(p => p.UserId == currentUserId))
            .ToListAsync();

        var upcoming = await _context.Meetings
            .Include(m => m.Organizer)
            .Include(m => m.Participants)
            .Where(m => m.PlannedDate >= today && m.StatusId != MeetingStatuses.Ids.Cancelled && m.StatusId != MeetingStatuses.Ids.Completed)
            .Where(m => m.OrganizerId == currentUserId || m.Participants.Any(p => p.UserId == currentUserId))
            .OrderBy(m => m.PlannedDate)
            .Take(5)
            .ToListAsync();

        var summary = new MeetingSummaryDto
        {
            TotalMeetings = meetings.Count,
            UpcomingMeetings = meetings.Count(m => m.PlannedDate >= today && m.StatusId != MeetingStatuses.Ids.Cancelled && m.StatusId != MeetingStatuses.Ids.Completed),
            TodayMeetings = meetings.Count(m => m.PlannedDate.Date == today && m.StatusId != MeetingStatuses.Ids.Cancelled),
            CompletedMeetings = meetings.Count(m => m.StatusId == MeetingStatuses.Ids.Completed),
            CancelledMeetings = meetings.Count(m => m.StatusId == MeetingStatuses.Ids.Cancelled),
            UpcomingList = upcoming.Select(m => new MeetingListDto
            {
                Id = m.Id,
                Title = m.Title,
                MeetingType = m.MeetingTypeId.ToString(),
                Status = m.StatusId.ToString(),
                PlannedDate = m.PlannedDate,
                PlannedEndDate = m.PlannedEndDate,
                Location = m.Location,
                IsOnline = m.IsOnline,
                OrganizerName = m.Organizer != null ? $"{m.Organizer.FirstName} {m.Organizer.LastName}" : null,
                ParticipantCount = m.Participants.Count,
                AcceptedCount = m.Participants.Count(p => p.StatusId == ParticipantStatuses.Ids.Accepted)
            }).ToList()
        };

        return Ok(summary);
    }

    // ===== HELPER METHODS =====

    private static MeetingDto MapToDto(Meeting meeting)
    {
        return new MeetingDto
        {
            Id = meeting.Id,
            Title = meeting.Title,
            Description = meeting.Description,
            MeetingType = meeting.MeetingTypeId.ToString(),
            Status = meeting.StatusId.ToString(),
            PlannedDate = meeting.PlannedDate,
            PlannedEndDate = meeting.PlannedEndDate,
            ActualDate = meeting.ActualDate,
            ActualEndDate = meeting.ActualEndDate,
            DurationMinutes = meeting.DurationMinutes,
            Location = meeting.Location,
            IsOnline = meeting.IsOnline,
            OnlineLink = meeting.OnlineLink,
            OnlinePlatform = meeting.OnlinePlatform,
            Notes = meeting.Notes,
            Agenda = meeting.Agenda,
            Minutes = meeting.Minutes,
            Decisions = meeting.Decisions,
            OrganizerId = meeting.OrganizerId,
            OrganizerName = meeting.Organizer != null ? $"{meeting.Organizer.FirstName} {meeting.Organizer.LastName}" : null,
            ProjectId = meeting.ProjectId,
            ProjectName = meeting.Project?.Name,
            CustomerId = meeting.CustomerId,
            CustomerName = meeting.Customer?.CompanyName,
            ReminderHoursBefore = meeting.ReminderHoursBefore,
            CreatedAt = meeting.CreatedAt,
            ParticipantCount = meeting.Participants.Count,
            AcceptedCount = meeting.Participants.Count(p => p.StatusId == ParticipantStatuses.Ids.Accepted || p.StatusId == ParticipantStatuses.Ids.Attended),
            DeclinedCount = meeting.Participants.Count(p => p.StatusId == ParticipantStatuses.Ids.Declined),
            Participants = meeting.Participants.Select(p => new MeetingParticipantDto
            {
                Id = p.Id,
                MeetingId = p.MeetingId,
                UserId = p.UserId,
                UserName = p.User != null ? $"{p.User.FirstName} {p.User.LastName}" : null,
                UserEmail = p.User?.Email,
                ExternalName = p.ExternalName,
                ExternalEmail = p.ExternalEmail,
                Status = p.StatusId.ToString(),
                IsRequired = p.IsRequired,
                ResponseNote = p.ResponseNote,
                RespondedAt = p.RespondedAt,
                AttendanceNote = p.AttendanceNote
            }).ToList(),
            Attachments = meeting.Attachments.Select(a => new MeetingAttachmentDto
            {
                Id = a.Id,
                MeetingId = a.MeetingId,
                FileName = a.FileName,
                FileSize = a.FileSize,
                ContentType = a.ContentType,
                Description = a.Description,
                UploadedByName = a.UploadedByUser != null ? $"{a.UploadedByUser.FirstName} {a.UploadedByUser.LastName}" : null,
                CreatedAt = a.CreatedAt
            }).ToList()
        };
    }
}

public class CancelMeetingRequest
{
    public string? Reason { get; set; }
}

public class PostponeMeetingRequest
{
    public DateTime NewDate { get; set; }
    public DateTime? NewEndDate { get; set; }
    public string? Reason { get; set; }
}
