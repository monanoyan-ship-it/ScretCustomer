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

    // Helper: DateTime'ı UTC'ye çevir (PostgreSQL için gerekli)
    private static DateTime? ToUtc(DateTime? dateTime)
    {
        if (!dateTime.HasValue) return null;
        if (dateTime.Value.Kind == DateTimeKind.Utc) return dateTime;
        return DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
    }

    private static DateTime ToUtc(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc) return dateTime;
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    public async Task<EvaluationDto?> GetByIdAsync(int id)
    {
        var evaluation = await _evaluationRepository.GetByIdAsync(id, includeDetails: true);
        return evaluation == null ? null : await MapToDtoAsync(evaluation);
    }

    public async Task<EvaluationDto?> GetByAssignmentIdAsync(int assignmentId)
    {
        var evaluation = await _evaluationRepository.GetByAssignmentIdAsync(assignmentId, includeDetails: true);
        return evaluation == null ? null : await MapToDtoAsync(evaluation);
    }

    public async Task<IEnumerable<EvaluationDto>> GetByEvaluatorIdAsync(int evaluatorId)
    {
        var evaluations = await _evaluationRepository.GetByEvaluatorIdAsync(evaluatorId);
        var result = new List<EvaluationDto>();
        foreach (var eval in evaluations)
        {
            result.Add(await MapToDtoAsync(eval));
        }
        return result;
    }

    public async Task<IEnumerable<EvaluationDto>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var evaluations = await _context.Evaluations
            .Include(e => e.Evaluator)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<EvaluationDto>();
        foreach (var eval in evaluations)
        {
            result.Add(await MapToDtoAsync(eval));
        }
        return result;
    }

    public async Task<IEnumerable<EvaluationDto>> GetByProjectIdAsync(int projectId)
    {
        var evaluations = await _context.Evaluations
            .Include(e => e.Evaluator)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => !e.IsDeleted && e.Assignment.ProjectId == projectId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var result = new List<EvaluationDto>();
        foreach (var eval in evaluations)
        {
            result.Add(await MapToDtoAsync(eval));
        }
        return result;
    }

    public async Task<EvaluationDto> StartEvaluationAsync(int assignmentId, int? evaluatorId)
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
            StartedAt = DateTime.UtcNow,
            FormOpenedAt = DateTime.UtcNow
        };

        var created = await _evaluationRepository.CreateAsync(evaluation);
        return await MapToDtoAsync(created);
    }

    public async Task<EvaluationDto> StartEvaluationAsync(StartEvaluationDto dto)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(dto.AssignmentId);
        if (assignment == null)
            throw new KeyNotFoundException($"Assignment with ID {dto.AssignmentId} not found");

        // Check if evaluation already exists
        var existing = await _evaluationRepository.GetByAssignmentIdAsync(dto.AssignmentId);
        if (existing != null)
        {
            // Return existing evaluation
            return await MapToDtoAsync(existing);
        }

        var evaluation = new Evaluation
        {
            AssignmentId = dto.AssignmentId,
            AssignmentPeriodId = dto.AssignmentPeriodId,
            EvaluatorId = dto.EvaluatorId,
            Status = EvaluationStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            FormOpenedAt = DateTime.UtcNow,
            CallId = dto.CallId,
            CallDate = ToUtc(dto.CallDate),
            EvaluatedPersonnelId = dto.EvaluatedPersonnelId,
            EvaluatedUnknownPersonnel = dto.EvaluatedUnknownPersonnel
        };

        var created = await _evaluationRepository.CreateAsync(evaluation);
        return await MapToDtoAsync(created);
    }

    public async Task<EvaluationDto> SaveDraftAsync(SubmitEvaluationDto dto)
    {
        dto.SaveAsDraft = true;
        return await ProcessEvaluationAsync(dto, EvaluationStatus.Draft);
    }

    public async Task<EvaluationDto> UpdateDraftAsync(UpdateDraftDto dto)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Answers)
            .FirstOrDefaultAsync(e => e.Id == dto.EvaluationId && !e.IsDeleted);

        if (evaluation == null)
            throw new KeyNotFoundException($"Evaluation with ID {dto.EvaluationId} not found");

        if (evaluation.Status != EvaluationStatus.Draft && evaluation.Status != EvaluationStatus.InProgress)
            throw new InvalidOperationException("Only draft or in-progress evaluations can be updated");

        // Update answers
        foreach (var answerDto in dto.Answers)
        {
            var existingAnswer = evaluation.Answers.FirstOrDefault(a => a.QuestionId == answerDto.QuestionId);
            if (existingAnswer != null)
            {
                UpdateAnswerFromDto(existingAnswer, answerDto);
            }
            else
            {
                evaluation.Answers.Add(CreateAnswerFromDto(answerDto));
            }
        }

        evaluation.Notes = dto.Notes;
        evaluation.EvaluationComment = dto.EvaluationComment;
        evaluation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await MapToDtoAsync(evaluation);
    }

    public async Task<EvaluationFormDto?> GetEvaluationFormAsync(int assignmentId)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Customer)
            .Include(a => a.Checklist)
                .ThenInclude(c => c.Sections.Where(s => s.IsActive))
                    .ThenInclude(s => s.Questions.Where(q => !q.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted);

        if (assignment == null)
            return null;

        // Get existing evaluation if any
        var existingEvaluation = await _context.Evaluations
            .Include(e => e.Answers)
            .FirstOrDefaultAsync(e => e.AssignmentId == assignmentId && !e.IsDeleted);

        // Get available personnel based on checklist's customer/organization
        // Personel, checklist'te belirtilen firma ve organizasyondan seçilir
        // Süpervizör olmayan kullanıcılar listelenir
        var personnelQuery = _context.CustomerPersonnel
            .Where(cp => !cp.IsDeleted && cp.IsActive);

        // Checklist'te firma belirtilmişse o firmaya göre filtrele
        if (assignment.Checklist?.CustomerId.HasValue == true)
        {
            personnelQuery = personnelQuery.Where(cp => cp.CustomerId == assignment.Checklist.CustomerId);
        }

        // Checklist'te organizasyon belirtilmişse o organizasyona göre filtrele
        if (assignment.Checklist?.CustomerOrganizationId.HasValue == true)
        {
            personnelQuery = personnelQuery.Where(cp =>
                cp.OrganizationId == assignment.Checklist.CustomerOrganizationId ||
                cp.OrganizationAccess.Any(oa => oa.CustomerOrganizationId == assignment.Checklist.CustomerOrganizationId));
        }

        // Süpervizörleri hariç tut (CustomerSupervisor rolü)
        personnelQuery = personnelQuery.Where(cp => cp.Role != Core.Enums.CustomerPersonnelRole.CustomerSupervisor);

        var personnel = await personnelQuery
            .Select(cp => new PersonnelOptionDto
            {
                Id = cp.Id,
                Name = $"{cp.FirstName} {cp.LastName}",
                Title = cp.Title ?? cp.Role.ToString()
            })
            .OrderBy(p => p.Name)
            .ToListAsync();

        // Get available periods for this assignment (only Open periods)
        var periods = await _context.AssignmentPeriods
            .Where(p => p.AssignmentId == assignmentId && !p.IsDeleted)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new PeriodOptionDto
            {
                Id = p.Id,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status.ToString(),
                TargetCount = p.TargetCount,
                CompletedCount = p.CompletedCount
            })
            .ToListAsync();

        return new EvaluationFormDto
        {
            AssignmentId = assignmentId,
            EvaluationId = existingEvaluation?.Id,
            Status = existingEvaluation?.Status.ToString() ?? "New",
            ProjectName = assignment.Project?.Name ?? "",
            BranchName = "",
            CustomerName = assignment.Project?.Customer?.CompanyName,
            ChecklistId = assignment.ChecklistId,
            ChecklistName = assignment.Checklist?.Name ?? "",
            ChecklistType = assignment.Checklist?.ChecklistType.ToString(),
            ScoringMethod = assignment.Checklist?.ScoringMethod.ToString(),
            MaxTotalPoints = assignment.Checklist?.MaxTotalPoints ?? 100,
            EstimatedDurationMinutes = assignment.Checklist?.EstimatedDurationMinutes,
            CallId = existingEvaluation?.CallId,
            CallDate = existingEvaluation?.CallDate,
            DurationMinutes = existingEvaluation?.DurationMinutes,
            EvaluatedPersonnelId = existingEvaluation?.EvaluatedPersonnelId,
            EvaluatedUnknownPersonnel = existingEvaluation?.EvaluatedUnknownPersonnel,
            EvaluationComment = existingEvaluation?.EvaluationComment,
            AvailablePersonnel = personnel,
            SelectedPeriodId = existingEvaluation?.AssignmentPeriodId,
            AvailablePeriods = periods,
            // Sorular direkt checklist'e bağlı (Section kaldırıldı)
            Sections = new List<EvaluationSectionDto>
            {
                new EvaluationSectionDto
                {
                    Id = 0,
                    Name = "Sorular",
                    Order = 1,
                    WeightPoints = assignment.Checklist?.Questions.Sum(q => q.WeightPoints) ?? 0,
                    MaxPoints = assignment.Checklist?.MaxTotalPoints ?? 100,
                    Questions = assignment.Checklist?.Questions
                        .Where(q => !q.IsDeleted)
                        .OrderBy(q => q.Order)
                        .Select(q => new EvaluationQuestionDto
                        {
                            Id = q.Id,
                            Text = q.Text,
                            Order = q.Order,
                            IsRequired = q.IsRequired,
                            AllowNA = q.AllowNA,
                            ScoringType = q.ScoringType.ToString(),
                            WeightPoints = q.WeightPoints,
                            ScaleSteps = q.ScaleSteps,
                            PenaltyType = q.PenaltyType.ToString(),
                            PenaltyValue = q.PenaltyValue,
                            RecommendedNote = q.RecommendedNote,
                            HelpText = q.HelpText,
                            SubCriteria = q.SubCriteria?
                                .Where(sc => !sc.IsDeleted && sc.IsActive)
                                .OrderBy(sc => sc.Order)
                                .Select(sc => new EvaluationSubCriteriaDto
                                {
                                    Id = sc.Id,
                                    Description = sc.Description,
                                    Order = sc.Order,
                                    WeightPoints = sc.WeightPoints,
                                    IsActive = sc.IsActive
                                })
                                .ToList()
                        })
                        .ToList() ?? new List<EvaluationQuestionDto>()
                }
            },
            ExistingAnswers = existingEvaluation?.Answers
                .Select(a => MapAnswerToDto(a))
                .ToList() ?? new List<AnswerDto>()
        };
    }

    public async Task<EvaluationFormDto?> GetExistingEvaluationFormAsync(int evaluationId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
            .FirstOrDefaultAsync(e => e.Id == evaluationId && !e.IsDeleted);

        if (evaluation == null)
            return null;

        return await GetEvaluationFormAsync(evaluation.AssignmentId);
    }

    public async Task<EvaluationDto> SubmitEvaluationAsync(SubmitEvaluationDto dto)
    {
        return await ProcessEvaluationAsync(dto, EvaluationStatus.Completed);
    }

    private async Task<EvaluationDto> ProcessEvaluationAsync(SubmitEvaluationDto dto, EvaluationStatus targetStatus)
    {
        // Get assignment with checklist details
        var assignment = await _context.Assignments
            .Include(a => a.Checklist)
                .ThenInclude(c => c.Sections)
                    .ThenInclude(s => s.Questions)
            .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId && !a.IsDeleted);

        if (assignment == null)
            throw new KeyNotFoundException($"Assignment with ID {dto.AssignmentId} not found");

        // Get or create evaluation
        var evaluation = await _context.Evaluations
            .Include(e => e.Answers)
            .FirstOrDefaultAsync(e => e.AssignmentId == dto.AssignmentId && !e.IsDeleted);

        if (evaluation == null)
        {
            evaluation = new Evaluation
            {
                AssignmentId = dto.AssignmentId,
                AssignmentPeriodId = dto.AssignmentPeriodId,
                EvaluatorId = dto.EvaluatorId,
                Status = EvaluationStatus.InProgress,
                StartedAt = DateTime.UtcNow,
                FormOpenedAt = dto.FormOpenedAt ?? DateTime.UtcNow
            };
            _context.Evaluations.Add(evaluation);
        }
        else if (dto.AssignmentPeriodId.HasValue && !evaluation.AssignmentPeriodId.HasValue)
        {
            // Update period if not set
            evaluation.AssignmentPeriodId = dto.AssignmentPeriodId;
        }

        // Get all questions from checklist
        var allQuestions = assignment.Checklist.Sections
            .SelectMany(s => s.Questions)
            .ToList();

        // Calculate scores with penalty handling
        var scoreResult = CalculateScoreWithPenalties(allQuestions, dto.Answers);

        // Clear existing answers and add new ones
        if (evaluation.Answers.Any())
        {
            _context.Answers.RemoveRange(evaluation.Answers);
        }

        foreach (var answerDto in dto.Answers)
        {
            var question = allQuestions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
            if (question == null) continue;

            var answer = CreateAnswerFromDto(answerDto);
            answer.EvaluationId = evaluation.Id;
            answer.EarnedPoints = CalculateEarnedPoints(question, answerDto);

            // Handle penalty application
            if (answerDto.ApplyPenalty && !string.IsNullOrEmpty(answerDto.SelectedPenaltyType))
            {
                answer.IsPenaltyApplied = true;
                answer.AppliedPenaltyType = Enum.TryParse<PenaltyType>(answerDto.SelectedPenaltyType, out var pt)
                    ? pt
                    : PenaltyType.None;
            }

            evaluation.Answers.Add(answer);
        }

        // Update evaluation fields
        evaluation.Status = targetStatus;
        evaluation.TotalScore = scoreResult.TotalEarned;
        evaluation.MaxScore = scoreResult.MaxPossible;
        evaluation.ScorePercentage = scoreResult.Percentage;
        evaluation.Notes = dto.Notes;
        evaluation.EvaluationComment = dto.EvaluationComment;
        evaluation.CallId = dto.CallId;
        evaluation.CallDate = ToUtc(dto.CallDate);
        evaluation.DurationMinutes = dto.DurationMinutes;
        evaluation.EvaluatedPersonnelId = dto.EvaluatedPersonnelId;
        evaluation.EvaluatedUnknownPersonnel = dto.EvaluatedUnknownPersonnel;
        evaluation.ControlDate = ToUtc(dto.ControlDate);
        evaluation.ControlTime = dto.ControlTime;
        evaluation.YellowCardCount = scoreResult.YellowCardCount;
        evaluation.RedCardCount = scoreResult.RedCardCount;
        evaluation.UpdatedAt = DateTime.UtcNow;

        if (targetStatus == EvaluationStatus.Completed)
        {
            evaluation.CompletedAt = DateTime.UtcNow;

            // Update assignment
            assignment.IsCompleted = true;
            assignment.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return await MapToDtoAsync(evaluation);
    }

    /// <summary>
    /// Ceza kartlarını dahil eden puan hesaplama
    /// </summary>
    private (decimal TotalEarned, decimal MaxPossible, decimal Percentage, int YellowCardCount, int RedCardCount)
        CalculateScoreWithPenalties(List<Question> allQuestions, List<SubmitAnswerDto> answers)
    {
        decimal totalEarned = 0;
        decimal totalMaxPoints = 0;
        int yellowCardCount = 0;
        int redCardCount = 0;

        // N/A işaretli soruların ID'leri
        var naQuestionIds = answers.Where(a => a.IsNA).Select(a => a.QuestionId).ToHashSet();

        foreach (var question in allQuestions)
        {
            // N/A sorularını atla
            if (naQuestionIds.Contains(question.Id))
                continue;

            // Puansız soruları atla
            if (question.ScoringType == ScoringType.Unscored)
                continue;

            var answer = answers.FirstOrDefault(a => a.QuestionId == question.Id);
            if (answer == null)
                continue;

            // Cezalı sorular için
            if (question.ScoringType == ScoringType.Penalty)
            {
                if (answer.ApplyPenalty)
                {
                    if (answer.SelectedPenaltyType == "YellowCard")
                        yellowCardCount++;
                    else if (answer.SelectedPenaltyType == "RedCard")
                        redCardCount++;

                    // Ceza puanını düş
                    totalEarned -= question.PenaltyValue;
                }
            }
            else
            {
                // Normal puanlı sorular
                // Ağırlık puanı (WeightPoints) sistemi
                // Toplam max puan = ağırlık puanları toplamı
                totalMaxPoints += question.WeightPoints;
                var earnedPoints = CalculateEarnedPoints(question, answer);
                totalEarned += earnedPoints ?? 0;
            }
        }

        // Yüzde hesapla
        var percentage = totalMaxPoints > 0 ? (totalEarned / totalMaxPoints) * 100 : 0;
        percentage = Math.Max(0, Math.Round(percentage, 2)); // Negatif olamaz

        return (totalEarned, totalMaxPoints, percentage, yellowCardCount, redCardCount);
    }

    private decimal? CalculateEarnedPoints(Question question, SubmitAnswerDto answer)
    {
        if (answer.IsNA)
            return null;

        // Eğer doğrudan puan verilmişse onu kullan
        if (answer.GivenPoints.HasValue)
            return answer.GivenPoints.Value;

        // Puansız sorular için null döndür
        if (question.ScoringType == ScoringType.Unscored)
            return null;

        // Cezalı sorular ayrıca işleniyor
        if (question.ScoringType == ScoringType.Penalty)
            return 0;

        // Ağırlık puanı ve kırılım sayısı sistemi
        // Formül: (cevap / ScaleSteps) * WeightPoints
        // Örnek: ağırlık=10, kırılım=4, cevap=4 → (4/4) * 10 = 10 puan
        // Örnek: ağırlık=10, kırılım=4, cevap=3 → (3/4) * 10 = 7.5 puan
        var weight = question.WeightPoints;
        var scaleSteps = question.ScaleSteps > 0 ? question.ScaleSteps : 4;

        // ScaleSteps = 1 ise Evet/Hayır tipi: 0 veya tam puan
        if (scaleSteps == 1)
        {
            var answered = answer.AnswerNumeric > 0 ||
                answer.AnswerText?.ToLower() == "evet" ||
                answer.AnswerText?.ToLower() == "yes";
            return answered ? weight : 0;
        }

        // ScaleSteps > 1 için orantılı hesaplama
        var answerValue = answer.AnswerNumeric ?? 0;
        return (answerValue / scaleSteps) * weight;
    }

    private Answer CreateAnswerFromDto(SubmitAnswerDto dto)
    {
        return new Answer
        {
            QuestionId = dto.QuestionId,
            AnswerText = dto.AnswerText,
            AnswerNumeric = dto.AnswerNumeric,
            IsNA = dto.IsNA,
            GivenPoints = dto.GivenPoints,
            Notes = dto.Notes,
            RecommendationNotes = dto.RecommendationNotes,
            IsPenaltyApplied = dto.ApplyPenalty,
            AppliedPenaltyType = Enum.TryParse<PenaltyType>(dto.SelectedPenaltyType, out var pt)
                ? pt
                : PenaltyType.None
        };
    }

    private void UpdateAnswerFromDto(Answer answer, SubmitAnswerDto dto)
    {
        answer.AnswerText = dto.AnswerText;
        answer.AnswerNumeric = dto.AnswerNumeric;
        answer.IsNA = dto.IsNA;
        answer.GivenPoints = dto.GivenPoints;
        answer.Notes = dto.Notes;
        answer.RecommendationNotes = dto.RecommendationNotes;
        answer.IsPenaltyApplied = dto.ApplyPenalty;
        answer.AppliedPenaltyType = Enum.TryParse<PenaltyType>(dto.SelectedPenaltyType, out var pt)
            ? pt
            : PenaltyType.None;
        answer.UpdatedAt = DateTime.UtcNow;
    }

    private AnswerDto MapAnswerToDto(Answer a)
    {
        return new AnswerDto
        {
            Id = a.Id,
            QuestionId = a.QuestionId,
            QuestionText = a.Question?.Text ?? "",
            QuestionType = a.Question?.ScoringType.ToString(), // ScoringType kullanılıyor
            AnswerText = a.AnswerText,
            AnswerNumeric = a.AnswerNumeric,
            IsNA = a.IsNA,
            EarnedPoints = a.EarnedPoints,
            GivenPoints = a.GivenPoints,
            Notes = a.Notes,
            RecommendationNotes = a.RecommendationNotes,
            AttachmentFileName = a.AttachmentFileName,
            IsPenaltyApplied = a.IsPenaltyApplied,
            AppliedPenaltyType = a.AppliedPenaltyType.ToString(),
            SectionOrder = a.Question?.Section?.Order,
            SectionName = a.Question?.Section?.Name,
            QuestionOrder = a.Question?.Order,
            MaxPoints = a.Question?.WeightPoints, // WeightPoints kullanılıyor
            ScoringType = a.Question?.ScoringType.ToString(),
            PenaltyType = a.Question?.PenaltyType.ToString(),
            PenaltyValue = a.Question?.PenaltyValue,
            HelpText = a.Question?.HelpText,
            RecommendedNote = a.Question?.RecommendedNote
        };
    }

    /// <summary>
    /// Kapatılmış değerlendirmeyi taslağa al (Admin yetkisi gerektirir)
    /// </summary>
    public async Task<EvaluationDto> RevertToDraftAsync(int evaluationId, int revertedByUserId, string? reason = null)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
            .FirstOrDefaultAsync(e => e.Id == evaluationId && !e.IsDeleted);

        if (evaluation == null)
            throw new KeyNotFoundException($"Evaluation with ID {evaluationId} not found");

        if (evaluation.Status == EvaluationStatus.Draft)
            throw new InvalidOperationException("Değerlendirme zaten taslak durumunda.");

        // Eski durumu logla
        var previousStatus = evaluation.Status;

        // Durumu taslağa çevir
        evaluation.Status = EvaluationStatus.Draft;
        evaluation.CompletedAt = null;
        evaluation.UpdatedAt = DateTime.UtcNow;

        // Assignment'ı da güncelle
        if (evaluation.Assignment != null)
        {
            evaluation.Assignment.IsCompleted = false;
            evaluation.Assignment.CompletedAt = null;
        }

        // Değişiklik logunu kaydet (Notes alanına ekle)
        var logEntry = $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Taslağa alındı. Önceki durum: {previousStatus}. Neden: {reason ?? "Belirtilmedi"}";
        evaluation.Notes = (evaluation.Notes ?? "") + logEntry;

        await _context.SaveChangesAsync();
        return await MapToDtoAsync(evaluation);
    }

    /// <summary>
    /// Değerlendirmeyi iptal et
    /// </summary>
    public async Task<EvaluationDto> CancelEvaluationAsync(int evaluationId, int cancelledByUserId, string? reason = null)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
            .FirstOrDefaultAsync(e => e.Id == evaluationId && !e.IsDeleted);

        if (evaluation == null)
            throw new KeyNotFoundException($"Evaluation with ID {evaluationId} not found");

        if (evaluation.Status == EvaluationStatus.Cancelled)
            throw new InvalidOperationException("Değerlendirme zaten iptal edilmiş.");

        // Eski durumu logla
        var previousStatus = evaluation.Status;

        // Durumu iptal et
        evaluation.Status = EvaluationStatus.Cancelled;
        evaluation.UpdatedAt = DateTime.UtcNow;

        // Değişiklik logunu kaydet
        var logEntry = $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] İptal edildi. Önceki durum: {previousStatus}. Neden: {reason ?? "Belirtilmedi"}";
        evaluation.Notes = (evaluation.Notes ?? "") + logEntry;

        await _context.SaveChangesAsync();
        return await MapToDtoAsync(evaluation);
    }

    private async Task<EvaluationDto> MapToDtoAsync(Evaluation evaluation)
    {
        // Load related data if not loaded
        if (evaluation.Assignment == null)
        {
            await _context.Entry(evaluation)
                .Reference(e => e.Assignment)
                .Query()
                .Include(a => a.Project)
                .Include(a => a.Checklist)
                .LoadAsync();
        }

        string? evaluatedPersonnelName = null;
        if (evaluation.EvaluatedPersonnelId.HasValue)
        {
            var personnel = await _context.Users
                .Where(u => u.Id == evaluation.EvaluatedPersonnelId.Value)
                .Select(u => $"{u.FirstName} {u.LastName}")
                .FirstOrDefaultAsync();
            evaluatedPersonnelName = personnel;
        }

        // Load period name if exists
        string? periodName = null;
        if (evaluation.AssignmentPeriodId.HasValue)
        {
            periodName = await _context.AssignmentPeriods
                .Where(p => p.Id == evaluation.AssignmentPeriodId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();
        }

        return new EvaluationDto
        {
            Id = evaluation.Id,
            AssignmentId = evaluation.AssignmentId,
            AssignmentPeriodId = evaluation.AssignmentPeriodId,
            AssignmentPeriodName = periodName,
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
            EvaluationComment = evaluation.EvaluationComment,
            CallId = evaluation.CallId,
            CallDate = evaluation.CallDate,
            DurationMinutes = evaluation.DurationMinutes,
            EvaluatedPersonnelId = evaluation.EvaluatedPersonnelId,
            EvaluatedPersonnelName = evaluatedPersonnelName,
            EvaluatedUnknownPersonnel = evaluation.EvaluatedUnknownPersonnel,
            YellowCardCount = evaluation.YellowCardCount,
            RedCardCount = evaluation.RedCardCount,
            FormOpenedAt = evaluation.FormOpenedAt,
            ControlDate = evaluation.ControlDate,
            ControlTime = evaluation.ControlTime,
            ProjectName = evaluation.Assignment?.Project?.Name,
            BranchName = null,
            ChecklistName = evaluation.Assignment?.Checklist?.Name,
            Answers = evaluation.Answers.Select(a => MapAnswerToDto(a)).ToList()
        };
    }
}
