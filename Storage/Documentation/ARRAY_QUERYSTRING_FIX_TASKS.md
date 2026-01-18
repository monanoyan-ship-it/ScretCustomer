# Array Query String Düzeltme Görevleri

> **Dosya Yolu:** `Storage/Documentation/ARRAY_QUERYSTRING_FIX_TASKS.md`
> **Tarih:** 2026-01-18

## !!! TALİMAT !!!

**SORU SORMA. DİREKT YAP. HATA ÇIKARSA DÜZELT.**

---

## SORUN

GET isteklerinde array parametreleri `projectIds=1,10` şeklinde gidiyor.
Backend bunu yanlış parse ediyor (string olarak "1,10" alıyor).

**Olması gereken:** `projectIds=1&projectIds=10`

---

## ÇÖZÜM

URL oluştururken array kontrolü ekle:

```javascript
Object.keys(params).forEach(function(key) {
    var value = params[key];
    if (Array.isArray(value)) {
        value.forEach(function(v) {
            url += '&' + key + '=' + encodeURIComponent(v);
        });
    } else {
        url += '&' + key + '=' + encodeURIComponent(value);
    }
});
```

---

## INSTANCE 1

### Dosyalar:
1. `wwwroot/js/CustomerPortal/externalEvaluations.js` → ✅ ZATEN YAPILDI
2. `wwwroot/js/CustomerPortal/internalEvaluations.js` → Line 342, `loadEvaluations()` içinde düzelt
3. `wwwroot/js/CustomerPortal/penalties.js` → `buildQueryParams()` ve URL kullanımı kontrol et
4. `wwwroot/js/CustomerPortal/suggestions.js` → `buildQueryParams()` kontrol et
5. `wwwroot/js/CustomerPortal/performanceByPeriod.js` → `buildQueryParams()` kontrol et
6. `wwwroot/js/CustomerPortal/supervisors.js` → `buildQueryParams()` kontrol et

### Ne yapacaksın:
1. Dosyayı aç
2. `Object.keys(params).forEach` veya `buildQueryParams` bul
3. Array değişken varsa (xxxIds gibi), yukarıdaki pattern'i uygula
4. Kaydet

---

## INSTANCE 2

### Dosyalar:
1. `wwwroot/js/Reports/Suggestions.js` → `buildQueryParams()` düzelt
2. `wwwroot/js/Reports/Penalties.js` → `buildQueryParams()` düzelt

### Ne yapacaksın:
1. Dosyayı aç
2. `buildQueryParams()` fonksiyonunu bul
3. Array parametreleri (xxxIds) varsa her elemanı ayrı parametre olarak ekle
4. Kaydet

**NOT:** `Reports/Index.js` POST kullanıyor, ona DOKUNMA.

---

## INSTANCE 3

### Dosyalar:
1. `wwwroot/js/Listenings/index.js` → `buildFilterParams()` ve URL kullanımı kontrol et
2. `wwwroot/js/Assignments/Index.js` → Filtreleme GET mi POST mu kontrol et

### Ne yapacaksın:
1. Dosyayı aç
2. `buildFilterParams()` veya params kullanımını bul
3. GET isteği + array varsa düzelt
4. POST + JSON.stringify varsa → DOKUNMA (düzgün çalışıyor)
5. Kaydet

---

## INSTANCE 4

### Dosyalar:
1. `wwwroot/js/Customers/customers.js`
2. `wwwroot/js/Customers/dealers.js`
3. `wwwroot/js/CustomerPersonnel/customer-personnel.js`
4. `wwwroot/js/InternalAssignments/index.js`
5. `wwwroot/js/Checklists/checklist.js`
6. `wwwroot/js/Approvals/Index.js`
7. `wwwroot/js/Meetings/Index.js`
8. `wwwroot/js/Trainings/Index.js`

### Ne yapacaksın:
1. Her dosyayı aç
2. `buildFilterParams()` fonksiyonunu bul
3. GET isteği + array varsa düzelt
4. POST + JSON.stringify varsa → DOKUNMA
5. Kaydet

---

## KONTROL

- POST + JSON.stringify → DOKUNMA (array düzgün gidiyor)
- GET + array → DÜZELT (yukarıdaki pattern)

---

## BİTİNCE

Kısa özet ver: "X dosya düzeltildi, Y dosya POST kullandığı için dokunulmadı"
