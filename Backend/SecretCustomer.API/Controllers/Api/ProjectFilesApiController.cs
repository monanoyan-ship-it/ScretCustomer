using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Project;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/project-files")]
[Authorize]
public class ProjectFilesApiController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ProjectFilesApiController> _logger;
    private readonly ILocalizationService _localizationService;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip", ".rar", ".ppt", ".pptx" };
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

    public ProjectFilesApiController(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        ILogger<ProjectFilesApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _context = context;
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
            var files = await _context.ProjectFiles
                .Where(f => f.ProjectId == projectId)
                .OrderByDescending(f => f.UploadedAt)
                .Select(f => new ProjectFileDto
                {
                    Id = f.Id,
                    ProjectId = f.ProjectId,
                    FileName = f.FileName,
                    OriginalFileName = f.OriginalFileName,
                    ContentType = f.ContentType,
                    FileSize = f.FileSize,
                    UploadedAt = f.UploadedAt,
                    UploadedById = f.UploadedById,
                    UploadedByName = f.UploadedBy != null
                        ? f.UploadedBy.FirstName + " " + f.UploadedBy.LastName
                        : null,
                    Description = f.Description
                })
                .ToListAsync();

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
    [Authorize(Roles = "Admin,QualitySpecialist")]
    public async Task<IActionResult> Upload(int projectId, IFormFile file, [FromForm] string? description)
    {
        try
        {
            // Proje kontrolu
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
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
            var projectFile = new ProjectFile
            {
                ProjectId = projectId,
                FileName = storedFileName,
                OriginalFileName = file.FileName,
                FilePath = $"/uploads/projects/{projectId}/{storedFileName}",
                FileSize = file.Length,
                ContentType = file.ContentType,
                Description = description,
                UploadedAt = DateTime.UtcNow,
                UploadedById = userId
            };

            _context.ProjectFiles.Add(projectFile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File {FileName} uploaded for project {ProjectId}", file.FileName, projectId);

            // Kullanici adini al
            string? uploadedByName = null;
            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    uploadedByName = $"{user.FirstName} {user.LastName}";
                }
            }

            return Ok(new ProjectFileDto
            {
                Id = projectFile.Id,
                ProjectId = projectFile.ProjectId,
                FileName = projectFile.FileName,
                OriginalFileName = projectFile.OriginalFileName,
                ContentType = projectFile.ContentType,
                FileSize = projectFile.FileSize,
                UploadedAt = projectFile.UploadedAt,
                UploadedById = projectFile.UploadedById,
                UploadedByName = uploadedByName,
                Description = projectFile.Description
            });
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
            var projectFile = await _context.ProjectFiles
                .Include(f => f.Project)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (projectFile == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            // Yetki kontrolu - Admin her zaman indirebilir, diger roller atama kontrolu yapilir
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin")
            {
                // QualitySpecialist/FieldWorker - projeye atanmis mi kontrol et
                if (int.TryParse(userIdClaim, out var userId))
                {
                    var isAssigned = await _context.Set<Assignment>()
                        .AnyAsync(a => a.ProjectId == projectFile.ProjectId &&
                                      (a.AssignedUserId == userId || a.AssignedCustomerPersonnelId == userId));

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

            var filePath = Path.Combine(_environment.WebRootPath, projectFile.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFoundOnServer")));
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, projectFile.ContentType, projectFile.OriginalFileName);
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
    [Authorize(Roles = "Admin,QualitySpecialist")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var projectFile = await _context.ProjectFiles.FindAsync(id);
            if (projectFile == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            // Fiziksel dosyayi sil
            var filePath = Path.Combine(_environment.WebRootPath, projectFile.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Veritabanindan sil (soft delete)
            projectFile.IsDeleted = true;
            projectFile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

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
    [Authorize(Roles = "Admin,QualitySpecialist")]
    public async Task<IActionResult> UpdateDescription(int id, [FromBody] UpdateProjectFileDescriptionDto dto)
    {
        try
        {
            var projectFile = await _context.ProjectFiles.FindAsync(id);
            if (projectFile == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.FileNotFound")));
            }

            projectFile.Description = dto.Description;
            projectFile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.ProjectFile.DescriptionUpdateSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project file description {FileId}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.ProjectFile.DescriptionUpdateError"), ex));
        }
    }
}
