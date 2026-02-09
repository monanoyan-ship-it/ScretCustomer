using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IAnnouncementDataService
{
    Task<Announcement?> GetByIdAsync(int id);
    Task<List<Announcement>> GetAllAsync();
    Task AddAsync(Announcement entity);
    Task UpdateAsync(Announcement entity);
    Task DeleteAsync(int id);
}
