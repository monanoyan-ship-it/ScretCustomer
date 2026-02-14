using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IEmailTemplateService
{
    Task<List<object>> GetAllAsync(int? templateTypeId, int? customerId);
    Task<object?> GetByIdAsync(int id);
    Task<(bool Success, int? Id, string Message)> CreateAsync(string name, string? description, string subject, string body, int templateTypeId, bool isActive, bool isDefault, int? customerId);
    Task<(bool Success, string Message)> UpdateAsync(int id, string name, string? description, string subject, string body, int templateTypeId, bool isActive, bool isDefault, int? customerId);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<(bool Success, int? Id, string Message)> DuplicateAsync(int id);
    Task<EmailTemplate?> FindByIdAsync(int id);
    Task<List<object>> GetTestProjectsAsync();
    Task<(bool Found, List<object> Personnel)> GetTestPersonnelAsync(int projectId);
    Task<Project?> GetTestProjectWithDetailsAsync(int projectId);
    Task<CustomerPersonnel?> GetTestPersonnelWithDetailsAsync(int personnelId);
}
