using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Checklist;

public class CreateChecklistDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public bool IsScored { get; set; } = true;

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
}
