namespace SecretCustomer.Core.DTOs.SavedFilter;

public class SavedFilterDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PageName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FilterData { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSavedFilterDto
{
    public string PageName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FilterData { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
}
