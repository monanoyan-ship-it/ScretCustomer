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
    private readonly ICustomerPersonnelOrganizationRepository _personnelOrgRepository;
    private readonly ApplicationDbContext _context;

    public ImportService(
        ICustomerRepository customerRepository,
        ICustomerPersonnelRepository customerPersonnelRepository,
        ICustomerPersonnelOrganizationRepository personnelOrgRepository,
        ApplicationDbContext context)
    {
        _customerRepository = customerRepository;
        _customerPersonnelRepository = customerPersonnelRepository;
        _personnelOrgRepository = personnelOrgRepository;
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

                    // Check if personnel exists in this company (username veya email ile)
                    // Aynı firmada aynı username var mı?
                    var existingPersonnel = await _context.CustomerPersonnel
                        .FirstOrDefaultAsync(p => p.CustomerId == customer.Id &&
                                                  p.Username.ToLower() == importDto.Username.ToLower() &&
                                                  !p.IsDeleted);

                    // Eğer username ile bulunamadıysa email ile kontrol et
                    if (existingPersonnel == null)
                    {
                        existingPersonnel = await _context.CustomerPersonnel
                            .FirstOrDefaultAsync(p => p.CustomerId == customer.Id &&
                                                      p.Email.ToLower() == importDto.Email.ToLower() &&
                                                      !p.IsDeleted);
                    }

                    if (existingPersonnel != null)
                    {
                        // Aynı firmada - organizasyonda mı kontrol et
                        if (organization != null)
                        {
                            var existsInOrg = await _personnelOrgRepository.ExistsAsync(existingPersonnel.Id, organization.Id);

                            if (existsInOrg)
                            {
                                // Zaten bu organizasyonda - uyarı ver ve atla
                                result.PersonnelSkipped++;
                                result.Warnings.Add($"Satır {rowNumber}: {importDto.Username} zaten {organization.Name} organizasyonunda mevcut");
                                result.ImportedPersonnel.Add(new ImportedPersonnelInfo
                                {
                                    Id = existingPersonnel.Id,
                                    FullName = importDto.FullName,
                                    Username = importDto.Username,
                                    Company = importDto.Company,
                                    Role = importDto.Role,
                                    Status = "AlreadyInOrg"
                                });
                                continue;
                            }
                            else
                            {
                                // Aynı kişi farklı organizasyona atanıyor - junction table'a ekle
                                var assignment = new CustomerPersonnelOrganization
                                {
                                    CustomerPersonnelId = existingPersonnel.Id,
                                    CustomerOrganizationId = organization.Id,
                                    SupervisorId = null, // CSV'den supervisor bilgisi gelmiyorsa null
                                    AssignedAt = DateTime.UtcNow,
                                    Notes = "CSV Import ile atandı",
                                    CreatedAt = DateTime.UtcNow
                                };

                                await _personnelOrgRepository.AddAsync(assignment);
                                result.PersonnelUpdated++;

                                result.ImportedPersonnel.Add(new ImportedPersonnelInfo
                                {
                                    Id = existingPersonnel.Id,
                                    FullName = importDto.FullName,
                                    Username = importDto.Username,
                                    Company = importDto.Company,
                                    Role = importDto.Role,
                                    Status = "AddedToOrg"
                                });
                                continue;
                            }
                        }

                        if (updateExisting)
                        {
                            // Update existing
                            existingPersonnel.FirstName = firstName;
                            existingPersonnel.LastName = lastName;
                            existingPersonnel.Email = importDto.Email;
                            existingPersonnel.RoleId = ParseRole(importDto.Role);
                            existingPersonnel.CustomerId = customer.Id;
                            // OrganizationId artık junction table'da - doğrudan set etme
                            existingPersonnel.UpdatedAt = DateTime.UtcNow;

                            await _customerPersonnelRepository.UpdateAsync(existingPersonnel);

                            // Junction table'da da organizasyona ekle (yoksa)
                            if (organization != null)
                            {
                                var existsInOrg = await _personnelOrgRepository.ExistsAsync(existingPersonnel.Id, organization.Id);
                                if (!existsInOrg)
                                {
                                    var assignment = new CustomerPersonnelOrganization
                                    {
                                        CustomerPersonnelId = existingPersonnel.Id,
                                        CustomerOrganizationId = organization.Id,
                                        SupervisorId = null, // Update durumunda supervisor belirlenemiyor
                                        AssignedAt = DateTime.UtcNow,
                                        Notes = "CSV Import ile güncellendi ve atandı",
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    await _personnelOrgRepository.AddAsync(assignment);
                                }
                            }
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
                        if (role == CustomerPersonnelRoles.Ids.Operator)
                        {
                            // Mevcut supervisor'ı al
                            currentSupervisorByCompany.TryGetValue(supervisorKey, out supervisorId);
                        }

                        // Create new personnel (OrganizationId ve SupervisorId artık junction table'da)
                        var newPersonnel = new CustomerPersonnel
                        {
                            CustomerId = customer.Id,
                            // OrganizationId ve SupervisorId artık junction table'da - doğrudan set etme
                            Username = importDto.Username,
                            Email = importDto.Email,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(importDto.Password),
                            FirstName = firstName,
                            LastName = lastName,
                            RoleId = role,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        var created = await _customerPersonnelRepository.CreateAsync(newPersonnel);
                        result.PersonnelCreated++;

                        // Junction table'a da ekle (multi-org desteği için)
                        if (organization != null)
                        {
                            var assignment = new CustomerPersonnelOrganization
                            {
                                CustomerPersonnelId = created.Id,
                                CustomerOrganizationId = organization.Id,
                                SupervisorId = supervisorId,
                                AssignedAt = DateTime.UtcNow,
                                Notes = "CSV Import ile oluşturuldu",
                                CreatedAt = DateTime.UtcNow
                            };
                            await _personnelOrgRepository.AddAsync(assignment);
                        }

                        // Eğer bu kişi Supervisor ise, sonraki operatörler için kaydet
                        if (role == CustomerPersonnelRoles.Ids.Supervisor)
                        {
                            currentSupervisorByCompany[supervisorKey] = created.Id;
                        }
                        // Manager gelirse supervisor sıfırlanır (isteğe bağlı)
                        else if (role == CustomerPersonnelRoles.Ids.Manager)
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
                    result.Errors.Add($"Satır {rowNumber}: {GetDeepestExceptionMessage(ex)}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Import hatası: {GetDeepestExceptionMessage(ex)}");
        }

        result.Success = result.Errors.Count == 0;
        return result;
    }

    private static string GetDeepestExceptionMessage(Exception ex)
    {
        var current = ex;
        while (current.InnerException != null)
            current = current.InnerException;
        return current.Message;
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

    private static int ParseRole(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "customermanager" => CustomerPersonnelRoles.Ids.Manager,
            "customersupervisor" => CustomerPersonnelRoles.Ids.Supervisor,
            "customeroperator" => CustomerPersonnelRoles.Ids.Operator,
            "manager" => CustomerPersonnelRoles.Ids.Manager,
            "supervisor" => CustomerPersonnelRoles.Ids.Supervisor,
            "operator" => CustomerPersonnelRoles.Ids.Operator,
            _ => CustomerPersonnelRoles.Ids.Operator
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
        var username = GetValue(values, columnMap, "username");
        var email = GetValue(values, columnMap, "email");

        // Email yoksa username@temp.com kullan
        if (string.IsNullOrWhiteSpace(email))
            email = $"{username}@temp.com";

        return new PersonnelImportDto
        {
            FullName = GetValue(values, columnMap, "fullname"),
            Username = username,
            Email = email,
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

    #region Checklist Import

    public async Task<ChecklistImportResultDto> ImportChecklistFromCsvAsync(string csvContent, string checklistName, int? customerId = null, string? description = null)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        return await ImportChecklistFromCsvAsync(stream, checklistName, customerId, description);
    }

    public async Task<ChecklistImportResultDto> ImportChecklistFromCsvAsync(Stream csvStream, string checklistName, int? customerId = null, string? description = null)
    {
        var result = new ChecklistImportResultDto { Success = true };

        try
        {
            // Yeni checklist oluştur
            var checklist = new Checklist
            {
                Name = checklistName,
                Description = description ?? $"CSV Import ile oluşturuldu - {DateTime.Now:dd.MM.yyyy HH:mm}",
                CustomerId = customerId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Checklists.Add(checklist);
            await _context.SaveChangesAsync();

            result.ChecklistId = checklist.Id;
            result.ChecklistName = checklist.Name;

            // CSV'yi oku
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

            if (!ValidateChecklistColumns(columnMap, result))
            {
                result.Success = false;
                return result;
            }

            result.TotalRows = lines.Count - 1;

            // Yeni checklist olduğu için order 0'dan başlar
            var maxOrder = 0;

            // Her satırı işle
            for (int i = 1; i < lines.Count; i++)
            {
                var rowNumber = i + 1;
                try
                {
                    var values = ParseCsvLine(lines[i]);
                    var importDto = MapToChecklistDto(values, columnMap);

                    if (string.IsNullOrWhiteSpace(importDto.QuestionText))
                    {
                        result.Warnings.Add($"Satır {rowNumber}: QuestionText boş, atlandı.");
                        continue;
                    }

                    maxOrder++;

                    // Yeni soru oluştur
                    var question = new Question
                    {
                        ChecklistId = checklist.Id,
                        Text = importDto.QuestionText,
                        GroupName = string.IsNullOrWhiteSpace(importDto.GroupName) ? null : importDto.GroupName,
                        Order = importDto.Order ?? maxOrder,
                        WeightPoints = importDto.WeightPoints,
                        MaxPoints = importDto.MaxPoints,
                        ScoringTypeId = ParseScoringType(importDto.ScoringType),
                        PenaltyTypeId = ParsePenaltyType(importDto.PenaltyType),
                        IsRequired = importDto.IsRequired,
                        HelpText = string.IsNullOrWhiteSpace(importDto.HelpText) ? null : importDto.HelpText,
                        AllowNA = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Questions.Add(question);
                    await _context.SaveChangesAsync();

                    result.QuestionsCreated++;

                    // Alt kriterleri ekle
                    if (!string.IsNullOrWhiteSpace(importDto.SubCriteria))
                    {
                        var subCriteriaList = importDto.SubCriteria.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        var subOrder = 0;

                        foreach (var subText in subCriteriaList)
                        {
                            var trimmedText = subText.Trim();
                            if (string.IsNullOrWhiteSpace(trimmedText)) continue;

                            subOrder++;
                            var subCriteria = new QuestionSubCriteria
                            {
                                QuestionId = question.Id,
                                Description = trimmedText,
                                WeightPoints = 0,
                                Order = subOrder,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.QuestionSubCriteria.Add(subCriteria);
                            result.SubCriteriaCreated++;
                        }

                        await _context.SaveChangesAsync();
                    }

                    result.ImportedQuestions.Add(new ImportedQuestionInfo
                    {
                        Id = question.Id,
                        Order = question.Order,
                        GroupName = question.GroupName ?? "",
                        QuestionText = question.Text,
                        WeightPoints = question.WeightPoints,
                        MaxPoints = question.MaxPoints,
                        ScoringType = ScoringTypes.GetById(question.ScoringTypeId)?.SystemName ?? "Scored",
                        SubCriteriaCount = importDto.SubCriteria?.Split('|', StringSplitOptions.RemoveEmptyEntries).Length ?? 0
                    });
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Satır {rowNumber}: {GetDeepestExceptionMessage(ex)}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Import hatası: {GetDeepestExceptionMessage(ex)}");
        }

        result.Success = result.Errors.Count == 0;
        return result;
    }

    private static bool ValidateChecklistColumns(Dictionary<string, int> columnMap, ChecklistImportResultDto result)
    {
        var requiredColumns = new[] { "questiontext" };
        var missingColumns = requiredColumns.Where(c => !columnMap.ContainsKey(c)).ToList();

        if (missingColumns.Any())
        {
            result.Errors.Add($"Eksik kolonlar: {string.Join(", ", missingColumns)}");
            return false;
        }

        return true;
    }

    private static ChecklistQuestionImportDto MapToChecklistDto(string[] values, Dictionary<string, int> columnMap)
    {
        return new ChecklistQuestionImportDto
        {
            GroupName = GetValue(values, columnMap, "groupname"),
            QuestionText = GetValue(values, columnMap, "questiontext"),
            WeightPoints = decimal.TryParse(GetValue(values, columnMap, "weightpoints", "1"), NumberStyles.Any, CultureInfo.InvariantCulture, out var wp) ? wp : 1,
            MaxPoints = int.TryParse(GetValue(values, columnMap, "maxpoints", "5"), out var mp) ? mp : 5,
            ScoringType = GetValue(values, columnMap, "scoringtype", "Scored"),
            PenaltyType = GetValue(values, columnMap, "penaltytype", "None"),
            SubCriteria = GetValue(values, columnMap, "subcriteria"),
            Order = int.TryParse(GetValue(values, columnMap, "order"), out var order) ? order : null,
            IsRequired = bool.TryParse(GetValue(values, columnMap, "isrequired", "false"), out var req) && req,
            HelpText = GetValue(values, columnMap, "helptext")
        };
    }

    private static int ParseScoringType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "scored" => ScoringTypes.Ids.Scored,
            "unscored" => ScoringTypes.Ids.Unscored,
            "penalty" => ScoringTypes.Ids.Penalty,
            "puanlı" => ScoringTypes.Ids.Scored,
            "puansız" => ScoringTypes.Ids.Unscored,
            "cezalı" => ScoringTypes.Ids.Penalty,
            _ => ScoringTypes.Ids.Scored
        };
    }

    private static int ParsePenaltyType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "none" => PenaltyTypes.Ids.None,
            "yellowcard" => PenaltyTypes.Ids.YellowCard,
            "redcard" => PenaltyTypes.Ids.RedCard,
            "yok" => PenaltyTypes.Ids.None,
            "sarı" or "sarı kart" or "sarıkart" => PenaltyTypes.Ids.YellowCard,
            "kırmızı" or "kırmızı kart" or "kırmızıkart" => PenaltyTypes.Ids.RedCard,
            _ => PenaltyTypes.Ids.None
        };
    }

    #endregion
}
