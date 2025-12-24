using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/answers")]
[Authorize]
public class AnswersApiController : BaseApiController
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<AnswersApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public AnswersApiController(
        IFileUploadService fileUploadService,
        ILogger<AnswersApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Cevaba dosya ekler
    /// </summary>
    [HttpPost("{answerId}/attachment")]
    [RequestSizeLimit(52428800)] // 50 MB
    public async Task<IActionResult> UploadAttachment(Guid answerId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotSelected")));
            }

            using var stream = file.OpenReadStream();
            var result = await _fileUploadService.UploadAnswerAttachmentAsync(
                answerId,
                stream,
                file.FileName,
                file.ContentType);

            if (!result.Success)
            {
                return BadRequest(CreateErrorResponse(result.ErrorMessage ?? "Upload failed"));
            }

            return Ok(new
            {
                message = await _localizationService.GetResourceAsync("Api.Common.FileUploadSuccess"),
                fileName = result.FileName,
                fileSize = result.FileSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading attachment for answer {AnswerId}", answerId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Answer.UploadError"), ex));
        }
    }

    /// <summary>
    /// Cevabın ekli dosyasını siler
    /// </summary>
    [HttpDelete("{answerId}/attachment")]
    public async Task<IActionResult> DeleteAttachment(Guid answerId)
    {
        try
        {
            var success = await _fileUploadService.DeleteAnswerAttachmentAsync(answerId);
            if (!success)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Common.FileDeleteSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment for answer {AnswerId}", answerId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Answer.DeleteError"), ex));
        }
    }

    /// <summary>
    /// Cevabın ekli dosyasını indirir
    /// </summary>
    [HttpGet("{answerId}/attachment")]
    [AllowAnonymous] // QR kod ile erişim için
    public async Task<IActionResult> DownloadAttachment(Guid answerId)
    {
        try
        {
            var result = await _fileUploadService.GetAnswerAttachmentAsync(answerId);
            if (result == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            var (fileStream, fileName, contentType) = result.Value;
            return File(fileStream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading attachment for answer {AnswerId}", answerId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Answer.DownloadError"), ex));
        }
    }
}
