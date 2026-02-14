using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Permission;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/permissions")]
[Authorize(Roles = "Admin")]
public class PermissionsApiController : BaseApiController
{
    private readonly IPermissionService _permissionService;
    private readonly IUserService _userService;
    private readonly ILogger<PermissionsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public PermissionsApiController(
        IPermissionService permissionService,
        IUserService userService,
        ILogger<PermissionsApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _permissionService = permissionService;
        _userService = userService;
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
            var permissions = await _permissionService.GetAllPermissionDtosAsync();
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
            var permissions = await _permissionService.GetPermissionDtosByCategoryAsync(categoryId);
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
            var result = await _permissionService.GetAllRolePermissionSummariesAsync();
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
            var rolePermissions = await _permissionService.GetRolePermissionDetailsAsync(roleId);
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
            await _permissionService.GrantOrUpdateRolePermissionAsync(dto.RoleId, dto.PermissionId, dto.IsGranted, dto.ScopeId, dto.Notes);
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
            await _permissionService.BulkSetRolePermissionsAsync(dto.RoleId, dto.PermissionIds, dto.ScopeId);
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

            var summary = await _permissionService.GetUserPermissionsSummaryAsync(userId);
            return Ok(summary);
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
            await _permissionService.GrantOrUpdateUserPermissionAsync(dto.UserId, dto.PermissionId, dto.IsGranted, dto.ScopeId, dto.ValidFrom, dto.ValidUntil, dto.Notes);
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
            var addedCount = await _permissionService.SyncPermissionsAsync();
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

    private async Task<string> GetRoleNameAsync(int roleId)
    {
        var item = UserRoles.GetById(roleId);
        if (item == null) return roleId.ToString();
        return await _localizationService.GetResourceAsync(item.NameResourceKey, (int?)null, item.Description);
    }
}
