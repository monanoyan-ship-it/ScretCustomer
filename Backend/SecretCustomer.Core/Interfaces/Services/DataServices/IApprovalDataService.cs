using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IApprovalDataService
{
    Task<Approval?> GetByIdAsync(int id);
    Task<List<Approval>> GetAllAsync();
    Task AddAsync(Approval entity);
    Task UpdateAsync(Approval entity);
    Task DeleteAsync(int id);
}
