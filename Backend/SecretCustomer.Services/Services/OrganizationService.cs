using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Organization;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class OrganizationService : IOrganizationService
{
    private readonly ApplicationDbContext _context;

    public OrganizationService(ApplicationDbContext context)
    {
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

    #region OrganizationUnit Methods

    public async Task<List<OrganizationUnitDto>> GetAllOrganizationUnitsAsync()
    {
        var units = await _context.OrganizationUnits
            .Include(o => o.Parent)
            .Include(o => o.Manager)
            .Include(o => o.Branch)
            .Include(o => o.Users)
            .Include(o => o.Children)
            .OrderBy(o => o.Level)
            .ThenBy(o => o.Order)
            .ThenBy(o => o.Name)
            .ToListAsync();

        return units.Select(MapToOrganizationUnitDto).ToList();
    }

    public async Task<OrganizationUnitDto?> GetOrganizationUnitByIdAsync(int id)
    {
        var unit = await _context.OrganizationUnits
            .Include(o => o.Parent)
            .Include(o => o.Manager)
            .Include(o => o.Branch)
            .Include(o => o.Users)
            .Include(o => o.Children)
            .FirstOrDefaultAsync(o => o.Id == id);

        return unit == null ? null : MapToOrganizationUnitDto(unit);
    }

    public async Task<List<OrganizationTreeNodeDto>> GetOrganizationTreeAsync()
    {
        var allUnits = await _context.OrganizationUnits
            .Include(o => o.Manager)
            .Include(o => o.Users)
            .Where(o => o.IsActive)
            .OrderBy(o => o.Order)
            .ThenBy(o => o.Name)
            .ToListAsync();

        // Build tree from root nodes (no parent)
        var rootNodes = allUnits.Where(o => o.ParentId == null).ToList();
        return rootNodes.Select(root => BuildTreeNode(root, allUnits)).ToList();
    }

    private OrganizationTreeNodeDto BuildTreeNode(OrganizationUnit unit, List<OrganizationUnit> allUnits)
    {
        var children = allUnits.Where(o => o.ParentId == unit.Id).ToList();
        return new OrganizationTreeNodeDto
        {
            Id = unit.Id,
            Name = unit.Name,
            Code = unit.Code,
            UnitType = unit.UnitType,
            UnitTypeName = GetUnitTypeName(unit.UnitType),
            Level = unit.Level,
            IsActive = unit.IsActive,
            ManagerName = unit.Manager != null ? $"{unit.Manager.FirstName} {unit.Manager.LastName}" : null,
            UserCount = unit.Users?.Count ?? 0,
            Children = children.Select(c => BuildTreeNode(c, allUnits)).ToList()
        };
    }

    public async Task<List<OrganizationUnitDto>> GetOrganizationUnitsAsync(OrganizationUnitFilterDto filter)
    {
        var query = _context.OrganizationUnits
            .Include(o => o.Parent)
            .Include(o => o.Manager)
            .Include(o => o.Branch)
            .Include(o => o.Users)
            .Include(o => o.Children)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(o =>
                o.Name.ToLower().Contains(searchLower) ||
                (o.Code != null && o.Code.ToLower().Contains(searchLower)) ||
                (o.Description != null && o.Description.ToLower().Contains(searchLower)));
        }

        if (filter.UnitType.HasValue)
        {
            query = query.Where(o => o.UnitType == filter.UnitType.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(o => o.IsActive == filter.IsActive.Value);
        }

        if (filter.ParentId.HasValue)
        {
            query = query.Where(o => o.ParentId == filter.ParentId.Value);
        }

        if (filter.BranchId.HasValue)
        {
            query = query.Where(o => o.BranchId == filter.BranchId.Value);
        }

        // Order and paginate
        var units = await query
            .OrderBy(o => o.Level)
            .ThenBy(o => o.Order)
            .ThenBy(o => o.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return units.Select(MapToOrganizationUnitDto).ToList();
    }

    public async Task<OrganizationUnitDto> CreateOrganizationUnitAsync(CreateOrganizationUnitDto dto)
    {
        // Calculate level based on parent
        int level = 0;
        if (dto.ParentId.HasValue)
        {
            var parent = await _context.OrganizationUnits.FindAsync(dto.ParentId.Value);
            if (parent != null)
            {
                level = parent.Level + 1;
            }
        }

        var unit = new OrganizationUnit
        {
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            UnitType = dto.UnitType,
            Level = level,
            Order = dto.Order,
            IsActive = dto.IsActive,
            ParentId = dto.ParentId,
            ManagerId = dto.ManagerId,
            BranchId = dto.BranchId
        };

        _context.OrganizationUnits.Add(unit);
        await _context.SaveChangesAsync();

        return await GetOrganizationUnitByIdAsync(unit.Id) ?? MapToOrganizationUnitDto(unit);
    }

    public async Task<OrganizationUnitDto> UpdateOrganizationUnitAsync(int id, UpdateOrganizationUnitDto dto)
    {
        var unit = await _context.OrganizationUnits.FindAsync(id);
        if (unit == null)
        {
            throw new InvalidOperationException("Organizasyon birimi bulunamadı.");
        }

        // Check for circular reference
        if (dto.ParentId.HasValue && dto.ParentId.Value == id)
        {
            throw new InvalidOperationException("Bir birim kendisinin üst birimi olamaz.");
        }

        // Check if new parent is a descendant of current unit
        if (dto.ParentId.HasValue)
        {
            var isDescendant = await IsDescendantOfAsync(dto.ParentId.Value, id);
            if (isDescendant)
            {
                throw new InvalidOperationException("Bir birim kendi alt birimi altına taşınamaz.");
            }
        }

        // Calculate new level
        int newLevel = 0;
        if (dto.ParentId.HasValue)
        {
            var parent = await _context.OrganizationUnits.FindAsync(dto.ParentId.Value);
            if (parent != null)
            {
                newLevel = parent.Level + 1;
            }
        }

        // If level changed, update all descendants
        if (unit.Level != newLevel || unit.ParentId != dto.ParentId)
        {
            await UpdateDescendantLevelsAsync(id, newLevel - unit.Level);
        }

        unit.Name = dto.Name;
        unit.Code = dto.Code;
        unit.Description = dto.Description;
        unit.UnitType = dto.UnitType;
        unit.Level = newLevel;
        unit.Order = dto.Order;
        unit.IsActive = dto.IsActive;
        unit.ParentId = dto.ParentId;
        unit.ManagerId = dto.ManagerId;
        unit.BranchId = dto.BranchId;

        await _context.SaveChangesAsync();

        return await GetOrganizationUnitByIdAsync(id) ?? MapToOrganizationUnitDto(unit);
    }

    private async Task<bool> IsDescendantOfAsync(int potentialDescendantId, int ancestorId)
    {
        var current = await _context.OrganizationUnits.FindAsync(potentialDescendantId);
        while (current != null)
        {
            if (current.ParentId == ancestorId)
            {
                return true;
            }
            if (!current.ParentId.HasValue)
            {
                break;
            }
            current = await _context.OrganizationUnits.FindAsync(current.ParentId);
        }
        return false;
    }

    private async Task UpdateDescendantLevelsAsync(int parentId, int levelDelta)
    {
        var children = await _context.OrganizationUnits
            .Where(o => o.ParentId == parentId)
            .ToListAsync();

        foreach (var child in children)
        {
            child.Level += levelDelta;
            await UpdateDescendantLevelsAsync(child.Id, levelDelta);
        }
    }

    public async Task<bool> DeleteOrganizationUnitAsync(int id)
    {
        var unit = await _context.OrganizationUnits
            .Include(o => o.Children)
            .Include(o => o.Users)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (unit == null)
        {
            return false;
        }

        // Check if unit has children
        if (unit.Children.Any())
        {
            throw new InvalidOperationException("Alt birimleri olan bir birim silinemez. Önce alt birimleri silin veya taşıyın.");
        }

        // Check if unit has users
        if (unit.Users.Any())
        {
            throw new InvalidOperationException("Kullanıcıları olan bir birim silinemez. Önce kullanıcıları başka birime taşıyın.");
        }

        unit.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<OrganizationUnitDto>> GetChildrenAsync(int parentId)
    {
        var children = await _context.OrganizationUnits
            .Include(o => o.Parent)
            .Include(o => o.Manager)
            .Include(o => o.Branch)
            .Include(o => o.Users)
            .Include(o => o.Children)
            .Where(o => o.ParentId == parentId)
            .OrderBy(o => o.Order)
            .ThenBy(o => o.Name)
            .ToListAsync();

        return children.Select(MapToOrganizationUnitDto).ToList();
    }

    public async Task<bool> MoveOrganizationUnitAsync(int id, int? newParentId)
    {
        var unit = await _context.OrganizationUnits.FindAsync(id);
        if (unit == null)
        {
            return false;
        }

        // Check for circular reference
        if (newParentId.HasValue)
        {
            if (newParentId.Value == id)
            {
                throw new InvalidOperationException("Bir birim kendisinin üst birimi olamaz.");
            }

            var isDescendant = await IsDescendantOfAsync(newParentId.Value, id);
            if (isDescendant)
            {
                throw new InvalidOperationException("Bir birim kendi alt birimi altına taşınamaz.");
            }
        }

        // Calculate new level
        int newLevel = 0;
        if (newParentId.HasValue)
        {
            var parent = await _context.OrganizationUnits.FindAsync(newParentId.Value);
            if (parent != null)
            {
                newLevel = parent.Level + 1;
            }
        }

        int levelDelta = newLevel - unit.Level;
        await UpdateDescendantLevelsAsync(id, levelDelta);

        unit.ParentId = newParentId;
        unit.Level = newLevel;

        await _context.SaveChangesAsync();
        return true;
    }

    private OrganizationUnitDto MapToOrganizationUnitDto(OrganizationUnit unit)
    {
        return new OrganizationUnitDto
        {
            Id = unit.Id,
            Name = unit.Name,
            Code = unit.Code,
            Description = unit.Description,
            UnitType = unit.UnitType,
            UnitTypeName = GetUnitTypeName(unit.UnitType),
            Level = unit.Level,
            Order = unit.Order,
            IsActive = unit.IsActive,
            ParentId = unit.ParentId,
            ParentName = unit.Parent?.Name,
            ManagerId = unit.ManagerId,
            ManagerName = unit.Manager != null ? $"{unit.Manager.FirstName} {unit.Manager.LastName}" : null,
            BranchId = unit.BranchId,
            BranchName = unit.Branch?.Name,
            UserCount = unit.Users?.Count ?? 0,
            ChildCount = unit.Children?.Count ?? 0,
            CreatedAt = unit.CreatedAt
        };
    }

    private string GetUnitTypeName(OrganizationUnitType type)
    {
        return type switch
        {
            OrganizationUnitType.Company => "Şirket",
            OrganizationUnitType.Department => "Departman",
            OrganizationUnitType.Division => "Bölüm",
            OrganizationUnitType.Team => "Takım",
            OrganizationUnitType.Unit => "Birim",
            _ => type.ToString()
        };
    }

    #endregion

    #region Delegation Methods

    public async Task<List<DelegationDto>> GetAllDelegationsAsync()
    {
        var delegations = await _context.Delegations
            .Include(d => d.DelegatorUser)
            .Include(d => d.DelegateeUser)
            .Include(d => d.DelegatorOrganizationUnit)
            .Include(d => d.DelegateeOrganizationUnit)
            .Include(d => d.ApprovedByUser)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return delegations.Select(MapToDelegationDto).ToList();
    }

    public async Task<DelegationDto?> GetDelegationByIdAsync(int id)
    {
        var delegation = await _context.Delegations
            .Include(d => d.DelegatorUser)
            .Include(d => d.DelegateeUser)
            .Include(d => d.DelegatorOrganizationUnit)
            .Include(d => d.DelegateeOrganizationUnit)
            .Include(d => d.ApprovedByUser)
            .FirstOrDefaultAsync(d => d.Id == id);

        return delegation == null ? null : MapToDelegationDto(delegation);
    }

    public async Task<PagedDelegationResult> GetDelegationsAsync(DelegationFilterDto filter)
    {
        var query = _context.Delegations
            .Include(d => d.DelegatorUser)
            .Include(d => d.DelegateeUser)
            .Include(d => d.DelegatorOrganizationUnit)
            .Include(d => d.DelegateeOrganizationUnit)
            .Include(d => d.ApprovedByUser)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(d =>
                (d.Title != null && d.Title.ToLower().Contains(searchLower)) ||
                (d.Description != null && d.Description.ToLower().Contains(searchLower)) ||
                d.DelegatorUser.FirstName.ToLower().Contains(searchLower) ||
                d.DelegatorUser.LastName.ToLower().Contains(searchLower) ||
                d.DelegateeUser.FirstName.ToLower().Contains(searchLower) ||
                d.DelegateeUser.LastName.ToLower().Contains(searchLower));
        }

        if (filter.DelegatorUserId.HasValue)
        {
            query = query.Where(d => d.DelegatorUserId == filter.DelegatorUserId.Value);
        }

        if (filter.DelegateeUserId.HasValue)
        {
            query = query.Where(d => d.DelegateeUserId == filter.DelegateeUserId.Value);
        }

        if (filter.DelegationType.HasValue)
        {
            query = query.Where(d => d.DelegationType == filter.DelegationType.Value);
        }

        if (filter.Reason.HasValue)
        {
            query = query.Where(d => d.Reason == filter.Reason.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == filter.IsActive.Value);
        }

        if (filter.IsApproved.HasValue)
        {
            query = query.Where(d => d.IsApproved == filter.IsApproved.Value);
        }

        if (filter.IsCurrent.HasValue && filter.IsCurrent.Value)
        {
            var now = DateTime.UtcNow;
            query = query.Where(d =>
                d.IsActive &&
                d.IsApproved &&
                d.StartDate <= now &&
                (!d.EndDate.HasValue || d.EndDate.Value >= now));
        }

        if (filter.StartDateFrom.HasValue)
        {
            query = query.Where(d => d.StartDate >= filter.StartDateFrom.Value);
        }

        if (filter.StartDateTo.HasValue)
        {
            query = query.Where(d => d.StartDate <= filter.StartDateTo.Value);
        }

        var totalCount = await query.CountAsync();

        var delegations = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedDelegationResult
        {
            Items = delegations.Select(MapToDelegationDto).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DelegationDto> CreateDelegationAsync(CreateDelegationDto dto)
    {
        // Validate users exist
        var delegator = await _context.Users.FindAsync(dto.DelegatorUserId);
        var delegatee = await _context.Users.FindAsync(dto.DelegateeUserId);

        if (delegator == null)
        {
            throw new InvalidOperationException("Vekalet veren kullanıcı bulunamadı.");
        }

        if (delegatee == null)
        {
            throw new InvalidOperationException("Vekalet alan kullanıcı bulunamadı.");
        }

        if (dto.DelegatorUserId == dto.DelegateeUserId)
        {
            throw new InvalidOperationException("Bir kullanıcı kendisine vekalet veremez.");
        }

        // Check for overlapping delegations
        var hasOverlap = await _context.Delegations.AnyAsync(d =>
            d.DelegatorUserId == dto.DelegatorUserId &&
            d.IsActive &&
            d.StartDate <= (dto.EndDate ?? DateTime.MaxValue) &&
            (!d.EndDate.HasValue || d.EndDate >= dto.StartDate));

        if (hasOverlap)
        {
            throw new InvalidOperationException("Bu tarih aralığında zaten aktif bir vekalet bulunmaktadır.");
        }

        var delegation = new Delegation
        {
            Title = dto.Title,
            Description = dto.Description,
            DelegatorUserId = dto.DelegatorUserId,
            DelegatorOrganizationUnitId = dto.DelegatorOrganizationUnitId,
            DelegateeUserId = dto.DelegateeUserId,
            DelegateeOrganizationUnitId = dto.DelegateeOrganizationUnitId,
            StartDate = ToUtc(dto.StartDate),
            EndDate = ToUtc(dto.EndDate),
            IsActive = true,
            DelegationType = dto.DelegationType,
            PermissionScope = dto.PermissionScope,
            Reason = dto.Reason,
            Notes = dto.Notes,
            IsApproved = false
        };

        _context.Delegations.Add(delegation);
        await _context.SaveChangesAsync();

        return await GetDelegationByIdAsync(delegation.Id) ?? MapToDelegationDto(delegation);
    }

    public async Task<DelegationDto> UpdateDelegationAsync(int id, UpdateDelegationDto dto)
    {
        var delegation = await _context.Delegations.FindAsync(id);
        if (delegation == null)
        {
            throw new InvalidOperationException("Vekalet bulunamadı.");
        }

        if (delegation.DelegatorUserId == dto.DelegateeUserId)
        {
            throw new InvalidOperationException("Bir kullanıcı kendisine vekalet veremez.");
        }

        delegation.Title = dto.Title;
        delegation.Description = dto.Description;
        delegation.DelegateeUserId = dto.DelegateeUserId;
        delegation.DelegateeOrganizationUnitId = dto.DelegateeOrganizationUnitId;
        delegation.StartDate = ToUtc(dto.StartDate);
        delegation.EndDate = ToUtc(dto.EndDate);
        delegation.IsActive = dto.IsActive;
        delegation.DelegationType = dto.DelegationType;
        delegation.PermissionScope = dto.PermissionScope;
        delegation.Reason = dto.Reason;
        delegation.Notes = dto.Notes;

        await _context.SaveChangesAsync();

        return await GetDelegationByIdAsync(id) ?? MapToDelegationDto(delegation);
    }

    public async Task<bool> DeleteDelegationAsync(int id)
    {
        var delegation = await _context.Delegations.FindAsync(id);
        if (delegation == null)
        {
            return false;
        }

        delegation.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DelegationDto> ApproveDelegationAsync(int id, int approverUserId, ApproveDelegationDto dto)
    {
        var delegation = await _context.Delegations.FindAsync(id);
        if (delegation == null)
        {
            throw new InvalidOperationException("Vekalet bulunamadı.");
        }

        delegation.IsApproved = dto.IsApproved;
        delegation.ApprovedAt = DateTime.UtcNow;
        delegation.ApprovedByUserId = approverUserId;

        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            delegation.Notes = (delegation.Notes ?? "") + $"\n[Onay Notu]: {dto.Notes}";
        }

        await _context.SaveChangesAsync();

        return await GetDelegationByIdAsync(id) ?? MapToDelegationDto(delegation);
    }

    public async Task<List<DelegationDto>> GetActiveDelegationsForUserAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var delegations = await _context.Delegations
            .Include(d => d.DelegatorUser)
            .Include(d => d.DelegateeUser)
            .Include(d => d.DelegatorOrganizationUnit)
            .Include(d => d.DelegateeOrganizationUnit)
            .Where(d =>
                (d.DelegatorUserId == userId || d.DelegateeUserId == userId) &&
                d.IsActive &&
                d.IsApproved &&
                d.StartDate <= now &&
                (!d.EndDate.HasValue || d.EndDate >= now))
            .OrderByDescending(d => d.StartDate)
            .ToListAsync();

        return delegations.Select(MapToDelegationDto).ToList();
    }

    public async Task<List<DelegationDto>> GetDelegationsGivenByUserAsync(int userId)
    {
        var delegations = await _context.Delegations
            .Include(d => d.DelegatorUser)
            .Include(d => d.DelegateeUser)
            .Include(d => d.DelegatorOrganizationUnit)
            .Include(d => d.DelegateeOrganizationUnit)
            .Include(d => d.ApprovedByUser)
            .Where(d => d.DelegatorUserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return delegations.Select(MapToDelegationDto).ToList();
    }

    public async Task<List<DelegationDto>> GetDelegationsReceivedByUserAsync(int userId)
    {
        var delegations = await _context.Delegations
            .Include(d => d.DelegatorUser)
            .Include(d => d.DelegateeUser)
            .Include(d => d.DelegatorOrganizationUnit)
            .Include(d => d.DelegateeOrganizationUnit)
            .Include(d => d.ApprovedByUser)
            .Where(d => d.DelegateeUserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return delegations.Select(MapToDelegationDto).ToList();
    }

    private DelegationDto MapToDelegationDto(Delegation delegation)
    {
        return new DelegationDto
        {
            Id = delegation.Id,
            Title = delegation.Title,
            Description = delegation.Description,
            DelegatorUserId = delegation.DelegatorUserId,
            DelegatorUserName = $"{delegation.DelegatorUser?.FirstName} {delegation.DelegatorUser?.LastName}".Trim(),
            DelegatorOrganizationUnitId = delegation.DelegatorOrganizationUnitId,
            DelegatorOrganizationUnitName = delegation.DelegatorOrganizationUnit?.Name,
            DelegateeUserId = delegation.DelegateeUserId,
            DelegateeUserName = $"{delegation.DelegateeUser?.FirstName} {delegation.DelegateeUser?.LastName}".Trim(),
            DelegateeOrganizationUnitId = delegation.DelegateeOrganizationUnitId,
            DelegateeOrganizationUnitName = delegation.DelegateeOrganizationUnit?.Name,
            StartDate = delegation.StartDate,
            EndDate = delegation.EndDate,
            IsActive = delegation.IsActive,
            DelegationType = delegation.DelegationType,
            DelegationTypeName = GetDelegationTypeName(delegation.DelegationType),
            PermissionScope = delegation.PermissionScope,
            Reason = delegation.Reason,
            ReasonName = GetDelegationReasonName(delegation.Reason),
            Notes = delegation.Notes,
            IsApproved = delegation.IsApproved,
            ApprovedAt = delegation.ApprovedAt,
            ApprovedByUserId = delegation.ApprovedByUserId,
            ApprovedByUserName = delegation.ApprovedByUser != null
                ? $"{delegation.ApprovedByUser.FirstName} {delegation.ApprovedByUser.LastName}".Trim()
                : null,
            CreatedAt = delegation.CreatedAt
        };
    }

    private string GetDelegationTypeName(DelegationType type)
    {
        return type switch
        {
            DelegationType.Full => "Tam Yetki",
            DelegationType.ReadOnly => "Salt Okuma",
            DelegationType.ApprovalOnly => "Sadece Onay",
            _ => type.ToString()
        };
    }

    private string GetDelegationReasonName(DelegationReason reason)
    {
        return reason switch
        {
            DelegationReason.AnnualLeave => "Yıllık İzin",
            DelegationReason.SickLeave => "Hastalık İzni",
            DelegationReason.BusinessTrip => "İş Seyahati",
            DelegationReason.Training => "Eğitim",
            DelegationReason.Other => "Diğer",
            _ => reason.ToString()
        };
    }

    #endregion
}
