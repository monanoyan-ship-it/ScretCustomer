using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Evaluation;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class EvaluationService : IEvaluationService
{
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ApplicationDbContext _context;

    public EvaluationService(
        IEvaluationRepository evaluationRepository,
        IAssignmentRepository assignmentRepository,
        ApplicationDbContext context)
    {
        _evaluationRepository = evaluationRepository;
        _assignmentRepository = assignmentRepository;
        _context = context;
    }

    public async Task<EvaluationDto?> GetByIdAsync(Guid id)
    {
        var evaluation = await _evaluationRepository.GetByIdAsync(id, includeDetails: true);
        return evaluation == null ? null : MapToDto(evaluation);
    }

    public async Task<EvaluationDto?> GetByAssignmentIdAsync(Guid assignmentId)
    {
        var evaluation = await _evaluationRepository.GetByAssignmentIdAsync(assignmentId, includeDetails: true);
        return evaluation == null ? null : MapToDto(evaluation);
    }

    public async Task<IEnumerable<EvaluationDto>> GetByEvaluatorIdAsync(Guid evaluatorId)
    {
        var evaluations = await _evaluationRepository.GetByEvaluatorIdAsync(evaluatorId);
        return evaluations.Select(MapToDto);
    }

    public async Task<EvaluationDto> StartEvaluationAsync(Guid assignmentId, Guid? evaluatorId)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
        if (assignment == null)
            throw new KeyNotFoundException($"Assignment with ID {assignmentId} not found");

        // Check if evaluation already exists
        var existing = await _evaluationRepository.GetByAssignmentIdAsync(assignmentId);
        if (existing != null)
            throw new InvalidOperationException("Evaluation already exists for this assignment");

        var evaluation = new Evaluation
        {
            AssignmentId = assignmentId,
            EvaluatorId = evaluatorId,
            Status = EvaluationStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        var created = await _evaluationRepository.CreateAsync(evaluation);
        return MapToDto(created);
    }

    public async Task<EvaluationDto> SubmitEvaluationAsync(SubmitEvaluationDto dto)
    {
        // Get assignment with checklist details
        var assignment = await _assignmentRepository.GetByUniqueLinkAsync(
            (await _assignmentRepository.GetByIdAsync(dto.AssignmentId))?.UniqueLink ?? "",
            includeDetails: true);

        if (assignment == null)
            throw new KeyNotFoundException($"Assignment with ID {dto.AssignmentId} not found");

        // Get or create evaluation
        var evaluation = await _evaluationRepository.GetByAssignmentIdAsync(dto.AssignmentId)
            ?? new Evaluation
            {
                AssignmentId = dto.AssignmentId,
                EvaluatorId = dto.EvaluatorId,
                Status = EvaluationStatus.InProgress,
                StartedAt = DateTime.UtcNow
            };

        // Get all questions from checklist
        var allQuestions = assignment.Checklist.Sections
            .SelectMany(s => s.Questions)
            .ToList();

        // Calculate scores with N/A handling
        var scoreResult = CalculateScoreWithNA(allQuestions, dto.Answers);

        // Create or update answers
        evaluation.Answers.Clear();
        foreach (var answerDto in dto.Answers)
        {
            var question = allQuestions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
            if (question == null) continue;

            var earnedPoints = CalculateEarnedPoints(question, answerDto);

            evaluation.Answers.Add(new Answer
            {
                QuestionId = answerDto.QuestionId,
                AnswerText = answerDto.AnswerText,
                AnswerNumeric = answerDto.AnswerNumeric,
                IsNA = answerDto.IsNA,
                EarnedPoints = earnedPoints
            });
        }

        // Update evaluation
        evaluation.Status = EvaluationStatus.Completed;
        evaluation.CompletedAt = DateTime.UtcNow;
        evaluation.TotalScore = scoreResult.TotalEarned;
        evaluation.MaxScore = scoreResult.MaxPossible;
        evaluation.ScorePercentage = scoreResult.Percentage;
        evaluation.Notes = dto.Notes;

        // Update assignment
        assignment.IsCompleted = true;
        assignment.CompletedAt = DateTime.UtcNow;
        await _assignmentRepository.UpdateAsync(assignment);

        // Save evaluation
        if (evaluation.Id == Guid.Empty)
            evaluation = await _evaluationRepository.CreateAsync(evaluation);
        else
            evaluation = await _evaluationRepository.UpdateAsync(evaluation);

        return MapToDto(evaluation);
    }

    /// <summary>
    /// N/A Puan Yeniden Dağıtım Algoritması
    /// </summary>
    private (decimal TotalEarned, decimal MaxPossible, decimal Percentage) CalculateScoreWithNA(
        List<Question> allQuestions,
        List<SubmitAnswerDto> answers)
    {
        decimal totalEarned = 0;
        decimal totalMaxPoints = allQuestions.Sum(q => q.Points);

        // N/A işaretli soruların puanlarını topla
        var naQuestionIds = answers.Where(a => a.IsNA).Select(a => a.QuestionId).ToHashSet();
        var naPointsTotal = allQuestions.Where(q => naQuestionIds.Contains(q.Id)).Sum(q => q.Points);

        // N/A çıkarıldıktan sonra kalan maksimum puan
        decimal adjustedMaxPoints = totalMaxPoints - naPointsTotal;

        if (adjustedMaxPoints == 0)
            return (0, 0, 0);

        // Cevaplanan soruların puanlarını hesapla
        foreach (var answer in answers.Where(a => !a.IsNA))
        {
            var question = allQuestions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null) continue;

            var earnedPoints = CalculateEarnedPoints(question, answer);
            totalEarned += earnedPoints ?? 0;
        }

        // Yüzde hesapla (N/A çıkarılmış maksimum puana göre)
        var percentage = (totalEarned / adjustedMaxPoints) * 100;

        return (totalEarned, adjustedMaxPoints, Math.Round(percentage, 2));
    }

    private decimal? CalculateEarnedPoints(Question question, SubmitAnswerDto answer)
    {
        if (answer.IsNA)
            return null;

        return question.Type switch
        {
            QuestionType.MultipleChoice => question.Points, // Doğru cevap için tam puan
            QuestionType.Likert => (answer.AnswerNumeric ?? 0) * (question.Points / 5.0m), // 1-5 ölçeği
            QuestionType.Star => (answer.AnswerNumeric ?? 0) * (question.Points / 5.0m), // 1-5 yıldız
            QuestionType.Text => null, // Metin soruları puansız
            _ => 0
        };
    }

    private EvaluationDto MapToDto(Evaluation evaluation)
    {
        return new EvaluationDto
        {
            Id = evaluation.Id,
            AssignmentId = evaluation.AssignmentId,
            EvaluatorId = evaluation.EvaluatorId,
            EvaluatorName = evaluation.Evaluator != null
                ? $"{evaluation.Evaluator.FirstName} {evaluation.Evaluator.LastName}"
                : null,
            Status = evaluation.Status.ToString(),
            TotalScore = evaluation.TotalScore,
            MaxScore = evaluation.MaxScore,
            ScorePercentage = evaluation.ScorePercentage,
            StartedAt = evaluation.StartedAt,
            CompletedAt = evaluation.CompletedAt,
            Notes = evaluation.Notes,
            Answers = evaluation.Answers.Select(a => new AnswerDto
            {
                Id = a.Id,
                QuestionId = a.QuestionId,
                QuestionText = a.Question?.Text ?? "",
                AnswerText = a.AnswerText,
                AnswerNumeric = a.AnswerNumeric,
                IsNA = a.IsNA,
                EarnedPoints = a.EarnedPoints
            }).ToList()
        };
    }
}
