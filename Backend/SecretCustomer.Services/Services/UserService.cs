using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.User;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ApplicationDbContext _context;

    public UserService(IUserRepository userRepository, IAuditLogService auditLogService, ApplicationDbContext context)
    {
        _userRepository = userRepository;
        _auditLogService = auditLogService;
        _context = context;
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        var userIds = users.Select(u => u.Id).ToList();

        // İstatistikleri çek
        var assignmentStats = await _context.Assignments
            .Where(a => a.AssignedUserId.HasValue && userIds.Contains(a.AssignedUserId.Value))
            .GroupBy(a => a.AssignedUserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var evaluationStats = await _context.Evaluations
            .Where(e => e.EvaluatorId.HasValue && userIds.Contains(e.EvaluatorId.Value))
            .GroupBy(e => e.EvaluatorId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        return users.Select(u => MapToDto(u,
            assignmentStats.GetValueOrDefault(u.Id, 0),
            evaluationStats.GetValueOrDefault(u.Id, 0))).ToList();
    }

    public async Task<IEnumerable<UserDto>> GetByRoleIdAsync(int roleId)
    {
        var users = await _userRepository.GetByRoleIdAsync(roleId);
        return users.Select(u => MapToDto(u)).ToList();
    }

    public async Task<IEnumerable<UserDto>> GetActiveUsersAsync()
    {
        var users = await _userRepository.GetActiveUsersAsync();
        return users.Select(u => MapToDto(u)).ToList();
    }

    /// <summary>
    /// Liste görünümü için - Filter DTO ile (çoklu filtre destekli)
    /// </summary>
    public async Task<IEnumerable<UserDto>> GetListAsync(UserFilterDto filter)
    {
        var query = _context.Users
            .Where(u => !u.IsDeleted)
            .AsQueryable();

        // Çoklu RoleIds filtresi
        if (filter.RoleIds?.Any() == true)
            query = query.Where(u => filter.RoleIds.Contains(u.RoleId));

        // IsActive filtresi
        if (filter.IsActive.HasValue)
            query = query.Where(u => u.IsActive == filter.IsActive.Value);

        // Arama
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                (u.FirstName + " " + u.LastName).ToLower().Contains(term));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var userIds = users.Select(u => u.Id).ToList();

        // İstatistikleri çek
        var assignmentStats = await _context.Assignments
            .Where(a => a.AssignedUserId.HasValue && userIds.Contains(a.AssignedUserId.Value))
            .GroupBy(a => a.AssignedUserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var evaluationStats = await _context.Evaluations
            .Where(e => e.EvaluatorId.HasValue && userIds.Contains(e.EvaluatorId.Value))
            .GroupBy(e => e.EvaluatorId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        return users.Select(u => MapToDto(u,
            assignmentStats.GetValueOrDefault(u.Id, 0),
            evaluationStats.GetValueOrDefault(u.Id, 0))).ToList();
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
            RoleId = createUserDto.RoleId,
            IsActive = createUserDto.IsActive,
            CreatedAt = TurkeyTime.Now
        };

        var createdUser = await _userRepository.CreateAsync(user);

        // Audit Log
        var roleName = UserRoles.GetById(createdUser.RoleId)?.SystemName ?? createdUser.RoleId.ToString();
        await _auditLogService.LogInfoAsync(
            $"Yeni kullanıcı oluşturuldu: {createdUser.Username} ({roleName})",
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
        user.PhoneNumber = updateUserDto.PhoneNumber;
        user.RoleId = updateUserDto.RoleId;
        user.IsActive = updateUserDto.IsActive;

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

    // Uniqueness checks
    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        return await _userRepository.ExistsByUsernameAsync(username);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _userRepository.ExistsByEmailAsync(email);
    }

    private static UserDto MapToDto(User user, int assignmentCount = 0, int evaluationCount = 0)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            RoleId = user.RoleId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt,
            AssignmentCount = assignmentCount,
            EvaluationCount = evaluationCount
        };
    }
}
