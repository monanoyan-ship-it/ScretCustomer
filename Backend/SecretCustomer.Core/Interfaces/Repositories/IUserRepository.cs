using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IUserRepository
{
    // Authentication related
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByUsernameAsync(string username);
    Task<bool> ExistsByEmailAsync(string email);

    // CRUD operations
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(int id);
    Task<bool> ChangePasswordAsync(int userId, string newPasswordHash);
}
