using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Customer;

/// <summary>
/// Admin tarafından müşteri personeli şifre değiştirme DTO'su
/// CurrentPassword sadece kullanıcının kendi şifresini değiştirirken gerekli
/// </summary>
public class CustomerPersonnelChangePasswordDto
{
    /// <summary>
    /// Mevcut şifre - sadece kullanıcı kendi şifresini değiştirirken gerekli
    /// Admin değiştirirken gönderilmez
    /// </summary>
    public string? CurrentPassword { get; set; }

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [MaxLength(100, ErrorMessage = "Şifre en fazla 100 karakter olabilir.")]
    public string NewPassword { get; set; } = string.Empty;
}
