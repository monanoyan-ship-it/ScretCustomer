using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IFieldWorkerRepository
{
    Task<FieldWorker?> GetByIdAsync(Guid id);
    Task<IEnumerable<FieldWorker>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<FieldWorker>> GetActiveAsync();
    Task<FieldWorker> CreateAsync(FieldWorker fieldWorker);
    Task<FieldWorker> UpdateAsync(FieldWorker fieldWorker);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, Guid? excludeId = null);
}
