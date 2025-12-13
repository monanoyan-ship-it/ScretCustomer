using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IPermissionService
{
    /// <summary>
    /// Kullanıcının belirli bir yetkiye sahip olup olmadığını kontrol eder
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode);

    /// <summary>
    /// Kullanıcının belirli bir yetkiye sahip olup olmadığını ve scope kontrolü yapar
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, PermissionScope requiredScope);

    /// <summary>
    /// Kullanıcının tüm yetkilerini getirir (Rol + Kullanıcı özeli kombinasyonu)
    /// </summary>
    Task<IEnumerable<Permission>> GetUserPermissionsAsync(Guid userId);

    /// <summary>
    /// Rolün tüm yetkilerini getirir
    /// </summary>
    Task<IEnumerable<Permission>> GetRolePermissionsAsync(UserRole role);

    /// <summary>
    /// Role yetki ekler veya günceller
    /// </summary>
    Task GrantRolePermissionAsync(UserRole role, Guid permissionId, PermissionScope scope = PermissionScope.All);

    /// <summary>
    /// Rolden yetki kaldırır
    /// </summary>
    Task RevokeRolePermissionAsync(UserRole role, Guid permissionId);

    /// <summary>
    /// Kullanıcıya özel yetki ekler veya günceller
    /// </summary>
    Task GrantUserPermissionAsync(Guid userId, Guid permissionId, bool isGranted, PermissionScope scope = PermissionScope.All);

    /// <summary>
    /// Kullanıcıdan özel yetkiyi kaldırır
    /// </summary>
    Task RevokeUserPermissionAsync(Guid userId, Guid permissionId);

    /// <summary>
    /// Tüm permission'ları getirir
    /// </summary>
    Task<IEnumerable<Permission>> GetAllPermissionsAsync();

    /// <summary>
    /// Kategoriye göre permission'ları getirir
    /// </summary>
    Task<IEnumerable<Permission>> GetPermissionsByCategoryAsync(PermissionCategory category);
}
