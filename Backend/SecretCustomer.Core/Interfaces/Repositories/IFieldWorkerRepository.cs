using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IFieldWorkerRepository
{
    Task<FieldWorker?> GetByIdAsync(int id);
    Task<FieldWorker?> GetByUserIdAsync(int userId);
    Task<IEnumerable<FieldWorker>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<FieldWorker>> GetActiveAsync();
    Task<FieldWorker> CreateAsync(FieldWorker fieldWorker);
    Task<FieldWorker> UpdateAsync(FieldWorker fieldWorker);
    Task DeleteAsync(int id);
    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, int? excludeId = null);
}
