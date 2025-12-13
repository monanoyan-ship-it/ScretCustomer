using Microsoft.AspNetCore.Authorization;

namespace SecretCustomer.API.Authorization;

/// <summary>
/// Permission requirement for policy-based authorization
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionCode { get; }

    public PermissionRequirement(string permissionCode)
    {
        PermissionCode = permissionCode;
    }
}
