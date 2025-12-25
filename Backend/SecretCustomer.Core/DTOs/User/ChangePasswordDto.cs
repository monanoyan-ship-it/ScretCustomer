using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.User;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Kullanıcı ID zorunludur.")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [MaxLength(100, ErrorMessage = "Şifre en fazla 100 karakter olabilir.")]
    public string NewPassword { get; set; } = string.Empty;
}
