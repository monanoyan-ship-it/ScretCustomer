using SecretCustomer.Core.DTOs.Approval;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IApprovalService
{
    Task<(List<ApprovalListDto> Items, int TotalCount)> GetApprovalsAsync(ApprovalFilterDto filter);
    Task<List<ApprovalListDto>> GetMyPendingApprovalsAsync(int userId);
    Task<ApprovalDto?> GetApprovalAsync(int id);
    Task<(int Id, string ReferenceNumber)> CreateApprovalAsync(Approval approval);
    Task<Approval?> FindByIdAsync(int id);
    Task SaveChangesAsync();
    Task<string> GenerateApprovalNumberAsync();
    Task<ApprovalSummaryDto> GetSummaryAsync();
}
