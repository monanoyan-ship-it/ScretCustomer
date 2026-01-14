using SecretCustomer.Core.DTOs.Customer;
using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ICustomerService
{
    Task<CustomerDto?> GetByIdAsync(int id);
    Task<IEnumerable<CustomerDto>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<CustomerDto>> GetActiveAsync();

    /// <summary>
    /// Performans için optimize edilmiş liste metodu - projection kullanır, Include kullanmaz
    /// </summary>
    Task<IEnumerable<CustomerListDto>> GetListAsync(bool includeInactive = false);
    Task<CustomerDto?> GetByTaxNumberAsync(string taxNumber);
    Task<CustomerDto> CreateAsync(CreateCustomerDto createCustomerDto);
    Task<CustomerDto> UpdateAsync(int id, UpdateCustomerDto updateCustomerDto);
    Task DeleteAsync(int id);

    /// <summary>
    /// Müşteri personel listesini Excel olarak dışa aktar
    /// Süpervizör ve bağlı personel hiyerarşisine göre düzenlenir
    /// </summary>
    Task<ExcelExportDto?> ExportPersonnelToExcelAsync(int customerId);
}
