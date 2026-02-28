namespace SecretCustomer.Core.DTOs.EvaluationImport;

public class ResolveUnmatchedItemDto
{
    public int? EntityId { get; set; }
    public int ActionId { get; set; }
    // Person creation
    public string? NewFirstName { get; set; }
    public string? NewLastName { get; set; }
    // Project creation
    public string? NewProjectName { get; set; }
    public int? NewProjectChecklistId { get; set; }
}
