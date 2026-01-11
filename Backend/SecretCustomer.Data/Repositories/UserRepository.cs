using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Authentication related - Case-insensitive username
    public async Task<User?> GetByUsernameAsync(string username)
    {
        var lowerUsername = username.ToLower();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == lowerUsername);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var lowerEmail = email.ToLower();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == lowerEmail);
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        var lowerUsername = username.ToLower();
        return await _context.Users.AnyAsync(u => u.Username.ToLower() == lowerUsername);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        var lowerEmail = email.ToLower();
        return await _context.Users.AnyAsync(u => u.Email.ToLower() == lowerEmail);
    }

    // CRUD operations
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetByRoleIdAsync(int roleId)
    {
        return await _context.Users
            .Where(u => u.RoleId == roleId)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string newPasswordHash)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.PasswordHash = newPasswordHash;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
