namespace SecretCustomer.Core.DTOs.Import;

/// <summary>
/// CSV'den okunan bayi verisi (CustomerDealer import)
/// Firma CSV'de degil, formdan secilir (customerId parametresi).
/// </summary>
public class CustomerDealerImportDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string DealerType { get; set; } = "Retail";

    /// <summary>
    /// Bayinin baglanacagi organizasyonlar - "|" ile ayrilmis isimler
    /// </summary>
    public string Organizations { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Bayi import islemi sonucu
/// </summary>
public class DealerImportResultDto
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int DealersCreated { get; set; }
    public int DealersSkipped { get; set; }
    public int OrganizationsLinked { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<ImportedDealerInfo> ImportedDealers { get; set; } = new();
}

/// <summary>
/// Import edilen bayi bilgisi
/// </summary>
public class ImportedDealerInfo
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Organizations { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Created, Skipped
}
