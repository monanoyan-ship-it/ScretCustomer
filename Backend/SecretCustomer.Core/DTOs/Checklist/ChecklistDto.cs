using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.Checklist;

public class ChecklistDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsScored { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }

    // Yeni alanlar
    public string ChecklistType { get; set; } = "CallPerformance";
    public string ChecklistTypeName { get; set; } = "Çağrı Performans";
    public string ScoringMethod { get; set; } = "Maximum";
    public string ScoringMethodName { get; set; } = "Maksimum";
    public decimal MaxTotalPoints { get; set; } = 100;
    public string? Code { get; set; }
    public string? TemplateName { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int? EstimatedDurationMinutes { get; set; }

    public List<SectionDto> Sections { get; set; } = new();
}

public class SectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }

    // Yeni alanlar
    public string GroupType { get; set; } = "Scored";
    public string GroupTypeName { get; set; } = "Puanlı";
    public decimal WeightPoints { get; set; } = 1;
    public decimal MaxPoints { get; set; } = 100;
    public bool IsActive { get; set; } = true;

    public List<QuestionDto> Questions { get; set; } = new();
}

public class QuestionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Order { get; set; }
    public int Points { get; set; }
    public bool AllowNA { get; set; }
    public bool IsRequired { get; set; }
    public List<QuestionOptionDto>? Options { get; set; }

    // Yeni alanlar
    public string ScoringType { get; set; } = "Scored";
    public string ScoringTypeName { get; set; } = "Puanlı";
    public decimal WeightPoints { get; set; } = 1;
    public decimal MaxPoints { get; set; } = 100;
    public string PenaltyType { get; set; } = "None";
    public string PenaltyTypeName { get; set; } = "Yok";
    public string? RecommendedNote { get; set; }
    public string? HelpText { get; set; }
    public decimal PenaltyValue { get; set; } = 0;
}

public class QuestionOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Points { get; set; }
}
