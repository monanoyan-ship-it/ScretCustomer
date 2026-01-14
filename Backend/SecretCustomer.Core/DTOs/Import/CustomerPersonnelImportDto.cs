using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.Import;

/// <summary>
/// CSV'den okunan müşteri personeli verisi (CustomerPersonnel import)
/// </summary>
public class CustomerPersonnelImportDto
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Organization { get; set; } = "Merkez";
}

/// <summary>
/// Import işlemi sonucu
/// </summary>
public class ImportResultDto
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int CustomersCreated { get; set; }
    public int CustomersExisted { get; set; }
    public int PersonnelCreated { get; set; }
    public int PersonnelUpdated { get; set; }
    public int PersonnelSkipped { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<ImportedCustomerPersonnelInfo> ImportedPersonnel { get; set; } = new();

    /// <summary>
    /// Birden fazla eşleşme bulunan firma adları.
    /// Key: CSV'deki firma adı, Value: Eşleşen firmalar
    /// </summary>
    public Dictionary<string, List<CompanyMatchInfo>> AmbiguousCompanyMatches { get; set; } = new();
}

/// <summary>
/// Eşleşen firma bilgisi
/// </summary>
public class CompanyMatchInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Import edilen müşteri personeli bilgisi
/// </summary>
public class ImportedCustomerPersonnelInfo
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Created, Updated, Skipped
}
