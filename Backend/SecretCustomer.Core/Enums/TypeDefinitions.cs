namespace SecretCustomer.Core.Enums;

/// <summary>
/// Code-based tip tanimlari icin base class
/// Database yerine kod icerisinde tanimlanan tipler icin kullanilir
/// </summary>
public class TypeItem
{
    public int Id { get; }
    public string SystemName { get; }
    public string NameResourceKey { get; }
    public string? Description { get; }
    public string? Icon { get; }
    public string? CssClass { get; }
    public int DisplayOrder { get; }
    public bool IsDefault { get; }
    public bool IsActive { get; }
    public bool IsSystem { get; }

    public TypeItem(
        int id,
        string systemName,
        string nameResourceKey,
        string? description = null,
        string? icon = null,
        string? cssClass = null,
        int displayOrder = 0,
        bool isDefault = false,
        bool isActive = true,
        bool isSystem = true)
    {
        Id = id;
        SystemName = systemName;
        NameResourceKey = nameResourceKey;
        Description = description;
        Icon = icon;
        CssClass = cssClass;
        DisplayOrder = displayOrder;
        IsDefault = isDefault;
        IsActive = isActive;
        IsSystem = isSystem;
    }
}

// ============================================================
// USER ROLES (Sistem Kullanicilari)
// ============================================================
public static class UserRoles
{
    public static readonly TypeItem Admin = new(1, "Admin", "Role.Admin", "Sistem yoneticisi", "bi-shield-fill-check", "bg-danger", 1);
    public static readonly TypeItem QualitySpecialist = new(2, "QualitySpecialist", "Role.QualitySpecialist", "Kalite uzmani", "bi-clipboard-check", "bg-primary", 2, isDefault: true);
    public static readonly TypeItem FieldWorker = new(3, "FieldWorker", "Role.FieldWorker", "Saha calisani", "bi-person-badge", "bg-success", 3);

    public static IEnumerable<TypeItem> All => new[] { Admin, QualitySpecialist, FieldWorker };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Admin = 1;
        public const int QualitySpecialist = 2;
        public const int FieldWorker = 3;
    }
}

// ============================================================
// CUSTOMER PERSONNEL ROLES (Musteri Personeli)
// ============================================================
public static class CustomerPersonnelRoles
{
    public static readonly TypeItem Manager = new(1, "CustomerManager", "CustomerPersonnel.Role.Manager", "Musteri yoneticisi - tum raporlari gorebilir", "bi-person-fill-gear", "bg-info", 1, isDefault: true);
    public static readonly TypeItem Supervisor = new(2, "CustomerSupervisor", "CustomerPersonnel.Role.Supervisor", "Musteri supervizoru - kendi takiminin raporlarini gorebilir", "bi-person-lines-fill", "bg-warning text-dark", 2);
    public static readonly TypeItem Operator = new(3, "CustomerOperator", "CustomerPersonnel.Role.Operator", "Musteri operatoru - sadece kendi raporlarini gorebilir", "bi-person", "bg-secondary", 3);

    public static IEnumerable<TypeItem> All => new[] { Manager, Supervisor, Operator };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Manager = 1;
        public const int Supervisor = 2;
        public const int Operator = 3;
    }
}

// ============================================================
// ASSIGNMENT STATUSES (Atama Durumlari)
// ============================================================
public static class AssignmentStatuses
{
    public static readonly TypeItem Pending = new(1, "Pending", "AssignmentStatus.Pending", "Beklemede", "bi-hourglass-split", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem InProgress = new(2, "InProgress", "AssignmentStatus.InProgress", "Devam ediyor", "bi-play-circle", "bg-primary", 2);
    public static readonly TypeItem Completed = new(3, "Completed", "AssignmentStatus.Completed", "Tamamlandi", "bi-check-circle", "bg-success", 3);
    public static readonly TypeItem Cancelled = new(4, "Cancelled", "AssignmentStatus.Cancelled", "Iptal edildi", "bi-x-circle", "bg-danger", 4);

    public static IEnumerable<TypeItem> All => new[] { Pending, InProgress, Completed, Cancelled };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Pending = 1;
        public const int InProgress = 2;
        public const int Completed = 3;
        public const int Cancelled = 4;
    }
}

// ============================================================
// EVALUATION STATUSES (Degerlendirme Durumlari)
// ============================================================
public static class EvaluationStatuses
{
    public static readonly TypeItem Draft = new(1, "Draft", "EvaluationStatus.Draft", "Taslak", "bi-file-earmark", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Submitted = new(2, "Submitted", "EvaluationStatus.Submitted", "Gonderildi", "bi-send", "bg-info", 2);
    public static readonly TypeItem Approved = new(3, "Approved", "EvaluationStatus.Approved", "Onaylandi", "bi-check-circle", "bg-success", 3);
    public static readonly TypeItem Rejected = new(4, "Rejected", "EvaluationStatus.Rejected", "Reddedildi", "bi-x-circle", "bg-danger", 4);
    public static readonly TypeItem NeedsRevision = new(5, "NeedsRevision", "EvaluationStatus.NeedsRevision", "Revizyon gerekli", "bi-pencil-square", "bg-warning text-dark", 5);

    public static IEnumerable<TypeItem> All => new[] { Draft, Submitted, Approved, Rejected, NeedsRevision };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Draft = 1;
        public const int Submitted = 2;
        public const int Approved = 3;
        public const int Rejected = 4;
        public const int NeedsRevision = 5;
    }
}

// ============================================================
// PROJECT STATUSES (Proje Durumlari)
// ============================================================
public static class ProjectStatuses
{
    public static readonly TypeItem Planning = new(1, "Planning", "ProjectStatus.Planning", "Planlama asamasinda", "bi-pencil-square", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Active = new(2, "Active", "ProjectStatus.Active", "Aktif", "bi-play-circle", "bg-success", 2);
    public static readonly TypeItem OnHold = new(3, "OnHold", "ProjectStatus.OnHold", "Beklemede", "bi-pause-circle", "bg-warning text-dark", 3);
    public static readonly TypeItem Completed = new(4, "Completed", "ProjectStatus.Completed", "Tamamlandi", "bi-check-circle", "bg-info", 4);
    public static readonly TypeItem Cancelled = new(5, "Cancelled", "ProjectStatus.Cancelled", "Iptal edildi", "bi-x-circle", "bg-danger", 5);

    public static IEnumerable<TypeItem> All => new[] { Planning, Active, OnHold, Completed, Cancelled };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Planning = 1;
        public const int Active = 2;
        public const int OnHold = 3;
        public const int Completed = 4;
        public const int Cancelled = 5;
    }
}

// ============================================================
// QUESTION SCORING TYPES (Soru Puanlama Tipleri)
// ============================================================
public static class QuestionScoringTypes
{
    public static readonly TypeItem Scored = new(1, "Scored", "ScoringType.Scored", "Puanli soru", "bi-123", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem YesNo = new(2, "YesNo", "ScoringType.YesNo", "Evet/Hayir sorusu", "bi-toggle-on", "bg-info", 2);
    public static readonly TypeItem Informational = new(3, "Informational", "ScoringType.Informational", "Bilgi sorusu (puansiz)", "bi-info-circle", "bg-secondary", 3);

    public static IEnumerable<TypeItem> All => new[] { Scored, YesNo, Informational };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Scored = 1;
        public const int YesNo = 2;
        public const int Informational = 3;
    }
}

// ============================================================
// PENALTY TYPES (Ceza Tipleri)
// ============================================================
public static class PenaltyTypes
{
    public static readonly TypeItem None = new(0, "None", "PenaltyType.None", "Ceza yok", "bi-check", "bg-success", 0, isDefault: true);
    public static readonly TypeItem Minor = new(1, "Minor", "PenaltyType.Minor", "Kucuk ceza", "bi-exclamation", "bg-warning text-dark", 1);
    public static readonly TypeItem Major = new(2, "Major", "PenaltyType.Major", "Buyuk ceza", "bi-exclamation-triangle", "bg-danger", 2);
    public static readonly TypeItem Critical = new(3, "Critical", "PenaltyType.Critical", "Kritik ceza", "bi-x-octagon", "bg-dark", 3);

    public static IEnumerable<TypeItem> All => new[] { None, Minor, Major, Critical };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int None = 0;
        public const int Minor = 1;
        public const int Major = 2;
        public const int Critical = 3;
    }
}

// ============================================================
// PERIOD STATUSES (Donem Durumlari)
// ============================================================
public static class PeriodStatuses
{
    public static readonly TypeItem Open = new(1, "Open", "PeriodStatus.Open", "Acik", "bi-door-open", "bg-success", 1, isDefault: true);
    public static readonly TypeItem Closed = new(2, "Closed", "PeriodStatus.Closed", "Kapali", "bi-door-closed", "bg-secondary", 2);

    public static IEnumerable<TypeItem> All => new[] { Open, Closed };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Open = 1;
        public const int Closed = 2;
    }
}

// ============================================================
// APPROVAL STATUSES (Onay Durumlari)
// ============================================================
public static class ApprovalStatuses
{
    public static readonly TypeItem Pending = new(1, "Pending", "ApprovalStatus.Pending", "Onay bekliyor", "bi-hourglass-split", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem Approved = new(2, "Approved", "ApprovalStatus.Approved", "Onaylandi", "bi-check-circle", "bg-success", 2);
    public static readonly TypeItem Rejected = new(3, "Rejected", "ApprovalStatus.Rejected", "Reddedildi", "bi-x-circle", "bg-danger", 3);

    public static IEnumerable<TypeItem> All => new[] { Pending, Approved, Rejected };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Pending = 1;
        public const int Approved = 2;
        public const int Rejected = 3;
    }
}
