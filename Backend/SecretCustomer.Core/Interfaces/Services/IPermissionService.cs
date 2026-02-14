using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.DTOs.Permission;


namespace SecretCustomer.Core.Interfaces.Services;

public interface IPermissionService
{
    /// <summary>
    /// Kullanıcının belirli bir yetkiye sahip olup olmadığını kontrol eder
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, string permissionCode);

    /// <summary>
    /// Kullanıcının belirli bir yetkiye sahip olup olmadığını ve scope kontrolü yapar
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, string permissionCode, int requiredScopeId);

    /// <summary>
    /// Kullanıcının tüm yetkilerini getirir (Rol + Kullanıcı özeli kombinasyonu)
    /// </summary>
    Task<IEnumerable<Permission>> GetUserPermissionsAsync(int userId);

    /// <summary>
    /// Rolün tüm yetkilerini getirir
    /// </summary>
    Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId);

    /// <summary>
    /// Role yetki ekler veya günceller
    /// </summary>
    Task GrantRolePermissionAsync(int roleId, int permissionId, int scopeId = PermissionScopes.Ids.All);

    /// <summary>
    /// Rolden yetki kaldırır
    /// </summary>
    Task RevokeRolePermissionAsync(int roleId, int permissionId);

    /// <summary>
    /// Kullanıcıya özel yetki ekler veya günceller
    /// </summary>
    Task GrantUserPermissionAsync(int userId, int permissionId, bool isGranted, int scopeId = PermissionScopes.Ids.All);

    /// <summary>
    /// Kullanıcıdan özel yetkiyi kaldırır
    /// </summary>
    Task RevokeUserPermissionAsync(int userId, int permissionId);

    /// <summary>
    /// Tüm permission'ları getirir
    /// </summary>
    Task<IEnumerable<Permission>> GetAllPermissionsAsync();

    /// <summary>
    /// Kategoriye göre permission'ları getirir
    /// </summary>
    Task<IEnumerable<Permission>> GetPermissionsByCategoryAsync(int categoryId);

    /// <summary>
    /// Tüm yetkileri DTO olarak getirir (localized category names)
    /// </summary>
    Task<List<PermissionDto>> GetAllPermissionDtosAsync();

    /// <summary>
    /// Kategoriye göre yetkileri DTO olarak getirir
    /// </summary>
    Task<List<PermissionDto>> GetPermissionDtosByCategoryAsync(int categoryId);

    /// <summary>
    /// Tüm rollerin yetki özetini getirir
    /// </summary>
    Task<List<RolePermissionsSummaryDto>> GetAllRolePermissionSummariesAsync();

    /// <summary>
    /// Belirli bir rolün yetkilerini DTO olarak getirir
    /// </summary>
    Task<List<RolePermissionDto>> GetRolePermissionDetailsAsync(int roleId);

    /// <summary>
    /// Role yetki ekler/günceller (controller DTO'dan)
    /// </summary>
    Task GrantOrUpdateRolePermissionAsync(int roleId, int permissionId, bool isGranted, int scopeId, string? notes);

    /// <summary>
    /// Toplu rol yetkisi ekler (mevcut yetkileri kaldırıp yenilerini ekler)
    /// </summary>
    Task BulkSetRolePermissionsAsync(int roleId, List<int> permissionIds, int scopeId);

    /// <summary>
    /// Kullanıcının yetki özetini getirir (rol + özel + efektif)
    /// </summary>
    Task<UserPermissionsSummaryDto> GetUserPermissionsSummaryAsync(int userId);

    /// <summary>
    /// Kullanıcıya özel yetki ekler/günceller (controller DTO'dan)
    /// </summary>
    Task GrantOrUpdateUserPermissionAsync(int userId, int permissionId, bool isGranted, int scopeId, DateTime? validFrom, DateTime? validUntil, string? notes);

    /// <summary>
    /// Eksik permission'ları database'e ekler
    /// </summary>
    Task<int> SyncPermissionsAsync();
}
