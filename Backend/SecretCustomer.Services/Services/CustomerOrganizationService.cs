using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.CustomerOrganization;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class CustomerOrganizationService : ICustomerOrganizationService
{
    private readonly ApplicationDbContext _context;
    private readonly ICustomerPersonnelOrganizationRepository _personnelOrgRepository;

    public CustomerOrganizationService(
        ApplicationDbContext context,
        ICustomerPersonnelOrganizationRepository personnelOrgRepository)
    {
        _context = context;
        _personnelOrgRepository = personnelOrgRepository;
    }

    public async Task<object> DebugCheckPersonnelAsync()
    {
        // Tüm organizasyonları ve personel sayılarını getir (junction table'dan)
        var orgs = await _context.CustomerOrganizations
            .Include(o => o.PersonnelAssignments)
            .Select(o => new
            {
                OrgId = o.Id,
                OrgName = o.Name,
                CustomerName = o.Customer != null ? o.Customer.CompanyName : "",
                PersonnelAssignmentCount = o.PersonnelAssignments.Count,
                PersonnelIds = o.PersonnelAssignments.Select(pa => pa.CustomerPersonnelId).ToList()
            })
            .ToListAsync();

        // Tüm personellerin organizasyon atamalarını getir (junction table'dan)
        var personnel = await _context.CustomerPersonnel
            .Include(p => p.OrganizationAssignments)
                .ThenInclude(oa => oa.CustomerOrganization)
            .Include(p => p.OrganizationAssignments)
                .ThenInclude(oa => oa.Supervisor)
            .Select(p => new
            {
                PersonnelId = p.Id,
                FullName = p.FirstName + " " + p.LastName,
                Role = (int)p.Role,
                RoleName = p.Role.ToString(),
                OrganizationCount = p.OrganizationAssignments.Count,
                Organizations = p.OrganizationAssignments.Select(oa => new {
                    OrgId = oa.CustomerOrganizationId,
                    OrgName = oa.CustomerOrganization != null ? oa.CustomerOrganization.Name : "NULL",
                    SupervisorId = oa.SupervisorId,
                    SupervisorName = oa.Supervisor != null ? oa.Supervisor.FirstName + " " + oa.Supervisor.LastName : "NULL"
                }).ToList()
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
                PersonnelWithOrg = personnel.Count(p => p.OrganizationCount > 0),
                PersonnelWithoutOrg = personnel.Count(p => p.OrganizationCount == 0)
            }
        };
    }

    public async Task<CustomerOrganizationDto?> GetByIdAsync(int id)
    {
        var org = await _context.CustomerOrganizations
            .Include(o => o.Customer)
            .Include(o => o.Parent)
            .Include(o => o.PersonnelAssignments) // Sadece junction table
            .Include(o => o.Children)
            .FirstOrDefaultAsync(o => o.Id == id);

        return org == null ? null : MapToDto(org);
    }

    public async Task<IEnumerable<CustomerOrganizationDto>> GetByCustomerIdAsync(int customerId, bool includeInactive = false)
    {
        var query = _context.CustomerOrganizations
            .Include(o => o.Customer)
            .Include(o => o.Parent)
            .Include(o => o.PersonnelAssignments) // Sadece junction table
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
            .Include(o => o.PersonnelAssignments) // Sadece junction table
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
            PersonnelCount = GetUniquePersonnelCount(org),
            Children = children.Select(c => BuildTreeNode(c, allOrgs)).ToList()
        };
    }

    // Sadece junction table'dan personel sayısı
    private static int GetUniquePersonnelCount(CustomerOrganization org)
    {
        return org.PersonnelAssignments?.Count ?? 0;
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
            .Include(o => o.Children.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == id);

        if (org == null)
        {
            throw new KeyNotFoundException($"Organizasyon bulunamadı (ID: {id})");
        }

        if (org.Children.Any())
        {
            throw new InvalidOperationException("Alt organizasyonları olan bir organizasyon silinemez. Önce alt organizasyonları silin.");
        }

        // Aktif personel atamalarını ayrıca kontrol et
        var activeAssignments = await _context.CustomerPersonnelOrganizations
            .Where(pa => pa.CustomerOrganizationId == id && !pa.IsDeleted)
            .CountAsync();

        if (activeAssignments > 0)
        {
            throw new InvalidOperationException($"Bu organizasyonda {activeAssignments} aktif personel var. Önce personelleri başka organizasyona taşıyın.");
        }

        org.IsDeleted = true;
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
            .Include(o => o.PersonnelAssignments) // Sadece junction table
                .ThenInclude(pa => pa.CustomerPersonnel)
            .Include(o => o.PersonnelAssignments)
                .ThenInclude(pa => pa.Supervisor)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org == null)
        {
            throw new KeyNotFoundException($"Organizasyon bulunamadı (ID: {organizationId})");
        }

        // Junction table'dan personelleri ve bu org için supervisor bilgisini al
        var assignmentsWithSupervisors = org.PersonnelAssignments?
            .Where(pa => pa.CustomerPersonnel != null && !pa.IsDeleted)
            .ToDictionary(pa => pa.CustomerPersonnelId, pa => pa.SupervisorId)
            ?? new Dictionary<int, int?>();

        var personnel = org.PersonnelAssignments?
            .Where(pa => pa.CustomerPersonnel != null && !pa.IsDeleted)
            .Select(pa => pa.CustomerPersonnel!)
            .ToList() ?? new List<CustomerPersonnel>();

        // Bu organizasyonda kimin kime bağlı olduğunu junction table'dan al
        Func<CustomerPersonnel, int?> getSupervisorInOrg = (p) =>
            assignmentsWithSupervisors.TryGetValue(p.Id, out var supId) ? supId : null;

        // Süpervizörler (Manager ve Supervisor rolleri)
        var supervisors = personnel
            .Where(p => p.Role == CustomerPersonnelRole.CustomerManager || p.Role == CustomerPersonnelRole.CustomerSupervisor)
            .Select(p => MapToPersonnelItemV2(p, personnel, assignmentsWithSupervisors))
            .OrderByDescending(s => s.TeamMembers.Count == 0) // Org yöneticisi (altında kimse yok) en üste
            .ThenBy(s => s.FullName)
            .ToList();

        // Bağımsız operatörler (bu organizasyonda süpervizörü olmayan VEYA süpervizörü bu org'da olmayan)
        var independentOperators = personnel
            .Where(p => p.Role == CustomerPersonnelRole.CustomerOperator)
            .Where(p => {
                var supIdInOrg = getSupervisorInOrg(p);
                return supIdInOrg == null || !personnel.Any(s => s.Id == supIdInOrg);
            })
            .Select(p => MapToPersonnelItemV2(p, personnel, assignmentsWithSupervisors))
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
            .Include(p => p.OrganizationAssignments) // Junction table
                .ThenInclude(oa => oa.CustomerOrganization)
            .Include(p => p.OrganizationAssignments)
                .ThenInclude(oa => oa.Supervisor)
            .Where(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Role)
            .ThenBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .ToListAsync();

        return personnel.Select(p => {
            // İlk organizasyon atamasından supervisor ve org bilgisini al
            var firstAssignment = p.OrganizationAssignments?.FirstOrDefault(oa => !oa.IsDeleted);
            return new OrganizationPersonnelSummaryDto
            {
                Id = p.Id,
                FullName = $"{p.FirstName} {p.LastName}",
                Username = p.Username,
                Email = p.Email,
                RoleName = GetRoleName(p.Role),
                Role = (int)p.Role,
                IsActive = p.IsActive,
                SupervisorId = firstAssignment?.SupervisorId,
                SupervisorName = firstAssignment?.Supervisor != null
                    ? $"{firstAssignment.Supervisor.FirstName} {firstAssignment.Supervisor.LastName}"
                    : null,
                OrganizationId = firstAssignment?.CustomerOrganizationId,
                OrganizationName = firstAssignment?.CustomerOrganization?.Name,
                OrganizationCount = p.OrganizationAssignments?.Count(oa => !oa.IsDeleted) ?? 0,
                ManagedOrganizationIds = new List<int>() // CustomerOrganizationManagers tablosu kaldırıldı
            };
        });
    }

    public async Task AssignPersonnelToOrganizationAsync(AssignPersonnelToOrganizationDto dto)
    {
        // V2 metodu kullan (junction table)
        await AddPersonnelToOrganizationAsync(dto.PersonnelId, dto.OrganizationId, dto.SupervisorId, null);
    }

    public async Task RemovePersonnelFromOrganizationAsync(int personnelId, int organizationId)
    {
        // V2 metodu kullan (junction table)
        await RemovePersonnelFromOrganizationV2Async(personnelId, organizationId);
    }

    public async Task SetSupervisorAsync(int personnelId, int? supervisorId)
    {
        // Multi-org modelde supervisor her organizasyon için ayrı belirlenir
        // Bu metod tüm organizasyon atamalarını günceller (backward compatibility için)
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

        // Tüm organizasyon atamalarını güncelle
        var assignments = await _personnelOrgRepository.GetByPersonnelIdAsync(personnelId);
        foreach (var assignment in assignments)
        {
            assignment.SupervisorId = supervisorId;
            assignment.UpdatedAt = DateTime.UtcNow;
            await _personnelOrgRepository.UpdateAsync(assignment);
        }
    }

    public async Task TransferTeamAndRemoveAsync(int organizationId, int personnelIdToRemove, int newSupervisorId)
    {
        // Validate personnel to remove - junction table'dan kontrol et
        var assignmentToRemove = await _personnelOrgRepository.GetAsync(personnelIdToRemove, organizationId);
        if (assignmentToRemove == null)
        {
            throw new KeyNotFoundException($"Personel bu organizasyona atanmamış (PersonelId: {personnelIdToRemove}, OrgId: {organizationId})");
        }

        // Validate new supervisor - junction table'da bu org'da olmalı
        var newSupervisorAssignment = await _personnelOrgRepository.GetAsync(newSupervisorId, organizationId);
        if (newSupervisorAssignment == null)
        {
            throw new InvalidOperationException("Yeni süpervizör bu organizasyona ait değil");
        }

        if (newSupervisorId == personnelIdToRemove)
        {
            throw new InvalidOperationException("Personel kendi kendine devredilemez");
        }

        // Bu organizasyonda bu personele bağlı ekip üyelerini bul
        var orgAssignments = await _personnelOrgRepository.GetByOrganizationIdAsync(organizationId);
        var teamMemberAssignments = orgAssignments
            .Where(a => a.SupervisorId == personnelIdToRemove)
            .ToList();

        // Ekip üyelerini yeni süpervizöre transfer et
        foreach (var memberAssignment in teamMemberAssignments)
        {
            memberAssignment.SupervisorId = newSupervisorId;
            memberAssignment.UpdatedAt = DateTime.UtcNow;
            await _personnelOrgRepository.UpdateAsync(memberAssignment);
        }

        // Personeli organizasyondan çıkar
        await _personnelOrgRepository.DeleteAsync(personnelIdToRemove, organizationId);
    }

    // ===== Coklu Organizasyon Destegi (Junction Table) =====

    public async Task<IEnumerable<PersonnelOrganizationAssignmentDto>> GetPersonnelOrganizationsAsync(int personnelId)
    {
        var assignments = await _personnelOrgRepository.GetByPersonnelIdAsync(personnelId);
        return assignments.Select(MapToAssignmentDto);
    }

    public async Task<PersonnelOrganizationAssignmentDto> AddPersonnelToOrganizationAsync(
        int personnelId, int organizationId, int? supervisorId = null, string? notes = null)
    {
        // Personel kontrolu
        var personnel = await _context.CustomerPersonnel.FindAsync(personnelId);
        if (personnel == null)
        {
            throw new KeyNotFoundException($"Personel bulunamadi (ID: {personnelId})");
        }

        // Organizasyon kontrolu
        var org = await _context.CustomerOrganizations.FindAsync(organizationId);
        if (org == null)
        {
            throw new KeyNotFoundException($"Organizasyon bulunamadi (ID: {organizationId})");
        }

        // Ayni musteri kontrolu
        if (personnel.CustomerId != org.CustomerId)
        {
            throw new InvalidOperationException("Personel ve organizasyon farkli musterilere ait");
        }

        // Zaten atanmis mi?
        if (await _personnelOrgRepository.ExistsAsync(personnelId, organizationId))
        {
            throw new InvalidOperationException("Personel zaten bu organizasyona atanmis");
        }

        // Supervisor kontrolu - junction table'dan kontrol et
        if (supervisorId.HasValue)
        {
            var supervisor = await _context.CustomerPersonnel.FindAsync(supervisorId.Value);
            if (supervisor == null)
            {
                throw new KeyNotFoundException($"Supervisor bulunamadi (ID: {supervisorId})");
            }
            // Supervisor'un bu organizasyonda gorev aliyor olmasi gerekir
            var supervisorInOrg = await _personnelOrgRepository.ExistsAsync(supervisorId.Value, organizationId);
            if (!supervisorInOrg)
            {
                throw new InvalidOperationException("Supervisor bu organizasyonda gorev almiyor");
            }
        }

        var assignment = new CustomerPersonnelOrganization
        {
            CustomerPersonnelId = personnelId,
            CustomerOrganizationId = organizationId,
            SupervisorId = supervisorId,
            Notes = notes,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _personnelOrgRepository.AddAsync(assignment);

        // Reload with includes
        var result = await _personnelOrgRepository.GetAsync(personnelId, organizationId);
        return MapToAssignmentDto(result!);
    }

    public async Task<PersonnelOrganizationAssignmentDto> UpdatePersonnelOrganizationAssignmentAsync(
        int personnelId, int organizationId, UpdatePersonnelOrganizationAssignmentDto dto)
    {
        var assignment = await _personnelOrgRepository.GetAsync(personnelId, organizationId);
        if (assignment == null)
        {
            throw new KeyNotFoundException($"Atama bulunamadi (PersonelId: {personnelId}, OrgId: {organizationId})");
        }

        // Supervisor kontrolu
        if (dto.SupervisorId.HasValue)
        {
            var supervisor = await _context.CustomerPersonnel.FindAsync(dto.SupervisorId.Value);
            if (supervisor == null)
            {
                throw new KeyNotFoundException($"Supervisor bulunamadi (ID: {dto.SupervisorId})");
            }
        }

        assignment.SupervisorId = dto.SupervisorId;
        assignment.Notes = dto.Notes;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _personnelOrgRepository.UpdateAsync(assignment);

        // Reload with includes
        var result = await _personnelOrgRepository.GetAsync(personnelId, organizationId);
        return MapToAssignmentDto(result!);
    }

    public async Task RemovePersonnelFromOrganizationV2Async(int personnelId, int organizationId)
    {
        var assignment = await _personnelOrgRepository.GetAsync(personnelId, organizationId);
        if (assignment == null)
        {
            throw new KeyNotFoundException($"Atama bulunamadi (PersonelId: {personnelId}, OrgId: {organizationId})");
        }

        await _personnelOrgRepository.DeleteAsync(personnelId, organizationId);
    }

    public async Task<IEnumerable<PersonnelOrganizationAssignmentDto>> GetOrganizationPersonnelAssignmentsAsync(int organizationId)
    {
        var assignments = await _personnelOrgRepository.GetByOrganizationIdAsync(organizationId);
        return assignments.Select(MapToAssignmentDto);
    }

    private PersonnelOrganizationAssignmentDto MapToAssignmentDto(CustomerPersonnelOrganization assignment)
    {
        return new PersonnelOrganizationAssignmentDto
        {
            Id = assignment.Id,
            CustomerPersonnelId = assignment.CustomerPersonnelId,
            PersonnelName = assignment.CustomerPersonnel != null
                ? $"{assignment.CustomerPersonnel.FirstName} {assignment.CustomerPersonnel.LastName}"
                : string.Empty,
            CustomerOrganizationId = assignment.CustomerOrganizationId,
            OrganizationName = assignment.CustomerOrganization?.Name ?? string.Empty,
            SupervisorId = assignment.SupervisorId,
            SupervisorName = assignment.Supervisor != null
                ? $"{assignment.Supervisor.FirstName} {assignment.Supervisor.LastName}"
                : null,
            AssignedAt = assignment.AssignedAt,
            Notes = assignment.Notes
        };
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
            PersonnelCount = GetUniquePersonnelCount(org),
            ChildrenCount = org.Children?.Count ?? 0,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        };
    }

    // V2: Junction table'dan supervisor bilgisini kullanan versiyon
    private OrganizationPersonnelItemDto MapToPersonnelItemV2(
        CustomerPersonnel p,
        List<CustomerPersonnel> allPersonnel,
        Dictionary<int, int?> supervisorMap)
    {
        // Bu organizasyonda bu süpervizöre bağlı operatörleri bul
        var teamMembers = allPersonnel
            .Where(op => supervisorMap.TryGetValue(op.Id, out var supId) && supId == p.Id)
            .Select(op => new OrganizationPersonnelItemDto
            {
                Id = op.Id,
                FirstName = op.FirstName,
                LastName = op.LastName,
                FullName = $"{op.FirstName} {op.LastName}",
                Username = op.Username,
                Email = op.Email,
                PhoneNumber = op.PhoneNumber,
                Department = op.Department,
                Title = op.Title,
                RoleName = GetRoleName(op.Role),
                Role = (int)op.Role,
                IsActive = op.IsActive,
                SupervisorId = p.Id,
                SupervisorName = $"{p.FirstName} {p.LastName}",
                TeamMembers = new List<OrganizationPersonnelItemDto>()
            })
            .ToList();

        // Bu personelin bu org'daki supervisor'ı
        supervisorMap.TryGetValue(p.Id, out var mySupervisorId);
        var mySupervisor = mySupervisorId.HasValue
            ? allPersonnel.FirstOrDefault(s => s.Id == mySupervisorId.Value)
            : null;

        return new OrganizationPersonnelItemDto
        {
            Id = p.Id,
            FirstName = p.FirstName,
            LastName = p.LastName,
            FullName = $"{p.FirstName} {p.LastName}",
            Username = p.Username,
            Email = p.Email,
            PhoneNumber = p.PhoneNumber,
            Department = p.Department,
            Title = p.Title,
            RoleName = GetRoleName(p.Role),
            Role = (int)p.Role,
            IsActive = p.IsActive,
            SupervisorId = mySupervisorId,
            SupervisorName = mySupervisor != null ? $"{mySupervisor.FirstName} {mySupervisor.LastName}" : null,
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

    // Multi-org için personel karşılaştırıcı (Union'da kullanılır)
    private class CustomerPersonnelComparer : IEqualityComparer<CustomerPersonnel>
    {
        public bool Equals(CustomerPersonnel? x, CustomerPersonnel? y)
        {
            if (x == null || y == null) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(CustomerPersonnel obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
