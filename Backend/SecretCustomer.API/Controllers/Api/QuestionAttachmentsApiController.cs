using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Question;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/question-attachments")]
[Authorize]
public class QuestionAttachmentsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<QuestionAttachmentsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public QuestionAttachmentsApiController(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        ILogger<QuestionAttachmentsApiController> logger,
        ILocalizationService localizationService)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Soruya ait dosyaları listele
    /// </summary>
    [HttpGet("question/{questionId}")]
    public async Task<IActionResult> GetByQuestion(Guid questionId)
    {
        try
        {
            var attachments = await _context.QuestionAttachments
                .Where(a => a.QuestionId == questionId)
                .OrderBy(a => a.Order)
                .Select(a => new QuestionAttachmentDto
                {
                    Id = a.Id,
                    QuestionId = a.QuestionId,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileSize = a.FileSize,
                    ContentType = a.ContentType,
                    Description = a.Description,
                    Order = a.Order,
                    CreatedAt = a.CreatedAt,
                    UploadedByUserName = a.UploadedByUser != null
                        ? a.UploadedByUser.FirstName + " " + a.UploadedByUser.LastName
                        : null
                })
                .ToListAsync();

            return Ok(attachments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachments for question {QuestionId}", questionId);
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Common.FilesLoadError") });
        }
    }

    /// <summary>
    /// Dosya yükle
    /// </summary>
    [HttpPost("question/{questionId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Upload(Guid questionId, IFormFile file, [FromForm] string? description)
    {
        try
        {
            // Soru kontrolü
            var question = await _context.Questions.FindAsync(questionId);
            if (question == null)
            {
                return NotFound(new { message = await _localizationService.GetResourceAsync("Api.Question.NotFound") });
            }

            // Dosya kontrolü
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.Common.FileNotSelected") });
            }

            // Boyut kontrolü
            if (file.Length > MaxFileSize)
            {
                return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.Common.FileSizeExceeded10MB") });
            }

            // Uzantı kontrolü
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.Common.FileTypeNotSupported") });
            }

            // Dosya kaydetme dizini
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "questions", questionId.ToString());
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Unique dosya adı
            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            // Dosyayı kaydet
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Kullanıcı ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? userId = null;
            if (Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            // Sıra numarası
            var maxOrder = await _context.QuestionAttachments
                .Where(a => a.QuestionId == questionId)
                .MaxAsync(a => (int?)a.Order) ?? 0;

            // Veritabanına kaydet
            var attachment = new QuestionAttachment
            {
                QuestionId = questionId,
                FileName = file.FileName,
                StoredFileName = storedFileName,
                FilePath = $"/uploads/questions/{questionId}/{storedFileName}",
                FileSize = file.Length,
                ContentType = file.ContentType,
                Description = description,
                Order = maxOrder + 1,
                UploadedByUserId = userId
            };

            _context.QuestionAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File {FileName} uploaded for question {QuestionId}", file.FileName, questionId);

            return Ok(new UploadAttachmentResultDto
            {
                Success = true,
                Message = await _localizationService.GetResourceAsync("Api.Common.FileUploadSuccess"),
                Attachment = new QuestionAttachmentDto
                {
                    Id = attachment.Id,
                    QuestionId = attachment.QuestionId,
                    FileName = attachment.FileName,
                    FilePath = attachment.FilePath,
                    FileSize = attachment.FileSize,
                    ContentType = attachment.ContentType,
                    Description = attachment.Description,
                    Order = attachment.Order,
                    CreatedAt = attachment.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for question {QuestionId}", questionId);
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Common.FileUploadError") });
        }
    }

    /// <summary>
    /// Dosya indir
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        try
        {
            var attachment = await _context.QuestionAttachments.FindAsync(id);
            if (attachment == null)
            {
                return NotFound(new { message = await _localizationService.GetResourceAsync("Api.Common.FileNotFound") });
            }

            var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = await _localizationService.GetResourceAsync("Api.Common.FileNotFoundOnServer") });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, attachment.ContentType, attachment.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading attachment {AttachmentId}", id);
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Common.FileDownloadError") });
        }
    }

    /// <summary>
    /// Dosya sil
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var attachment = await _context.QuestionAttachments.FindAsync(id);
            if (attachment == null)
            {
                return NotFound(new { message = await _localizationService.GetResourceAsync("Api.Common.FileNotFound") });
            }

            // Fiziksel dosyayı sil
            var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Veritabanından sil (soft delete)
            attachment.IsDeleted = true;
            attachment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Attachment {AttachmentId} deleted", id);

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Common.FileDeleteSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {AttachmentId}", id);
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Common.FileDeleteError") });
        }
    }

    /// <summary>
    /// Dosya sırasını güncelle
    /// </summary>
    [HttpPut("{id}/order")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] int newOrder)
    {
        try
        {
            var attachment = await _context.QuestionAttachments.FindAsync(id);
            if (attachment == null)
            {
                return NotFound(new { message = await _localizationService.GetResourceAsync("Api.Common.FileNotFound") });
            }

            attachment.Order = newOrder;
            attachment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.QuestionAttachment.OrderUpdateSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attachment order {AttachmentId}", id);
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.QuestionAttachment.OrderUpdateError") });
        }
    }
}
