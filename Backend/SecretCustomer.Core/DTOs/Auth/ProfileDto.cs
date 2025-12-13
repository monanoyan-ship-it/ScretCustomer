using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Auth;

/// <summary>
/// Kullanıcı profil bilgileri DTO
/// </summary>
public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Profil güncelleme DTO
/// </summary>
public class UpdateProfileDto
{
    [Required(ErrorMessage = "Ad alanı zorunludur")]
    [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad alanı zorunludur")]
    [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir")]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Şifre değiştirme DTO
/// </summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "Mevcut şifre alanı zorunludur")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre alanı zorunludur")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    [StringLength(100, ErrorMessage = "Şifre en fazla 100 karakter olabilir")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrar alanı zorunludur")]
    [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Şifre değiştirme sonucu
/// </summary>
public class ChangePasswordResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
