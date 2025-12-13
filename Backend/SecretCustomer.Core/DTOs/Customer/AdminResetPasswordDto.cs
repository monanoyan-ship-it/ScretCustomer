using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Customer;

/// <summary>
/// Admin tarafından şifre sıfırlama için kullanılan DTO
/// Eski şifre gerektirmez
/// </summary>
public class AdminResetPasswordDto
{
    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [MaxLength(100, ErrorMessage = "Şifre en fazla 100 karakter olabilir.")]
    public string NewPassword { get; set; } = string.Empty;
}
