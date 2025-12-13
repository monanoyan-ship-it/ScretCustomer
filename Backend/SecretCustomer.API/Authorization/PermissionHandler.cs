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
    private readonly ILogger<PermissionHandler> _logger;

    public PermissionHandler(IPermissionService permissionService, ILogger<PermissionHandler> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Kullanıcı giriş yapmış mı?
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("User is not authenticated");
            return;
        }

        // User ID'yi al
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return;
        }

        // Kullanıcının yetkisi var mı kontrol et
        var hasPermission = await _permissionService.HasPermissionAsync(userId, requirement.PermissionCode);

        if (hasPermission)
        {
            _logger.LogInformation("User {UserId} has permission {PermissionCode}", userId, requirement.PermissionCode);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("User {UserId} does NOT have permission {PermissionCode}", userId, requirement.PermissionCode);
        }
    }
}
