namespace SecretCustomer.Core.Enums;

/// <summary>
/// Müşteri personel izin tipleri
/// </summary>
public enum CustomerPermissionType
{
    // Görev İzinleri
    TaskView = 1,
    TaskCreate = 2,
    TaskEdit = 3,
    TaskDelete = 4,
    TaskAssign = 5,

    // Personel İzinleri
    PersonnelView = 10,
    PersonnelCreate = 11,
    PersonnelEdit = 12,
    PersonnelDelete = 13,

    // Rapor İzinleri
    ReportView = 20,
    ReportExport = 21,
    ReportCreate = 22,

    // Şube İzinleri
    BranchView = 30,
    BranchManage = 31,

    // Proje İzinleri
    ProjectView = 40,
    ProjectManage = 41
}