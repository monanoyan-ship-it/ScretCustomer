using SecretCustomer.Core.DTOs.CustomerScoreThreshold;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ICustomerScoreThresholdService
{
    Task<List<CustomerScoreThresholdDto>> GetAllAsync(int customerId);
    Task<CustomerScoreThresholdDto> SaveAsync(int customerId, SaveCustomerScoreThresholdDto dto, string? userId = null);
    Task<List<CustomerScoreThresholdDto>> BulkSaveAsync(int customerId, BulkSaveCustomerScoreThresholdDto dto, string? userId = null);
    Task<CustomerScoreThresholdDto?> GetByCustomerAndProjectTypeAsync(int customerId, int projectTypeId);
}
