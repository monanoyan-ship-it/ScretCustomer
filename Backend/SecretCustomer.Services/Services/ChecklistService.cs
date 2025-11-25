using System.Text.Json;
using SecretCustomer.Core.DTOs.Checklist;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class ChecklistService : IChecklistService
{
    private readonly IChecklistRepository _checklistRepository;

    public ChecklistService(IChecklistRepository checklistRepository)
    {
        _checklistRepository = checklistRepository;
    }

    public async Task<ChecklistDto?> GetByIdAsync(Guid id)
    {
        var checklist = await _checklistRepository.GetByIdAsync(id, includeDetails: true);
        return checklist == null ? null : MapToDto(checklist);
    }

    public async Task<IEnumerable<ChecklistDto>> GetAllAsync(bool includeInactive = false)
    {
        var checklists = await _checklistRepository.GetAllAsync(includeInactive);
        return checklists.Select(MapToDto);
    }

    public async Task<ChecklistDto> CreateAsync(CreateChecklistDto dto)
    {
        var checklist = new Checklist
        {
            Name = dto.Name,
            Description = dto.Description,
            IsScored = dto.IsScored,
            IsActive = true,
            Version = 1,
            Sections = dto.Sections.Select(s => new Section
            {
                Name = s.Name,
                Description = s.Description,
                Order = s.Order,
                Questions = s.Questions.Select(q => new Question
                {
                    Text = q.Text,
                    Type = Enum.Parse<QuestionType>(q.Type),
                    Order = q.Order,
                    Points = q.Points,
                    AllowNA = q.AllowNA,
                    IsRequired = q.IsRequired,
                    OptionsJson = q.Options != null ? JsonSerializer.Serialize(q.Options) : null
                }).ToList()
            }).ToList()
        };

        var created = await _checklistRepository.CreateAsync(checklist);
        return MapToDto(created);
    }

    public async Task<ChecklistDto> UpdateAsync(UpdateChecklistDto dto)
    {
        var existing = await _checklistRepository.GetByIdAsync(dto.Id, includeDetails: true);
        if (existing == null)
            throw new KeyNotFoundException($"Checklist with ID {dto.Id} not found");

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.IsScored = dto.IsScored;
        existing.IsActive = dto.IsActive;

        // Update sections
        var existingSectionIds = existing.Sections.Select(s => s.Id).ToHashSet();
        var dtoSectionIds = dto.Sections.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();

        // Remove deleted sections
        var sectionsToRemove = existing.Sections.Where(s => !dtoSectionIds.Contains(s.Id)).ToList();
        foreach (var section in sectionsToRemove)
        {
            section.IsDeleted = true;
        }

        // Update or add sections
        foreach (var sectionDto in dto.Sections)
        {
            if (sectionDto.Id.HasValue)
            {
                // Update existing section
                var section = existing.Sections.FirstOrDefault(s => s.Id == sectionDto.Id.Value);
                if (section != null)
                {
                    section.Name = sectionDto.Name;
                    section.Description = sectionDto.Description;
                    section.Order = sectionDto.Order;

                    UpdateQuestions(section, sectionDto.Questions);
                }
            }
            else
            {
                // Add new section
                existing.Sections.Add(new Section
                {
                    Name = sectionDto.Name,
                    Description = sectionDto.Description,
                    Order = sectionDto.Order,
                    Questions = sectionDto.Questions.Select(q => new Question
                    {
                        Text = q.Text,
                        Type = Enum.Parse<QuestionType>(q.Type),
                        Order = q.Order,
                        Points = q.Points,
                        AllowNA = q.AllowNA,
                        IsRequired = q.IsRequired,
                        OptionsJson = q.Options != null ? JsonSerializer.Serialize(q.Options) : null
                    }).ToList()
                });
            }
        }

        var updated = await _checklistRepository.UpdateAsync(existing);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _checklistRepository.DeleteAsync(id);
    }

    public async Task<ChecklistDto> CloneChecklistAsync(Guid id, string newName)
    {
        var original = await _checklistRepository.GetByIdAsync(id, includeDetails: true);
        if (original == null)
            throw new KeyNotFoundException($"Checklist with ID {id} not found");

        var versionCount = await _checklistRepository.GetVersionCountAsync(newName);

        var cloned = new Checklist
        {
            Name = $"{newName} v{versionCount + 1}",
            Description = original.Description,
            IsScored = original.IsScored,
            IsActive = true,
            Version = versionCount + 1,
            Sections = original.Sections.Select(s => new Section
            {
                Name = s.Name,
                Description = s.Description,
                Order = s.Order,
                Questions = s.Questions.Select(q => new Question
                {
                    Text = q.Text,
                    Type = q.Type,
                    Order = q.Order,
                    Points = q.Points,
                    AllowNA = q.AllowNA,
                    IsRequired = q.IsRequired,
                    OptionsJson = q.OptionsJson
                }).ToList()
            }).ToList()
        };

        var created = await _checklistRepository.CreateAsync(cloned);
        return MapToDto(created);
    }

    private void UpdateQuestions(Section section, List<UpdateQuestionDto> questionDtos)
    {
        var existingQuestionIds = section.Questions.Select(q => q.Id).ToHashSet();
        var dtoQuestionIds = questionDtos.Where(q => q.Id.HasValue).Select(q => q.Id!.Value).ToHashSet();

        // Remove deleted questions
        var questionsToRemove = section.Questions.Where(q => !dtoQuestionIds.Contains(q.Id)).ToList();
        foreach (var question in questionsToRemove)
        {
            question.IsDeleted = true;
        }

        // Update or add questions
        foreach (var questionDto in questionDtos)
        {
            if (questionDto.Id.HasValue)
            {
                // Update existing question
                var question = section.Questions.FirstOrDefault(q => q.Id == questionDto.Id.Value);
                if (question != null)
                {
                    question.Text = questionDto.Text;
                    question.Type = Enum.Parse<QuestionType>(questionDto.Type);
                    question.Order = questionDto.Order;
                    question.Points = questionDto.Points;
                    question.AllowNA = questionDto.AllowNA;
                    question.IsRequired = questionDto.IsRequired;
                    question.OptionsJson = questionDto.Options != null ? JsonSerializer.Serialize(questionDto.Options) : null;
                }
            }
            else
            {
                // Add new question
                section.Questions.Add(new Question
                {
                    Text = questionDto.Text,
                    Type = Enum.Parse<QuestionType>(questionDto.Type),
                    Order = questionDto.Order,
                    Points = questionDto.Points,
                    AllowNA = questionDto.AllowNA,
                    IsRequired = questionDto.IsRequired,
                    OptionsJson = questionDto.Options != null ? JsonSerializer.Serialize(questionDto.Options) : null
                });
            }
        }
    }

    private List<QuestionOptionDto>? ParseQuestionOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
            return null;

        try
        {
            // Try to deserialize as List<QuestionOptionDto>
            return JsonSerializer.Deserialize<List<QuestionOptionDto>>(optionsJson);
        }
        catch (JsonException)
        {
            // If that fails, it might be a simple string or comma-separated values
            // Return null for now - frontend will handle it
            return null;
        }
    }

    private ChecklistDto MapToDto(Checklist checklist)
    {
        return new ChecklistDto
        {
            Id = checklist.Id,
            Name = checklist.Name,
            Description = checklist.Description,
            IsScored = checklist.IsScored,
            IsActive = checklist.IsActive,
            Version = checklist.Version,
            CreatedAt = checklist.CreatedAt,
            Sections = checklist.Sections.OrderBy(s => s.Order).Select(s => new SectionDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Order = s.Order,
                Questions = s.Questions.OrderBy(q => q.Order).Select(q => new QuestionDto
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = q.Type.ToString(),
                    Order = q.Order,
                    Points = q.Points,
                    AllowNA = q.AllowNA,
                    IsRequired = q.IsRequired,
                    Options = ParseQuestionOptions(q.OptionsJson)
                }).ToList()
            }).ToList()
        };
    }
}
