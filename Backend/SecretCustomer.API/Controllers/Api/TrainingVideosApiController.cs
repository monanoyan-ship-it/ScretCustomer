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

    #endregion

    #region Participants

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
    /// Hatırlatma gönder
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

    #endregion
}

[ApiController]
[Route("api/my-trainings")]
[Authorize]
public class MyTrainingsApiController : BaseApiController
{
    private readonly ITrainingVideoService _trainingVideoService;
    private readonly ILogger<MyTrainingsApiController> _logger;

    public MyTrainingsApiController(
        ITrainingVideoService trainingVideoService,
        ILogger<MyTrainingsApiController> logger,
        IConfiguration configuration) : base(configuration)
    {
        _trainingVideoService = trainingVideoService;
        _logger = logger;
    }

    /// <summary>
    /// CustomerPersonnelId'yi claim'den alır
    /// </summary>
    private int GetCurrentCustomerPersonnelId()
    {
        // CustomerPersonnel için özel claim kullanılıyor
        var claim = User.FindFirst("CustomerPersonnelId");
        if (claim != null && int.TryParse(claim.Value, out var cpId))
            return cpId;

        return 0;
    }

    /// <summary>
    /// Kendi eğitimlerimi getir (CustomerPersonnel için)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyTrainings()
    {
        try
        {
            var customerPersonnelId = GetCurrentCustomerPersonnelId();
            if (customerPersonnelId == 0)
                return Unauthorized(CreateErrorResponse("CustomerPersonnel yetkisi gerekli"));

            var trainings = await _trainingVideoService.GetMyTrainingsAsync(customerPersonnelId);
            return Ok(trainings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading my trainings");
            return StatusCode(500, CreateErrorResponse("Eğitimler yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Tek bir eğitimi getir
    /// </summary>
    [HttpGet("{participantId}")]
    public async Task<IActionResult> GetMyTraining(int participantId)
    {
        try
        {
            var customerPersonnelId = GetCurrentCustomerPersonnelId();
            if (customerPersonnelId == 0)
                return Unauthorized(CreateErrorResponse("CustomerPersonnel yetkisi gerekli"));

            var training = await _trainingVideoService.GetMyTrainingByIdAsync(customerPersonnelId, participantId);
            if (training == null)
                return NotFound(CreateErrorResponse("Eğitim bulunamadı"));

            return Ok(training);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading my training {ParticipantId}", participantId);
            return StatusCode(500, CreateErrorResponse("Eğitim yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// İzleme ilerlemesini güncelle
    /// </summary>
    [HttpPost("{participantId}/progress")]
    public async Task<IActionResult> UpdateProgress(int participantId, [FromBody] UpdateWatchProgressDto dto)
    {
        try
        {
            var customerPersonnelId = GetCurrentCustomerPersonnelId();
            if (customerPersonnelId == 0)
                return Unauthorized(CreateErrorResponse("CustomerPersonnel yetkisi gerekli"));

            // Katılımcının kendisine ait olduğunu doğrula
            var training = await _trainingVideoService.GetMyTrainingByIdAsync(customerPersonnelId, participantId);
            if (training == null)
                return NotFound(CreateErrorResponse("Eğitim bulunamadı"));

            var result = await _trainingVideoService.UpdateWatchProgressAsync(participantId, dto);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating watch progress for {ParticipantId}", participantId);
            return StatusCode(500, CreateErrorResponse("İlerleme güncellenirken hata oluştu", ex));
        }
    }
}
