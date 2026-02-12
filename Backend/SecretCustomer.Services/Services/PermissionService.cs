using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<PermissionService> _logger;
    private readonly IAuditLogService _auditLogService;

    public PermissionService(
        ApplicationDbContext context,
        IUserRepository userRepository,
        ILogger<PermissionService> logger,
        IAuditLogService auditLogService)
    {
        _context = context;
        _userRepository = userRepository;
        _logger = logger;
        _auditLogService = auditLogService;
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
                _logger.LogWarning("Permission {PermissionCode} not found", permissionCode);
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
            _logger.LogError(ex, "Error checking permission {PermissionCode} for user {UserId}", permissionCode, userId);
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
            _logger.LogError(ex, "Error checking permission {PermissionCode} with scope for user {UserId}", permissionCode, userId);
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
}
