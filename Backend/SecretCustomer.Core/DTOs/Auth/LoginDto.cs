using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Auth;

public class LoginDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? BranchId { get; set; }
}

public class RegisterDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public int? BranchId { get; set; }
}

// Password Reset DTOs
public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ResetToken { get; set; } // Development ortamında token döndürmek için
}

public class ResetPasswordDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ===== IMPERSONATION DTOs (Video 17 - Firma Olarak Görüntüle) =====

/// <summary>
/// Müşteri olarak görüntüleme başlatma isteği
/// </summary>
public class StartImpersonationDto
{
    public int CustomerId { get; set; }
}

/// <summary>
/// Impersonation sonucu
/// </summary>
public class ImpersonationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public ImpersonationInfoDto? ImpersonationInfo { get; set; }
}

/// <summary>
/// Impersonation bilgileri
/// </summary>
public class ImpersonationInfoDto
{
    public bool IsImpersonating { get; set; }
    public int? OriginalUserId { get; set; }
    public string? OriginalUserName { get; set; }
    public string? OriginalRole { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
}

/// <summary>
/// Müşteri listesi (impersonation seçimi için)
/// </summary>
public class CustomerListItemDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public bool IsActive { get; set; }
}
