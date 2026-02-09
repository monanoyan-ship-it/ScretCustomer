using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IExcelColumnDataService
{
    Task<ExcelColumn?> GetByIdAsync(int id);
    Task<List<ExcelColumn>> GetAllAsync();
    Task AddAsync(ExcelColumn entity);
    Task UpdateAsync(ExcelColumn entity);
    Task DeleteAsync(int id);
}
