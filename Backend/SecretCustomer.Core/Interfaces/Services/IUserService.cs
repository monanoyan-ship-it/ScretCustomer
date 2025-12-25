using SecretCustomer.Core.DTOs.User;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(int id);
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<IEnumerable<UserDto>> GetByRoleAsync(UserRole role);
    Task<IEnumerable<UserDto>> GetByBranchAsync(int branchId);
    Task<IEnumerable<UserDto>> GetActiveUsersAsync();
    Task<UserDto> CreateAsync(CreateUserDto createUserDto);
    Task<UserDto> UpdateAsync(int id, UpdateUserDto updateUserDto);
    Task DeleteAsync(int id);
    Task<bool> ChangePasswordAsync(int userId, string newPassword);
    Task<bool> AdminChangePasswordAsync(int userId, string newPassword);

    // Branch assignment
    Task<UserDto> AssignToBranchAsync(int userId, int branchId);
    Task<UserDto> RemoveFromBranchAsync(int userId);
    Task<IEnumerable<UserDto>> AssignMultipleToBranchAsync(List<int> userIds, int branchId);
}
