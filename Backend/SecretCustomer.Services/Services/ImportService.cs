using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Import;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class ImportService : IImportService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerPersonnelRepository _customerPersonnelRepository;
    private readonly ApplicationDbContext _context;

    public ImportService(
        ICustomerRepository customerRepository,
        ICustomerPersonnelRepository customerPersonnelRepository,
        ApplicationDbContext context)
    {
        _customerRepository = customerRepository;
        _customerPersonnelRepository = customerPersonnelRepository;
        _context = context;
    }

    public async Task<ImportResultDto> ImportPersonnelFromCsvAsync(string csvContent, bool updateExisting = false)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        return await ImportPersonnelFromCsvAsync(stream, updateExisting);
    }

    public async Task<ImportResultDto> ImportPersonnelFromCsvAsync(Stream csvStream, bool updateExisting = false)
    {
        var result = new ImportResultDto { Success = true };
        var customerCache = new Dictionary<string, Customer>(StringComparer.OrdinalIgnoreCase);
        var organizationCache = new Dictionary<string, CustomerOrganization>(StringComparer.OrdinalIgnoreCase);

        // Takım lideri takibi - her firma için ayrı
        var currentSupervisorByCompany = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var reader = new StreamReader(csvStream, Encoding.UTF8);
            var lines = new List<string>();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }

            if (lines.Count < 2)
            {
                result.Success = false;
                result.Errors.Add("CSV dosyası boş veya sadece başlık satırı içeriyor.");
                return result;
            }

            // Parse header
            var header = ParseCsvLine(lines[0]);
            var columnMap = CreateColumnMap(header);

            if (!ValidateColumns(columnMap, result))
            {
                result.Success = false;
                return result;
            }

            result.TotalRows = lines.Count - 1;

            // Process each row
            for (int i = 1; i < lines.Count; i++)
            {
                var rowNumber = i + 1;
                try
                {
                    var values = ParseCsvLine(lines[i]);
                    var importDto = MapToDto(values, columnMap);

                    if (string.IsNullOrWhiteSpace(importDto.FullName))
                    {
                        result.Warnings.Add($"Satır {rowNumber}: FullName boş, atlandı.");
                        result.PersonnelSkipped++;
                        continue;
                    }

                    // Get or create customer
                    var customer = await GetOrCreateCustomerAsync(importDto.Company, customerCache, result);
                    if (customer == null)
                    {
                        result.Errors.Add($"Satır {rowNumber}: Firma oluşturulamadı - {importDto.Company}");
                        continue;
                    }

                    // Get or create organization
                    var organization = await GetOrCreateOrganizationAsync(
                        importDto.Organization,
                        customer.Id,
                        organizationCache);

                    // Parse name
                    var (firstName, lastName) = ParseFullName(importDto.FullName);

                    // Check if personnel exists
                    var existingPersonnel = await _customerPersonnelRepository.GetByUsernameAsync(importDto.Username);

                    if (existingPersonnel != null)
                    {
                        if (updateExisting)
                        {
                            // Update existing
                            existingPersonnel.FirstName = firstName;
                            existingPersonnel.LastName = lastName;
                            existingPersonnel.Email = importDto.Email;
                            existingPersonnel.Role = ParseRole(importDto.Role);
                            existingPersonnel.CustomerId = customer.Id;
                            existingPersonnel.OrganizationId = organization?.Id;
                            existingPersonnel.UpdatedAt = DateTime.UtcNow;

                            await _customerPersonnelRepository.UpdateAsync(existingPersonnel);
                            result.PersonnelUpdated++;

                            result.ImportedPersonnel.Add(new ImportedPersonnelInfo
                            {
                                Id = existingPersonnel.Id,
                                FullName = importDto.FullName,
                                Username = importDto.Username,
                                Company = importDto.Company,
                                Role = importDto.Role,
                                Status = "Updated"
                            });
                        }
                        else
                        {
                            result.PersonnelSkipped++;
                            result.Warnings.Add($"Satır {rowNumber}: Kullanıcı adı zaten mevcut - {importDto.Username}");

                            result.ImportedPersonnel.Add(new ImportedPersonnelInfo
                            {
                                Id = existingPersonnel.Id,
                                FullName = importDto.FullName,
                                Username = importDto.Username,
                                Company = importDto.Company,
                                Role = importDto.Role,
                                Status = "Skipped"
                            });
                        }
                    }
                    else
                    {
                        // Parse role
                        var role = ParseRole(importDto.Role);

                        // Supervisor takibi için key
                        var supervisorKey = $"{importDto.Company}_{importDto.Organization}";

                        // Eğer Supervisor ise, sonraki operatörler için kaydet
                        // Eğer Manager ise, supervisor'ı sıfırla (yeni grup başlangıcı)
                        int? supervisorId = null;
                        if (role == CustomerPersonnelRole.CustomerOperator)
                        {
                            // Mevcut supervisor'ı al
                            currentSupervisorByCompany.TryGetValue(supervisorKey, out supervisorId);
                        }

                        // Create new personnel
                        var newPersonnel = new CustomerPersonnel
                        {
                            CustomerId = customer.Id,
                            OrganizationId = organization?.Id,
                            SupervisorId = supervisorId,
                            Username = importDto.Username,
                            Email = importDto.Email,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(importDto.Password),
                            FirstName = firstName,
                            LastName = lastName,
                            Role = role,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        var created = await _customerPersonnelRepository.CreateAsync(newPersonnel);
                        result.PersonnelCreated++;

                        // Eğer bu kişi Supervisor ise, sonraki operatörler için kaydet
                        if (role == CustomerPersonnelRole.CustomerSupervisor)
                        {
                            currentSupervisorByCompany[supervisorKey] = created.Id;
                        }
                        // Manager gelirse supervisor sıfırlanır (isteğe bağlı)
                        else if (role == CustomerPersonnelRole.CustomerManager)
                        {
                            currentSupervisorByCompany[supervisorKey] = null;
                        }

                        result.ImportedPersonnel.Add(new ImportedPersonnelInfo
                        {
                            Id = created.Id,
                            FullName = importDto.FullName,
                            Username = importDto.Username,
                            Company = importDto.Company,
                            Role = importDto.Role,
                            Status = "Created"
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Satır {rowNumber}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Import hatası: {ex.Message}");
        }

        result.Success = result.Errors.Count == 0;
        return result;
    }

    private async Task<CustomerOrganization?> GetOrCreateOrganizationAsync(
        string organizationName,
        int customerId,
        Dictionary<string, CustomerOrganization> cache)
    {
        if (string.IsNullOrWhiteSpace(organizationName))
            organizationName = "Merkez";

        var cacheKey = $"{customerId}_{organizationName}";

        // Check cache first
        if (cache.TryGetValue(cacheKey, out var cachedOrg))
            return cachedOrg;

        // Check database
        var existingOrg = await _context.CustomerOrganizations
            .FirstOrDefaultAsync(o => !o.IsDeleted &&
                                      o.CustomerId == customerId &&
                                      o.Name.ToLower() == organizationName.ToLower());

        if (existingOrg != null)
        {
            cache[cacheKey] = existingOrg;
            return existingOrg;
        }

        // Create new organization
        var newOrg = new CustomerOrganization
        {
            Name = organizationName,
            CustomerId = customerId,
            Level = 0,
            Order = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Description = "CSV Import ile oluşturuldu"
        };

        _context.CustomerOrganizations.Add(newOrg);
        await _context.SaveChangesAsync();

        cache[cacheKey] = newOrg;
        return newOrg;
    }

    private async Task<Customer?> GetOrCreateCustomerAsync(
        string companyName,
        Dictionary<string, Customer> cache,
        ImportResultDto result)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return null;

        // Check cache first
        if (cache.TryGetValue(companyName, out var cachedCustomer))
            return cachedCustomer;

        // 1. Önce tam eşleşme ara
        var existingCustomer = await _customerRepository.GetByNameAsync(companyName);

        // 2. Bulunamadıysa LIKE ile ara (Contains)
        if (existingCustomer == null)
        {
            var matches = await _context.Customers
                .Where(c => !c.IsDeleted && c.CompanyName.ToLower().Contains(companyName.ToLower()))
                .ToListAsync();

            if (matches.Count == 1)
            {
                // Tek eşleşme - direkt kullan
                existingCustomer = matches[0];
            }
            else if (matches.Count > 1)
            {
                // Birden fazla eşleşme - AmbiguousMatches'a ekle, kullanıcı seçecek
                result.AmbiguousCompanyMatches[companyName] = matches
                    .Select(c => new CompanyMatchInfo { Id = c.Id, Name = c.CompanyName })
                    .ToList();
                return null; // Bu firma için personel eklenmeyecek, kullanıcı seçim yapmalı
            }
        }

        if (existingCustomer != null)
        {
            cache[companyName] = existingCustomer;
            result.CustomersExisted++;
            return existingCustomer;
        }

        // Hiç eşleşme yok - yeni firma oluştur
        var newCustomer = new Customer
        {
            CompanyName = companyName,
            TaxNumber = GenerateTempTaxNumber(companyName),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Notes = "CSV Import ile oluşturuldu"
        };

        var created = await _customerRepository.CreateAsync(newCustomer);
        cache[companyName] = created;
        result.CustomersCreated++;

        return created;
    }

    private static string GenerateTempTaxNumber(string companyName)
    {
        // Generate a temporary tax number based on company name hash
        var hash = companyName.GetHashCode();
        return $"TEMP-{Math.Abs(hash) % 10000000000:D10}";
    }

    private static (string firstName, string lastName) ParseFullName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return ("", "");

        if (parts.Length == 1)
            return (parts[0], "");

        // First part is first name, rest is last name
        var firstName = parts[0];
        var lastName = string.Join(" ", parts.Skip(1));

        return (firstName, lastName);
    }

    private static CustomerPersonnelRole ParseRole(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "customermanager" => CustomerPersonnelRole.CustomerManager,
            "customersupervisor" => CustomerPersonnelRole.CustomerSupervisor,
            "customeroperator" => CustomerPersonnelRole.CustomerOperator,
            "manager" => CustomerPersonnelRole.CustomerManager,
            "supervisor" => CustomerPersonnelRole.CustomerSupervisor,
            "operator" => CustomerPersonnelRole.CustomerOperator,
            _ => CustomerPersonnelRole.CustomerOperator
        };
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString().Trim());
        return result.ToArray();
    }

    private static Dictionary<string, int> CreateColumnMap(string[] header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < header.Length; i++)
        {
            var columnName = header[i].Trim().ToLowerInvariant();
            map[columnName] = i;
        }

        return map;
    }

    private static bool ValidateColumns(Dictionary<string, int> columnMap, ImportResultDto result)
    {
        var requiredColumns = new[] { "fullname", "username", "company" };
        var missingColumns = requiredColumns.Where(c => !columnMap.ContainsKey(c)).ToList();

        if (missingColumns.Any())
        {
            result.Errors.Add($"Eksik kolonlar: {string.Join(", ", missingColumns)}");
            return false;
        }

        return true;
    }

    private static PersonnelImportDto MapToDto(string[] values, Dictionary<string, int> columnMap)
    {
        return new PersonnelImportDto
        {
            FullName = GetValue(values, columnMap, "fullname"),
            Username = GetValue(values, columnMap, "username"),
            Email = GetValue(values, columnMap, "email", "a@b.com"),
            Password = GetValue(values, columnMap, "password", "user@123"),
            Role = GetValue(values, columnMap, "role", "CustomerOperator"),
            RoleId = int.TryParse(GetValue(values, columnMap, "roleid", "3"), out var roleId) ? roleId : 3,
            Company = GetValue(values, columnMap, "company"),
            Organization = GetValue(values, columnMap, "organization", "Merkez")
        };
    }

    private static string GetValue(string[] values, Dictionary<string, int> columnMap, string columnName, string defaultValue = "")
    {
        if (!columnMap.TryGetValue(columnName, out var index) || index >= values.Length)
            return defaultValue;

        var value = values[index].Trim();
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}
