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

    // Helper: DateTime'ı UTC'ye çevir (PostgreSQL için gerekli)
    private static DateTime? ToUtc(DateTime? dateTime)
    {
        if (!dateTime.HasValue) return null;
        if (dateTime.Value.Kind == DateTimeKind.Utc) return dateTime;
        return DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
    }

    public async Task<ChecklistDto?> GetByIdAsync(int id)
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
            // Yeni alanlar
            ChecklistType = Enum.TryParse<ChecklistType>(dto.ChecklistType, out var clType) ? clType : ChecklistType.CallPerformance,
            ScoringMethod = Enum.TryParse<ScoringMethod>(dto.ScoringMethod, out var scMethod) ? scMethod : ScoringMethod.Maximum,
            MaxTotalPoints = dto.MaxTotalPoints,
            Code = dto.Code,
            TemplateName = dto.TemplateName,
            ValidFrom = ToUtc(dto.ValidFrom),
            ValidUntil = ToUtc(dto.ValidUntil),
            EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
            Sections = dto.Sections.Select(s => new Section
            {
                Name = s.Name,
                Description = s.Description,
                Order = s.Order,
                // Yeni alanlar
                GroupType = Enum.TryParse<ScoringType>(s.GroupType, out var gType) ? gType : ScoringType.Scored,
                WeightPoints = s.WeightPoints,
                MaxPoints = s.MaxPoints,
                IsActive = s.IsActive,
                Questions = s.Questions.Select(q => new Question
                {
                    Text = q.Text,
                    Type = Enum.Parse<QuestionType>(q.Type),
                    Order = q.Order,
                    Points = q.Points,
                    AllowNA = q.AllowNA,
                    IsRequired = q.IsRequired,
                    OptionsJson = q.Options != null ? JsonSerializer.Serialize(q.Options) : null,
                    // Yeni alanlar
                    ScoringType = Enum.TryParse<ScoringType>(q.ScoringType, out var sType) ? sType : ScoringType.Scored,
                    WeightPoints = q.WeightPoints,
                    MaxPoints = q.MaxPoints,
                    PenaltyType = Enum.TryParse<PenaltyType>(q.PenaltyType, out var pType) ? pType : PenaltyType.None,
                    RecommendedNote = q.RecommendedNote,
                    HelpText = q.HelpText,
                    PenaltyValue = q.PenaltyValue
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
        // Yeni alanlar
        existing.ChecklistType = Enum.TryParse<ChecklistType>(dto.ChecklistType, out var clType) ? clType : ChecklistType.CallPerformance;
        existing.ScoringMethod = Enum.TryParse<ScoringMethod>(dto.ScoringMethod, out var scMethod) ? scMethod : ScoringMethod.Maximum;
        existing.MaxTotalPoints = dto.MaxTotalPoints;
        existing.Code = dto.Code;
        existing.TemplateName = dto.TemplateName;
        existing.ValidFrom = ToUtc(dto.ValidFrom);
        existing.ValidUntil = ToUtc(dto.ValidUntil);
        existing.EstimatedDurationMinutes = dto.EstimatedDurationMinutes;

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
                    // Yeni alanlar
                    section.GroupType = Enum.TryParse<ScoringType>(sectionDto.GroupType, out var gType) ? gType : ScoringType.Scored;
                    section.WeightPoints = sectionDto.WeightPoints;
                    section.MaxPoints = sectionDto.MaxPoints;
                    section.IsActive = sectionDto.IsActive;

                    UpdateQuestions(section, sectionDto.Questions);
                }
            }
            else
            {
                // Add new section - 0 ile EF Core "Added" olarak algılar
                existing.Sections.Add(new Section
                {
                    Id = 0,
                    Name = sectionDto.Name,
                    Description = sectionDto.Description,
                    Order = sectionDto.Order,
                    // Yeni alanlar
                    GroupType = Enum.TryParse<ScoringType>(sectionDto.GroupType, out var gType) ? gType : ScoringType.Scored,
                    WeightPoints = sectionDto.WeightPoints,
                    MaxPoints = sectionDto.MaxPoints,
                    IsActive = sectionDto.IsActive,
                    Questions = sectionDto.Questions.Select(q => new Question
                    {
                        Id = 0,
                        Text = q.Text,
                        Type = Enum.Parse<QuestionType>(q.Type),
                        Order = q.Order,
                        Points = q.Points,
                        AllowNA = q.AllowNA,
                        IsRequired = q.IsRequired,
                        OptionsJson = q.Options != null ? JsonSerializer.Serialize(q.Options) : null,
                        // Yeni alanlar
                        ScoringType = Enum.TryParse<ScoringType>(q.ScoringType, out var sType) ? sType : ScoringType.Scored,
                        WeightPoints = q.WeightPoints,
                        MaxPoints = q.MaxPoints,
                        PenaltyType = Enum.TryParse<PenaltyType>(q.PenaltyType, out var pType) ? pType : PenaltyType.None,
                        RecommendedNote = q.RecommendedNote,
                        HelpText = q.HelpText,
                        PenaltyValue = q.PenaltyValue
                    }).ToList()
                });
            }
        }

        var updated = await _checklistRepository.UpdateAsync(existing);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _checklistRepository.DeleteAsync(id);
    }

    public async Task<ChecklistDto> CloneChecklistAsync(int id, string newName)
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
                    question.ScoringType = Enum.TryParse<ScoringType>(questionDto.ScoringType, out var sType) ? sType : ScoringType.Scored;
                    question.WeightPoints = questionDto.WeightPoints;
                    question.MaxPoints = questionDto.MaxPoints;
                    question.PenaltyType = Enum.TryParse<PenaltyType>(questionDto.PenaltyType, out var pType) ? pType : PenaltyType.None;
                    question.RecommendedNote = questionDto.RecommendedNote;
                    question.HelpText = questionDto.HelpText;
                    question.PenaltyValue = questionDto.PenaltyValue;
                }
                // else: ID var ama bulunamadı - atla, hata loglanabilir
            }
            else
            {
                // Add new question (ID null) - 0 ile EF Core "Added" olarak algılar
                section.Questions.Add(new Question
                {
                    Id = 0,
                    Text = questionDto.Text,
                    Type = Enum.Parse<QuestionType>(questionDto.Type),
                    Order = questionDto.Order,
                    Points = questionDto.Points,
                    AllowNA = questionDto.AllowNA,
                    IsRequired = questionDto.IsRequired,
                    OptionsJson = questionDto.Options != null ? JsonSerializer.Serialize(questionDto.Options) : null,
                    ScoringType = Enum.TryParse<ScoringType>(questionDto.ScoringType, out var sType) ? sType : ScoringType.Scored,
                    WeightPoints = questionDto.WeightPoints,
                    MaxPoints = questionDto.MaxPoints,
                    PenaltyType = Enum.TryParse<PenaltyType>(questionDto.PenaltyType, out var pType) ? pType : PenaltyType.None,
                    RecommendedNote = questionDto.RecommendedNote,
                    HelpText = questionDto.HelpText,
                    PenaltyValue = questionDto.PenaltyValue
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
            // Yeni alanlar
            ChecklistType = checklist.ChecklistType.ToString(),
            ChecklistTypeName = GetChecklistTypeName(checklist.ChecklistType),
            ScoringMethod = checklist.ScoringMethod.ToString(),
            ScoringMethodName = GetScoringMethodName(checklist.ScoringMethod),
            MaxTotalPoints = checklist.MaxTotalPoints,
            Code = checklist.Code,
            TemplateName = checklist.TemplateName,
            ValidFrom = checklist.ValidFrom,
            ValidUntil = checklist.ValidUntil,
            EstimatedDurationMinutes = checklist.EstimatedDurationMinutes,
            Sections = checklist.Sections.OrderBy(s => s.Order).Select(s => new SectionDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Order = s.Order,
                // Yeni alanlar
                GroupType = s.GroupType.ToString(),
                GroupTypeName = GetScoringTypeName(s.GroupType),
                WeightPoints = s.WeightPoints,
                MaxPoints = s.MaxPoints,
                IsActive = s.IsActive,
                Questions = s.Questions.OrderBy(q => q.Order).Select(q => new QuestionDto
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = q.Type.ToString(),
                    Order = q.Order,
                    Points = q.Points,
                    AllowNA = q.AllowNA,
                    IsRequired = q.IsRequired,
                    Options = ParseQuestionOptions(q.OptionsJson),
                    // Yeni alanlar
                    ScoringType = q.ScoringType.ToString(),
                    ScoringTypeName = GetScoringTypeName(q.ScoringType),
                    WeightPoints = q.WeightPoints,
                    MaxPoints = q.MaxPoints,
                    PenaltyType = q.PenaltyType.ToString(),
                    PenaltyTypeName = GetPenaltyTypeName(q.PenaltyType),
                    RecommendedNote = q.RecommendedNote,
                    HelpText = q.HelpText,
                    PenaltyValue = q.PenaltyValue
                }).ToList()
            }).ToList()
        };
    }

    private static string GetChecklistTypeName(ChecklistType type) => type switch
    {
        ChecklistType.CallPerformance => "Çağrı Performans",
        ChecklistType.PhysicalAudit => "Fiziksel Denetim",
        ChecklistType.MysteryShopping => "Gizli Müşteri",
        ChecklistType.OnlineEvaluation => "Online Değerlendirme",
        ChecklistType.Survey => "Anket",
        _ => type.ToString()
    };

    private static string GetScoringMethodName(ScoringMethod method) => method switch
    {
        ScoringMethod.Maximum => "Maksimum",
        ScoringMethod.Average => "Ortalama",
        ScoringMethod.WeightedAverage => "Ağırlıklı Ortalama",
        ScoringMethod.Sum => "Toplam",
        _ => method.ToString()
    };

    private static string GetScoringTypeName(ScoringType type) => type switch
    {
        ScoringType.Scored => "Puanlı",
        ScoringType.Unscored => "Puansız",
        ScoringType.Penalty => "Cezalı",
        _ => type.ToString()
    };

    private static string GetPenaltyTypeName(PenaltyType type) => type switch
    {
        PenaltyType.None => "Yok",
        PenaltyType.YellowCard => "Sarı Kart",
        PenaltyType.RedCard => "Kırmızı Kart",
        _ => type.ToString()
    };
}
