# SecretCustomer Development Patterns

Bu dosya projedeki standart pattern'leri tanımlar. **YENİ BİR ÖZELLİK EKLENİRKEN BU DOSYA MUTLAKA OKUNMALIDIR.**

---

## 1. View/JavaScript Yapısı (SPA Modal Pattern)

### DOĞRU Pattern (Branches, Users, Checklists gibi)
```
Views/
  ModuleName/
    Index.cshtml          # TEK DOSYA - Liste + Create Modal + Edit Modal + Detail Modal
wwwroot/js/
  ModuleName/
    Index.js              # TEK DOSYA - Tüm ViewModel mantığı
```

### YANLIŞ Pattern (KULLANILMAMALI)
```
Views/
  ModuleName/
    Index.cshtml          # YANLIŞ - Ayrı sayfalara bölünmüş
    Create.cshtml         # YANLIŞ
    Edit.cshtml           # YANLIŞ
    Detail.cshtml         # YANLIŞ
wwwroot/js/
  ModuleName/
    index.js              # YANLIŞ - Ayrı JS dosyalarına bölünmüş
    create.js             # YANLIŞ
    edit.js               # YANLIŞ
    detail.js             # YANLIŞ
```

---

## 2. KnockoutJS Binding Pattern

### DOĞRU - Spesifik element'e bind et
```javascript
$(document).ready(function() {
    ko.applyBindings(new ModuleViewModel(), document.getElementById('module-app'));
});
```

### YANLIŞ - Tüm document'a bind etme
```javascript
ko.applyBindings(new ModuleViewModel());  // YANLIŞ!
```

---

## 3. View Yapısı Template

### Index.cshtml Şablonu:
```html
@{
    ViewData["Title"] = "Modül Başlığı";
}

<div id="module-app" class="container-fluid">
    <!-- Header with Create Button -->
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h2>Modül Başlığı</h2>
        <button class="btn btn-primary" data-bind="click: createNew">
            <i class="bi bi-plus-circle"></i> Yeni Ekle
        </button>
    </div>

    <!-- Loading -->
    <div data-bind="visible: isLoading" class="text-center py-5">
        <div class="spinner-border text-primary"></div>
    </div>

    <!-- Error/Success Messages -->
    <div data-bind="visible: errorMessage" class="alert alert-danger alert-dismissible fade show">
        <span data-bind="text: errorMessage"></span>
        <button type="button" class="btn-close" data-bind="click: function() { errorMessage(''); }"></button>
    </div>

    <!-- Table/List -->
    <div data-bind="visible: !isLoading()">
        <!-- Empty State -->
        <div data-bind="visible: items().length === 0">
            <div class="alert alert-info">
                <i class="bi bi-info-circle"></i> Henüz kayıt bulunmamaktadır.
            </div>
        </div>

        <!-- Data Table -->
        <div data-bind="visible: items().length > 0" class="card shadow">
            <div class="card-body">
                <table class="table table-hover">
                    <!-- ... -->
                </table>
            </div>
        </div>
    </div>

    <!-- Create/Edit Modal (TEK MODAL) -->
    <div class="modal fade" data-bind="css: { show: isModalOpen }, style: { display: isModalOpen() ? 'block' : 'none' }" tabindex="-1">
        <div class="modal-dialog modal-lg">
            <div class="modal-content" data-bind="with: editingItem">
                <div class="modal-header">
                    <h5 class="modal-title">
                        <span data-bind="visible: !id">Yeni Kayıt</span>
                        <span data-bind="visible: id">Kayıt Düzenle</span>
                    </h5>
                    <button type="button" class="btn-close" data-bind="click: $parent.closeModal"></button>
                </div>
                <div class="modal-body">
                    <!-- Form fields -->
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bind="click: $parent.closeModal">İptal</button>
                    <button type="button" class="btn btn-primary" data-bind="click: $parent.save, disable: $parent.isSaving">
                        Kaydet
                    </button>
                </div>
            </div>
        </div>
    </div>
    <div class="modal-backdrop fade" data-bind="css: { show: isModalOpen }, style: { display: isModalOpen() ? 'block' : 'none' }"></div>

    <!-- Delete Confirmation (Shared Partial) -->
    <partial name="_DeleteConfirmationModal" />

</div>

@section Scripts {
    <script src="~/js/Shared/delete-confirmation.js"></script>
    <script src="~/js/ModuleName/Index.js"></script>
}
```

---

## 4. JavaScript ViewModel Şablonu

### Index.js Şablonu:
```javascript
function ModuleViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');

    // Modal state
    self.isModalOpen = ko.observable(false);
    self.editingItem = ko.observable(null);

    // Data
    self.items = ko.observableArray([]);

    // CRUD operations
    self.loadItems = function() {
        self.isLoading(true);
        fetch('/api/module')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.items(data);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.createNew = function() {
        self.editingItem({
            id: null,
            name: ko.observable(''),
            // ... other fields
        });
        self.isModalOpen(true);
    };

    self.editItem = function(item) {
        self.editingItem({
            id: item.id,
            name: ko.observable(item.name),
            // ... other fields
        });
        self.isModalOpen(true);
    };

    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingItem(null);
    };

    self.save = function() {
        var item = self.editingItem();
        var isNew = !item.id;
        var url = isNew ? '/api/module' : '/api/module/' + item.id;
        var method = isNew ? 'POST' : 'PUT';

        self.isSaving(true);
        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                name: item.name(),
                // ... other fields
            })
        })
        .then(function(r) {
            if (r.ok) {
                self.closeModal();
                self.loadItems();
                self.successMessage(isNew ? 'Kayıt eklendi.' : 'Kayıt güncellendi.');
            } else {
                return r.json().then(function(err) {
                    self.errorMessage(err.message || 'Bir hata oluştu.');
                });
            }
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    self.deleteItem = function(item) {
        if (confirm('Silmek istediğinizden emin misiniz?')) {
            fetch('/api/module/' + item.id, { method: 'DELETE' })
                .then(function(r) {
                    if (r.ok) {
                        self.loadItems();
                        self.successMessage('Kayıt silindi.');
                    }
                });
        }
    };

    // Initialize
    self.loadItems();
}

// DOĞRU BINDING - Spesifik element'e
$(document).ready(function() {
    ko.applyBindings(new ModuleViewModel(), document.getElementById('module-app'));
});
```

---

## 5. API Controller Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ModuleApiController : ControllerBase
{
    // GET /api/module - Liste
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] FilterDto filter)

    // GET /api/module/{id} - Tekil
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)

    // POST /api/module - Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDto dto)

    // PUT /api/module/{id} - Update
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDto dto)

    // DELETE /api/module/{id} - Delete
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
}
```

---

## 6. MVC Controller Pattern (View Controller)

```csharp
[Authorize]
public class ModuleController : Controller
{
    // TEK ACTION - Index
    public IActionResult Index()
    {
        return View();
    }
}
```

**NOT:** Create, Edit, Detail gibi ayrı action'lar OLMAMALI. Her şey Index içinde modal ile yapılmalı.

---

## 7. Rapor Sayfaları İçin Pattern

Rapor sayfaları Index.cshtml pattern'i izler ama modal yerine filtre ve tablo içerir:

```html
<div id="report-app">
    <!-- Filters -->
    <div class="card mb-4">
        <div class="card-header">Filtreler</div>
        <div class="card-body">
            <!-- Filter controls -->
        </div>
    </div>

    <!-- Summary Cards (optional) -->
    <!-- Data Table -->
    <!-- Pagination (if needed) -->
</div>
```

---

## 8. Düzeltilmiş Modüller

Bu modüller doğru SPA Modal Pattern'e dönüştürüldü:

- [x] Calls (tek Index.cshtml + Index.js)
- [x] Trainings (tek Index.cshtml + Index.js)
- [x] Meetings (tek Index.cshtml + Index.js)
- [x] Approvals (tek Index.cshtml + Index.js)
- [x] Evaluations (tek Index.cshtml + Index.js, modal-fullscreen kullanıyor)
- [ ] Notifications (Settings ayrı kalabilir)

---

## 9. Doğru Yapılmış Modüller (Referans)

Bu modüller doğru pattern kullanıyor:

- Branches - Tek Index.cshtml + modal
- Users - Tek Index.cshtml + modal
- Checklists - Tek Index.cshtml + modal
- Customers - Tek Index.cshtml + modal
- FieldWorkers - Tek Index.cshtml + modal
- Personnel - Tek Index.cshtml + modal
- VisitDetails - Tek Index.cshtml + tab yapısı + modal (Sektör ve Alan Tanımları)

---

## 10. Visit Details - Dinamik Alan Sistemi

Ziyaret detayları için dinamik alan sistemi (EAV - Entity-Attribute-Value pattern).

### Tablo Yapısı

```
VisitSectors (Sektör Tanımları)
├── Id, Code, Name, Description, IconClass, SortOrder, IsActive
│
VisitFieldDefinitions (Alan Tanımları)
├── Id, SectorId (nullable = ortak alan), Code, Name
├── FieldType (Int, Decimal, Bool, String, DateTime, Rating)
├── Category (Time, Staff, Facility, General, Sector)
├── IsRequired, MaxRating, MaxLength, MinValue, MaxValue
├── Placeholder, HelpText, SortOrder, IsActive
│
VisitDetailValues (Değerler - EAV)
├── Id, CustomerVisitId, FieldDefinitionId
├── IntValue, DecimalValue, BoolValue, StringValue, DateTimeValue
```

### Kullanım

**Sektör Oluşturma:**
```
POST /api/visit-details/sectors
{ "code": "BANK", "name": "Banka", "iconClass": "bi-bank", "isActive": true }
```

**Alan Tanımı Oluşturma:**
```
POST /api/visit-details/fields
{
  "sectorId": null,  // null = tüm sektörlerde geçerli ortak alan
  "code": "wait_time",
  "name": "Bekleme Süresi (dk)",
  "fieldType": 0,    // Int
  "category": 0,     // Time
  "isRequired": true
}
```

**Ziyaret Detayı Kaydetme:**
```
POST /api/visit-details/values
{
  "customerVisitId": "...",
  "values": [
    { "fieldDefinitionId": "...", "value": 15 },
    { "fieldDefinitionId": "...", "value": true }
  ]
}
```

### API Endpoints

| Endpoint | Açıklama |
|----------|----------|
| `GET /api/visit-details/sectors` | Tüm sektörler |
| `GET /api/visit-details/fields` | Tüm alan tanımları |
| `GET /api/visit-details/fields/sector/{id}` | Sektöre özel alanlar |
| `GET /api/visit-details/fields/for-visit?sectorId=` | Ziyaret için geçerli alanlar (ortak + sektöre özel) |
| `GET /api/visit-details/values/{customerVisitId}` | Ziyaret detayları |
| `POST /api/visit-details/values` | Toplu değer kaydetme |
| `GET /api/visit-details/statistics/{fieldId}` | Alan istatistikleri |

### Yönetim UI

`/VisitDetails/Index` - Admin only
- **Sektörler Tab:** Sektör CRUD
- **Alan Tanımları Tab:** Alan tanımı CRUD, sektöre göre filtreleme

---

## 11. Kütüphane Kullanımı (Offline Uyumluluk)

### KESİNLİKLE CDN KULLANILMAZ!

Uygulama offline çalışmalıdır. Tüm kütüphaneler `wwwroot/lib/` altında yerel olarak bulunmalıdır.

### YANLIŞ - CDN Link Kullanımı
```html
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
<script src="https://cdnjs.cloudflare.com/ajax/libs/toastr.js/latest/toastr.min.js"></script>
```

### DOĞRU - Yerel Dosya Kullanımı
```html
<link href="~/lib/bootstrap/bootstrap.min.css" rel="stylesheet">
<script src="~/lib/toastr/toastr.min.js"></script>
```

### Mevcut Yerel Kütüphaneler
```
wwwroot/lib/
  bootstrap/           # CSS ve JS
  bootstrap-icons/     # CSS ve font dosyaları
  chartjs/             # Chart.js
  jquery/              # jQuery
  knockout/            # KnockoutJS
  toastr/              # Toastr notifications
```

### Yeni Kütüphane Ekleme
1. Kütüphane dosyalarını `wwwroot/lib/{library-name}/` altına indir
2. `_Layout.cshtml`'de yerel path kullan
3. CDN referansı KULLANMA

---

## 12. Onay Modalları (Confirmation)

### Native confirm() KULLANILMAZ!

Browser'ın native `confirm()` popup'ı yerine Bootstrap modal kullanılmalıdır.

### YANLIŞ
```javascript
if (confirm('Silmek istediğinize emin misiniz?')) {
    // işlem
}
```

### DOĞRU - Shared Modal Kullanımı
```javascript
// Silme onayı için
showDeleteConfirm('Kayıt adı', function() {
    // silme işlemi
});

// Genel onay için
showConfirmModal({
    title: 'Onay Başlığı',
    message: 'Onay mesajı',
    type: 'warning',  // warning, danger, info, success
    confirmText: 'Onayla',
    confirmIcon: 'bi-check',
    onConfirm: function() {
        // onaylanan işlem
    }
});
```

### Shared Modal Dosyaları
- **Modal HTML:** `_Layout.cshtml` içinde `#sharedConfirmModal`
- **JS Helper:** `wwwroot/js/shared/confirm-modal.js`

---

## 13. Migration Kuralları (Entity Framework Core)

### Migration Oluşturma
```bash
# Proje kök dizininde çalıştır:
dotnet ef migrations add MigrationIsmi --project Backend/SecretCustomer.Data --startup-project Backend/SecretCustomer.API
```

### Migration Uygulama (Database Update)
```bash
dotnet ef database update --project Backend/SecretCustomer.Data --startup-project Backend/SecretCustomer.API
```

### Migration İptali/Geri Alma
```bash
# Son migration'ı sil (uygulanmadan önce)
dotnet ef migrations remove --project Backend/SecretCustomer.Data --startup-project Backend/SecretCustomer.API

# Belirli migration'a geri dön
dotnet ef database update MigrationIsmi --project Backend/SecretCustomer.Data --startup-project Backend/SecretCustomer.API
```

### ÖNEMLİ KURALLAR
1. **Migration ismi açıklayıcı olmalı:** `AddMustChangePasswordToUser`, `RemoveFieldWorkerEntity`
2. **Her entity değişikliği için migration oluştur**
3. **Migration oluşturduktan sonra database update yap**
4. **Production'da otomatik migration AÇIK:** `Program.cs`'de `db.Database.Migrate()` var
5. **Migration dosyalarını silme!** Sadece `migrations remove` komutu kullan

### Connection String
PostgreSQL bağlantısı `appsettings.json` ve `appsettings.Development.json`'da tanımlı.

---

## 14. JavaScript Localization Pattern (Çok Dilli Destek)

### Genel Bilgi

Proje çok dilli yapıyı destekler:
- **Server-side:** `Html.T("Key", "Fallback")` - Razor view'larda
- **Client-side:** `T("Key", "Fallback")` - JavaScript'te

### JavaScript'te Çeviri Kullanımı

Her JS modülü kendi kullandığı çeviri key'lerini **başlangıçta** yüklemeli:

```javascript
// Modülde kullanılan tüm çeviri key'leri
var TRANSLATION_KEYS = [
    'Module.LoadError',
    'Module.SaveSuccess',
    'Module.DeleteConfirm',
    'Validation.Required',
    'Common.Confirm',
    // ... modülde kullanılan tüm T() key'leri
];

// Çevirileri yükle, SONRA ViewModel başlat
$(document).ready(function () {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new ModuleViewModel(), document.getElementById('module-app'));
    });
});
```

### YANLIŞ - Çevirileri yüklemeden kullanma
```javascript
$(document).ready(function () {
    ko.applyBindings(new ModuleViewModel(), document.getElementById('module-app'));
});
// T() çağrıları sadece fallback değerleri döner!
```

### DOĞRU - Önce çevirileri yükle
```javascript
$(document).ready(function () {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new ModuleViewModel(), document.getElementById('module-app'));
    });
});
```

### Key Listesi Nasıl Oluşturulur?

1. JS dosyasında tüm `T('...')` çağrılarını bul
2. `confirm-modal.js` kullanılıyorsa o key'leri de ekle:
   - `Confirm.Title`, `Confirm.Message`, `Confirm.DeleteTitle`
   - `Confirm.DeleteMessage`, `Confirm.YesDelete`, `Confirm.CancelTitle`
   - `Confirm.CancelMessage`, `Confirm.YesCancel`
   - `Common.Confirm`, `Common.OK`

### API Endpoint

```
POST /api/localization/resources/batch
Body: { "keys": ["Key1", "Key2", ...] }
Response: { "Key1": "Çeviri1", "Key2": "Çeviri2", ... }
```

### Avantajları

- ✅ Sadece ihtiyaç duyulan key'ler yüklenir (tüm çeviriler değil)
- ✅ Hızlı - küçük payload
- ✅ Dil değiştiğinde doğru çeviriler gelir
- ✅ Timing sorunu yok - çeviriler yüklenmeden ViewModel başlamaz

### Örnek Modül: Languages/index.js

Referans olarak `wwwroot/js/Languages/index.js` dosyasına bakılabilir.

---

## ÖZET

1. **Her modül TEK Index.cshtml ile çalışır**
2. **Create/Edit/Detail işlemleri MODAL ile yapılır**
3. **ko.applyBindings MUTLAKA spesifik div'e bağlanır**
4. **Ayrı sayfa (Create.cshtml, Edit.cshtml, Detail.cshtml) OLMAZ**
5. **CDN KULLANILMAZ - Tüm kütüphaneler yerel olmalı**
6. **Native confirm() KULLANILMAZ - showConfirmModal() kullan**
7. **JS'de T() kullanımı için önce Localization.loadKeys() çağır**
