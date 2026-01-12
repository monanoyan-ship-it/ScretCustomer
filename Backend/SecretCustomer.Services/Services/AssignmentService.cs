using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Assignment;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IChecklistRepository _checklistRepository;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public AssignmentService(
        IAssignmentRepository assignmentRepository,
        IProjectRepository projectRepository,
        IChecklistRepository checklistRepository,
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _assignmentRepository = assignmentRepository;
        _projectRepository = projectRepository;
        _checklistRepository = checklistRepository;
        _context = context;
        _auditLogService = auditLogService;
    }

    #region TEMEL CRUD

    public async Task<AssignmentDto?> GetByIdAsync(int id)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedCustomerPersonnel)
            .Include(a => a.Evaluations)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        return assignment == null ? null : MapToDto(assignment);
    }

    public async Task<AssignmentDetailDto?> GetDetailByIdAsync(int id)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Customer)
            .Include(a => a.Project)
                .ThenInclude(p => p.Organization)
            .Include(a => a.Project)
                .ThenInclude(p => p.Files.Where(f => !f.IsDeleted))
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedCustomerPersonnel)
            .Include(a => a.Evaluations)
                .ThenInclude(e => e.Evaluator)
            .Include(a => a.Periods)
                .ThenInclude(p => p.Evaluations)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (assignment == null) return null;

        var dto = MapToDto(assignment);
        var detailDto = new AssignmentDetailDto
        {
            Id = dto.Id,
            ProjectId = dto.ProjectId,
            ProjectName = dto.ProjectName,
            ProjectCode = dto.ProjectCode,
            ChecklistId = dto.ChecklistId,
            ChecklistName = dto.ChecklistName,
            AssignedUserId = dto.AssignedUserId,
            AssignedUserName = dto.AssignedUserName,
            AssignedCustomerPersonnelId = dto.AssignedCustomerPersonnelId,
            AssignedCustomerPersonnelName = dto.AssignedCustomerPersonnelName,
            ExternalEmail = dto.ExternalEmail,
            ExternalName = dto.ExternalName,
            UniqueLink = dto.UniqueLink,
            DueDate = dto.DueDate,
            IsCompleted = dto.IsCompleted,
            CompletedAt = dto.CompletedAt,
            CreatedAt = dto.CreatedAt,
            Status = dto.Status,
            EvaluationId = dto.EvaluationId,
            EvaluationStatus = dto.EvaluationStatus,
            EvaluationScore = dto.EvaluationScore,
            YellowCardCount = dto.YellowCardCount,
            RedCardCount = dto.RedCardCount,
            // Proje Firma/Organizasyon Bilgileri
            CustomerName = assignment.Project?.Customer?.CompanyName,
            OrganizationName = assignment.Project?.Organization?.Name
        };

        // Get evaluation details
        var evaluation = assignment.Evaluations.FirstOrDefault();
        if (evaluation != null)
        {
            detailDto.EvaluatorName = evaluation.Evaluator != null
                ? $"{evaluation.Evaluator.FirstName} {evaluation.Evaluator.LastName}"
                : null;
            detailDto.EvaluationDate = evaluation.CreatedAt;
            detailDto.EvaluationNotes = evaluation.Notes;
        }

        // Get periods with statistics
        detailDto.Periods = assignment.Periods
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new AssignmentPeriodSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = PeriodStatuses.GetById(p.StatusId)?.SystemName ?? "",
                TargetCount = p.TargetCount,
                CompletedCount = p.Evaluations.Count(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed),
                AverageScore = p.Evaluations
                    .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed && e.ScorePercentage.HasValue)
                    .Average(e => e.ScorePercentage)
            })
            .ToList();

        // Get project files
        if (assignment.Project?.Files != null)
        {
            detailDto.ProjectFiles = assignment.Project.Files
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new ProjectFileInfoDto
                {
                    Id = f.Id,
                    OriginalFileName = f.OriginalFileName,
                    ContentType = f.ContentType,
                    FileSize = f.FileSize,
                    Description = f.Description
                })
                .ToList();
        }

        return detailDto;
    }

    public async Task<IEnumerable<AssignmentDto>> GetAllAsync()
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedCustomerPersonnel)
            .Include(a => a.Evaluations)
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    public async Task<AssignmentDto?> GetByUniqueLinkAsync(string uniqueLink)
    {
        var assignment = await _assignmentRepository.GetByUniqueLinkAsync(uniqueLink, includeDetails: true);
        return assignment == null ? null : MapToDto(assignment);
    }

    public async Task<AssignmentDto> CreateAsync(CreateAssignmentDto dto)
    {
        await ValidateAsync(dto);

        // External müşteri için daha önce atama yapılmış mı kontrol et
        if (!string.IsNullOrEmpty(dto.ExternalEmail))
        {
            var exists = await _assignmentRepository.ExistsByEmailAsync(dto.ProjectId, dto.ExternalEmail);
            if (exists)
                throw new InvalidOperationException($"Bu proje için {dto.ExternalEmail} adresine zaten atama yapılmış.");
        }

        var assignment = new Assignment
        {
            ProjectId = dto.ProjectId,
            ChecklistId = dto.ChecklistId,
            AssignedUserId = dto.AssignedUserId,
            AssignedCustomerPersonnelId = dto.AssignedCustomerPersonnelId,
            ExternalEmail = dto.ExternalEmail,
            ExternalName = dto.ExternalName,
            UniqueLink = Guid.NewGuid().ToString(),
            DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc),
            TypeId = AssignmentTypes.Ids.InternalUser, // Bizim değerlendirmelerimiz
            IsCompleted = false
        };

        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(assignment.Id) ?? MapToDto(assignment);
    }

    public async Task UpdateAsync(int id, UpdateAssignmentDto dto)
    {
        var assignment = await _context.Assignments.FindAsync(id);
        if (assignment == null || assignment.IsDeleted)
            throw new KeyNotFoundException($"Atama bulunamadı: {id}");

        assignment.ProjectId = dto.ProjectId;
        assignment.ChecklistId = dto.ChecklistId;
        assignment.AssignedUserId = dto.AssignedUserId;
        assignment.AssignedCustomerPersonnelId = dto.AssignedCustomerPersonnelId;
        assignment.ExternalEmail = dto.ExternalEmail;
        assignment.ExternalName = dto.ExternalName;
        assignment.DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc);
        assignment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var assignment = await _context.Assignments.FindAsync(id);
        if (assignment == null) return false;

        assignment.IsDeleted = true;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region FİLTRELEME

    public async Task<IEnumerable<AssignmentDto>> GetByProjectIdAsync(int projectId)
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedCustomerPersonnel)
            .Include(a => a.Evaluations)
            .Where(a => a.ProjectId == projectId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    public async Task<IEnumerable<AssignmentDto>> GetByUserIdAsync(int userId)
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Include(a => a.Evaluations)
            .Include(a => a.Periods)
            .Where(a => a.AssignedUserId == userId && !a.IsDeleted)
            // Sıralama: DueDate en uzak (gelecekte) olan en üstte
            .OrderByDescending(a => a.DueDate)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    public async Task<IEnumerable<AssignmentDto>> GetByCustomerPersonnelIdAsync(int customerPersonnelId)
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedCustomerPersonnel)
            .Include(a => a.Evaluations)
            .Include(a => a.Periods)
            .Where(a => a.AssignedCustomerPersonnelId == customerPersonnelId && !a.IsDeleted)
            // Sıralama: Bekleyenler önce, sonra DueDate'e göre
            .OrderBy(a => a.IsCompleted)
            .ThenBy(a => a.DueDate)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    public async Task<IEnumerable<AssignmentDto>> GetFilteredAsync(AssignmentFilterDto filter)
    {
        var query = _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedCustomerPersonnel)
            .Include(a => a.Evaluations)
            .Where(a => !a.IsDeleted)
            .AsQueryable();

        if (filter.ProjectId.HasValue)
            query = query.Where(a => a.ProjectId == filter.ProjectId.Value);

        if (filter.AssignedUserId.HasValue)
            query = query.Where(a => a.AssignedUserId == filter.AssignedUserId.Value);

        // Status filtrelemesi
        if (!string.IsNullOrEmpty(filter.Status))
        {
            var now = DateTime.UtcNow;
            query = filter.Status switch
            {
                "Pending" => query.Where(a => !a.IsCompleted && !a.IsCancelled && a.DueDate >= now),
                "InProgress" => query.Where(a => !a.IsCompleted && !a.IsCancelled && a.DueDate >= now),
                "Completed" => query.Where(a => a.IsCompleted),
                "Expired" => query.Where(a => !a.IsCompleted && !a.IsCancelled && a.DueDate < now),
                "Cancelled" => query.Where(a => a.IsCancelled),
                _ => query
            };
        }

        if (filter.IsCompleted.HasValue)
            query = query.Where(a => a.IsCompleted == filter.IsCompleted.Value);

        if (filter.DueDateFrom.HasValue)
            query = query.Where(a => a.DueDate >= filter.DueDateFrom.Value);

        if (filter.DueDateTo.HasValue)
            query = query.Where(a => a.DueDate <= filter.DueDateTo.Value);

        if (filter.IsExpired == true)
            query = query.Where(a => !a.IsCompleted && a.DueDate < DateTime.UtcNow);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(a =>
                (a.Project != null && a.Project.Name.ToLower().Contains(term)) ||
                (a.Checklist != null && a.Checklist.Name.ToLower().Contains(term)) ||
                (a.AssignedUser != null && (a.AssignedUser.FirstName + " " + a.AssignedUser.LastName).ToLower().Contains(term)) ||
                (a.ExternalEmail != null && a.ExternalEmail.ToLower().Contains(term)) ||
                (a.ExternalName != null && a.ExternalName.ToLower().Contains(term)));
        }

        // Dynamic sorting
        var isAscending = filter.SortDirection?.ToLower() == "asc";
        IOrderedQueryable<Assignment> orderedQuery = filter.SortBy?.ToLower() switch
        {
            "projectname" => isAscending
                ? query.OrderBy(a => a.Project != null ? a.Project.Name : null)
                : query.OrderByDescending(a => a.Project != null ? a.Project.Name : null),
            "checklistname" => isAscending
                ? query.OrderBy(a => a.Checklist != null ? a.Checklist.Name : null)
                : query.OrderByDescending(a => a.Checklist != null ? a.Checklist.Name : null),
            "assigneename" => isAscending
                ? query.OrderBy(a => a.AssignedUser != null ? a.AssignedUser.FirstName : a.ExternalName)
                : query.OrderByDescending(a => a.AssignedUser != null ? a.AssignedUser.FirstName : a.ExternalName),
            "duedate" => isAscending ? query.OrderBy(a => a.DueDate) : query.OrderByDescending(a => a.DueDate),
            "status" => isAscending ? query.OrderBy(a => a.IsCompleted) : query.OrderByDescending(a => a.IsCompleted),
            "score" => isAscending
                ? query.OrderBy(a => a.Evaluations.OrderByDescending(e => e.CreatedAt).FirstOrDefault() != null
                    ? a.Evaluations.OrderByDescending(e => e.CreatedAt).FirstOrDefault()!.TotalScore : 0)
                : query.OrderByDescending(a => a.Evaluations.OrderByDescending(e => e.CreatedAt).FirstOrDefault() != null
                    ? a.Evaluations.OrderByDescending(e => e.CreatedAt).FirstOrDefault()!.TotalScore : 0),
            _ => query.OrderByDescending(a => a.CreatedAt) // Default
        };

        var assignments = await orderedQuery.ToListAsync();
        return assignments.Select(MapToDto);
    }

    #endregion

    #region DURUM YÖNETİMİ

    public async Task<AssignmentDto> CompleteAssignmentAsync(int id)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null || assignment.IsDeleted)
            throw new KeyNotFoundException($"Atama bulunamadı: {id}");

        assignment.IsCompleted = true;
        assignment.CompletedAt = DateTime.UtcNow;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit Log
        await _auditLogService.LogInfoAsync(
            $"Atama tamamlandı: {assignment.Project?.Name}",
            "AssignmentService");

        return await GetByIdAsync(id) ?? throw new KeyNotFoundException("Atama bulunamadı");
    }

    public async Task<AssignmentDto> CancelAssignmentAsync(int id, string? reason)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null || assignment.IsDeleted)
            throw new KeyNotFoundException($"Atama bulunamadı: {id}");

        assignment.IsDeleted = true;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit Log
        await _auditLogService.LogWarningAsync(
            $"Atama iptal edildi: {assignment.Project?.Name} - Sebep: {reason ?? "Belirtilmedi"}",
            "AssignmentService");

        return await GetByIdAsync(id) ?? throw new KeyNotFoundException("Atama bulunamadı");
    }

    public async Task<AssignmentDto> ReopenAssignmentAsync(int id)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Evaluations)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null || assignment.IsDeleted)
            throw new KeyNotFoundException($"Atama bulunamadı: {id}");

        if (!assignment.IsCompleted)
            throw new InvalidOperationException("Bu atama zaten açık durumda.");

        // Assignment'ı yeniden aç
        assignment.IsCompleted = false;
        assignment.CompletedAt = null;
        assignment.UpdatedAt = DateTime.UtcNow;

        // İlişkili Evaluation'ları da InProgress yap
        foreach (var evaluation in assignment.Evaluations)
        {
            if (evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            {
                evaluation.StatusId = EvaluationStatuses.Ids.InProgress;
                evaluation.CompletedAt = null;
                evaluation.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        // Audit Log
        await _auditLogService.LogInfoAsync(
            $"Atama yeniden açıldı: {assignment.Project?.Name}",
            "AssignmentService");

        return await GetByIdAsync(id) ?? throw new KeyNotFoundException("Atama bulunamadı");
    }

    public async Task<AssignmentDto> ReassignAsync(int id, ReassignAssignmentDto dto)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null || assignment.IsDeleted)
            throw new KeyNotFoundException($"Atama bulunamadı: {id}");

        if (assignment.IsCompleted)
            throw new InvalidOperationException("Tamamlanmış atama yeniden atanamaz.");

        if (dto.NewAssignedUserId.HasValue)
            assignment.AssignedUserId = dto.NewAssignedUserId;

        if (dto.NewAssignedCustomerPersonnelId.HasValue)
            assignment.AssignedCustomerPersonnelId = dto.NewAssignedCustomerPersonnelId;

        if (!string.IsNullOrEmpty(dto.NewExternalEmail))
        {
            assignment.ExternalEmail = dto.NewExternalEmail;
            assignment.ExternalName = dto.NewExternalName;
        }

        if (dto.NewDueDate.HasValue)
            assignment.DueDate = DateTime.SpecifyKind(dto.NewDueDate.Value, DateTimeKind.Utc);

        assignment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit Log
        await _auditLogService.LogInfoAsync(
            $"Atama yeniden atandı: {assignment.Project?.Name}",
            "AssignmentService");

        return await GetByIdAsync(id) ?? throw new KeyNotFoundException("Atama bulunamadı");
    }

    public async Task<AssignmentDto> UpdateDueDateAsync(int id, DateTime newDueDate)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment == null || assignment.IsDeleted)
            throw new KeyNotFoundException($"Atama bulunamadı: {id}");

        if (assignment.IsCompleted)
            throw new InvalidOperationException("Tamamlanmış atamanın tarihi değiştirilemez.");

        assignment.DueDate = DateTime.SpecifyKind(newDueDate, DateTimeKind.Utc);
        assignment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit Log
        await _auditLogService.LogInfoAsync(
            $"Atama tarihi güncellendi: {assignment.Project?.Name} - Yeni tarih: {newDueDate:dd.MM.yyyy}",
            "AssignmentService");

        return await GetByIdAsync(id) ?? throw new KeyNotFoundException("Atama bulunamadı");
    }

    #endregion

    #region TOPLU İŞLEMLER

    public async Task<int> DeleteByProjectIdAsync(int projectId)
    {
        var assignments = await _context.Assignments
            .Where(a => a.ProjectId == projectId && !a.IsDeleted && !a.IsCompleted)
            .ToListAsync();

        foreach (var assignment in assignments)
        {
            assignment.IsDeleted = true;
            assignment.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return assignments.Count;
    }

    #endregion

    #region İSTATİSTİKLER

    public async Task<AssignmentSummaryDto> GetSummaryAsync(int? projectId = null)
    {
        var query = _context.Assignments
            .Include(a => a.Evaluations)
            .Where(a => !a.IsDeleted);

        if (projectId.HasValue)
            query = query.Where(a => a.ProjectId == projectId.Value);

        var assignments = await query.ToListAsync();
        var now = DateTime.UtcNow;

        var completed = assignments.Where(a => a.IsCompleted).ToList();
        var evaluationScores = completed
            .SelectMany(a => a.Evaluations)
            .Where(e => e.ScorePercentage.HasValue)
            .Select(e => e.ScorePercentage!.Value)
            .ToList();

        return new AssignmentSummaryDto
        {
            TotalAssignments = assignments.Count,
            PendingCount = assignments.Count(a => !a.IsCompleted && a.DueDate >= now),
            InProgressCount = assignments.Count(a => !a.IsCompleted && a.Evaluations.Any(e => e.StatusId == EvaluationStatuses.Ids.Draft)),
            CompletedCount = completed.Count,
            ExpiredCount = assignments.Count(a => !a.IsCompleted && a.DueDate < now),
            CancelledCount = 0, // IsDeleted ones are not included
            CompletionRate = assignments.Count > 0 ? Math.Round((decimal)completed.Count / assignments.Count * 100, 1) : 0,
            AverageScore = evaluationScores.Any() ? Math.Round(evaluationScores.Average(), 1) : 0,
            TotalYellowCards = completed.SelectMany(a => a.Evaluations).Sum(e => e.YellowCardCount),
            TotalRedCards = completed.SelectMany(a => a.Evaluations).Sum(e => e.RedCardCount)
        };
    }

    public async Task<IEnumerable<ProjectAssignmentSummaryDto>> GetProjectSummariesAsync()
    {
        var projects = await _context.Projects
            .Include(p => p.Assignments.Where(a => !a.IsDeleted))
            .Where(p => !p.IsDeleted && p.IsActive)
            .ToListAsync();

        return projects.Select(p => new ProjectAssignmentSummaryDto
        {
            ProjectId = p.Id,
            ProjectName = p.Name,
            TotalAssignments = p.Assignments.Count,
            CompletedAssignments = p.Assignments.Count(a => a.IsCompleted),
            PendingAssignments = p.Assignments.Count(a => !a.IsCompleted),
            CompletionPercentage = p.Assignments.Count > 0
                ? Math.Round((decimal)p.Assignments.Count(a => a.IsCompleted) / p.Assignments.Count * 100, 1)
                : 0
        });
    }

    #endregion

    #region SÜRESI DOLANLAR

    public async Task<IEnumerable<AssignmentDto>> GetExpiredAsync()
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Where(a => !a.IsDeleted && !a.IsCompleted && a.DueDate < DateTime.UtcNow)
            .OrderBy(a => a.DueDate)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    public async Task<IEnumerable<AssignmentDto>> GetUpcomingDueAsync(int daysAhead = 3)
    {
        var deadline = DateTime.UtcNow.AddDays(daysAhead);
        var assignments = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Where(a => !a.IsDeleted && !a.IsCompleted && a.DueDate >= DateTime.UtcNow && a.DueDate <= deadline)
            .OrderBy(a => a.DueDate)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    #endregion

    #region PRIVATE HELPERS

    private async Task ValidateAsync(CreateAssignmentDto dto)
    {
        var projectExists = await _projectRepository.ExistsAsync(dto.ProjectId);
        if (!projectExists)
            throw new KeyNotFoundException($"Proje bulunamadı: {dto.ProjectId}");

        var checklistExists = await _checklistRepository.ExistsAsync(dto.ChecklistId);
        if (!checklistExists)
            throw new KeyNotFoundException($"Kontrol listesi bulunamadı: {dto.ChecklistId}");
    }

    private AssignmentDto MapToDto(Assignment assignment)
    {
        var evaluation = assignment.Evaluations?.FirstOrDefault();
        var status = assignment.IsCompleted ? "Completed"
            : assignment.DueDate < DateTime.UtcNow ? "Expired"
            : evaluation != null && evaluation.StatusId == EvaluationStatuses.Ids.Draft ? "InProgress"
            : "Pending";

        return new AssignmentDto
        {
            Id = assignment.Id,
            ProjectId = assignment.ProjectId,
            ProjectName = assignment.Project?.Name ?? "",
            ProjectCode = assignment.Project?.Code,
            ChecklistId = assignment.ChecklistId,
            ChecklistName = assignment.Checklist?.Name ?? "",
            AssignedUserId = assignment.AssignedUserId,
            AssignedUserName = assignment.AssignedUser != null
                ? $"{assignment.AssignedUser.FirstName} {assignment.AssignedUser.LastName}"
                : null,
            AssignedCustomerPersonnelId = assignment.AssignedCustomerPersonnelId,
            AssignedCustomerPersonnelName = assignment.AssignedCustomerPersonnel != null
                ? $"{assignment.AssignedCustomerPersonnel.FirstName} {assignment.AssignedCustomerPersonnel.LastName}"
                : null,
            ExternalEmail = assignment.ExternalEmail,
            ExternalName = assignment.ExternalName,
            UniqueLink = assignment.UniqueLink,
            DueDate = assignment.DueDate,
            IsCompleted = assignment.IsCompleted,
            CompletedAt = assignment.CompletedAt,
            CreatedAt = assignment.CreatedAt,
            Status = status,
            EvaluationId = evaluation?.Id,
            EvaluationStatus = evaluation != null ? EvaluationStatuses.GetById(evaluation.StatusId)?.SystemName ?? "" : null,
            EvaluationScore = evaluation?.ScorePercentage,
            YellowCardCount = evaluation?.YellowCardCount ?? 0,
            RedCardCount = evaluation?.RedCardCount ?? 0,
            EvaluationCount = assignment.Evaluations?.Count ?? 0
        };
    }

    #endregion

    #region DÖNEMLER (PERIODS)

    public async Task<IEnumerable<Core.DTOs.AssignmentPeriod.AssignmentPeriodDto>> GetPeriodsAsync(int assignmentId)
    {
        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted);

        if (assignment == null)
            throw new KeyNotFoundException("Atama bulunamadı");

        var periods = await _context.AssignmentPeriods
            .Include(p => p.Evaluations)
            .Include(p => p.CreatedByUser)
            .Where(p => p.AssignmentId == assignmentId && !p.IsDeleted)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        return periods.Select(MapPeriodToDto);
    }

    public async Task<Core.DTOs.AssignmentPeriod.AssignmentPeriodDto> CreatePeriodAsync(Core.DTOs.AssignmentPeriod.CreateAssignmentPeriodDto dto)
    {
        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId && !a.IsDeleted);

        if (assignment == null)
            throw new KeyNotFoundException("Atama bulunamadı");

        // Tarihleri UTC'ye çevir
        var startDateUtc = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
        var endDateUtc = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc);

        // Aynı tarih aralığında dönem var mı kontrol et
        var overlapping = await _context.AssignmentPeriods
            .AnyAsync(p => p.AssignmentId == dto.AssignmentId && !p.IsDeleted &&
                ((startDateUtc >= p.StartDate && startDateUtc <= p.EndDate) ||
                 (endDateUtc >= p.StartDate && endDateUtc <= p.EndDate)));

        if (overlapping)
            throw new InvalidOperationException("Bu tarih aralığında zaten bir dönem mevcut");

        var period = new Core.Entities.AssignmentPeriod
        {
            AssignmentId = dto.AssignmentId,
            Name = dto.Name,
            StartDate = startDateUtc,
            EndDate = endDateUtc,
            TargetCount = dto.TargetCount,
            Notes = dto.Notes,
            StatusId = PeriodStatuses.Ids.Open,
            CreatedAt = DateTime.UtcNow
        };

        _context.AssignmentPeriods.Add(period);
        await _context.SaveChangesAsync();

        // Reload to get the ID
        return new Core.DTOs.AssignmentPeriod.AssignmentPeriodDto
        {
            Id = period.Id,
            AssignmentId = period.AssignmentId,
            Name = period.Name,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            Status = PeriodStatuses.GetById(period.StatusId)?.SystemName ?? "",
            StatusName = period.StatusId == PeriodStatuses.Ids.Open ? "Açık" : "Kapalı",
            TargetCount = period.TargetCount,
            CompletedCount = 0,
            AverageScore = null,
            Notes = period.Notes,
            CreatedByUserId = period.CreatedByUserId,
            CreatedByUserName = null,
            CreatedAt = period.CreatedAt
        };
    }

    public async Task<Core.DTOs.AssignmentPeriod.AssignmentPeriodDto> UpdatePeriodAsync(Core.DTOs.AssignmentPeriod.UpdateAssignmentPeriodDto dto)
    {
        var period = await _context.AssignmentPeriods
            .Include(p => p.Evaluations)
            .FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted);

        if (period == null)
            throw new KeyNotFoundException("Dönem bulunamadı");

        period.Name = dto.Name;
        period.StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
        period.EndDate = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc);
        period.TargetCount = dto.TargetCount;
        period.Notes = dto.Notes;
        period.StatusId = PeriodStatuses.GetBySystemName(dto.Status)?.Id ?? PeriodStatuses.Ids.Open;
        period.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapPeriodToDto(period);
    }

    public async Task<Core.DTOs.AssignmentPeriod.AssignmentPeriodDto> ClosePeriodAsync(int periodId)
    {
        var period = await _context.AssignmentPeriods
            .Include(p => p.Evaluations)
            .FirstOrDefaultAsync(p => p.Id == periodId && !p.IsDeleted);

        if (period == null)
            throw new KeyNotFoundException("Dönem bulunamadı");

        period.StatusId = PeriodStatuses.Ids.Closed;
        period.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapPeriodToDto(period);
    }

    public async Task<Core.DTOs.AssignmentPeriod.AssignmentPeriodDto> ReopenPeriodAsync(int periodId)
    {
        var period = await _context.AssignmentPeriods
            .Include(p => p.Evaluations)
            .FirstOrDefaultAsync(p => p.Id == periodId && !p.IsDeleted);

        if (period == null)
            throw new KeyNotFoundException("Dönem bulunamadı");

        period.StatusId = PeriodStatuses.Ids.Open;
        period.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapPeriodToDto(period);
    }

    public async Task DeletePeriodAsync(int periodId)
    {
        var period = await _context.AssignmentPeriods
            .Include(p => p.Evaluations)
            .FirstOrDefaultAsync(p => p.Id == periodId && !p.IsDeleted);

        if (period == null)
            throw new KeyNotFoundException("Dönem bulunamadı");

        if (period.Evaluations.Any(e => !e.IsDeleted))
            throw new InvalidOperationException("Bu dönemde değerlendirmeler var, silinemez");

        period.IsDeleted = true;
        period.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private Core.DTOs.AssignmentPeriod.AssignmentPeriodDto MapPeriodToDto(Core.Entities.AssignmentPeriod period)
    {
        var completedEvaluations = period.Evaluations?
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed)
            .ToList() ?? new List<Core.Entities.Evaluation>();

        return new Core.DTOs.AssignmentPeriod.AssignmentPeriodDto
        {
            Id = period.Id,
            AssignmentId = period.AssignmentId,
            Name = period.Name,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            Status = PeriodStatuses.GetById(period.StatusId)?.SystemName ?? "",
            StatusName = period.StatusId == PeriodStatuses.Ids.Open ? "Açık" : "Kapalı",
            TargetCount = period.TargetCount,
            CompletedCount = completedEvaluations.Count,
            AverageScore = completedEvaluations.Any(e => e.ScorePercentage.HasValue)
                ? completedEvaluations.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage)
                : null,
            Notes = period.Notes,
            CreatedByUserId = period.CreatedByUserId,
            CreatedByUserName = period.CreatedByUser != null
                ? $"{period.CreatedByUser.FirstName} {period.CreatedByUser.LastName}"
                : null,
            CreatedAt = period.CreatedAt
        };
    }

    #endregion

    #region İÇ DEĞERLENDİRME ATAMALARI

    public async Task<IEnumerable<AssignmentDto>> GetInternalAssignmentsAsync(InternalAssignmentFilterDto filter)
    {
        var query = _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedCustomerPersonnel)
            .Where(a => !a.IsDeleted && a.AssignedCustomerPersonnelId != null);

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(a => a.Project != null && a.Project.CustomerId == filter.CustomerId);
        }

        if (filter.ProjectId.HasValue)
        {
            query = query.Where(a => a.ProjectId == filter.ProjectId);
        }

        if (filter.AssignedCustomerPersonnelId.HasValue)
        {
            query = query.Where(a => a.AssignedCustomerPersonnelId == filter.AssignedCustomerPersonnelId);
        }

        if (filter.IsCompleted.HasValue)
        {
            query = query.Where(a => a.IsCompleted == filter.IsCompleted.Value);
        }

        if (filter.DueDateFrom.HasValue)
        {
            query = query.Where(a => a.DueDate >= filter.DueDateFrom);
        }

        if (filter.DueDateTo.HasValue)
        {
            query = query.Where(a => a.DueDate <= filter.DueDateTo);
        }

        var assignments = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        return assignments.Select(MapToDto);
    }

    public async Task<InternalAssignmentSummaryDto> GetInternalAssignmentSummaryAsync(int? customerId)
    {
        var query = _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p!.Customer)
            .Where(a => !a.IsDeleted && a.AssignedCustomerPersonnelId != null);

        if (customerId.HasValue)
        {
            query = query.Where(a => a.Project != null && a.Project.CustomerId == customerId);
        }

        var assignments = await query.ToListAsync();

        var customer = customerId.HasValue
            ? await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId)
            : null;

        return new InternalAssignmentSummaryDto
        {
            CustomerId = customerId ?? 0,
            CustomerName = customer?.CompanyName ?? "Tüm Müşteriler",
            TotalAssignments = assignments.Count,
            CompletedAssignments = assignments.Count(a => a.IsCompleted),
            PendingAssignments = assignments.Count(a => !a.IsCompleted),
            OverdueAssignments = assignments.Count(a => a.DueDate < DateTime.UtcNow && !a.IsCompleted),
            CompletionRate = assignments.Count > 0
                ? Math.Round((decimal)assignments.Count(a => a.IsCompleted) / assignments.Count * 100, 1)
                : 0
        };
    }

    public async Task<InternalAssignmentResultDto> CreateInternalAssignmentsAsync(CreateInternalAssignmentsDto dto)
    {
        var result = new InternalAssignmentResultDto();

        // Proje kontrolü
        var project = await _context.Projects
            .Include(p => p.Checklist)
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId && !p.IsDeleted);

        if (project == null)
            throw new KeyNotFoundException("Proje bulunamadı");

        if (project.Checklist == null || project.Checklist.IsDeleted)
            throw new KeyNotFoundException("Projeye ait checklist bulunamadı");

        var checklistId = project.ChecklistId;

        // Personel listesi
        var personnelQuery = _context.CustomerPersonnel
            .Where(cp => cp.CustomerId == dto.CustomerId && !cp.IsDeleted && cp.IsActive);

        if (dto.PersonnelIds != null && dto.PersonnelIds.Any())
        {
            personnelQuery = personnelQuery.Where(cp => dto.PersonnelIds.Contains(cp.Id));
        }
        else if (dto.RoleFilterId.HasValue)
        {
            personnelQuery = personnelQuery.Where(cp => cp.RoleId == dto.RoleFilterId);
        }

        if (dto.OrganizationId.HasValue)
        {
            personnelQuery = personnelQuery.Where(cp => cp.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == dto.OrganizationId));
        }

        var personnelList = await personnelQuery.ToListAsync();
        result.TotalRequested = personnelList.Count;

        foreach (var personnel in personnelList)
        {
            try
            {
                // Aynı kişiye aynı proje zaten atanmış mı?
                var existingAssignment = await _context.Assignments
                    .FirstOrDefaultAsync(a => a.ProjectId == dto.ProjectId
                        && a.AssignedCustomerPersonnelId == personnel.Id
                        && !a.IsDeleted
                        && !a.IsCompleted);

                if (existingAssignment != null)
                {
                    result.SkippedCount++;
                    result.Results.Add(new InternalAssignmentItemResult
                    {
                        PersonnelId = personnel.Id,
                        PersonnelName = $"{personnel.FirstName} {personnel.LastName}",
                        Status = "Skipped",
                        Message = "Zaten aktif ataması var"
                    });
                    continue;
                }

                var assignment = new Assignment
                {
                    ProjectId = dto.ProjectId,
                    ChecklistId = checklistId,
                    AssignedCustomerPersonnelId = personnel.Id,
                    DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc),
                    TypeId = AssignmentTypes.Ids.CustomerPersonnel,
                    UniqueLink = Guid.NewGuid().ToString("N"),
                    CreatedAt = DateTime.UtcNow,
                    IsCompleted = false,
                    IsDeleted = false
                };

                _context.Assignments.Add(assignment);
                await _context.SaveChangesAsync();

                result.SuccessCount++;
                result.Results.Add(new InternalAssignmentItemResult
                {
                    PersonnelId = personnel.Id,
                    PersonnelName = $"{personnel.FirstName} {personnel.LastName}",
                    AssignmentId = assignment.Id,
                    Status = "Created",
                    Message = "Atama oluşturuldu"
                });
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                var errorMessage = ExceptionHelper.GetFullExceptionChain(ex);
                result.Errors.Add($"{personnel.FirstName} {personnel.LastName}: {errorMessage}");
                result.Results.Add(new InternalAssignmentItemResult
                {
                    PersonnelId = personnel.Id,
                    PersonnelName = $"{personnel.FirstName} {personnel.LastName}",
                    Status = "Failed",
                    Message = errorMessage
                });
            }
        }

        return result;
    }

    #endregion
}
