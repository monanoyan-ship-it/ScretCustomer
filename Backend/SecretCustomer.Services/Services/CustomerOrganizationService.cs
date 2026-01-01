using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.CustomerOrganization;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class CustomerOrganizationService : ICustomerOrganizationService
{
    private readonly ApplicationDbContext _context;

    public CustomerOrganizationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<object> DebugCheckPersonnelAsync()
    {
        // Tüm organizasyonları ve personel sayılarını getir
        var orgs = await _context.CustomerOrganizations
            .Include(o => o.Personnel)
            .Select(o => new
            {
                OrgId = o.Id,
                OrgName = o.Name,
                CustomerName = o.Customer != null ? o.Customer.CompanyName : "",
                PersonnelCollectionCount = o.Personnel.Count,
                PersonnelIds = o.Personnel.Select(p => p.Id).ToList()
            })
            .ToListAsync();

        // Tüm personellerin OrganizationId'lerini getir
        var personnel = await _context.CustomerPersonnel
            .Select(p => new
            {
                PersonnelId = p.Id,
                FullName = p.FirstName + " " + p.LastName,
                Role = (int)p.Role,
                RoleName = p.Role.ToString(),
                SupervisorId = p.SupervisorId,
                SupervisorName = p.Supervisor != null ? p.Supervisor.FirstName + " " + p.Supervisor.LastName : "NULL",
                OrganizationId = p.OrganizationId,
                OrganizationName = p.Organization != null ? p.Organization.Name : "NULL"
            })
            .ToListAsync();

        return new
        {
            Organizations = orgs,
            Personnel = personnel,
            Summary = new
            {
                TotalOrgs = orgs.Count,
                TotalPersonnel = personnel.Count,
                PersonnelWithOrg = personnel.Count(p => p.OrganizationId != null),
                PersonnelWithoutOrg = personnel.Count(p => p.OrganizationId == null)
            }
        };
    }

    public async Task<CustomerOrganizationDto?> GetByIdAsync(int id)
    {
        var org = await _context.CustomerOrganizations
            .Include(o => o.Customer)
            .Include(o => o.Parent)
            .Include(o => o.Personnel)
            .Include(o => o.Children)
            .FirstOrDefaultAsync(o => o.Id == id);

        return org == null ? null : MapToDto(org);
    }

    public async Task<IEnumerable<CustomerOrganizationDto>> GetByCustomerIdAsync(int customerId, bool includeInactive = false)
    {
        var query = _context.CustomerOrganizations
            .Include(o => o.Customer)
            .Include(o => o.Parent)
            .Include(o => o.Personnel)
            .Include(o => o.Children)
            .Where(o => o.CustomerId == customerId);

        if (!includeInactive)
        {
            query = query.Where(o => o.IsActive);
        }

        var orgs = await query.OrderBy(o => o.Order).ThenBy(o => o.Name).ToListAsync();
        return orgs.Select(MapToDto);
    }

    public async Task<OrganizationTreeDto?> GetOrganizationTreeAsync(int customerId)
    {
        var orgs = await _context.CustomerOrganizations
            .Include(o => o.Personnel)
            .Where(o => o.CustomerId == customerId && o.IsActive)
            .OrderBy(o => o.Order)
            .ThenBy(o => o.Name)
            .ToListAsync();

        if (!orgs.Any()) return null;

        // Root organizasyonları bul (ParentId null olanlar)
        var rootOrgs = orgs.Where(o => o.ParentId == null).ToList();

        // Recursive tree builder
        var tree = new OrganizationTreeDto
        {
            Id = 0,
            Name = "Organizasyonlar",
            Level = -1,
            IsActive = true,
            Children = rootOrgs.Select(o => BuildTreeNode(o, orgs)).ToList()
        };

        return tree;
    }

    private OrganizationTreeDto BuildTreeNode(CustomerOrganization org, List<CustomerOrganization> allOrgs)
    {
        var children = allOrgs.Where(o => o.ParentId == org.Id).ToList();

        return new OrganizationTreeDto
        {
            Id = org.Id,
            Name = org.Name,
            Code = org.Code,
            Level = org.Level,
            IsActive = org.IsActive,
            PersonnelCount = org.Personnel?.Count ?? 0,
            Children = children.Select(c => BuildTreeNode(c, allOrgs)).ToList()
        };
    }

    public async Task<CustomerOrganizationDto> CreateAsync(CreateCustomerOrganizationDto dto)
    {
        // Validate customer exists
        var customer = await _context.Customers.FindAsync(dto.CustomerId);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Müşteri bulunamadı (ID: {dto.CustomerId})");
        }

        // Validate parent if provided
        int level = 0;
        if (dto.ParentId.HasValue)
        {
            var parent = await _context.CustomerOrganizations.FindAsync(dto.ParentId.Value);
            if (parent == null)
            {
                throw new KeyNotFoundException($"Üst organizasyon bulunamadı (ID: {dto.ParentId})");
            }
            if (parent.CustomerId != dto.CustomerId)
            {
                throw new InvalidOperationException("Üst organizasyon farklı bir müşteriye ait");
            }
            level = parent.Level + 1;
        }

        var org = new CustomerOrganization
        {
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            CustomerId = dto.CustomerId,
            ParentId = dto.ParentId,
            Level = level,
            Order = dto.Order,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.CustomerOrganizations.Add(org);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(org.Id) ?? throw new InvalidOperationException("Organizasyon oluşturulamadı");
    }

    public async Task<CustomerOrganizationDto> UpdateAsync(int id, UpdateCustomerOrganizationDto dto)
    {
        var org = await _context.CustomerOrganizations.FindAsync(id);
        if (org == null)
        {
            throw new KeyNotFoundException($"Organizasyon bulunamadı (ID: {id})");
        }

        // Validate parent if provided
        if (dto.ParentId.HasValue)
        {
            if (dto.ParentId.Value == id)
            {
                throw new InvalidOperationException("Organizasyon kendisinin üstü olamaz");
            }

            var parent = await _context.CustomerOrganizations.FindAsync(dto.ParentId.Value);
            if (parent == null)
            {
                throw new KeyNotFoundException($"Üst organizasyon bulunamadı (ID: {dto.ParentId})");
            }
            if (parent.CustomerId != org.CustomerId)
            {
                throw new InvalidOperationException("Üst organizasyon farklı bir müşteriye ait");
            }
            org.Level = parent.Level + 1;
        }
        else
        {
            org.Level = 0;
        }

        org.Name = dto.Name;
        org.Code = dto.Code;
        org.Description = dto.Description;
        org.ParentId = dto.ParentId;
        org.Order = dto.Order;
        org.IsActive = dto.IsActive;
        org.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(org.Id) ?? throw new InvalidOperationException("Organizasyon güncellenemedi");
    }

    public async Task DeleteAsync(int id)
    {
        var org = await _context.CustomerOrganizations
            .Include(o => o.Children)
            .Include(o => o.Personnel)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (org == null)
        {
            throw new KeyNotFoundException($"Organizasyon bulunamadı (ID: {id})");
        }

        if (org.Children.Any())
        {
            throw new InvalidOperationException("Alt organizasyonları olan bir organizasyon silinemez. Önce alt organizasyonları silin.");
        }

        if (org.Personnel.Any())
        {
            throw new InvalidOperationException("Personeli olan bir organizasyon silinemez. Önce personelleri başka organizasyona taşıyın.");
        }

        _context.CustomerOrganizations.Remove(org);
        await _context.SaveChangesAsync();
    }

    public async Task MoveOrganizationAsync(int organizationId, int? newParentId)
    {
        var org = await _context.CustomerOrganizations.FindAsync(organizationId);
        if (org == null)
        {
            throw new KeyNotFoundException($"Organizasyon bulunamadı (ID: {organizationId})");
        }

        // Kendisine taşınamaz
        if (newParentId.HasValue && newParentId.Value == organizationId)
        {
            throw new InvalidOperationException("Organizasyon kendisinin altına taşınamaz");
        }

        // Yeni parent kontrolü
        int newLevel = 0;
        if (newParentId.HasValue)
        {
            var newParent = await _context.CustomerOrganizations.FindAsync(newParentId.Value);
            if (newParent == null)
            {
                throw new KeyNotFoundException($"Hedef organizasyon bulunamadı (ID: {newParentId})");
            }
            if (newParent.CustomerId != org.CustomerId)
            {
                throw new InvalidOperationException("Farklı müşteriye ait organizasyona taşınamaz");
            }

            // Kendi alt organizasyonlarına taşınamaz (döngüsel referans)
            if (await IsDescendantOfAsync(newParentId.Value, organizationId))
            {
                throw new InvalidOperationException("Organizasyon kendi alt organizasyonlarına taşınamaz");
            }

            newLevel = newParent.Level + 1;
        }

        // Parent ve level güncelle
        org.ParentId = newParentId;
        org.Level = newLevel;
        org.UpdatedAt = DateTime.UtcNow;

        // Alt organizasyonların level'larını güncelle
        await UpdateChildrenLevelsAsync(organizationId, newLevel + 1);

        await _context.SaveChangesAsync();
    }

    private async Task<bool> IsDescendantOfAsync(int potentialParentId, int ancestorId)
    {
        var current = await _context.CustomerOrganizations.FindAsync(potentialParentId);
        while (current != null && current.ParentId.HasValue)
        {
            if (current.ParentId.Value == ancestorId)
            {
                return true;
            }
            current = await _context.CustomerOrganizations.FindAsync(current.ParentId.Value);
        }
        return false;
    }

    private async Task UpdateChildrenLevelsAsync(int parentId, int level)
    {
        var children = await _context.CustomerOrganizations
            .Where(o => o.ParentId == parentId)
            .ToListAsync();

        foreach (var child in children)
        {
            child.Level = level;
            child.UpdatedAt = DateTime.UtcNow;
            await UpdateChildrenLevelsAsync(child.Id, level + 1);
        }
    }

    public async Task<OrganizationPersonnelListDto> GetPersonnelByOrganizationIdAsync(int organizationId)
    {
        var org = await _context.CustomerOrganizations
            .Include(o => o.Personnel)
                .ThenInclude(p => p.Supervisor)
            .Include(o => o.Personnel)
                .ThenInclude(p => p.TeamMembers)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org == null)
        {
            throw new KeyNotFoundException($"Organizasyon bulunamadı (ID: {organizationId})");
        }

        var personnel = org.Personnel.ToList();

        // Süpervizörler (Manager ve Supervisor rolleri)
        var supervisors = personnel
            .Where(p => p.Role == CustomerPersonnelRole.CustomerManager || p.Role == CustomerPersonnelRole.CustomerSupervisor)
            .Select(p => MapToPersonnelItem(p, personnel))
            .ToList();

        // Bağımsız operatörler (süpervizörü olmayan VEYA süpervizörü farklı organizasyonda)
        var independentOperators = personnel
            .Where(p => p.Role == CustomerPersonnelRole.CustomerOperator &&
                       (p.SupervisorId == null || !personnel.Any(s => s.Id == p.SupervisorId)))
            .Select(p => MapToPersonnelItem(p, personnel))
            .ToList();

        return new OrganizationPersonnelListDto
        {
            OrganizationId = org.Id,
            OrganizationName = org.Name,
            Supervisors = supervisors,
            Operators = independentOperators
        };
    }

    public async Task<IEnumerable<OrganizationPersonnelSummaryDto>> GetPersonnelPoolAsync(int customerId)
    {
        var personnel = await _context.CustomerPersonnel
            .Include(p => p.Supervisor)
            .Include(p => p.Organization)
            .Include(p => p.ManagedOrganizations)
            .Where(p => p.CustomerId == customerId && p.IsActive)
            .OrderBy(p => p.Role)
            .ThenBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .ToListAsync();

        return personnel.Select(p => new OrganizationPersonnelSummaryDto
        {
            Id = p.Id,
            FullName = $"{p.FirstName} {p.LastName}",
            Username = p.Username,
            Email = p.Email,
            RoleName = GetRoleName(p.Role),
            Role = (int)p.Role,
            IsActive = p.IsActive,
            SupervisorId = p.SupervisorId,
            SupervisorName = p.Supervisor != null ? $"{p.Supervisor.FirstName} {p.Supervisor.LastName}" : null,
            OrganizationId = p.OrganizationId,
            OrganizationName = p.Organization?.Name,
            ManagedOrganizationIds = p.ManagedOrganizations?.Select(m => m.CustomerOrganizationId).ToList() ?? new List<int>()
        });
    }

    public async Task AssignPersonnelToOrganizationAsync(AssignPersonnelToOrganizationDto dto)
    {
        var personnel = await _context.CustomerPersonnel.FindAsync(dto.PersonnelId);
        if (personnel == null)
        {
            throw new KeyNotFoundException($"Personel bulunamadı (ID: {dto.PersonnelId})");
        }

        var org = await _context.CustomerOrganizations.FindAsync(dto.OrganizationId);
        if (org == null)
        {
            throw new KeyNotFoundException($"Organizasyon bulunamadı (ID: {dto.OrganizationId})");
        }

        if (personnel.CustomerId != org.CustomerId)
        {
            throw new InvalidOperationException("Personel ve organizasyon farklı müşterilere ait");
        }

        // Süpervizör kontrolü
        if (dto.SupervisorId.HasValue)
        {
            var supervisor = await _context.CustomerPersonnel.FindAsync(dto.SupervisorId.Value);
            if (supervisor == null)
            {
                throw new KeyNotFoundException($"Süpervizör bulunamadı (ID: {dto.SupervisorId})");
            }
            if (supervisor.OrganizationId != dto.OrganizationId)
            {
                throw new InvalidOperationException("Süpervizör bu organizasyona ait değil");
            }
            personnel.SupervisorId = dto.SupervisorId;
        }

        personnel.OrganizationId = dto.OrganizationId;
        personnel.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task RemovePersonnelFromOrganizationAsync(int personnelId, int organizationId)
    {
        var personnel = await _context.CustomerPersonnel.FindAsync(personnelId);
        if (personnel == null)
        {
            throw new KeyNotFoundException($"Personel bulunamadı (ID: {personnelId})");
        }

        if (personnel.OrganizationId != organizationId)
        {
            throw new InvalidOperationException("Personel bu organizasyona ait değil");
        }

        personnel.OrganizationId = null;
        personnel.SupervisorId = null;
        personnel.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task SetSupervisorAsync(int personnelId, int? supervisorId)
    {
        var personnel = await _context.CustomerPersonnel.FindAsync(personnelId);
        if (personnel == null)
        {
            throw new KeyNotFoundException($"Personel bulunamadı (ID: {personnelId})");
        }

        if (supervisorId.HasValue)
        {
            var supervisor = await _context.CustomerPersonnel.FindAsync(supervisorId.Value);
            if (supervisor == null)
            {
                throw new KeyNotFoundException($"Süpervizör bulunamadı (ID: {supervisorId})");
            }
            if (supervisor.CustomerId != personnel.CustomerId)
            {
                throw new InvalidOperationException("Süpervizör farklı bir müşteriye ait");
            }
        }

        personnel.SupervisorId = supervisorId;
        personnel.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task TransferTeamAndRemoveAsync(int organizationId, int personnelIdToRemove, int newSupervisorId)
    {
        // Validate personnel to remove
        var personnelToRemove = await _context.CustomerPersonnel
            .Include(p => p.TeamMembers)
            .FirstOrDefaultAsync(p => p.Id == personnelIdToRemove);

        if (personnelToRemove == null)
        {
            throw new KeyNotFoundException($"Personel bulunamadı (ID: {personnelIdToRemove})");
        }

        if (personnelToRemove.OrganizationId != organizationId)
        {
            throw new InvalidOperationException("Personel bu organizasyona ait değil");
        }

        // Validate new supervisor
        var newSupervisor = await _context.CustomerPersonnel.FindAsync(newSupervisorId);
        if (newSupervisor == null)
        {
            throw new KeyNotFoundException($"Yeni süpervizör bulunamadı (ID: {newSupervisorId})");
        }

        if (newSupervisor.OrganizationId != organizationId)
        {
            throw new InvalidOperationException("Yeni süpervizör bu organizasyona ait değil");
        }

        if (newSupervisor.Id == personnelIdToRemove)
        {
            throw new InvalidOperationException("Personel kendi kendine devredilemez");
        }

        // Transfer team members to new supervisor
        var teamMembers = personnelToRemove.TeamMembers?.ToList() ?? new List<CustomerPersonnel>();
        foreach (var member in teamMembers)
        {
            member.SupervisorId = newSupervisorId;
            member.UpdatedAt = DateTime.UtcNow;
        }

        // Remove personnel from organization
        personnelToRemove.OrganizationId = null;
        personnelToRemove.SupervisorId = null;
        personnelToRemove.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private CustomerOrganizationDto MapToDto(CustomerOrganization org)
    {
        return new CustomerOrganizationDto
        {
            Id = org.Id,
            Name = org.Name,
            Code = org.Code,
            Description = org.Description,
            Level = org.Level,
            Order = org.Order,
            IsActive = org.IsActive,
            CustomerId = org.CustomerId,
            CustomerName = org.Customer?.CompanyName ?? string.Empty,
            ParentId = org.ParentId,
            ParentName = org.Parent?.Name,
            PersonnelCount = org.Personnel?.Count ?? 0,
            ChildrenCount = org.Children?.Count ?? 0,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        };
    }

    private OrganizationPersonnelItemDto MapToPersonnelItem(CustomerPersonnel p, List<CustomerPersonnel> allPersonnel)
    {
        // Bu süpervizöre bağlı operatörleri bul
        var teamMembers = allPersonnel
            .Where(op => op.SupervisorId == p.Id)
            .Select(op => new OrganizationPersonnelItemDto
            {
                Id = op.Id,
                FullName = $"{op.FirstName} {op.LastName}",
                Username = op.Username,
                Email = op.Email,
                RoleName = GetRoleName(op.Role),
                Role = (int)op.Role,
                IsActive = op.IsActive,
                SupervisorId = op.SupervisorId,
                SupervisorName = $"{p.FirstName} {p.LastName}",
                TeamMembers = new List<OrganizationPersonnelItemDto>()
            })
            .ToList();

        return new OrganizationPersonnelItemDto
        {
            Id = p.Id,
            FullName = $"{p.FirstName} {p.LastName}",
            Username = p.Username,
            Email = p.Email,
            RoleName = GetRoleName(p.Role),
            Role = (int)p.Role,
            IsActive = p.IsActive,
            SupervisorId = p.SupervisorId,
            SupervisorName = p.Supervisor != null ? $"{p.Supervisor.FirstName} {p.Supervisor.LastName}" : null,
            TeamMembers = teamMembers
        };
    }

    private static string GetRoleName(CustomerPersonnelRole role)
    {
        return role switch
        {
            CustomerPersonnelRole.CustomerManager => "Müşteri Yöneticisi",
            CustomerPersonnelRole.CustomerSupervisor => "Müşteri Süpervizörü",
            CustomerPersonnelRole.CustomerOperator => "Müşteri Operatörü",
            _ => role.ToString()
        };
    }
}
