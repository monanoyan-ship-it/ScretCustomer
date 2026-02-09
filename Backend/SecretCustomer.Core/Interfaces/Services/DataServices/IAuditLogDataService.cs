using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IAuditLogDataService
{
    Task<AuditLog?> GetByIdAsync(int id);
    Task<List<AuditLog>> GetAllAsync();
    Task AddAsync(AuditLog entity);
    Task UpdateAsync(AuditLog entity);
    Task DeleteAsync(int id);
}
