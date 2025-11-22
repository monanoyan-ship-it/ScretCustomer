using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Evaluation;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EvaluationController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;
    private readonly ILogger<EvaluationController> _logger;

    public EvaluationController(
        IEvaluationService evaluationService,
        ILogger<EvaluationController> logger)
    {
        _evaluationService = evaluationService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EvaluationDto>> GetById(Guid id)
    {
        try
        {
            var evaluation = await _evaluationService.GetByIdAsync(id);
            if (evaluation == null)
                return NotFound($"Evaluation with ID {id} not found");

            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluation {EvaluationId}", id);
            return StatusCode(500, "An error occurred while retrieving the evaluation");
        }
    }

    [HttpGet("assignment/{assignmentId}")]
    public async Task<ActionResult<EvaluationDto>> GetByAssignmentId(Guid assignmentId)
    {
        try
        {
            var evaluation = await _evaluationService.GetByAssignmentIdAsync(assignmentId);
            if (evaluation == null)
                return NotFound($"Evaluation for assignment {assignmentId} not found");

            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluation for assignment {AssignmentId}", assignmentId);
            return StatusCode(500, "An error occurred while retrieving the evaluation");
        }
    }

    [HttpGet("evaluator/{evaluatorId}")]
    public async Task<ActionResult<IEnumerable<EvaluationDto>>> GetByEvaluatorId(Guid evaluatorId)
    {
        try
        {
            var evaluations = await _evaluationService.GetByEvaluatorIdAsync(evaluatorId);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluations for evaluator {EvaluatorId}", evaluatorId);
            return StatusCode(500, "An error occurred while retrieving evaluations");
        }
    }

    [HttpPost("start")]
    public async Task<ActionResult<EvaluationDto>> StartEvaluation([FromBody] StartEvaluationRequest request)
    {
        try
        {
            var evaluation = await _evaluationService.StartEvaluationAsync(request.AssignmentId, request.EvaluatorId);
            return Ok(evaluation);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Assignment not found");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting evaluation");
            return StatusCode(500, "An error occurred while starting the evaluation");
        }
    }

    [HttpPost("submit")]
    public async Task<ActionResult<EvaluationDto>> SubmitEvaluation([FromBody] SubmitEvaluationDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var evaluation = await _evaluationService.SubmitEvaluationAsync(dto);
            return Ok(evaluation);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Related entity not found");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting evaluation");
            return StatusCode(500, "An error occurred while submitting the evaluation");
        }
    }
}

public record StartEvaluationRequest(Guid AssignmentId, Guid? EvaluatorId);
