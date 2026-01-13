using SecretCustomer.Core.DTOs.Dealer;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IDealerService
{
    Task<PagedDealerResult> GetAllAsync(DealerFilterDto filter);
    Task<DealerDto?> GetByIdAsync(int id);
    Task<DealerDto> CreateAsync(CreateDealerDto dto);
    Task<DealerDto?> UpdateAsync(int id, UpdateDealerDto dto);
    Task<bool> DeleteAsync(int id);
    Task<List<DealerDto>> GetByCustomerAsync(int customerId);
    Task<string> GenerateCodeAsync(int customerId);
}
