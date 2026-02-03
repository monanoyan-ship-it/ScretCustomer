namespace SecretCustomer.Core.DTOs.Dashboard;

public class ChartExportRequestDto
{
    public string ChartType { get; set; } = string.Empty;
    public string ChartImage { get; set; } = string.Empty;
    public string? ChartTitle { get; set; }
    public string DataJson { get; set; } = string.Empty;
}
