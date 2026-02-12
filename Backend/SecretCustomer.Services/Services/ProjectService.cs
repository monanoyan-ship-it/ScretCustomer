using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Project;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IChecklistRepository _checklistRepository;
    private readonly ApplicationDbContext _context;
    private readonly INotificationCreatorService _notificationCreator;

    public ProjectService(
        IProjectRepository projectRepository,
        IChecklistRepository checklistRepository,
        ApplicationDbContext context,
        INotificationCreatorService notificationCreator)
    {
        _projectRepository = projectRepository;
        _checklistRepository = checklistRepository;
        _context = context;
        _notificationCreator = notificationCreator;
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

    public async Task<ProjectDto?> GetByIdAsync(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Checklist)
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.ProjectManager)
            .Include(p => p.Evaluations)
            .Include(p => p.Assignments)
            .Include(p => p.TeamMembers)
            .Include(p => p.EmailTemplate)
            .Include(p => p.ReminderEmailTemplate)
            .Include(p => p.SurveyInvitations)
            .Include(p => p.SurveyExternalInvitations)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        return project == null ? null : MapToDto(project);
    }

    public async Task<ProjectDetailDto?> GetDetailByIdAsync(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Checklist)
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.ProjectManager)
            .Include(p => p.Evaluations)
            .Include(p => p.Assignments)
            .Include(p => p.TeamMembers)
                .ThenInclude(tm => tm.User)
            .Include(p => p.EmailTemplate)
            .Include(p => p.ReminderEmailTemplate)
            .Include(p => p.SurveyInvitations)
            .Include(p => p.SurveyExternalInvitations)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (project == null)
            return null;

        return await MapToDetailDtoAsync(project);
    }

    public async Task<IEnumerable<ProjectDto>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Projects
            .Include(p => p.Checklist)
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.ProjectManager)
            .Include(p => p.Evaluations)
            .Include(p => p.Assignments)
            .Include(p => p.TeamMembers)
            .Include(p => p.EmailTemplate)
            .Include(p => p.ReminderEmailTemplate)
            .Include(p => p.SurveyInvitations)
            .Include(p => p.SurveyExternalInvitations)
            .Where(p => !p.IsDeleted);

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        var projects = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return projects.Select(MapToDto);
    }

    public async Task<IEnumerable<ProjectSummaryDto>> GetSummariesAsync()
    {
        var projects = await _context.Projects
            .Include(p => p.Assignments)
            .Include(p => p.SurveyInvitations)
            .Include(p => p.SurveyExternalInvitations)
            .Where(p => !p.IsDeleted && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(p =>
        {
            var daysRemaining = (p.EndDate - TurkeyTime.Now).Days;

            // İlerleme: Sadece OnlineSurvey için hesaplanır
            decimal completionPercentage = -1;
            if (p.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey)
            {
                var totalSent = (p.SurveyInvitations?.Count(si => !si.IsDeleted && si.StatusId == SurveyInvitationStatuses.Ids.Sent) ?? 0) +
                               (p.SurveyExternalInvitations?.Count(sei => !sei.IsDeleted && sei.StatusId == SurveyInvitationStatuses.Ids.Sent) ?? 0);
                var totalCompleted = (p.SurveyInvitations?.Count(si => !si.IsDeleted && si.IsCompleted) ?? 0) +
                                    (p.SurveyExternalInvitations?.Count(sei => !sei.IsDeleted && sei.IsCompleted) ?? 0);
                completionPercentage = totalSent > 0 ? Math.Round((decimal)totalCompleted / totalSent * 100, 1) : -1;
            }

            return new ProjectSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Status = ProjectStatuses.GetById(p.StatusId)?.SystemName ?? "",
                ProjectType = ProjectTypes.GetById(p.ProjectTypeId)?.SystemName ?? "",
                CompletionPercentage = completionPercentage,
                AverageScore = 0, // Will calculate if needed
                DaysRemaining = Math.Max(0, daysRemaining),
                IsOverdue = daysRemaining < 0
            };
        });
    }

    /// <summary>
    /// Liste görünümü için optimize edilmiş method - Projection kullanır
    /// </summary>
    public async Task<IEnumerable<ProjectListDto>> GetListAsync(
        string? searchText = null,
        int? customerId = null,
        string? projectType = null,
        string? status = null,
        int? projectManagerId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeInactive = false)
    {
        var query = _context.Projects
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        // Filtreler
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                (p.Code != null && p.Code.ToLower().Contains(search)) ||
                (p.Description != null && p.Description.ToLower().Contains(search)));
        }

        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(projectType))
        {
            var typeItem = ProjectTypes.GetBySystemName(projectType);
            if (typeItem != null)
                query = query.Where(p => p.ProjectTypeId == typeItem.Id);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusItem = ProjectStatuses.GetBySystemName(status);
            if (statusItem != null)
                query = query.Where(p => p.StatusId == statusItem.Id);
        }

        if (projectManagerId.HasValue)
            query = query.Where(p => p.ProjectManagerId == projectManagerId.Value);

        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.StartDate >= start || p.EndDate >= start);
        }

        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(p => p.StartDate <= end || p.EndDate <= end);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectListDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                ChecklistId = p.ChecklistId,
                ChecklistName = p.Checklist != null ? p.Checklist.Name : null,
                ProjectType = ProjectTypes.GetById(p.ProjectTypeId) != null ? ProjectTypes.GetById(p.ProjectTypeId)!.SystemName : "",
                Status = ProjectStatuses.GetById(p.StatusId) != null ? ProjectStatuses.GetById(p.StatusId)!.SystemName : "",
                AssignmentType = AssignmentTypes.GetById(p.AssignmentTypeId) != null ? AssignmentTypes.GetById(p.AssignmentTypeId)!.SystemName : "",
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                TotalAssignments = p.Assignments.Count(a => !a.IsDeleted),
                CompletedAssignments = p.Assignments.Count(a => !a.IsDeleted && a.IsCompleted),
                // İlerleme: Sadece OnlineSurvey için hesaplanır (firma personeli + dış katılımcı davetiyeleri)
                CompletionPercentage = p.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey
                    ? (
                        // Toplam gönderilen davet sayısı
                        (p.SurveyInvitations.Count(si => !si.IsDeleted && si.StatusId == SurveyInvitationStatuses.Ids.Sent) +
                         p.SurveyExternalInvitations.Count(sei => !sei.IsDeleted && sei.StatusId == SurveyInvitationStatuses.Ids.Sent)) > 0
                        ? Math.Round(
                            (decimal)(p.SurveyInvitations.Count(si => !si.IsDeleted && si.IsCompleted) +
                                     p.SurveyExternalInvitations.Count(sei => !sei.IsDeleted && sei.IsCompleted)) /
                            (p.SurveyInvitations.Count(si => !si.IsDeleted && si.StatusId == SurveyInvitationStatuses.Ids.Sent) +
                             p.SurveyExternalInvitations.Count(sei => !sei.IsDeleted && sei.StatusId == SurveyInvitationStatuses.Ids.Sent)) * 100, 1)
                        : -1 // Henüz davetiye gönderilmemiş
                      )
                    : -1, // OnlineSurvey değilse ilerleme gösterilmez
                CustomerId = p.CustomerId,
                CustomerName = p.Customer != null ? p.Customer.CompanyName : null,
                CustomerCode = p.Customer != null ? p.Customer.Code : null,
                OrganizationId = p.OrganizationId,
                OrganizationName = p.Organization != null ? p.Organization.Name : null,
                ProjectManagerId = p.ProjectManagerId,
                ProjectManagerName = p.ProjectManager != null ? p.ProjectManager.FirstName + " " + p.ProjectManager.LastName : null,
                TargetCount = p.TargetCount,
                // İstatistikler
                AverageScore = p.Evaluations
                    .Where(e => !e.IsDeleted && e.ScorePercentage.HasValue)
                    .Any()
                    ? Math.Round(p.Evaluations
                        .Where(e => !e.IsDeleted && e.ScorePercentage.HasValue)
                        .Average(e => e.ScorePercentage!.Value), 1)
                    : 0,
                TotalYellowCards = p.Evaluations
                    .Where(e => !e.IsDeleted)
                    .Sum(e => e.YellowCardCount),
                TotalRedCards = p.Evaluations
                    .Where(e => !e.IsDeleted)
                    .Sum(e => e.RedCardCount),
                TeamMemberCount = p.TeamMembers.Count(tm => !tm.IsDeleted)
            })
            .ToListAsync();
    }

    /// <summary>
    /// Liste görünümü için optimize edilmiş method - Filter DTO ile (çoklu filtre destekli)
    /// </summary>
    public async Task<IEnumerable<ProjectListDto>> GetListAsync(ProjectFilterDto filter)
    {
        var query = _context.Projects
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!filter.IncludeInactive)
            query = query.Where(p => p.IsActive);

        // Arama
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var search = filter.SearchText.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                (p.Code != null && p.Code.ToLower().Contains(search)) ||
                (p.Description != null && p.Description.ToLower().Contains(search)));
        }

        // Çoklu CustomerIds filtresi
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(p => p.CustomerId.HasValue && filter.CustomerIds.Contains(p.CustomerId.Value));

        // Çoklu ProjectTypes filtresi
        if (filter.ProjectTypes?.Any() == true)
        {
            var typeIds = filter.ProjectTypes
                .Select(t => ProjectTypes.GetBySystemName(t)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (typeIds.Any())
                query = query.Where(p => typeIds.Contains(p.ProjectTypeId));
        }

        // Çoklu Statuses filtresi
        if (filter.Statuses?.Any() == true)
        {
            var statusIds = filter.Statuses
                .Select(s => ProjectStatuses.GetBySystemName(s)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (statusIds.Any())
                query = query.Where(p => statusIds.Contains(p.StatusId));
        }

        // Çoklu ProjectManagerIds filtresi
        if (filter.ProjectManagerIds?.Any() == true)
            query = query.Where(p => p.ProjectManagerId.HasValue && filter.ProjectManagerIds.Contains(p.ProjectManagerId.Value));

        // Bitiş tarihi filtresi (proje bitiş tarihine göre)
        if (!string.IsNullOrEmpty(filter.EndingDateFilter))
        {
            var today = TurkeyTime.Now.Date;
            var todayStart = DateTime.SpecifyKind(today, DateTimeKind.Utc);
            var todayEnd = DateTime.SpecifyKind(today.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            switch (filter.EndingDateFilter)
            {
                case "overdue":
                    // Süresi geçmiş: EndDate bugünden önce ve status hala Active
                    query = query.Where(p => p.EndDate < todayStart && p.StatusId == ProjectStatuses.Ids.Active);
                    break;
                case "today":
                    // Bugün bitecek
                    query = query.Where(p => p.EndDate >= todayStart && p.EndDate <= todayEnd);
                    break;
                case "next7Days":
                    // 7 gün içinde bitecek
                    var next7Days = DateTime.SpecifyKind(today.AddDays(7).AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(p => p.EndDate >= todayStart && p.EndDate <= next7Days);
                    break;
                case "thisWeek":
                    // Bu hafta bitecek (Pazartesi-Pazar)
                    var dayOfWeek = (int)today.DayOfWeek;
                    var daysToMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
                    var weekStart = DateTime.SpecifyKind(today.AddDays(-daysToMonday), DateTimeKind.Utc);
                    var weekEnd = DateTime.SpecifyKind(weekStart.AddDays(7).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(p => p.EndDate >= weekStart && p.EndDate <= weekEnd);
                    break;
                case "next30Days":
                    // 30 gün içinde bitecek
                    var next30Days = DateTime.SpecifyKind(today.AddDays(30).AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(p => p.EndDate >= todayStart && p.EndDate <= next30Days);
                    break;
                case "thisMonth":
                    // Bu ay bitecek
                    var monthStart = DateTime.SpecifyKind(new DateTime(today.Year, today.Month, 1), DateTimeKind.Utc);
                    var monthEnd = DateTime.SpecifyKind(monthStart.AddMonths(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(p => p.EndDate >= monthStart && p.EndDate <= monthEnd);
                    break;
                case "nextMonth":
                    // Gelecek ay bitecek
                    var nextMonthStart = DateTime.SpecifyKind(new DateTime(today.Year, today.Month, 1).AddMonths(1), DateTimeKind.Utc);
                    var nextMonthEnd = DateTime.SpecifyKind(nextMonthStart.AddMonths(1).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(p => p.EndDate >= nextMonthStart && p.EndDate <= nextMonthEnd);
                    break;
                case "thisQuarter":
                    // Bu çeyrek bitecek
                    var quarter = (today.Month - 1) / 3;
                    var quarterStart = DateTime.SpecifyKind(new DateTime(today.Year, quarter * 3 + 1, 1), DateTimeKind.Utc);
                    var quarterEnd = DateTime.SpecifyKind(quarterStart.AddMonths(3).AddTicks(-1), DateTimeKind.Utc);
                    query = query.Where(p => p.EndDate >= quarterStart && p.EndDate <= quarterEnd);
                    break;
            }
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectListDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                ChecklistId = p.ChecklistId,
                ChecklistName = p.Checklist != null ? p.Checklist.Name : null,
                ProjectType = ProjectTypes.GetById(p.ProjectTypeId) != null ? ProjectTypes.GetById(p.ProjectTypeId)!.SystemName : "",
                Status = ProjectStatuses.GetById(p.StatusId) != null ? ProjectStatuses.GetById(p.StatusId)!.SystemName : "",
                AssignmentType = AssignmentTypes.GetById(p.AssignmentTypeId) != null ? AssignmentTypes.GetById(p.AssignmentTypeId)!.SystemName : "",
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                TotalAssignments = p.Assignments.Count(a => !a.IsDeleted),
                CompletedAssignments = p.Assignments.Count(a => !a.IsDeleted && a.IsCompleted),
                CompletionPercentage = p.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey
                    ? (
                        (p.SurveyInvitations.Count(si => !si.IsDeleted && si.StatusId == SurveyInvitationStatuses.Ids.Sent) +
                         p.SurveyExternalInvitations.Count(sei => !sei.IsDeleted && sei.StatusId == SurveyInvitationStatuses.Ids.Sent)) > 0
                        ? Math.Round(
                            (decimal)(p.SurveyInvitations.Count(si => !si.IsDeleted && si.IsCompleted) +
                                     p.SurveyExternalInvitations.Count(sei => !sei.IsDeleted && sei.IsCompleted)) /
                            (p.SurveyInvitations.Count(si => !si.IsDeleted && si.StatusId == SurveyInvitationStatuses.Ids.Sent) +
                             p.SurveyExternalInvitations.Count(sei => !sei.IsDeleted && sei.StatusId == SurveyInvitationStatuses.Ids.Sent)) * 100, 1)
                        : -1
                      )
                    : -1,
                CustomerId = p.CustomerId,
                CustomerName = p.Customer != null ? p.Customer.CompanyName : null,
                CustomerCode = p.Customer != null ? p.Customer.Code : null,
                OrganizationId = p.OrganizationId,
                OrganizationName = p.Organization != null ? p.Organization.Name : null,
                ProjectManagerId = p.ProjectManagerId,
                ProjectManagerName = p.ProjectManager != null ? p.ProjectManager.FirstName + " " + p.ProjectManager.LastName : null,
                TargetCount = p.TargetCount,
                AverageScore = p.Evaluations
                    .Where(e => !e.IsDeleted && e.ScorePercentage.HasValue)
                    .Any()
                    ? Math.Round(p.Evaluations
                        .Where(e => !e.IsDeleted && e.ScorePercentage.HasValue)
                        .Average(e => e.ScorePercentage!.Value), 1)
                    : 0,
                TotalYellowCards = p.Evaluations
                    .Where(e => !e.IsDeleted)
                    .Sum(e => e.YellowCardCount),
                TotalRedCards = p.Evaluations
                    .Where(e => !e.IsDeleted)
                    .Sum(e => e.RedCardCount),
                TeamMemberCount = p.TeamMembers.Count(tm => !tm.IsDeleted)
            })
            .ToListAsync();
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto)
    {
        // Validate checklist exists
        var checklistExists = await _checklistRepository.ExistsAsync(dto.ChecklistId);
        if (!checklistExists)
            throw new KeyNotFoundException($"Checklist with ID {dto.ChecklistId} not found");

        // Generate code if not provided
        var code = dto.Code ?? await GenerateProjectCodeAsync();

        var project = new Project
        {
            Code = code,
            Name = dto.Name,
            Description = dto.Description,
            ChecklistId = dto.ChecklistId,
            ProjectTypeId = ProjectTypes.GetBySystemName(dto.ProjectType)?.Id ?? ProjectTypes.Ids.MysteryShopping,
            StatusId = ProjectStatuses.Ids.Draft,
            AssignmentTypeId = AssignmentTypes.GetBySystemName(dto.AssignmentType)?.Id ?? AssignmentTypes.Ids.InternalBranch,
            StartDate = ToUtc(dto.StartDate),
            EndDate = ToUtc(dto.EndDate),
            IsActive = true,
            TargetCount = dto.TargetCount,
            DailyQuota = dto.DailyQuota,
            WeeklyQuota = dto.WeeklyQuota,
            MonthlyQuota = dto.MonthlyQuota,
            EstimatedBudget = dto.EstimatedBudget,
            CostPerEvaluation = dto.CostPerEvaluation,
            ReportingFrequencyDays = dto.ReportingFrequencyDays,
            AutoGenerateReports = dto.AutoGenerateReports,
            ProjectManagerId = dto.ProjectManagerId,
            MinimumScoreThreshold = dto.MinimumScoreThreshold,
            Priority = dto.Priority,
            Tags = dto.Tags,
            Notes = dto.Notes,
            CustomerId = dto.CustomerId,
            OrganizationId = dto.OrganizationId,
            // Survey Settings
            SurveyIdentityTypeId = !string.IsNullOrEmpty(dto.SurveyIdentityType)
                ? SurveyIdentityTypes.GetBySystemName(dto.SurveyIdentityType)?.Id
                : null,
            EmailTemplateId = dto.EmailTemplateId,
            ReminderEmailTemplateId = dto.ReminderEmailTemplateId,
            SendReminderEmails = dto.SendReminderEmails,
            ReminderDaysBeforeEnd = dto.ReminderDaysBeforeEnd
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Add team members if provided
        if (dto.TeamMembers?.Any() == true)
        {
            foreach (var memberDto in dto.TeamMembers)
            {
                project.TeamMembers.Add(new ProjectTeamMember
                {
                    ProjectId = project.Id,
                    UserId = memberDto.UserId,
                    Role = memberDto.Role
                });
            }
        }

        await _context.SaveChangesAsync();
        return MapToDto(project);
    }

    public async Task<ProjectDto> UpdateAsync(int id, CreateProjectDto dto)
    {
        var project = await _context.Projects
            .Include(p => p.TeamMembers)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (project == null)
            throw new KeyNotFoundException($"Project with ID {id} not found");

        // Validate checklist exists
        var checklistExists = await _checklistRepository.ExistsAsync(dto.ChecklistId);
        if (!checklistExists)
            throw new KeyNotFoundException($"Checklist with ID {dto.ChecklistId} not found");

        project.Code = dto.Code ?? project.Code;
        project.Name = dto.Name;
        project.Description = dto.Description;
        project.ChecklistId = dto.ChecklistId;
        project.ProjectTypeId = ProjectTypes.GetBySystemName(dto.ProjectType)?.Id ?? project.ProjectTypeId;
        project.AssignmentTypeId = AssignmentTypes.GetBySystemName(dto.AssignmentType)?.Id ?? project.AssignmentTypeId;
        project.StartDate = ToUtc(dto.StartDate);
        project.EndDate = ToUtc(dto.EndDate);
        project.TargetCount = dto.TargetCount;
        project.DailyQuota = dto.DailyQuota;
        project.WeeklyQuota = dto.WeeklyQuota;
        project.MonthlyQuota = dto.MonthlyQuota;
        project.EstimatedBudget = dto.EstimatedBudget;
        project.CostPerEvaluation = dto.CostPerEvaluation;
        project.ReportingFrequencyDays = dto.ReportingFrequencyDays;
        project.AutoGenerateReports = dto.AutoGenerateReports;
        project.ProjectManagerId = dto.ProjectManagerId;
        project.MinimumScoreThreshold = dto.MinimumScoreThreshold;
        project.Priority = dto.Priority;
        project.Tags = dto.Tags;
        project.Notes = dto.Notes;
        project.CustomerId = dto.CustomerId;
        project.OrganizationId = dto.OrganizationId;
        // Survey Settings
        project.SurveyIdentityTypeId = !string.IsNullOrEmpty(dto.SurveyIdentityType)
            ? SurveyIdentityTypes.GetBySystemName(dto.SurveyIdentityType)?.Id
            : null;
        project.EmailTemplateId = dto.EmailTemplateId;
        project.ReminderEmailTemplateId = dto.ReminderEmailTemplateId;
        project.SendReminderEmails = dto.SendReminderEmails;
        project.ReminderDaysBeforeEnd = dto.ReminderDaysBeforeEnd;
        project.UpdatedAt = TurkeyTime.Now;

        // Update team members
        if (dto.TeamMembers != null)
        {
            // Mevcut üyeleri kaldır (soft delete)
            foreach (var existingMember in project.TeamMembers.Where(m => !m.IsDeleted).ToList())
            {
                existingMember.IsDeleted = true;
            }

            // Yeni üyeleri ekle
            var newMemberUserIds = new List<int>();
            foreach (var memberDto in dto.TeamMembers.Where(m => m.UserId > 0))
            {
                // Daha önce eklenmiş ama silinmiş mi kontrol et
                var existing = project.TeamMembers.FirstOrDefault(m => m.UserId == memberDto.UserId);
                if (existing != null)
                {
                    if (existing.IsDeleted)
                        newMemberUserIds.Add(memberDto.UserId);
                    // Geri aktif et
                    existing.IsDeleted = false;
                    existing.Role = memberDto.Role ?? "Evaluator";
                }
                else
                {
                    // Yeni ekle
                    project.TeamMembers.Add(new ProjectTeamMember
                    {
                        ProjectId = project.Id,
                        UserId = memberDto.UserId,
                        Role = memberDto.Role ?? "Evaluator",
                        AssignedAt = TurkeyTime.Now,
                        IsActive = true
                    });
                    newMemberUserIds.Add(memberDto.UserId);
                }
            }
        }

        await _context.SaveChangesAsync();

        // Yeni eklenen ekip üyelerine bildirim gönder
        if (dto.TeamMembers != null)
        {
            var newMemberIds = project.TeamMembers
                .Where(m => !m.IsDeleted && m.AssignedAt >= TurkeyTime.Now.AddSeconds(-5))
                .Select(m => m.UserId)
                .ToList();

            if (newMemberIds.Any())
            {
                await _notificationCreator.CreateBulkAsync(
                    newMemberIds,
                    NotificationTypes.Ids.Assignment,
                    "Proje Ekibine Eklendi",
                    $"{project.Name} projesinin ekibine eklendiniz.",
                    actionUrl: $"/Projects/Detail/{project.Id}",
                    relatedEntityId: project.Id,
                    relatedEntityType: "Project");
            }
        }

        return MapToDto(project);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return false;

        project.IsDeleted = true;
        project.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ProjectDto> CloseProjectAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            throw new KeyNotFoundException($"Project with ID {id} not found");

        project.IsActive = false;
        project.StatusId = ProjectStatuses.Ids.Completed;
        project.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        return MapToDto(project);
    }

    public async Task<ProjectDto> UpdateStatusAsync(int id, UpdateProjectStatusDto dto)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            throw new KeyNotFoundException($"Project with ID {id} not found");

        var statusItem = ProjectStatuses.GetBySystemName(dto.Status);
        if (statusItem != null)
        {
            project.StatusId = statusItem.Id;
            if (!string.IsNullOrEmpty(dto.Notes))
                project.Notes = (project.Notes ?? "") + "\n" + TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm") + ": " + dto.Notes;

            project.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }

        return MapToDto(project);
    }

    public async Task<ProjectDto> StartProjectAsync(int id)
    {
        return await UpdateStatusAsync(id, new UpdateProjectStatusDto { Status = "Active", Notes = "Proje baslatildi" });
    }

    public async Task<ProjectDto> PauseProjectAsync(int id)
    {
        return await UpdateStatusAsync(id, new UpdateProjectStatusDto { Status = "Paused", Notes = "Proje duraklatildi" });
    }

    public async Task<ProjectDto> CompleteProjectAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            throw new KeyNotFoundException($"Project with ID {id} not found");

        project.StatusId = ProjectStatuses.Ids.Completed;
        project.IsActive = false;
        project.Notes = (project.Notes ?? "") + "\n" + TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm") + ": Proje tamamlandi";
        project.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        // Proje ekibine bildirim
        var teamUserIds = await _context.ProjectTeamMembers
            .Where(tm => tm.ProjectId == id && !tm.IsDeleted)
            .Select(tm => tm.UserId)
            .ToListAsync();
        if (teamUserIds.Any())
        {
            await _notificationCreator.CreateBulkAsync(
                teamUserIds,
                NotificationTypes.Ids.Success,
                "Proje Tamamlandı",
                $"{project.Name} projesi tamamlandı.",
                relatedEntityId: id,
                relatedEntityType: "Project");
        }

        return MapToDto(project);
    }

    public async Task<ProjectDto> CancelProjectAsync(int id, string? reason)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            throw new KeyNotFoundException($"Project with ID {id} not found");

        project.StatusId = ProjectStatuses.Ids.Cancelled;
        project.IsActive = false;
        project.Notes = (project.Notes ?? "") + "\n" + TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm") + ": Proje iptal edildi" + (string.IsNullOrEmpty(reason) ? "" : " - " + reason);
        project.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        // Proje ekibine bildirim
        var cancelTeamUserIds = await _context.ProjectTeamMembers
            .Where(tm => tm.ProjectId == id && !tm.IsDeleted)
            .Select(tm => tm.UserId)
            .ToListAsync();
        if (cancelTeamUserIds.Any())
        {
            await _notificationCreator.CreateBulkAsync(
                cancelTeamUserIds,
                NotificationTypes.Ids.Warning,
                "Proje İptal Edildi",
                $"{project.Name} projesi iptal edildi.{(string.IsNullOrEmpty(reason) ? "" : $" Sebep: {reason}")}",
                relatedEntityId: id,
                relatedEntityType: "Project");
        }

        return MapToDto(project);
    }

    public async Task<ProjectDetailDto> ManageTeamAsync(int projectId, ManageProjectTeamDto dto)
    {
        var project = await _context.Projects
            .Include(p => p.TeamMembers)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
            throw new KeyNotFoundException($"Project with ID {projectId} not found");

        // Remove members
        foreach (var userId in dto.RemoveUserIds)
        {
            var member = project.TeamMembers.FirstOrDefault(m => m.UserId == userId);
            if (member != null)
            {
                member.IsActive = false;
                member.IsDeleted = true;
            }
        }

        // Add members
        foreach (var userId in dto.AddUserIds)
        {
            if (!project.TeamMembers.Any(m => m.UserId == userId && !m.IsDeleted))
            {
                project.TeamMembers.Add(new ProjectTeamMember
                {
                    ProjectId = projectId,
                    UserId = userId,
                    Role = "Evaluator"
                });
            }
        }

        await _context.SaveChangesAsync();
        return await GetDetailByIdAsync(projectId) ?? throw new KeyNotFoundException("Project not found after update");
    }

    public async Task<ProjectDetailDto> GetStatisticsAsync(int projectId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var detail = await GetDetailByIdAsync(projectId);
        if (detail == null)
            throw new KeyNotFoundException($"Project with ID {projectId} not found");

        // Get daily statistics
        var start = startDate ?? detail.StartDate;
        var end = endDate ?? TurkeyTime.Now;

        var evaluations = await _context.Evaluations
            .Where(e => e.ProjectId == projectId && !e.IsDeleted)
            .Where(e => e.CreatedAt >= start && e.CreatedAt <= end)
            .ToListAsync();

        var dailyStats = evaluations
            .GroupBy(e => e.CreatedAt.Date)
            .Select(g => new ProjectStatisticDto
            {
                Date = g.Key,
                TotalEvaluations = g.Count(),
                CompletedEvaluations = g.Count(e => e.StatusId == EvaluationStatuses.Ids.Completed),
                AverageScore = g.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage ?? 0),
                YellowCards = g.Sum(e => e.YellowCardCount),
                RedCards = g.Sum(e => e.RedCardCount)
            })
            .OrderBy(s => s.Date)
            .ToList();

        detail.DailyStatistics = dailyStats;
        return detail;
    }

    public async Task<IEnumerable<ProjectDto>> GetByCustomerIdAsync(int customerId)
    {
        var projects = await _context.Projects
            .Include(p => p.Checklist)
            .Include(p => p.Assignments)
            .Where(p => p.CustomerId == customerId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(MapToDto);
    }

    public async Task<IEnumerable<ProjectDto>> GetByManagerIdAsync(int managerId)
    {
        var projects = await _context.Projects
            .Include(p => p.Checklist)
            .Include(p => p.Assignments)
            .Where(p => p.ProjectManagerId == managerId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(MapToDto);
    }

    public async Task<IEnumerable<ProjectDto>> GetActiveProjectsAsync()
    {
        var projects = await _context.Projects
            .Include(p => p.Checklist)
            .Include(p => p.Assignments)
            .Where(p => p.StatusId == ProjectStatuses.Ids.Active && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(MapToDto);
    }

    public async Task<IEnumerable<ProjectDto>> GetUpcomingDeadlinesAsync(int daysAhead = 7)
    {
        var deadline = TurkeyTime.Now.AddDays(daysAhead);
        var projects = await _context.Projects
            .Include(p => p.Checklist)
            .Include(p => p.Assignments)
            .Where(p => p.EndDate <= deadline && p.EndDate >= TurkeyTime.Now && p.StatusId == ProjectStatuses.Ids.Active && !p.IsDeleted)
            .OrderBy(p => p.EndDate)
            .ToListAsync();

        return projects.Select(MapToDto);
    }

    public async Task<string> GenerateProjectCodeAsync()
    {
        var year = TurkeyTime.Now.Year;
        var prefix = $"PRJ-{year}-";

        var lastProject = await _context.Projects
            .Where(p => p.Code != null && p.Code.StartsWith(prefix))
            .OrderByDescending(p => p.Code)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastProject?.Code != null)
        {
            var parts = lastProject.Code.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var num))
            {
                nextNumber = num + 1;
            }
        }

        return $"{prefix}{nextNumber:D3}";
    }

    private ProjectDto MapToDto(Project project)
    {
        var totalAssignments = project.Assignments?.Count ?? 0;
        var completedAssignments = project.Assignments?.Count(a => a.IsCompleted) ?? 0;

        // İlerleme: Sadece OnlineSurvey için hesaplanır
        decimal completionPercentage = -1;
        if (project.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey)
        {
            var totalSent = (project.SurveyInvitations?.Count(si => !si.IsDeleted && si.StatusId == SurveyInvitationStatuses.Ids.Sent) ?? 0) +
                           (project.SurveyExternalInvitations?.Count(sei => !sei.IsDeleted && sei.StatusId == SurveyInvitationStatuses.Ids.Sent) ?? 0);
            var totalCompleted = (project.SurveyInvitations?.Count(si => !si.IsDeleted && si.IsCompleted) ?? 0) +
                                (project.SurveyExternalInvitations?.Count(sei => !sei.IsDeleted && sei.IsCompleted) ?? 0);
            completionPercentage = totalSent > 0 ? Math.Round((decimal)totalCompleted / totalSent * 100, 1) : -1;
        }

        // Calculate average score from project evaluations
        var evaluations = project.Evaluations?
            .Where(e => e.ScorePercentage.HasValue)
            .Select(e => e.ScorePercentage!.Value)
            .ToList() ?? new List<decimal>();

        var averageScore = evaluations.Any() ? Math.Round(evaluations.Average(), 1) : 0;

        // Calculate card counts from all evaluations
        var yellowCards = project.Evaluations?
            .Sum(e => e.YellowCardCount) ?? 0;

        var redCards = project.Evaluations?
            .Sum(e => e.RedCardCount) ?? 0;

        return new ProjectDto
        {
            Id = project.Id,
            Code = project.Code,
            Name = project.Name,
            Description = project.Description,
            ChecklistId = project.ChecklistId,
            ChecklistName = project.Checklist?.Name ?? "",
            ProjectType = ProjectTypes.GetById(project.ProjectTypeId)?.SystemName ?? "",
            Status = ProjectStatuses.GetById(project.StatusId)?.SystemName ?? "",
            AssignmentType = AssignmentTypes.GetById(project.AssignmentTypeId)?.SystemName ?? "",
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            IsActive = project.IsActive,
            CreatedAt = project.CreatedAt,
            TotalAssignments = totalAssignments,
            CompletedAssignments = completedAssignments,
            PendingAssignments = totalAssignments - completedAssignments,
            CompletionPercentage = completionPercentage,
            AverageScore = averageScore,
            TargetCount = project.TargetCount,
            DailyQuota = project.DailyQuota,
            WeeklyQuota = project.WeeklyQuota,
            MonthlyQuota = project.MonthlyQuota,
            EstimatedBudget = project.EstimatedBudget,
            ActualBudget = project.ActualBudget,
            CostPerEvaluation = project.CostPerEvaluation,
            ReportingFrequencyDays = project.ReportingFrequencyDays,
            AutoGenerateReports = project.AutoGenerateReports,
            LastReportDate = project.LastReportDate,
            ProjectManagerId = project.ProjectManagerId,
            ProjectManagerName = project.ProjectManager != null
                ? $"{project.ProjectManager.FirstName} {project.ProjectManager.LastName}"
                : null,
            MinimumScoreThreshold = project.MinimumScoreThreshold,
            Priority = project.Priority,
            Tags = project.Tags,
            Notes = project.Notes,
            CustomerId = project.CustomerId,
            CustomerName = project.Customer?.CompanyName,
            CustomerCode = project.Customer?.Code,
            OrganizationId = project.OrganizationId,
            OrganizationName = project.Organization?.Name,
            // Survey Settings
            SurveyIdentityType = project.SurveyIdentityTypeId.HasValue
                ? SurveyIdentityTypes.GetById(project.SurveyIdentityTypeId.Value)?.SystemName
                : null,
            EmailTemplateId = project.EmailTemplateId,
            EmailTemplateName = project.EmailTemplate?.Name,
            ReminderEmailTemplateId = project.ReminderEmailTemplateId,
            ReminderEmailTemplateName = project.ReminderEmailTemplate?.Name,
            SendReminderEmails = project.SendReminderEmails,
            ReminderDaysBeforeEnd = project.ReminderDaysBeforeEnd,
            TotalYellowCards = yellowCards,
            TotalRedCards = redCards,
            TeamMemberCount = project.TeamMembers?.Count(m => !m.IsDeleted) ?? 0,
            TargetBranchCount = 0 // Branch system removed
        };
    }

    private Task<ProjectDetailDto> MapToDetailDtoAsync(Project project)
    {
        var dto = MapToDto(project);
        var detailDto = new ProjectDetailDto
        {
            Id = dto.Id,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            ChecklistId = dto.ChecklistId,
            ChecklistName = dto.ChecklistName,
            ProjectType = dto.ProjectType,
            Status = dto.Status,
            AssignmentType = dto.AssignmentType,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt,
            TotalAssignments = dto.TotalAssignments,
            CompletedAssignments = dto.CompletedAssignments,
            PendingAssignments = dto.PendingAssignments,
            CompletionPercentage = dto.CompletionPercentage,
            AverageScore = dto.AverageScore,
            TargetCount = dto.TargetCount,
            DailyQuota = dto.DailyQuota,
            WeeklyQuota = dto.WeeklyQuota,
            MonthlyQuota = dto.MonthlyQuota,
            EstimatedBudget = dto.EstimatedBudget,
            ActualBudget = dto.ActualBudget,
            CostPerEvaluation = dto.CostPerEvaluation,
            ReportingFrequencyDays = dto.ReportingFrequencyDays,
            AutoGenerateReports = dto.AutoGenerateReports,
            LastReportDate = dto.LastReportDate,
            ProjectManagerId = dto.ProjectManagerId,
            ProjectManagerName = dto.ProjectManagerName,
            MinimumScoreThreshold = dto.MinimumScoreThreshold,
            Priority = dto.Priority,
            Tags = dto.Tags,
            Notes = dto.Notes,
            CustomerId = dto.CustomerId,
            CustomerName = dto.CustomerName,
            CustomerCode = dto.CustomerCode,
            OrganizationId = dto.OrganizationId,
            OrganizationName = dto.OrganizationName,
            // Survey Settings
            SurveyIdentityType = dto.SurveyIdentityType,
            EmailTemplateId = dto.EmailTemplateId,
            EmailTemplateName = dto.EmailTemplateName,
            ReminderEmailTemplateId = dto.ReminderEmailTemplateId,
            ReminderEmailTemplateName = dto.ReminderEmailTemplateName,
            SendReminderEmails = dto.SendReminderEmails,
            ReminderDaysBeforeEnd = dto.ReminderDaysBeforeEnd,
            TotalYellowCards = dto.TotalYellowCards,
            TotalRedCards = dto.TotalRedCards,
            TeamMemberCount = dto.TeamMemberCount,
            TargetBranchCount = dto.TargetBranchCount
        };

        // Map team members
        detailDto.TeamMembers = project.TeamMembers?
            .Where(tm => !tm.IsDeleted)
            .Select(tm =>
            {
                var completedEvals = project.Evaluations?
                    .Count(e => e.EvaluatorId == tm.UserId) ?? 0;

                return new ProjectTeamMemberDto
                {
                    Id = tm.Id,
                    UserId = tm.UserId,
                    UserName = tm.User != null ? $"{tm.User.FirstName} {tm.User.LastName}" : "",
                    Email = tm.User?.Email,
                    Role = tm.Role,
                    AssignedAt = tm.AssignedAt,
                    IsActive = tm.IsActive,
                    CompletedEvaluations = completedEvals
                };
            })
            .ToList() ?? new List<ProjectTeamMemberDto>();

        return Task.FromResult(detailDto);
    }
}
