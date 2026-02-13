using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.DTOs.Evaluation;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services;

public class EvaluationService : IEvaluationService
{
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILocalizationService _localizationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EvaluationService> _logger;

    public EvaluationService(
        IEvaluationRepository evaluationRepository,
        IAssignmentRepository assignmentRepository,
        ApplicationDbContext context,
        ILocalizationService localizationService,
        IServiceProvider serviceProvider,
        IHttpContextAccessor httpContextAccessor,
        ILogger<EvaluationService> logger)
    {
        _evaluationRepository = evaluationRepository;
        _assignmentRepository = assignmentRepository;
        _context = context;
        _localizationService = localizationService;
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
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

    public async Task<EvaluationDto?> GetByProjectIdSingleAsync(int projectId)
    {
        var evaluation = await _evaluationRepository.GetByProjectIdAsync(projectId, includeDetails: true);
        return evaluation == null ? null : await MapToDtoAsync(evaluation);
    }

    public async Task<IEnumerable<EvaluationDto>> GetByEvaluatorIdAsync(int evaluatorId)
    {
        // Projection kullanarak N+1 problemini çöz
        return await _context.Evaluations
            .Where(e => e.EvaluatorId == evaluatorId && !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EvaluationDto
            {
                Id = e.Id,
                ProjectId = e.ProjectId,
                AssignmentPeriodId = e.AssignmentPeriodId,
                AssignmentPeriodName = e.AssignmentPeriod != null ? e.AssignmentPeriod.Name : null,
                EvaluatorId = e.EvaluatorId,
                EvaluatorName = e.EvaluatorCustomerPersonnel != null
                    ? e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName
                    : (e.Evaluator != null ? e.Evaluator.FirstName + " " + e.Evaluator.LastName : null),
                Status = EvaluationStatuses.GetById(e.StatusId) != null ? EvaluationStatuses.GetById(e.StatusId)!.SystemName : "",
                TotalScore = e.TotalScore,
                MaxScore = e.MaxScore,
                ScorePercentage = e.ScorePercentage,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                Notes = e.Notes,
                EvaluationComment = e.EvaluationComment,
                CallId = e.CallId,
                CallDate = e.CallDate,
                CallTime = e.CallTime,
                Duration = e.Duration,
                EvaluatedPersonnelId = e.EvaluatedCustomerPersonnelId,
                EvaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : e.EvaluatedUnknownPersonnel,
                EvaluatedUnknownPersonnel = e.EvaluatedUnknownPersonnel,
                DealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : null,
                CustomerName = e.EvaluatedCustomerPersonnel != null && e.EvaluatedCustomerPersonnel.Customer != null
                    ? e.EvaluatedCustomerPersonnel.Customer.CompanyName : null,
                YellowCardCount = e.YellowCardCount,
                RedCardCount = e.RedCardCount,
                FormOpenedAt = e.FormOpenedAt,
                ControlDate = e.ControlDate,
                ControlTime = e.ControlTime,
                ProjectName = e.Project != null
                    ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name)
                    : null,
                ChecklistName = e.Project != null && e.Project.Checklist != null ? e.Project.Checklist.Name : null,
                ScoringMethod = e.Project != null && e.Project.Checklist != null
                    ? ScoringMethods.GetById(e.Project.Checklist.ScoringMethodId) != null
                        ? ScoringMethods.GetById(e.Project.Checklist.ScoringMethodId)!.SystemName
                        : null
                    : null,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// CustomerPersonnel kullanıcısının değerlendirmelerini getirir
    /// </summary>
    public async Task<IEnumerable<EvaluationDto>> GetByEvaluatorCustomerPersonnelIdAsync(int customerPersonnelId)
    {
        // Projection kullanarak N+1 problemini çöz
        return await _context.Evaluations
            .Where(e => e.EvaluatorCustomerPersonnelId == customerPersonnelId && !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EvaluationDto
            {
                Id = e.Id,
                ProjectId = e.ProjectId,
                AssignmentPeriodId = e.AssignmentPeriodId,
                AssignmentPeriodName = e.AssignmentPeriod != null ? e.AssignmentPeriod.Name : null,
                EvaluatorId = e.EvaluatorId,
                EvaluatorName = e.EvaluatorCustomerPersonnel != null
                    ? e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName
                    : (e.Evaluator != null ? e.Evaluator.FirstName + " " + e.Evaluator.LastName : null),
                Status = EvaluationStatuses.GetById(e.StatusId) != null ? EvaluationStatuses.GetById(e.StatusId)!.SystemName : "",
                TotalScore = e.TotalScore,
                MaxScore = e.MaxScore,
                ScorePercentage = e.ScorePercentage,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                Notes = e.Notes,
                EvaluationComment = e.EvaluationComment,
                CallId = e.CallId,
                CallDate = e.CallDate,
                CallTime = e.CallTime,
                Duration = e.Duration,
                EvaluatedPersonnelId = e.EvaluatedCustomerPersonnelId,
                EvaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : e.EvaluatedUnknownPersonnel,
                EvaluatedUnknownPersonnel = e.EvaluatedUnknownPersonnel,
                DealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : null,
                CustomerName = e.EvaluatedCustomerPersonnel != null && e.EvaluatedCustomerPersonnel.Customer != null
                    ? e.EvaluatedCustomerPersonnel.Customer.CompanyName : null,
                YellowCardCount = e.YellowCardCount,
                RedCardCount = e.RedCardCount,
                FormOpenedAt = e.FormOpenedAt,
                ControlDate = e.ControlDate,
                ControlTime = e.ControlTime,
                ProjectName = e.Project != null
                    ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name)
                    : null,
                ChecklistName = e.Project != null && e.Project.Checklist != null ? e.Project.Checklist.Name : null,
                ScoringMethod = e.Project != null && e.Project.Checklist != null
                    ? ScoringMethods.GetById(e.Project.Checklist.ScoringMethodId) != null
                        ? ScoringMethods.GetById(e.Project.Checklist.ScoringMethodId)!.SystemName
                        : null
                    : null,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// Temsilcinin kendisi hakkındaki değerlendirmeleri getirir (EvaluatedCustomerPersonnelId ile)
    /// CustomerOperator'ın kendi performansını görmesi için kullanılır
    /// </summary>
    public async Task<IEnumerable<EvaluationDto>> GetByEvaluatedCustomerPersonnelIdAsync(int customerPersonnelId)
    {
        // Projection kullanarak N+1 problemini çöz
        return await _context.Evaluations
            .Where(e => e.EvaluatedCustomerPersonnelId == customerPersonnelId && !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EvaluationDto
            {
                Id = e.Id,
                ProjectId = e.ProjectId,
                AssignmentPeriodId = e.AssignmentPeriodId,
                AssignmentPeriodName = e.AssignmentPeriod != null ? e.AssignmentPeriod.Name : null,
                EvaluatorId = e.EvaluatorId,
                EvaluatorName = e.EvaluatorCustomerPersonnel != null
                    ? e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName
                    : (e.Evaluator != null ? e.Evaluator.FirstName + " " + e.Evaluator.LastName : null),
                Status = EvaluationStatuses.GetById(e.StatusId) != null ? EvaluationStatuses.GetById(e.StatusId)!.SystemName : "",
                TotalScore = e.TotalScore,
                MaxScore = e.MaxScore,
                ScorePercentage = e.ScorePercentage,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                Notes = e.Notes,
                EvaluationComment = e.EvaluationComment,
                CallId = e.CallId,
                CallDate = e.CallDate,
                CallTime = e.CallTime,
                Duration = e.Duration,
                EvaluatedPersonnelId = e.EvaluatedCustomerPersonnelId,
                EvaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : e.EvaluatedUnknownPersonnel,
                EvaluatedUnknownPersonnel = e.EvaluatedUnknownPersonnel,
                DealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : null,
                CustomerName = e.EvaluatedCustomerPersonnel != null && e.EvaluatedCustomerPersonnel.Customer != null
                    ? e.EvaluatedCustomerPersonnel.Customer.CompanyName : null,
                YellowCardCount = e.YellowCardCount,
                RedCardCount = e.RedCardCount,
                FormOpenedAt = e.FormOpenedAt,
                ControlDate = e.ControlDate,
                ControlTime = e.ControlTime,
                ProjectName = e.Project != null
                    ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name)
                    : null,
                ChecklistName = e.Project != null && e.Project.Checklist != null ? e.Project.Checklist.Name : null,
                ScoringMethod = e.Project != null && e.Project.Checklist != null
                    ? ScoringMethods.GetById(e.Project.Checklist.ScoringMethodId) != null
                        ? ScoringMethods.GetById(e.Project.Checklist.ScoringMethodId)!.SystemName
                        : null
                    : null,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<EvaluationDto>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        // Projection kullanarak N+1 problemini çöz
        return await _context.Evaluations
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EvaluationDto
            {
                Id = e.Id,
                ProjectId = e.ProjectId,
                AssignmentPeriodId = e.AssignmentPeriodId,
                AssignmentPeriodName = e.AssignmentPeriod != null ? e.AssignmentPeriod.Name : null,
                EvaluatorId = e.EvaluatorId,
                EvaluatorName = e.EvaluatorCustomerPersonnel != null
                    ? e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName
                    : (e.Evaluator != null ? e.Evaluator.FirstName + " " + e.Evaluator.LastName : null),
                Status = EvaluationStatuses.GetById(e.StatusId) != null ? EvaluationStatuses.GetById(e.StatusId)!.SystemName : "",
                TotalScore = e.TotalScore,
                MaxScore = e.MaxScore,
                ScorePercentage = e.ScorePercentage,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                Notes = e.Notes,
                EvaluationComment = e.EvaluationComment,
                CallId = e.CallId,
                CallDate = e.CallDate,
                CallTime = e.CallTime,
                Duration = e.Duration,
                EvaluatedPersonnelId = e.EvaluatedCustomerPersonnelId,
                EvaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : e.EvaluatedUnknownPersonnel,
                EvaluatedUnknownPersonnel = e.EvaluatedUnknownPersonnel,
                DealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : null,
                CustomerName = e.EvaluatedCustomerPersonnel != null && e.EvaluatedCustomerPersonnel.Customer != null
                    ? e.EvaluatedCustomerPersonnel.Customer.CompanyName : null,
                YellowCardCount = e.YellowCardCount,
                RedCardCount = e.RedCardCount,
                FormOpenedAt = e.FormOpenedAt,
                ControlDate = e.ControlDate,
                ControlTime = e.ControlTime,
                ProjectName = e.Project != null
                    ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name)
                    : null,
                ChecklistName = e.Project != null && e.Project.Checklist != null ? e.Project.Checklist.Name : null,
                ScoringMethod = e.Project != null && e.Project.Checklist != null
                    ? ScoringMethods.GetById(e.Project.Checklist.ScoringMethodId) != null
                        ? ScoringMethods.GetById(e.Project.Checklist.ScoringMethodId)!.SystemName
                        : null
                    : null,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<EvaluationDto>> GetByProjectIdAsync(int projectId)
    {
        // Projection kullanarak N+1 problemini çöz
        return await _context.Evaluations
            .Where(e => !e.IsDeleted && e.ProjectId == projectId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EvaluationDto
            {
                Id = e.Id,
                ProjectId = e.ProjectId,
                AssignmentPeriodId = e.AssignmentPeriodId,
                AssignmentPeriodName = e.AssignmentPeriod != null ? e.AssignmentPeriod.Name : null,
                EvaluatorId = e.EvaluatorId,
                EvaluatorName = e.EvaluatorCustomerPersonnel != null
                    ? e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName
                    : (e.Evaluator != null ? e.Evaluator.FirstName + " " + e.Evaluator.LastName : null),
                Status = EvaluationStatuses.GetById(e.StatusId) != null ? EvaluationStatuses.GetById(e.StatusId)!.SystemName : "",
                TotalScore = e.TotalScore,
                MaxScore = e.MaxScore,
                ScorePercentage = e.ScorePercentage,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                Notes = e.Notes,
                EvaluationComment = e.EvaluationComment,
                CallId = e.CallId,
                CallDate = e.CallDate,
                CallTime = e.CallTime,
                Duration = e.Duration,
                EvaluatedPersonnelId = e.EvaluatedCustomerPersonnelId,
                EvaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : e.EvaluatedUnknownPersonnel,
                EvaluatedUnknownPersonnel = e.EvaluatedUnknownPersonnel,
                DealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : null,
                CustomerName = e.EvaluatedCustomerPersonnel != null && e.EvaluatedCustomerPersonnel.Customer != null
                    ? e.EvaluatedCustomerPersonnel.Customer.CompanyName : null,
                YellowCardCount = e.YellowCardCount,
                RedCardCount = e.RedCardCount,
                FormOpenedAt = e.FormOpenedAt,
                ControlDate = e.ControlDate,
                ControlTime = e.ControlTime,
                ProjectName = e.Project != null
                    ? (e.Project.Code != null ? e.Project.Code + " - " + e.Project.Name : e.Project.Name)
                    : null,
                ChecklistName = e.Project != null && e.Project.Checklist != null ? e.Project.Checklist.Name : null,
                ScoringMethod = e.Project != null && e.Project.Checklist != null
                    ? ScoringMethods.GetById(e.Project.Checklist.ScoringMethodId) != null
                        ? ScoringMethods.GetById(e.Project.Checklist.ScoringMethodId)!.SystemName
                        : null
                    : null,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<EvaluationDto> StartEvaluationAsync(int projectId, int? evaluatorId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
            throw new KeyNotFoundException($"Project with ID {projectId} not found");

        var evaluation = new Evaluation
        {
            ProjectId = projectId,
            ChecklistId = project.ChecklistId,
            EvaluatorId = evaluatorId,
            StatusId = EvaluationStatuses.Ids.InProgress,
            StartedAt = TurkeyTime.Now,
            FormOpenedAt = TurkeyTime.Now
        };

        var created = await _evaluationRepository.CreateAsync(evaluation);
        return await MapToDtoAsync(created);
    }

    public async Task<EvaluationDto> StartEvaluationAsync(StartEvaluationDto dto)
    {
        var project = await _context.Projects.FindAsync(dto.ProjectId);
        if (project == null)
            throw new KeyNotFoundException($"Project with ID {dto.ProjectId} not found");

        var evaluation = new Evaluation
        {
            ProjectId = dto.ProjectId,
            ChecklistId = project.ChecklistId,
            AssignmentId = dto.AssignmentId > 0 ? dto.AssignmentId : null,
            AssignmentPeriodId = dto.AssignmentPeriodId,
            // User mı CustomerPersonnel mı olduğunu ayır
            EvaluatorId = dto.EvaluatorId > 0 ? dto.EvaluatorId : null,
            EvaluatorCustomerPersonnelId = dto.EvaluatorCustomerPersonnelId > 0 ? dto.EvaluatorCustomerPersonnelId : null,
            StatusId = EvaluationStatuses.Ids.InProgress,
            StartedAt = TurkeyTime.Now,
            FormOpenedAt = TurkeyTime.Now,
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
        evaluation.DescriptionsJson = SerializeDescriptions(dto.Descriptions);
        evaluation.UpdatedAt = TurkeyTime.Now;

        await _context.SaveChangesAsync();
        return await MapToDtoAsync(evaluation);
    }

    public async Task<EvaluationFormDto?> GetEvaluationFormAsync(int assignmentId)
    {
        // Assignment üzerinden projeyi bul
        var assignment = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Customer)
            .Include(a => a.Project)
                .ThenInclude(p => p.Organization)
            .Include(a => a.Project)
                .ThenInclude(p => p.Checklist)
                    .ThenInclude(c => c.CustomerOrganization)
            .Include(a => a.Project)
                .ThenInclude(p => p.Checklist)
                    .ThenInclude(c => c.Questions.Where(q => !q.IsDeleted))
                        .ThenInclude(q => q.SubCriteria.Where(sc => !sc.IsDeleted && sc.IsActive))
            .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted);

        if (assignment?.Project == null)
            return null;

        var project = assignment.Project;
        var projectId = project.Id;

        // Not: Her "Ekle" dediğinde yeni evaluation oluşturulacak
        // Mevcut evaluationlar sadece düzenleme endpoint'inden yüklenir
        // Bu fonksiyon her zaman boş form döndürür

        // Organizasyon önce Project'ten, yoksa Checklist'ten geliyor
        var organizations = new List<OrganizationOptionDto>();
        // Öncelik: Project.OrganizationId > Checklist.CustomerOrganizationId
        int? selectedOrganizationId = project.OrganizationId ?? project.Checklist?.CustomerOrganizationId;

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
        else if (project.Checklist?.CustomerId.HasValue == true)
        {
            organizations = await _context.CustomerOrganizations
                .Where(co => !co.IsDeleted && co.IsActive && co.CustomerId == project.Checklist.CustomerId)
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
            var customerId = project.CustomerId ?? project.Checklist?.CustomerId;
            if (customerId.HasValue)
            {
                personnel = await GetPersonnelByCustomerAsync(customerId.Value);
            }
        }

        // Get available periods (only Open periods)
        var periods = new List<PeriodOptionDto>();

        return new EvaluationFormDto
        {
            ProjectId = projectId,
            AssignmentId = assignmentId,
            EvaluationId = null, // Yeni evaluation oluşturulacak
            Status = "New",
            ProjectName = !string.IsNullOrEmpty(project.Code) ? $"{project.Code} - {project.Name}" : (project.Name ?? ""),
            CustomerName = project.Customer?.CompanyName,
            ChecklistId = project.ChecklistId,
            ChecklistName = project.Checklist?.Name ?? "",
            ChecklistType = project.Checklist != null ? ChecklistTypes.GetById(project.Checklist.ChecklistTypeId)?.SystemName : null,
            ScoringMethod = project.Checklist?.ScoringMethodId.ToString(),
            MaxTotalPoints = project.Checklist?.MaxTotalPoints ?? 100,
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
            PenaltyGroups = BuildPenaltyGroupsFromQuestions(project.Checklist?.Questions.Where(q => !q.IsDeleted).ToList()),
            ExistingAnswers = new List<AnswerDto>() // Yeni form, cevap yok
        };
    }

    public async Task<EvaluationFormDto?> GetExistingEvaluationFormAsync(int evaluationId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Customer)
            .Include(e => e.Project)
                .ThenInclude(p => p.Organization)
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
                    .ThenInclude(c => c.CustomerOrganization)
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
                    .ThenInclude(c => c.Questions.Where(q => !q.IsDeleted))
                        .ThenInclude(q => q.SubCriteria.Where(sc => !sc.IsDeleted && sc.IsActive))
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
                    .ThenInclude(s => s.SubCriteria)
            .FirstOrDefaultAsync(e => e.Id == evaluationId && !e.IsDeleted);

        if (evaluation == null)
            return null;

        var project = evaluation.Project;

        // Organizasyon - Öncelik: Evaluation > Project > Checklist
        int? selectedOrganizationId = evaluation.EvaluatedOrganizationId
            ?? project.OrganizationId
            ?? project.Checklist?.CustomerOrganizationId;

        var personnel = new List<PersonnelOptionDto>();
        if (selectedOrganizationId.HasValue)
        {
            personnel = await GetPersonnelByOrganizationAsync(selectedOrganizationId.Value);
        }
        else
        {
            // Organizasyon seçilmemişse tüm firma personelini getir
            var customerId = project.CustomerId ?? project.Checklist?.CustomerId;
            if (customerId.HasValue)
            {
                personnel = await GetPersonnelByCustomerAsync(customerId.Value);
            }
        }

        // Dönemler
        var periods = new List<PeriodOptionDto>();

        return new EvaluationFormDto
        {
            ProjectId = project.Id,
            EvaluationId = evaluation.Id,
            Status = EvaluationStatuses.GetById(evaluation.StatusId)?.SystemName ?? "",
            ProjectName = !string.IsNullOrEmpty(project.Code) ? $"{project.Code} - {project.Name}" : (project.Name ?? ""),
            CustomerName = project.Customer?.CompanyName,
            ChecklistId = project.ChecklistId,
            ChecklistName = project.Checklist?.Name ?? "",
            ChecklistType = project.Checklist != null ? ChecklistTypes.GetById(project.Checklist.ChecklistTypeId)?.SystemName : null,
            ScoringMethod = project.Checklist?.ScoringMethodId.ToString(),
            MaxTotalPoints = project.Checklist?.MaxTotalPoints ?? 100,
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
            PenaltyGroups = BuildPenaltyGroupsFromQuestions(project.Checklist?.Questions.Where(q => !q.IsDeleted).ToList()),
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
        // Get project with checklist details (Questions direkt Checklist'e bağlı, GroupName ile gruplandırılır)
        var project = await _context.Projects
            .Include(p => p.Checklist)
                .ThenInclude(c => c.Questions.Where(q => !q.IsDeleted))
                    .ThenInclude(q => q.SubCriteria.Where(sc => sc.IsActive))
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId && !p.IsDeleted);

        if (project == null)
            throw new KeyNotFoundException($"Project with ID {dto.ProjectId} not found");

        // CallId tekrar kontrolü - aynı müşteriye ait başka bir dinlemede aynı CallId varsa hata ver
        if (!string.IsNullOrWhiteSpace(dto.CallId) && project.CustomerId != null)
        {
            var customerId = project.CustomerId.Value;
            var duplicateExists = await _context.Evaluations
                .AnyAsync(e => !e.IsDeleted &&
                              e.CallId == dto.CallId &&
                              e.Project.CustomerId == customerId &&
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
                ProjectId = dto.ProjectId,
                ChecklistId = project.ChecklistId,
                AssignmentId = dto.AssignmentId > 0 ? dto.AssignmentId : null,
                AssignmentPeriodId = dto.AssignmentPeriodId,
                // User mı CustomerPersonnel mı olduğunu ayır
                EvaluatorId = dto.EvaluatorId > 0 ? dto.EvaluatorId : null,
                EvaluatorCustomerPersonnelId = dto.EvaluatorCustomerPersonnelId > 0 ? dto.EvaluatorCustomerPersonnelId : null,
                StatusId = EvaluationStatuses.Ids.InProgress,
                StartedAt = TurkeyTime.Now,
                FormOpenedAt = dto.FormOpenedAt ?? TurkeyTime.Now
            };
            _context.Evaluations.Add(evaluation);
        }
        else if (dto.AssignmentPeriodId.HasValue && !evaluation.AssignmentPeriodId.HasValue)
        {
            // Update period if not set
            evaluation.AssignmentPeriodId = dto.AssignmentPeriodId;
        }

        // Get all questions from checklist (Questions direkt Checklist'e bağlı)
        var allQuestions = project.Checklist.Questions
            .Where(q => !q.IsDeleted)
            .ToList();

        // Calculate scores with penalty handling
        var scoreResult = CalculateScoreWithPenalties(allQuestions, dto.Answers, project.Checklist.ScoringMethodId);

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
                        SelectedAt = TurkeyTime.Now
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
        // FieldWorker ziyaretleri için bayi ID
        evaluation.CustomerDealerId = dto.CustomerDealerId > 0 ? dto.CustomerDealerId : null;
        evaluation.YellowCardCount = scoreResult.YellowCardCount;
        evaluation.RedCardCount = scoreResult.RedCardCount;
        evaluation.UpdatedAt = TurkeyTime.Now;

        if (targetStatusId == EvaluationStatuses.Ids.Completed)
        {
            evaluation.CompletedAt = TurkeyTime.Now;
            // Not: Assignment tamamlandı olarak işaretlenmiyor
            // Aynı atamaya sınırsız dinleme (evaluation) eklenebilir
        }

        await _context.SaveChangesAsync();

        // AssignmentCustomerDealer.EvaluationId güncelle (taslak veya tamamlandı fark etmez)
        if (dto.AssignmentId.HasValue && dto.AssignmentId.Value > 0 && dto.CustomerDealerId.HasValue && dto.CustomerDealerId.Value > 0)
        {
            var assignmentDealer = await _context.Set<AssignmentCustomerDealer>()
                .FirstOrDefaultAsync(acd => acd.AssignmentId == dto.AssignmentId.Value
                                         && acd.CustomerDealerId == dto.CustomerDealerId.Value);
            if (assignmentDealer != null)
            {
                assignmentDealer.EvaluationId = evaluation.Id;
                await _context.SaveChangesAsync();
            }
        }

        // "Her Kayıtta" bildirim gönder (tamamlandıysa)
        if (targetStatusId == EvaluationStatuses.Ids.Completed)
        {
            var evaluationId = evaluation.Id;
            // BaseUrl'i Task.Run öncesinde yakala (HttpContext Task.Run içinde yok)
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "";
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var notificationService = scope.ServiceProvider.GetService<IEvaluationNotificationService>();
                    if (notificationService != null)
                    {
                        var evalForNotification = await dbContext.Evaluations
                            .Include(e => e.Project)
                            .Include(e => e.EvaluatedCustomerPersonnel)
                            .Include(e => e.EvaluatedOrganization)
                            .FirstOrDefaultAsync(e => e.Id == evaluationId);

                        if (evalForNotification != null)
                        {
                            await notificationService.SendSingleEvaluationNotificationAsync(evalForNotification, baseUrl);

                            // Proje ekibine in-app bildirim gönder (evaluator hariç)
                            var notificationCreator = scope.ServiceProvider.GetService<INotificationCreatorService>();
                            if (notificationCreator != null)
                            {
                                var teamMemberUserIds = await dbContext.ProjectTeamMembers
                                    .Where(tm => tm.ProjectId == evalForNotification.ProjectId
                                        && tm.UserId != evalForNotification.EvaluatorId)
                                    .Select(tm => tm.UserId)
                                    .ToListAsync();

                                if (teamMemberUserIds.Any())
                                {
                                    var projectName = evalForNotification.Project?.Name ?? "Proje";
                                    await notificationCreator.CreateBulkAsync(
                                        teamMemberUserIds,
                                        NotificationTypes.Ids.Info,
                                        "Yeni Değerlendirme Tamamlandı",
                                        $"{projectName} projesinde yeni bir değerlendirme tamamlandı.",
                                        actionUrl: $"/Evaluations/Detail/{evaluationId}",
                                        relatedEntityId: evaluationId,
                                        relatedEntityType: "Evaluation",
                                        senderUserId: evalForNotification.EvaluatorId);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Bildirim hatası ana işlemi etkilemesin ama loglansın
                    using var logScope = _serviceProvider.CreateScope();
                    var auditLog = logScope.ServiceProvider.GetService<IAuditLogService>();
                    await auditLog!.LogErrorAsync($"Değerlendirme #{evaluationId} bildirim gönderiminde hata", "EvaluationNotification", ex);
                }
            });
        }

        // Yeni personel talebi oluştur (Listede Yok seçilmişse - taslak dahil her durumda)
        string? personnelRequestWarning = null;
        if (dto.NewPersonnel != null &&
            !string.IsNullOrWhiteSpace(dto.NewPersonnel.FirstName) &&
            !string.IsNullOrWhiteSpace(dto.NewPersonnel.LastName) &&
            project.CustomerId != null)
        {
            try
            {
                personnelRequestWarning = await CreatePersonnelRequestAsync(evaluation, dto, project.CustomerId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PersonnelRequest oluşturulurken hata (EvaluationId: {EvaluationId})", evaluation.Id);
                // Evaluation zaten kaydedildi, personel talebi hatası evaluation'ı etkilemesin
            }
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
        CalculateScoreWithPenalties(List<Question> allQuestions, List<SubmitAnswerDto> answers, int scoringMethodId = ScoringMethods.Ids.Maximum)
    {
        // SubmitAnswerDto -> ScoreAnswerDto dönüşümü
        // GivenPoints yoksa AnswerNumeric'i kullan (normal 0-5 arası puanlama için)
        var scoreAnswers = answers.Select(a => new ScoreAnswerDto
        {
            QuestionId = a.QuestionId,
            GivenPoints = a.GivenPoints ?? a.AnswerNumeric,
            SelectedSubCriteriaIds = a.SelectedSubCriteriaIds,
            ApplyPenalty = a.ApplyPenalty,
            SelectedPenaltyType = a.SelectedPenaltyType
        }).ToList();

        var result = CalculateScoreCore(allQuestions, scoreAnswers, scoringMethodId);
        return (result.TotalEarned, result.MaxPossible, result.Percentage, result.YellowCardCount, result.RedCardCount);
    }

    /// <summary>
    /// TEK HESAPLAMA NOKTASI - Tüm puan hesaplamaları buradan geçer
    ///
    /// Hesaplama Mantığı (Maximum modu - varsayılan):
    /// - Penalty sorular: Her zaman opsiyonel, sadece ApplyPenalty=true ise ceza uygulanır, ağırlığa dahil edilmez
    /// - Unscored sorular: Puan hesabına katılmaz
    /// - Scored sorular:
    ///   - Cevap verilmişse → hesaplamaya dahil
    ///   - Cevap verilmemişse + zorunlu değilse → atla
    ///   - Cevap verilmemişse + zorunluysa → 0 puan olarak dahil
    ///
    /// Hesaplama Mantığı (CriteriaTotal modu - SESTEK tarzı):
    /// - Seçilen SubCriteria'nın WeightPoints değeri direkt kazanılan puan olur
    /// - Negatif puanlar olabilir (Ödül & Ceza soruları)
    /// - MaxPossible = Tüm soruların maksimum SubCriteria puanlarının toplamı
    /// </summary>
    private ScoreCalculationResultDto CalculateScoreCore(List<Question> questions, List<ScoreAnswerDto> answers, int scoringMethodId = ScoringMethods.Ids.Maximum)
    {
        decimal totalEarned = 0;
        decimal totalMaxPoints = 0;
        int yellowCardCount = 0;
        int redCardCount = 0;
        int includedQuestionCount = 0;

        // Cevapları dictionary'e çevir (hızlı erişim için)
        var answerDict = answers.ToDictionary(a => a.QuestionId, a => a);

        // CriteriaTotal modu mu?
        bool isCriteriaTotal = scoringMethodId == ScoringMethods.Ids.CriteriaTotal;

        foreach (var question in questions)
        {
            var answer = answerDict.GetValueOrDefault(question.Id);

            // 1. Puansız soruları atla
            if (question.ScoringTypeId == ScoringTypes.Ids.Unscored)
                continue;

            // 2. Cezalı sorular - her zaman opsiyonel, sadece ceza uygulanırsa işle
            if (question.ScoringTypeId == ScoringTypes.Ids.Penalty)
            {
                if (answer != null && answer.ApplyPenalty && answer.GivenPoints.HasValue && answer.GivenPoints.Value > 0)
                {
                    if (answer.SelectedPenaltyType == "YellowCard")
                        yellowCardCount++;
                    else if (answer.SelectedPenaltyType == "RedCard")
                        redCardCount++;

                    // Orantılı ceza: (cevap / MaxPoints) * WeightPoints
                    var maxPoints = question.MaxPoints > 0 ? question.MaxPoints : 2m;
                    var penaltyAmount = (answer.GivenPoints.Value / maxPoints) * question.WeightPoints;
                    totalEarned -= penaltyAmount;
                }
                // Penalty sorular ağırlığa dahil edilmez
                continue;
            }

            // 3. Normal puanlı sorular (Scored)
            if (isCriteriaTotal)
            {
                // CriteriaTotal modu: Seçilen SubCriteria'nın puanını al
                bool hasSubCriteriaSelection = answer?.SelectedSubCriteriaIds?.Any() == true;

                // Zorunlu olmayan soru ve cevap verilmemiş → atla
                if (!question.IsRequired && !hasSubCriteriaSelection)
                    continue;

                // Bu soru hesaplamaya dahil
                includedQuestionCount++;

                // SubCriteria'ların maksimum puanını bul (MaxPossible için)
                var subCriteriaList = question.SubCriteria?.Where(sc => sc.IsActive).ToList() ?? new List<QuestionSubCriteria>();
                if (subCriteriaList.Any())
                {
                    totalMaxPoints += subCriteriaList.Max(sc => sc.WeightPoints);
                }

                // Cevap varsa puanı hesapla
                if (hasSubCriteriaSelection)
                {
                    var selectedSubCriteriaId = answer!.SelectedSubCriteriaIds!.First();
                    var selectedSubCriteria = subCriteriaList.FirstOrDefault(sc => sc.Id == selectedSubCriteriaId);
                    if (selectedSubCriteria != null)
                    {
                        totalEarned += selectedSubCriteria.WeightPoints;
                    }
                }
                // Cevap yoksa (zorunlu soru ama cevap verilmemiş) → 0 puan
            }
            else
            {
                // Maximum modu: Mevcut hesaplama mantığı
                bool hasAnswer = answer != null && answer.GivenPoints.HasValue;

                // Zorunlu olmayan soru ve cevap verilmemiş → atla
                if (!question.IsRequired && !hasAnswer)
                    continue;

                // Bu soru hesaplamaya dahil
                includedQuestionCount++;
                totalMaxPoints += question.WeightPoints;

                // Cevap varsa puanı hesapla
                if (hasAnswer)
                {
                    // Formül: (cevap / MaxPoints) * WeightPoints
                    var maxPoints = question.MaxPoints > 0 ? question.MaxPoints : 5;
                    var earnedPoints = (answer!.GivenPoints!.Value / maxPoints) * question.WeightPoints;
                    totalEarned += earnedPoints;
                }
                // Cevap yoksa (zorunlu soru ama cevap verilmemiş) → 0 puan
            }
        }

        // Yüzde hesapla
        var percentage = totalMaxPoints > 0 ? (totalEarned / totalMaxPoints) * 100 : 0;
        // CriteriaTotal'da negatif yüzde olabilir, Maximum'da olamaz
        if (!isCriteriaTotal)
        {
            percentage = Math.Max(0, percentage);
        }
        percentage = Math.Round(percentage, 2);

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
        // Puansız sorular için null döndür
        if (question.ScoringTypeId == ScoringTypes.Ids.Unscored)
            return null;

        // Cevap değerini al (GivenPoints veya AnswerNumeric)
        var answerValue = answer.GivenPoints ?? answer.AnswerNumeric ?? 0;
        var maxPoints = question.MaxPoints > 0 ? question.MaxPoints : 5m;
        var weight = question.WeightPoints;

        // Cezalı sorular - ceza uygulanmışsa orantılı ceza hesapla
        if (question.ScoringTypeId == ScoringTypes.Ids.Penalty)
        {
            if (answer.ApplyPenalty && answerValue > 0)
            {
                // Orantılı ceza: (cevap / MaxPoints) * WeightPoints
                return (answerValue / maxPoints) * weight;
            }
            return 0;
        }

        // Normal puanlı sorular
        // Formül: (cevap / MaxPoints) * WeightPoints
        // Örnek: ağırlık=10, max=5, cevap=5 → (5/5) * 10 = 10 puan
        // Örnek: ağırlık=10, max=5, cevap=3 → (3/5) * 10 = 6 puan

        // MaxPoints = 1 ise Evet/Hayır tipi: 0 veya tam puan
        if (maxPoints == 1)
        {
            var answered = answerValue > 0 ||
                answer.AnswerText?.ToLower() == "evet" ||
                answer.AnswerText?.ToLower() == "yes";
            return answered ? weight : 0;
        }

        // MaxPoints > 1 için orantılı hesaplama
        return (answerValue / maxPoints) * weight;
    }

    private Answer CreateAnswerFromDto(SubmitAnswerDto dto)
    {
        return new Answer
        {
            QuestionId = dto.QuestionId,
            AnswerText = dto.AnswerText,
            AnswerNumeric = dto.AnswerNumeric,
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
        answer.GivenPoints = dto.GivenPoints;
        answer.Notes = dto.Notes;
        answer.RecommendationNotes = dto.RecommendationNotes;
        answer.IsPenaltyApplied = dto.ApplyPenalty;
        answer.AppliedPenaltyTypeId = PenaltyTypes.GetBySystemName(dto.SelectedPenaltyType)?.Id
            ?? PenaltyTypes.Ids.None;
        answer.UpdatedAt = TurkeyTime.Now;
    }

    /// <summary>
    /// Soruları PenaltyType'a göre grupla: Sorular, Sarı Kartlar, Kırmızı Kartlar
    /// </summary>
    private List<PenaltyGroupDto> BuildPenaltyGroupsFromQuestions(List<Question>? questions)
    {
        if (questions == null || !questions.Any())
            return new List<PenaltyGroupDto>();

        // Tüm grupları topla (GroupName + minOrder ile)
        var allGroups = new List<(string Name, string PenaltyType, int MinOrder, List<Question> Questions)>();

        // 1. GroupName'i dolu olan soruları GroupName'e göre grupla
        var groupedByName = questions
            .Where(q => !string.IsNullOrWhiteSpace(q.GroupName))
            .GroupBy(q => q.GroupName!)
            .ToList();

        foreach (var group in groupedByName)
        {
            var groupQuestions = group.OrderBy(q => q.Order).ToList();
            var dominantPenaltyType = group
                .GroupBy(q => q.PenaltyTypeId)
                .OrderByDescending(g => g.Count())
                .First().Key;
            var penaltyTypeName = PenaltyTypes.GetById(dominantPenaltyType)?.SystemName ?? "None";

            allGroups.Add((group.Key, penaltyTypeName, groupQuestions.Min(q => q.Order), groupQuestions));
        }

        // 2. GroupName'i BOŞ olan normal sorular → "Genel"
        var normalWithoutGroup = questions
            .Where(q => string.IsNullOrWhiteSpace(q.GroupName) && q.PenaltyTypeId == PenaltyTypes.Ids.None)
            .OrderBy(q => q.Order)
            .ToList();
        if (normalWithoutGroup.Any())
        {
            allGroups.Add(("Genel", "None", normalWithoutGroup.Min(q => q.Order), normalWithoutGroup));
        }

        // 3. GroupName'i BOŞ olan Sarı Kartlar → "Sarı Kartlar"
        var yellowWithoutGroup = questions
            .Where(q => string.IsNullOrWhiteSpace(q.GroupName) && q.PenaltyTypeId == PenaltyTypes.Ids.YellowCard)
            .OrderBy(q => q.Order)
            .ToList();
        if (yellowWithoutGroup.Any())
        {
            allGroups.Add(("Sarı Kartlar", "YellowCard", yellowWithoutGroup.Min(q => q.Order), yellowWithoutGroup));
        }

        // 4. GroupName'i BOŞ olan Kırmızı Kartlar → "Kırmızı Kartlar"
        var redWithoutGroup = questions
            .Where(q => string.IsNullOrWhiteSpace(q.GroupName) && q.PenaltyTypeId == PenaltyTypes.Ids.RedCard)
            .OrderBy(q => q.Order)
            .ToList();
        if (redWithoutGroup.Any())
        {
            allGroups.Add(("Kırmızı Kartlar", "RedCard", redWithoutGroup.Min(q => q.Order), redWithoutGroup));
        }

        // TÜM grupları MinOrder'a göre sırala
        var result = new List<PenaltyGroupDto>();
        var order = 1;
        foreach (var group in allGroups.OrderBy(g => g.MinOrder))
        {
            result.Add(CreatePenaltyGroup(group.Questions, group.Name, group.PenaltyType, order++));
        }

        return result;
    }

    private PenaltyGroupDto CreatePenaltyGroup(List<Question> questions, string name, string penaltyType, int order)
    {
        return new PenaltyGroupDto
        {
            Id = order,
            Name = name,
            Order = order,
            PenaltyType = penaltyType,
            WeightPoints = questions.Sum(q => q.WeightPoints),
            MaxPoints = questions.Sum(q => q.MaxPoints),
            Questions = questions.Select(q => new EvaluationQuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                Order = q.Order,
                IsRequired = q.IsRequired,
                ScoringType = ScoringTypes.GetById(q.ScoringTypeId)?.SystemName ?? "Scored",
                WeightPoints = q.WeightPoints,
                MaxPoints = q.MaxPoints,
                PenaltyType = PenaltyTypes.GetById(q.PenaltyTypeId)?.SystemName ?? "None",
                RecommendedNote = q.RecommendedNote,
                HelpText = q.HelpText,
                AllowComment = q.AllowComment,
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
            EarnedPoints = a.EarnedPoints,
            GivenPoints = a.GivenPoints,
            Notes = a.Notes,
            RecommendationNotes = a.RecommendationNotes,
            AttachmentFileName = a.AttachmentFileName,
            IsPenaltyApplied = a.IsPenaltyApplied,
            AppliedPenaltyType = PenaltyTypes.GetById(a.AppliedPenaltyTypeId)?.SystemName ?? "None",
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
        evaluation.UpdatedAt = TurkeyTime.Now;

        // Not: Assignment artık tamamlandı olarak işaretlenmediği için
        // reopen'da da güncellemesine gerek yok

        // Değişiklik logunu kaydet (Notes alanına ekle)
        var logEntry = $"\n[{TurkeyTime.Now:yyyy-MM-dd HH:mm}] Taslağa alındı. Önceki durum: {previousStatus}. Neden: {reason ?? "Belirtilmedi"}";
        evaluation.Notes = (evaluation.Notes ?? "") + logEntry;

        await _context.SaveChangesAsync();

        // Evaluator'a bildirim gönder (taslağa alan kişi farklıysa)
        if (evaluation.EvaluatorId.HasValue && evaluation.EvaluatorId.Value != revertedByUserId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationCreator = scope.ServiceProvider.GetService<INotificationCreatorService>();
                if (notificationCreator != null)
                {
                    await notificationCreator.CreateAsync(
                        evaluation.EvaluatorId.Value,
                        NotificationTypes.Ids.Warning,
                        "Değerlendirme Taslağa Alındı",
                        $"#{evaluationId} numaralı değerlendirmeniz taslağa alındı. Neden: {reason ?? "Belirtilmedi"}",
                        actionUrl: $"/Evaluations/Detail/{evaluationId}",
                        relatedEntityId: evaluationId,
                        relatedEntityType: "Evaluation",
                        senderUserId: revertedByUserId);
                }
            }
            catch { /* Bildirim hatası ana işlemi etkilemesin */ }
        }

        return await MapToDtoAsync(evaluation);
    }

    /// <summary>
    /// Değerlendirmeyi iptal et
    /// </summary>
    public async Task<EvaluationDto> CancelEvaluationAsync(int evaluationId, int cancelledByUserId, string? reason = null)
    {
        var evaluation = await _context.Evaluations
            .FirstOrDefaultAsync(e => e.Id == evaluationId && !e.IsDeleted);

        if (evaluation == null)
            throw new KeyNotFoundException($"Evaluation with ID {evaluationId} not found");

        if (evaluation.StatusId == EvaluationStatuses.Ids.Cancelled)
            throw new InvalidOperationException("Değerlendirme zaten iptal edilmiş.");

        // Eski durumu logla
        var previousStatusName = EvaluationStatuses.GetById(evaluation.StatusId)?.SystemName ?? "Unknown";

        // Durumu iptal et
        evaluation.StatusId = EvaluationStatuses.Ids.Cancelled;
        evaluation.UpdatedAt = TurkeyTime.Now;

        // Değişiklik logunu kaydet
        var logEntry = $"\n[{TurkeyTime.Now:yyyy-MM-dd HH:mm}] İptal edildi. Önceki durum: {previousStatusName}. Neden: {reason ?? "Belirtilmedi"}";
        evaluation.Notes = (evaluation.Notes ?? "") + logEntry;

        await _context.SaveChangesAsync();

        // Evaluator'a bildirim gönder (iptal eden kişi farklıysa)
        if (evaluation.EvaluatorId.HasValue && evaluation.EvaluatorId.Value != cancelledByUserId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationCreator = scope.ServiceProvider.GetService<INotificationCreatorService>();
                if (notificationCreator != null)
                {
                    await notificationCreator.CreateAsync(
                        evaluation.EvaluatorId.Value,
                        NotificationTypes.Ids.Warning,
                        "Değerlendirme İptal Edildi",
                        $"#{evaluationId} numaralı değerlendirmeniz iptal edildi. Neden: {reason ?? "Belirtilmedi"}",
                        actionUrl: $"/Evaluations/Detail/{evaluationId}",
                        relatedEntityId: evaluationId,
                        relatedEntityType: "Evaluation",
                        senderUserId: cancelledByUserId);
                }
            }
            catch { /* Bildirim hatası ana işlemi etkilemesin */ }
        }

        return await MapToDtoAsync(evaluation);
    }

    private async Task<EvaluationDto> MapToDtoAsync(Evaluation evaluation)
    {
        // Load related data if not loaded
        if (evaluation.Project == null)
        {
            await _context.Entry(evaluation)
                .Reference(e => e.Project)
                .Query()
                .Include(p => p.Checklist)
                .LoadAsync();
        }

        string? evaluatedPersonnelName = null;
        string? customerName = null;
        string? organizationName = null;
        string? supervisorName = null;
        string? dealerName = null;

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

        // Load dealer name if exists
        if (evaluation.CustomerDealerId.HasValue)
        {
            dealerName = await _context.CustomerDealers
                .Where(d => d.Id == evaluation.CustomerDealerId.Value)
                .Select(d => d.Name)
                .FirstOrDefaultAsync();
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
            ProjectId = evaluation.ProjectId,
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
            DealerName = dealerName,
            CustomerName = customerName,
            OrganizationName = organizationName,
            SupervisorName = supervisorName,
            YellowCardCount = evaluation.YellowCardCount,
            RedCardCount = evaluation.RedCardCount,
            FormOpenedAt = evaluation.FormOpenedAt,
            ControlDate = evaluation.ControlDate,
            ControlTime = evaluation.ControlTime,
            ProjectName = !string.IsNullOrEmpty(evaluation.Project?.Code) ? $"{evaluation.Project.Code} - {evaluation.Project.Name}" : evaluation.Project?.Name,
            ChecklistName = evaluation.Project?.Checklist?.Name,
            ScoringMethod = evaluation.Project?.Checklist != null
                ? ScoringMethods.GetById(evaluation.Project.Checklist.ScoringMethodId)?.SystemName
                : null,
            CreatedAt = evaluation.CreatedAt,
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

        // FK değerlerini belirle (0 atanırsa FK constraint violation olur)
        var organizationId = dto.EvaluatedOrganizationId ?? evaluation.EvaluatedOrganizationId ?? 0;
        var requestedByUserId = dto.EvaluatorId ?? evaluation.EvaluatorId ?? 0;

        // CustomerPersonnel ise (EvaluatorId yok), ilk Admin kullanıcısını fallback olarak kullan
        if (requestedByUserId == 0 && dto.EvaluatorCustomerPersonnelId > 0)
        {
            var firstAdmin = await _context.Users
                .Where(u => u.RoleId == UserRoles.Ids.Admin && u.IsActive && !u.IsDeleted)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
            requestedByUserId = firstAdmin;
        }

        // FK'ler geçerli değilse oluşturma (0 = geçersiz FK)
        if (organizationId <= 0 || requestedByUserId <= 0)
        {
            _logger.LogWarning(
                "PersonnelRequest oluşturulamadı: geçersiz FK değerleri (OrganizationId: {OrgId}, RequestedByUserId: {UserId}, EvaluationId: {EvalId})",
                organizationId, requestedByUserId, evaluation.Id);
            return null;
        }

        var personnelRequest = new PersonnelRequest
        {
            EvaluationId = evaluation.Id,
            CustomerId = customerId,
            CustomerOrganizationId = organizationId,
            FirstName = firstName,
            LastName = lastName,
            Title = dto.NewPersonnel.Title?.Trim(),
            Notes = $"Değerlendirme #{evaluation.Id} sırasında oluşturuldu",
            RequestedByUserId = requestedByUserId,
            Status = ApprovalStatuses.Ids.Pending
        };

        _context.PersonnelRequests.Add(personnelRequest);
        await _context.SaveChangesAsync();

        // Admin'lere bildirim gönder (NotificationCreatorService üzerinden - SignalR push + email)
        var notificationCreator = _serviceProvider.GetRequiredService<INotificationCreatorService>();
        var adminIds = await _context.Users
            .Where(u => u.RoleId == UserRoles.Ids.Admin && u.IsActive && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync();

        if (adminIds.Any())
        {
            await notificationCreator.CreateBulkAsync(
                adminIds,
                NotificationTypes.Ids.Info,
                await _localizationService.GetResourceAsync("PersonnelRequest.New"),
                $"{await _localizationService.GetResourceAsync("PersonnelRequest.New")}: {personnelRequest.FullName}",
                actionUrl: $"/UserRequests?tab=personnel&id={personnelRequest.Id}",
                relatedEntityId: personnelRequest.Id,
                relatedEntityType: "PersonnelRequest",
                senderUserId: requestedByUserId);
        }
        return null; // Başarılı, warning yok
    }

    /// <summary>
    /// Puan hesapla - API endpoint için wrapper
    /// Checklist ID'den soruları çeker ve CalculateScoreCore'u çağırır
    /// Kaydetmez, sadece hesaplayıp sonucu döndürür
    /// </summary>
    public async Task<ScoreCalculationResultDto> CalculateScoreAsync(CalculateScoreRequestDto request)
    {
        // Checklist ve sorularını getir
        var checklist = await _context.Checklists
            .Include(c => c.Questions.Where(q => !q.IsDeleted))
                .ThenInclude(q => q.SubCriteria.Where(sc => sc.IsActive))
            .FirstOrDefaultAsync(c => c.Id == request.ChecklistId);

        if (checklist == null)
        {
            return new ScoreCalculationResultDto();
        }

        var questions = checklist.Questions.OrderBy(q => q.Order).ToList();

        // Tek hesaplama noktasını kullan
        return CalculateScoreCore(questions, request.Answers, checklist.ScoringMethodId);
    }

    /// <summary>
    /// Mevcut değerlendirmenin puanını yeniden hesapla ve kaydet
    /// </summary>
    public async Task<(bool Success, string Message)> RecalculateScoreAsync(int evaluationId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
                    .ThenInclude(c => c!.Questions.Where(q => !q.IsDeleted))
                        .ThenInclude(q => q.SubCriteria.Where(sc => sc.IsActive))
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
            .FirstOrDefaultAsync(e => e.Id == evaluationId);

        if (evaluation == null)
            return (false, "Değerlendirme bulunamadı.");

        if (evaluation.StatusId != EvaluationStatuses.Ids.Completed)
            return (false, "Sadece tamamlanmış değerlendirmeler yeniden hesaplanabilir.");

        var checklist = evaluation.Project.Checklist;
        var questions = checklist?.Questions.ToList() ?? new List<Question>();

        if (!questions.Any())
            return (false, "Checklist soruları bulunamadı.");

        // Answer'ları ScoreAnswerDto'ya dönüştür
        var scoreAnswers = evaluation.Answers.Select(a => new ScoreAnswerDto
        {
            QuestionId = a.QuestionId,
            // GivenPoints yoksa AnswerNumeric'i kullan
            GivenPoints = a.GivenPoints ?? a.AnswerNumeric,
            // SubCriteria seçimlerini al (CriteriaTotal modu için)
            SelectedSubCriteriaIds = a.SubCriteriaSelections?.Select(s => s.SubCriteriaId).ToList(),
            ApplyPenalty = a.AppliedPenaltyTypeId > 0,
            SelectedPenaltyType = a.AppliedPenaltyTypeId > 0
                ? PenaltyTypes.GetById(a.AppliedPenaltyTypeId)?.SystemName
                : null
        }).ToList();

        // Yeniden hesapla
        var result = CalculateScoreCore(questions, scoreAnswers, checklist?.ScoringMethodId ?? ScoringMethods.Ids.Maximum);

        // Sonuçları kaydet
        evaluation.TotalScore = result.TotalEarned;
        evaluation.MaxScore = result.MaxPossible;
        evaluation.ScorePercentage = result.Percentage;
        evaluation.YellowCardCount = result.YellowCardCount;
        evaluation.RedCardCount = result.RedCardCount;

        await _context.SaveChangesAsync();

        return (true, $"Puan yeniden hesaplandı: %{result.Percentage:F1}");
    }

    public async Task<List<string>> GetPastDescriptionsAsync()
    {
        var recentDescriptions = await _context.Evaluations
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.DescriptionsJson != null && e.DescriptionsJson != "")
            .OrderByDescending(e => e.CreatedAt)
            .Take(500)
            .Select(e => e.DescriptionsJson!)
            .ToListAsync();

        var allDescriptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var json in recentDescriptions)
        {
            try
            {
                var descriptions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                if (descriptions != null)
                {
                    foreach (var d in descriptions.Where(d => !string.IsNullOrWhiteSpace(d)))
                    {
                        allDescriptions.Add(d.Trim());
                    }
                }
            }
            catch { /* JSON parse hatası - devam et */ }
        }

        return allDescriptions.OrderBy(d => d).ToList();
    }
}
