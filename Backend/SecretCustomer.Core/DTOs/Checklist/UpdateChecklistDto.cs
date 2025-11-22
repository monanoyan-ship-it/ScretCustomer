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
}
