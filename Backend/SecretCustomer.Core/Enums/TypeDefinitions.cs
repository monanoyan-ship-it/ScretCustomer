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
// ASSIGNMENT TYPES (Atama Tipleri)
// ============================================================
public static class AssignmentTypes
{
    public static readonly TypeItem InternalBranch = new(1, "InternalBranch", "AssignmentType.InternalBranch", "Ic sube degerlendirmesi", "bi-building", "bg-primary", 1);
    public static readonly TypeItem InternalUser = new(2, "InternalUser", "AssignmentType.InternalUser", "Ic kullanici degerlendirmesi", "bi-person", "bg-info", 2, isDefault: true);
    public static readonly TypeItem ExternalCustomer = new(3, "ExternalCustomer", "AssignmentType.ExternalCustomer", "Dis musteri anketi", "bi-envelope", "bg-success", 3);
    public static readonly TypeItem CustomerPersonnel = new(4, "CustomerPersonnel", "AssignmentType.CustomerPersonnel", "Musteri personeli degerlendirmesi", "bi-person-badge", "bg-warning text-dark", 4);
    public static readonly TypeItem FieldWorker = new(5, "FieldWorker", "AssignmentType.FieldWorker", "Saha calisani degerlendirmesi", "bi-geo-alt", "bg-secondary", 5);

    public static IEnumerable<TypeItem> All => new[] { InternalBranch, InternalUser, ExternalCustomer, CustomerPersonnel, FieldWorker };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int InternalBranch = 1;
        public const int InternalUser = 2;
        public const int ExternalCustomer = 3;
        public const int CustomerPersonnel = 4;
        public const int FieldWorker = 5;
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
    public static readonly TypeItem Expired = new(4, "Expired", "AssignmentStatus.Expired", "Suresi dolmus", "bi-clock-history", "bg-secondary", 4);
    public static readonly TypeItem Cancelled = new(5, "Cancelled", "AssignmentStatus.Cancelled", "Iptal edildi", "bi-x-circle", "bg-danger", 5);

    public static IEnumerable<TypeItem> All => new[] { Pending, InProgress, Completed, Expired, Cancelled };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Pending = 1;
        public const int InProgress = 2;
        public const int Completed = 3;
        public const int Expired = 4;
        public const int Cancelled = 5;
    }
}

// ============================================================
// EVALUATION STATUSES (Degerlendirme Durumlari)
// ============================================================
public static class EvaluationStatuses
{
    public static readonly TypeItem Pending = new(1, "Pending", "EvaluationStatus.Pending", "Beklemede - Henuz baslanmadi", "bi-clock", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem InProgress = new(2, "InProgress", "EvaluationStatus.InProgress", "Devam ediyor - Degerlendirme yapiliyor", "bi-play-circle", "bg-primary", 2);
    public static readonly TypeItem Completed = new(3, "Completed", "EvaluationStatus.Completed", "Tamamlandi - Degerlendirme bitti", "bi-check-circle", "bg-success", 3);
    public static readonly TypeItem Draft = new(4, "Draft", "EvaluationStatus.Draft", "Taslak - Kaydedildi ama tamamlanmadi", "bi-file-earmark", "bg-warning text-dark", 4);
    public static readonly TypeItem Cancelled = new(5, "Cancelled", "EvaluationStatus.Cancelled", "Iptal edildi", "bi-x-circle", "bg-danger", 5);

    public static IEnumerable<TypeItem> All => new[] { Pending, InProgress, Completed, Draft, Cancelled };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Pending = 1;
        public const int InProgress = 2;
        public const int Completed = 3;
        public const int Draft = 4;
        public const int Cancelled = 5;
    }
}

// ============================================================
// PROJECT STATUSES (Proje Durumlari)
// ============================================================
public static class ProjectStatuses
{
    public static readonly TypeItem Draft = new(1, "Draft", "ProjectStatus.Draft", "Taslak - Henuz baslatilmadi", "bi-pencil-square", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Planned = new(2, "Planned", "ProjectStatus.Planned", "Planlanmis - Baslangic tarihi bekleniyor", "bi-calendar-event", "bg-info", 2);
    public static readonly TypeItem Active = new(3, "Active", "ProjectStatus.Active", "Aktif - Devam ediyor", "bi-play-circle", "bg-success", 3);
    public static readonly TypeItem Paused = new(4, "Paused", "ProjectStatus.Paused", "Duraklatildi - Gecici olarak durduruldu", "bi-pause-circle", "bg-warning text-dark", 4);
    public static readonly TypeItem Completed = new(5, "Completed", "ProjectStatus.Completed", "Tamamlandi - Basariyla bitti", "bi-check-circle", "bg-primary", 5);
    public static readonly TypeItem Cancelled = new(6, "Cancelled", "ProjectStatus.Cancelled", "Iptal Edildi", "bi-x-circle", "bg-danger", 6);

    public static IEnumerable<TypeItem> All => new[] { Draft, Planned, Active, Paused, Completed, Cancelled };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Draft = 1;
        public const int Planned = 2;
        public const int Active = 3;
        public const int Paused = 4;
        public const int Completed = 5;
        public const int Cancelled = 6;
    }
}

// ============================================================
// PROJECT TYPES (Proje Tipleri)
// ============================================================
public static class ProjectTypes
{
    public static readonly TypeItem MysteryShopping = new(1, "MysteryShopping", "ProjectType.MysteryShopping", "Gizli Musteri", "bi-incognito", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem CallAuditing = new(2, "CallAuditing", "ProjectType.CallAuditing", "Cagri Denetleme", "bi-telephone", "bg-info", 2);
    public static readonly TypeItem PhysicalAudit = new(3, "PhysicalAudit", "ProjectType.PhysicalAudit", "Fiziksel Denetim", "bi-building-check", "bg-success", 3);
    public static readonly TypeItem OnlineSurvey = new(4, "OnlineSurvey", "ProjectType.OnlineSurvey", "Online Anket", "bi-globe", "bg-secondary", 4);
    public static readonly TypeItem CustomerSatisfaction = new(5, "CustomerSatisfaction", "ProjectType.CustomerSatisfaction", "Musteri Memnuniyeti", "bi-emoji-smile", "bg-warning text-dark", 5);
    public static readonly TypeItem TrainingEvaluation = new(6, "TrainingEvaluation", "ProjectType.TrainingEvaluation", "Egitim Degerlendirmesi", "bi-mortarboard", "bg-purple", 6);
    public static readonly TypeItem QualityControl = new(7, "QualityControl", "ProjectType.QualityControl", "Kalite Kontrol", "bi-patch-check", "bg-danger", 7);

    public static IEnumerable<TypeItem> All => new[] { MysteryShopping, CallAuditing, PhysicalAudit, OnlineSurvey, CustomerSatisfaction, TrainingEvaluation, QualityControl };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int MysteryShopping = 1;
        public const int CallAuditing = 2;
        public const int PhysicalAudit = 3;
        public const int OnlineSurvey = 4;
        public const int CustomerSatisfaction = 5;
        public const int TrainingEvaluation = 6;
        public const int QualityControl = 7;
    }
}

// ============================================================
// QUESTION TYPES (Soru Tipleri)
// ============================================================
public static class QuestionTypes
{
    public static readonly TypeItem MultipleChoice = new(1, "MultipleChoice", "QuestionType.MultipleChoice", "Coktan secmeli", "bi-ui-radios", "bg-primary", 1);
    public static readonly TypeItem Likert = new(2, "Likert", "QuestionType.Likert", "Likert olcegi", "bi-sliders", "bg-info", 2, isDefault: true);
    public static readonly TypeItem Star = new(3, "Star", "QuestionType.Star", "Yildiz derecelendirme", "bi-star", "bg-warning text-dark", 3);
    public static readonly TypeItem Text = new(4, "Text", "QuestionType.Text", "Acik uclu metin", "bi-fonts", "bg-secondary", 4);
    public static readonly TypeItem YesNo = new(5, "YesNo", "QuestionType.YesNo", "Evet/Hayir", "bi-toggle-on", "bg-success", 5);
    public static readonly TypeItem Rating = new(6, "Rating", "QuestionType.Rating", "Puan derecelendirme", "bi-speedometer2", "bg-danger", 6);

    public static IEnumerable<TypeItem> All => new[] { MultipleChoice, Likert, Star, Text, YesNo, Rating };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int MultipleChoice = 1;
        public const int Likert = 2;
        public const int Star = 3;
        public const int Text = 4;
        public const int YesNo = 5;
        public const int Rating = 6;
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
// SCORING METHODS (Puanlama Yontemleri)
// ============================================================
public static class ScoringMethods
{
    public static readonly TypeItem Maximum = new(1, "Maximum", "ScoringMethod.Maximum", "Maksimum puan uzerinden hesaplama", "bi-arrow-up-circle", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Average = new(2, "Average", "ScoringMethod.Average", "Ortalama puan hesaplama", "bi-calculator", "bg-info", 2);
    public static readonly TypeItem WeightedAverage = new(3, "WeightedAverage", "ScoringMethod.WeightedAverage", "Agirlikli ortalama", "bi-graph-up", "bg-warning text-dark", 3);
    public static readonly TypeItem Sum = new(4, "Sum", "ScoringMethod.Sum", "Toplam puan", "bi-plus-circle", "bg-success", 4);

    public static IEnumerable<TypeItem> All => new[] { Maximum, Average, WeightedAverage, Sum };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Maximum = 1;
        public const int Average = 2;
        public const int WeightedAverage = 3;
        public const int Sum = 4;
    }
}

// ============================================================
// SCORING TYPES (Puanlama Tipleri)
// ============================================================
public static class ScoringTypes
{
    public static readonly TypeItem Scored = new(1, "Scored", "ScoringType.Scored", "Puanli - Normal puanlama yapilir", "bi-123", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Unscored = new(2, "Unscored", "ScoringType.Unscored", "Puansiz - Puan hesaplanmaz", "bi-dash-circle", "bg-secondary", 2);
    public static readonly TypeItem Penalty = new(3, "Penalty", "ScoringType.Penalty", "Cezali - Sari/Kirmizi kart uygulanir", "bi-exclamation-triangle", "bg-danger", 3);

    public static IEnumerable<TypeItem> All => new[] { Scored, Unscored, Penalty };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Scored = 1;
        public const int Unscored = 2;
        public const int Penalty = 3;
    }
}

// ============================================================
// PENALTY TYPES (Ceza Tipleri)
// ============================================================
public static class PenaltyTypes
{
    public static readonly TypeItem None = new(0, "None", "PenaltyType.None", "Ceza yok", "bi-check-circle", "bg-success", 1, isDefault: true);
    public static readonly TypeItem YellowCard = new(1, "YellowCard", "PenaltyType.YellowCard", "Sari Kart - Uyari", "bi-card-heading", "bg-warning text-dark", 2);
    public static readonly TypeItem RedCard = new(2, "RedCard", "PenaltyType.RedCard", "Kirmizi Kart - Kritik hata", "bi-card-heading", "bg-danger", 3);

    public static IEnumerable<TypeItem> All => new[] { None, YellowCard, RedCard };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int None = 0;
        public const int YellowCard = 1;
        public const int RedCard = 2;
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
// TASK ASSIGNMENT ROLES (Gorev Atama Rolleri)
// ============================================================
public static class TaskAssignmentRoles
{
    public static readonly TypeItem Owner = new(1, "Owner", "TaskAssignmentRole.Owner", "Gorev sahibi - Gorevi yurutur", "bi-person-fill", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Assistant = new(2, "Assistant", "TaskAssignmentRole.Assistant", "Gorev yardimcisi - Destege katkida bulunur", "bi-person", "bg-info", 2);
    public static readonly TypeItem Observer = new(3, "Observer", "TaskAssignmentRole.Observer", "Gozlemci - Sadece takip eder", "bi-eye", "bg-secondary", 3);
    public static readonly TypeItem Approver = new(4, "Approver", "TaskAssignmentRole.Approver", "Onaylayici - Gorevi onaylar", "bi-check-circle", "bg-success", 4);

    public static IEnumerable<TypeItem> All => new[] { Owner, Assistant, Observer, Approver };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Owner = 1;
        public const int Assistant = 2;
        public const int Observer = 3;
        public const int Approver = 4;
    }
}

// ============================================================
// TASK PRIORITIES (Gorev Oncelikleri)
// ============================================================
public static class TaskPriorities
{
    public static readonly TypeItem Low = new(1, "Low", "TaskPriority.Low", "Dusuk oncelik", "bi-flag", "bg-secondary", 1);
    public static readonly TypeItem Medium = new(2, "Medium", "TaskPriority.Medium", "Orta oncelik", "bi-flag-fill", "bg-info", 2, isDefault: true);
    public static readonly TypeItem High = new(3, "High", "TaskPriority.High", "Yuksek oncelik", "bi-exclamation-triangle", "bg-warning text-dark", 3);
    public static readonly TypeItem Critical = new(4, "Critical", "TaskPriority.Critical", "Kritik oncelik", "bi-exclamation-circle-fill", "bg-danger", 4);

    public static IEnumerable<TypeItem> All => new[] { Low, Medium, High, Critical };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Low = 1;
        public const int Medium = 2;
        public const int High = 3;
        public const int Critical = 4;
    }
}

// ============================================================
// TASK STATUSES (Gorev Durumlari)
// ============================================================
public static class TaskStatuses
{
    public static readonly TypeItem NotStarted = new(1, "NotStarted", "TaskStatus.NotStarted", "Baslamadi", "bi-hourglass", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem InProgress = new(2, "InProgress", "TaskStatus.InProgress", "Devam ediyor", "bi-play-circle", "bg-primary", 2);
    public static readonly TypeItem Completed = new(3, "Completed", "TaskStatus.Completed", "Tamamlandi", "bi-check-circle", "bg-success", 3);
    public static readonly TypeItem Cancelled = new(4, "Cancelled", "TaskStatus.Cancelled", "Iptal edildi", "bi-x-circle", "bg-danger", 4);

    public static IEnumerable<TypeItem> All => new[] { NotStarted, InProgress, Completed, Cancelled };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int NotStarted = 1;
        public const int InProgress = 2;
        public const int Completed = 3;
        public const int Cancelled = 4;
    }
}

// ============================================================
// APPROVAL TYPES (Onay Turleri)
// ============================================================
public static class ApprovalTypes
{
    public static readonly TypeItem Evaluation = new(0, "Evaluation", "ApprovalType.Evaluation", "Degerlendirme onayi", "bi-clipboard-check", "bg-primary", 0);
    public static readonly TypeItem Assignment = new(1, "Assignment", "ApprovalType.Assignment", "Atama onayi", "bi-person-check", "bg-info", 1);
    public static readonly TypeItem Project = new(2, "Project", "ApprovalType.Project", "Proje onayi", "bi-folder-check", "bg-success", 2);
    public static readonly TypeItem Meeting = new(3, "Meeting", "ApprovalType.Meeting", "Toplanti onayi", "bi-calendar-check", "bg-warning text-dark", 3);
    public static readonly TypeItem Training = new(4, "Training", "ApprovalType.Training", "Egitim onayi", "bi-mortarboard", "bg-purple", 4);
    public static readonly TypeItem Report = new(5, "Report", "ApprovalType.Report", "Rapor onayi", "bi-file-earmark-check", "bg-secondary", 5);
    public static readonly TypeItem Delegation = new(6, "Delegation", "ApprovalType.Delegation", "Vekalet onayi", "bi-person-badge", "bg-dark", 6);
    public static readonly TypeItem General = new(7, "General", "ApprovalType.General", "Genel onay", "bi-check-circle", "bg-light text-dark", 7, isDefault: true);

    public static IEnumerable<TypeItem> All => new[] { Evaluation, Assignment, Project, Meeting, Training, Report, Delegation, General };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Evaluation = 0;
        public const int Assignment = 1;
        public const int Project = 2;
        public const int Meeting = 3;
        public const int Training = 4;
        public const int Report = 5;
        public const int Delegation = 6;
        public const int General = 7;
    }
}

// ============================================================
// APPROVAL STATUSES (Onay Durumlari)
// ============================================================
public static class ApprovalStatuses
{
    public static readonly TypeItem Pending = new(0, "Pending", "ApprovalStatus.Pending", "Beklemede", "bi-hourglass-split", "bg-warning text-dark", 0, isDefault: true);
    public static readonly TypeItem Approved = new(1, "Approved", "ApprovalStatus.Approved", "Onaylandi", "bi-check-circle", "bg-success", 1);
    public static readonly TypeItem Rejected = new(2, "Rejected", "ApprovalStatus.Rejected", "Reddedildi", "bi-x-circle", "bg-danger", 2);
    public static readonly TypeItem RevisionRequested = new(3, "RevisionRequested", "ApprovalStatus.RevisionRequested", "Revizyon istendi", "bi-pencil-square", "bg-info", 3);
    public static readonly TypeItem Cancelled = new(4, "Cancelled", "ApprovalStatus.Cancelled", "Iptal edildi", "bi-x-octagon", "bg-secondary", 4);
    public static readonly TypeItem Expired = new(5, "Expired", "ApprovalStatus.Expired", "Zaman asimi", "bi-clock-history", "bg-dark", 5);

    public static IEnumerable<TypeItem> All => new[] { Pending, Approved, Rejected, RevisionRequested, Cancelled, Expired };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Pending = 0;
        public const int Approved = 1;
        public const int Rejected = 2;
        public const int RevisionRequested = 3;
        public const int Cancelled = 4;
        public const int Expired = 5;
    }
}

// ============================================================
// NOTIFICATION TYPES (Bildirim Turleri)
// ============================================================
public static class NotificationTypes
{
    public static readonly TypeItem Info = new(0, "Info", "NotificationType.Info", "Bilgi", "bi-info-circle", "bg-info", 0, isDefault: true);
    public static readonly TypeItem Success = new(1, "Success", "NotificationType.Success", "Basari", "bi-check-circle", "bg-success", 1);
    public static readonly TypeItem Warning = new(2, "Warning", "NotificationType.Warning", "Uyari", "bi-exclamation-triangle", "bg-warning text-dark", 2);
    public static readonly TypeItem Error = new(3, "Error", "NotificationType.Error", "Hata", "bi-x-circle", "bg-danger", 3);
    public static readonly TypeItem ApprovalRequest = new(4, "ApprovalRequest", "NotificationType.ApprovalRequest", "Onay talebi", "bi-check2-square", "bg-primary", 4);
    public static readonly TypeItem Assignment = new(5, "Assignment", "NotificationType.Assignment", "Atama bildirimi", "bi-person-plus", "bg-secondary", 5);
    public static readonly TypeItem MeetingInvite = new(6, "MeetingInvite", "NotificationType.MeetingInvite", "Toplanti daveti", "bi-calendar-event", "bg-purple", 6);
    public static readonly TypeItem TrainingInvite = new(7, "TrainingInvite", "NotificationType.TrainingInvite", "Egitim daveti", "bi-mortarboard", "bg-teal", 7);
    public static readonly TypeItem Reminder = new(8, "Reminder", "NotificationType.Reminder", "Hatirlatma", "bi-bell", "bg-orange", 8);
    public static readonly TypeItem System = new(9, "System", "NotificationType.System", "Sistem bildirimi", "bi-gear", "bg-dark", 9);

    public static IEnumerable<TypeItem> All => new[] { Info, Success, Warning, Error, ApprovalRequest, Assignment, MeetingInvite, TrainingInvite, Reminder, System };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Info = 0;
        public const int Success = 1;
        public const int Warning = 2;
        public const int Error = 3;
        public const int ApprovalRequest = 4;
        public const int Assignment = 5;
        public const int MeetingInvite = 6;
        public const int TrainingInvite = 7;
        public const int Reminder = 8;
        public const int System = 9;
    }
}

// ============================================================
// NOTIFICATION CHANNELS (Bildirim Kanallari)
// ============================================================
public static class NotificationChannels
{
    public static readonly TypeItem InApp = new(0, "InApp", "NotificationChannel.InApp", "Uygulama ici", "bi-app", "bg-primary", 0, isDefault: true);
    public static readonly TypeItem Email = new(1, "Email", "NotificationChannel.Email", "E-posta", "bi-envelope", "bg-info", 1);
    public static readonly TypeItem Sms = new(2, "Sms", "NotificationChannel.Sms", "SMS", "bi-chat-dots", "bg-success", 2);
    public static readonly TypeItem Push = new(3, "Push", "NotificationChannel.Push", "Push bildirimi", "bi-phone-vibrate", "bg-warning text-dark", 3);

    public static IEnumerable<TypeItem> All => new[] { InApp, Email, Sms, Push };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int InApp = 0;
        public const int Email = 1;
        public const int Sms = 2;
        public const int Push = 3;
    }
}

// ============================================================
// NOTIFICATION PRIORITIES (Bildirim Oncelikleri)
// ============================================================
public static class NotificationPriorities
{
    public static readonly TypeItem Low = new(0, "Low", "NotificationPriority.Low", "Dusuk", "bi-arrow-down", "bg-secondary", 0);
    public static readonly TypeItem Normal = new(1, "Normal", "NotificationPriority.Normal", "Normal", "bi-dash", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem High = new(2, "High", "NotificationPriority.High", "Yuksek", "bi-arrow-up", "bg-warning text-dark", 2);
    public static readonly TypeItem Urgent = new(3, "Urgent", "NotificationPriority.Urgent", "Acil", "bi-exclamation-circle", "bg-danger", 3);

    public static IEnumerable<TypeItem> All => new[] { Low, Normal, High, Urgent };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Low = 0;
        public const int Normal = 1;
        public const int High = 2;
        public const int Urgent = 3;
    }
}

// ============================================================
// ANNOUNCEMENT TYPES (Duyuru Tipleri)
// ============================================================
public static class AnnouncementTypes
{
    public static readonly TypeItem Info = new(0, "Info", "AnnouncementType.Info", "Bilgi", "bi-info-circle", "bg-info", 0, isDefault: true);
    public static readonly TypeItem Warning = new(1, "Warning", "AnnouncementType.Warning", "Uyari", "bi-exclamation-triangle", "bg-warning text-dark", 1);
    public static readonly TypeItem Success = new(2, "Success", "AnnouncementType.Success", "Basari/Iyi Haber", "bi-check-circle", "bg-success", 2);
    public static readonly TypeItem Important = new(3, "Important", "AnnouncementType.Important", "Onemli/Acil", "bi-exclamation-circle", "bg-danger", 3);
    public static readonly TypeItem News = new(4, "News", "AnnouncementType.News", "Haber", "bi-newspaper", "bg-primary", 4);
    public static readonly TypeItem System = new(5, "System", "AnnouncementType.System", "Sistem", "bi-gear", "bg-secondary", 5);

    public static IEnumerable<TypeItem> All => new[] { Info, Warning, Success, Important, News, System };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Info = 0;
        public const int Warning = 1;
        public const int Success = 2;
        public const int Important = 3;
        public const int News = 4;
        public const int System = 5;
    }
}

// ============================================================
// CHECKLIST TYPES (Kontrol Listesi Tipleri)
// ============================================================
public static class ChecklistTypes
{
    public static readonly TypeItem CallPerformance = new(1, "CallPerformance", "ChecklistType.CallPerformance", "Cagri Performans degerlendirmesi", "bi-telephone", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem PhysicalAudit = new(2, "PhysicalAudit", "ChecklistType.PhysicalAudit", "Fiziksel denetim", "bi-building", "bg-info", 2);
    public static readonly TypeItem MysteryShopping = new(3, "MysteryShopping", "ChecklistType.MysteryShopping", "Gizli Musteri", "bi-incognito", "bg-warning text-dark", 3);
    public static readonly TypeItem OnlineEvaluation = new(4, "OnlineEvaluation", "ChecklistType.OnlineEvaluation", "Online degerlendirme", "bi-globe", "bg-success", 4);
    public static readonly TypeItem Survey = new(5, "Survey", "ChecklistType.Survey", "Genel anket", "bi-clipboard-data", "bg-secondary", 5);
    public static readonly TypeItem BankMysteryShopping = new(6, "BankMysteryShopping", "ChecklistType.BankMysteryShopping", "Banka Gizli Musteri", "bi-bank", "bg-dark", 6);

    public static IEnumerable<TypeItem> All => new[] { CallPerformance, PhysicalAudit, MysteryShopping, OnlineEvaluation, Survey, BankMysteryShopping };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int CallPerformance = 1;
        public const int PhysicalAudit = 2;
        public const int MysteryShopping = 3;
        public const int OnlineEvaluation = 4;
        public const int Survey = 5;
        public const int BankMysteryShopping = 6;
    }
}

// ============================================================
// CALL TYPES (Cagri Tipleri)
// ============================================================
public static class CallTypes
{
    public static readonly TypeItem Inbound = new(0, "Inbound", "CallType.Inbound", "Gelen cagri", "bi-telephone-inbound", "bg-success", 0, isDefault: true);
    public static readonly TypeItem Outbound = new(1, "Outbound", "CallType.Outbound", "Giden cagri", "bi-telephone-outbound", "bg-primary", 1);
    public static readonly TypeItem Internal = new(2, "Internal", "CallType.Internal", "Dahili cagri", "bi-telephone", "bg-secondary", 2);
    public static readonly TypeItem Conference = new(3, "Conference", "CallType.Conference", "Konferans cagrisi", "bi-people", "bg-info", 3);
    public static readonly TypeItem MysteryCall = new(4, "MysteryCall", "CallType.MysteryCall", "Gizli musteri cagrisi", "bi-incognito", "bg-warning text-dark", 4);

    public static IEnumerable<TypeItem> All => new[] { Inbound, Outbound, Internal, Conference, MysteryCall };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Inbound = 0;
        public const int Outbound = 1;
        public const int Internal = 2;
        public const int Conference = 3;
        public const int MysteryCall = 4;
    }
}

// ============================================================
// CALL STATUSES (Cagri Durumlari)
// ============================================================
public static class CallStatuses
{
    public static readonly TypeItem Scheduled = new(0, "Scheduled", "CallStatus.Scheduled", "Planlandi", "bi-calendar-event", "bg-info", 0);
    public static readonly TypeItem Pending = new(1, "Pending", "CallStatus.Pending", "Beklemede", "bi-hourglass-split", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem InProgress = new(2, "InProgress", "CallStatus.InProgress", "Devam ediyor", "bi-play-circle", "bg-primary", 2);
    public static readonly TypeItem Completed = new(3, "Completed", "CallStatus.Completed", "Tamamlandi", "bi-check-circle", "bg-success", 3);
    public static readonly TypeItem Missed = new(4, "Missed", "CallStatus.Missed", "Cevapsiz", "bi-telephone-x", "bg-secondary", 4);
    public static readonly TypeItem Cancelled = new(5, "Cancelled", "CallStatus.Cancelled", "Iptal edildi", "bi-x-circle", "bg-danger", 5);
    public static readonly TypeItem Failed = new(6, "Failed", "CallStatus.Failed", "Basarisiz", "bi-exclamation-circle", "bg-danger", 6);
    public static readonly TypeItem Evaluated = new(7, "Evaluated", "CallStatus.Evaluated", "Degerlendirildi", "bi-clipboard-check", "bg-success", 7);

    public static IEnumerable<TypeItem> All => new[] { Scheduled, Pending, InProgress, Completed, Missed, Cancelled, Failed, Evaluated };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Scheduled = 0;
        public const int Pending = 1;
        public const int InProgress = 2;
        public const int Completed = 3;
        public const int Missed = 4;
        public const int Cancelled = 5;
        public const int Failed = 6;
        public const int Evaluated = 7;
    }
}

// ============================================================
// CALL PRIORITIES (Cagri Oncelikleri)
// ============================================================
public static class CallPriorities
{
    public static readonly TypeItem Low = new(0, "Low", "CallPriority.Low", "Dusuk", "bi-arrow-down", "bg-secondary", 0);
    public static readonly TypeItem Normal = new(1, "Normal", "CallPriority.Normal", "Normal", "bi-dash", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem High = new(2, "High", "CallPriority.High", "Yuksek", "bi-arrow-up", "bg-warning text-dark", 2);
    public static readonly TypeItem Urgent = new(3, "Urgent", "CallPriority.Urgent", "Acil", "bi-exclamation-circle", "bg-danger", 3);

    public static IEnumerable<TypeItem> All => new[] { Low, Normal, High, Urgent };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Low = 0;
        public const int Normal = 1;
        public const int High = 2;
        public const int Urgent = 3;
    }
}

// ============================================================
// CUSTOMER PERMISSION TYPES (Musteri Personel Izin Tipleri)
// ============================================================
public static class CustomerPermissionTypes
{
    // Gorev Izinleri
    public static readonly TypeItem TaskView = new(1, "TaskView", "CustomerPermission.TaskView", "Gorevleri goruntuleyebilir", "bi-eye", "bg-info", 1);
    public static readonly TypeItem TaskCreate = new(2, "TaskCreate", "CustomerPermission.TaskCreate", "Gorev olusturabilir", "bi-plus-circle", "bg-success", 2);
    public static readonly TypeItem TaskEdit = new(3, "TaskEdit", "CustomerPermission.TaskEdit", "Gorevleri duzenleyebilir", "bi-pencil", "bg-warning text-dark", 3);
    public static readonly TypeItem TaskDelete = new(4, "TaskDelete", "CustomerPermission.TaskDelete", "Gorevleri silebilir", "bi-trash", "bg-danger", 4);
    public static readonly TypeItem TaskAssign = new(5, "TaskAssign", "CustomerPermission.TaskAssign", "Gorev atayabilir", "bi-person-plus", "bg-primary", 5);

    // Personel Izinleri
    public static readonly TypeItem PersonnelView = new(10, "PersonnelView", "CustomerPermission.PersonnelView", "Personeli goruntuleyebilir", "bi-people", "bg-info", 10);
    public static readonly TypeItem PersonnelCreate = new(11, "PersonnelCreate", "CustomerPermission.PersonnelCreate", "Personel olusturabilir", "bi-person-plus", "bg-success", 11);
    public static readonly TypeItem PersonnelEdit = new(12, "PersonnelEdit", "CustomerPermission.PersonnelEdit", "Personeli duzenleyebilir", "bi-person-gear", "bg-warning text-dark", 12);
    public static readonly TypeItem PersonnelDelete = new(13, "PersonnelDelete", "CustomerPermission.PersonnelDelete", "Personeli silebilir", "bi-person-x", "bg-danger", 13);

    // Rapor Izinleri
    public static readonly TypeItem ReportView = new(20, "ReportView", "CustomerPermission.ReportView", "Raporlari goruntuleyebilir", "bi-file-earmark-bar-graph", "bg-info", 20);
    public static readonly TypeItem ReportExport = new(21, "ReportExport", "CustomerPermission.ReportExport", "Raporlari disari aktarabilir", "bi-download", "bg-success", 21);
    public static readonly TypeItem ReportCreate = new(22, "ReportCreate", "CustomerPermission.ReportCreate", "Rapor olusturabilir", "bi-file-earmark-plus", "bg-primary", 22);

    // Sube Izinleri
    public static readonly TypeItem BranchView = new(30, "BranchView", "CustomerPermission.BranchView", "Subeleri goruntuleyebilir", "bi-building", "bg-info", 30);
    public static readonly TypeItem BranchManage = new(31, "BranchManage", "CustomerPermission.BranchManage", "Subeleri yonetebilir", "bi-building-gear", "bg-primary", 31);

    // Proje Izinleri
    public static readonly TypeItem ProjectView = new(40, "ProjectView", "CustomerPermission.ProjectView", "Projeleri goruntuleyebilir", "bi-folder", "bg-info", 40);
    public static readonly TypeItem ProjectManage = new(41, "ProjectManage", "CustomerPermission.ProjectManage", "Projeleri yonetebilir", "bi-folder-check", "bg-primary", 41);

    public static IEnumerable<TypeItem> All => new[] {
        TaskView, TaskCreate, TaskEdit, TaskDelete, TaskAssign,
        PersonnelView, PersonnelCreate, PersonnelEdit, PersonnelDelete,
        ReportView, ReportExport, ReportCreate,
        BranchView, BranchManage,
        ProjectView, ProjectManage
    };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        // Gorev Izinleri
        public const int TaskView = 1;
        public const int TaskCreate = 2;
        public const int TaskEdit = 3;
        public const int TaskDelete = 4;
        public const int TaskAssign = 5;

        // Personel Izinleri
        public const int PersonnelView = 10;
        public const int PersonnelCreate = 11;
        public const int PersonnelEdit = 12;
        public const int PersonnelDelete = 13;

        // Rapor Izinleri
        public const int ReportView = 20;
        public const int ReportExport = 21;
        public const int ReportCreate = 22;

        // Sube Izinleri
        public const int BranchView = 30;
        public const int BranchManage = 31;

        // Proje Izinleri
        public const int ProjectView = 40;
        public const int ProjectManage = 41;
    }
}

// ============================================================
// CUSTOMER TASK TYPES (Musteri Gorev Tipleri)
// ============================================================
public static class CustomerTaskTypes
{
    public static readonly TypeItem Inspection = new(1, "Inspection", "CustomerTaskType.Inspection", "Denetim gorevi", "bi-clipboard-check", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Audit = new(2, "Audit", "CustomerTaskType.Audit", "Audit gorevi", "bi-file-earmark-check", "bg-info", 2);
    public static readonly TypeItem Survey = new(3, "Survey", "CustomerTaskType.Survey", "Anket/Degerlendirme gorevi", "bi-card-checklist", "bg-success", 3);
    public static readonly TypeItem FieldWork = new(4, "FieldWork", "CustomerTaskType.FieldWork", "Saha calismasi", "bi-geo-alt", "bg-warning text-dark", 4);
    public static readonly TypeItem Reporting = new(5, "Reporting", "CustomerTaskType.Reporting", "Raporlama gorevi", "bi-file-earmark-bar-graph", "bg-secondary", 5);

    public static IEnumerable<TypeItem> All => new[] { Inspection, Audit, Survey, FieldWork, Reporting };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Inspection = 1;
        public const int Audit = 2;
        public const int Survey = 3;
        public const int FieldWork = 4;
        public const int Reporting = 5;
    }
}

// ============================================================
// EXCEL COLUMN TYPES (Excel Sutun Tipleri)
// ============================================================
public static class ExcelColumnTypes
{
    public static readonly TypeItem Text = new(1, "Text", "ExcelColumnType.Text", "Metin", "bi-fonts", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Number = new(2, "Number", "ExcelColumnType.Number", "Sayi", "bi-123", "bg-info", 2);
    public static readonly TypeItem Date = new(3, "Date", "ExcelColumnType.Date", "Tarih", "bi-calendar", "bg-primary", 3);
    public static readonly TypeItem Boolean = new(4, "Boolean", "ExcelColumnType.Boolean", "Evet/Hayir", "bi-toggle-on", "bg-success", 4);
    public static readonly TypeItem Email = new(5, "Email", "ExcelColumnType.Email", "E-posta", "bi-envelope", "bg-warning text-dark", 5);
    public static readonly TypeItem Phone = new(6, "Phone", "ExcelColumnType.Phone", "Telefon", "bi-telephone", "bg-dark", 6);
    public static readonly TypeItem Dropdown = new(7, "Dropdown", "ExcelColumnType.Dropdown", "Secenekli liste", "bi-list", "bg-purple", 7);

    public static IEnumerable<TypeItem> All => new[] { Text, Number, Date, Boolean, Email, Phone, Dropdown };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Text = 1;
        public const int Number = 2;
        public const int Date = 3;
        public const int Boolean = 4;
        public const int Email = 5;
        public const int Phone = 6;
        public const int Dropdown = 7;
    }
}

// ============================================================
// VISIT TYPES (Ziyaret Tipleri)
// ============================================================
public static class VisitTypes
{
    public static readonly TypeItem Routine = new(0, "Routine", "VisitType.Routine", "Rutin ziyaret", "bi-arrow-repeat", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Initial = new(1, "Initial", "VisitType.Initial", "Ilk ziyaret", "bi-1-circle", "bg-info", 2);
    public static readonly TypeItem FollowUp = new(2, "FollowUp", "VisitType.FollowUp", "Takip ziyareti", "bi-arrow-right-circle", "bg-primary", 3);
    public static readonly TypeItem Audit = new(3, "Audit", "VisitType.Audit", "Denetim ziyareti", "bi-clipboard-check", "bg-warning text-dark", 4);
    public static readonly TypeItem Training = new(4, "Training", "VisitType.Training", "Egitim ziyareti", "bi-mortarboard", "bg-success", 5);
    public static readonly TypeItem Complaint = new(5, "Complaint", "VisitType.Complaint", "Sikayet/Sorun cozumu", "bi-exclamation-triangle", "bg-danger", 6);
    public static readonly TypeItem Sales = new(6, "Sales", "VisitType.Sales", "Satis ziyareti", "bi-cash-coin", "bg-success", 7);
    public static readonly TypeItem MysteryShop = new(7, "MysteryShop", "VisitType.MysteryShop", "Gizli musteri ziyareti", "bi-incognito", "bg-dark", 8);
    public static readonly TypeItem BankMysteryShop = new(8, "BankMysteryShop", "VisitType.BankMysteryShop", "Banka gizli musteri ziyareti (GBF)", "bi-bank", "bg-primary", 9);
    public static readonly TypeItem Other = new(99, "Other", "VisitType.Other", "Diger", "bi-three-dots", "bg-secondary", 99);

    public static IEnumerable<TypeItem> All => new[] { Routine, Initial, FollowUp, Audit, Training, Complaint, Sales, MysteryShop, BankMysteryShop, Other };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Routine = 0;
        public const int Initial = 1;
        public const int FollowUp = 2;
        public const int Audit = 3;
        public const int Training = 4;
        public const int Complaint = 5;
        public const int Sales = 6;
        public const int MysteryShop = 7;
        public const int BankMysteryShop = 8;
        public const int Other = 99;
    }
}

// ============================================================
// VISIT STATUSES (Ziyaret Durumlari)
// ============================================================
public static class VisitStatuses
{
    public static readonly TypeItem Planned = new(0, "Planned", "VisitStatus.Planned", "Planlandi", "bi-calendar-event", "bg-info", 1, isDefault: true);
    public static readonly TypeItem InProgress = new(1, "InProgress", "VisitStatus.InProgress", "Devam ediyor", "bi-play-circle", "bg-primary", 2);
    public static readonly TypeItem Completed = new(2, "Completed", "VisitStatus.Completed", "Tamamlandi", "bi-check-circle", "bg-success", 3);
    public static readonly TypeItem Cancelled = new(3, "Cancelled", "VisitStatus.Cancelled", "Iptal edildi", "bi-x-circle", "bg-danger", 4);
    public static readonly TypeItem Postponed = new(4, "Postponed", "VisitStatus.Postponed", "Ertelendi", "bi-clock-history", "bg-warning text-dark", 5);
    public static readonly TypeItem NotCompleted = new(5, "NotCompleted", "VisitStatus.NotCompleted", "Gerceklestirilemedi", "bi-slash-circle", "bg-secondary", 6);

    public static IEnumerable<TypeItem> All => new[] { Planned, InProgress, Completed, Cancelled, Postponed, NotCompleted };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Planned = 0;
        public const int InProgress = 1;
        public const int Completed = 2;
        public const int Cancelled = 3;
        public const int Postponed = 4;
        public const int NotCompleted = 5;
    }
}

// ============================================================
// ATTACHMENT TYPES (Ek Dosya Tipleri)
// ============================================================
public static class AttachmentTypes
{
    public static readonly TypeItem Photo = new(0, "Photo", "AttachmentType.Photo", "Fotograf", "bi-image", "bg-info", 1, isDefault: true);
    public static readonly TypeItem Document = new(1, "Document", "AttachmentType.Document", "Belge", "bi-file-earmark-text", "bg-primary", 2);
    public static readonly TypeItem Video = new(2, "Video", "AttachmentType.Video", "Video", "bi-camera-video", "bg-danger", 3);
    public static readonly TypeItem Audio = new(3, "Audio", "AttachmentType.Audio", "Ses kaydi", "bi-mic", "bg-success", 4);
    public static readonly TypeItem Other = new(99, "Other", "AttachmentType.Other", "Diger", "bi-file", "bg-secondary", 99);

    public static IEnumerable<TypeItem> All => new[] { Photo, Document, Video, Audio, Other };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Photo = 0;
        public const int Document = 1;
        public const int Video = 2;
        public const int Audio = 3;
        public const int Other = 99;
    }
}

// ============================================================
// TRAINING TYPES (Egitim Tipleri)
// ============================================================
public static class TrainingTypes
{
    public static readonly TypeItem InPerson = new(0, "InPerson", "TrainingType.InPerson", "Yuz yuze egitim", "bi-people", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Online = new(1, "Online", "TrainingType.Online", "Online egitim", "bi-laptop", "bg-info", 2);
    public static readonly TypeItem Hybrid = new(2, "Hybrid", "TrainingType.Hybrid", "Hibrit egitim", "bi-shuffle", "bg-secondary", 3);
    public static readonly TypeItem SelfPaced = new(3, "SelfPaced", "TrainingType.SelfPaced", "Kendi kendine ogrenme", "bi-book", "bg-success", 4);
    public static readonly TypeItem OnTheJob = new(4, "OnTheJob", "TrainingType.OnTheJob", "Is basi egitim", "bi-briefcase", "bg-warning text-dark", 5);
    public static readonly TypeItem Workshop = new(5, "Workshop", "TrainingType.Workshop", "Workshop", "bi-tools", "bg-dark", 6);
    public static readonly TypeItem Seminar = new(6, "Seminar", "TrainingType.Seminar", "Seminer", "bi-easel", "bg-info", 7);
    public static readonly TypeItem Certification = new(7, "Certification", "TrainingType.Certification", "Sertifika programi", "bi-award", "bg-success", 8);

    public static IEnumerable<TypeItem> All => new[] { InPerson, Online, Hybrid, SelfPaced, OnTheJob, Workshop, Seminar, Certification };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int InPerson = 0;
        public const int Online = 1;
        public const int Hybrid = 2;
        public const int SelfPaced = 3;
        public const int OnTheJob = 4;
        public const int Workshop = 5;
        public const int Seminar = 6;
        public const int Certification = 7;
    }
}

// ============================================================
// TRAINING STATUSES (Egitim Durumlari)
// ============================================================
public static class TrainingStatuses
{
    public static readonly TypeItem Draft = new(0, "Draft", "TrainingStatus.Draft", "Taslak", "bi-file-earmark", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Planned = new(1, "Planned", "TrainingStatus.Planned", "Planlandi", "bi-calendar-event", "bg-info", 2);
    public static readonly TypeItem PendingApproval = new(2, "PendingApproval", "TrainingStatus.PendingApproval", "Onay bekliyor", "bi-hourglass-split", "bg-warning text-dark", 3);
    public static readonly TypeItem Approved = new(3, "Approved", "TrainingStatus.Approved", "Onaylandi", "bi-check-circle", "bg-success", 4);
    public static readonly TypeItem InProgress = new(4, "InProgress", "TrainingStatus.InProgress", "Devam ediyor", "bi-play-circle", "bg-primary", 5);
    public static readonly TypeItem Completed = new(5, "Completed", "TrainingStatus.Completed", "Tamamlandi", "bi-check2-circle", "bg-success", 6);
    public static readonly TypeItem Cancelled = new(6, "Cancelled", "TrainingStatus.Cancelled", "Iptal edildi", "bi-x-circle", "bg-danger", 7);
    public static readonly TypeItem Postponed = new(7, "Postponed", "TrainingStatus.Postponed", "Ertelendi", "bi-clock-history", "bg-warning text-dark", 8);

    public static IEnumerable<TypeItem> All => new[] { Draft, Planned, PendingApproval, Approved, InProgress, Completed, Cancelled, Postponed };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Draft = 0;
        public const int Planned = 1;
        public const int PendingApproval = 2;
        public const int Approved = 3;
        public const int InProgress = 4;
        public const int Completed = 5;
        public const int Cancelled = 6;
        public const int Postponed = 7;
    }
}

// ============================================================
// TRAINING CATEGORIES (Egitim Kategorileri)
// ============================================================
public static class TrainingCategories
{
    public static readonly TypeItem General = new(0, "General", "TrainingCategory.General", "Genel", "bi-grid", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Technical = new(1, "Technical", "TrainingCategory.Technical", "Teknik", "bi-gear", "bg-dark", 2);
    public static readonly TypeItem Sales = new(2, "Sales", "TrainingCategory.Sales", "Satis", "bi-cash-coin", "bg-success", 3);
    public static readonly TypeItem CustomerService = new(3, "CustomerService", "TrainingCategory.CustomerService", "Musteri hizmetleri", "bi-headset", "bg-info", 4);
    public static readonly TypeItem Management = new(4, "Management", "TrainingCategory.Management", "Yonetim", "bi-briefcase", "bg-primary", 5);
    public static readonly TypeItem Communication = new(5, "Communication", "TrainingCategory.Communication", "Iletisim", "bi-chat-dots", "bg-info", 6);
    public static readonly TypeItem Product = new(6, "Product", "TrainingCategory.Product", "Urun", "bi-box", "bg-warning text-dark", 7);
    public static readonly TypeItem Process = new(7, "Process", "TrainingCategory.Process", "Surec", "bi-diagram-3", "bg-secondary", 8);
    public static readonly TypeItem Safety = new(8, "Safety", "TrainingCategory.Safety", "Guvenlik", "bi-shield-check", "bg-danger", 9);
    public static readonly TypeItem Compliance = new(9, "Compliance", "TrainingCategory.Compliance", "Uyum", "bi-clipboard-check", "bg-warning text-dark", 10);

    public static IEnumerable<TypeItem> All => new[] { General, Technical, Sales, CustomerService, Management, Communication, Product, Process, Safety, Compliance };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int General = 0;
        public const int Technical = 1;
        public const int Sales = 2;
        public const int CustomerService = 3;
        public const int Management = 4;
        public const int Communication = 5;
        public const int Product = 6;
        public const int Process = 7;
        public const int Safety = 8;
        public const int Compliance = 9;
    }
}

// ============================================================
// TRAINING PARTICIPANT STATUSES (Egitim Katilimci Durumlari)
// ============================================================
public static class TrainingParticipantStatuses
{
    public static readonly TypeItem Invited = new(0, "Invited", "TrainingParticipantStatus.Invited", "Davetli", "bi-envelope", "bg-info", 1, isDefault: true);
    public static readonly TypeItem Accepted = new(1, "Accepted", "TrainingParticipantStatus.Accepted", "Kabul etti", "bi-check", "bg-success", 2);
    public static readonly TypeItem Declined = new(2, "Declined", "TrainingParticipantStatus.Declined", "Reddetti", "bi-x", "bg-danger", 3);
    public static readonly TypeItem Attended = new(3, "Attended", "TrainingParticipantStatus.Attended", "Katildi", "bi-person-check", "bg-primary", 4);
    public static readonly TypeItem NotAttended = new(4, "NotAttended", "TrainingParticipantStatus.NotAttended", "Katilmadi", "bi-person-x", "bg-warning text-dark", 5);
    public static readonly TypeItem Completed = new(5, "Completed", "TrainingParticipantStatus.Completed", "Tamamladi", "bi-check2-circle", "bg-success", 6);
    public static readonly TypeItem Failed = new(6, "Failed", "TrainingParticipantStatus.Failed", "Basarisiz", "bi-x-circle", "bg-danger", 7);

    public static IEnumerable<TypeItem> All => new[] { Invited, Accepted, Declined, Attended, NotAttended, Completed, Failed };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Invited = 0;
        public const int Accepted = 1;
        public const int Declined = 2;
        public const int Attended = 3;
        public const int NotAttended = 4;
        public const int Completed = 5;
        public const int Failed = 6;
    }
}

// ============================================================
// GENDERS (Cinsiyet)
// ============================================================
public static class Genders
{
    public static readonly TypeItem Unspecified = new(0, "Unspecified", "Gender.Unspecified", "Belirtilmemis", "bi-question-circle", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Male = new(1, "Male", "Gender.Male", "Erkek", "bi-gender-male", "bg-primary", 2);
    public static readonly TypeItem Female = new(2, "Female", "Gender.Female", "Kadin", "bi-gender-female", "bg-danger", 3);

    public static IEnumerable<TypeItem> All => new[] { Unspecified, Male, Female };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Unspecified = 0;
        public const int Male = 1;
        public const int Female = 2;
    }
}

// ============================================================
// MEETING TYPES (Toplanti Tipleri)
// ============================================================
public static class MeetingTypes
{
    public static readonly TypeItem General = new(0, "General", "MeetingType.General", "Genel toplanti", "bi-calendar", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Project = new(1, "Project", "MeetingType.Project", "Proje toplantisi", "bi-kanban", "bg-primary", 2);
    public static readonly TypeItem Evaluation = new(2, "Evaluation", "MeetingType.Evaluation", "Degerlendirme toplantisi", "bi-clipboard-check", "bg-info", 3);
    public static readonly TypeItem Training = new(3, "Training", "MeetingType.Training", "Egitim toplantisi", "bi-mortarboard", "bg-success", 4);
    public static readonly TypeItem Customer = new(4, "Customer", "MeetingType.Customer", "Musteri toplantisi", "bi-building", "bg-warning text-dark", 5);
    public static readonly TypeItem KickOff = new(5, "KickOff", "MeetingType.KickOff", "Kick-off toplantisi", "bi-rocket", "bg-danger", 6);
    public static readonly TypeItem Closing = new(6, "Closing", "MeetingType.Closing", "Kapanis toplantisi", "bi-flag", "bg-dark", 7);

    public static IEnumerable<TypeItem> All => new[] { General, Project, Evaluation, Training, Customer, KickOff, Closing };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int General = 0;
        public const int Project = 1;
        public const int Evaluation = 2;
        public const int Training = 3;
        public const int Customer = 4;
        public const int KickOff = 5;
        public const int Closing = 6;
    }
}

// ============================================================
// MEETING STATUSES (Toplanti Durumlari)
// ============================================================
public static class MeetingStatuses
{
    public static readonly TypeItem Planned = new(0, "Planned", "MeetingStatus.Planned", "Planlandi", "bi-calendar-event", "bg-info", 1, isDefault: true);
    public static readonly TypeItem PendingApproval = new(1, "PendingApproval", "MeetingStatus.PendingApproval", "Onay bekliyor", "bi-hourglass", "bg-warning text-dark", 2);
    public static readonly TypeItem Approved = new(2, "Approved", "MeetingStatus.Approved", "Onaylandi", "bi-check-circle", "bg-success", 3);
    public static readonly TypeItem InProgress = new(3, "InProgress", "MeetingStatus.InProgress", "Devam ediyor", "bi-play-circle", "bg-primary", 4);
    public static readonly TypeItem Completed = new(4, "Completed", "MeetingStatus.Completed", "Tamamlandi", "bi-check2-all", "bg-success", 5);
    public static readonly TypeItem Cancelled = new(5, "Cancelled", "MeetingStatus.Cancelled", "Iptal edildi", "bi-x-circle", "bg-danger", 6);
    public static readonly TypeItem Postponed = new(6, "Postponed", "MeetingStatus.Postponed", "Ertelendi", "bi-arrow-clockwise", "bg-secondary", 7);

    public static IEnumerable<TypeItem> All => new[] { Planned, PendingApproval, Approved, InProgress, Completed, Cancelled, Postponed };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Planned = 0;
        public const int PendingApproval = 1;
        public const int Approved = 2;
        public const int InProgress = 3;
        public const int Completed = 4;
        public const int Cancelled = 5;
        public const int Postponed = 6;
    }
}

// ============================================================
// PARTICIPANT STATUSES (Katilimci Durumlari)
// ============================================================
public static class ParticipantStatuses
{
    public static readonly TypeItem Invited = new(0, "Invited", "ParticipantStatus.Invited", "Davet edildi", "bi-envelope", "bg-info", 1, isDefault: true);
    public static readonly TypeItem Accepted = new(1, "Accepted", "ParticipantStatus.Accepted", "Kabul etti", "bi-check", "bg-success", 2);
    public static readonly TypeItem Declined = new(2, "Declined", "ParticipantStatus.Declined", "Reddetti", "bi-x", "bg-danger", 3);
    public static readonly TypeItem Tentative = new(3, "Tentative", "ParticipantStatus.Tentative", "Belirsiz", "bi-question", "bg-warning text-dark", 4);
    public static readonly TypeItem Attended = new(4, "Attended", "ParticipantStatus.Attended", "Katildi", "bi-person-check", "bg-success", 5);
    public static readonly TypeItem NotAttended = new(5, "NotAttended", "ParticipantStatus.NotAttended", "Katilmadi", "bi-person-x", "bg-secondary", 6);

    public static IEnumerable<TypeItem> All => new[] { Invited, Accepted, Declined, Tentative, Attended, NotAttended };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Invited = 0;
        public const int Accepted = 1;
        public const int Declined = 2;
        public const int Tentative = 3;
        public const int Attended = 4;
        public const int NotAttended = 5;
    }
}

// ============================================================
// PERMISSION CATEGORIES (Yetki Kategorileri)
// ============================================================
public static class PermissionCategories
{
    public static readonly TypeItem Users = new(1, "Users", "PermissionCategory.Users", "Kullanici yonetimi", "bi-people", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Roles = new(2, "Roles", "PermissionCategory.Roles", "Rol yonetimi", "bi-shield", "bg-info", 2);
    public static readonly TypeItem Projects = new(3, "Projects", "PermissionCategory.Projects", "Proje yonetimi", "bi-kanban", "bg-success", 3);
    public static readonly TypeItem Assignments = new(4, "Assignments", "PermissionCategory.Assignments", "Gorev/Atama yonetimi", "bi-clipboard-check", "bg-warning text-dark", 4);
    public static readonly TypeItem Checklists = new(5, "Checklists", "PermissionCategory.Checklists", "Kontrol listesi yonetimi", "bi-list-check", "bg-secondary", 5);
    public static readonly TypeItem Evaluations = new(6, "Evaluations", "PermissionCategory.Evaluations", "Degerlendirme yonetimi", "bi-star", "bg-danger", 6);
    public static readonly TypeItem Branches = new(7, "Branches", "PermissionCategory.Branches", "Sube yonetimi", "bi-building", "bg-dark", 7);
    public static readonly TypeItem FieldWorkers = new(8, "FieldWorkers", "PermissionCategory.FieldWorkers", "Saha calisani yonetimi", "bi-person-badge", "bg-primary", 8);
    public static readonly TypeItem Reports = new(9, "Reports", "PermissionCategory.Reports", "Rapor yonetimi", "bi-bar-chart", "bg-info", 9);
    public static readonly TypeItem Dashboard = new(10, "Dashboard", "PermissionCategory.Dashboard", "Dashboard erisimi", "bi-speedometer2", "bg-success", 10);
    public static readonly TypeItem ExcelTemplates = new(11, "ExcelTemplates", "PermissionCategory.ExcelTemplates", "Excel template yonetimi", "bi-file-earmark-excel", "bg-success", 11);
    public static readonly TypeItem Customers = new(12, "Customers", "PermissionCategory.Customers", "Musteri yonetimi", "bi-building", "bg-warning text-dark", 12);
    public static readonly TypeItem CustomerPersonnel = new(13, "CustomerPersonnel", "PermissionCategory.CustomerPersonnel", "Musteri personeli yonetimi", "bi-people", "bg-secondary", 13);
    public static readonly TypeItem Settings = new(14, "Settings", "PermissionCategory.Settings", "Sistem ayarlari", "bi-gear", "bg-dark", 14);
    public static readonly TypeItem Languages = new(15, "Languages", "PermissionCategory.Languages", "Dil yonetimi", "bi-translate", "bg-info", 15);
    public static readonly TypeItem Trainings = new(16, "Trainings", "PermissionCategory.Trainings", "Egitim yonetimi", "bi-mortarboard", "bg-primary", 16);
    public static readonly TypeItem Meetings = new(17, "Meetings", "PermissionCategory.Meetings", "Toplanti yonetimi", "bi-calendar-event", "bg-success", 17);
    public static readonly TypeItem Approvals = new(18, "Approvals", "PermissionCategory.Approvals", "Onay yonetimi", "bi-check2-square", "bg-warning text-dark", 18);
    public static readonly TypeItem DraftRequests = new(19, "DraftRequests", "PermissionCategory.DraftRequests", "Taslak talepleri", "bi-file-earmark-text", "bg-secondary", 19);
    public static readonly TypeItem CustomerOrganizations = new(20, "CustomerOrganizations", "PermissionCategory.CustomerOrganizations", "Musteri organizasyonlari", "bi-diagram-3", "bg-info", 20);
    public static readonly TypeItem Personnel = new(21, "Personnel", "PermissionCategory.Personnel", "Personel (Sube personeli)", "bi-person-vcard", "bg-dark", 21);

    public static IEnumerable<TypeItem> All => new[] { Users, Roles, Projects, Assignments, Checklists, Evaluations, Branches, FieldWorkers, Reports, Dashboard, ExcelTemplates, Customers, CustomerPersonnel, Settings, Languages, Trainings, Meetings, Approvals, DraftRequests, CustomerOrganizations, Personnel };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Users = 1;
        public const int Roles = 2;
        public const int Projects = 3;
        public const int Assignments = 4;
        public const int Checklists = 5;
        public const int Evaluations = 6;
        public const int Branches = 7;
        public const int FieldWorkers = 8;
        public const int Reports = 9;
        public const int Dashboard = 10;
        public const int ExcelTemplates = 11;
        public const int Customers = 12;
        public const int CustomerPersonnel = 13;
        public const int Settings = 14;
        public const int Languages = 15;
        public const int Trainings = 16;
        public const int Meetings = 17;
        public const int Approvals = 18;
        public const int DraftRequests = 19;
        public const int CustomerOrganizations = 20;
        public const int Personnel = 21;
    }
}

// ============================================================
// PERMISSION SCOPES (Yetki Kapsamlari)
// ============================================================
public static class PermissionScopes
{
    public static readonly TypeItem All = new(1, "All", "PermissionScope.All", "Tum kaynaklara erisim", "bi-globe", "bg-success", 1, isDefault: true);
    public static readonly TypeItem Own = new(2, "Own", "PermissionScope.Own", "Sadece kendi olusturdugu kaynaklara erisim", "bi-person", "bg-primary", 2);
    public static readonly TypeItem Branch = new(3, "Branch", "PermissionScope.Branch", "Kendi subesine ait kaynaklara erisim", "bi-building", "bg-info", 3);
    public static readonly TypeItem Department = new(4, "Department", "PermissionScope.Department", "Kendi departmanina ait kaynaklara erisim", "bi-diagram-3", "bg-warning text-dark", 4);
    public static readonly TypeItem Customer = new(5, "Customer", "PermissionScope.Customer", "Kendi musterisine ait kaynaklara erisim", "bi-person-badge", "bg-secondary", 5);

    public static IEnumerable<TypeItem> AllItems => new[] { All, Own, Branch, Department, Customer };
    public static TypeItem Default => AllItems.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => AllItems.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => AllItems.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int All = 1;
        public const int Own = 2;
        public const int Branch = 3;
        public const int Department = 4;
        public const int Customer = 5;
    }
}

// ============================================================
// LOG TYPES (Log Turleri)
// ============================================================
public static class LogTypes
{
    public static readonly TypeItem Info = new(0, "Info", "LogType.Info", "Bilgi mesaji", "bi-info-circle", "bg-info", 1, isDefault: true);
    public static readonly TypeItem Warning = new(1, "Warning", "LogType.Warning", "Uyari", "bi-exclamation-triangle", "bg-warning text-dark", 2);
    public static readonly TypeItem Error = new(2, "Error", "LogType.Error", "Hata", "bi-x-circle", "bg-danger", 3);
    public static readonly TypeItem DataCreate = new(10, "DataCreate", "LogType.DataCreate", "Veri olusturma", "bi-plus-circle", "bg-success", 10);
    public static readonly TypeItem DataUpdate = new(11, "DataUpdate", "LogType.DataUpdate", "Veri guncelleme", "bi-pencil", "bg-primary", 11);
    public static readonly TypeItem DataDelete = new(12, "DataDelete", "LogType.DataDelete", "Veri silme", "bi-trash", "bg-danger", 12);
    public static readonly TypeItem Login = new(20, "Login", "LogType.Login", "Kullanici girisi", "bi-box-arrow-in-right", "bg-success", 20);
    public static readonly TypeItem Logout = new(21, "Logout", "LogType.Logout", "Kullanici cikisi", "bi-box-arrow-right", "bg-secondary", 21);
    public static readonly TypeItem LoginFailed = new(22, "LoginFailed", "LogType.LoginFailed", "Basarisiz giris denemesi", "bi-shield-x", "bg-danger", 22);
    public static readonly TypeItem AccessDenied = new(23, "AccessDenied", "LogType.AccessDenied", "Yetki hatasi", "bi-slash-circle", "bg-danger", 23);

    public static IEnumerable<TypeItem> All => new[] { Info, Warning, Error, DataCreate, DataUpdate, DataDelete, Login, Logout, LoginFailed, AccessDenied };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Info = 0;
        public const int Warning = 1;
        public const int Error = 2;
        public const int DataCreate = 10;
        public const int DataUpdate = 11;
        public const int DataDelete = 12;
        public const int Login = 20;
        public const int Logout = 21;
        public const int LoginFailed = 22;
        public const int AccessDenied = 23;
    }
}

// ============================================================
// SETTING VALUE TYPES (Ayar Degeri Tipleri)
// ============================================================
public static class SettingValueTypes
{
    public static readonly TypeItem String = new(0, "String", "SettingValueType.String", "Metin", "bi-fonts", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Bool = new(1, "Bool", "SettingValueType.Bool", "Evet/Hayir", "bi-toggle-on", "bg-success", 2);
    public static readonly TypeItem Int = new(2, "Int", "SettingValueType.Int", "Tam sayi", "bi-123", "bg-info", 3);
    public static readonly TypeItem Decimal = new(3, "Decimal", "SettingValueType.Decimal", "Ondalik sayi", "bi-calculator", "bg-primary", 4);
    public static readonly TypeItem Json = new(4, "Json", "SettingValueType.Json", "JSON veri", "bi-braces", "bg-warning text-dark", 5);
    public static readonly TypeItem DateTime = new(5, "DateTime", "SettingValueType.DateTime", "Tarih/Saat", "bi-calendar-event", "bg-dark", 6);

    public static IEnumerable<TypeItem> All => new[] { String, Bool, Int, Decimal, Json, DateTime };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int String = 0;
        public const int Bool = 1;
        public const int Int = 2;
        public const int Decimal = 3;
        public const int Json = 4;
        public const int DateTime = 5;
    }
}

// ============================================================
// DATE RANGE TYPES (Tarih Araligi Tipleri)
// ============================================================
public static class DateRangeTypes
{
    public static readonly TypeItem Today = new(1, "today", "DateRange.Today", "Bugun", "bi-calendar-day", "bg-primary", 1);
    public static readonly TypeItem Yesterday = new(2, "yesterday", "DateRange.Yesterday", "Dun", "bi-calendar-minus", "bg-secondary", 2);
    public static readonly TypeItem Last7Days = new(3, "last7Days", "DateRange.Last7Days", "Son 7 Gun", "bi-calendar-week", "bg-info", 3);
    public static readonly TypeItem Last30Days = new(4, "last30Days", "DateRange.Last30Days", "Son 30 Gun", "bi-calendar-month", "bg-info", 4);
    public static readonly TypeItem ThisWeek = new(5, "thisWeek", "DateRange.ThisWeek", "Bu Hafta", "bi-calendar2-week", "bg-success", 5);
    public static readonly TypeItem LastWeek = new(6, "lastWeek", "DateRange.LastWeek", "Gecen Hafta", "bi-calendar2-week", "bg-secondary", 6);
    public static readonly TypeItem ThisMonth = new(7, "thisMonth", "DateRange.ThisMonth", "Bu Ay", "bi-calendar3", "bg-success", 7);
    public static readonly TypeItem LastMonth = new(8, "lastMonth", "DateRange.LastMonth", "Gecen Ay", "bi-calendar3", "bg-secondary", 8);
    public static readonly TypeItem ThisQuarter = new(9, "thisQuarter", "DateRange.ThisQuarter", "Bu Ceyrek", "bi-calendar-range", "bg-warning text-dark", 9);
    public static readonly TypeItem ThisYear = new(10, "thisYear", "DateRange.ThisYear", "Bu Yil", "bi-calendar4", "bg-danger", 10);

    public static IEnumerable<TypeItem> All => new[] { Today, Yesterday, Last7Days, Last30Days, ThisWeek, LastWeek, ThisMonth, LastMonth, ThisQuarter, ThisYear };
    public static TypeItem Default => Today;
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Today = 1;
        public const int Yesterday = 2;
        public const int Last7Days = 3;
        public const int Last30Days = 4;
        public const int ThisWeek = 5;
        public const int LastWeek = 6;
        public const int ThisMonth = 7;
        public const int LastMonth = 8;
        public const int ThisQuarter = 9;
        public const int ThisYear = 10;
    }
}

// ============================================================
// MONTHS (Aylar)
// ============================================================
public static class Months
{
    public static readonly TypeItem January = new(1, "January", "Common.Month.January", displayOrder: 1);
    public static readonly TypeItem February = new(2, "February", "Common.Month.February", displayOrder: 2);
    public static readonly TypeItem March = new(3, "March", "Common.Month.March", displayOrder: 3);
    public static readonly TypeItem April = new(4, "April", "Common.Month.April", displayOrder: 4);
    public static readonly TypeItem May = new(5, "May", "Common.Month.May", displayOrder: 5);
    public static readonly TypeItem June = new(6, "June", "Common.Month.June", displayOrder: 6);
    public static readonly TypeItem July = new(7, "July", "Common.Month.July", displayOrder: 7);
    public static readonly TypeItem August = new(8, "August", "Common.Month.August", displayOrder: 8);
    public static readonly TypeItem September = new(9, "September", "Common.Month.September", displayOrder: 9);
    public static readonly TypeItem October = new(10, "October", "Common.Month.October", displayOrder: 10);
    public static readonly TypeItem November = new(11, "November", "Common.Month.November", displayOrder: 11);
    public static readonly TypeItem December = new(12, "December", "Common.Month.December", displayOrder: 12);

    public static IEnumerable<TypeItem> All => new[] { January, February, March, April, May, June, July, August, September, October, November, December };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    /// <summary>
    /// DateTime'dan ay TypeItem döndürür (1-12 arası month değerinden)
    /// </summary>
    public static TypeItem? GetByMonth(int month) => GetById(month);
    public static TypeItem? GetByDate(DateTime date) => GetById(date.Month);

    public static class Ids
    {
        public const int January = 1;
        public const int February = 2;
        public const int March = 3;
        public const int April = 4;
        public const int May = 5;
        public const int June = 6;
        public const int July = 7;
        public const int August = 8;
        public const int September = 9;
        public const int October = 10;
        public const int November = 11;
        public const int December = 12;
    }
}

// ============================================================
// DAYS OF WEEK (Haftanın Günleri)
// ============================================================
public static class DaysOfWeek
{
    // .NET DayOfWeek enum: Sunday=0, Monday=1, ..., Saturday=6
    // Ancak iş günleri mantığı için Monday=1 başlatıyoruz
    public static readonly TypeItem Monday = new(1, "Monday", "Common.Day.Monday", displayOrder: 1);
    public static readonly TypeItem Tuesday = new(2, "Tuesday", "Common.Day.Tuesday", displayOrder: 2);
    public static readonly TypeItem Wednesday = new(3, "Wednesday", "Common.Day.Wednesday", displayOrder: 3);
    public static readonly TypeItem Thursday = new(4, "Thursday", "Common.Day.Thursday", displayOrder: 4);
    public static readonly TypeItem Friday = new(5, "Friday", "Common.Day.Friday", displayOrder: 5);
    public static readonly TypeItem Saturday = new(6, "Saturday", "Common.Day.Saturday", displayOrder: 6);
    public static readonly TypeItem Sunday = new(0, "Sunday", "Common.Day.Sunday", displayOrder: 7);

    public static IEnumerable<TypeItem> All => new[] { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday };
    public static IEnumerable<TypeItem> Weekdays => new[] { Monday, Tuesday, Wednesday, Thursday, Friday };
    public static IEnumerable<TypeItem> Weekend => new[] { Saturday, Sunday };

    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    /// <summary>
    /// .NET DayOfWeek enum'dan TypeItem döndürür
    /// </summary>
    public static TypeItem? GetByDayOfWeek(DayOfWeek dayOfWeek) => GetById((int)dayOfWeek);
    public static TypeItem? GetByDate(DateTime date) => GetByDayOfWeek(date.DayOfWeek);

    public static class Ids
    {
        public const int Sunday = 0;
        public const int Monday = 1;
        public const int Tuesday = 2;
        public const int Wednesday = 3;
        public const int Thursday = 4;
        public const int Friday = 5;
        public const int Saturday = 6;
    }
}

// ============================================================
// DEALER TYPES (Bayi Tipleri)
// ============================================================
public static class DealerTypes
{
    public static readonly TypeItem Retail = new(1, "Retail", "DealerType.Retail", "Perakende bayi", "bi-shop", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Wholesale = new(2, "Wholesale", "DealerType.Wholesale", "Toptan bayi", "bi-box-seam", "bg-info", 2);
    public static readonly TypeItem Franchise = new(3, "Franchise", "DealerType.Franchise", "Franchise bayi", "bi-building", "bg-warning text-dark", 3);
    public static readonly TypeItem Authorized = new(4, "Authorized", "DealerType.Authorized", "Yetkili bayi", "bi-award", "bg-success", 4);

    public static IEnumerable<TypeItem> All => new[] { Retail, Wholesale, Franchise, Authorized };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Retail = 1;
        public const int Wholesale = 2;
        public const int Franchise = 3;
        public const int Authorized = 4;
    }
}

// ============================================================
// REQUEST STATUSES (Talep Durumlari)
// ============================================================
public static class RequestStatuses
{
    public static readonly TypeItem Pending = new(1, "Pending", "RequestStatus.Pending", "Beklemede - Henüz işlenmedi", "bi-hourglass-split", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem Approved = new(2, "Approved", "RequestStatus.Approved", "Onaylandı", "bi-check-circle", "bg-success", 2);
    public static readonly TypeItem Rejected = new(3, "Rejected", "RequestStatus.Rejected", "Reddedildi", "bi-x-circle", "bg-danger", 3);

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

// ============================================================
// REQUEST TYPES (Talep Tipleri)
// ============================================================
public static class RequestTypes
{
    public static readonly TypeItem NewDealer = new(1, "NewDealer", "RequestType.NewDealer", "Yeni bayi talebi", "bi-plus-circle", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem UpdateDealer = new(2, "UpdateDealer", "RequestType.UpdateDealer", "Bayi güncelleme talebi", "bi-pencil", "bg-info", 2);
    public static readonly TypeItem NewPersonnel = new(3, "NewPersonnel", "RequestType.NewPersonnel", "Yeni personel talebi", "bi-person-plus", "bg-success", 3);

    public static IEnumerable<TypeItem> All => new[] { NewDealer, UpdateDealer, NewPersonnel };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int NewDealer = 1;
        public const int UpdateDealer = 2;
        public const int NewPersonnel = 3;
    }
}

// ============================================================
// EMAIL TEMPLATE TYPES (Email Şablon Tipleri)
// ============================================================
public static class EmailTemplateTypes
{
    public static readonly TypeItem SurveyInvitation = new(1, "SurveyInvitation", "EmailTemplateType.SurveyInvitation", "Anket Davetiyesi", "bi-envelope-paper", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem SurveyReminder = new(2, "SurveyReminder", "EmailTemplateType.SurveyReminder", "Anket Hatırlatma", "bi-bell", "bg-warning text-dark", 2);
    public static readonly TypeItem SurveyThankYou = new(3, "SurveyThankYou", "EmailTemplateType.SurveyThankYou", "Anket Teşekkür", "bi-heart", "bg-success", 3);
    public static readonly TypeItem ReportNotification = new(4, "ReportNotification", "EmailTemplateType.ReportNotification", "Rapor Bildirimi", "bi-file-earmark-bar-graph", "bg-info", 4);
    public static readonly TypeItem PasswordReset = new(5, "PasswordReset", "EmailTemplateType.PasswordReset", "Şifre Sıfırlama", "bi-key", "bg-danger", 5);
    public static readonly TypeItem WelcomeEmail = new(6, "WelcomeEmail", "EmailTemplateType.WelcomeEmail", "Hoş Geldiniz", "bi-hand-wave", "bg-secondary", 6);
    public static readonly TypeItem Custom = new(7, "Custom", "EmailTemplateType.Custom", "Özel Şablon", "bi-palette", "bg-dark", 7);

    public static IEnumerable<TypeItem> All => new[] { SurveyInvitation, SurveyReminder, SurveyThankYou, ReportNotification, PasswordReset, WelcomeEmail, Custom };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int SurveyInvitation = 1;
        public const int SurveyReminder = 2;
        public const int SurveyThankYou = 3;
        public const int ReportNotification = 4;
        public const int PasswordReset = 5;
        public const int WelcomeEmail = 6;
        public const int Custom = 7;
    }
}

// ============================================================
// SURVEY IDENTITY TYPES (Anket Kimlik Tipleri)
// ============================================================
public static class SurveyIdentityTypes
{
    public static readonly TypeItem Anonymous = new(1, "Anonymous", "SurveyIdentityType.Anonymous", "Anonim (Kimlik Kapalı)", "bi-incognito", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Identified = new(2, "Identified", "SurveyIdentityType.Identified", "Kimlik Açık", "bi-person-badge", "bg-primary", 2);

    public static IEnumerable<TypeItem> All => new[] { Anonymous, Identified };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Anonymous = 1;
        public const int Identified = 2;
    }
}

// ============================================================
// SELECTION TYPES (Seçim Tipleri - SubCriteria için)
// ============================================================
public static class SelectionTypes
{
    public static readonly TypeItem Single = new(1, "Single", "SelectionType.Single", "Tek Seçim", "bi-circle", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Multiple = new(2, "Multiple", "SelectionType.Multiple", "Çoklu Seçim", "bi-check2-square", "bg-info", 2);

    public static IEnumerable<TypeItem> All => new[] { Single, Multiple };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Single = 1;
        public const int Multiple = 2;
    }
}
