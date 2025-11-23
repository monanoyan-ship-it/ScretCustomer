using SecretCustomer.Core.DTOs.Dashboard;

namespace SecretCustomer.API.ViewModels;

public class DashboardViewModel
{
    public int TotalEvaluations { get; set; }
    public decimal AverageScore { get; set; }
    public decimal PercentageChange { get; set; }
    public List<TopBranchDto> TopBranches { get; set; } = new();
    public List<TopBranchDto> BottomBranches { get; set; } = new();
}
