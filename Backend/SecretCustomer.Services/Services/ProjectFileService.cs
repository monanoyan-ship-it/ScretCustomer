using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Project;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class ProjectFileService : IProjectFileService
{
    private readonly ApplicationDbContext _context;

    public ProjectFileService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProjectFileDto>> GetByProjectAsync(int projectId)
    {
        var files = await _context.ProjectFiles
            .Where(f => f.ProjectId == projectId)
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => new ProjectFileDto
            {
                Id = f.Id,
                ProjectId = f.ProjectId,
                FileName = f.FileName,
                OriginalFileName = f.OriginalFileName,
                ContentType = f.ContentType,
                FileSize = f.FileSize,
                UploadedAt = f.UploadedAt,
                UploadedById = f.UploadedById,
                UploadedByName = f.UploadedBy != null
                    ? f.UploadedBy.FirstName + " " + f.UploadedBy.LastName
                    : null,
                Description = f.Description
            })
            .ToListAsync();

        return files;
    }

    public async Task<bool> ProjectExistsAsync(int projectId)
    {
        return await _context.Projects.AnyAsync(p => p.Id == projectId);
    }

    public async Task<ProjectFileDto> SaveFileRecordAsync(int projectId, string storedFileName, string originalFileName, string filePath, long fileSize, string contentType, string? description, int? uploadedByUserId)
    {
        var projectFile = new ProjectFile
        {
            ProjectId = projectId,
            FileName = storedFileName,
            OriginalFileName = originalFileName,
            FilePath = filePath,
            FileSize = fileSize,
            ContentType = contentType,
            Description = description,
            UploadedAt = TurkeyTime.Now,
            UploadedById = uploadedByUserId
        };

        _context.ProjectFiles.Add(projectFile);
        await _context.SaveChangesAsync();

        // Kullanici adini al
        string? uploadedByName = null;
        if (uploadedByUserId.HasValue)
        {
            var user = await _context.Users.FindAsync(uploadedByUserId.Value);
            if (user != null)
            {
                uploadedByName = $"{user.FirstName} {user.LastName}";
            }
        }

        return new ProjectFileDto
        {
            Id = projectFile.Id,
            ProjectId = projectFile.ProjectId,
            FileName = projectFile.FileName,
            OriginalFileName = projectFile.OriginalFileName,
            ContentType = projectFile.ContentType,
            FileSize = projectFile.FileSize,
            UploadedAt = projectFile.UploadedAt,
            UploadedById = projectFile.UploadedById,
            UploadedByName = uploadedByName,
            Description = projectFile.Description
        };
    }

    public async Task<(string FilePath, string ContentType, string OriginalFileName, int ProjectId)?> GetDownloadInfoAsync(int id)
    {
        var projectFile = await _context.ProjectFiles
            .Include(f => f.Project)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (projectFile == null)
            return null;

        return (projectFile.FilePath, projectFile.ContentType, projectFile.OriginalFileName, projectFile.ProjectId);
    }

    public async Task<bool> IsUserAssignedToProjectAsync(int userId, int projectId)
    {
        return await _context.Set<Assignment>()
            .AnyAsync(a => a.ProjectId == projectId &&
                          (a.AssignedUserId == userId || a.AssignedCustomerPersonnelId == userId));
    }

    public async Task<(bool Success, string? FilePath)> DeleteAsync(int id)
    {
        var projectFile = await _context.ProjectFiles.FindAsync(id);
        if (projectFile == null)
            return (false, null);

        var filePath = projectFile.FilePath;

        // Veritabanindan sil (soft delete)
        projectFile.IsDeleted = true;
        projectFile.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        return (true, filePath);
    }

    public async Task<bool> UpdateDescriptionAsync(int id, string? description)
    {
        var projectFile = await _context.ProjectFiles.FindAsync(id);
        if (projectFile == null)
            return false;

        projectFile.Description = description;
        projectFile.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        return true;
    }
}
