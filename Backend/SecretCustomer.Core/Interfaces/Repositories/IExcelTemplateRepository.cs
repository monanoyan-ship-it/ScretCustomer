using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IExcelTemplateRepository
{
    Task<ExcelTemplate?> GetByIdAsync(Guid id, bool includeColumns = false);
    Task<IEnumerable<ExcelTemplate>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<ExcelTemplate>> GetByEntityTypeAsync(string entityType);
    Task<ExcelTemplate> CreateAsync(ExcelTemplate template);
    Task<ExcelTemplate> UpdateAsync(ExcelTemplate template);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
