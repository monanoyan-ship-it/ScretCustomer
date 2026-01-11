using SecretCustomer.Core.Enums;


namespace SecretCustomer.Core.DTOs.Permission;


/// <summary>
/// Permission DTO - Yetki detayları
/// </summary>
public class PermissionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Role Permission DTO - Rol yetkisi detayları
/// </summary>
public class RolePermissionDto
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int PermissionId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string PermissionDisplayName { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
    public int ScopeId { get; set; }
    public string ScopeName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// User Permission DTO - Kullanıcı özel yetkisi detayları
/// </summary>
public class UserPermissionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public int PermissionId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string PermissionDisplayName { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
    public int ScopeId { get; set; }
    public string ScopeName { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Grant Role Permission DTO - Role yetki verme isteği
/// </summary>
public class GrantRolePermissionDto
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public bool IsGranted { get; set; } = true;
    public int ScopeId { get; set; } = PermissionScopes.Ids.All;
    public string? Notes { get; set; }
}

/// <summary>
/// Grant User Permission DTO - Kullanıcıya yetki verme isteği
/// </summary>
public class GrantUserPermissionDto
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }
    public bool IsGranted { get; set; } = true;
    public int ScopeId { get; set; } = PermissionScopes.Ids.All;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Bulk Grant Role Permission DTO - Toplu rol yetkisi verme
/// </summary>
public class BulkRolePermissionDto
{
    public int RoleId { get; set; }
    public List<int> PermissionIds { get; set; } = new();
    public int ScopeId { get; set; } = PermissionScopes.Ids.All;
}

/// <summary>
/// Role Permissions Summary DTO - Rol yetkileri özeti
/// </summary>
public class RolePermissionsSummaryDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int TotalPermissions { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new();
}

/// <summary>
/// User Permissions Summary DTO - Kullanıcı yetkileri özeti
/// </summary>
public class UserPermissionsSummaryDto
{
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public List<PermissionDto> RolePermissions { get; set; } = new();
    public List<UserPermissionDto> CustomPermissions { get; set; } = new();
    public List<PermissionDto> EffectivePermissions { get; set; } = new();
}
