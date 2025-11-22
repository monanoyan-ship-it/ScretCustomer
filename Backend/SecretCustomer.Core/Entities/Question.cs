using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

public class Question : BaseEntity
{
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;

    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public int Order { get; set; }
    public int Points { get; set; } = 0; // Her sorunun puanı
    public bool AllowNA { get; set; } = false; // N/A seçeneğine izin verir mi?
    public bool IsRequired { get; set; } = true;

    // JSON olarak saklanacak seçenekler (MultipleChoice için)
    public string? OptionsJson { get; set; }

    // Navigation properties
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
