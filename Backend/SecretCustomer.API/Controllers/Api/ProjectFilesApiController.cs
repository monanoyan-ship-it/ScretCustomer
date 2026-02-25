using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Project;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/project-files")]
[Authorize]
public class ProjectFilesApiController : BaseApiController
{
    private readonly IProjectFileService _projectFileService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ProjectFilesApiController> _logger;
    private readonly ILocalizationService _localizationService;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip", ".rar", ".ppt", ".pptx" };
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

    public ProjectFilesApiController(
        IProjectFileService projectFileService,
        IWebHostEnvironment environment,
        ILogger<ProjectFilesApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _projectFileService = projectFileService;
        _environment = environment;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Projeye ait dosyalari listele
    /// </summary>
    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetByProject(int projectId)
    {
        try
        {
            var files = await _projectFileService.GetByProjectAsync(projectId);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FilesLoadError"), ex));
        }
    }

    /// <summary>
    /// Dosya yukle
    /// </summary>
    [HttpPost("project/{projectId}")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> Upload(int projectId, IFormFile file, [FromForm] string? description)
    {
        try
        {
            // Proje kontrolu
            var projectExists = await _projectFileService.ProjectExistsAsync(projectId);
            if (!projectExists)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
            }

            // Dosya kontrolu
            if (file == null || file.Length == 0)
            {
                return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotSelected")));
            }

            // Boyut kontrolu
            if (file.Length > MaxFileSize)
            {
                return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileSizeExceeded50MB")));
            }

            // Uzanti kontrolu
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileTypeNotSupported")));
            }

            // Dosya kaydetme dizini
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "projects", projectId.ToString());
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Unique dosya adi
            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            // Dosyayi kaydet
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Kullanici ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (int.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            // Veritabanina kaydet
            var dbFilePath = $"/uploads/projects/{projectId}/{storedFileName}";
            var result = await _projectFileService.SaveFileRecordAsync(projectId, storedFileName, file.FileName, dbFilePath, file.Length, file.ContentType, description, userId);

            _logger.LogInformation("File {FileName} uploaded for project {ProjectId}", file.FileName, projectId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for project {ProjectId}", projectId);
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
            var downloadInfo = await _projectFileService.GetDownloadInfoAsync(id);
            if (downloadInfo == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            var (dbFilePath, contentType, originalFileName, projectId) = downloadInfo.Value;

            // Yetki kontrolu - Admin her zaman indirebilir, diger roller atama kontrolu yapilir
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin")
            {
                // QualitySpecialist/FieldWorker - projeye atanmis mi kontrol et
                if (int.TryParse(userIdClaim, out var userId))
                {
                    var isAssigned = await _projectFileService.IsUserAssignedToProjectAsync(userId, projectId);

                    if (!isAssigned)
                    {
                        return Forbid();
                    }
                }
                else
                {
                    return Forbid();
                }
            }

            var filePath = Path.Combine(_environment.WebRootPath, dbFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFoundOnServer")));
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType, originalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading project file {FileId}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Answer.DownloadError"), ex));
        }
    }

    /// <summary>
    /// Dosya sil
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var (success, dbFilePath) = await _projectFileService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            // Fiziksel dosyayi sil
            if (dbFilePath != null)
            {
                var filePath = Path.Combine(_environment.WebRootPath, dbFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _logger.LogInformation("Project file {FileId} deleted", id);

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Common.FileDeleteSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project file {FileId}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Answer.DeleteError"), ex));
        }
    }

    /// <summary>
    /// Dosya aciklamasini guncelle
    /// </summary>
    [HttpPut("{id}/description")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> UpdateDescription(int id, [FromBody] UpdateProjectFileDescriptionDto dto)
    {
        try
        {
            var success = await _projectFileService.UpdateDescriptionAsync(id, dto.Description);
            if (!success)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.ProjectFile.DescriptionUpdateSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project file description {FileId}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.ProjectFile.DescriptionUpdateError"), ex));
        }
    }
}
