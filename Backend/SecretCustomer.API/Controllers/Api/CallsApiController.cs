using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Call;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/calls")]
[Authorize]
public class CallsApiController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CallsApiController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly ILocalizationService _localizationService;

    public CallsApiController(
        ApplicationDbContext context,
        ILogger<CallsApiController> logger,
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

    private string GenerateReferenceNumber()
    {
        return $"CALL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    /// <summary>
    /// Çağrı listesi
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCalls([FromQuery] CallFilterDto filter)
    {
        var query = _context.Calls
            .Include(c => c.Project)
            .Include(c => c.Customer)
            .Include(c => c.AssignedUser)
            .AsQueryable();

        // Filters
        if (filter.ProjectId.HasValue)
            query = query.Where(c => c.ProjectId == filter.ProjectId.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(c => c.CustomerId == filter.CustomerId.Value);

        if (filter.AssignedUserId.HasValue)
            query = query.Where(c => c.AssignedUserId == filter.AssignedUserId.Value);

        if (!string.IsNullOrEmpty(filter.CallType) && Enum.TryParse<CallType>(filter.CallType, out var callType))
            query = query.Where(c => c.CallType == callType);

        if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<CallStatus>(filter.Status, out var status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrEmpty(filter.Priority) && Enum.TryParse<CallPriority>(filter.Priority, out var priority))
            query = query.Where(c => c.Priority == priority);

        if (filter.StartDate.HasValue)
            query = query.Where(c => c.StartTime >= filter.StartDate.Value || c.ScheduledDate >= filter.StartDate.Value || c.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(c => c.StartTime <= filter.EndDate.Value || c.ScheduledDate <= filter.EndDate.Value || c.CreatedAt <= filter.EndDate.Value);

        if (filter.RequiresCallback.HasValue)
            query = query.Where(c => c.RequiresCallback == filter.RequiresCallback.Value && !c.CallbackCompleted);

        if (filter.HasRecording.HasValue)
            query = query.Where(c => filter.HasRecording.Value ? c.RecordingPath != null : c.RecordingPath == null);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(c =>
                c.ReferenceNumber.ToLower().Contains(term) ||
                (c.Subject != null && c.Subject.ToLower().Contains(term)) ||
                (c.CallerPhone != null && c.CallerPhone.Contains(term)) ||
                (c.CallerName != null && c.CallerName.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var calls = await query
            .OrderByDescending(c => c.StartTime ?? c.ScheduledDate ?? c.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new CallListDto
            {
                Id = c.Id,
                ReferenceNumber = c.ReferenceNumber,
                CallType = c.CallType.ToString(),
                Status = c.Status.ToString(),
                Priority = c.Priority.ToString(),
                Subject = c.Subject,
                CallerPhone = c.CallerPhone,
                CallerName = c.CallerName,
                StartTime = c.StartTime,
                DurationSeconds = c.DurationSeconds,
                ProjectName = c.Project != null ? c.Project.Name : null,
                AssignedUserName = c.AssignedUser != null ? c.AssignedUser.FirstName + " " + c.AssignedUser.LastName : null,
                RequiresCallback = c.RequiresCallback,
                CallbackCompleted = c.CallbackCompleted,
                HasRecording = c.RecordingPath != null
            })
            .ToListAsync();

        return Ok(new
        {
            items = calls,
            totalCount,
            page = filter.Page,
            pageSize = filter.PageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
        });
    }

    /// <summary>
    /// Çağrı detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCall(int id)
    {
        var call = await _context.Calls
            .Include(c => c.Project)
            .Include(c => c.Customer)
            .Include(c => c.AssignedUser)
            .Include(c => c.CreatedByUser)
            .Include(c => c.Attachments)
                .ThenInclude(a => a.UploadedByUser)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (call == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Call.NotFound")));

        var dto = MapToDto(call);
        return Ok(dto);
    }

    /// <summary>
    /// Çağrı oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCall([FromBody] CreateCallDto dto)
    {
        var currentUserId = GetCurrentUserId();

        var call = new Call
        {
            ReferenceNumber = GenerateReferenceNumber(),
            CallType = Enum.TryParse<CallType>(dto.CallType, out var ct) ? ct : CallType.Inbound,
            Status = CallStatus.Pending,
            Priority = Enum.TryParse<CallPriority>(dto.Priority, out var cp) ? cp : CallPriority.Normal,
            Subject = dto.Subject,
            Description = dto.Description,
            CallerPhone = dto.CallerPhone,
            CallerName = dto.CallerName,
            CalledPhone = dto.CalledPhone,
            ScheduledDate = dto.ScheduledDate,
            ProjectId = dto.ProjectId,
            CustomerId = dto.CustomerId,
            AssignedUserId = dto.AssignedUserId ?? currentUserId,
            CreatedByUserId = currentUserId,
            Tags = dto.Tags
        };

        _context.Calls.Add(call);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Call created: {CallId} by user {UserId}", call.Id, currentUserId);

        return CreatedAtAction(nameof(GetCall), new { id = call.Id }, new { id = call.Id, referenceNumber = call.ReferenceNumber, message = await _localizationService.GetResourceAsync("Api.Call.CreateSuccess") });
    }

    /// <summary>
    /// Çağrı güncelle
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCall(int id, [FromBody] UpdateCallDto dto)
    {
        var call = await _context.Calls.FindAsync(id);
        if (call == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Call.NotFound")));

        call.CallType = Enum.TryParse<CallType>(dto.CallType, out var ct) ? ct : CallType.Inbound;
        call.Status = Enum.TryParse<CallStatus>(dto.Status, out var st) ? st : CallStatus.Pending;
        call.Priority = Enum.TryParse<CallPriority>(dto.Priority, out var cp) ? cp : CallPriority.Normal;
        call.Subject = dto.Subject;
        call.Description = dto.Description;
        call.CallerPhone = dto.CallerPhone;
        call.CallerName = dto.CallerName;
        call.CalledPhone = dto.CalledPhone;
        call.ScheduledDate = dto.ScheduledDate;
        call.Notes = dto.Notes;
        call.Outcome = dto.Outcome;
        call.ProjectId = dto.ProjectId;
        call.CustomerId = dto.CustomerId;
        call.AssignedUserId = dto.AssignedUserId;
        call.Tags = dto.Tags;
        call.RequiresCallback = dto.RequiresCallback;
        call.CallbackDate = dto.CallbackDate;
        call.CallbackNote = dto.CallbackNote;

        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Call.UpdateSuccess") });
    }

    /// <summary>
    /// Çağrıyı sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCall(int id)
    {
        var call = await _context.Calls.FindAsync(id);
        if (call == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Call.NotFound")));

        call.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Call.DeleteSuccess") });
    }

    /// <summary>
    /// Çağrıyı başlat
    /// </summary>
    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartCall(int id, [FromBody] StartCallDto? dto)
    {
        var call = await _context.Calls.FindAsync(id);
        if (call == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Call.NotFound")));

        call.Status = CallStatus.InProgress;
        call.StartTime = dto?.StartTime ?? DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Call.StartSuccess") });
    }

    /// <summary>
    /// Çağrıyı tamamla
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteCall(int id, [FromBody] CompleteCallDto dto)
    {
        var call = await _context.Calls.FindAsync(id);
        if (call == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Call.NotFound")));

        call.Status = CallStatus.Completed;
        call.EndTime = dto.EndTime ?? DateTime.UtcNow;

        if (dto.DurationSeconds.HasValue)
        {
            call.DurationSeconds = dto.DurationSeconds;
        }
        else if (call.StartTime.HasValue)
        {
            call.DurationSeconds = (int)(call.EndTime.Value - call.StartTime.Value).TotalSeconds;
        }

        call.Notes = dto.Notes ?? call.Notes;
        call.Outcome = dto.Outcome ?? call.Outcome;
        call.RequiresCallback = dto.RequiresCallback;
        call.CallbackDate = dto.CallbackDate;
        call.CallbackNote = dto.CallbackNote;

        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Call.CompleteSuccess") });
    }

    /// <summary>
    /// Çağrıyı cevapsız olarak işaretle
    /// </summary>
    [HttpPost("{id}/missed")]
    public async Task<IActionResult> MarkAsMissed(int id)
    {
        var call = await _context.Calls.FindAsync(id);
        if (call == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Call.NotFound")));

        call.Status = CallStatus.Missed;
        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Call.MissedSuccess") });
    }

    /// <summary>
    /// Geri aramayı tamamla
    /// </summary>
    [HttpPost("{id}/callback-complete")]
    public async Task<IActionResult> CompleteCallback(int id)
    {
        var call = await _context.Calls.FindAsync(id);
        if (call == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Call.NotFound")));

        call.CallbackCompleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Call.CallbackCompleteSuccess") });
    }

    // ===== KAYIT DOSYASI İŞLEMLERİ =====

    /// <summary>
    /// Çağrı kaydı yükle
    /// </summary>
    [HttpPost("{id}/recording")]
    public async Task<IActionResult> UploadRecording(int id, IFormFile file)
    {
        var call = await _context.Calls.FindAsync(id);
        if (call == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Call.NotFound")));

        if (file == null || file.Length == 0)
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotSelected")));

        // Max 100MB for recordings
        if (file.Length > 100 * 1024 * 1024)
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileSizeExceeded100MB")));

        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "calls", "recordings", id.ToString());
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        call.RecordingPath = filePath;
        call.RecordingFileName = file.FileName;
        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Call.RecordingUploadSuccess") });
    }

    /// <summary>
    /// Çağrı kaydını indir
    /// </summary>
    [HttpGet("{id}/recording")]
    public async Task<IActionResult> DownloadRecording(int id)
    {
        var call = await _context.Calls.FindAsync(id);
        if (call == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Call.NotFound")));

        if (string.IsNullOrEmpty(call.RecordingPath) || !System.IO.File.Exists(call.RecordingPath))
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Call.RecordingNotFound")));

        var fileBytes = await System.IO.File.ReadAllBytesAsync(call.RecordingPath);
        var contentType = "audio/mpeg";
        if (call.RecordingFileName?.EndsWith(".wav") == true) contentType = "audio/wav";
        if (call.RecordingFileName?.EndsWith(".mp3") == true) contentType = "audio/mpeg";
        if (call.RecordingFileName?.EndsWith(".ogg") == true) contentType = "audio/ogg";

        return File(fileBytes, contentType, call.RecordingFileName ?? "recording.mp3");
    }

    // ===== DOSYA EKİ İŞLEMLERİ =====

    /// <summary>
    /// Dosya yükle
    /// </summary>
    [HttpPost("{id}/attachments")]
    public async Task<IActionResult> UploadAttachment(int id, IFormFile file, [FromForm] string? description)
    {
        var call = await _context.Calls.FindAsync(id);
        if (call == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Call.NotFound")));

        if (file == null || file.Length == 0)
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotSelected")));

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileSizeExceeded10MB")));

        var currentUserId = GetCurrentUserId();
        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "calls", id.ToString());
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new CallAttachment
        {
            CallId = id,
            FileName = file.FileName,
            FilePath = filePath,
            FileSize = file.Length,
            ContentType = file.ContentType,
            Description = description,
            UploadedByUserId = currentUserId
        };

        _context.CallAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        return Ok(new { id = attachment.Id, message = await _localizationService.GetResourceAsync("Api.Common.FileUploadSuccess") });
    }

    /// <summary>
    /// Dosya indir
    /// </summary>
    [HttpGet("{id}/attachments/{attachmentId}/download")]
    public async Task<IActionResult> DownloadAttachment(int id, int attachmentId)
    {
        var attachment = await _context.CallAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.CallId == id);

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
        var attachment = await _context.CallAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.CallId == id);

        if (attachment == null)
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));

        attachment.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = await _localizationService.GetResourceAsync("Api.Common.FileDeleteSuccess") });
    }

    // ===== ÖZET VE İSTATİSTİK =====

    /// <summary>
    /// Çağrı özeti (Dashboard için)
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetCallSummary()
    {
        var today = DateTime.UtcNow.Date;
        var calls = await _context.Calls.ToListAsync();

        var recent = await _context.Calls
            .Include(c => c.Project)
            .Include(c => c.AssignedUser)
            .OrderByDescending(c => c.StartTime ?? c.CreatedAt)
            .Take(10)
            .ToListAsync();

        var completedCalls = calls.Where(c => c.Status == CallStatus.Completed && c.DurationSeconds.HasValue).ToList();

        var summary = new CallSummaryDto
        {
            TotalCalls = calls.Count,
            TodayCalls = calls.Count(c => (c.StartTime ?? c.CreatedAt).Date == today),
            PendingCalls = calls.Count(c => c.Status == CallStatus.Pending || c.Status == CallStatus.Scheduled),
            CompletedCalls = calls.Count(c => c.Status == CallStatus.Completed),
            MissedCalls = calls.Count(c => c.Status == CallStatus.Missed),
            CallbackRequired = calls.Count(c => c.RequiresCallback && !c.CallbackCompleted),
            AverageDurationSeconds = completedCalls.Any() ? (int)completedCalls.Average(c => c.DurationSeconds!.Value) : 0,
            AverageWaitTimeSeconds = completedCalls.Where(c => c.WaitTimeSeconds.HasValue).Any()
                ? (int)completedCalls.Where(c => c.WaitTimeSeconds.HasValue).Average(c => c.WaitTimeSeconds!.Value) : 0,
            RecentCalls = recent.Select(c => new CallListDto
            {
                Id = c.Id,
                ReferenceNumber = c.ReferenceNumber,
                CallType = c.CallType.ToString(),
                Status = c.Status.ToString(),
                Priority = c.Priority.ToString(),
                Subject = c.Subject,
                CallerPhone = c.CallerPhone,
                CallerName = c.CallerName,
                StartTime = c.StartTime,
                DurationSeconds = c.DurationSeconds,
                ProjectName = c.Project?.Name,
                AssignedUserName = c.AssignedUser != null ? $"{c.AssignedUser.FirstName} {c.AssignedUser.LastName}" : null,
                RequiresCallback = c.RequiresCallback,
                CallbackCompleted = c.CallbackCompleted,
                HasRecording = c.RecordingPath != null
            }).ToList()
        };

        return Ok(summary);
    }

    // ===== HELPER METHODS =====

    private static CallDto MapToDto(Call call)
    {
        return new CallDto
        {
            Id = call.Id,
            ReferenceNumber = call.ReferenceNumber,
            CallType = call.CallType.ToString(),
            Status = call.Status.ToString(),
            Priority = call.Priority.ToString(),
            Subject = call.Subject,
            Description = call.Description,
            CallerPhone = call.CallerPhone,
            CallerName = call.CallerName,
            CalledPhone = call.CalledPhone,
            ScheduledDate = call.ScheduledDate,
            StartTime = call.StartTime,
            EndTime = call.EndTime,
            DurationSeconds = call.DurationSeconds,
            WaitTimeSeconds = call.WaitTimeSeconds,
            Notes = call.Notes,
            Outcome = call.Outcome,
            RecordingFileName = call.RecordingFileName,
            HasRecording = !string.IsNullOrEmpty(call.RecordingPath),
            ExternalCallId = call.ExternalCallId,
            ProjectId = call.ProjectId,
            ProjectName = call.Project?.Name,
            CustomerId = call.CustomerId,
            CustomerName = call.Customer?.CompanyName,
            AssignedUserId = call.AssignedUserId,
            AssignedUserName = call.AssignedUser != null ? $"{call.AssignedUser.FirstName} {call.AssignedUser.LastName}" : null,
            EvaluationId = call.EvaluationId,
            Tags = call.Tags,
            RequiresCallback = call.RequiresCallback,
            CallbackDate = call.CallbackDate,
            CallbackNote = call.CallbackNote,
            CallbackCompleted = call.CallbackCompleted,
            CreatedAt = call.CreatedAt,
            Attachments = call.Attachments.Select(a => new CallAttachmentDto
            {
                Id = a.Id,
                CallId = a.CallId,
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
