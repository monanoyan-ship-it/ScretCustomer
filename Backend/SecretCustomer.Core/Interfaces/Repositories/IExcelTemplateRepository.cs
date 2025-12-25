using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IExcelTemplateRepository
{
    Task<ExcelTemplate?> GetByIdAsync(int id, bool includeColumns = false);
    Task<IEnumerable<ExcelTemplate>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<ExcelTemplate>> GetByEntityTypeAsync(string entityType);
    Task<ExcelTemplate> CreateAsync(ExcelTemplate template);
    Task<ExcelTemplate> UpdateAsync(ExcelTemplate template);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
