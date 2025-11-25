using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/evaluations")]
public class EvaluationsApiController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;
    private readonly ILogger<EvaluationsApiController> _logger;

    public EvaluationsApiController(
        IEvaluationService evaluationService,
        ILogger<EvaluationsApiController> logger)
    {
        _evaluationService = evaluationService;
        _logger = logger;
    }

    [HttpGet("evaluator")]
    [Authorize]
    public async Task<IActionResult> GetByEvaluator()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            var evaluations = await _evaluationService.GetByEvaluatorIdAsync(userId);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluations for current user");
            return StatusCode(500, new { message = "Değerlendirmeler yüklenirken bir hata oluştu." });
        }
    }
}
