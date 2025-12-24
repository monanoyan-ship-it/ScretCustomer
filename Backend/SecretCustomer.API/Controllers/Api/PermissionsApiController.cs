using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Permission;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

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
            var permissions = await _context.Permissions
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.SortOrder)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    DisplayName = p.DisplayName,
                    Category = p.Category,
                    CategoryName = GetCategoryName(p.Category),
                    Description = p.Description,
                    IsActive = p.IsActive,
                    SortOrder = p.SortOrder
                })
                .ToListAsync();

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
    public async Task<IActionResult> GetByCategory(PermissionCategory category)
    {
        try
        {
            var permissions = await _context.Permissions
                .Where(p => p.Category == category && !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.SortOrder)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    DisplayName = p.DisplayName,
                    Category = p.Category,
                    CategoryName = GetCategoryName(p.Category),
                    Description = p.Description,
                    IsActive = p.IsActive,
                    SortOrder = p.SortOrder
                })
                .ToListAsync();

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permissions by category {Category}", category);
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

            foreach (UserRole role in Enum.GetValues<UserRole>())
            {
                var rolePermissions = await _context.RolePermissions
                    .Where(rp => rp.Role == role && rp.IsGranted && !rp.IsDeleted)
                    .Include(rp => rp.Permission)
                    .Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Code = rp.Permission.Code,
                        DisplayName = rp.Permission.DisplayName,
                        Category = rp.Permission.Category,
                        CategoryName = GetCategoryName(rp.Permission.Category),
                        Description = rp.Permission.Description,
                        IsActive = rp.Permission.IsActive,
                        SortOrder = rp.Permission.SortOrder
                    })
                    .ToListAsync();

                result.Add(new RolePermissionsSummaryDto
                {
                    Role = role,
                    RoleName = GetRoleName(role),
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
    [HttpGet("roles/{role}")]
    public async Task<IActionResult> GetRolePermissions(UserRole role)
    {
        try
        {
            var rolePermissions = await _context.RolePermissions
                .Where(rp => rp.Role == role && !rp.IsDeleted)
                .Include(rp => rp.Permission)
                .Select(rp => new RolePermissionDto
                {
                    Id = rp.Id,
                    Role = rp.Role,
                    RoleName = GetRoleName(rp.Role),
                    PermissionId = rp.PermissionId,
                    PermissionCode = rp.Permission.Code,
                    PermissionDisplayName = rp.Permission.DisplayName,
                    IsGranted = rp.IsGranted,
                    Scope = rp.Scope,
                    ScopeName = GetScopeName(rp.Scope),
                    Notes = rp.Notes
                })
                .ToListAsync();

            return Ok(rolePermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permissions for role {Role}", role);
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
                .FirstOrDefaultAsync(rp => rp.Role == dto.Role && rp.PermissionId == dto.PermissionId);

            if (existing != null)
            {
                existing.IsGranted = dto.IsGranted;
                existing.Scope = dto.Scope;
                existing.Notes = dto.Notes;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                await _context.RolePermissions.AddAsync(new Core.Entities.RolePermission
                {
                    Role = dto.Role,
                    PermissionId = dto.PermissionId,
                    IsGranted = dto.IsGranted,
                    Scope = dto.Scope,
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
                .Where(rp => rp.Role == dto.Role)
                .ToListAsync();

            _context.RolePermissions.RemoveRange(existingPermissions);

            // Yeni yetkileri ekle
            foreach (var permissionId in dto.PermissionIds)
            {
                await _context.RolePermissions.AddAsync(new Core.Entities.RolePermission
                {
                    Role = dto.Role,
                    PermissionId = permissionId,
                    IsGranted = true,
                    Scope = dto.Scope
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
    [HttpDelete("roles/{role}/{permissionId}")]
    public async Task<IActionResult> RevokeRolePermission(UserRole role, Guid permissionId)
    {
        try
        {
            await _permissionService.RevokeRolePermissionAsync(role, permissionId);
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
    public async Task<IActionResult> GetUserPermissions(Guid userId)
    {
        try
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.NotFound")));

            // Rol yetkileri
            var rolePermissions = await _context.RolePermissions
                .Where(rp => rp.Role == user.Role && rp.IsGranted && !rp.IsDeleted)
                .Include(rp => rp.Permission)
                .Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Code = rp.Permission.Code,
                    DisplayName = rp.Permission.DisplayName,
                    Category = rp.Permission.Category,
                    CategoryName = GetCategoryName(rp.Permission.Category),
                    Description = rp.Permission.Description,
                    IsActive = rp.Permission.IsActive,
                    SortOrder = rp.Permission.SortOrder
                })
                .ToListAsync();

            // Kullanıcı özel yetkileri
            var customPermissions = await _context.UserPermissions
                .Where(up => up.UserId == userId && !up.IsDeleted)
                .Include(up => up.Permission)
                .Select(up => new UserPermissionDto
                {
                    Id = up.Id,
                    UserId = up.UserId,
                    UserFullName = user.FirstName + " " + user.LastName,
                    PermissionId = up.PermissionId,
                    PermissionCode = up.Permission.Code,
                    PermissionDisplayName = up.Permission.DisplayName,
                    IsGranted = up.IsGranted,
                    Scope = up.Scope,
                    ScopeName = GetScopeName(up.Scope),
                    ValidFrom = up.ValidFrom,
                    ValidUntil = up.ValidUntil,
                    Notes = up.Notes
                })
                .ToListAsync();

            // Efektif yetkiler (rol + özel)
            var effectivePermissions = await _permissionService.GetUserPermissionsAsync(userId);
            var effectivePermissionDtos = effectivePermissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                DisplayName = p.DisplayName,
                Category = p.Category,
                CategoryName = GetCategoryName(p.Category),
                Description = p.Description,
                IsActive = p.IsActive,
                SortOrder = p.SortOrder
            }).ToList();

            return Ok(new UserPermissionsSummaryDto
            {
                UserId = userId,
                UserFullName = user.FirstName + " " + user.LastName,
                Role = user.Role,
                RoleName = GetRoleName(user.Role),
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
                existing.Scope = dto.Scope;
                existing.ValidFrom = dto.ValidFrom;
                existing.ValidUntil = dto.ValidUntil;
                existing.Notes = dto.Notes;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                await _context.UserPermissions.AddAsync(new Core.Entities.UserPermission
                {
                    UserId = dto.UserId,
                    PermissionId = dto.PermissionId,
                    IsGranted = dto.IsGranted,
                    Scope = dto.Scope,
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
    public async Task<IActionResult> RevokeUserPermission(Guid userId, Guid permissionId)
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
        var categories = Enum.GetValues<PermissionCategory>()
            .Select(c => new
            {
                Value = (int)c,
                Name = GetCategoryName(c)
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
        var scopes = Enum.GetValues<PermissionScope>()
            .Select(s => new
            {
                Value = (int)s,
                Name = GetScopeName(s)
            })
            .ToList();

        return Ok(scopes);
    }

    /// <summary>
    /// Rolleri getirir
    /// </summary>
    [HttpGet("roles-list")]
    public IActionResult GetRoles()
    {
        var roles = Enum.GetValues<UserRole>()
            .Select(r => new
            {
                Value = (int)r,
                Name = GetRoleName(r)
            })
            .ToList();

        return Ok(roles);
    }

    private static string GetCategoryName(PermissionCategory category) => category switch
    {
        PermissionCategory.Users => "Kullanıcılar",
        PermissionCategory.Roles => "Roller",
        PermissionCategory.Projects => "Projeler",
        PermissionCategory.Assignments => "Atamalar",
        PermissionCategory.Checklists => "Kontrol Listeleri",
        PermissionCategory.Evaluations => "Değerlendirmeler",
        PermissionCategory.Branches => "Şubeler",
        PermissionCategory.FieldWorkers => "Saha Çalışanları",
        PermissionCategory.Reports => "Raporlar",
        PermissionCategory.Dashboard => "Dashboard",
        PermissionCategory.ExcelTemplates => "Excel Şablonları",
        PermissionCategory.Customers => "Müşteriler",
        PermissionCategory.CustomerPersonnel => "Müşteri Personeli",
        PermissionCategory.Settings => "Ayarlar",
        _ => category.ToString()
    };

    private static string GetScopeName(PermissionScope scope) => scope switch
    {
        PermissionScope.All => "Tümü",
        PermissionScope.Own => "Sadece Kendi",
        PermissionScope.Branch => "Şube",
        PermissionScope.Department => "Departman",
        PermissionScope.Customer => "Müşteri",
        _ => scope.ToString()
    };

    private static string GetRoleName(UserRole role) => role switch
    {
        UserRole.Admin => "Yönetici",
        UserRole.TeamLeader => "Takım Lideri",
        UserRole.Evaluator => "Değerlendirici",
        UserRole.CustomerRepresentative => "Müşteri Temsilcisi",
        UserRole.FieldWorker => "Saha Çalışanı",
        _ => role.ToString()
    };
}
