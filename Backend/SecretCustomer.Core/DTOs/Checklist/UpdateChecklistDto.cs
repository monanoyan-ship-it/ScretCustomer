using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Checklist;

public class UpdateChecklistDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public bool IsScored { get; set; }

    public bool IsActive { get; set; }

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

    public List<UpdateSectionDto> Sections { get; set; } = new();
}

public class UpdateSectionDto
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int Order { get; set; }

    // Yeni alanlar
    public string GroupType { get; set; } = "Scored";
    public decimal WeightPoints { get; set; } = 1;
    public decimal MaxPoints { get; set; } = 100;
    public bool IsActive { get; set; } = true;

    public List<UpdateQuestionDto> Questions { get; set; } = new();
}

public class UpdateQuestionDto
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    public int Order { get; set; }

    [Range(0, 1000)]
    public int Points { get; set; }

    public bool AllowNA { get; set; }

    public bool IsRequired { get; set; }

    public List<QuestionOptionDto>? Options { get; set; }

    // Yeni alanlar
    public string ScoringType { get; set; } = "Scored";
    public decimal WeightPoints { get; set; } = 1;
    public decimal MaxPoints { get; set; } = 100;
    public string PenaltyType { get; set; } = "None";

    [MaxLength(2000)]
    public string? RecommendedNote { get; set; }

    [MaxLength(1000)]
    public string? HelpText { get; set; }

    public decimal PenaltyValue { get; set; } = 0;
}
