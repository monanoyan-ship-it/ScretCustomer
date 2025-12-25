using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Customer;

public class CustomerDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string? Notes { get; set; }
    public int PersonnelCount { get; set; }
    public int BranchCount { get; set; }
    public int ProjectCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCustomerDto
{
    [Required(ErrorMessage = "Firma adı zorunludur")]
    [StringLength(255)]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vergi numarası zorunludur")]
    [StringLength(50)]
    public string TaxNumber { get; set; } = string.Empty;

    [StringLength(20)]
    [Phone]
    public string? Phone { get; set; }

    [StringLength(255)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? ContractStartDate { get; set; }

    public DateTime? ContractEndDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class UpdateCustomerDto
{
    [Required(ErrorMessage = "Firma adı zorunludur")]
    [StringLength(255)]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vergi numarası zorunludur")]
    [StringLength(50)]
    public string TaxNumber { get; set; } = string.Empty;

    [StringLength(20)]
    [Phone]
    public string? Phone { get; set; }

    [StringLength(255)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    public bool IsActive { get; set; }

    public DateTime? ContractStartDate { get; set; }

    public DateTime? ContractEndDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
