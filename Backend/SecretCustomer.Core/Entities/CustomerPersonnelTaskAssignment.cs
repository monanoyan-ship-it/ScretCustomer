using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Core.Entities;

public class CustomerPersonnelTaskAssignment : BaseEntity
{
    public int PersonnelId { get; set; }
    public CustomerPersonnel Personnel { get; set; } = null!;

    public int TaskListId { get; set; }
    public CustomerTaskList TaskList { get; set; } = null!;

    /// <summary>
    /// Personelin bu görevdeki rolü
    /// </summary>
    public int AssignmentRoleId { get; set; }

    /// <summary>
    /// Atama tarihi
    /// </summary>
    public DateTime AssignedDate { get; set; } = TurkeyTime.Now;

    /// <summary>
    /// Tamamlanma tarihi
    /// </summary>
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// Görev notları
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Atama aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;
}