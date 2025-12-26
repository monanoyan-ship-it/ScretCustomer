using SecretCustomer.Core.DTOs.User;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;

    public UserService(IUserRepository userRepository, IAuditLogService auditLogService)
    {
        _userRepository = userRepository;
        _auditLogService = auditLogService;
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToDto);
    }

    public async Task<IEnumerable<UserDto>> GetByRoleAsync(UserRole role)
    {
        var users = await _userRepository.GetByRoleAsync(role);
        return users.Select(MapToDto);
    }

    public async Task<IEnumerable<UserDto>> GetByBranchAsync(int branchId)
    {
        var users = await _userRepository.GetByBranchAsync(branchId);
        return users.Select(MapToDto);
    }

    public async Task<IEnumerable<UserDto>> GetActiveUsersAsync()
    {
        var users = await _userRepository.GetActiveUsersAsync();
        return users.Select(MapToDto);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto createUserDto)
    {
        // Check if username already exists
        if (await _userRepository.ExistsByUsernameAsync(createUserDto.Username))
        {
            throw new InvalidOperationException("Username already exists");
        }

        // Check if email already exists
        if (await _userRepository.ExistsByEmailAsync(createUserDto.Email))
        {
            throw new InvalidOperationException("Email already exists");
        }

        // Create user entity
        var user = new User
        {
            Username = createUserDto.Username,
            Email = createUserDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            Role = createUserDto.Role,
            IsActive = createUserDto.IsActive,
            BranchId = createUserDto.BranchId,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(user);

        // Audit Log
        await _auditLogService.LogInfoAsync(
            $"Yeni kullanıcı oluşturuldu: {createdUser.Username} ({createdUser.Role})",
            "UserService");

        return MapToDto(createdUser);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserDto updateUserDto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {id} not found");
        }

        // Check if email is being changed and if it's already in use
        if (user.Email != updateUserDto.Email &&
            await _userRepository.ExistsByEmailAsync(updateUserDto.Email))
        {
            throw new InvalidOperationException("Email already exists");
        }

        // Update user properties
        user.Email = updateUserDto.Email;
        user.FirstName = updateUserDto.FirstName;
        user.LastName = updateUserDto.LastName;
        user.Role = updateUserDto.Role;
        user.IsActive = updateUserDto.IsActive;
        user.BranchId = updateUserDto.BranchId;

        var updatedUser = await _userRepository.UpdateAsync(user);

        // Audit Log
        await _auditLogService.LogInfoAsync(
            $"Kullanıcı güncellendi: {updatedUser.Username}",
            "UserService");

        return MapToDto(updatedUser);
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {id} not found");
        }

        await _userRepository.DeleteAsync(id);

        // Audit Log
        await _auditLogService.LogWarningAsync(
            $"Kullanıcı silindi: {user.Username}",
            "UserService");
    }

    public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Hash new password
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        // Update password
        var result = await _userRepository.ChangePasswordAsync(userId, newPasswordHash);

        // Audit Log
        await _auditLogService.LogInfoAsync(
            $"Kullanıcı şifresini değiştirdi: {user.Username}",
            "UserService");

        return result;
    }

    public async Task<bool> AdminChangePasswordAsync(int userId, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Hash new password
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        // Update password (admin doesn't need current password)
        var result = await _userRepository.ChangePasswordAsync(userId, newPasswordHash);

        // Audit Log
        await _auditLogService.LogWarningAsync(
            $"Admin kullanıcının şifresini değiştirdi: {user.Username}",
            "UserService");

        return result;
    }

    // Branch assignment
    public async Task<UserDto> AssignToBranchAsync(int userId, int branchId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        user.BranchId = branchId;
        var updatedUser = await _userRepository.UpdateAsync(user);
        return MapToDto(updatedUser);
    }

    public async Task<UserDto> RemoveFromBranchAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        user.BranchId = null;
        var updatedUser = await _userRepository.UpdateAsync(user);
        return MapToDto(updatedUser);
    }

    public async Task<IEnumerable<UserDto>> AssignMultipleToBranchAsync(List<int> userIds, int branchId)
    {
        var updatedUsers = new List<UserDto>();

        foreach (var userId in userIds)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.BranchId = branchId;
                var updatedUser = await _userRepository.UpdateAsync(user);
                updatedUsers.Add(MapToDto(updatedUser));
            }
        }

        return updatedUsers;
    }

    // Uniqueness checks
    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        return await _userRepository.ExistsByUsernameAsync(username);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _userRepository.ExistsByEmailAsync(email);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            IsActive = user.IsActive,
            BranchId = user.BranchId,
            BranchName = user.Branch?.Name,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
