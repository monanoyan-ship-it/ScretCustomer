using SecretCustomer.Core.DTOs.SavedFilter;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ISavedFilterService
{
    Task<IEnumerable<SavedFilterDto>> GetByUserAndPageAsync(int userId, string pageName);
    Task<SavedFilterDto?> GetByIdAsync(int id);
    Task<SavedFilterDto> CreateAsync(int userId, CreateSavedFilterDto dto);
    Task DeleteAsync(int id, int userId);
    Task SetDefaultAsync(int userId, string pageName, int filterId);
}
