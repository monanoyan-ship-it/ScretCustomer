using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Customer;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ApplicationDbContext _context;

    public CustomerService(ICustomerRepository customerRepository, ApplicationDbContext context)
    {
        _customerRepository = customerRepository;
        _context = context;
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
            CreatedAt = DateTime.UtcNow
        };

        var createdCustomer = await _customerRepository.CreateAsync(customer);
        return MapToDto(createdCustomer);
    }

    public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerDto updateCustomerDto)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
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
            UpdatedAt = customer.UpdatedAt
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
            .OrderBy(p => p.Role)
            .ThenBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Personel Listesi");

        // Header bilgileri
        sheet.Cell(1, 1).Value = "Müşteri:";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 2).Value = customer.CompanyName;

        sheet.Cell(2, 1).Value = "Dışa Aktarma Tarihi:";
        sheet.Cell(2, 1).Style.Font.Bold = true;
        sheet.Cell(2, 2).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        int row = 4;

        // Tablo başlıkları
        var headers = new[] { "Organizasyon", "Süpervizör", "Ad Soyad", "Kullanıcı Adı", "E-posta", "Telefon", "Rol", "Departman", "Ünvan" };
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
                .Where(p => p.Role == CustomerPersonnelRole.CustomerManager || p.Role == CustomerPersonnelRole.CustomerSupervisor)
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .ToList();

            // Bağımsız süpervizörler (altında ekibi olmayanlar) - en üstte
            var independentSupervisors = supervisorPersonnel
                .Where(s => !supervisorAssignments.ContainsKey(s.Id) || !supervisorAssignments[s.Id].Any())
                .ToList();

            foreach (var sup in independentSupervisors)
            {
                WritePersonnelRow(sheet, row, org.Name, "", sup);
                row++;
            }

            // Süpervizörler ve ekipleri
            var supervisorsWithTeam = supervisorPersonnel
                .Where(s => supervisorAssignments.ContainsKey(s.Id) && supervisorAssignments[s.Id].Any())
                .ToList();

            foreach (var sup in supervisorsWithTeam)
            {
                // Süpervizör satırı
                WritePersonnelRow(sheet, row, org.Name, "", sup);
                sheet.Cell(row, 3).Style.Font.Bold = true;
                row++;

                // Ekip üyeleri
                var teamMembers = supervisorAssignments[sup.Id]
                    .OrderBy(tm => tm.FirstName)
                    .ThenBy(tm => tm.LastName)
                    .ToList();

                foreach (var tm in teamMembers)
                {
                    WritePersonnelRow(sheet, row, org.Name, $"{sup.FirstName} {sup.LastName}", tm);
                    sheet.Cell(row, 3).Value = $"    └ {tm.FirstName} {tm.LastName}";
                    sheet.Cell(row, 3).Style.Font.FontColor = XLColor.FromHtml("#333333");
                    row++;
                }
            }

            // Bağımsız operatörler (süpervizörü olmayan)
            var independentOperators = assignments
                .Where(pa => pa.CustomerPersonnel?.Role == CustomerPersonnelRole.CustomerOperator && pa.SupervisorId == null)
                .Select(pa => pa.CustomerPersonnel!)
                .DistinctBy(p => p.Id)
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .ToList();

            foreach (var op in independentOperators)
            {
                WritePersonnelRow(sheet, row, org.Name, "(Bağımsız)", op);
                row++;
            }
        }

        // Havuzdaki personeller (organizasyona atanmamış)
        if (poolPersonnel.Any())
        {
            row++; // Boş satır

            foreach (var p in poolPersonnel)
            {
                WritePersonnelRow(sheet, row, "(Havuzda)", "", p);
                sheet.Row(row).Style.Font.FontColor = XLColor.Gray;
                row++;
            }
        }

        // Kolon genişliklerini ayarla
        sheet.Columns().AdjustToContents();

        // Excel'i byte array'e çevir
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"{customer.CompanyName.Replace(" ", "_")}_Personel_{DateTime.Now:yyyyMMdd}.xlsx";

        return new ExcelExportDto
        {
            FileContent = stream.ToArray(),
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    private void WritePersonnelRow(IXLWorksheet sheet, int row, string orgName, string supervisorName, CustomerPersonnel p)
    {
        sheet.Cell(row, 1).Value = orgName;
        sheet.Cell(row, 2).Value = supervisorName;
        sheet.Cell(row, 3).Value = $"{p.FirstName} {p.LastName}";
        sheet.Cell(row, 4).Value = p.Username;
        sheet.Cell(row, 5).Value = p.Email;
        sheet.Cell(row, 6).Value = p.PhoneNumber ?? "";
        sheet.Cell(row, 7).Value = GetRoleName(p.Role);
        sheet.Cell(row, 8).Value = p.Department ?? "";
        sheet.Cell(row, 9).Value = p.Title ?? "";
    }

    private static string GetRoleName(CustomerPersonnelRole role)
    {
        return role switch
        {
            CustomerPersonnelRole.CustomerManager => "Müşteri Yöneticisi",
            CustomerPersonnelRole.CustomerSupervisor => "Müşteri Süpervizörü",
            CustomerPersonnelRole.CustomerOperator => "Müşteri Operatörü",
            _ => role.ToString()
        };
    }
}
