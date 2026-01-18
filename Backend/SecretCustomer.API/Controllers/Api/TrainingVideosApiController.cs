using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.TrainingVideo;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/training-videos")]
[Authorize]
public class TrainingVideosApiController : BaseApiController
{
    private readonly ITrainingVideoService _trainingVideoService;
    private readonly ILogger<TrainingVideosApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public TrainingVideosApiController(
        ITrainingVideoService trainingVideoService,
        ILogger<TrainingVideosApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _trainingVideoService = trainingVideoService;
        _logger = logger;
        _localizationService = localizationService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    #region Video CRUD

    /// <summary>
    /// Video listesini getir
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] TrainingVideoFilterDto? filter)
    {
        try
        {
            var videos = await _trainingVideoService.GetListAsync(filter);
            return Ok(videos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading training videos");
            return StatusCode(500, CreateErrorResponse("Video listesi yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Video detayını getir
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var video = await _trainingVideoService.GetByIdAsync(id);
            if (video == null)
                return NotFound(CreateErrorResponse("Video bulunamadı"));

            return Ok(video);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading training video {Id}", id);
            return StatusCode(500, CreateErrorResponse("Video yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Yeni video yükle
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [DisableRequestSizeLimit] // Video upload - limit Program.cs'de 500MB olarak ayarlı
    public async Task<IActionResult> Create([FromForm] CreateTrainingVideoDto dto, IFormFile videoFile, IFormFile? thumbnailFile)
    {
        try
        {
            if (videoFile == null || videoFile.Length == 0)
                return BadRequest(CreateErrorResponse("Video dosyası gereklidir"));

            using var videoStream = videoFile.OpenReadStream();
            Stream? thumbnailStream = thumbnailFile?.OpenReadStream();

            var video = await _trainingVideoService.CreateAsync(dto, videoStream, videoFile.FileName, thumbnailStream);

            thumbnailStream?.Dispose();
            return CreatedAtAction(nameof(GetById), new { id = video.Id }, video);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating training video");
            return StatusCode(500, CreateErrorResponse("Video oluşturulurken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Video güncelle
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTrainingVideoDto dto)
    {
        try
        {
            var video = await _trainingVideoService.UpdateAsync(id, dto);
            return Ok(video);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating training video {Id}", id);
            return StatusCode(500, CreateErrorResponse("Video güncellenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Video sil
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _trainingVideoService.DeleteAsync(id);
            if (!result)
                return NotFound(CreateErrorResponse("Video bulunamadı"));

            return Ok(new { message = "Video silindi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting training video {Id}", id);
            return StatusCode(500, CreateErrorResponse("Video silinirken hata oluştu", ex));
        }
    }

    #endregion

    #region Scope Options

    /// <summary>
    /// Kapsam seçeneklerini getir (Checklist, Soru Grupları, Sorular)
    /// </summary>
    [HttpGet("scopes/options")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetScopeOptions()
    {
        try
        {
            var options = await _trainingVideoService.GetScopeOptionsAsync();
            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading scope options");
            return StatusCode(500, CreateErrorResponse("Kapsam seçenekleri yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Video scope'undaki checklist'lerde değerlendirmesi olan müşterileri getir
    /// </summary>
    [HttpGet("{id}/scope-customers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetScopeCustomers(int id)
    {
        try
        {
            var customers = await _trainingVideoService.GetScopeCustomersAsync(id);
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading scope customers for video {Id}", id);
            return StatusCode(500, CreateErrorResponse("Müşteri listesi yüklenirken hata oluştu", ex));
        }
    }

    #endregion

    #region Video Streaming

    /// <summary>
    /// Video stream (izleme için)
    /// </summary>
    [HttpGet("{id}/stream")]
    public async Task<IActionResult> StreamVideo(int id)
    {
        try
        {
            var videoPath = await _trainingVideoService.GetVideoPathAsync(id);
            if (videoPath == null || !System.IO.File.Exists(videoPath))
                return NotFound(CreateErrorResponse("Video bulunamadı"));

            var contentType = GetContentType(videoPath);
            return PhysicalFile(videoPath, contentType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming video {Id}", id);
            return StatusCode(500, CreateErrorResponse("Video oynatılırken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Video thumbnail (önizleme görseli)
    /// </summary>
    [HttpGet("{id}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(int id)
    {
        try
        {
            var video = await _trainingVideoService.GetByIdAsync(id);
            if (video == null)
                return NotFound();

            if (string.IsNullOrEmpty(video.ThumbnailPath) || !System.IO.File.Exists(video.ThumbnailPath))
            {
                // Default thumbnail döndür
                var defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "video-placeholder.png");
                if (System.IO.File.Exists(defaultPath))
                    return PhysicalFile(defaultPath, "image/png");
                return NotFound();
            }

            return PhysicalFile(video.ThumbnailPath, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading thumbnail for video {Id}", id);
            return NotFound();
        }
    }

    private static string GetContentType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".ogg" => "video/ogg",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            _ => "video/mp4"
        };
    }

    /// <summary>
    /// Video scope'una göre ilişkili projeleri getir
    /// </summary>
    [HttpGet("{id}/related-projects")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRelatedProjects(int id)
    {
        try
        {
            var video = await _trainingVideoService.GetByIdAsync(id);
            if (video == null)
                return NotFound(CreateErrorResponse("Video bulunamadı"));

            // Video scope'undaki checklist'lerin projelerini bul
            var checklistIds = video.Scopes
                .Where(s => s.ChecklistId.HasValue)
                .Select(s => s.ChecklistId!.Value)
                .Distinct()
                .ToList();

            if (!checklistIds.Any())
                return Ok(new List<object>());

            // ApplicationDbContext'e erişim için IServiceProvider kullan
            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SecretCustomer.Data.ApplicationDbContext>();

            var projects = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                context.Projects
                    .Where(p => !p.IsDeleted && checklistIds.Contains(p.ChecklistId))
                    .Select(p => new { p.Id, p.Name, p.ChecklistId })
            );

            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading related projects for video {Id}", id);
            return StatusCode(500, CreateErrorResponse("İlişkili projeler yüklenirken hata oluştu", ex));
        }
    }

    #endregion
}

[ApiController]
[Route("api/training-video-assignments")]
[Authorize]
public class TrainingVideoAssignmentsApiController : BaseApiController
{
    private readonly ITrainingVideoService _trainingVideoService;
    private readonly ILogger<TrainingVideoAssignmentsApiController> _logger;

    public TrainingVideoAssignmentsApiController(
        ITrainingVideoService trainingVideoService,
        ILogger<TrainingVideoAssignmentsApiController> logger,
        IConfiguration configuration) : base(configuration)
    {
        _trainingVideoService = trainingVideoService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    #region Assignment CRUD

    /// <summary>
    /// Atama listesini getir
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] TrainingVideoAssignmentFilterDto? filter)
    {
        try
        {
            var assignments = await _trainingVideoService.GetAssignmentsAsync(filter);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignments");
            return StatusCode(500, CreateErrorResponse("Atamalar yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Atama detayını getir
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var assignment = await _trainingVideoService.GetAssignmentByIdAsync(id);
            if (assignment == null)
                return NotFound(CreateErrorResponse("Atama bulunamadı"));

            return Ok(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse("Atama yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Yeni atama oluştur
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateTrainingVideoAssignmentDto dto)
    {
        try
        {
            var assignment = await _trainingVideoService.CreateAssignmentAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = assignment.Id }, assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment");
            return StatusCode(500, CreateErrorResponse("Atama oluşturulurken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Otomatik atama önizlemesi
    /// </summary>
    [HttpPost("preview")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PreviewAutoAssignment([FromBody] CreateTrainingVideoAssignmentDto dto)
    {
        try
        {
            var preview = await _trainingVideoService.PreviewAutoAssignmentAsync(dto);
            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing auto assignment");
            return StatusCode(500, CreateErrorResponse("Önizleme oluşturulurken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Atama sil
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _trainingVideoService.DeleteAssignmentAsync(id);
            if (!result)
                return NotFound(CreateErrorResponse("Atama bulunamadı"));

            return Ok(new { message = "Atama silindi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse("Atama silinirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Eğitim video atamaları için email şablonlarını getir
    /// </summary>
    [HttpGet("email-templates")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetEmailTemplates()
    {
        try
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SecretCustomer.Data.ApplicationDbContext>();

            var templates = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                context.EmailTemplates
                    .Where(t => !t.IsDeleted && t.IsActive &&
                           t.TemplateTypeId == SecretCustomer.Core.Enums.EmailTemplateTypes.Ids.TrainingVideo)
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.Description,
                        t.TemplateTypeId,
                        TemplateTypeName = SecretCustomer.Core.Enums.EmailTemplateTypes.GetById(t.TemplateTypeId)!.Description,
                        t.IsDefault
                    })
            );

            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading email templates");
            return StatusCode(500, CreateErrorResponse("Email şablonları yüklenirken hata oluştu", ex));
        }
    }

    #endregion

    #region Participants

    /// <summary>
    /// Tüm atamalardaki katılımcıları getir
    /// </summary>
    [HttpGet("all-participants")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllParticipants()
    {
        try
        {
            var participants = await _trainingVideoService.GetAllParticipantsAsync();
            return Ok(participants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all participants");
            return StatusCode(500, CreateErrorResponse("Katılımcılar yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Atama katılımcılarını getir
    /// </summary>
    [HttpGet("{id}/participants")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetParticipants(int id)
    {
        try
        {
            var participants = await _trainingVideoService.GetParticipantsAsync(id);
            return Ok(participants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading participants for assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse("Katılımcılar yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Hatırlatma gönder (tamamlamamışlara)
    /// </summary>
    [HttpPost("{id}/send-reminders")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendReminders(int id)
    {
        try
        {
            var result = await _trainingVideoService.SendRemindersAsync(id);
            return Ok(new { message = "Hatırlatmalar gönderildi", success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminders for assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse("Hatırlatmalar gönderilirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Seçili katılımcılara email gönder
    /// </summary>
    [HttpPost("send-emails")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendEmails([FromBody] SecretCustomer.Core.DTOs.TrainingVideo.SendTrainingEmailDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var count = await _trainingVideoService.SendEmailsAsync(dto, userId, dto.EmailTypeId);
            return Ok(new { message = $"{count} kişiye email gönderildi", sentCount = count, success = count > 0 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending emails for assignment {AssignmentId}", dto.AssignmentId);
            return StatusCode(500, CreateErrorResponse("Email gönderilirken hata oluştu", ex));
        }
    }

    #endregion
}
