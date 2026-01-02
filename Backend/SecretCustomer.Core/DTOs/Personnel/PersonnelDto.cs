using System.ComponentModel.DataAnnotations;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.Personnel;

/// <summary>
/// Personel listesi için DTO
/// </summary>
public class PersonnelDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? TcKimlikNo { get; set; }
    public string? ErpNo { get; set; }
    public string? SicilNo { get; set; }
    public string? Title { get; set; }
    public Gender Gender { get; set; }
    public string GenderText => Gender switch
    {
        Gender.Male => "Erkek",
        Gender.Female => "Kadın",
        _ => "Belirtilmemiş"
    };
    public DateTime? BirthDate { get; set; }
    public DateTime? HireDate { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    // Customer info
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }

    // Stats
    public int EvaluationCount { get; set; }
    public decimal? AverageScore { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Personel oluşturma DTO
/// </summary>
public class CreatePersonnelDto
{
    [Required(ErrorMessage = "Ad alanı zorunludur")]
    [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad alanı zorunludur")]
    [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(11, MinimumLength = 11, ErrorMessage = "TC Kimlik No 11 haneli olmalıdır")]
    public string? TcKimlikNo { get; set; }

    [StringLength(50, ErrorMessage = "ERP No en fazla 50 karakter olabilir")]
    public string? ErpNo { get; set; }

    [StringLength(50, ErrorMessage = "Sicil No en fazla 50 karakter olabilir")]
    public string? SicilNo { get; set; }

    [StringLength(100, ErrorMessage = "Unvan en fazla 100 karakter olabilir")]
    public string? Title { get; set; }

    public Gender Gender { get; set; } = Gender.Unspecified;

    public DateTime? BirthDate { get; set; }

    public DateTime? HireDate { get; set; }

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir")]
    public string? PhoneNumber { get; set; }

    [StringLength(100, ErrorMessage = "Departman en fazla 100 karakter olabilir")]
    public string? Department { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public int? CustomerId { get; set; }
}

/// <summary>
/// Personel güncelleme DTO
/// </summary>
public class UpdatePersonnelDto : CreatePersonnelDto
{
}

/// <summary>
/// Personel filtre DTO
/// </summary>
public class PersonnelFilterDto
{
    public string? SearchTerm { get; set; }
    public int? CustomerId { get; set; }
    public bool? IsActive { get; set; }
    public Gender? Gender { get; set; }
    public string? Department { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Sayfalı personel sonucu
/// </summary>
public class PagedPersonnelResult
{
    public List<PersonnelDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
