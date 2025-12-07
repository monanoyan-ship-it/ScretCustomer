using SecretCustomer.Core.DTOs.User;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<IEnumerable<UserDto>> GetByRoleAsync(UserRole role);
    Task<IEnumerable<UserDto>> GetByBranchAsync(Guid branchId);
    Task<IEnumerable<UserDto>> GetActiveUsersAsync();
    Task<UserDto> CreateAsync(CreateUserDto createUserDto);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto updateUserDto);
    Task DeleteAsync(Guid id);
    Task<bool> ChangePasswordAsync(Guid userId, string newPassword);
    Task<bool> AdminChangePasswordAsync(Guid userId, string newPassword);

    // Branch assignment
    Task<UserDto> AssignToBranchAsync(Guid userId, Guid branchId);
    Task<UserDto> RemoveFromBranchAsync(Guid userId);
    Task<IEnumerable<UserDto>> AssignMultipleToBranchAsync(List<Guid> userIds, Guid branchId);
}
