using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Assignment;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
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
            .Include(a => a.Branch)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedFieldWorker)
            .Include(a => a.AssignedCustomerPersonnel)
            .Include(a => a.Evaluations)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        return assignment == null ? null : MapToDto(assignment);
    }

    public async Task<AssignmentDetailDto?> GetDetailByIdAsync(int id)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.Branch)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedFieldWorker)
            .Include(a => a.AssignedCustomerPersonnel)
            .Include(a => a.Evaluations)
                .ThenInclude(e => e.Evaluator)
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
            BranchId = dto.BranchId,
            BranchName = dto.BranchName,
            BranchCode = dto.BranchCode,
            AssignedUserId = dto.AssignedUserId,
            AssignedUserName = dto.AssignedUserName,
            AssignedFieldWorkerId = dto.AssignedFieldWorkerId,
            AssignedFieldWorkerName = dto.AssignedFieldWorkerName,
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
            RedCardCount = dto.RedCardCount
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

        return detailDto;
    }

    public async Task<IEnumerable<AssignmentDto>> GetAllAsync()
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.Branch)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedFieldWorker)
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
            BranchId = dto.BranchId,
            AssignedUserId = dto.AssignedUserId,
            AssignedFieldWorkerId = dto.AssignedFieldWorkerId,
            AssignedCustomerPersonnelId = dto.AssignedCustomerPersonnelId,
            ExternalEmail = dto.ExternalEmail,
            ExternalName = dto.ExternalName,
            UniqueLink = Guid.NewGuid().ToString(),
            DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc),
            IsCompleted = false
        };

        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(assignment.Id) ?? MapToDto(assignment);
    }

    public async Task<IEnumerable<AssignmentDto>> CreateBulkAsync(BulkAssignmentDto dto)
    {
        // Validate project and checklist
        var projectExists = await _projectRepository.ExistsAsync(dto.ProjectId);
        if (!projectExists)
            throw new KeyNotFoundException($"Proje bulunamadı: {dto.ProjectId}");

        var checklistExists = await _checklistRepository.ExistsAsync(dto.ChecklistId);
        if (!checklistExists)
            throw new KeyNotFoundException($"Kontrol listesi bulunamadı: {dto.ChecklistId}");

        var assignments = dto.Assignments.Select(a => new Assignment
        {
            ProjectId = dto.ProjectId,
            ChecklistId = dto.ChecklistId,
            BranchId = a.BranchId,
            AssignedUserId = a.AssignedUserId,
            AssignedFieldWorkerId = a.AssignedFieldWorkerId,
            ExternalEmail = a.ExternalEmail,
            ExternalName = a.ExternalName,
            UniqueLink = Guid.NewGuid().ToString(),
            DueDate = DateTime.SpecifyKind(a.DueDate, DateTimeKind.Utc),
            IsCompleted = false
        }).ToList();

        await _context.Assignments.AddRangeAsync(assignments);
        await _context.SaveChangesAsync();

        return await GetByProjectIdAsync(dto.ProjectId);
    }

    public async Task UpdateAsync(int id, UpdateAssignmentDto dto)
    {
        var assignment = await _context.Assignments.FindAsync(id);
        if (assignment == null || assignment.IsDeleted)
            throw new KeyNotFoundException($"Atama bulunamadı: {id}");

        assignment.ProjectId = dto.ProjectId;
        assignment.ChecklistId = dto.ChecklistId;
        assignment.BranchId = dto.BranchId;
        assignment.AssignedUserId = dto.AssignedUserId;
        assignment.AssignedFieldWorkerId = dto.AssignedFieldWorkerId;
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
            .Include(a => a.Branch)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedFieldWorker)
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
            .Include(a => a.Branch)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedFieldWorker)
            .Include(a => a.Evaluations)
            .Where(a => (a.AssignedUserId == userId || a.AssignedFieldWorker!.UserId == userId) && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    public async Task<IEnumerable<AssignmentDto>> GetByBranchIdAsync(int branchId)
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.Branch)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedFieldWorker)
            .Include(a => a.Evaluations)
            .Where(a => a.BranchId == branchId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    public async Task<IEnumerable<AssignmentDto>> GetByFieldWorkerIdAsync(int fieldWorkerId)
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.Branch)
            .Include(a => a.AssignedFieldWorker)
            .Include(a => a.Evaluations)
            .Where(a => a.AssignedFieldWorkerId == fieldWorkerId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    public async Task<IEnumerable<AssignmentDto>> GetFilteredAsync(AssignmentFilterDto filter)
    {
        var query = _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.Branch)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedFieldWorker)
            .Include(a => a.AssignedCustomerPersonnel)
            .Include(a => a.Evaluations)
            .Where(a => !a.IsDeleted)
            .AsQueryable();

        if (filter.ProjectId.HasValue)
            query = query.Where(a => a.ProjectId == filter.ProjectId.Value);

        if (filter.BranchId.HasValue)
            query = query.Where(a => a.BranchId == filter.BranchId.Value);

        if (filter.AssignedUserId.HasValue)
            query = query.Where(a => a.AssignedUserId == filter.AssignedUserId.Value);

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
                (a.Branch != null && a.Branch.Name.ToLower().Contains(term)) ||
                (a.ExternalEmail != null && a.ExternalEmail.ToLower().Contains(term)) ||
                (a.ExternalName != null && a.ExternalName.ToLower().Contains(term)));
        }

        var assignments = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
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
            $"Atama tamamlandı: {assignment.Project?.Name} - {assignment.Branch?.Name ?? "Şube yok"}",
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

        if (dto.NewAssignedFieldWorkerId.HasValue)
            assignment.AssignedFieldWorkerId = dto.NewAssignedFieldWorkerId;

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

    #endregion

    #region TOPLU İŞLEMLER

    public async Task<IEnumerable<AssignmentDto>> CreateForProjectBranchesAsync(BulkProjectAssignmentDto dto)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectBranches)
                .ThenInclude(pb => pb.Branch)
            .Include(p => p.Checklist)
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId && !p.IsDeleted);

        if (project == null)
            throw new KeyNotFoundException($"Proje bulunamadı: {dto.ProjectId}");

        var branches = project.ProjectBranches
            .Where(pb => !pb.IsDeleted && pb.IsActive)
            .ToList();

        if (dto.BranchIds?.Any() == true)
            branches = branches.Where(pb => dto.BranchIds.Contains(pb.BranchId)).ToList();

        if (!branches.Any())
            throw new InvalidOperationException("Atama yapılacak şube bulunamadı.");

        var assignments = new List<Assignment>();

        foreach (var branch in branches)
        {
            for (int i = 0; i < dto.AssignmentsPerBranch; i++)
            {
                assignments.Add(new Assignment
                {
                    ProjectId = dto.ProjectId,
                    ChecklistId = project.ChecklistId,
                    BranchId = branch.BranchId,
                    AssignedUserId = dto.AssignedUserId,
                    UniqueLink = Guid.NewGuid().ToString(),
                    DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc),
                    IsCompleted = false
                });
            }
        }

        await _context.Assignments.AddRangeAsync(assignments);
        await _context.SaveChangesAsync();

        return await GetByProjectIdAsync(dto.ProjectId);
    }

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
            InProgressCount = assignments.Count(a => !a.IsCompleted && a.Evaluations.Any(e => e.Status == EvaluationStatus.Draft)),
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

    public async Task<IEnumerable<BranchAssignmentSummaryDto>> GetBranchSummariesAsync(int projectId)
    {
        var assignments = await _context.Assignments
            .Include(a => a.Branch)
            .Include(a => a.Evaluations)
            .Where(a => a.ProjectId == projectId && !a.IsDeleted && a.BranchId.HasValue)
            .ToListAsync();

        return assignments
            .GroupBy(a => new { a.BranchId, BranchName = a.Branch?.Name ?? "", BranchCode = a.Branch?.Code })
            .Select(g =>
            {
                var completed = g.Where(a => a.IsCompleted).ToList();
                var scores = completed
                    .SelectMany(a => a.Evaluations)
                    .Where(e => e.ScorePercentage.HasValue)
                    .Select(e => e.ScorePercentage!.Value)
                    .ToList();

                return new BranchAssignmentSummaryDto
                {
                    BranchId = g.Key.BranchId!.Value,
                    BranchName = g.Key.BranchName,
                    BranchCode = g.Key.BranchCode,
                    TotalAssignments = g.Count(),
                    CompletedAssignments = completed.Count,
                    AverageScore = scores.Any() ? Math.Round(scores.Average(), 1) : 0,
                    YellowCards = completed.SelectMany(a => a.Evaluations).Sum(e => e.YellowCardCount),
                    RedCards = completed.SelectMany(a => a.Evaluations).Sum(e => e.RedCardCount)
                };
            });
    }

    #endregion

    #region SÜRESI DOLANLAR

    public async Task<IEnumerable<AssignmentDto>> GetExpiredAsync()
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.Branch)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedFieldWorker)
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
            .Include(a => a.Branch)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedFieldWorker)
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
            : evaluation != null && evaluation.Status == EvaluationStatus.Draft ? "InProgress"
            : "Pending";

        return new AssignmentDto
        {
            Id = assignment.Id,
            ProjectId = assignment.ProjectId,
            ProjectName = assignment.Project?.Name ?? "",
            ProjectCode = assignment.Project?.Code,
            ChecklistId = assignment.ChecklistId,
            ChecklistName = assignment.Checklist?.Name ?? "",
            BranchId = assignment.BranchId,
            BranchName = assignment.Branch?.Name,
            BranchCode = assignment.Branch?.Code,
            AssignedUserId = assignment.AssignedUserId,
            AssignedUserName = assignment.AssignedUser != null
                ? $"{assignment.AssignedUser.FirstName} {assignment.AssignedUser.LastName}"
                : null,
            AssignedFieldWorkerId = assignment.AssignedFieldWorkerId,
            AssignedFieldWorkerName = assignment.AssignedFieldWorker != null
                ? $"{assignment.AssignedFieldWorker.FirstName} {assignment.AssignedFieldWorker.LastName}"
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
            EvaluationStatus = evaluation?.Status.ToString(),
            EvaluationScore = evaluation?.ScorePercentage,
            YellowCardCount = evaluation?.YellowCardCount ?? 0,
            RedCardCount = evaluation?.RedCardCount ?? 0
        };
    }

    #endregion
}
