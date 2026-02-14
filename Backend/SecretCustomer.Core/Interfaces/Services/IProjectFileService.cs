using SecretCustomer.Core.DTOs.Project;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IProjectFileService
{
    Task<List<ProjectFileDto>> GetByProjectAsync(int projectId);
    Task<bool> ProjectExistsAsync(int projectId);
    Task<ProjectFileDto> SaveFileRecordAsync(int projectId, string storedFileName, string originalFileName, string filePath, long fileSize, string contentType, string? description, int? uploadedByUserId);
    Task<(string FilePath, string ContentType, string OriginalFileName, int ProjectId)?> GetDownloadInfoAsync(int id);
    Task<bool> IsUserAssignedToProjectAsync(int userId, int projectId);
    Task<(bool Success, string? FilePath)> DeleteAsync(int id);
    Task<bool> UpdateDescriptionAsync(int id, string? description);
}
