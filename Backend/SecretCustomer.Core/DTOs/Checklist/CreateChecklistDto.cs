using System.ComponentModel.DataAnnotations;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.Checklist;

public class CreateChecklistDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public bool IsScored { get; set; } = true;

    // Yeni alanlar
    public string ChecklistType { get; set; } = "CallPerformance";
    public string ScoringMethod { get; set; } = "Maximum";
    public decimal MaxTotalPoints { get; set; } = 100;

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(100)]
    public string? TemplateName { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int? EstimatedDurationMinutes { get; set; }

    public List<CreateSectionDto> Sections { get; set; } = new();
}

public class CreateSectionDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int Order { get; set; }

    // Yeni alanlar
    public string GroupType { get; set; } = "Scored";
    public decimal WeightPoints { get; set; } = 1;
    public decimal MaxPoints { get; set; } = 5; // Likert 0-5 için
    public bool IsActive { get; set; } = true;

    public List<CreateQuestionDto> Questions { get; set; } = new();
}

public class CreateQuestionDto
{
    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty; // MultipleChoice, Likert, Star, Text

    public int Order { get; set; }

    [Range(0, 1000)]
    public int Points { get; set; }

    public bool AllowNA { get; set; } = false;

    public bool IsRequired { get; set; } = true;

    public List<QuestionOptionDto>? Options { get; set; }

    // Yeni alanlar
    public string ScoringType { get; set; } = "Scored";
    public decimal WeightPoints { get; set; } = 1;
    public decimal MaxPoints { get; set; } = 5; // Likert 0-5 için
    public string PenaltyType { get; set; } = "None";

    [MaxLength(2000)]
    public string? RecommendedNote { get; set; }

    [MaxLength(1000)]
    public string? HelpText { get; set; }

    public decimal PenaltyValue { get; set; } = 0;
}
