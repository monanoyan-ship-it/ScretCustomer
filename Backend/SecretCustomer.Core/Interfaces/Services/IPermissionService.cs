using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Entities;


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
}
