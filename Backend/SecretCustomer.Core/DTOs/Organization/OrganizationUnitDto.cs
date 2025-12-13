using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.Organization;

/// <summary>
/// Organizasyon birimi görüntüleme DTO'su
/// </summary>
public class OrganizationUnitDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public OrganizationUnitType UnitType { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int UserCount { get; set; }
    public int ChildCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrganizationUnitDto> Children { get; set; } = new();
}

/// <summary>
/// Organizasyon birimi oluşturma DTO'su
/// </summary>
public class CreateOrganizationUnitDto
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public OrganizationUnitType UnitType { get; set; } = OrganizationUnitType.Department;
    public int Order { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public Guid? ParentId { get; set; }
    public Guid? ManagerId { get; set; }
    public Guid? BranchId { get; set; }
}

/// <summary>
/// Organizasyon birimi güncelleme DTO'su
/// </summary>
public class UpdateOrganizationUnitDto
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public OrganizationUnitType UnitType { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? ManagerId { get; set; }
    public Guid? BranchId { get; set; }
}

/// <summary>
/// Organizasyon birimi filtre DTO'su
/// </summary>
public class OrganizationUnitFilterDto
{
    public string? SearchTerm { get; set; }
    public OrganizationUnitType? UnitType { get; set; }
    public bool? IsActive { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? BranchId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Organizasyon hiyerarşi düğümü (ağaç yapısı için)
/// </summary>
public class OrganizationTreeNodeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public OrganizationUnitType UnitType { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsActive { get; set; }
    public string? ManagerName { get; set; }
    public int UserCount { get; set; }
    public List<OrganizationTreeNodeDto> Children { get; set; } = new();
}
