using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.CustomerOrganization;

/// <summary>
/// Müşteri organizasyonu DTO
/// </summary>
public class CustomerOrganizationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int Level { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int PersonnelCount { get; set; }
    public int ChildrenCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Hiyerarşi için alt organizasyonlar
    public List<CustomerOrganizationDto> Children { get; set; } = new();
}

/// <summary>
/// Organizasyon oluşturma DTO
/// </summary>
public class CreateCustomerOrganizationDto
{
    [Required(ErrorMessage = "Organizasyon adı zorunludur")]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Code { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Müşteri seçimi zorunludur")]
    public int CustomerId { get; set; }

    public int? ParentId { get; set; }

    public int Order { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Organizasyon güncelleme DTO
/// </summary>
public class UpdateCustomerOrganizationDto
{
    [Required(ErrorMessage = "Organizasyon adı zorunludur")]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Code { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public int? ParentId { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>
/// Organizasyona personel atama DTO
/// </summary>
public class AssignPersonnelToOrganizationDto
{
    [Required(ErrorMessage = "Personel seçimi zorunludur")]
    public int PersonnelId { get; set; }

    [Required(ErrorMessage = "Organizasyon seçimi zorunludur")]
    public int OrganizationId { get; set; }

    /// <summary>
    /// Süpervizör ID (varsa bu personelin bağlı olacağı süpervizör)
    /// </summary>
    public int? SupervisorId { get; set; }
}

/// <summary>
/// Organizasyon personeli özet DTO (havuz için)
/// </summary>
public class OrganizationPersonnelSummaryDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int Role { get; set; }
    public bool IsActive { get; set; }
    public int? SupervisorId { get; set; }
    public string? SupervisorName { get; set; }
    public int? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }

    // Bu personelin yönettiği organizasyonlar (many-to-many)
    public List<int> ManagedOrganizationIds { get; set; } = new();
}

/// <summary>
/// Organizasyon hiyerarşi görünümü için DTO
/// </summary>
public class OrganizationTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; }
    public int PersonnelCount { get; set; }
    public List<OrganizationTreeDto> Children { get; set; } = new();
}

/// <summary>
/// Organizasyondaki personel listesi için DTO
/// </summary>
public class OrganizationPersonnelListDto
{
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public List<OrganizationPersonnelItemDto> Supervisors { get; set; } = new();
    public List<OrganizationPersonnelItemDto> Operators { get; set; } = new();
}

/// <summary>
/// Organizasyondaki personel item DTO
/// </summary>
public class OrganizationPersonnelItemDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int Role { get; set; }
    public bool IsActive { get; set; }
    public int? SupervisorId { get; set; }
    public string? SupervisorName { get; set; }

    // Bu süpervizöre bağlı operatörler
    public List<OrganizationPersonnelItemDto> TeamMembers { get; set; } = new();
}
