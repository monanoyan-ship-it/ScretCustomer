namespace SecretCustomer.Core.DTOs.EvaluationImport;

public class CustomerPersonnelSearchDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? RoleName { get; set; }
}

public class UserSearchDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? RoleName { get; set; }
}

public class ProjectSearchDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ChecklistId { get; set; }
}

public class CustomerSearchDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

public class ChecklistSearchDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
