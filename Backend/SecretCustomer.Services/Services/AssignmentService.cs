using SecretCustomer.Core.DTOs.Assignment;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IChecklistRepository _checklistRepository;

    public AssignmentService(
        IAssignmentRepository assignmentRepository,
        IProjectRepository projectRepository,
        IChecklistRepository checklistRepository)
    {
        _assignmentRepository = assignmentRepository;
        _projectRepository = projectRepository;
        _checklistRepository = checklistRepository;
    }

    public async Task<AssignmentDto?> GetByIdAsync(Guid id)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(id, includeDetails: true);
        return assignment == null ? null : MapToDto(assignment);
    }

    public async Task<IEnumerable<AssignmentDto>> GetAllAsync()
    {
        var assignments = await _assignmentRepository.GetAllAsync();
        return assignments.Select(MapToDto);
    }

    public async Task<AssignmentDto?> GetByUniqueLinkAsync(string uniqueLink)
    {
        var assignment = await _assignmentRepository.GetByUniqueLinkAsync(uniqueLink, includeDetails: true);
        return assignment == null ? null : MapToDto(assignment);
    }

    public async Task<IEnumerable<AssignmentDto>> GetByProjectIdAsync(Guid projectId)
    {
        var assignments = await _assignmentRepository.GetByProjectIdAsync(projectId);
        return assignments.Select(MapToDto);
    }

    public async Task<IEnumerable<AssignmentDto>> GetByUserIdAsync(Guid userId)
    {
        var assignments = await _assignmentRepository.GetByUserIdAsync(userId);
        return assignments.Select(MapToDto);
    }

    public async Task<IEnumerable<AssignmentDto>> GetByBranchIdAsync(Guid branchId)
    {
        var assignments = await _assignmentRepository.GetByBranchIdAsync(branchId);
        return assignments.Select(MapToDto);
    }

    public async Task<AssignmentDto> CreateAsync(CreateAssignmentDto dto)
    {
        await ValidateAsync(dto);

        // External müşteri için daha önce atama yapılmış mı kontrol et
        if (!string.IsNullOrEmpty(dto.ExternalEmail))
        {
            var exists = await _assignmentRepository.ExistsByEmailAsync(dto.ProjectId, dto.ExternalEmail);
            if (exists)
                throw new InvalidOperationException($"An active assignment already exists for {dto.ExternalEmail} in this project");
        }

        var assignment = new Assignment
        {
            ProjectId = dto.ProjectId,
            ChecklistId = dto.ChecklistId,
            BranchId = dto.BranchId,
            AssignedUserId = dto.AssignedUserId,
            AssignedFieldWorkerId = dto.AssignedFieldWorkerId,
            ExternalEmail = dto.ExternalEmail,
            ExternalName = dto.ExternalName,
            UniqueLink = Guid.NewGuid().ToString(),
            DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc),
            IsCompleted = false
        };

        var created = await _assignmentRepository.CreateAsync(assignment);
        return MapToDto(created);
    }

    public async Task<IEnumerable<AssignmentDto>> CreateBulkAsync(BulkAssignmentDto dto)
    {
        // Validate project and checklist
        var projectExists = await _projectRepository.ExistsAsync(dto.ProjectId);
        if (!projectExists)
            throw new KeyNotFoundException($"Project with ID {dto.ProjectId} not found");

        var checklistExists = await _checklistRepository.ExistsAsync(dto.ChecklistId);
        if (!checklistExists)
            throw new KeyNotFoundException($"Checklist with ID {dto.ChecklistId} not found");

        var assignments = dto.Assignments.Select(a => new Assignment
        {
            ProjectId = dto.ProjectId,
            ChecklistId = dto.ChecklistId,
            BranchId = a.BranchId,
            AssignedUserId = a.AssignedUserId,
            ExternalEmail = a.ExternalEmail,
            ExternalName = a.ExternalName,
            UniqueLink = Guid.NewGuid().ToString(),
            DueDate = a.DueDate,
            IsCompleted = false
        }).ToList();

        var created = await _assignmentRepository.CreateBulkAsync(assignments);
        return created.Select(MapToDto);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _assignmentRepository.DeleteAsync(id);
    }

    public async Task UpdateAsync(Guid id, UpdateAssignmentDto dto)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(id);
        if (assignment == null)
            throw new KeyNotFoundException($"Assignment with ID {id} not found");

        assignment.ProjectId = dto.ProjectId;
        assignment.ChecklistId = dto.ChecklistId;
        assignment.BranchId = dto.BranchId;
        assignment.AssignedUserId = dto.AssignedUserId;
        assignment.AssignedFieldWorkerId = dto.AssignedFieldWorkerId;
        assignment.ExternalEmail = dto.ExternalEmail;
        assignment.ExternalName = dto.ExternalName;
        assignment.DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc);

        await _assignmentRepository.UpdateAsync(assignment);
    }

    public async Task<AssignmentDto> CompleteAssignmentAsync(Guid id)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(id);
        if (assignment == null)
            throw new KeyNotFoundException($"Assignment with ID {id} not found");

        assignment.IsCompleted = true;
        assignment.CompletedAt = DateTime.UtcNow;

        var updated = await _assignmentRepository.UpdateAsync(assignment);
        return MapToDto(updated);
    }

    private async Task ValidateAsync(CreateAssignmentDto dto)
    {
        var projectExists = await _projectRepository.ExistsAsync(dto.ProjectId);
        if (!projectExists)
            throw new KeyNotFoundException($"Project with ID {dto.ProjectId} not found");

        var checklistExists = await _checklistRepository.ExistsAsync(dto.ChecklistId);
        if (!checklistExists)
            throw new KeyNotFoundException($"Checklist with ID {dto.ChecklistId} not found");

        // Either internal user OR external email must be provided
        if (!dto.AssignedUserId.HasValue && string.IsNullOrEmpty(dto.ExternalEmail))
            throw new InvalidOperationException("Either AssignedUserId or ExternalEmail must be provided");
    }

    private AssignmentDto MapToDto(Assignment assignment)
    {
        return new AssignmentDto
        {
            Id = assignment.Id,
            ProjectId = assignment.ProjectId,
            ProjectName = assignment.Project?.Name ?? "",
            ChecklistId = assignment.ChecklistId,
            ChecklistName = assignment.Checklist?.Name ?? "",
            BranchId = assignment.BranchId,
            BranchName = assignment.Branch?.Name,
            AssignedUserId = assignment.AssignedUserId,
            AssignedUserName = assignment.AssignedUser != null
                ? $"{assignment.AssignedUser.FirstName} {assignment.AssignedUser.LastName}"
                : null,
            ExternalEmail = assignment.ExternalEmail,
            ExternalName = assignment.ExternalName,
            UniqueLink = assignment.UniqueLink,
            DueDate = assignment.DueDate,
            IsCompleted = assignment.IsCompleted,
            CompletedAt = assignment.CompletedAt,
            CreatedAt = assignment.CreatedAt
        };
    }
}
