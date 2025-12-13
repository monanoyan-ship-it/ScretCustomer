using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.CustomerVisit;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Data;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/customer-visits")]
[Authorize]
public class CustomerVisitsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CustomerVisitsApiController> _logger;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".mp4", ".mp3" };
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

    public CustomerVisitsApiController(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        ILogger<CustomerVisitsApiController> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Ziyaretleri listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CustomerVisitFilterDto filter)
    {
        try
        {
            var query = _context.CustomerVisits
                .Include(v => v.Customer)
                .Include(v => v.Branch)
                .Include(v => v.VisitorUser)
                .Include(v => v.Project)
                .Include(v => v.Attachments.Where(a => !a.IsDeleted))
                .AsQueryable();

            // Apply filters
            if (filter.CustomerId.HasValue)
                query = query.Where(v => v.CustomerId == filter.CustomerId);

            if (filter.BranchId.HasValue)
                query = query.Where(v => v.BranchId == filter.BranchId);

            if (filter.VisitorUserId.HasValue)
                query = query.Where(v => v.VisitorUserId == filter.VisitorUserId);

            if (filter.VisitType.HasValue)
                query = query.Where(v => v.VisitType == filter.VisitType);

            if (filter.Status.HasValue)
                query = query.Where(v => v.Status == filter.Status);

            if (filter.FromDate.HasValue)
                query = query.Where(v => v.PlannedDate >= filter.FromDate);

            if (filter.ToDate.HasValue)
                query = query.Where(v => v.PlannedDate <= filter.ToDate);

            if (filter.RequiresFollowUp.HasValue)
                query = query.Where(v => v.RequiresFollowUp == filter.RequiresFollowUp);

            var visits = await query
                .OrderByDescending(v => v.PlannedDate)
                .Select(v => new CustomerVisitSummaryDto
                {
                    Id = v.Id,
                    CustomerName = v.Customer != null ? v.Customer.CompanyName : null,
                    BranchName = v.Branch != null ? v.Branch.Name : null,
                    VisitorUserName = v.VisitorUser.FirstName + " " + v.VisitorUser.LastName,
                    VisitType = v.VisitType,
                    Status = v.Status,
                    PlannedDate = v.PlannedDate,
                    ActualDate = v.ActualDate,
                    RequiresFollowUp = v.RequiresFollowUp,
                    AttachmentCount = v.Attachments.Count
                })
                .ToListAsync();

            return Ok(visits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer visits");
            return StatusCode(500, new { message = "Ziyaretler yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Tek ziyaret detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var visit = await _context.CustomerVisits
                .Include(v => v.Customer)
                .Include(v => v.Branch)
                .Include(v => v.VisitorUser)
                .Include(v => v.Project)
                .Include(v => v.Attachments.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync(v => v.Id == id);

            if (visit == null)
                return NotFound(new { message = "Ziyaret bulunamadı." });

            var dto = new CustomerVisitDto
            {
                Id = visit.Id,
                CustomerId = visit.CustomerId,
                CustomerName = visit.Customer?.CompanyName,
                BranchId = visit.BranchId,
                BranchName = visit.Branch?.Name,
                VisitorUserId = visit.VisitorUserId,
                VisitorUserName = $"{visit.VisitorUser.FirstName} {visit.VisitorUser.LastName}",
                VisitType = visit.VisitType,
                Status = visit.Status,
                PlannedDate = visit.PlannedDate,
                PlannedTime = visit.PlannedTime,
                ActualDate = visit.ActualDate,
                StartTime = visit.StartTime,
                EndTime = visit.EndTime,
                DurationMinutes = visit.DurationMinutes,
                Purpose = visit.Purpose,
                Notes = visit.Notes,
                Observations = visit.Observations,
                ContactPersonName = visit.ContactPersonName,
                ContactPersonTitle = visit.ContactPersonTitle,
                ContactPersonPhone = visit.ContactPersonPhone,
                Address = visit.Address,
                Latitude = visit.Latitude,
                Longitude = visit.Longitude,
                RequiresFollowUp = visit.RequiresFollowUp,
                FollowUpNotes = visit.FollowUpNotes,
                FollowUpDate = visit.FollowUpDate,
                ProjectId = visit.ProjectId,
                ProjectName = visit.Project?.Name,
                EvaluationId = visit.EvaluationId,
                CreatedAt = visit.CreatedAt,
                Attachments = visit.Attachments.Select(a => new CustomerVisitAttachmentDto
                {
                    Id = a.Id,
                    CustomerVisitId = a.CustomerVisitId,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileSize = a.FileSize,
                    ContentType = a.ContentType,
                    Description = a.Description,
                    Order = a.Order,
                    AttachmentType = a.AttachmentType,
                    CreatedAt = a.CreatedAt
                }).ToList()
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer visit {Id}", id);
            return StatusCode(500, new { message = "Ziyaret yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Yeni ziyaret oluştur
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,TeamLeader,CustomerRepresentative")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerVisitDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Kullanıcı bilgisi bulunamadı." });

            var visit = new CustomerVisit
            {
                CustomerId = dto.CustomerId,
                BranchId = dto.BranchId,
                VisitorUserId = userId,
                VisitType = dto.VisitType,
                Status = VisitStatus.Planned,
                PlannedDate = dto.PlannedDate,
                PlannedTime = dto.PlannedTime,
                Purpose = dto.Purpose,
                ContactPersonName = dto.ContactPersonName,
                ContactPersonTitle = dto.ContactPersonTitle,
                ContactPersonPhone = dto.ContactPersonPhone,
                Address = dto.Address,
                ProjectId = dto.ProjectId
            };

            _context.CustomerVisits.Add(visit);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer visit {Id} created", visit.Id);

            return Ok(new { id = visit.Id, message = "Ziyaret başarıyla oluşturuldu." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer visit");
            return StatusCode(500, new { message = "Ziyaret oluşturulurken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Ziyareti güncelle
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,TeamLeader,CustomerRepresentative")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateCustomerVisitDto dto)
    {
        try
        {
            var visit = await _context.CustomerVisits.FindAsync(id);
            if (visit == null)
                return NotFound(new { message = "Ziyaret bulunamadı." });

            visit.CustomerId = dto.CustomerId;
            visit.BranchId = dto.BranchId;
            visit.VisitType = dto.VisitType;
            visit.PlannedDate = dto.PlannedDate;
            visit.PlannedTime = dto.PlannedTime;
            visit.Purpose = dto.Purpose;
            visit.ContactPersonName = dto.ContactPersonName;
            visit.ContactPersonTitle = dto.ContactPersonTitle;
            visit.ContactPersonPhone = dto.ContactPersonPhone;
            visit.Address = dto.Address;
            visit.ProjectId = dto.ProjectId;
            visit.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer visit {Id} updated", id);

            return Ok(new { message = "Ziyaret güncellendi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer visit {Id}", id);
            return StatusCode(500, new { message = "Ziyaret güncellenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Ziyareti başlat
    /// </summary>
    [HttpPost("{id}/start")]
    public async Task<IActionResult> Start(Guid id)
    {
        try
        {
            var visit = await _context.CustomerVisits.FindAsync(id);
            if (visit == null)
                return NotFound(new { message = "Ziyaret bulunamadı." });

            visit.Status = VisitStatus.InProgress;
            visit.StartTime = DateTime.UtcNow;
            visit.ActualDate = DateTime.UtcNow;
            visit.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer visit {Id} started", id);

            return Ok(new { message = "Ziyaret başlatıldı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting customer visit {Id}", id);
            return StatusCode(500, new { message = "Ziyaret başlatılırken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Ziyareti tamamla
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteCustomerVisitDto dto)
    {
        try
        {
            var visit = await _context.CustomerVisits.FindAsync(id);
            if (visit == null)
                return NotFound(new { message = "Ziyaret bulunamadı." });

            visit.Status = VisitStatus.Completed;
            visit.ActualDate = dto.ActualDate ?? DateTime.UtcNow;
            visit.StartTime = dto.StartTime ?? visit.StartTime;
            visit.EndTime = dto.EndTime ?? DateTime.UtcNow;
            visit.DurationMinutes = dto.DurationMinutes;
            visit.Notes = dto.Notes;
            visit.Observations = dto.Observations;
            visit.ContactPersonName = dto.ContactPersonName ?? visit.ContactPersonName;
            visit.ContactPersonTitle = dto.ContactPersonTitle ?? visit.ContactPersonTitle;
            visit.Latitude = dto.Latitude;
            visit.Longitude = dto.Longitude;
            visit.RequiresFollowUp = dto.RequiresFollowUp;
            visit.FollowUpNotes = dto.FollowUpNotes;
            visit.FollowUpDate = dto.FollowUpDate;
            visit.UpdatedAt = DateTime.UtcNow;

            // Calculate duration if not provided
            if (!visit.DurationMinutes.HasValue && visit.StartTime.HasValue && visit.EndTime.HasValue)
            {
                visit.DurationMinutes = (int)(visit.EndTime.Value - visit.StartTime.Value).TotalMinutes;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer visit {Id} completed", id);

            return Ok(new { message = "Ziyaret tamamlandı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing customer visit {Id}", id);
            return StatusCode(500, new { message = "Ziyaret tamamlanırken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Ziyareti iptal et
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string? reason)
    {
        try
        {
            var visit = await _context.CustomerVisits.FindAsync(id);
            if (visit == null)
                return NotFound(new { message = "Ziyaret bulunamadı." });

            visit.Status = VisitStatus.Cancelled;
            visit.Notes = string.IsNullOrEmpty(reason)
                ? visit.Notes
                : $"{visit.Notes}\n[İptal Sebebi: {reason}]";
            visit.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer visit {Id} cancelled", id);

            return Ok(new { message = "Ziyaret iptal edildi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling customer visit {Id}", id);
            return StatusCode(500, new { message = "Ziyaret iptal edilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Ziyareti sil (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var visit = await _context.CustomerVisits.FindAsync(id);
            if (visit == null)
                return NotFound(new { message = "Ziyaret bulunamadı." });

            visit.IsDeleted = true;
            visit.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer visit {Id} deleted", id);

            return Ok(new { message = "Ziyaret silindi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer visit {Id}", id);
            return StatusCode(500, new { message = "Ziyaret silinirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Ziyarete dosya ekle
    /// </summary>
    [HttpPost("{visitId}/attachments")]
    public async Task<IActionResult> UploadAttachment(Guid visitId, IFormFile file, [FromForm] string? description, [FromForm] AttachmentType? attachmentType)
    {
        try
        {
            var visit = await _context.CustomerVisits.FindAsync(visitId);
            if (visit == null)
                return NotFound(new { message = "Ziyaret bulunamadı." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            if (file.Length > MaxFileSize)
                return BadRequest(new { message = "Dosya boyutu 50MB'dan büyük olamaz." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return BadRequest(new { message = $"Bu dosya türü desteklenmiyor." });

            // Determine attachment type from extension if not provided
            var type = attachmentType ?? (extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" => AttachmentType.Photo,
                ".mp4" => AttachmentType.Video,
                ".mp3" => AttachmentType.Audio,
                _ => AttachmentType.Document
            });

            // Save file
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "visits", visitId.ToString());
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var maxOrder = await _context.CustomerVisitAttachments
                .Where(a => a.CustomerVisitId == visitId)
                .MaxAsync(a => (int?)a.Order) ?? 0;

            var attachment = new CustomerVisitAttachment
            {
                CustomerVisitId = visitId,
                FileName = file.FileName,
                StoredFileName = storedFileName,
                FilePath = $"/uploads/visits/{visitId}/{storedFileName}",
                FileSize = file.Length,
                ContentType = file.ContentType,
                Description = description,
                Order = maxOrder + 1,
                AttachmentType = type
            };

            _context.CustomerVisitAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Attachment {Id} uploaded for visit {VisitId}", attachment.Id, visitId);

            return Ok(new CustomerVisitAttachmentDto
            {
                Id = attachment.Id,
                CustomerVisitId = attachment.CustomerVisitId,
                FileName = attachment.FileName,
                FilePath = attachment.FilePath,
                FileSize = attachment.FileSize,
                ContentType = attachment.ContentType,
                Description = attachment.Description,
                Order = attachment.Order,
                AttachmentType = attachment.AttachmentType,
                CreatedAt = attachment.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading attachment for visit {VisitId}", visitId);
            return StatusCode(500, new { message = "Dosya yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Dosya indir
    /// </summary>
    [HttpGet("attachments/{id}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid id)
    {
        try
        {
            var attachment = await _context.CustomerVisitAttachments.FindAsync(id);
            if (attachment == null)
                return NotFound(new { message = "Dosya bulunamadı." });

            var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "Dosya sunucuda bulunamadı." });

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, attachment.ContentType, attachment.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading attachment {Id}", id);
            return StatusCode(500, new { message = "Dosya indirilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Dosya sil
    /// </summary>
    [HttpDelete("attachments/{id}")]
    public async Task<IActionResult> DeleteAttachment(Guid id)
    {
        try
        {
            var attachment = await _context.CustomerVisitAttachments.FindAsync(id);
            if (attachment == null)
                return NotFound(new { message = "Dosya bulunamadı." });

            // Delete physical file
            var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            // Soft delete
            attachment.IsDeleted = true;
            attachment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Attachment {Id} deleted", id);

            return Ok(new { message = "Dosya silindi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {Id}", id);
            return StatusCode(500, new { message = "Dosya silinirken bir hata oluştu." });
        }
    }
}
