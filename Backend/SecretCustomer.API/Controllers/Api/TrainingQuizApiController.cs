using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.TrainingQuiz;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/training-quiz")]
[Authorize]
public class TrainingQuizApiController : BaseApiController
{
    private readonly ITrainingQuizService _trainingQuizService;
    private readonly ILogger<TrainingQuizApiController> _logger;

    public TrainingQuizApiController(
        ITrainingQuizService trainingQuizService,
        ILogger<TrainingQuizApiController> logger,
        IConfiguration configuration) : base(configuration)
    {
        _trainingQuizService = trainingQuizService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    #region Quiz CRUD (Admin)

    /// <summary>
    /// Quiz listesini getir
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] TrainingQuizFilterDto? filter)
    {
        try
        {
            var quizzes = await _trainingQuizService.GetListAsync(filter);
            return Ok(quizzes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading training quizzes");
            return StatusCode(500, CreateErrorResponse("Anket listesi yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Quiz detayını getir
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var quiz = await _trainingQuizService.GetByIdAsync(id);
            if (quiz == null)
                return NotFound(CreateErrorResponse("Anket bulunamadı"));

            return Ok(quiz);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading training quiz {Id}", id);
            return StatusCode(500, CreateErrorResponse("Anket yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Video'ya bağlı quiz'i getir
    /// </summary>
    [HttpGet("by-video/{videoId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetByVideoId(int videoId)
    {
        try
        {
            var quiz = await _trainingQuizService.GetByVideoIdAsync(videoId);
            if (quiz == null)
                return NotFound(CreateErrorResponse("Bu videoya bağlı anket bulunamadı"));

            return Ok(quiz);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading quiz for video {VideoId}", videoId);
            return StatusCode(500, CreateErrorResponse("Anket yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Yeni quiz oluştur
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateTrainingQuizDto dto)
    {
        try
        {
            var quiz = await _trainingQuizService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = quiz.Id }, quiz);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating training quiz");
            return StatusCode(500, CreateErrorResponse("Anket oluşturulurken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Quiz güncelle
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTrainingQuizDto dto)
    {
        try
        {
            var quiz = await _trainingQuizService.UpdateAsync(id, dto);
            return Ok(quiz);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating training quiz {Id}", id);
            return StatusCode(500, CreateErrorResponse("Anket güncellenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Quiz sil
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _trainingQuizService.DeleteAsync(id);
            return Ok(new { message = "Anket silindi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting training quiz {Id}", id);
            return StatusCode(500, CreateErrorResponse("Anket silinirken hata oluştu", ex));
        }
    }

    #endregion

    #region Sorular

    /// <summary>
    /// Quiz'e soru ekle
    /// </summary>
    [HttpPost("{quizId}/questions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddQuestion(int quizId, [FromBody] CreateTrainingQuizQuestionDto dto)
    {
        try
        {
            var question = await _trainingQuizService.AddQuestionAsync(quizId, dto);
            return Ok(question);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding question to quiz {QuizId}", quizId);
            return StatusCode(500, CreateErrorResponse("Soru eklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Soru güncelle
    /// </summary>
    [HttpPut("questions/{questionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody] UpdateTrainingQuizQuestionDto dto)
    {
        try
        {
            var question = await _trainingQuizService.UpdateQuestionAsync(questionId, dto);
            return Ok(question);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question {QuestionId}", questionId);
            return StatusCode(500, CreateErrorResponse("Soru güncellenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Soru sil
    /// </summary>
    [HttpDelete("questions/{questionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteQuestion(int questionId)
    {
        try
        {
            await _trainingQuizService.DeleteQuestionAsync(questionId);
            return Ok(new { message = "Soru silindi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question {QuestionId}", questionId);
            return StatusCode(500, CreateErrorResponse("Soru silinirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Soruları yeniden sırala
    /// </summary>
    [HttpPost("{quizId}/questions/reorder")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReorderQuestions(int quizId, [FromBody] List<int> questionIds)
    {
        try
        {
            await _trainingQuizService.ReorderQuestionsAsync(quizId, questionIds);
            return Ok(new { message = "Sıralama güncellendi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering questions for quiz {QuizId}", quizId);
            return StatusCode(500, CreateErrorResponse("Sıralama güncellenirken hata oluştu", ex));
        }
    }

    #endregion

    #region Seçenekler

    /// <summary>
    /// Soruya seçenek ekle
    /// </summary>
    [HttpPost("questions/{questionId}/options")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddOption(int questionId, [FromBody] CreateTrainingQuizOptionDto dto)
    {
        try
        {
            var option = await _trainingQuizService.AddOptionAsync(questionId, dto);
            return Ok(option);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding option to question {QuestionId}", questionId);
            return StatusCode(500, CreateErrorResponse("Seçenek eklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Seçenek güncelle
    /// </summary>
    [HttpPut("options/{optionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOption(int optionId, [FromBody] UpdateTrainingQuizOptionDto dto)
    {
        try
        {
            var option = await _trainingQuizService.UpdateOptionAsync(optionId, dto);
            return Ok(option);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating option {OptionId}", optionId);
            return StatusCode(500, CreateErrorResponse("Seçenek güncellenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Seçenek sil
    /// </summary>
    [HttpDelete("options/{optionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteOption(int optionId)
    {
        try
        {
            await _trainingQuizService.DeleteOptionAsync(optionId);
            return Ok(new { message = "Seçenek silindi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting option {OptionId}", optionId);
            return StatusCode(500, CreateErrorResponse("Seçenek silinirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Seçenekleri yeniden sırala
    /// </summary>
    [HttpPost("questions/{questionId}/options/reorder")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReorderOptions(int questionId, [FromBody] List<int> optionIds)
    {
        try
        {
            await _trainingQuizService.ReorderOptionsAsync(questionId, optionIds);
            return Ok(new { message = "Sıralama güncellendi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering options for question {QuestionId}", questionId);
            return StatusCode(500, CreateErrorResponse("Sıralama güncellenirken hata oluştu", ex));
        }
    }

    #endregion

    #region Quiz Yanıtları (Admin)

    /// <summary>
    /// Quiz yanıtlarını getir
    /// </summary>
    [HttpGet("responses")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetResponses([FromQuery] TrainingQuizResponseFilterDto? filter)
    {
        try
        {
            var responses = await _trainingQuizService.GetResponsesAsync(filter);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading quiz responses");
            return StatusCode(500, CreateErrorResponse("Yanıtlar yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Quiz yanıt detayını getir
    /// </summary>
    [HttpGet("responses/{responseId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetResponseById(int responseId)
    {
        try
        {
            var response = await _trainingQuizService.GetResponseByIdAsync(responseId);
            if (response == null)
                return NotFound(CreateErrorResponse("Yanıt bulunamadı"));

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading quiz response {ResponseId}", responseId);
            return StatusCode(500, CreateErrorResponse("Yanıt yüklenirken hata oluştu", ex));
        }
    }

    #endregion

    #region Video Quiz Durumu

    /// <summary>
    /// Video'nun quiz durumunu getir
    /// </summary>
    [HttpGet("video-status/{videoId}")]
    public async Task<IActionResult> GetVideoQuizStatus(int videoId, [FromQuery] int? participantId, [FromQuery] int? externalParticipantId)
    {
        try
        {
            var status = await _trainingQuizService.GetVideoQuizStatusAsync(videoId, participantId, externalParticipantId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading quiz status for video {VideoId}", videoId);
            return StatusCode(500, CreateErrorResponse("Durum yüklenirken hata oluştu", ex));
        }
    }

    #endregion

    #region Video-Quiz Atama

    /// <summary>
    /// Video için kullanılabilir quizleri getir (atanmamış veya bu videoya atanmış)
    /// </summary>
    [HttpGet("available-for-video")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAvailableQuizzesForVideo([FromQuery] int? videoId)
    {
        try
        {
            var quizzes = await _trainingQuizService.GetAvailableQuizzesForVideoAsync(videoId);
            return Ok(quizzes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading available quizzes for video {VideoId}", videoId);
            return StatusCode(500, CreateErrorResponse("Anketler yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Video'ya quiz ata (veya atamasını kaldır)
    /// </summary>
    [HttpPost("assign-to-video/{videoId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignQuizToVideo(int videoId, [FromBody] AssignQuizToVideoDto dto)
    {
        try
        {
            await _trainingQuizService.AssignQuizToVideoAsync(dto.QuizId, videoId);
            return Ok(new { message = dto.QuizId.HasValue ? "Anket atandı" : "Anket ataması kaldırıldı" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning quiz {QuizId} to video {VideoId}", dto.QuizId, videoId);
            return StatusCode(500, CreateErrorResponse("Anket atanırken hata oluştu", ex));
        }
    }

    #endregion
}

/// <summary>
/// Katılımcı quiz endpoint'leri - TrainingVideos controller'ında
/// </summary>
[ApiController]
[Route("api/training-videos")]
public class TrainingVideoQuizApiController : BaseApiController
{
    private readonly ITrainingQuizService _trainingQuizService;
    private readonly ILogger<TrainingVideoQuizApiController> _logger;

    public TrainingVideoQuizApiController(
        ITrainingQuizService trainingQuizService,
        ILogger<TrainingVideoQuizApiController> logger,
        IConfiguration configuration) : base(configuration)
    {
        _trainingQuizService = trainingQuizService;
        _logger = logger;
    }

    #region Katılımcı (Internal) Quiz Endpoint'leri

    /// <summary>
    /// Katılımcının quiz bilgisini getir
    /// </summary>
    [HttpGet("participant/{participantId}/quiz")]
    [Authorize]
    public async Task<IActionResult> GetParticipantQuiz(int participantId)
    {
        try
        {
            var quiz = await _trainingQuizService.GetQuizForParticipantAsync(participantId, isExternal: false);
            if (quiz == null)
                return NotFound(CreateErrorResponse("Bu video için anket bulunamadı"));

            return Ok(quiz);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading quiz for participant {ParticipantId}", participantId);
            return StatusCode(500, CreateErrorResponse("Anket yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Katılımcı quiz'i başlat (soruları getir)
    /// </summary>
    [HttpPost("participant/{participantId}/quiz/start")]
    [Authorize]
    public async Task<IActionResult> StartParticipantQuiz(int participantId)
    {
        try
        {
            var quiz = await _trainingQuizService.StartQuizAsync(participantId, isExternal: false);
            if (quiz == null)
                return NotFound(CreateErrorResponse("Bu video için anket bulunamadı"));

            return Ok(quiz);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting quiz for participant {ParticipantId}", participantId);
            return StatusCode(500, CreateErrorResponse("Anket başlatılırken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Katılımcı quiz cevaplarını gönder
    /// </summary>
    [HttpPost("participant/{participantId}/quiz/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitParticipantQuiz(int participantId, [FromBody] SubmitTrainingQuizDto dto)
    {
        try
        {
            dto.ParticipantId = participantId;
            dto.ExternalParticipantId = null;

            var result = await _trainingQuizService.SubmitQuizAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting quiz for participant {ParticipantId}", participantId);
            return StatusCode(500, CreateErrorResponse("Anket gönderilirken hata oluştu", ex));
        }
    }

    #endregion

    #region Dış Katılımcı Quiz Endpoint'leri

    /// <summary>
    /// Dış katılımcının quiz bilgisini getir
    /// </summary>
    [HttpGet("external/{token}/quiz")]
    [AllowAnonymous]
    public async Task<IActionResult> GetExternalQuiz(string token)
    {
        try
        {
            // Token'dan participant ID'yi çöz
            var participantId = await GetExternalParticipantIdByToken(token);
            if (participantId == null)
                return NotFound(CreateErrorResponse("Geçersiz link"));

            var quiz = await _trainingQuizService.GetQuizForParticipantAsync(participantId.Value, isExternal: true);
            if (quiz == null)
                return NotFound(CreateErrorResponse("Bu video için anket bulunamadı"));

            return Ok(quiz);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading quiz for external participant with token");
            return StatusCode(500, CreateErrorResponse("Anket yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Dış katılımcı quiz'i başlat (soruları getir)
    /// </summary>
    [HttpPost("external/{token}/quiz/start")]
    [AllowAnonymous]
    public async Task<IActionResult> StartExternalQuiz(string token)
    {
        try
        {
            var participantId = await GetExternalParticipantIdByToken(token);
            if (participantId == null)
                return NotFound(CreateErrorResponse("Geçersiz link"));

            var quiz = await _trainingQuizService.StartQuizAsync(participantId.Value, isExternal: true);
            if (quiz == null)
                return NotFound(CreateErrorResponse("Bu video için anket bulunamadı"));

            return Ok(quiz);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting quiz for external participant with token");
            return StatusCode(500, CreateErrorResponse("Anket başlatılırken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Dış katılımcı quiz cevaplarını gönder
    /// </summary>
    [HttpPost("external/{token}/quiz/submit")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitExternalQuiz(string token, [FromBody] SubmitTrainingQuizDto dto)
    {
        try
        {
            var participantId = await GetExternalParticipantIdByToken(token);
            if (participantId == null)
                return NotFound(CreateErrorResponse("Geçersiz link"));

            dto.ParticipantId = null;
            dto.ExternalParticipantId = participantId.Value;

            var result = await _trainingQuizService.SubmitQuizAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting quiz for external participant with token");
            return StatusCode(500, CreateErrorResponse("Anket gönderilirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Token'dan external participant ID'yi bul
    /// </summary>
    private async Task<int?> GetExternalParticipantIdByToken(string token)
    {
        // Bu metod TrainingVideoService'ten alınabilir veya DbContext kullanılabilir
        // Şimdilik basit bir implementasyon
        using var scope = HttpContext.RequestServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SecretCustomer.Data.ApplicationDbContext>();

        var participant = await context.TrainingVideoExternalParticipants
            .FirstOrDefaultAsync(p => p.Token == token && !p.IsDeleted);

        return participant?.Id;
    }

    #endregion
}
