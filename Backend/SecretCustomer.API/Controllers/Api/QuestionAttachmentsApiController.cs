using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Question;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/question-attachments")]
[Authorize]
public class QuestionAttachmentsApiController : BaseApiController
{
    private readonly IQuestionAttachmentService _questionAttachmentService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<QuestionAttachmentsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public QuestionAttachmentsApiController(
        IQuestionAttachmentService questionAttachmentService,
        IWebHostEnvironment environment,
        ILogger<QuestionAttachmentsApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _questionAttachmentService = questionAttachmentService;
        _environment = environment;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Soruya ait dosyaları listele
    /// </summary>
    [HttpGet("question/{questionId}")]
    public async Task<IActionResult> GetByQuestion(int questionId)
    {
        try
        {
            var attachments = await _questionAttachmentService.GetByQuestionAsync(questionId);
            return Ok(attachments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachments for question {QuestionId}", questionId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FilesLoadError"), ex));
        }
    }

    /// <summary>
    /// Dosya yükle
    /// </summary>
    [HttpPost("question/{questionId}")]
    [Authorize(Roles = "Admin,QualitySpecialist")]
    public async Task<IActionResult> Upload(int questionId, IFormFile file, [FromForm] string? description)
    {
        try
        {
            // Soru kontrolü
            var questionExists = await _questionAttachmentService.QuestionExistsAsync(questionId);
            if (!questionExists)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Question.NotFound")));
            }

            // Dosya kontrolü
            if (file == null || file.Length == 0)
            {
                return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotSelected")));
            }

            // Boyut kontrolü
            if (file.Length > MaxFileSize)
            {
                return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileSizeExceeded10MB")));
            }

            // Uzantı kontrolü
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileTypeNotSupported")));
            }

            // Dosya kaydetme dizini
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "questions", questionId.ToString());
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Unique dosya adı
            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var physicalFilePath = Path.Combine(uploadsFolder, storedFileName);

            // Dosyayı kaydet
            using (var stream = new FileStream(physicalFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Kullanıcı ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (int.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            // DB'ye kaydet (service üzerinden)
            var dbFilePath = $"/uploads/questions/{questionId}/{storedFileName}";
            var attachmentDto = await _questionAttachmentService.UploadAsync(
                questionId, file.FileName, storedFileName, dbFilePath, file.Length, file.ContentType, description, userId);

            _logger.LogInformation("File {FileName} uploaded for question {QuestionId}", file.FileName, questionId);

            return Ok(new UploadAttachmentResultDto
            {
                Success = true,
                Message = await _localizationService.GetResourceAsync("Api.Common.FileUploadSuccess"),
                Attachment = attachmentDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for question {QuestionId}", questionId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Answer.UploadError"), ex));
        }
    }

    /// <summary>
    /// Dosya indir
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        try
        {
            var downloadInfo = await _questionAttachmentService.GetDownloadInfoAsync(id);
            if (downloadInfo == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            var (dbFilePath, contentType, fileName) = downloadInfo.Value;

            var physicalFilePath = Path.Combine(_environment.WebRootPath, dbFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(physicalFilePath))
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFoundOnServer")));
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalFilePath);
            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading attachment {AttachmentId}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Answer.DownloadError"), ex));
        }
    }

    /// <summary>
    /// Dosya sil
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,QualitySpecialist")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var (success, dbFilePath) = await _questionAttachmentService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            // Fiziksel dosyayı sil
            if (dbFilePath != null)
            {
                var physicalFilePath = Path.Combine(_environment.WebRootPath, dbFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(physicalFilePath))
                {
                    System.IO.File.Delete(physicalFilePath);
                }
            }

            _logger.LogInformation("Attachment {AttachmentId} deleted", id);

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Common.FileDeleteSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {AttachmentId}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Answer.DeleteError"), ex));
        }
    }

    /// <summary>
    /// Dosya sırasını güncelle
    /// </summary>
    [HttpPut("{id}/order")]
    [Authorize(Roles = "Admin,QualitySpecialist")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] int newOrder)
    {
        try
        {
            var success = await _questionAttachmentService.UpdateOrderAsync(id, newOrder);
            if (!success)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.QuestionAttachment.OrderUpdateSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attachment order {AttachmentId}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.QuestionAttachment.OrderUpdateError"), ex));
        }
    }
}
