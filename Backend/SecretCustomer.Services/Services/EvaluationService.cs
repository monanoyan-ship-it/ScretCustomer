using System.Text.Json;
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
    private readonly ILocalizationService _localizationService;

    public EvaluationService(
        IEvaluationRepository evaluationRepository,
        IAssignmentRepository assignmentRepository,
        ApplicationDbContext context,
        ILocalizationService localizationService)
    {
        _evaluationRepository = evaluationRepository;
        _assignmentRepository = assignmentRepository;
        _context = context;
        _localizationService = localizationService;
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

    // Helper: Descriptions listesini JSON'a serialize et (boş olanları filtrele)
    private static string? SerializeDescriptions(List<string>? descriptions)
    {
        if (descriptions == null || descriptions.Count == 0) return null;
        // Boş olanları filtrele
        var filtered = descriptions.Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
        if (filtered.Count == 0) return null;
        return JsonSerializer.Serialize(filtered);
    }

    // Helper: JSON'dan Descriptions listesini deserialize et
    private static List<string> DeserializeDescriptions(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
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

    /// <summary>
    /// CustomerPersonnel kullanıcısının değerlendirmelerini getirir
    /// </summary>
    public async Task<IEnumerable<EvaluationDto>> GetByEvaluatorCustomerPersonnelIdAsync(int customerPersonnelId)
    {
        var evaluations = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatorCustomerPersonnel)
            .Where(e => e.EvaluatorCustomerPersonnelId == customerPersonnelId && !e.IsDeleted)
            .OrderByDescending(e => e.CompletedAt ?? e.CreatedAt)
            .ToListAsync();

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
            .Include(e => e.EvaluatorCustomerPersonnel)
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
            .Include(e => e.EvaluatorCustomerPersonnel)
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
            StatusId = EvaluationStatuses.Ids.InProgress,
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
            // User mı CustomerPersonnel mı olduğunu ayır
            EvaluatorId = dto.EvaluatorId > 0 ? dto.EvaluatorId : null,
            EvaluatorCustomerPersonnelId = dto.EvaluatorCustomerPersonnelId > 0 ? dto.EvaluatorCustomerPersonnelId : null,
            StatusId = EvaluationStatuses.Ids.InProgress,
            StartedAt = DateTime.UtcNow,
            FormOpenedAt = DateTime.UtcNow,
            CallId = dto.CallId,
            CallDate = ToUtc(dto.CallDate),
            CallTime = dto.CallTime,
            // Frontend'den gelen evaluatedPersonnelId aslında CustomerPersonnel ID'si
            EvaluatedCustomerPersonnelId = dto.EvaluatedPersonnelId > 0 ? dto.EvaluatedPersonnelId : null,
            EvaluatedUnknownPersonnel = dto.EvaluatedUnknownPersonnel
        };

        var created = await _evaluationRepository.CreateAsync(evaluation);
        return await MapToDtoAsync(created);
    }

    public async Task<EvaluationDto> SaveDraftAsync(SubmitEvaluationDto dto)
    {
        dto.SaveAsDraft = true;
        return await ProcessEvaluationAsync(dto, EvaluationStatuses.Ids.Draft);
    }

    public async Task<EvaluationDto> UpdateDraftAsync(UpdateDraftDto dto)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Answers)
            .FirstOrDefaultAsync(e => e.Id == dto.EvaluationId && !e.IsDeleted);

        if (evaluation == null)
            throw new KeyNotFoundException($"Evaluation with ID {dto.EvaluationId} not found");

        if (evaluation.StatusId != EvaluationStatuses.Ids.Draft && evaluation.StatusId != EvaluationStatuses.Ids.InProgress)
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
            .Include(a => a.Project)
                .ThenInclude(p => p.Organization)
            .Include(a => a.Checklist)
                .ThenInclude(c => c.CustomerOrganization)
            .Include(a => a.Checklist)
                .ThenInclude(c => c.Questions.Where(q => !q.IsDeleted))
                    .ThenInclude(q => q.SubCriteria.Where(sc => !sc.IsDeleted && sc.IsActive))
            .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted);

        if (assignment == null)
            return null;

        // Not: Her "Ekle" dediğinde yeni evaluation oluşturulacak
        // Mevcut evaluationlar sadece düzenleme endpoint'inden yüklenir
        // Bu fonksiyon her zaman boş form döndürür

        // Organizasyon önce Project'ten, yoksa Checklist'ten geliyor
        var organizations = new List<OrganizationOptionDto>();
        // Öncelik: Project.OrganizationId > Checklist.CustomerOrganizationId
        int? selectedOrganizationId = assignment.Project?.OrganizationId ?? assignment.Checklist?.CustomerOrganizationId;

        // Organizasyon seçiliyse sadece o organizasyonu göster
        if (selectedOrganizationId.HasValue)
        {
            var org = await _context.CustomerOrganizations
                .Where(co => co.Id == selectedOrganizationId.Value && !co.IsDeleted)
                .Select(co => new OrganizationOptionDto
                {
                    Id = co.Id,
                    Name = co.Name,
                    Code = co.Code,
                    Level = co.Level,
                    PersonnelCount = co.PersonnelAssignments.Count(pa => !pa.CustomerPersonnel.IsDeleted && pa.CustomerPersonnel.IsActive)
                })
                .FirstOrDefaultAsync();
            if (org != null)
                organizations.Add(org);
        }
        // Organizasyon seçilmemişse checklist'in customer'ına göre tüm organizasyonları göster
        else if (assignment.Checklist?.CustomerId.HasValue == true)
        {
            organizations = await _context.CustomerOrganizations
                .Where(co => !co.IsDeleted && co.IsActive && co.CustomerId == assignment.Checklist.CustomerId)
                .Select(co => new OrganizationOptionDto
                {
                    Id = co.Id,
                    Name = co.Name,
                    Code = co.Code,
                    Level = co.Level,
                    PersonnelCount = co.PersonnelAssignments.Count(pa => !pa.CustomerPersonnel.IsDeleted && pa.CustomerPersonnel.IsActive)
                })
                .OrderBy(o => o.Level)
                .ThenBy(o => o.Name)
                .ToListAsync();
        }

        // Personel listesi - organizasyon seçiliyse o organizasyonun personeli, yoksa tüm firma personeli
        var personnel = new List<PersonnelOptionDto>();
        if (selectedOrganizationId.HasValue)
        {
            personnel = await GetPersonnelByOrganizationAsync(selectedOrganizationId.Value);
        }
        else
        {
            // Organizasyon seçilmemişse (proje tüm firmayı kapsıyorsa) müşterinin tüm personelini getir
            var customerId = assignment.Project?.CustomerId ?? assignment.Checklist?.CustomerId;
            if (customerId.HasValue)
            {
                personnel = await GetPersonnelByCustomerAsync(customerId.Value);
            }
        }

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
                Status = p.StatusId == PeriodStatuses.Ids.Open ? "Open" : "Closed",
                TargetCount = p.TargetCount,
                CompletedCount = p.CompletedCount
            })
            .ToListAsync();

        return new EvaluationFormDto
        {
            AssignmentId = assignmentId,
            EvaluationId = null, // Yeni evaluation oluşturulacak
            Status = "New",
            ProjectName = assignment.Project?.Name ?? "",
            BranchName = "",
            CustomerName = assignment.Project?.Customer?.CompanyName,
            ChecklistId = assignment.ChecklistId,
            ChecklistName = assignment.Checklist?.Name ?? "",
            ChecklistType = assignment.Checklist != null ? ChecklistTypes.GetById(assignment.Checklist.ChecklistTypeId)?.SystemName : null,
            ScoringMethod = assignment.Checklist?.ScoringMethodId.ToString(),
            MaxTotalPoints = assignment.Checklist?.MaxTotalPoints ?? 100,
            CallId = null,
            CallDate = null,
            CallTime = null,
            Duration = null,
            Descriptions = new List<string>(),
            EvaluatedPersonnelId = null,
            EvaluatedUnknownPersonnel = null,
            EvaluationComment = null,
            AvailableOrganizations = organizations,
            SelectedOrganizationId = selectedOrganizationId,
            AvailablePersonnel = personnel,
            SelectedPeriodId = null,
            AvailablePeriods = periods,
            // Soruları GroupName'e göre grupla
            Sections = BuildSectionsFromQuestions(assignment.Checklist?.Questions.Where(q => !q.IsDeleted).ToList()),
            ExistingAnswers = new List<AnswerDto>() // Yeni form, cevap yok
        };
    }

    public async Task<EvaluationFormDto?> GetExistingEvaluationFormAsync(int evaluationId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
                    .ThenInclude(p => p.Customer)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
                    .ThenInclude(p => p.Organization)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
                    .ThenInclude(c => c.CustomerOrganization)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
                    .ThenInclude(c => c.Questions.Where(q => !q.IsDeleted))
                        .ThenInclude(q => q.SubCriteria.Where(sc => !sc.IsDeleted && sc.IsActive))
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .FirstOrDefaultAsync(e => e.Id == evaluationId && !e.IsDeleted);

        if (evaluation == null)
            return null;

        var assignment = evaluation.Assignment;
        if (assignment == null)
            return null;

        // Organizasyon - Öncelik: Evaluation > Project > Checklist
        int? selectedOrganizationId = evaluation.EvaluatedOrganizationId
            ?? assignment.Project?.OrganizationId
            ?? assignment.Checklist?.CustomerOrganizationId;

        var personnel = new List<PersonnelOptionDto>();
        if (selectedOrganizationId.HasValue)
        {
            personnel = await GetPersonnelByOrganizationAsync(selectedOrganizationId.Value);
        }
        else
        {
            // Organizasyon seçilmemişse tüm firma personelini getir
            var customerId = assignment.Project?.CustomerId ?? assignment.Checklist?.CustomerId;
            if (customerId.HasValue)
            {
                personnel = await GetPersonnelByCustomerAsync(customerId.Value);
            }
        }

        // Dönemler
        var periods = await _context.AssignmentPeriods
            .Where(p => p.AssignmentId == assignment.Id && !p.IsDeleted)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new PeriodOptionDto
            {
                Id = p.Id,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.StatusId == PeriodStatuses.Ids.Open ? "Open" : "Closed",
                TargetCount = p.TargetCount,
                CompletedCount = p.CompletedCount
            })
            .ToListAsync();

        return new EvaluationFormDto
        {
            AssignmentId = assignment.Id,
            EvaluationId = evaluation.Id,
            Status = EvaluationStatuses.GetById(evaluation.StatusId)?.SystemName ?? "",
            ProjectName = assignment.Project?.Name ?? "",
            CustomerName = assignment.Project?.Customer?.CompanyName,
            ChecklistId = assignment.ChecklistId,
            ChecklistName = assignment.Checklist?.Name ?? "",
            ChecklistType = assignment.Checklist != null ? ChecklistTypes.GetById(assignment.Checklist.ChecklistTypeId)?.SystemName : null,
            ScoringMethod = assignment.Checklist?.ScoringMethodId.ToString(),
            MaxTotalPoints = assignment.Checklist?.MaxTotalPoints ?? 100,
            CallId = evaluation.CallId,
            CallDate = evaluation.CallDate,
            CallTime = evaluation.CallTime,
            Duration = evaluation.Duration,
            Descriptions = DeserializeDescriptions(evaluation.DescriptionsJson),
            // Frontend'e CustomerPersonnel ID'sini evaluatedPersonnelId olarak gönder
            EvaluatedPersonnelId = evaluation.EvaluatedCustomerPersonnelId,
            EvaluatedUnknownPersonnel = evaluation.EvaluatedUnknownPersonnel,
            EvaluationComment = evaluation.EvaluationComment,
            SelectedOrganizationId = selectedOrganizationId,
            AvailablePersonnel = personnel,
            SelectedPeriodId = evaluation.AssignmentPeriodId,
            AvailablePeriods = periods,
            // Soruları GroupName'e göre grupla
            Sections = BuildSectionsFromQuestions(assignment.Checklist?.Questions.Where(q => !q.IsDeleted).ToList()),
            ExistingAnswers = evaluation.Answers
                .Select(a => MapAnswerToDto(a))
                .ToList()
        };
    }

    public async Task<EvaluationDto> SubmitEvaluationAsync(SubmitEvaluationDto dto)
    {
        return await ProcessEvaluationAsync(dto, EvaluationStatuses.Ids.Completed);
    }

    private async Task<EvaluationDto> ProcessEvaluationAsync(SubmitEvaluationDto dto, int targetStatusId)
    {
        // Get assignment with checklist details (Sections kaldırıldı, Questions direkt Checklist'e bağlı)
        var assignment = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
                .ThenInclude(c => c.Questions)
            .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId && !a.IsDeleted);

        if (assignment == null)
            throw new KeyNotFoundException($"Assignment with ID {dto.AssignmentId} not found");

        // CallId tekrar kontrolü - aynı müşteriye ait başka bir dinlemede aynı CallId varsa hata ver
        if (!string.IsNullOrWhiteSpace(dto.CallId) && assignment.Project?.CustomerId != null)
        {
            var customerId = assignment.Project.CustomerId.Value;
            var duplicateExists = await _context.Evaluations
                .AnyAsync(e => !e.IsDeleted &&
                              e.CallId == dto.CallId &&
                              e.Assignment.Project.CustomerId == customerId &&
                              (!dto.EvaluationId.HasValue || e.Id != dto.EvaluationId.Value));

            if (duplicateExists)
                throw new InvalidOperationException($"Bu Çağrı ID ({dto.CallId}) daha önce kaydedilmiş. Aynı Çağrı ID ile yeni dinleme eklenemez.");
        }

        Evaluation? evaluation = null;

        // EvaluationId varsa onu kullan (taslak güncelleme), yoksa yeni oluştur
        if (dto.EvaluationId.HasValue && dto.EvaluationId.Value > 0)
        {
            evaluation = await _context.Evaluations
                .Include(e => e.Answers)
                    .ThenInclude(a => a.SubCriteriaSelections)
                .FirstOrDefaultAsync(e => e.Id == dto.EvaluationId.Value && !e.IsDeleted);
        }

        if (evaluation == null)
        {
            evaluation = new Evaluation
            {
                AssignmentId = dto.AssignmentId,
                AssignmentPeriodId = dto.AssignmentPeriodId,
                // User mı CustomerPersonnel mı olduğunu ayır
                EvaluatorId = dto.EvaluatorId > 0 ? dto.EvaluatorId : null,
                EvaluatorCustomerPersonnelId = dto.EvaluatorCustomerPersonnelId > 0 ? dto.EvaluatorCustomerPersonnelId : null,
                StatusId = EvaluationStatuses.Ids.InProgress,
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

        // Get all questions from checklist (Questions direkt Checklist'e bağlı)
        var allQuestions = assignment.Checklist.Questions
            .Where(q => !q.IsDeleted)
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
                answer.AppliedPenaltyTypeId = PenaltyTypes.GetBySystemName(answerDto.SelectedPenaltyType)?.Id
                    ?? PenaltyTypes.Ids.None;
            }

            // Alt kriter seçimlerini ekle
            if (answerDto.SelectedSubCriteriaIds?.Any() == true)
            {
                foreach (var subCriteriaId in answerDto.SelectedSubCriteriaIds)
                {
                    answer.SubCriteriaSelections.Add(new AnswerSubCriteriaSelection
                    {
                        SubCriteriaId = subCriteriaId,
                        SelectedAt = DateTime.UtcNow
                    });
                }
            }

            evaluation.Answers.Add(answer);
        }

        // Update evaluation fields
        evaluation.StatusId = targetStatusId;
        evaluation.TotalScore = scoreResult.TotalEarned;
        evaluation.MaxScore = scoreResult.MaxPossible;
        evaluation.ScorePercentage = scoreResult.Percentage;
        evaluation.Notes = dto.Notes;
        evaluation.EvaluationComment = dto.EvaluationComment;
        evaluation.CallId = dto.CallId;
        evaluation.CallDate = ToUtc(dto.CallDate);
        evaluation.CallTime = dto.CallTime;
        evaluation.Duration = dto.Duration;
        evaluation.DescriptionsJson = SerializeDescriptions(dto.Descriptions);
        // 0 değerlerini null'a çevir (FK hatası önleme)
        evaluation.EvaluatedOrganizationId = dto.EvaluatedOrganizationId > 0 ? dto.EvaluatedOrganizationId : null;
        // Frontend'den gelen evaluatedPersonnelId aslında CustomerPersonnel ID'si
        // EvaluatedPersonnelId (User) şimdilik kullanılmıyor
        evaluation.EvaluatedPersonnelId = null;
        evaluation.EvaluatedCustomerPersonnelId = (dto.EvaluatedCustomerPersonnelId ?? dto.EvaluatedPersonnelId) > 0
            ? (dto.EvaluatedCustomerPersonnelId ?? dto.EvaluatedPersonnelId)
            : null;
        evaluation.EvaluatedUnknownPersonnel = dto.EvaluatedUnknownPersonnel;
        evaluation.ControlDate = ToUtc(dto.ControlDate);
        evaluation.ControlTime = dto.ControlTime;
        evaluation.YellowCardCount = scoreResult.YellowCardCount;
        evaluation.RedCardCount = scoreResult.RedCardCount;
        evaluation.UpdatedAt = DateTime.UtcNow;

        if (targetStatusId == EvaluationStatuses.Ids.Completed)
        {
            evaluation.CompletedAt = DateTime.UtcNow;
            // Not: Assignment tamamlandı olarak işaretlenmiyor
            // Aynı atamaya sınırsız dinleme (evaluation) eklenebilir
        }

        await _context.SaveChangesAsync();

        // Yeni personel talebi oluştur (Listede Yok seçilmişse - taslak dahil her durumda)
        string? personnelRequestWarning = null;
        if (dto.NewPersonnel != null &&
            !string.IsNullOrWhiteSpace(dto.NewPersonnel.FirstName) &&
            !string.IsNullOrWhiteSpace(dto.NewPersonnel.LastName) &&
            assignment.Project?.CustomerId != null)
        {
            personnelRequestWarning = await CreatePersonnelRequestAsync(evaluation, dto, assignment.Project.CustomerId.Value);
        }

        var result = await MapToDtoAsync(evaluation);

        // Warning varsa ekle
        if (!string.IsNullOrEmpty(personnelRequestWarning))
        {
            result.Warnings.Add(personnelRequestWarning);
        }

        return result;
    }

    /// <summary>
    /// Submit/Draft için puan hesaplama wrapper metodu
    /// Asıl hesaplama CalculateScoreCore'da yapılır
    /// </summary>
    private (decimal TotalEarned, decimal MaxPossible, decimal Percentage, int YellowCardCount, int RedCardCount)
        CalculateScoreWithPenalties(List<Question> allQuestions, List<SubmitAnswerDto> answers)
    {
        // SubmitAnswerDto -> ScoreAnswerDto dönüşümü
        var scoreAnswers = answers.Select(a => new ScoreAnswerDto
        {
            QuestionId = a.QuestionId,
            IsIncluded = a.IsIncluded, // Frontend'den gelen değeri kullan
            IsNA = a.IsNA,
            GivenPoints = a.GivenPoints,
            ApplyPenalty = a.ApplyPenalty,
            SelectedPenaltyType = a.SelectedPenaltyType
        }).ToList();

        var result = CalculateScoreCore(allQuestions, scoreAnswers);
        return (result.TotalEarned, result.MaxPossible, result.Percentage, result.YellowCardCount, result.RedCardCount);
    }

    /// <summary>
    /// TEK HESAPLAMA NOKTASI - Tüm puan hesaplamaları buradan geçer
    ///
    /// Hesaplama Mantığı:
    /// - Penalty sorular: Her zaman opsiyonel, sadece ApplyPenalty=true ise ceza uygulanır, ağırlığa dahil edilmez
    /// - Unscored sorular: Puan hesabına katılmaz
    /// - Scored sorular:
    ///   - IsRequired=true: Her zaman dahil (cevap zorunlu)
    ///   - IsRequired=false: IsIncluded=true ise dahil, false ise atla
    /// </summary>
    private ScoreCalculationResultDto CalculateScoreCore(List<Question> questions, List<ScoreAnswerDto> answers)
    {
        decimal totalEarned = 0;
        decimal totalMaxPoints = 0;
        int yellowCardCount = 0;
        int redCardCount = 0;
        int includedQuestionCount = 0;

        // Cevapları dictionary'e çevir (hızlı erişim için)
        var answerDict = answers.ToDictionary(a => a.QuestionId, a => a);

        foreach (var question in questions)
        {
            var answer = answerDict.GetValueOrDefault(question.Id);

            // 1. Puansız soruları atla
            if (question.ScoringTypeId == ScoringTypes.Ids.Unscored)
                continue;

            // 2. Cezalı sorular - her zaman opsiyonel, sadece ceza uygulanırsa işle
            if (question.ScoringTypeId == ScoringTypes.Ids.Penalty)
            {
                if (answer != null && answer.ApplyPenalty)
                {
                    if (answer.SelectedPenaltyType == "YellowCard")
                        yellowCardCount++;
                    else if (answer.SelectedPenaltyType == "RedCard")
                        redCardCount++;

                    // Ceza puanını düş (ağırlık puanı kadar)
                    totalEarned -= question.WeightPoints;
                }
                // Penalty sorular ağırlığa dahil edilmez
                continue;
            }

            // 3. Normal puanlı sorular (Scored)
            // N/A işaretli mi?
            if (answer != null && answer.IsNA)
                continue;

            // Zorunlu olmayan soru ve dahil edilmemiş → atla
            if (!question.IsRequired && (answer == null || !answer.IsIncluded))
                continue;

            // Bu soru hesaplamaya dahil
            includedQuestionCount++;
            totalMaxPoints += question.WeightPoints;

            // Cevap varsa puanı hesapla
            if (answer != null && answer.GivenPoints.HasValue)
            {
                // Formül: (cevap / MaxPoints) * WeightPoints
                var maxPoints = question.MaxPoints > 0 ? question.MaxPoints : 5;
                var earnedPoints = (answer.GivenPoints.Value / maxPoints) * question.WeightPoints;
                totalEarned += earnedPoints;
            }
            // Cevap yoksa (zorunlu soru ama cevap verilmemiş) → 0 puan
        }

        // Yüzde hesapla
        var percentage = totalMaxPoints > 0 ? (totalEarned / totalMaxPoints) * 100 : 0;
        percentage = Math.Max(0, Math.Round(percentage, 2)); // Negatif olamaz

        return new ScoreCalculationResultDto
        {
            TotalEarned = Math.Round(totalEarned, 2),
            MaxPossible = Math.Round(totalMaxPoints, 2),
            Percentage = percentage,
            YellowCardCount = yellowCardCount,
            RedCardCount = redCardCount,
            IncludedQuestionCount = includedQuestionCount,
            TotalQuestionCount = questions.Count(q => q.ScoringTypeId == ScoringTypes.Ids.Scored)
        };
    }

    private decimal? CalculateEarnedPoints(Question question, SubmitAnswerDto answer)
    {
        if (answer.IsNA)
            return null;

        // Eğer doğrudan puan verilmişse onu kullan
        if (answer.GivenPoints.HasValue)
            return answer.GivenPoints.Value;

        // Puansız sorular için null döndür
        if (question.ScoringTypeId == ScoringTypes.Ids.Unscored)
            return null;

        // Cezalı sorular ayrıca işleniyor
        if (question.ScoringTypeId == ScoringTypes.Ids.Penalty)
            return 0;

        // Ağırlık puanı ve maksimum puan sistemi
        // Formül: (cevap / MaxPoints) * WeightPoints
        // Örnek: ağırlık=10, max=5, cevap=5 → (5/5) * 10 = 10 puan
        // Örnek: ağırlık=10, max=5, cevap=3 → (3/5) * 10 = 6 puan
        var weight = question.WeightPoints;
        var maxPoints = question.MaxPoints > 0 ? question.MaxPoints : 5;

        // MaxPoints = 1 ise Evet/Hayır tipi: 0 veya tam puan
        if (maxPoints == 1)
        {
            var answered = answer.AnswerNumeric > 0 ||
                answer.AnswerText?.ToLower() == "evet" ||
                answer.AnswerText?.ToLower() == "yes";
            return answered ? weight : 0;
        }

        // MaxPoints > 1 için orantılı hesaplama
        var answerValue = answer.AnswerNumeric ?? 0;
        return (answerValue / maxPoints) * weight;
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
            AppliedPenaltyTypeId = PenaltyTypes.GetBySystemName(dto.SelectedPenaltyType)?.Id
                ?? PenaltyTypes.Ids.None
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
        answer.AppliedPenaltyTypeId = PenaltyTypes.GetBySystemName(dto.SelectedPenaltyType)?.Id
            ?? PenaltyTypes.Ids.None;
        answer.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soruları PenaltyType'a göre grupla: Sorular, Sarı Kartlar, Kırmızı Kartlar
    /// </summary>
    private List<EvaluationSectionDto> BuildSectionsFromQuestions(List<Question>? questions)
    {
        if (questions == null || !questions.Any())
            return new List<EvaluationSectionDto>();

        var result = new List<EvaluationSectionDto>();
        var order = 1;

        // 1. Normal Sorular (PenaltyType = None)
        var normalQuestions = questions.Where(q => q.PenaltyTypeId == PenaltyTypes.Ids.None).OrderBy(q => q.Order).ToList();
        if (normalQuestions.Any())
        {
            result.Add(CreateQuestionGroup(normalQuestions, "Sorular", order++));
        }

        // 2. Sarı Kartlar
        var yellowCards = questions.Where(q => q.PenaltyTypeId == PenaltyTypes.Ids.YellowCard).OrderBy(q => q.Order).ToList();
        if (yellowCards.Any())
        {
            result.Add(CreateQuestionGroup(yellowCards, "Sarı Kartlar", order++));
        }

        // 3. Kırmızı Kartlar
        var redCards = questions.Where(q => q.PenaltyTypeId == PenaltyTypes.Ids.RedCard).OrderBy(q => q.Order).ToList();
        if (redCards.Any())
        {
            result.Add(CreateQuestionGroup(redCards, "Kırmızı Kartlar", order++));
        }

        return result;
    }

    private EvaluationSectionDto CreateQuestionGroup(List<Question> questions, string name, int order)
    {
        return new EvaluationSectionDto
        {
            Id = order,
            Name = name,
            Order = order,
            WeightPoints = questions.Sum(q => q.WeightPoints),
            MaxPoints = questions.Sum(q => q.MaxPoints),
            Questions = questions.Select(q => new EvaluationQuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                Order = q.Order,
                IsRequired = q.IsRequired,
                AllowNA = q.AllowNA,
                ScoringType = ScoringTypes.GetById(q.ScoringTypeId)?.SystemName ?? "Scored",
                WeightPoints = q.WeightPoints,
                MaxPoints = q.MaxPoints,
                PenaltyType = PenaltyTypes.GetById(q.PenaltyTypeId)?.SystemName ?? "None",
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
            }).ToList()
        };
    }

    private AnswerDto MapAnswerToDto(Answer a)
    {
        return new AnswerDto
        {
            Id = a.Id,
            QuestionId = a.QuestionId,
            QuestionText = a.Question?.Text ?? "",
            QuestionType = a.Question != null ? ScoringTypes.GetById(a.Question.ScoringTypeId)?.SystemName : null,
            AnswerText = a.AnswerText,
            AnswerNumeric = a.AnswerNumeric,
            IsNA = a.IsNA,
            EarnedPoints = a.EarnedPoints,
            GivenPoints = a.GivenPoints,
            Notes = a.Notes,
            RecommendationNotes = a.RecommendationNotes,
            AttachmentFileName = a.AttachmentFileName,
            IsPenaltyApplied = a.IsPenaltyApplied,
            AppliedPenaltyType = PenaltyTypes.GetById(a.AppliedPenaltyTypeId)?.SystemName ?? "None",
            SectionOrder = null, // Section kaldırıldı
            SectionName = null, // Section kaldırıldı
            GroupName = a.Question?.GroupName,
            QuestionOrder = a.Question?.Order,
            QuestionMaxPoints = a.Question?.MaxPoints ?? 5, // Sorunun max puanı (örn: 5)
            WeightPoints = a.Question?.WeightPoints ?? 0, // Ağırlık puanı
            MaxPoints = a.Question?.WeightPoints, // Geriye uyumluluk için
            ScoringType = a.Question != null ? ScoringTypes.GetById(a.Question.ScoringTypeId)?.SystemName : null,
            PenaltyType = a.Question != null ? PenaltyTypes.GetById(a.Question.PenaltyTypeId)?.SystemName : null,
            HelpText = a.Question?.HelpText,
            RecommendedNote = a.Question?.RecommendedNote,
            SelectedSubCriteriaIds = a.SubCriteriaSelections?.Select(s => s.SubCriteriaId).ToList(),
            SelectedSubCriteria = a.SubCriteriaSelections?.Select(s => s.SubCriteria?.Description ?? "").Where(d => !string.IsNullOrEmpty(d)).ToList()
        };
    }

    /// <summary>
    /// Kapatılmış değerlendirmeyi taslağa al (Admin yetkisi gerektirir)
    /// </summary>
    public async Task<EvaluationDto> RevertToDraftAsync(int evaluationId, int revertedByUserId, string? reason = null)
    {
        // Assignment include etmiyoruz çünkü silinmiş olabilir
        var evaluation = await _context.Evaluations
            .FirstOrDefaultAsync(e => e.Id == evaluationId && !e.IsDeleted);

        if (evaluation == null)
            throw new KeyNotFoundException($"Evaluation with ID {evaluationId} not found");

        if (evaluation.StatusId == EvaluationStatuses.Ids.Draft)
            throw new InvalidOperationException("Değerlendirme zaten taslak durumunda.");

        // Eski durumu logla
        var previousStatus = EvaluationStatuses.GetById(evaluation.StatusId)?.SystemName ?? "Unknown";

        // Durumu taslağa çevir
        evaluation.StatusId = EvaluationStatuses.Ids.Draft;
        evaluation.CompletedAt = null;
        evaluation.UpdatedAt = DateTime.UtcNow;

        // Not: Assignment artık tamamlandı olarak işaretlenmediği için
        // reopen'da da güncellemesine gerek yok

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

        if (evaluation.StatusId == EvaluationStatuses.Ids.Cancelled)
            throw new InvalidOperationException("Değerlendirme zaten iptal edilmiş.");

        // Eski durumu logla
        var previousStatusName = EvaluationStatuses.GetById(evaluation.StatusId)?.SystemName ?? "Unknown";

        // Durumu iptal et
        evaluation.StatusId = EvaluationStatuses.Ids.Cancelled;
        evaluation.UpdatedAt = DateTime.UtcNow;

        // Değişiklik logunu kaydet
        var logEntry = $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] İptal edildi. Önceki durum: {previousStatusName}. Neden: {reason ?? "Belirtilmedi"}";
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
        string? customerName = null;
        string? organizationName = null;
        string? supervisorName = null;

        // CustomerPersonnel tablosundan bilgileri çek (EvaluatedCustomerPersonnelId kullanılıyor)
        if (evaluation.EvaluatedCustomerPersonnelId.HasValue)
        {
            var personnelData = await _context.CustomerPersonnel
                .Where(cp => cp.Id == evaluation.EvaluatedCustomerPersonnelId.Value)
                .Include(cp => cp.Customer)
                .Include(cp => cp.OrganizationAssignments)
                    .ThenInclude(oa => oa.CustomerOrganization)
                .Include(cp => cp.OrganizationAssignments)
                    .ThenInclude(oa => oa.Supervisor)
                .FirstOrDefaultAsync();

            if (personnelData != null)
            {
                evaluatedPersonnelName = $"{personnelData.FirstName} {personnelData.LastName}";
                customerName = personnelData.Customer?.CompanyName;
                organizationName = personnelData.OrganizationAssignments != null
                    ? string.Join(", ", personnelData.OrganizationAssignments
                        .Where(oa => oa.CustomerOrganization != null)
                        .Select(oa => oa.CustomerOrganization!.Name))
                    : null;
                supervisorName = personnelData.OrganizationAssignments != null
                    ? string.Join(", ", personnelData.OrganizationAssignments
                        .Where(oa => oa.Supervisor != null)
                        .Select(oa => $"{oa.Supervisor!.FirstName} {oa.Supervisor.LastName}")
                        .Distinct())
                    : null;
            }
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
            EvaluatorName = evaluation.EvaluatorCustomerPersonnel != null
                ? $"{evaluation.EvaluatorCustomerPersonnel.FirstName} {evaluation.EvaluatorCustomerPersonnel.LastName}"
                : (evaluation.Evaluator != null
                    ? $"{evaluation.Evaluator.FirstName} {evaluation.Evaluator.LastName}"
                    : null),
            Status = EvaluationStatuses.GetById(evaluation.StatusId)?.SystemName ?? "",
            TotalScore = evaluation.TotalScore,
            MaxScore = evaluation.MaxScore,
            ScorePercentage = evaluation.ScorePercentage,
            StartedAt = evaluation.StartedAt,
            CompletedAt = evaluation.CompletedAt,
            Notes = evaluation.Notes,
            EvaluationComment = evaluation.EvaluationComment,
            CallId = evaluation.CallId,
            CallDate = evaluation.CallDate,
            CallTime = evaluation.CallTime,
            Duration = evaluation.Duration,
            Descriptions = DeserializeDescriptions(evaluation.DescriptionsJson),
            // Frontend'e CustomerPersonnel ID'sini evaluatedPersonnelId olarak gönder
            EvaluatedPersonnelId = evaluation.EvaluatedCustomerPersonnelId,
            EvaluatedPersonnelName = evaluatedPersonnelName,
            EvaluatedUnknownPersonnel = evaluation.EvaluatedUnknownPersonnel,
            CustomerName = customerName,
            OrganizationName = organizationName,
            SupervisorName = supervisorName,
            YellowCardCount = evaluation.YellowCardCount,
            RedCardCount = evaluation.RedCardCount,
            FormOpenedAt = evaluation.FormOpenedAt,
            ControlDate = evaluation.ControlDate,
            ControlTime = evaluation.ControlTime,
            ProjectName = evaluation.Assignment?.Project?.Name,
            ChecklistName = evaluation.Assignment?.Checklist?.Name,
            AssigneeName = evaluation.Assignment?.AssignedUser != null
                ? $"{evaluation.Assignment.AssignedUser.FirstName} {evaluation.Assignment.AssignedUser.LastName}"
                : null,
            Answers = evaluation.Answers.Select(a => MapAnswerToDto(a)).ToList()
        };
    }

    /// <summary>
    /// Organizasyona göre personel listesi getir (sadece junction table'dan)
    /// </summary>
    public async Task<List<PersonnelOptionDto>> GetPersonnelByOrganizationAsync(int organizationId)
    {
        var personnel = await _context.CustomerPersonnel
            .Where(cp => !cp.IsDeleted && cp.IsActive)
            .Where(cp => cp.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == organizationId && !oa.IsDeleted))
            // Süpervizörleri hariç tut
            .Where(cp => cp.RoleId != CustomerPersonnelRoles.Ids.Supervisor)
            .OrderBy(cp => cp.FirstName)
            .ThenBy(cp => cp.LastName)
            .Select(cp => new PersonnelOptionDto
            {
                Id = cp.Id,
                Name = cp.FirstName + " " + cp.LastName,
                Title = cp.Title ?? "",
                // Junction table'dan ilk organizasyon ID'si
                OrganizationId = cp.OrganizationAssignments
                    .Where(oa => !oa.IsDeleted)
                    .Select(oa => (int?)oa.CustomerOrganizationId)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return personnel;
    }

    /// <summary>
    /// Müşterinin tüm personelini getirir (organizasyon seçilmemişse)
    /// </summary>
    public async Task<List<PersonnelOptionDto>> GetPersonnelByCustomerAsync(int customerId)
    {
        var personnel = await _context.CustomerPersonnel
            .Where(cp => !cp.IsDeleted && cp.IsActive && cp.CustomerId == customerId)
            // Süpervizörleri hariç tut
            .Where(cp => cp.RoleId != CustomerPersonnelRoles.Ids.Supervisor)
            .OrderBy(cp => cp.FirstName)
            .ThenBy(cp => cp.LastName)
            .Select(cp => new PersonnelOptionDto
            {
                Id = cp.Id,
                Name = cp.FirstName + " " + cp.LastName,
                Title = cp.Title ?? "",
                OrganizationId = cp.OrganizationAssignments
                    .Where(oa => !oa.IsDeleted)
                    .Select(oa => (int?)oa.CustomerOrganizationId)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return personnel;
    }

    /// <summary>
    /// Yeni personel talebi oluşturur (Listede Yok seçildiğinde)
    /// </summary>
    /// <returns>Warning mesajı (varsa) veya null</returns>
    private async Task<string?> CreatePersonnelRequestAsync(Evaluation evaluation, SubmitEvaluationDto dto, int customerId)
    {
        if (dto.NewPersonnel == null) return null;

        // Bu evaluation için zaten bir talep var mı kontrol et
        var existingRequestForEvaluation = await _context.PersonnelRequests
            .AnyAsync(pr => pr.EvaluationId == evaluation.Id);

        if (existingRequestForEvaluation) return null; // Zaten bu evaluation için talep var

        var firstName = dto.NewPersonnel.FirstName.Trim();
        var lastName = dto.NewPersonnel.LastName.Trim();

        // Aynı müşteride aynı ad-soyad ile bekleyen talep var mı kontrol et
        var existingPendingRequest = await _context.PersonnelRequests
            .FirstOrDefaultAsync(pr => pr.CustomerId == customerId &&
                                       pr.FirstName.ToLower() == firstName.ToLower() &&
                                       pr.LastName.ToLower() == lastName.ToLower() &&
                                       pr.Status == ApprovalStatuses.Ids.Pending);

        if (existingPendingRequest != null)
        {
            // Zaten bekleyen talep var, yeni talep oluşturma ama kayda devam et
            var message = await _localizationService.GetResourceAsync(
                "PersonnelRequest.AlreadyPending",
                "'{0}' için zaten bekleyen bir personel talebi bulunmaktadır. Yöneticinizle iletişime geçin.");
            return string.Format(message, $"{firstName} {lastName}");
        }

        var personnelRequest = new PersonnelRequest
        {
            EvaluationId = evaluation.Id,
            CustomerId = customerId,
            CustomerOrganizationId = dto.EvaluatedOrganizationId ?? 0,
            FirstName = firstName,
            LastName = lastName,
            Title = dto.NewPersonnel.Title?.Trim(),
            Notes = $"Değerlendirme #{evaluation.Id} sırasında oluşturuldu",
            RequestedByUserId = dto.EvaluatorId ?? 0,
            Status = ApprovalStatuses.Ids.Pending
        };

        _context.PersonnelRequests.Add(personnelRequest);
        await _context.SaveChangesAsync();

        // Admin'lere bildirim gönder
        var admins = await _context.Users
            .Where(u => u.RoleId == UserRoles.Ids.Admin && u.IsActive && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var adminId in admins)
        {
            var notification = new Notification
            {
                RecipientUserId = adminId,
                NotificationTypeId = NotificationTypes.Ids.Info,
                ChannelId = NotificationChannels.Ids.InApp,
                PriorityId = NotificationPriorities.Ids.Normal,
                Title = "PersonnelRequest.New",
                Message = $"Yeni personel talebi: {personnelRequest.FullName}",
                ActionUrl = $"/UserRequests?tab=personnel&id={personnelRequest.Id}",
                IsRead = false
            };
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();
        return null; // Başarılı, warning yok
    }

    /// <summary>
    /// Puan hesapla - API endpoint için wrapper
    /// Checklist ID'den soruları çeker ve CalculateScoreCore'u çağırır
    /// Kaydetmez, sadece hesaplayıp sonucu döndürür
    /// </summary>
    public async Task<ScoreCalculationResultDto> CalculateScoreAsync(CalculateScoreRequestDto request)
    {
        // Checklist'in sorularını getir
        var questions = await _context.Questions
            .Where(q => q.ChecklistId == request.ChecklistId && !q.IsDeleted)
            .OrderBy(q => q.Order)
            .ToListAsync();

        // Tek hesaplama noktasını kullan
        return CalculateScoreCore(questions, request.Answers);
    }
}
