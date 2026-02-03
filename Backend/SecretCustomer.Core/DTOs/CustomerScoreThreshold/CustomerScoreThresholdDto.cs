namespace SecretCustomer.Core.DTOs.CustomerScoreThreshold;

public class CustomerScoreThresholdDto
{
    public int Id { get; set; }
    public int ProjectTypeId { get; set; }
    public string ProjectTypeName { get; set; } = string.Empty;
    public string ProjectTypeIcon { get; set; } = string.Empty;
    public string ProjectTypeColor { get; set; } = string.Empty;
    public decimal SuccessThreshold { get; set; } = 80;
    public decimal WarningThreshold { get; set; } = 60;
    public bool IsActive { get; set; } = true;
}

public class SaveCustomerScoreThresholdDto
{
    public int ProjectTypeId { get; set; }
    public decimal SuccessThreshold { get; set; } = 80;
    public decimal WarningThreshold { get; set; } = 60;
    public bool IsActive { get; set; } = true;
}

public class BulkSaveCustomerScoreThresholdDto
{
    public List<SaveCustomerScoreThresholdDto> Settings { get; set; } = new();
}
