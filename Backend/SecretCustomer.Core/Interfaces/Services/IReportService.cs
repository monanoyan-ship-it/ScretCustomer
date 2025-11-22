using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IReportService
{
    Task<EvaluationReportDto?> GetEvaluationReportAsync(Guid evaluationId);
    Task<BranchPerformanceReportDto?> GetBranchPerformanceReportAsync(Guid branchId, ReportFilterDto? filter = null);
    Task<ProjectReportDto?> GetProjectReportAsync(Guid projectId);
    Task<IEnumerable<EvaluationReportDto>> GetEvaluationReportsAsync(ReportFilterDto filter);
}
