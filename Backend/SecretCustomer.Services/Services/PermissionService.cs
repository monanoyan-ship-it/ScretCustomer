using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        ApplicationDbContext context,
        IUserRepository userRepository,
        ILogger<PermissionService> logger)
    {
        _context = context;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcının belirli bir yetkiye sahip olup olmadığını kontrol eder
    /// Önce kullanıcı özel yetkilerine bakar, sonra rol yetkilerine
    /// </summary>
    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode)
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
                    (!up.ValidFrom.HasValue || up.ValidFrom <= DateTime.UtcNow) &&
                    (!up.ValidUntil.HasValue || up.ValidUntil >= DateTime.UtcNow));

            if (userPermission != null)
            {
                return userPermission.IsGranted;
            }

            // 2. Kullanıcı özel yetkisi yoksa, rol yetkilerine bak
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp =>
                    rp.Role == user.Role &&
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
    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, PermissionScope requiredScope)
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
                    (!up.ValidFrom.HasValue || up.ValidFrom <= DateTime.UtcNow) &&
                    (!up.ValidUntil.HasValue || up.ValidUntil >= DateTime.UtcNow));

            if (userPermission != null)
            {
                return userPermission.IsGranted && userPermission.Scope >= requiredScope;
            }

            // Rol yetkilerine bak
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp =>
                    rp.Role == user.Role &&
                    rp.PermissionId == permission.Id);

            return rolePermission != null && rolePermission.IsGranted && rolePermission.Scope >= requiredScope;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {PermissionCode} with scope for user {UserId}", permissionCode, userId);
            return false;
        }
    }

    public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return Enumerable.Empty<Permission>();

        // Rol yetkilerini al
        var rolePermissions = await _context.RolePermissions
            .Where(rp => rp.Role == user.Role && rp.IsGranted)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission)
            .ToListAsync();

        // Kullanıcı özel yetkilerini al
        var userPermissions = await _context.UserPermissions
            .Where(up =>
                up.UserId == userId &&
                up.IsGranted &&
                (!up.ValidFrom.HasValue || up.ValidFrom <= DateTime.UtcNow) &&
                (!up.ValidUntil.HasValue || up.ValidUntil >= DateTime.UtcNow))
            .Include(up => up.Permission)
            .Select(up => up.Permission)
            .ToListAsync();

        // Birleştir ve distinct yap
        return rolePermissions.Union(userPermissions).Distinct();
    }

    public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(UserRole role)
    {
        return await _context.RolePermissions
            .Where(rp => rp.Role == role && rp.IsGranted)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission)
            .ToListAsync();
    }

    public async Task GrantRolePermissionAsync(UserRole role, Guid permissionId, PermissionScope scope = PermissionScope.All)
    {
        var existing = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.Role == role && rp.PermissionId == permissionId);

        if (existing != null)
        {
            existing.IsGranted = true;
            existing.Scope = scope;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            await _context.RolePermissions.AddAsync(new RolePermission
            {
                Role = role,
                PermissionId = permissionId,
                IsGranted = true,
                Scope = scope
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task RevokeRolePermissionAsync(UserRole role, Guid permissionId)
    {
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.Role == role && rp.PermissionId == permissionId);

        if (rolePermission != null)
        {
            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();
        }
    }

    public async Task GrantUserPermissionAsync(Guid userId, Guid permissionId, bool isGranted, PermissionScope scope = PermissionScope.All)
    {
        var existing = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (existing != null)
        {
            existing.IsGranted = isGranted;
            existing.Scope = scope;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            await _context.UserPermissions.AddAsync(new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                IsGranted = isGranted,
                Scope = scope
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task RevokeUserPermissionAsync(Guid userId, Guid permissionId)
    {
        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (userPermission != null)
        {
            _context.UserPermissions.Remove(userPermission);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
    {
        return await _context.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Permission>> GetPermissionsByCategoryAsync(PermissionCategory category)
    {
        return await _context.Permissions
            .Where(p => p.Category == category && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }
}
