using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Customer;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILocalizationService _localizationService;

    public CustomerService(ICustomerRepository customerRepository, ApplicationDbContext context, ILocalizationService localizationService)
    {
        _customerRepository = customerRepository;
        _context = context;
        _localizationService = localizationService;
    }

    // DateTime'ı UTC'ye çevirir (PostgreSQL için gerekli)
    private static DateTime? ToUtc(DateTime? dateTime)
    {
        if (dateTime == null) return null;
        if (dateTime.Value.Kind == DateTimeKind.Utc) return dateTime;
        return DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id, includeDetails: true);
        return customer == null ? null : MapToDto(customer);
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync(bool includeInactive = false)
    {
        var customers = await _customerRepository.GetAllAsync(includeInactive);
        return customers.Select(MapToDto);
    }

    public async Task<IEnumerable<CustomerDto>> GetActiveAsync()
    {
        var customers = await _customerRepository.GetActiveAsync();
        return customers.Select(MapToDto);
    }

    public async Task<IEnumerable<CustomerListDto>> GetListAsync(bool includeInactive = false)
    {
        var query = _context.Customers
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        // Projection - Include kullanmadan COUNT subqueries ile
        return await query
            .OrderBy(c => c.CompanyName)
            .Select(c => new CustomerListDto
            {
                Id = c.Id,
                Code = c.Code,
                CompanyName = c.CompanyName,
                TaxNumber = c.TaxNumber,
                Phone = c.Phone,
                Email = c.Email,
                City = c.City,
                IsActive = c.IsActive,
                ContractStartDate = c.ContractStartDate,
                ContractEndDate = c.ContractEndDate,
                CreatedAt = c.CreatedAt,
                // COUNT subqueries - çok daha hızlı
                PersonnelCount = c.Personnel.Count(p => !p.IsDeleted),
                OrganizationCount = c.Organizations.Count(o => !o.IsDeleted),
                BranchCount = c.Organizations.Count(o => !o.IsDeleted),
                ProjectCount = c.Projects.Count(p => !p.IsDeleted),
                // Hedefler ve Kotalar
                TargetCount = c.TargetCount,
                DailyQuota = c.DailyQuota,
                WeeklyQuota = c.WeeklyQuota,
                MonthlyQuota = c.MonthlyQuota
            })
            .ToListAsync();
    }

    public async Task<PagedCustomerResult> GetFilteredListAsync(CustomerFilterDto filter)
    {
        var query = _context.Customers
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        // Active status filter
        if (!filter.IncludeInactive)
        {
            if (filter.IsActive.HasValue)
                query = query.Where(c => c.IsActive == filter.IsActive.Value);
            else
                query = query.Where(c => c.IsActive);
        }

        // Search term (global search)
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(c =>
                c.CompanyName.ToLower().Contains(term) ||
                (c.Code != null && c.Code.ToLower().Contains(term)) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                (c.City != null && c.City.ToLower().Contains(term)) ||
                (c.TaxNumber != null && c.TaxNumber.ToLower().Contains(term)));
        }

        // Çoklu firma adı filtresi (OR mantığı)
        if (filter.CompanyNames?.Any() == true)
        {
            var names = filter.CompanyNames.Select(n => n.ToLower()).ToList();
            query = query.Where(c => names.Any(n => c.CompanyName.ToLower().Contains(n)));
        }

        // Çoklu kod filtresi (OR mantığı)
        if (filter.Codes?.Any() == true)
        {
            var codes = filter.Codes.Select(code => code.ToLower()).ToList();
            query = query.Where(c => c.Code != null && codes.Any(code => c.Code.ToLower().Contains(code)));
        }

        // Çoklu email filtresi (OR mantığı)
        if (filter.Emails?.Any() == true)
        {
            var emails = filter.Emails.Select(e => e.ToLower()).ToList();
            query = query.Where(c => c.Email != null && emails.Any(email => c.Email.ToLower().Contains(email)));
        }

        // Çoklu şehir filtresi (OR mantığı)
        if (filter.Cities?.Any() == true)
        {
            var cities = filter.Cities.Select(city => city.ToLower()).ToList();
            query = query.Where(c => c.City != null && cities.Any(city => c.City.ToLower().Contains(city)));
        }

        // Çoklu vergi numarası filtresi (OR mantığı)
        if (filter.TaxNumbers?.Any() == true)
        {
            var taxNumbers = filter.TaxNumbers.Select(t => t.ToLower()).ToList();
            query = query.Where(c => c.TaxNumber != null && taxNumbers.Any(t => c.TaxNumber.ToLower().Contains(t)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Sorting
        var sortBy = filter.SortBy?.ToLower() ?? "companyname";
        var sortDesc = filter.SortDirection?.ToLower() == "desc";

        query = sortBy switch
        {
            "code" => sortDesc ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
            "email" => sortDesc ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            "city" => sortDesc ? query.OrderByDescending(c => c.City) : query.OrderBy(c => c.City),
            "taxnumber" => sortDesc ? query.OrderByDescending(c => c.TaxNumber) : query.OrderBy(c => c.TaxNumber),
            "isactive" => sortDesc ? query.OrderByDescending(c => c.IsActive) : query.OrderBy(c => c.IsActive),
            "createdat" => sortDesc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
            _ => sortDesc ? query.OrderByDescending(c => c.CompanyName) : query.OrderBy(c => c.CompanyName)
        };

        // Pagination
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new CustomerListDto
            {
                Id = c.Id,
                Code = c.Code,
                CompanyName = c.CompanyName,
                TaxNumber = c.TaxNumber,
                Phone = c.Phone,
                Email = c.Email,
                City = c.City,
                IsActive = c.IsActive,
                ContractStartDate = c.ContractStartDate,
                ContractEndDate = c.ContractEndDate,
                CreatedAt = c.CreatedAt,
                PersonnelCount = c.Personnel.Count(p => !p.IsDeleted),
                OrganizationCount = c.Organizations.Count(o => !o.IsDeleted),
                BranchCount = c.Organizations.Count(o => !o.IsDeleted),
                ProjectCount = c.Projects.Count(p => !p.IsDeleted),
                TargetCount = c.TargetCount,
                DailyQuota = c.DailyQuota,
                WeeklyQuota = c.WeeklyQuota,
                MonthlyQuota = c.MonthlyQuota
            })
            .ToListAsync();

        return new PagedCustomerResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<CustomerDto?> GetByTaxNumberAsync(string taxNumber)
    {
        var customer = await _customerRepository.GetByTaxNumberAsync(taxNumber);
        return customer == null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto createCustomerDto)
    {
        // Check if tax number already exists
        if (await _customerRepository.ExistsByTaxNumberAsync(createCustomerDto.TaxNumber))
        {
            throw new InvalidOperationException("Bu vergi numarası ile kayıtlı bir müşteri zaten mevcut");
        }

        // Check if email already exists (if provided)
        if (!string.IsNullOrEmpty(createCustomerDto.Email) &&
            await _customerRepository.ExistsByEmailAsync(createCustomerDto.Email))
        {
            throw new InvalidOperationException("Bu e-posta adresi ile kayıtlı bir müşteri zaten mevcut");
        }

        var customer = new Customer
        {
            Code = createCustomerDto.Code,
            CompanyName = createCustomerDto.CompanyName,
            TaxNumber = createCustomerDto.TaxNumber,
            Phone = createCustomerDto.Phone,
            Email = createCustomerDto.Email,
            Address = createCustomerDto.Address,
            City = createCustomerDto.City,
            IsActive = createCustomerDto.IsActive,
            ContractStartDate = ToUtc(createCustomerDto.ContractStartDate),
            ContractEndDate = ToUtc(createCustomerDto.ContractEndDate),
            Notes = createCustomerDto.Notes,
            TargetCount = createCustomerDto.TargetCount,
            DailyQuota = createCustomerDto.DailyQuota,
            WeeklyQuota = createCustomerDto.WeeklyQuota,
            MonthlyQuota = createCustomerDto.MonthlyQuota,
            EvaluationNotificationFrequencyId = createCustomerDto.EvaluationNotificationFrequencyId,
            EvaluationNotificationTemplateId = createCustomerDto.EvaluationNotificationTemplateId,
            NotificationEmails = createCustomerDto.NotificationEmails,
            CreatedAt = TurkeyTime.Now
        };

        // Bildirim kurallarını ekle
        if (createCustomerDto.NotificationRules?.Any() == true)
        {
            foreach (var ruleDto in createCustomerDto.NotificationRules)
            {
                customer.NotificationRules.Add(new CustomerNotificationRule
                {
                    FrequencyId = ruleDto.FrequencyId,
                    DayOfWeek = ruleDto.DayOfWeek,
                    DayOfMonth = ruleDto.DayOfMonth,
                    Emails = ruleDto.Emails,
                    SendToPersonnel = ruleDto.SendToPersonnel,
                    SendToSupervisor = ruleDto.SendToSupervisor,
                    EmailTemplateId = ruleDto.EmailTemplateId,
                    TokenExpirationDays = ruleDto.TokenExpirationDays,
                    IsActive = ruleDto.IsActive,
                    CreatedAt = TurkeyTime.Now
                });
            }
        }

        var createdCustomer = await _customerRepository.CreateAsync(customer);
        return MapToDto(createdCustomer);
    }

    public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerDto updateCustomerDto)
    {
        var customer = await _customerRepository.GetByIdAsync(id, includeDetails: true);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Müşteri bulunamadı (ID: {id})");
        }

        // Check if tax number is being changed and if it's already in use
        if (customer.TaxNumber != updateCustomerDto.TaxNumber &&
            await _customerRepository.ExistsByTaxNumberAsync(updateCustomerDto.TaxNumber, id))
        {
            throw new InvalidOperationException("Bu vergi numarası ile kayıtlı bir müşteri zaten mevcut");
        }

        // Check if email is being changed and if it's already in use
        if (!string.IsNullOrEmpty(updateCustomerDto.Email) &&
            customer.Email != updateCustomerDto.Email &&
            await _customerRepository.ExistsByEmailAsync(updateCustomerDto.Email, id))
        {
            throw new InvalidOperationException("Bu e-posta adresi ile kayıtlı bir müşteri zaten mevcut");
        }

        customer.Code = updateCustomerDto.Code;
        customer.CompanyName = updateCustomerDto.CompanyName;
        customer.TaxNumber = updateCustomerDto.TaxNumber;
        customer.Phone = updateCustomerDto.Phone;
        customer.Email = updateCustomerDto.Email;
        customer.Address = updateCustomerDto.Address;
        customer.City = updateCustomerDto.City;
        customer.IsActive = updateCustomerDto.IsActive;
        customer.ContractStartDate = ToUtc(updateCustomerDto.ContractStartDate);
        customer.ContractEndDate = ToUtc(updateCustomerDto.ContractEndDate);
        customer.Notes = updateCustomerDto.Notes;
        customer.TargetCount = updateCustomerDto.TargetCount;
        customer.DailyQuota = updateCustomerDto.DailyQuota;
        customer.WeeklyQuota = updateCustomerDto.WeeklyQuota;
        customer.MonthlyQuota = updateCustomerDto.MonthlyQuota;
        customer.EvaluationNotificationFrequencyId = updateCustomerDto.EvaluationNotificationFrequencyId;
        customer.EvaluationNotificationTemplateId = updateCustomerDto.EvaluationNotificationTemplateId;
        customer.NotificationEmails = updateCustomerDto.NotificationEmails;

        // Bildirim kurallarını güncelle (gönderilmişse)
        if (updateCustomerDto.NotificationRules != null)
        {
            var existingRules = customer.NotificationRules.ToList();
            var incomingIds = updateCustomerDto.NotificationRules
                .Where(r => r.Id > 0)
                .Select(r => r.Id)
                .ToHashSet();

            // Silinecek kurallar (incoming'de olmayan mevcut kurallar)
            var toRemove = existingRules.Where(r => !incomingIds.Contains(r.Id)).ToList();
            foreach (var rule in toRemove)
            {
                _context.CustomerNotificationRules.Remove(rule);
            }

            // Mevcut kuralları güncelle
            foreach (var ruleDto in updateCustomerDto.NotificationRules.Where(r => r.Id > 0))
            {
                var existing = existingRules.FirstOrDefault(r => r.Id == ruleDto.Id);
                if (existing != null)
                {
                    existing.FrequencyId = ruleDto.FrequencyId;
                    existing.DayOfWeek = ruleDto.DayOfWeek;
                    existing.DayOfMonth = ruleDto.DayOfMonth;
                    existing.Emails = ruleDto.Emails;
                    existing.SendToPersonnel = ruleDto.SendToPersonnel;
                    existing.SendToSupervisor = ruleDto.SendToSupervisor;
                    existing.EmailTemplateId = ruleDto.EmailTemplateId;
                    existing.TokenExpirationDays = ruleDto.TokenExpirationDays;
                    existing.IsActive = ruleDto.IsActive;
                    existing.UpdatedAt = TurkeyTime.Now;
                }
            }

            // Yeni kuralları ekle (Id == 0)
            foreach (var ruleDto in updateCustomerDto.NotificationRules.Where(r => r.Id == 0))
            {
                customer.NotificationRules.Add(new CustomerNotificationRule
                {
                    FrequencyId = ruleDto.FrequencyId,
                    DayOfWeek = ruleDto.DayOfWeek,
                    DayOfMonth = ruleDto.DayOfMonth,
                    Emails = ruleDto.Emails,
                    SendToPersonnel = ruleDto.SendToPersonnel,
                    SendToSupervisor = ruleDto.SendToSupervisor,
                    EmailTemplateId = ruleDto.EmailTemplateId,
                    TokenExpirationDays = ruleDto.TokenExpirationDays,
                    IsActive = ruleDto.IsActive,
                    CreatedAt = TurkeyTime.Now
                });
            }
        }

        var updatedCustomer = await _customerRepository.UpdateAsync(customer);
        return MapToDto(updatedCustomer);
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Müşteri bulunamadı (ID: {id})");
        }

        await _customerRepository.DeleteAsync(id);
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Code = customer.Code,
            CompanyName = customer.CompanyName,
            TaxNumber = customer.TaxNumber,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address,
            City = customer.City,
            IsActive = customer.IsActive,
            ContractStartDate = customer.ContractStartDate,
            ContractEndDate = customer.ContractEndDate,
            Notes = customer.Notes,
            PersonnelCount = customer.Personnel?.Count(p => !p.IsDeleted) ?? 0,
            OrganizationCount = customer.Organizations?.Count(o => !o.IsDeleted) ?? 0,
            BranchCount = customer.Organizations?.Count(o => !o.IsDeleted) ?? 0, // Now shows organization count
            ProjectCount = customer.Projects?.Count(p => !p.IsDeleted) ?? 0,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            TargetCount = customer.TargetCount,
            DailyQuota = customer.DailyQuota,
            WeeklyQuota = customer.WeeklyQuota,
            MonthlyQuota = customer.MonthlyQuota,
            EvaluationNotificationFrequencyId = customer.EvaluationNotificationFrequencyId,
            EvaluationNotificationTemplateId = customer.EvaluationNotificationTemplateId,
            EvaluationNotificationTemplateName = customer.EvaluationNotificationTemplate?.Name,
            NotificationEmails = customer.NotificationEmails,
            LastNotificationSentAt = customer.LastNotificationSentAt,
            NotificationRules = customer.NotificationRules?.Select(r => new CustomerNotificationRuleDto
            {
                Id = r.Id,
                CustomerId = r.CustomerId,
                FrequencyId = r.FrequencyId,
                DayOfWeek = r.DayOfWeek,
                DayOfMonth = r.DayOfMonth,
                Emails = r.Emails,
                SendToPersonnel = r.SendToPersonnel,
                SendToSupervisor = r.SendToSupervisor,
                EmailTemplateId = r.EmailTemplateId,
                EmailTemplateName = r.EmailTemplate?.Name,
                TokenExpirationDays = r.TokenExpirationDays,
                IsActive = r.IsActive,
                LastSentAt = r.LastSentAt
            }).ToList() ?? new()
        };
    }

    public async Task<ExcelExportDto?> ExportPersonnelToExcelAsync(int customerId)
    {
        // Müşteriyi getir
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted);

        if (customer == null)
            return null;

        // Tüm organizasyonları ve personel atamalarını getir
        var organizations = await _context.CustomerOrganizations
            .Include(o => o.PersonnelAssignments.Where(pa => !pa.IsDeleted))
                .ThenInclude(pa => pa.CustomerPersonnel)
            .Include(o => o.PersonnelAssignments.Where(pa => !pa.IsDeleted))
                .ThenInclude(pa => pa.Supervisor)
            .Where(o => o.CustomerId == customerId && !o.IsDeleted && o.IsActive)
            .OrderBy(o => o.Name)
            .ToListAsync();

        // Havuzdaki personeller (hiçbir organizasyona atanmamış)
        var allPersonnelIds = organizations
            .SelectMany(o => o.PersonnelAssignments)
            .Select(pa => pa.CustomerPersonnelId)
            .Distinct()
            .ToHashSet();

        var poolPersonnel = await _context.CustomerPersonnel
            .Where(p => p.CustomerId == customerId && !p.IsDeleted && p.IsActive && !allPersonnelIds.Contains(p.Id))
            .OrderBy(p => p.RoleId)
            .ThenBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .ToListAsync();

        // Yerelleştirilmiş metinleri al
        var sheetName = await _localizationService.GetResourceAsync("Excel.PersonnelList");
        var customerLabel = await _localizationService.GetResourceAsync("Common.Customer");
        var exportDateLabel = await _localizationService.GetResourceAsync("Excel.ExportDate");
        var organizationHeader = await _localizationService.GetResourceAsync("Common.Organization");
        var supervisorHeader = await _localizationService.GetResourceAsync("CustomerPersonnel.Supervisor");
        var fullNameHeader = await _localizationService.GetResourceAsync("Common.FullName");
        var usernameHeader = await _localizationService.GetResourceAsync("User.Username");
        var emailHeader = await _localizationService.GetResourceAsync("Common.Email");
        var phoneHeader = await _localizationService.GetResourceAsync("Common.Phone");
        var roleHeader = await _localizationService.GetResourceAsync("Common.Role");
        var departmentHeader = await _localizationService.GetResourceAsync("CustomerPersonnel.Department");
        var titleHeader = await _localizationService.GetResourceAsync("Customer.JobTitle");
        var independentLabel = await _localizationService.GetResourceAsync("Excel.Independent");
        var poolLabel = await _localizationService.GetResourceAsync("Excel.Pool");

        // Rol isimlerini önceden yükle
        var roleNames = new Dictionary<int, string>
        {
            { CustomerPersonnelRoles.Ids.Manager, await _localizationService.GetResourceAsync("Role.CustomerManager") },
            { CustomerPersonnelRoles.Ids.Supervisor, await _localizationService.GetResourceAsync("Role.CustomerSupervisor") },
            { CustomerPersonnelRoles.Ids.Operator, await _localizationService.GetResourceAsync("Role.CustomerOperator") },
            { CustomerPersonnelRoles.Ids.Inspector, await _localizationService.GetResourceAsync("Role.CustomerInspector") }
        };

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        // Header bilgileri
        sheet.Cell(1, 1).Value = customerLabel + ":";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 2).Value = customer.CompanyName;

        sheet.Cell(2, 1).Value = exportDateLabel + ":";
        sheet.Cell(2, 1).Style.Font.Bold = true;
        sheet.Cell(2, 2).Value = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");

        int row = 4;

        // Tablo başlıkları
        var headers = new[] { organizationHeader, supervisorHeader, fullNameHeader, usernameHeader, emailHeader, phoneHeader, roleHeader, departmentHeader, titleHeader };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(row, i + 1).Value = headers[i];
            sheet.Cell(row, i + 1).Style.Font.Bold = true;
            sheet.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            sheet.Cell(row, i + 1).Style.Font.FontColor = XLColor.White;
        }
        row++;

        // Her organizasyon için
        foreach (var org in organizations)
        {
            var assignments = org.PersonnelAssignments
                .Where(pa => pa.CustomerPersonnel != null)
                .ToList();

            if (!assignments.Any()) continue;

            // Süpervizörler ve ekipleri
            var supervisorAssignments = assignments
                .Where(pa => pa.SupervisorId.HasValue)
                .GroupBy(pa => pa.SupervisorId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(pa => pa.CustomerPersonnel!).ToList());

            // Süpervizörler (Manager ve Supervisor rolleri)
            var supervisorPersonnel = assignments
                .Select(pa => pa.CustomerPersonnel!)
                .DistinctBy(p => p.Id)
                .Where(p => p.RoleId == CustomerPersonnelRoles.Ids.Manager || p.RoleId == CustomerPersonnelRoles.Ids.Supervisor)
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .ToList();

            // Bağımsız süpervizörler (altında ekibi olmayanlar) - en üstte
            var independentSupervisors = supervisorPersonnel
                .Where(s => !supervisorAssignments.ContainsKey(s.Id) || !supervisorAssignments[s.Id].Any())
                .ToList();

            foreach (var sup in independentSupervisors)
            {
                WritePersonnelRow(sheet, row, org.Name, "", sup, roleNames);
                row++;
            }

            // Süpervizörler ve ekipleri
            var supervisorsWithTeam = supervisorPersonnel
                .Where(s => supervisorAssignments.ContainsKey(s.Id) && supervisorAssignments[s.Id].Any())
                .ToList();

            foreach (var sup in supervisorsWithTeam)
            {
                // Süpervizör satırı
                WritePersonnelRow(sheet, row, org.Name, "", sup, roleNames);
                sheet.Cell(row, 3).Style.Font.Bold = true;
                row++;

                // Ekip üyeleri
                var teamMembers = supervisorAssignments[sup.Id]
                    .OrderBy(tm => tm.FirstName)
                    .ThenBy(tm => tm.LastName)
                    .ToList();

                foreach (var tm in teamMembers)
                {
                    WritePersonnelRow(sheet, row, org.Name, $"{sup.FirstName} {sup.LastName}", tm, roleNames);
                    sheet.Cell(row, 3).Value = $"    └ {tm.FirstName} {tm.LastName}";
                    sheet.Cell(row, 3).Style.Font.FontColor = XLColor.FromHtml("#333333");
                    row++;
                }
            }

            // Bağımsız operatörler (süpervizörü olmayan)
            var independentOperators = assignments
                .Where(pa => pa.CustomerPersonnel != null && pa.CustomerPersonnel.RoleId == CustomerPersonnelRoles.Ids.Operator && pa.SupervisorId == null)
                .Select(pa => pa.CustomerPersonnel!)
                .DistinctBy(p => p.Id)
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .ToList();

            foreach (var op in independentOperators)
            {
                WritePersonnelRow(sheet, row, org.Name, $"({independentLabel})", op, roleNames);
                row++;
            }
        }

        // Havuzdaki personeller (organizasyona atanmamış)
        if (poolPersonnel.Any())
        {
            row++; // Boş satır

            foreach (var p in poolPersonnel)
            {
                WritePersonnelRow(sheet, row, $"({poolLabel})", "", p, roleNames);
                sheet.Row(row).Style.Font.FontColor = XLColor.Gray;
                row++;
            }
        }

        // Kolon genişliklerini ayarla
        sheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(sheet);

        // Excel'i byte array'e çevir
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"{customer.CompanyName.Replace(" ", "_")}_Personel_{TurkeyTime.Now:yyyyMMdd}.xlsx";

        return new ExcelExportDto
        {
            FileContent = stream.ToArray(),
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    private static void WritePersonnelRow(IXLWorksheet sheet, int row, string orgName, string supervisorName, CustomerPersonnel p, Dictionary<int, string> roleNames)
    {
        sheet.Cell(row, 1).Value = orgName;
        sheet.Cell(row, 2).Value = supervisorName;
        sheet.Cell(row, 3).Value = $"{p.FirstName} {p.LastName}";
        sheet.Cell(row, 4).Value = p.Username;
        sheet.Cell(row, 5).Value = p.Email;
        sheet.Cell(row, 6).Value = p.PhoneNumber ?? "";
        sheet.Cell(row, 7).Value = roleNames.TryGetValue(p.RoleId, out var roleName) ? roleName : CustomerPersonnelRoles.GetById(p.RoleId)?.SystemName ?? "";
        sheet.Cell(row, 8).Value = p.Department ?? "";
        sheet.Cell(row, 9).Value = p.Title ?? "";
    }

}
