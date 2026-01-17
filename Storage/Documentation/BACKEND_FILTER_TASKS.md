# Backend Filtre Standardizasyon Görevleri

> **Tarih:** 2026-01-18
> **Durum:** TAMAMLANDI
> **Referans:** ReportService.cs ve ReportFilterDto

## !!! GENEL TALİMAT !!!

**SORU SORMA, DİREKT YAP.**

- Frontend çoklu filtre gönderiyor (array olarak)
- Backend bu array'leri alıp OR mantığıyla sorgulamalı
- **TEKİL PARAMETRELER YASAK** - sadece çoğul kullan
- Excel export endpoint'leri de aynı filtreleri kullanacak

---

## Yapılacak İşler (Her Servis İçin)

### 1. DTO Güncelleme
```csharp
// SADECE ÇOĞUL - Tekil YASAK!
public List<int>? ProjectIds { get; set; }
public List<int>? CustomerIds { get; set; }
public List<int>? OrganizationIds { get; set; }
```

### 2. Sorguları Güncelle
```csharp
// Contains() ve Any() kullan
if (filter.ProjectIds?.Any() == true)
    query = query.Where(x => filter.ProjectIds.Contains(x.ProjectId));

// Nullable int için
if (filter.EvaluatorIds?.Any() == true)
    query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));
```

### 3. Excel Export Kontrol
Export metodları `buildFilterParams()` ile aynı filtreleri almalı.

---

## INSTANCE 1: Raporlar & Değerlendirmeler ✅ TAMAMLANDI

**Sorumluluk:** Report ve Evaluation backend işleri

### DOSYALAR:
1. ✅ `SecretCustomer.Services/Services/ReportService.cs`
2. ✅ `SecretCustomer.Services/Services/EvaluationService.cs`
3. ✅ `SecretCustomer.Core/DTOs/Report/ReportDto.cs`
4. ✅ `SecretCustomer.API/Controllers/Api/ReportsApiController.cs`
5. ✅ `SecretCustomer.API/Controllers/Api/EvaluationsApiController.cs`

---

## INSTANCE 2: CustomerPortal Backend ✅ TAMAMLANDI

**Sorumluluk:** CustomerPortal API endpoint'leri

### DOSYALAR:
1. ✅ `SecretCustomer.API/Controllers/Api/CustomerPortalController.cs`
2. ✅ `SecretCustomer.API/Controllers/CustomerPortalController.cs`

---

## INSTANCE 3: Müşteriler & Organizasyonlar ✅ TAMAMLANDI

**Sorumluluk:** Customer, Organization, Personnel backend işleri

### DOSYALAR:
1. ✅ `SecretCustomer.Services/Services/CustomerService.cs`
2. ✅ `SecretCustomer.Services/Services/CustomerOrganizationService.cs`
3. ✅ `SecretCustomer.Services/Services/CustomerPersonnelService.cs`
4. ✅ `SecretCustomer.Services/Services/DealerService.cs`
5. ✅ `SecretCustomer.API/Controllers/Api/CustomersApiController.cs`
6. ✅ `SecretCustomer.API/Controllers/Api/CustomerOrganizationsApiController.cs`
7. ✅ `SecretCustomer.API/Controllers/Api/CustomerPersonnelApiController.cs`
8. ✅ `SecretCustomer.API/Controllers/Api/DealersApiController.cs`

---

## INSTANCE 4: Diğer Modüller ✅ TAMAMLANDI

**Sorumluluk:** Assignment, Project, User, Checklist, Training, Meeting, Approval backend işleri

### DOSYALAR:
1. ✅ `SecretCustomer.Services/Services/AssignmentService.cs`
2. ✅ `SecretCustomer.Services/Services/ProjectService.cs`
3. ✅ `SecretCustomer.Services/Services/UserService.cs`
4. ✅ `SecretCustomer.Services/Services/ChecklistService.cs`
5. ✅ `SecretCustomer.API/Controllers/Api/AssignmentsApiController.cs`
6. ✅ `SecretCustomer.API/Controllers/Api/ProjectsApiController.cs`
7. ✅ `SecretCustomer.API/Controllers/Api/UsersApiController.cs`
8. ✅ `SecretCustomer.API/Controllers/Api/ChecklistsApiController.cs`
9. ✅ `SecretCustomer.API/Controllers/Api/TrainingsApiController.cs`
10. ✅ `SecretCustomer.API/Controllers/Api/MeetingsApiController.cs`
11. ✅ `SecretCustomer.API/Controllers/Api/ApprovalsApiController.cs`
12. ✅ `SecretCustomer.API/Controllers/Api/InternalAssignmentsApiController.cs`

---

## Referans: Sorgu Pattern'i

```csharp
public async Task<PagedResult<T>> GetListAsync(FilterDto filter)
{
    var query = _context.Entities.AsQueryable();

    // Çoklu int filtre - Contains() kullan
    if (filter.ProjectIds?.Any() == true)
        query = query.Where(x => filter.ProjectIds.Contains(x.ProjectId));

    // Nullable int için - HasValue kontrolü ekle
    if (filter.EvaluatorIds?.Any() == true)
        query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

    // Çoklu string filtre (case-insensitive)
    if (filter.PersonnelNames?.Any() == true)
        query = query.Where(x => x.PersonnelName != null &&
            filter.PersonnelNames.Any(n => x.PersonnelName.ToLower().Contains(n.ToLower())));

    return await query.ToPagedResultAsync(filter.Page, filter.PageSize);
}
```

---

## Build Durumu

**Son Build:** 2026-01-18 - ✅ Başarılı (0 uyarı, 0 hata)
