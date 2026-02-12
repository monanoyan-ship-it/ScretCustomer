using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Permission;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/permissions")]
[Authorize(Roles = "Admin")]
public class PermissionsApiController : BaseApiController
{
    private readonly IPermissionService _permissionService;
    private readonly IUserService _userService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PermissionsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public PermissionsApiController(
        IPermissionService permissionService,
        IUserService userService,
        ApplicationDbContext context,
        ILogger<PermissionsApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _permissionService = permissionService;
        _userService = userService;
        _context = context;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Tüm yetkileri getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllPermissions()
    {
        try
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

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permissions");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Permission.LoadError"), ex));
        }
    }

    /// <summary>
    /// Kategoriye göre yetkileri getirir
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(int categoryId)
    {
        try
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

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permissions by category {CategoryId}", categoryId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Permission.LoadError"), ex));
        }
    }

    /// <summary>
    /// Tüm rollerin yetki özetini getirir
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRolePermissions()
    {
        try
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

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role permissions");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Permission.RoleLoadError"), ex));
        }
    }

    /// <summary>
    /// Belirli bir rolün yetkilerini getirir
    /// </summary>
    [HttpGet("roles/{roleId:int}")]
    public async Task<IActionResult> GetRolePermissions(int roleId)
    {
        try
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

            return Ok(rolePermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permissions for role {RoleId}", roleId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Permission.RoleLoadError"), ex));
        }
    }

    /// <summary>
    /// Role yetki ekler/günceller
    /// </summary>
    [HttpPost("roles")]
    public async Task<IActionResult> GrantRolePermission([FromBody] GrantRolePermissionDto dto)
    {
        try
        {
            var existing = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == dto.RoleId && rp.PermissionId == dto.PermissionId);

            if (existing != null)
            {
                existing.IsGranted = dto.IsGranted;
                existing.ScopeId = dto.ScopeId;
                existing.Notes = dto.Notes;
                existing.UpdatedAt = TurkeyTime.Now;
            }
            else
            {
                await _context.RolePermissions.AddAsync(new Core.Entities.RolePermission
                {
                    RoleId = dto.RoleId,
                    PermissionId = dto.PermissionId,
                    IsGranted = dto.IsGranted,
                    ScopeId = dto.ScopeId,
                    Notes = dto.Notes
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Permission.RolePermissionUpdateSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting role permission");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Permission.RolePermissionUpdateError"), ex));
        }
    }

    /// <summary>
    /// Toplu rol yetkisi ekler
    /// </summary>
    [HttpPost("roles/bulk")]
    public async Task<IActionResult> BulkGrantRolePermissions([FromBody] BulkRolePermissionDto dto)
    {
        try
        {
            // Önce mevcut yetkileri kaldır
            var existingPermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == dto.RoleId)
                .ToListAsync();

            _context.RolePermissions.RemoveRange(existingPermissions);

            // Yeni yetkileri ekle
            foreach (var permissionId in dto.PermissionIds)
            {
                await _context.RolePermissions.AddAsync(new Core.Entities.RolePermission
                {
                    RoleId = dto.RoleId,
                    PermissionId = permissionId,
                    IsGranted = true,
                    ScopeId = dto.ScopeId
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Permission.BulkAssignSuccess"), count = dto.PermissionIds.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk granting role permissions");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Permission.BulkAssignError"), ex));
        }
    }

    /// <summary>
    /// Rolden yetki kaldırır
    /// </summary>
    [HttpDelete("roles/{roleId:int}/{permissionId:int}")]
    public async Task<IActionResult> RevokeRolePermission(int roleId, int permissionId)
    {
        try
        {
            await _permissionService.RevokeRolePermissionAsync(roleId, permissionId);
            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Permission.RolePermissionRevokeSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking role permission");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Permission.RolePermissionRevokeError"), ex));
        }
    }

    /// <summary>
    /// Kullanıcının yetkilerini getirir
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserPermissions(int userId)
    {
        try
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.NotFound")));

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
            var effectivePermissions = await _permissionService.GetUserPermissionsAsync(userId);
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

            return Ok(new UserPermissionsSummaryDto
            {
                UserId = userId,
                UserFullName = user.FirstName + " " + user.LastName,
                RoleId = user.RoleId,
                RoleName = await GetRoleNameAsync(user.RoleId),
                RolePermissions = rolePermissions,
                CustomPermissions = customPermissions,
                EffectivePermissions = effectivePermissionDtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user permissions for {UserId}", userId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Permission.UserPermissionLoadError"), ex));
        }
    }

    /// <summary>
    /// Kullanıcıya özel yetki ekler
    /// </summary>
    [HttpPost("users")]
    public async Task<IActionResult> GrantUserPermission([FromBody] GrantUserPermissionDto dto)
    {
        try
        {
            var existing = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == dto.UserId && up.PermissionId == dto.PermissionId);

            if (existing != null)
            {
                existing.IsGranted = dto.IsGranted;
                existing.ScopeId = dto.ScopeId;
                existing.ValidFrom = dto.ValidFrom;
                existing.ValidUntil = dto.ValidUntil;
                existing.Notes = dto.Notes;
                existing.UpdatedAt = TurkeyTime.Now;
            }
            else
            {
                await _context.UserPermissions.AddAsync(new Core.Entities.UserPermission
                {
                    UserId = dto.UserId,
                    PermissionId = dto.PermissionId,
                    IsGranted = dto.IsGranted,
                    ScopeId = dto.ScopeId,
                    ValidFrom = dto.ValidFrom,
                    ValidUntil = dto.ValidUntil,
                    Notes = dto.Notes
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Permission.UserPermissionUpdateSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting user permission");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Permission.UserPermissionUpdateError"), ex));
        }
    }

    /// <summary>
    /// Kullanıcıdan özel yetki kaldırır
    /// </summary>
    [HttpDelete("users/{userId}/{permissionId}")]
    public async Task<IActionResult> RevokeUserPermission(int userId, int permissionId)
    {
        try
        {
            await _permissionService.RevokeUserPermissionAsync(userId, permissionId);
            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Permission.UserPermissionRevokeSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking user permission");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Permission.UserPermissionRevokeError"), ex));
        }
    }

    /// <summary>
    /// Yetki kategorilerini getirir
    /// </summary>
    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        var categories = PermissionCategories.All
            .Select(c => new
            {
                Value = c.Id,
                Name = c.Description ?? c.SystemName
            })
            .ToList();

        return Ok(categories);
    }

    /// <summary>
    /// Yetki kapsamlarını getirir
    /// </summary>
    [HttpGet("scopes")]
    public IActionResult GetScopes()
    {
        var scopes = PermissionScopes.AllItems
            .Select(s => new
            {
                Value = s.Id,
                Name = s.Description ?? s.SystemName
            })
            .ToList();

        return Ok(scopes);
    }

    /// <summary>
    /// Rolleri getirir
    /// </summary>
    [HttpGet("roles-list")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = new List<object>();
        foreach (var r in UserRoles.All)
        {
            roles.Add(new
            {
                Value = r.Id,
                Name = await GetRoleNameAsync(r.Id)
            });
        }

        return Ok(roles);
    }

    /// <summary>
    /// Eksik permission'ları database'e ekler (mevcut verileri silmez)
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncPermissions()
    {
        try
        {
            var addedCount = await SeedData.SyncPermissionsAsync(_context, _logger);
            return Ok(new
            {
                message = addedCount > 0
                    ? $"{addedCount} yeni yetki eklendi"
                    : "Tüm yetkiler zaten mevcut",
                addedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing permissions");
            return StatusCode(500, CreateErrorResponse("Yetkiler senkronize edilirken hata oluştu", ex));
        }
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
