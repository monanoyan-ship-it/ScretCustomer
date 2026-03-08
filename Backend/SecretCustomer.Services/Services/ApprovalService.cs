using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Approval;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _context;

    public ApprovalService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(List<ApprovalListDto> Items, int TotalCount)> GetApprovalsAsync(ApprovalFilterDto filter)
    {
        var query = _context.Approvals
            .Include(a => a.RequestedByUser)
            .Include(a => a.RequestedByCustomerPersonnel)
            .Include(a => a.ApproverUser)
            .Include(a => a.ApprovedByUser)
            .AsQueryable();

        // Çoklu ApprovalTypes filtresi
        if (filter.ApprovalTypes?.Any() == true)
        {
            var typeIds = filter.ApprovalTypes
                .Select(t => ApprovalTypes.GetBySystemName(t)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (typeIds.Any())
                query = query.Where(a => typeIds.Contains(a.ApprovalTypeId));
        }

        // Çoklu Statuses filtresi
        if (filter.Statuses?.Any() == true)
        {
            var statusIds = filter.Statuses
                .Select(s => ApprovalStatuses.GetBySystemName(s)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (statusIds.Any())
                query = query.Where(a => statusIds.Contains(a.StatusId));
        }

        // Çoklu Priorities filtresi
        if (filter.Priorities?.Any() == true)
        {
            var priorityIds = filter.Priorities
                .Select(p => NotificationPriorities.GetBySystemName(p)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (priorityIds.Any())
                query = query.Where(a => priorityIds.Contains(a.PriorityId));
        }

        // Çoklu RequestedByUserIds filtresi
        if (filter.RequestedByUserIds?.Any() == true)
            query = query.Where(a => a.RequestedByUserId.HasValue && filter.RequestedByUserIds.Contains(a.RequestedByUserId.Value));

        // Çoklu ApproverUserIds filtresi
        if (filter.ApproverUserIds?.Any() == true)
            query = query.Where(a => a.ApproverUserId.HasValue && filter.ApproverUserIds.Contains(a.ApproverUserId.Value));

        // DateRanges pattern
        if (filter.DateRanges?.Any() == true)
        {
            query = DateRangeHelper.ApplyOrFilter(query, filter.DateRanges, "RequestedAt");
        }

        if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
            query = query.Where(a => a.DueDate.HasValue && a.DueDate.Value < TurkeyTime.Now && a.StatusId == ApprovalStatuses.Ids.Pending);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(searchTerm) ||
                                     a.ReferenceNumber.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        // Dynamic sorting
        var isAscending = filter.SortDirection?.ToLower() == "asc";
        IOrderedQueryable<Approval> orderedQuery = filter.SortBy?.ToLower() switch
        {
            "referencenumber" => isAscending ? query.OrderBy(a => a.ReferenceNumber) : query.OrderByDescending(a => a.ReferenceNumber),
            "approvaltype" => isAscending ? query.OrderBy(a => a.ApprovalTypeId) : query.OrderByDescending(a => a.ApprovalTypeId),
            "status" => isAscending ? query.OrderBy(a => a.StatusId) : query.OrderByDescending(a => a.StatusId),
            "title" => isAscending ? query.OrderBy(a => a.Title) : query.OrderByDescending(a => a.Title),
            "requestedbyusername" => isAscending
                ? query.OrderBy(a => a.RequestedByUser != null ? a.RequestedByUser.FirstName : a.RequestedByCustomerPersonnel != null ? a.RequestedByCustomerPersonnel.FirstName : "")
                : query.OrderByDescending(a => a.RequestedByUser != null ? a.RequestedByUser.FirstName : a.RequestedByCustomerPersonnel != null ? a.RequestedByCustomerPersonnel.FirstName : ""),
            "requestedat" => isAscending ? query.OrderBy(a => a.RequestedAt) : query.OrderByDescending(a => a.RequestedAt),
            "priority" => isAscending ? query.OrderBy(a => a.PriorityId) : query.OrderByDescending(a => a.PriorityId),
            _ => query.OrderByDescending(a => a.RequestedAt)
        };

        var approvals = await orderedQuery
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new ApprovalListDto
            {
                Id = a.Id,
                ReferenceNumber = a.ReferenceNumber,
                ApprovalType = ApprovalTypes.GetById(a.ApprovalTypeId)!.SystemName,
                Status = ApprovalStatuses.GetById(a.StatusId)!.SystemName,
                Title = a.Title,
                RequestedByUserName = a.RequestedByUser != null
                        ? a.RequestedByUser.FirstName + " " + a.RequestedByUser.LastName
                        : a.RequestedByCustomerPersonnel != null
                            ? a.RequestedByCustomerPersonnel.FirstName + " " + a.RequestedByCustomerPersonnel.LastName
                            : "-",
                RequestedAt = a.RequestedAt,
                DueDate = a.DueDate,
                Priority = NotificationPriorities.GetById(a.PriorityId)!.SystemName,
                RelatedEntityId = a.RelatedEntityId,
                RelatedEntityType = a.RelatedEntityType
            })
            .ToListAsync();

        return (approvals, totalCount);
    }

    public async Task<List<ApprovalListDto>> GetMyPendingApprovalsAsync(int userId)
    {
        var approvals = await _context.Approvals
            .Include(a => a.RequestedByUser)
            .Include(a => a.RequestedByCustomerPersonnel)
            .Where(a => a.ApproverUserId == userId && a.StatusId == ApprovalStatuses.Ids.Pending)
            .OrderByDescending(a => a.PriorityId)
            .ThenBy(a => a.DueDate)
            .Select(a => new ApprovalListDto
            {
                Id = a.Id,
                ReferenceNumber = a.ReferenceNumber,
                ApprovalType = ApprovalTypes.GetById(a.ApprovalTypeId)!.SystemName,
                Status = ApprovalStatuses.GetById(a.StatusId)!.SystemName,
                Title = a.Title,
                RequestedByUserName = a.RequestedByUser != null
                        ? a.RequestedByUser.FirstName + " " + a.RequestedByUser.LastName
                        : a.RequestedByCustomerPersonnel != null
                            ? a.RequestedByCustomerPersonnel.FirstName + " " + a.RequestedByCustomerPersonnel.LastName
                            : "-",
                RequestedAt = a.RequestedAt,
                DueDate = a.DueDate,
                Priority = NotificationPriorities.GetById(a.PriorityId)!.SystemName,
                RelatedEntityId = a.RelatedEntityId,
                RelatedEntityType = a.RelatedEntityType
            })
            .ToListAsync();

        return approvals;
    }

    public async Task<ApprovalDto?> GetApprovalAsync(int id)
    {
        var approval = await _context.Approvals
            .Include(a => a.RequestedByUser)
            .Include(a => a.RequestedByCustomerPersonnel)
            .Include(a => a.ApproverUser)
            .Include(a => a.ApprovedByUser)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (approval == null)
            return null;

        var dto = new ApprovalDto
        {
            Id = approval.Id,
            ReferenceNumber = approval.ReferenceNumber,
            ApprovalType = ApprovalTypes.GetById(approval.ApprovalTypeId)?.SystemName ?? "",
            Status = ApprovalStatuses.GetById(approval.StatusId)?.SystemName ?? "",
            Title = approval.Title,
            Description = approval.Description,
            RelatedEntityId = approval.RelatedEntityId,
            RelatedEntityType = approval.RelatedEntityType,
            RequestedByUserId = approval.RequestedByUserId,
            RequestedByUserName = approval.RequestedByUser != null
                ? $"{approval.RequestedByUser.FirstName} {approval.RequestedByUser.LastName}"
                : approval.RequestedByCustomerPersonnel != null
                    ? $"{approval.RequestedByCustomerPersonnel.FirstName} {approval.RequestedByCustomerPersonnel.LastName}"
                    : "-",
            ApproverUserId = approval.ApproverUserId,
            ApproverUserName = approval.ApproverUser != null ? $"{approval.ApproverUser.FirstName} {approval.ApproverUser.LastName}" : null,
            ApprovedByUserId = approval.ApprovedByUserId,
            ApprovedByUserName = approval.ApprovedByUser != null ? $"{approval.ApprovedByUser.FirstName} {approval.ApprovedByUser.LastName}" : null,
            RequestedAt = approval.RequestedAt,
            DueDate = approval.DueDate,
            RespondedAt = approval.RespondedAt,
            ResponseNote = approval.ResponseNote,
            Priority = NotificationPriorities.GetById(approval.PriorityId)?.SystemName ?? "",
            ApprovalLevel = approval.ApprovalLevel,
            RequiredApprovalLevels = approval.RequiredApprovalLevels,
            CreatedAt = approval.CreatedAt
        };

        return dto;
    }

    public async Task<(int Id, string ReferenceNumber)> CreateApprovalAsync(Approval approval)
    {
        _context.Approvals.Add(approval);
        await _context.SaveChangesAsync();
        return (approval.Id, approval.ReferenceNumber);
    }

    public async Task<Approval?> FindByIdAsync(int id)
    {
        return await _context.Approvals.FindAsync(id);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<string> GenerateApprovalNumberAsync()
    {
        var year = TurkeyTime.Now.Year;
        var count = await _context.Approvals.CountAsync(a => a.CreatedAt.Year == year) + 1;
        return $"APR-{year}-{count:D4}";
    }

    public async Task<ApprovalSummaryDto> GetSummaryAsync()
    {
        var now = TurkeyTime.Now;
        var today = now.Date;

        var summary = new ApprovalSummaryDto
        {
            TotalApprovals = await _context.Approvals.CountAsync(),
            PendingApprovals = await _context.Approvals.CountAsync(a => a.StatusId == ApprovalStatuses.Ids.Pending),
            ApprovedCount = await _context.Approvals.CountAsync(a => a.StatusId == ApprovalStatuses.Ids.Approved),
            RejectedCount = await _context.Approvals.CountAsync(a => a.StatusId == ApprovalStatuses.Ids.Rejected),
            OverdueCount = await _context.Approvals.CountAsync(a => a.StatusId == ApprovalStatuses.Ids.Pending && a.DueDate.HasValue && a.DueDate.Value < now),
            TodayApprovals = await _context.Approvals.CountAsync(a => a.RequestedAt.Date == today),
            RecentApprovals = await _context.Approvals
                .Include(a => a.RequestedByUser)
                .Include(a => a.RequestedByCustomerPersonnel)
                .OrderByDescending(a => a.RequestedAt)
                .Take(5)
                .Select(a => new ApprovalListDto
                {
                    Id = a.Id,
                    ReferenceNumber = a.ReferenceNumber,
                    ApprovalType = ApprovalTypes.GetById(a.ApprovalTypeId)!.SystemName,
                    Status = ApprovalStatuses.GetById(a.StatusId)!.SystemName,
                    Title = a.Title,
                    RequestedByUserName = a.RequestedByUser != null
                        ? a.RequestedByUser.FirstName + " " + a.RequestedByUser.LastName
                        : a.RequestedByCustomerPersonnel != null
                            ? a.RequestedByCustomerPersonnel.FirstName + " " + a.RequestedByCustomerPersonnel.LastName
                            : "-",
                    RequestedAt = a.RequestedAt,
                    DueDate = a.DueDate,
                    Priority = NotificationPriorities.GetById(a.PriorityId)!.SystemName,
                    RelatedEntityId = a.RelatedEntityId,
                    RelatedEntityType = a.RelatedEntityType
                })
                .ToListAsync()
        };

        return summary;
    }
}
