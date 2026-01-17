# Filtre Sistemi Standardizasyon Görevleri

> **Tarih:** 2026-01-17
> **Durum:** TAMAMLANDI (2026-01-18)
> **Referans Sayfa:** `/Listenings`

## !!! GENEL TALİMAT !!!

**SORU SORMA, DİREKT YAP.**

- Hangi sayfaların dönüştürüleceği, hangilerinin atlanacağı bu dosyada yazıyor
- Kendi başına karar ver ve uygula
- Kullanıcıya "şunu mu yapayım bunu mu" diye sorma
- Hata çıkarsa düzelt ve devam et
- İşin bitince kısa özet ver

## Genel Kurallar

### 1. Filtre Sistemi Özellikleri
- **Chip-based filtreler:** Eklenen her filtre bir "chip" olarak görünür
- **Çoklu değer desteği:** Aynı tipten birden fazla filtre eklenebilir (örn: Proje A + Proje B)
- **Otomatik arama:** Filtre eklenince/kaldırılınca otomatik `search()` çağrılır
- **Varsayılan filtre YOK:** Sayfa açıldığında filtre olmadan yüklenir

### 2. Frontend Değişiklikleri (Her Sayfa İçin)
```javascript
// 1. addFilter() fonksiyonunda aynı tipten filtre kısıtlamasını KALDIR
// ESKİ (SİL):
if (type !== 'personnel' && type !== 'supervisor') {
    self.activeFilters.remove(function(f) { return f.type === type; });
}

// YENİ:
// Tüm filtre tipleri çoklu değer destekler
self.activeFilters.push(filter);
self.selectedFilterType('');
self.search(); // Filtre eklenince otomatik ara

// 2. removeFilter() fonksiyonuna otomatik arama ekle
self.removeFilter = function(filter) {
    self.activeFilters.remove(filter);
    self.search(); // Filtre kaldırılınca otomatik ara
};

// 3. buildFilterParams() fonksiyonunu çoklu değer destekleyecek şekilde güncelle
self.buildFilterParams = function() {
    var params = { page: self.page(), pageSize: self.pageSize() };

    var projectIds = [];
    var customerIds = [];
    // ... diğer array'ler

    self.activeFilters().forEach(function(filter) {
        switch (filter.type) {
            case 'project':
                projectIds.push(filter.value);
                break;
            // ... diğer case'ler
        }
    });

    if (projectIds.length > 0) params.projectIds = projectIds;
    // ... diğer array'leri params'a ekle

    return params;
};
```

### 3. Backend DTO Değişiklikleri
```csharp
// SADECE çoklu property'ler kullan - TEKİL YASAK!
public List<int>? CustomerIds { get; set; }
public List<int>? ProjectIds { get; set; }
public List<int>? OrganizationIds { get; set; }
// ... diğer array'ler

// NormalizeFilters GEREKLI DEĞIL - sadece çoğul kullanıldığında
```

### 4. Backend Service Değişiklikleri
```csharp
// Çoklu filtre sorgusu - Contains() kullan
if (filter.ProjectIds?.Any() == true)
    query = query.Where(e => filter.ProjectIds.Contains(e.ProjectId));

if (filter.CustomerIds?.Any() == true)
    query = query.Where(e => filter.CustomerIds.Contains(e.CustomerId));
// ... diğer filtreler
```

---

## INSTANCE 1: Ana Filtre Sistemleri (Chip-based) ✅ TAMAMLANDI

**Sorumluluk:** Mevcut chip-based filtre sistemlerini standartlaştır

### DÖNÜŞTÜRÜLECEK SAYFALAR (4 adet - HEPSİ):
1. ✅ `CustomerPortal/internalEvaluations.js` + `InternalEvaluations.cshtml`
2. ✅ `CustomerPortal/externalEvaluations.js` + `ExternalEvaluations.cshtml`
3. ✅ `Projects/Index.js` + `Projects/Index.cshtml`
4. ✅ `Assignments/Index.js` + `Assignments/Index.cshtml`

---

## INSTANCE 2: Rapor Sayfaları ✅ TAMAMLANDI

**Sorumluluk:** Liste gösteren rapor sayfalarını standartlaştır

### DÖNÜŞTÜRÜLECEK SAYFALAR (3 adet):
1. ✅ `Reports/Index.js` + `Reports/Index.cshtml`
2. ✅ `Reports/Penalties.js` + `Reports/Penalties.cshtml`
3. ✅ `Reports/Suggestions.js` + `Reports/Suggestions.cshtml`

### ATLANACAK SAYFALAR (4 adet):
- ⏭️ `Reports/PersonnelReportCard.js` - Tek personel seçimi, liste yok
- ⏭️ `Reports/PersonnelQuestionPerformance.js` - Sadece export, liste yok
- ⏭️ `Reports/SurveyResults.js` - Karmaşık modal yapısı
- ⏭️ `Reports/performanceTracking.js` - Dashboard tarzı

---

## INSTANCE 3: CustomerPortal Sayfaları ✅ TAMAMLANDI

**Sorumluluk:** CustomerPortal'daki liste sayfalarını standartlaştır

### DÖNÜŞTÜRÜLECEK SAYFALAR (5 adet):
1. ✅ `CustomerPortal/Evaluations.js` + `Evaluations.cshtml`
2. ✅ `CustomerPortal/supervisors.js` + `Supervisors.cshtml`
3. ✅ `CustomerPortal/suggestions.js` + `Suggestions.cshtml`
4. ✅ `CustomerPortal/penalties.js` + `Penalties.cshtml`
5. ✅ `CustomerPortal/performanceByPeriod.js` + `PerformanceByPeriod.cshtml`

### ATLANACAK SAYFALAR (2 adet):
- ⏭️ `CustomerPortal/personnelReportCard.js` - Tek personel detay sayfası
- ⏭️ `CustomerPortal/dashboard.js` - Dashboard widget'ları

---

## INSTANCE 4: Yönetim Sayfaları ✅ TAMAMLANDI

**Sorumluluk:** Yönetim/CRUD sayfalarını standartlaştır

### DÖNÜŞTÜRÜLECEK SAYFALAR (13 adet - HEPSİ):
1. ✅ `Customers/customers.js` + `Index.cshtml`
2. ✅ `Customers/organizations.js` + `Organizations.cshtml`
3. ✅ `Customers/dealers.js` + `Dealers.cshtml`
4. ✅ `Customers/personnel.js` + `Personnel.cshtml`
5. ✅ `Users/Index.js` + `Index.cshtml`
6. ✅ `InternalAssignments/index.js` + `Index.cshtml`
7. ✅ `Approvals/Index.js` + `Index.cshtml`
8. ✅ `Trainings/Index.js` + `Index.cshtml`
9. ✅ `Meetings/Index.js` + `Index.cshtml`
10. ✅ `Checklists/checklist.js` + `Index.cshtml`
11. ✅ `CustomerPersonnel/customer-personnel.js` + `Index.cshtml`
12. ✅ `CustomerOrganizations/Index.js` + `Index.cshtml`
13. ✅ `Evaluations/Index.js` + `Index.cshtml`

---

## Referans: Listenings/index.js

```javascript
// addFilter() - Çoklu değer desteği
self.activeFilters.push(filter);
self.selectedFilterType('');
self.search(); // Filtre eklenince otomatik ara

// removeFilter() - Otomatik arama
self.removeFilter = function(filter) {
    self.activeFilters.remove(filter);
    self.search(); // Filtre kaldırılınca otomatik ara
};

// buildFilterParams() - Array desteği
self.buildFilterParams = function() {
    var params = { page: self.page(), pageSize: self.pageSize(), ... };

    var customerIds = [];
    var projectIds = [];
    // ... diğer array'ler

    self.activeFilters().forEach(function(filter) {
        switch (filter.type) {
            case 'project':
                projectIds.push(filter.value);
                break;
            // ...
        }
    });

    if (projectIds.length > 0) params.projectIds = projectIds;
    // ...

    return params;
};
```

## Dosya Yolları

```
Frontend JS:
Backend\SecretCustomer.API\wwwroot\js\[Folder]\[file].js

Frontend View:
Backend\SecretCustomer.API\Views\[Folder]\[file].cshtml

Backend DTO:
Backend\SecretCustomer.Core\DTOs\[Folder]\[file].cs

Backend Service:
Backend\SecretCustomer.Services\Services\[file].cs

Backend Controller:
Backend\SecretCustomer.API\Controllers\[file].cs
```
