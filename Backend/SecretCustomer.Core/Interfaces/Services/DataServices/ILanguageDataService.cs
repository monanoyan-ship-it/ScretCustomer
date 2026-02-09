using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ILanguageDataService
{
    Task<Language?> GetByIdAsync(int id);
    Task<List<Language>> GetAllAsync();
    Task AddAsync(Language entity);
    Task UpdateAsync(Language entity);
    Task DeleteAsync(int id);
}
