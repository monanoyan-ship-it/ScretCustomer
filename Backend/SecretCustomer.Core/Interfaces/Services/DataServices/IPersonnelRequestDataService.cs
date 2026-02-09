using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IPersonnelRequestDataService
{
    Task<PersonnelRequest?> GetByIdAsync(int id);
    Task<List<PersonnelRequest>> GetAllAsync();
    Task AddAsync(PersonnelRequest entity);
    Task UpdateAsync(PersonnelRequest entity);
    Task DeleteAsync(int id);
}
