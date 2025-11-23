using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.API.ViewModels;
using SecretCustomer.Core.DTOs.Evaluation;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Evaluator,Admin")]
public class EvaluationsController : Controller
{
    private readonly IAssignmentService _assignmentService;
    private readonly IEvaluationService _evaluationService;
    private readonly IChecklistService _checklistService;
    private readonly ILogger<EvaluationsController> _logger;

    public EvaluationsController(
        IAssignmentService assignmentService,
        IEvaluationService evaluationService,
        IChecklistService checklistService,
        ILogger<EvaluationsController> logger)
    {
        _assignmentService = assignmentService;
        _evaluationService = evaluationService;
        _checklistService = checklistService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                TempData["Error"] = "Kullanıcı kimliği bulunamadı.";
                return View(new EvaluationIndexViewModel());
            }

            var assignments = await _assignmentService.GetByUserIdAsync(userId);
            var pendingAssignments = assignments
                .Where(a => !a.IsCompleted)
                .ToList();

            var viewModel = new EvaluationIndexViewModel
            {
                PendingAssignments = pendingAssignments
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluations");
            TempData["Error"] = "Değerlendirmeler yüklenirken bir hata oluştu.";
            return View(new EvaluationIndexViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Start(Guid assignmentId)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(assignmentId);
            if (assignment == null)
            {
                TempData["Error"] = "Atama bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // Verify user has access
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!User.IsInRole("Admin") &&
                (!Guid.TryParse(userIdClaim, out Guid userId) || assignment.AssignedUserId != userId))
            {
                TempData["Error"] = "Bu değerlendirmeye erişim yetkiniz yok.";
                return RedirectToAction(nameof(Index));
            }

            var checklist = await _checklistService.GetByIdAsync(assignment.ChecklistId);
            if (checklist == null)
            {
                TempData["Error"] = "Kontrol listesi bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new EvaluationFormViewModel
            {
                AssignmentId = assignmentId,
                ProjectName = assignment.ProjectName,
                BranchName = assignment.BranchName ?? "",
                ChecklistName = checklist.Name,
                Sections = checklist.Sections.Select(s => new EvaluationSectionViewModel
                {
                    Name = s.Name,
                    Questions = s.Questions.OrderBy(q => q.Order).Select(q => new EvaluationQuestionViewModel
                    {
                        QuestionId = q.Id,
                        Text = q.Text,
                        QuestionType = q.Type,
                        Points = q.Points,
                        AllowNA = q.AllowNA,
                        Options = q.Options != null ? string.Join(",", q.Options.Select(o => o.Value)) : "",
                        Order = q.Order
                    }).ToList()
                }).ToList()
            };

            return View("Evaluate", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting evaluation for assignment {AssignmentId}", assignmentId);
            TempData["Error"] = "Değerlendirme başlatılırken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(EvaluationFormViewModel model)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                TempData["Error"] = "Kullanıcı kimliği bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var answers = new List<SubmitAnswerDto>();
            foreach (var section in model.Sections)
            {
                foreach (var question in section.Questions)
                {
                    var answer = new SubmitAnswerDto
                    {
                        QuestionId = question.QuestionId,
                        AnswerNumeric = question.AnswerNumeric,
                        AnswerText = question.AnswerText,
                        IsNA = question.AnswerIsNA
                    };
                    answers.Add(answer);
                }
            }

            var evaluationDto = new SubmitEvaluationDto
            {
                AssignmentId = model.AssignmentId,
                EvaluatorId = userId,
                Notes = model.Notes,
                Answers = answers
            };

            await _evaluationService.SubmitEvaluationAsync(evaluationDto);
            TempData["Success"] = "Değerlendirme başarıyla gönderildi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting evaluation");
            ModelState.AddModelError(string.Empty, "Değerlendirme gönderilirken bir hata oluştu.");
            return View("Evaluate", model);
        }
    }
}
