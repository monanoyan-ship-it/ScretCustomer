using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.Organization;

/// <summary>
/// Vekalet görüntüleme DTO'su
/// </summary>
public class DelegationDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }

    public int DelegatorUserId { get; set; }
    public string DelegatorUserName { get; set; } = string.Empty;
    public int? DelegatorOrganizationUnitId { get; set; }
    public string? DelegatorOrganizationUnitName { get; set; }

    public int DelegateeUserId { get; set; }
    public string DelegateeUserName { get; set; } = string.Empty;
    public int? DelegateeOrganizationUnitId { get; set; }
    public string? DelegateeOrganizationUnitName { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public DelegationType DelegationType { get; set; }
    public string DelegationTypeName { get; set; } = string.Empty;
    public string? PermissionScope { get; set; }
    public DelegationReason Reason { get; set; }
    public string ReasonName { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }

    public DateTime CreatedAt { get; set; }

    // Computed properties
    public bool IsExpired => EndDate.HasValue && EndDate.Value < DateTime.UtcNow;
    public bool IsCurrent => IsActive && IsApproved && !IsExpired && StartDate <= DateTime.UtcNow;
    public string Status => IsCurrent ? "Aktif" : (IsExpired ? "Süresi Dolmuş" : (!IsApproved ? "Onay Bekliyor" : "Pasif"));
}

/// <summary>
/// Vekalet oluşturma DTO'su
/// </summary>
public class CreateDelegationDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int DelegatorUserId { get; set; }
    public int? DelegatorOrganizationUnitId { get; set; }
    public int DelegateeUserId { get; set; }
    public int? DelegateeOrganizationUnitId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DelegationType DelegationType { get; set; } = DelegationType.Full;
    public string? PermissionScope { get; set; }
    public DelegationReason Reason { get; set; } = DelegationReason.Other;
    public string? Notes { get; set; }
}

/// <summary>
/// Vekalet güncelleme DTO'su
/// </summary>
public class UpdateDelegationDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int DelegateeUserId { get; set; }
    public int? DelegateeOrganizationUnitId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public DelegationType DelegationType { get; set; }
    public string? PermissionScope { get; set; }
    public DelegationReason Reason { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Vekalet onaylama DTO'su
/// </summary>
public class ApproveDelegationDto
{
    public bool IsApproved { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Vekalet filtre DTO'su
/// </summary>
public class DelegationFilterDto
{
    public string? SearchTerm { get; set; }
    public int? DelegatorUserId { get; set; }
    public int? DelegateeUserId { get; set; }
    public DelegationType? DelegationType { get; set; }
    public DelegationReason? Reason { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsApproved { get; set; }
    public bool? IsCurrent { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Sayfalanmış vekalet sonucu
/// </summary>
public class PagedDelegationResult
{
    public List<DelegationDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
