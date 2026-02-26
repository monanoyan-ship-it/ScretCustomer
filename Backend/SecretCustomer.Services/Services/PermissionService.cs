using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.DTOs.Permission;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;
    private readonly IUserService _userService;

    public PermissionService(
        ApplicationDbContext context,
        IUserRepository userRepository,
        IAuditLogService auditLogService,
        ILocalizationService localizationService,
        IUserService userService)
    {
        _context = context;
        _userRepository = userRepository;
        _auditLogService = auditLogService;
        _localizationService = localizationService;
        _userService = userService;
    }

    /// <summary>
    /// Kullanıcının belirli bir yetkiye sahip olup olmadığını kontrol eder
    /// Önce kullanıcı özel yetkilerine bakar, sonra rol yetkilerine
    /// </summary>
    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Code == permissionCode && p.IsActive);

            if (permission == null)
            {
                await _auditLogService.LogWarningAsync($"Permission {permissionCode} not found", "PermissionService");
                return false;
            }

            // 1. Önce kullanıcı özel yetkilerine bak (override eder)
            var userPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up =>
                    up.UserId == userId &&
                    up.PermissionId == permission.Id &&
                    (!up.ValidFrom.HasValue || up.ValidFrom <= TurkeyTime.Now) &&
                    (!up.ValidUntil.HasValue || up.ValidUntil >= TurkeyTime.Now));

            if (userPermission != null)
            {
                return userPermission.IsGranted;
            }

            // 2. Kullanıcı özel yetkisi yoksa, rol yetkilerine bak
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp =>
                    rp.RoleId == user.RoleId &&
                    rp.PermissionId == permission.Id);

            return rolePermission?.IsGranted ?? false;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error checking permission {permissionCode} for user {userId}", "PermissionService", ex);
            return false;
        }
    }

    /// <summary>
    /// Scope kontrolü ile yetki kontrolü
    /// </summary>
    public async Task<bool> HasPermissionAsync(int userId, string permissionCode, int requiredScopeId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Code == permissionCode && p.IsActive);

            if (permission == null)
                return false;

            // Kullanıcı özel yetkilerine bak
            var userPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up =>
                    up.UserId == userId &&
                    up.PermissionId == permission.Id &&
                    (!up.ValidFrom.HasValue || up.ValidFrom <= TurkeyTime.Now) &&
                    (!up.ValidUntil.HasValue || up.ValidUntil >= TurkeyTime.Now));

            if (userPermission != null)
            {
                return userPermission.IsGranted && userPermission.ScopeId >= requiredScopeId;
            }

            // Rol yetkilerine bak
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp =>
                    rp.RoleId == user.RoleId &&
                    rp.PermissionId == permission.Id);

            return rolePermission != null && rolePermission.IsGranted && rolePermission.ScopeId >= requiredScopeId;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error checking permission {permissionCode} with scope for user {userId}", "PermissionService", ex);
            return false;
        }
    }

    public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return Enumerable.Empty<Permission>();

        // Rol yetkilerini al
        var rolePermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == user.RoleId && rp.IsGranted)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission)
            .ToListAsync();

        // Kullanıcı özel yetkilerini al
        var userPermissions = await _context.UserPermissions
            .Where(up =>
                up.UserId == userId &&
                up.IsGranted &&
                (!up.ValidFrom.HasValue || up.ValidFrom <= TurkeyTime.Now) &&
                (!up.ValidUntil.HasValue || up.ValidUntil >= TurkeyTime.Now))
            .Include(up => up.Permission)
            .Select(up => up.Permission)
            .ToListAsync();

        // Birleştir ve distinct yap
        return rolePermissions.Union(userPermissions).Distinct();
    }

    public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId)
    {
        return await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId && rp.IsGranted)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission)
            .ToListAsync();
    }

    public async Task GrantRolePermissionAsync(int roleId, int permissionId, int scopeId = PermissionScopes.Ids.All)
    {
        var permission = await _context.Permissions.FindAsync(permissionId);
        var existing = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (existing != null)
        {
            existing.IsGranted = true;
            existing.ScopeId = scopeId;
            existing.UpdatedAt = TurkeyTime.Now;
        }
        else
        {
            await _context.RolePermissions.AddAsync(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                IsGranted = true,
                ScopeId = scopeId
            });
        }

        await _context.SaveChangesAsync();

        // Audit Log
        var roleName = UserRoles.GetById(roleId)?.SystemName ?? roleId.ToString();
        await _auditLogService.LogWarningAsync(
            $"Rol yetkisi verildi: {roleName} - {permission?.DisplayName ?? permissionId.ToString()}",
            "PermissionService");
    }

    public async Task RevokeRolePermissionAsync(int roleId, int permissionId)
    {
        var permission = await _context.Permissions.FindAsync(permissionId);
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (rolePermission != null)
        {
            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            // Audit Log
            var roleName = UserRoles.GetById(roleId)?.SystemName ?? roleId.ToString();
            await _auditLogService.LogWarningAsync(
                $"Rol yetkisi kaldırıldı: {roleName} - {permission?.DisplayName ?? permissionId.ToString()}",
                "PermissionService");
        }
    }

    public async Task GrantUserPermissionAsync(int userId, int permissionId, bool isGranted, int scopeId = PermissionScopes.Ids.All)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        var permission = await _context.Permissions.FindAsync(permissionId);
        var existing = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (existing != null)
        {
            existing.IsGranted = isGranted;
            existing.ScopeId = scopeId;
            existing.UpdatedAt = TurkeyTime.Now;
        }
        else
        {
            await _context.UserPermissions.AddAsync(new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                IsGranted = isGranted,
                ScopeId = scopeId
            });
        }

        await _context.SaveChangesAsync();

        // Audit Log
        var action = isGranted ? "verildi" : "reddedildi";
        await _auditLogService.LogWarningAsync(
            $"Kullanıcı yetkisi {action}: {user?.Username ?? userId.ToString()} - {permission?.DisplayName ?? permissionId.ToString()}",
            "PermissionService");
    }

    public async Task RevokeUserPermissionAsync(int userId, int permissionId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        var permission = await _context.Permissions.FindAsync(permissionId);
        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (userPermission != null)
        {
            _context.UserPermissions.Remove(userPermission);
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogWarningAsync(
                $"Kullanıcı yetkisi kaldırıldı: {user?.Username ?? userId.ToString()} - {permission?.DisplayName ?? permissionId.ToString()}",
                "PermissionService");
        }
    }

    public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
    {
        return await _context.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.CategoryId)
            .ThenBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Permission>> GetPermissionsByCategoryAsync(int categoryId)
    {
        return await _context.Permissions
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<List<PermissionDto>> GetAllPermissionDtosAsync()
    {
        var permissionsData = await _context.Permissions
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.CategoryId)
            .ThenBy(p => p.SortOrder)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.DisplayName,
                p.CategoryId,
                p.Description,
                p.IsActive,
                p.SortOrder
            })
            .ToListAsync();

        var permissions = new List<PermissionDto>();
        foreach (var p in permissionsData)
        {
            permissions.Add(new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                DisplayName = p.DisplayName,
                CategoryId = p.CategoryId,
                CategoryName = await GetCategoryNameAsync(p.CategoryId),
                Description = p.Description,
                IsActive = p.IsActive,
                SortOrder = p.SortOrder
            });
        }

        return permissions;
    }

    public async Task<List<PermissionDto>> GetPermissionDtosByCategoryAsync(int categoryId)
    {
        var permissionsData = await _context.Permissions
            .Where(p => p.CategoryId == categoryId && !p.IsDeleted && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.DisplayName,
                p.CategoryId,
                p.Description,
                p.IsActive,
                p.SortOrder
            })
            .ToListAsync();

        var permissions = new List<PermissionDto>();
        foreach (var p in permissionsData)
        {
            permissions.Add(new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                DisplayName = p.DisplayName,
                CategoryId = p.CategoryId,
                CategoryName = await GetCategoryNameAsync(p.CategoryId),
                Description = p.Description,
                IsActive = p.IsActive,
                SortOrder = p.SortOrder
            });
        }

        return permissions;
    }

    public async Task<List<RolePermissionsSummaryDto>> GetAllRolePermissionSummariesAsync()
    {
        var result = new List<RolePermissionsSummaryDto>();

        foreach (var role in UserRoles.All)
        {
            var rolePermissionsData = await _context.RolePermissions
                .Where(rp => rp.RoleId == role.Id && rp.IsGranted && !rp.IsDeleted)
                .Include(rp => rp.Permission)
                .Select(rp => new
                {
                    rp.Permission.Id,
                    rp.Permission.Code,
                    rp.Permission.DisplayName,
                    rp.Permission.CategoryId,
                    rp.Permission.Description,
                    rp.Permission.IsActive,
                    rp.Permission.SortOrder
                })
                .ToListAsync();

            var rolePermissions = new List<PermissionDto>();
            foreach (var rp in rolePermissionsData)
            {
                rolePermissions.Add(new PermissionDto
                {
                    Id = rp.Id,
                    Code = rp.Code,
                    DisplayName = rp.DisplayName,
                    CategoryId = rp.CategoryId,
                    CategoryName = await GetCategoryNameAsync(rp.CategoryId),
                    Description = rp.Description,
                    IsActive = rp.IsActive,
                    SortOrder = rp.SortOrder
                });
            }

            result.Add(new RolePermissionsSummaryDto
            {
                RoleId = role.Id,
                RoleName = await GetRoleNameAsync(role.Id),
                TotalPermissions = rolePermissions.Count,
                Permissions = rolePermissions
            });
        }

        return result;
    }

    public async Task<List<RolePermissionDto>> GetRolePermissionDetailsAsync(int roleId)
    {
        var rolePermissionsData = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId && !rp.IsDeleted)
            .Include(rp => rp.Permission)
            .Select(rp => new
            {
                rp.Id,
                rp.RoleId,
                rp.PermissionId,
                PermissionCode = rp.Permission.Code,
                PermissionDisplayName = rp.Permission.DisplayName,
                rp.IsGranted,
                rp.ScopeId,
                rp.Notes
            })
            .ToListAsync();

        var rolePermissions = new List<RolePermissionDto>();
        foreach (var rp in rolePermissionsData)
        {
            rolePermissions.Add(new RolePermissionDto
            {
                Id = rp.Id,
                RoleId = rp.RoleId,
                RoleName = await GetRoleNameAsync(rp.RoleId),
                PermissionId = rp.PermissionId,
                PermissionCode = rp.PermissionCode,
                PermissionDisplayName = rp.PermissionDisplayName,
                IsGranted = rp.IsGranted,
                ScopeId = rp.ScopeId,
                ScopeName = await GetScopeNameAsync(rp.ScopeId),
                Notes = rp.Notes
            });
        }

        return rolePermissions;
    }

    public async Task GrantOrUpdateRolePermissionAsync(int roleId, int permissionId, bool isGranted, int scopeId, string? notes)
    {
        var existing = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (existing != null)
        {
            existing.IsGranted = isGranted;
            existing.ScopeId = scopeId;
            existing.Notes = notes;
            existing.UpdatedAt = TurkeyTime.Now;
        }
        else
        {
            await _context.RolePermissions.AddAsync(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                IsGranted = isGranted,
                ScopeId = scopeId,
                Notes = notes
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task BulkSetRolePermissionsAsync(int roleId, List<int> permissionIds, int scopeId)
    {
        // Önce mevcut yetkileri kaldır
        var existingPermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        _context.RolePermissions.RemoveRange(existingPermissions);

        // Yeni yetkileri ekle
        foreach (var permissionId in permissionIds)
        {
            await _context.RolePermissions.AddAsync(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                IsGranted = true,
                ScopeId = scopeId
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<UserPermissionsSummaryDto> GetUserPermissionsSummaryAsync(int userId)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User {userId} not found");

        // Rol yetkileri
        var rolePermissionsData = await _context.RolePermissions
            .Where(rp => rp.RoleId == user.RoleId && rp.IsGranted && !rp.IsDeleted)
            .Include(rp => rp.Permission)
            .Select(rp => new
            {
                rp.Permission.Id,
                rp.Permission.Code,
                rp.Permission.DisplayName,
                rp.Permission.CategoryId,
                rp.Permission.Description,
                rp.Permission.IsActive,
                rp.Permission.SortOrder
            })
            .ToListAsync();

        var rolePermissions = new List<PermissionDto>();
        foreach (var rp in rolePermissionsData)
        {
            rolePermissions.Add(new PermissionDto
            {
                Id = rp.Id,
                Code = rp.Code,
                DisplayName = rp.DisplayName,
                CategoryId = rp.CategoryId,
                CategoryName = await GetCategoryNameAsync(rp.CategoryId),
                Description = rp.Description,
                IsActive = rp.IsActive,
                SortOrder = rp.SortOrder
            });
        }

        // Kullanıcı özel yetkileri
        var customPermissionsData = await _context.UserPermissions
            .Where(up => up.UserId == userId && !up.IsDeleted)
            .Include(up => up.Permission)
            .Select(up => new
            {
                up.Id,
                up.UserId,
                up.PermissionId,
                PermissionCode = up.Permission.Code,
                PermissionDisplayName = up.Permission.DisplayName,
                up.IsGranted,
                up.ScopeId,
                up.ValidFrom,
                up.ValidUntil,
                up.Notes
            })
            .ToListAsync();

        var customPermissions = new List<UserPermissionDto>();
        foreach (var up in customPermissionsData)
        {
            customPermissions.Add(new UserPermissionDto
            {
                Id = up.Id,
                UserId = up.UserId,
                UserFullName = user.FirstName + " " + user.LastName,
                PermissionId = up.PermissionId,
                PermissionCode = up.PermissionCode,
                PermissionDisplayName = up.PermissionDisplayName,
                IsGranted = up.IsGranted,
                ScopeId = up.ScopeId,
                ScopeName = await GetScopeNameAsync(up.ScopeId),
                ValidFrom = up.ValidFrom,
                ValidUntil = up.ValidUntil,
                Notes = up.Notes
            });
        }

        // Efektif yetkiler (rol + özel)
        var effectivePermissions = await GetUserPermissionsAsync(userId);
        var effectivePermissionDtos = new List<PermissionDto>();
        foreach (var p in effectivePermissions)
        {
            effectivePermissionDtos.Add(new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                DisplayName = p.DisplayName,
                CategoryId = p.CategoryId,
                CategoryName = await GetCategoryNameAsync(p.CategoryId),
                Description = p.Description,
                IsActive = p.IsActive,
                SortOrder = p.SortOrder
            });
        }

        return new UserPermissionsSummaryDto
        {
            UserId = userId,
            UserFullName = user.FirstName + " " + user.LastName,
            RoleId = user.RoleId,
            RoleName = await GetRoleNameAsync(user.RoleId),
            RolePermissions = rolePermissions,
            CustomPermissions = customPermissions,
            EffectivePermissions = effectivePermissionDtos
        };
    }

    public async Task GrantOrUpdateUserPermissionAsync(int userId, int permissionId, bool isGranted, int scopeId, DateTime? validFrom, DateTime? validUntil, string? notes)
    {
        var existing = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (existing != null)
        {
            existing.IsGranted = isGranted;
            existing.ScopeId = scopeId;
            existing.ValidFrom = validFrom;
            existing.ValidUntil = validUntil;
            existing.Notes = notes;
            existing.UpdatedAt = TurkeyTime.Now;
        }
        else
        {
            await _context.UserPermissions.AddAsync(new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                IsGranted = isGranted,
                ScopeId = scopeId,
                ValidFrom = validFrom,
                ValidUntil = validUntil,
                Notes = notes
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> SyncPermissionsAsync()
    {
        return await SeedData.SyncPermissionsAsync(_context, NullLogger.Instance);
    }

    private async Task<string> GetCategoryNameAsync(int categoryId)
    {
        var item = PermissionCategories.GetById(categoryId);
        if (item == null) return categoryId.ToString();
        return await _localizationService.GetResourceAsync(item.NameResourceKey, (int?)null, item.Description);
    }

    private async Task<string> GetScopeNameAsync(int scopeId)
    {
        var item = PermissionScopes.GetById(scopeId);
        if (item == null) return scopeId.ToString();
        return await _localizationService.GetResourceAsync(item.NameResourceKey, (int?)null, item.Description);
    }

    private async Task<string> GetRoleNameAsync(int roleId)
    {
        var item = UserRoles.GetById(roleId);
        if (item == null) return roleId.ToString();
        return await _localizationService.GetResourceAsync(item.NameResourceKey, (int?)null, item.Description);
    }
}
