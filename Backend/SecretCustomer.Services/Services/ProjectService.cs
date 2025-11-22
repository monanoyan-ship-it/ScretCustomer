using SecretCustomer.Core.DTOs.Project;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IChecklistRepository _checklistRepository;

    public ProjectService(
        IProjectRepository projectRepository,
        IChecklistRepository checklistRepository)
    {
        _projectRepository = projectRepository;
        _checklistRepository = checklistRepository;
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id)
    {
        var project = await _projectRepository.GetByIdAsync(id, includeDetails: true);
        return project == null ? null : MapToDto(project);
    }

    public async Task<IEnumerable<ProjectDto>> GetAllAsync(bool includeInactive = false)
    {
        var projects = await _projectRepository.GetAllAsync(includeInactive);
        return projects.Select(MapToDto);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto)
    {
        // Validate checklist exists
        var checklistExists = await _checklistRepository.ExistsAsync(dto.ChecklistId);
        if (!checklistExists)
            throw new KeyNotFoundException($"Checklist with ID {dto.ChecklistId} not found");

        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description,
            ChecklistId = dto.ChecklistId,
            AssignmentType = Enum.Parse<AssignmentType>(dto.AssignmentType),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = true
        };

        var created = await _projectRepository.CreateAsync(project);
        return MapToDto(created);
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, CreateProjectDto dto)
    {
        var existing = await _projectRepository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Project with ID {id} not found");

        // Validate checklist exists
        var checklistExists = await _checklistRepository.ExistsAsync(dto.ChecklistId);
        if (!checklistExists)
            throw new KeyNotFoundException($"Checklist with ID {dto.ChecklistId} not found");

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.ChecklistId = dto.ChecklistId;
        existing.AssignmentType = Enum.Parse<AssignmentType>(dto.AssignmentType);
        existing.StartDate = dto.StartDate;
        existing.EndDate = dto.EndDate;

        var updated = await _projectRepository.UpdateAsync(existing);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _projectRepository.DeleteAsync(id);
    }

    public async Task<ProjectDto> CloseProjectAsync(Guid id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
            throw new KeyNotFoundException($"Project with ID {id} not found");

        project.IsActive = false;
        var updated = await _projectRepository.UpdateAsync(project);
        return MapToDto(updated);
    }

    private ProjectDto MapToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            ChecklistId = project.ChecklistId,
            ChecklistName = project.Checklist?.Name ?? "",
            AssignmentType = project.AssignmentType.ToString(),
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            IsActive = project.IsActive,
            CreatedAt = project.CreatedAt,
            TotalAssignments = project.Assignments?.Count ?? 0,
            CompletedAssignments = project.Assignments?.Count(a => a.IsCompleted) ?? 0
        };
    }
}
