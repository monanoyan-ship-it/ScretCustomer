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
    private readonly INotificationCreatorService _notificationCreator;

    public AssignmentService(
        IAssignmentRepository assignmentRepository,
        IProjectRepository projectRepository,
        IChecklistRepository checklistRepository,
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        INotificationCreatorService notificationCreator)
    {
        _assignmentRepository = assignmentRepository;
        _projectRepository = projectRepository;
        _checklistRepository = checklistRepository;
        _context = context;
        _auditLogService = auditLogService;
        _notificationCreator = notificationCreator;
    }

    #region TEMEL CRUD

    public async Task<AssignmentDto?> GetByIdAsync(int id)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Evaluations)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedCustomerPersonnel)
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
            .Include(a => a.Project)
                .ThenInclude(p => p.Evaluations)
                    .ThenInclude(e => e.Evaluator)
            .Include(a => a.Periods)
                .ThenInclude(p => p.Evaluations)
            .Include(a => a.AssignmentCustomerDealers.Where(acd => !acd.IsDeleted))
                .ThenInclude(acd => acd.CustomerDealer)
            .Include(a => a.AssignmentCustomerDealers.Where(acd => !acd.IsDeleted))
                .ThenInclude(acd => acd.Evaluation)
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
            ScoringMethod = dto.ScoringMethod,
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
        var evaluation = assignment.Project.Evaluations.FirstOrDefault();
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

        // Get assigned dealers (branches)
        detailDto.Dealers = assignment.AssignmentCustomerDealers
            .Where(acd => !acd.IsDeleted)
            .OrderBy(acd => acd.SortOrder)
            .Select(acd => new AssignmentCustomerDealerDto
            {
                Id = acd.Id,
                AssignmentId = acd.AssignmentId,
                CustomerDealerId = acd.CustomerDealerId,
                CustomerDealerName = acd.CustomerDealer?.Name ?? "",
                CustomerDealerCode = acd.CustomerDealer?.Code,
                City = acd.CustomerDealer?.City,
                District = acd.CustomerDealer?.District,
                SortOrder = acd.SortOrder,
                HasEvaluation = acd.EvaluationId.HasValue,
                EvaluationId = acd.EvaluationId,
                EvaluationScore = acd.Evaluation?.ScorePercentage,
                EvaluationDate = acd.Evaluation?.CreatedAt
            })
            .ToList();

        // CustomerId for adding dealers
        detailDto.CustomerId = assignment.Project?.CustomerId;

        return detailDto;
    }

    public async Task<IEnumerable<AssignmentDto>> GetAllAsync()
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Evaluations)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedCustomerPersonnel)
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.Id)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    public async Task<IEnumerable<AssignmentListDto>> GetListAsync(
        int? projectId = null,
        int? assignedUserId = null,
        string? status = null,
        string? searchTerm = null)
    {
        var now = TurkeyTime.Now;
        var query = _context.Assignments
            .Where(a => !a.IsDeleted)
            .AsQueryable();

        // Filters
        if (projectId.HasValue)
            query = query.Where(a => a.ProjectId == projectId.Value);

        if (assignedUserId.HasValue)
            query = query.Where(a => a.AssignedUserId == assignedUserId.Value);

        if (!string.IsNullOrEmpty(status))
        {
            query = status switch
            {
                "Pending" => query.Where(a => !a.IsCompleted && a.DueDate >= now),
                "InProgress" => query.Where(a => !a.IsCompleted && a.DueDate >= now),
                "Completed" => query.Where(a => a.IsCompleted),
                "Expired" => query.Where(a => !a.IsCompleted && a.DueDate < now),
                _ => query
            };
        }

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(a =>
                (a.Project != null && a.Project.Name.ToLower().Contains(term)) ||
                (a.Checklist != null && a.Checklist.Name.ToLower().Contains(term)) ||
                (a.AssignedUser != null && (a.AssignedUser.FirstName + " " + a.AssignedUser.LastName).ToLower().Contains(term)) ||
                (a.ExternalEmail != null && a.ExternalEmail.ToLower().Contains(term)) ||
                (a.ExternalName != null && a.ExternalName.ToLower().Contains(term)));
        }

        // Projection - Include kullanmadan
        return await query
            .OrderByDescending(a => a.Id)
            .Select(a => new AssignmentListDto
            {
                Id = a.Id,
                ProjectId = a.ProjectId,
                ProjectName = a.Project != null ? a.Project.Name : "",
                ProjectCode = a.Project != null ? a.Project.Code : null,
                ChecklistId = a.ChecklistId,
                ChecklistName = a.Checklist != null ? a.Checklist.Name : "",
                ScoringMethod = a.Checklist != null ? ScoringMethods.GetById(a.Checklist.ScoringMethodId)!.SystemName : "Maximum",
                AssignedUserId = a.AssignedUserId,
                AssignedUserName = a.AssignedUser != null
                    ? a.AssignedUser.FirstName + " " + a.AssignedUser.LastName
                    : null,
                AssignedCustomerPersonnelId = a.AssignedCustomerPersonnelId,
                AssignedCustomerPersonnelName = a.AssignedCustomerPersonnel != null
                    ? a.AssignedCustomerPersonnel.FirstName + " " + a.AssignedCustomerPersonnel.LastName
                    : null,
                ExternalEmail = a.ExternalEmail,
                ExternalName = a.ExternalName,
                UniqueLink = a.UniqueLink,
                DueDate = a.DueDate,
                IsCompleted = a.IsCompleted,
                CompletedAt = a.CompletedAt,
                CreatedAt = a.CreatedAt,
                Status = a.IsCompleted ? "Completed"
                    : a.DueDate < now ? "Expired"
                    : a.Project.Evaluations.Any(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Draft) ? "InProgress"
                    : "Pending",
                // Evaluation aggregate'leri
                EvaluationId = a.Project.Evaluations.Where(e => !e.IsDeleted).OrderByDescending(e => e.CreatedAt).Select(e => (int?)e.Id).FirstOrDefault(),
                EvaluationScore = a.Project.Evaluations.Where(e => !e.IsDeleted).OrderByDescending(e => e.CreatedAt).Select(e => e.ScorePercentage).FirstOrDefault(),
                YellowCardCount = a.Project.Evaluations.Where(e => !e.IsDeleted).OrderByDescending(e => e.CreatedAt).Select(e => e.YellowCardCount).FirstOrDefault(),
                RedCardCount = a.Project.Evaluations.Where(e => !e.IsDeleted).OrderByDescending(e => e.CreatedAt).Select(e => e.RedCardCount).FirstOrDefault(),
                EvaluationCount = a.Project.Evaluations.Count(e => !e.IsDeleted &&
                    ((e.EvaluatorId != null && e.EvaluatorId == a.AssignedUserId) ||
                     (e.EvaluatorCustomerPersonnelId != null && e.EvaluatorCustomerPersonnelId == a.AssignedCustomerPersonnelId)))
            })
            .ToListAsync();
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

        // FieldWorker ataması ise AssignedFieldWorkerId'yi AssignedUserId olarak kullan
        var assignedUserId = dto.AssignedFieldWorkerId ?? dto.AssignedUserId;

        // Dahili kullanıcı için mükerrer atama kontrolü
        // Aynı kullanıcıya aynı projede tamamlanmamış ve tarih çakışan atama var mı?
        if (assignedUserId.HasValue)
        {
            var newDueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc);
            var existingAssignment = await _context.Assignments
                .Where(a => a.ProjectId == dto.ProjectId
                         && a.AssignedUserId == assignedUserId
                         && !a.IsCompleted
                         && !a.IsDeleted
                         && newDueDate <= a.DueDate)
                .Select(a => new { a.Id, a.DueDate })
                .FirstOrDefaultAsync();

            if (existingAssignment != null)
            {
                throw new InvalidOperationException(
                    $"Bu kullanıcıya bu projede {existingAssignment.DueDate:dd.MM.yyyy} tarihine kadar tamamlanmamış bir atama zaten mevcut.");
            }
        }

        // Atama tipini belirle
        var assignmentType = dto.AssignedFieldWorkerId.HasValue
            ? AssignmentTypes.Ids.FieldWorker
            : AssignmentTypes.Ids.InternalUser;

        var assignment = new Assignment
        {
            ProjectId = dto.ProjectId,
            ChecklistId = dto.ChecklistId,
            AssignedUserId = assignedUserId,
            AssignedCustomerPersonnelId = dto.AssignedCustomerPersonnelId,
            ExternalEmail = dto.ExternalEmail,
            ExternalName = dto.ExternalName,
            UniqueLink = Guid.NewGuid().ToString(),
            DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc),
            TypeId = assignmentType,
            IsCompleted = false
        };

        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();

        // CustomerDealer (şube) ilişkilerini ekle
        if (dto.CustomerDealerIds?.Any() == true)
        {
            var sortOrder = 0;
            foreach (var dealerId in dto.CustomerDealerIds)
            {
                var assignmentDealer = new AssignmentCustomerDealer
                {
                    AssignmentId = assignment.Id,
                    CustomerDealerId = dealerId,
                    SortOrder = sortOrder++,
                    CreatedAt = TurkeyTime.Now
                };
                _context.AssignmentCustomerDealers.Add(assignmentDealer);
            }
            await _context.SaveChangesAsync();
        }

        // Atanan kullanıcıya bildirim gönder
        if (assignment.AssignedUserId.HasValue)
        {
            var projectName = await _context.Projects
                .Where(p => p.Id == assignment.ProjectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync() ?? "Proje";

            await _notificationCreator.CreateAsync(
                assignment.AssignedUserId.Value,
                NotificationTypes.Ids.Assignment,
                "Yeni Atama",
                $"{projectName} projesinde size yeni bir atama yapıldı.",
                actionUrl: $"/Assignments/Detail/{assignment.Id}",
                relatedEntityId: assignment.Id,
                relatedEntityType: "Assignment");
        }

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
        assignment.UpdatedAt = TurkeyTime.Now;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var assignment = await _context.Assignments.FindAsync(id);
        if (assignment == null) return false;

        assignment.IsDeleted = true;
        assignment.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region FİLTRELEME

    public async Task<IEnumerable<AssignmentDto>> GetByProjectIdAsync(int projectId)
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Evaluations)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedCustomerPersonnel)
            .Where(a => a.ProjectId == projectId && !a.IsDeleted)
            .OrderByDescending(a => a.Id)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    public async Task<IEnumerable<AssignmentDto>> GetByUserIdAsync(int userId)
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Evaluations)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
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
                .ThenInclude(p => p.Evaluations)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedCustomerPersonnel)
            .Include(a => a.Periods)
            .Where(a => a.AssignedCustomerPersonnelId == customerPersonnelId && !a.IsDeleted)
            // Sıralama: Bekleyenler önce, sonra DueDate'e göre
            .OrderBy(a => a.IsCompleted)
            .ThenBy(a => a.DueDate)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    public async Task<PagedAssignmentResult> GetFilteredAsync(AssignmentFilterDto filter)
    {
        var query = _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Evaluations)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedCustomerPersonnel)
            .Where(a => !a.IsDeleted)
            .AsQueryable();

        // Çoklu ProjectIds filtresi
        if (filter.ProjectIds?.Any() == true)
            query = query.Where(a => filter.ProjectIds.Contains(a.ProjectId));

        // Çoklu AssignedUserIds filtresi
        if (filter.AssignedUserIds?.Any() == true)
            query = query.Where(a => a.AssignedUserId.HasValue && filter.AssignedUserIds.Contains(a.AssignedUserId.Value));

        // Çoklu ChecklistIds filtresi
        if (filter.ChecklistIds?.Any() == true)
            query = query.Where(a => filter.ChecklistIds.Contains(a.ChecklistId));

        // Çoklu Status filtrelemesi (OR mantığı)
        if (filter.Statuses?.Any() == true)
        {
            var now = TurkeyTime.Now;
            var statusPredicates = new List<System.Linq.Expressions.Expression<Func<Assignment, bool>>>();

            foreach (var status in filter.Statuses)
            {
                switch (status)
                {
                    case "Pending":
                        statusPredicates.Add(a => !a.IsCompleted && a.DueDate >= now);
                        break;
                    case "InProgress":
                        statusPredicates.Add(a => !a.IsCompleted && a.DueDate >= now);
                        break;
                    case "Completed":
                        statusPredicates.Add(a => a.IsCompleted);
                        break;
                    case "Expired":
                        statusPredicates.Add(a => !a.IsCompleted && a.DueDate < now);
                        break;
                }
            }

            if (statusPredicates.Any())
            {
                var combined = statusPredicates.Aggregate((current, next) =>
                {
                    var param = System.Linq.Expressions.Expression.Parameter(typeof(Assignment), "a");
                    var body = System.Linq.Expressions.Expression.OrElse(
                        System.Linq.Expressions.Expression.Invoke(current, param),
                        System.Linq.Expressions.Expression.Invoke(next, param));
                    return System.Linq.Expressions.Expression.Lambda<Func<Assignment, bool>>(body, param);
                });
                query = query.Where(combined);
            }
        }

        if (filter.IsCompleted.HasValue)
            query = query.Where(a => a.IsCompleted == filter.IsCompleted.Value);

        if (filter.DueDateFrom.HasValue)
            query = query.Where(a => a.DueDate >= filter.DueDateFrom.Value);

        if (filter.DueDateTo.HasValue)
            query = query.Where(a => a.DueDate <= filter.DueDateTo.Value);

        // Son tarih filtresi (semantic date filtering - Projects pattern)
        if (!string.IsNullOrEmpty(filter.DueDateFilter))
        {
            var today = TurkeyTime.Now.Date;
            var todayStart = DateTime.SpecifyKind(today, DateTimeKind.Utc);
            var todayEnd = DateTime.SpecifyKind(today.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            switch (filter.DueDateFilter)
            {
                case "overdue":
                    // Süresi geçmiş: DueDate bugünden önce ve tamamlanmamış
                    query = query.Where(a => a.DueDate < todayStart && !a.IsCompleted);
                    break;
                case "today":
                    // Bugün son tarih
                    query = query.Where(a => a.DueDate >= todayStart && a.DueDate <= todayEnd);
                    break;
                case "tomorrow":
                    // Yarın son tarih
                    var tomorrowStart = DateTime.SpecifyKind(today.AddDays(1), DateTimeKind.Utc);
                    var tomorrowEnd = DateTime.SpecifyKind(today.AddDays(2).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.DueDate >= tomorrowStart && a.DueDate <= tomorrowEnd);
                    break;
                case "next7Days":
                    // 7 gün içinde
                    var next7Days = DateTime.SpecifyKind(today.AddDays(7).AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.DueDate >= todayStart && a.DueDate <= next7Days);
                    break;
                case "thisWeek":
                    // Bu hafta (Pazartesi-Pazar)
                    var dayOfWeek = (int)today.DayOfWeek;
                    var daysToMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
                    var weekStart = DateTime.SpecifyKind(today.AddDays(-daysToMonday), DateTimeKind.Utc);
                    var weekEnd = DateTime.SpecifyKind(weekStart.AddDays(7).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.DueDate >= weekStart && a.DueDate <= weekEnd);
                    break;
                case "next30Days":
                    // 30 gün içinde
                    var next30Days = DateTime.SpecifyKind(today.AddDays(30).AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.DueDate >= todayStart && a.DueDate <= next30Days);
                    break;
                case "thisMonth":
                    // Bu ay
                    var monthStart = DateTime.SpecifyKind(new DateTime(today.Year, today.Month, 1), DateTimeKind.Utc);
                    var monthEnd = DateTime.SpecifyKind(monthStart.AddMonths(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.DueDate >= monthStart && a.DueDate <= monthEnd);
                    break;
                case "nextMonth":
                    // Gelecek ay
                    var nextMonthStart = DateTime.SpecifyKind(new DateTime(today.Year, today.Month, 1).AddMonths(1), DateTimeKind.Utc);
                    var nextMonthEnd = DateTime.SpecifyKind(nextMonthStart.AddMonths(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.DueDate >= nextMonthStart && a.DueDate <= nextMonthEnd);
                    break;
            }
        }

        if (filter.IsExpired == true)
            query = query.Where(a => !a.IsCompleted && a.DueDate < TurkeyTime.Now);

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
                ? query.OrderBy(a => a.Project.Evaluations.OrderByDescending(e => e.CreatedAt).FirstOrDefault() != null
                    ? a.Project.Evaluations.OrderByDescending(e => e.CreatedAt).FirstOrDefault()!.TotalScore : 0)
                : query.OrderByDescending(a => a.Project.Evaluations.OrderByDescending(e => e.CreatedAt).FirstOrDefault() != null
                    ? a.Project.Evaluations.OrderByDescending(e => e.CreatedAt).FirstOrDefault()!.TotalScore : 0),
            _ => query.OrderByDescending(a => a.Id) // Default
        };

        // Total count (paging için)
        var totalCount = await orderedQuery.CountAsync();

        // Paging uygula
        var assignments = await orderedQuery
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedAssignmentResult
        {
            Items = assignments.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
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
        assignment.CompletedAt = TurkeyTime.Now;
        assignment.UpdatedAt = TurkeyTime.Now;

        await _context.SaveChangesAsync();

        // Audit Log
        await _auditLogService.LogInfoAsync(
            $"Atama tamamlandı: {assignment.Project?.Name}",
            "AssignmentService");

        // Atanan kullanıcıya bildirim
        if (assignment.AssignedUserId.HasValue)
        {
            await _notificationCreator.CreateAsync(
                assignment.AssignedUserId.Value,
                NotificationTypes.Ids.Success,
                "Atama Tamamlandı",
                $"{assignment.Project?.Name} projesindeki atamanız tamamlandı olarak işaretlendi.",
                relatedEntityId: id,
                relatedEntityType: "Assignment");
        }

        return await GetByIdAsync(id) ?? throw new KeyNotFoundException("Atama bulunamadı");
    }

    public async Task<AssignmentDto> CancelAssignmentAsync(int id, string? reason)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Evaluations)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedCustomerPersonnel)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        if (assignment == null)
            throw new KeyNotFoundException($"Atama bulunamadı: {id}");

        assignment.IsDeleted = true;
        assignment.UpdatedAt = TurkeyTime.Now;

        await _context.SaveChangesAsync();

        // Audit Log
        await _auditLogService.LogWarningAsync(
            $"Atama iptal edildi: {assignment.Project?.Name} - Sebep: {reason ?? "Belirtilmedi"}",
            "AssignmentService");

        // Atanan kullanıcıya bildirim
        if (assignment.AssignedUserId.HasValue)
        {
            await _notificationCreator.CreateAsync(
                assignment.AssignedUserId.Value,
                NotificationTypes.Ids.Warning,
                "Atama İptal Edildi",
                $"{assignment.Project?.Name} projesindeki atamanız iptal edildi. Sebep: {reason ?? "Belirtilmedi"}",
                relatedEntityId: id,
                relatedEntityType: "Assignment");
        }

        // İptal edilen atamayı direkt map et (GetByIdAsync silinen kayıtları filtreliyor)
        return MapToDto(assignment);
    }

    public async Task<AssignmentDto> ReopenAssignmentAsync(int id)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Evaluations)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Include(a => a.AssignedCustomerPersonnel)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        if (assignment == null)
            throw new KeyNotFoundException($"Atama bulunamadı: {id}");

        if (!assignment.IsCompleted)
            throw new InvalidOperationException("Bu atama zaten açık durumda.");

        // Assignment'ı yeniden aç
        assignment.IsCompleted = false;
        assignment.CompletedAt = null;
        assignment.UpdatedAt = TurkeyTime.Now;

        // İlişkili Evaluation'ları da InProgress yap
        foreach (var evaluation in assignment.Project.Evaluations)
        {
            if (evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            {
                evaluation.StatusId = EvaluationStatuses.Ids.InProgress;
                evaluation.CompletedAt = null;
                evaluation.UpdatedAt = TurkeyTime.Now;
            }
        }

        await _context.SaveChangesAsync();

        // Audit Log
        await _auditLogService.LogInfoAsync(
            $"Atama yeniden açıldı: {assignment.Project?.Name}",
            "AssignmentService");

        return MapToDto(assignment);
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

        // FieldWorker ataması ise NewAssignedFieldWorkerId'yi AssignedUserId olarak kullan
        if (dto.NewAssignedFieldWorkerId.HasValue)
        {
            assignment.AssignedUserId = dto.NewAssignedFieldWorkerId;
            assignment.TypeId = AssignmentTypes.Ids.FieldWorker;
        }
        else if (dto.NewAssignedUserId.HasValue)
        {
            assignment.AssignedUserId = dto.NewAssignedUserId;
            assignment.TypeId = AssignmentTypes.Ids.InternalUser;
        }

        if (dto.NewAssignedCustomerPersonnelId.HasValue)
            assignment.AssignedCustomerPersonnelId = dto.NewAssignedCustomerPersonnelId;

        if (!string.IsNullOrEmpty(dto.NewExternalEmail))
        {
            assignment.ExternalEmail = dto.NewExternalEmail;
            assignment.ExternalName = dto.NewExternalName;
        }

        if (dto.NewDueDate.HasValue)
            assignment.DueDate = DateTime.SpecifyKind(dto.NewDueDate.Value, DateTimeKind.Utc);

        assignment.UpdatedAt = TurkeyTime.Now;
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
        assignment.UpdatedAt = TurkeyTime.Now;
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
            assignment.UpdatedAt = TurkeyTime.Now;
        }

        await _context.SaveChangesAsync();
        return assignments.Count;
    }

    #endregion

    #region İSTATİSTİKLER

    public async Task<AssignmentSummaryDto> GetSummaryAsync(int? projectId = null)
    {
        var query = _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Evaluations)
            .Where(a => !a.IsDeleted);

        if (projectId.HasValue)
            query = query.Where(a => a.ProjectId == projectId.Value);

        var assignments = await query.ToListAsync();
        var now = TurkeyTime.Now;

        var completed = assignments.Where(a => a.IsCompleted).ToList();
        var evaluationScores = completed
            .SelectMany(a => a.Project.Evaluations)
            .Where(e => e.ScorePercentage.HasValue)
            .Select(e => e.ScorePercentage!.Value)
            .ToList();

        return new AssignmentSummaryDto
        {
            TotalAssignments = assignments.Count,
            PendingCount = assignments.Count(a => !a.IsCompleted && a.DueDate >= now),
            InProgressCount = assignments.Count(a => !a.IsCompleted && a.Project.Evaluations.Any(e => e.StatusId == EvaluationStatuses.Ids.Draft)),
            CompletedCount = completed.Count,
            ExpiredCount = assignments.Count(a => !a.IsCompleted && a.DueDate < now),
            CancelledCount = 0, // IsDeleted ones are not included
            CompletionRate = assignments.Count > 0 ? Math.Round((decimal)completed.Count / assignments.Count * 100, 1) : 0,
            AverageScore = evaluationScores.Any() ? Math.Round(evaluationScores.Average(), 1) : 0,
            TotalYellowCards = completed.SelectMany(a => a.Project.Evaluations).Sum(e => e.YellowCardCount),
            TotalRedCards = completed.SelectMany(a => a.Project.Evaluations).Sum(e => e.RedCardCount)
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
            ProjectName = !string.IsNullOrEmpty(p.Code) ? $"{p.Code} - {p.Name}" : p.Name,
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
            .Where(a => !a.IsDeleted && !a.IsCompleted && a.DueDate < TurkeyTime.Now)
            .OrderBy(a => a.DueDate)
            .ToListAsync();

        return assignments.Select(MapToDto);
    }

    public async Task<IEnumerable<AssignmentDto>> GetUpcomingDueAsync(int daysAhead = 3)
    {
        var deadline = TurkeyTime.Now.AddDays(daysAhead);
        var assignments = await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Where(a => !a.IsDeleted && !a.IsCompleted && a.DueDate >= TurkeyTime.Now && a.DueDate <= deadline)
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
        var evaluation = assignment.Project?.Evaluations?.FirstOrDefault();
        var status = assignment.IsCompleted ? "Completed"
            : assignment.DueDate < TurkeyTime.Now ? "Expired"
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
            ScoringMethod = assignment.Checklist != null ? ScoringMethods.GetById(assignment.Checklist.ScoringMethodId)?.SystemName : "Maximum",
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
            EvaluationCount = assignment.Project?.Evaluations?
                .Count(e => !e.IsDeleted &&
                    ((e.EvaluatorId != null && e.EvaluatorId == assignment.AssignedUserId) ||
                     (e.EvaluatorCustomerPersonnelId != null && e.EvaluatorCustomerPersonnelId == assignment.AssignedCustomerPersonnelId))) ?? 0
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
            CreatedAt = TurkeyTime.Now
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
        period.UpdatedAt = TurkeyTime.Now;

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
        period.UpdatedAt = TurkeyTime.Now;

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
        period.UpdatedAt = TurkeyTime.Now;

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
        period.UpdatedAt = TurkeyTime.Now;

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

        // Çoklu CustomerIds filtresi
        if (filter.CustomerIds?.Any() == true)
        {
            query = query.Where(a => a.Project != null && a.Project.CustomerId.HasValue && filter.CustomerIds.Contains(a.Project.CustomerId.Value));
        }

        // Çoklu ProjectIds filtresi
        if (filter.ProjectIds?.Any() == true)
        {
            query = query.Where(a => filter.ProjectIds.Contains(a.ProjectId));
        }

        // Çoklu AssignedCustomerPersonnelIds filtresi
        if (filter.AssignedCustomerPersonnelIds?.Any() == true)
        {
            query = query.Where(a => a.AssignedCustomerPersonnelId.HasValue && filter.AssignedCustomerPersonnelIds.Contains(a.AssignedCustomerPersonnelId.Value));
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

        // Son tarih filtresi (semantic date filtering - Projects pattern)
        if (!string.IsNullOrEmpty(filter.DueDateFilter))
        {
            var today = TurkeyTime.Now.Date;
            var todayStart = DateTime.SpecifyKind(today, DateTimeKind.Utc);
            var todayEnd = DateTime.SpecifyKind(today.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            switch (filter.DueDateFilter)
            {
                case "overdue":
                    // Süresi geçmiş: DueDate bugünden önce ve tamamlanmamış
                    query = query.Where(a => a.DueDate < todayStart && !a.IsCompleted);
                    break;
                case "today":
                    // Bugün son tarih
                    query = query.Where(a => a.DueDate >= todayStart && a.DueDate <= todayEnd);
                    break;
                case "tomorrow":
                    // Yarın son tarih
                    var tomorrowStart = DateTime.SpecifyKind(today.AddDays(1), DateTimeKind.Utc);
                    var tomorrowEnd = DateTime.SpecifyKind(today.AddDays(2).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.DueDate >= tomorrowStart && a.DueDate <= tomorrowEnd);
                    break;
                case "next7Days":
                    // 7 gün içinde
                    var next7Days = DateTime.SpecifyKind(today.AddDays(7).AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.DueDate >= todayStart && a.DueDate <= next7Days);
                    break;
                case "thisWeek":
                    // Bu hafta (Pazartesi-Pazar)
                    var dayOfWeek = (int)today.DayOfWeek;
                    var daysToMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
                    var weekStart = DateTime.SpecifyKind(today.AddDays(-daysToMonday), DateTimeKind.Utc);
                    var weekEnd = DateTime.SpecifyKind(weekStart.AddDays(7).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.DueDate >= weekStart && a.DueDate <= weekEnd);
                    break;
                case "next30Days":
                    // 30 gün içinde
                    var next30Days = DateTime.SpecifyKind(today.AddDays(30).AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.DueDate >= todayStart && a.DueDate <= next30Days);
                    break;
                case "thisMonth":
                    // Bu ay
                    var monthStart = DateTime.SpecifyKind(new DateTime(today.Year, today.Month, 1), DateTimeKind.Utc);
                    var monthEnd = DateTime.SpecifyKind(monthStart.AddMonths(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.DueDate >= monthStart && a.DueDate <= monthEnd);
                    break;
                case "nextMonth":
                    // Gelecek ay
                    var nextMonthStart = DateTime.SpecifyKind(new DateTime(today.Year, today.Month, 1).AddMonths(1), DateTimeKind.Utc);
                    var nextMonthEnd = DateTime.SpecifyKind(nextMonthStart.AddMonths(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(a => a.DueDate >= nextMonthStart && a.DueDate <= nextMonthEnd);
                    break;
            }
        }

        var assignments = await query.OrderByDescending(a => a.Id).ToListAsync();
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
            OverdueAssignments = assignments.Count(a => a.DueDate < TurkeyTime.Now && !a.IsCompleted),
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
                    CreatedAt = TurkeyTime.Now,
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

    #region ATAMA ŞUBE (DEALER) YÖNETİMİ

    public async Task<IEnumerable<AssignmentCustomerDealerDto>> GetAssignmentDealersAsync(int assignmentId)
    {
        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted);

        if (assignment == null)
            throw new KeyNotFoundException("Atama bulunamadı");

        var dealers = await _context.AssignmentCustomerDealers
            .Include(acd => acd.CustomerDealer)
            .Include(acd => acd.Evaluation)
            .Where(acd => acd.AssignmentId == assignmentId && !acd.IsDeleted)
            .OrderBy(acd => acd.SortOrder)
            .ToListAsync();

        return dealers.Select(acd => new AssignmentCustomerDealerDto
        {
            Id = acd.Id,
            AssignmentId = acd.AssignmentId,
            CustomerDealerId = acd.CustomerDealerId,
            CustomerDealerName = acd.CustomerDealer?.Name ?? "",
            CustomerDealerCode = acd.CustomerDealer?.Code,
            City = acd.CustomerDealer?.City,
            District = acd.CustomerDealer?.District,
            SortOrder = acd.SortOrder,
            HasEvaluation = acd.EvaluationId.HasValue,
            EvaluationId = acd.EvaluationId,
            EvaluationScore = acd.Evaluation?.ScorePercentage,
            EvaluationDate = acd.Evaluation?.CreatedAt
        });
    }

    public async Task<AssignmentCustomerDealerDto> AddDealerToAssignmentAsync(int assignmentId, int customerDealerId)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted);

        if (assignment == null)
            throw new KeyNotFoundException("Atama bulunamadı");

        // Şubenin zaten ekli olup olmadığını kontrol et
        var exists = await _context.AssignmentCustomerDealers
            .AnyAsync(acd => acd.AssignmentId == assignmentId && acd.CustomerDealerId == customerDealerId && !acd.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Bu şube zaten atamaya ekli");

        // Şubenin projenin müşterisine ait olup olmadığını kontrol et
        var dealer = await _context.CustomerDealers
            .FirstOrDefaultAsync(d => d.Id == customerDealerId && !d.IsDeleted);

        if (dealer == null)
            throw new KeyNotFoundException("Şube bulunamadı");

        if (assignment.Project?.CustomerId != dealer.CustomerId)
            throw new InvalidOperationException("Şube bu projenin müşterisine ait değil");

        // Mevcut en yüksek sıra numarasını bul
        var maxSortOrder = await _context.AssignmentCustomerDealers
            .Where(acd => acd.AssignmentId == assignmentId && !acd.IsDeleted)
            .MaxAsync(acd => (int?)acd.SortOrder) ?? -1;

        var assignmentDealer = new AssignmentCustomerDealer
        {
            AssignmentId = assignmentId,
            CustomerDealerId = customerDealerId,
            SortOrder = maxSortOrder + 1,
            CreatedAt = TurkeyTime.Now
        };

        _context.AssignmentCustomerDealers.Add(assignmentDealer);
        await _context.SaveChangesAsync();

        return new AssignmentCustomerDealerDto
        {
            Id = assignmentDealer.Id,
            AssignmentId = assignmentDealer.AssignmentId,
            CustomerDealerId = assignmentDealer.CustomerDealerId,
            CustomerDealerName = dealer.Name,
            CustomerDealerCode = dealer.Code,
            City = dealer.City,
            District = dealer.District,
            SortOrder = assignmentDealer.SortOrder,
            HasEvaluation = false,
            EvaluationId = null,
            EvaluationScore = null,
            EvaluationDate = null
        };
    }

    public async Task RemoveDealerFromAssignmentAsync(int assignmentId, int customerDealerId)
    {
        var assignmentDealer = await _context.AssignmentCustomerDealers
            .Include(acd => acd.Evaluation)
            .FirstOrDefaultAsync(acd => acd.AssignmentId == assignmentId && acd.CustomerDealerId == customerDealerId && !acd.IsDeleted);

        if (assignmentDealer == null)
            throw new KeyNotFoundException("Atama-şube ilişkisi bulunamadı");

        // Ziyaret yapılmışsa silinmesine izin verme
        if (assignmentDealer.EvaluationId.HasValue)
            throw new InvalidOperationException("Bu şube için ziyaret yapılmış, çıkarılamaz");

        assignmentDealer.IsDeleted = true;
        assignmentDealer.UpdatedAt = TurkeyTime.Now;

        await _context.SaveChangesAsync();
    }

    #endregion
}
