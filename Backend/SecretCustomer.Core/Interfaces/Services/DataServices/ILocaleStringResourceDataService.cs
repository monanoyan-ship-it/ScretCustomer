using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ILocaleStringResourceDataService
{
    Task<LocaleStringResource?> GetByIdAsync(int id);
    Task<List<LocaleStringResource>> GetAllAsync();
    Task AddAsync(LocaleStringResource entity);
    Task UpdateAsync(LocaleStringResource entity);
    Task DeleteAsync(int id);
}
