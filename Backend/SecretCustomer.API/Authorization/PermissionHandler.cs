using Microsoft.AspNetCore.Authorization;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Authorization;

/// <summary>
/// Custom authorization handler for permission-based access control
/// </summary>
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogService _auditLogService;

    public PermissionHandler(IPermissionService permissionService, IAuditLogService auditLogService)
    {
        _permissionService = permissionService;
        _auditLogService = auditLogService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Kullanıcı giriş yapmış mı?
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _auditLogService.LogWarningAsync("User is not authenticated", "Authorization");
            return;
        }

        // User ID'yi al
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            await _auditLogService.LogWarningAsync("User ID claim not found or invalid", "Authorization");
            return;
        }

        // Kullanıcının yetkisi var mı kontrol et
        var hasPermission = await _permissionService.HasPermissionAsync(userId, requirement.PermissionCode);

        if (hasPermission)
        {
            await _auditLogService.LogInfoAsync($"User {userId} has permission {requirement.PermissionCode}", "Authorization");
            context.Succeed(requirement);
        }
        else
        {
            await _auditLogService.LogWarningAsync($"User {userId} does NOT have permission {requirement.PermissionCode}", "Authorization");
        }
    }
}
