using SecretCustomer.Core.DTOs.Announcement;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IAnnouncementService
{
    Task<List<AnnouncementDto>> GetAllAsync(string userRole, bool includeExpired);
    Task<List<AnnouncementSummaryDto>> GetForDashboardAsync(string userRole, int count);
    Task<AnnouncementDto?> GetByIdAsync(int id);
    Task<List<AnnouncementDto>> GetAllAdminAsync();
    Task<(AnnouncementDto Announcement, List<int> NotifyUserIds)> CreateAsync(CreateAnnouncementDto dto, int? userId);
    Task<(bool Success, string Message)> UpdateAsync(int id, CreateAnnouncementDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}
