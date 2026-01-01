namespace SecretCustomer.Core.DTOs.SystemSetting;

public class SystemSettingDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ValueType { get; set; } = "string";
    public string? Category { get; set; }
}

public class CreateSystemSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ValueType { get; set; } = "string";
    public string? Category { get; set; }
}

public class UpdateSystemSettingDto
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
