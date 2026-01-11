using System.ComponentModel.DataAnnotations;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.Customer;

public class CustomerPersonnelDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public int? SupervisorId { get; set; }
    public string? SupervisorName { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string Role { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public int TaskAssignmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Multi-org desteği
    public int OrganizationAssignmentCount { get; set; }
    public List<int> OrganizationIds { get; set; } = new();
}

public class CreateCustomerPersonnelDto
{
    [Required(ErrorMessage = "Müşteri seçimi zorunludur")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir. Boşluk ve Türkçe karakter kullanılamaz.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur")]
    [StringLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur")]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad zorunludur")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(20)]
    [Phone]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(100)]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Rol seçimi zorunludur")]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class UpdateCustomerPersonnelDto
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir. Boşluk ve Türkçe karakter kullanılamaz.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur")]
    [StringLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 6)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Ad zorunludur")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(20)]
    [Phone]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(100)]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Rol seçimi zorunludur")]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class ChangeOrganizationDto
{
    [Required(ErrorMessage = "Yeni organizasyon seçimi zorunludur")]
    public int NewOrganizationId { get; set; }
}
