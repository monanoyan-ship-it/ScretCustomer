using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IExcelTemplateDataService
{
    Task<ExcelTemplate?> GetByIdAsync(int id);
    Task<List<ExcelTemplate>> GetAllAsync();
    Task AddAsync(ExcelTemplate entity);
    Task UpdateAsync(ExcelTemplate entity);
    Task DeleteAsync(int id);
}
