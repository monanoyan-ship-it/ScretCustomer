# SecretCustomer Development Patterns

Bu dosya projedeki standart pattern'leri tanımlar. **YENİ BİR ÖZELLİK EKLENİRKEN BU DOSYA MUTLAKA OKUNMALIDIR.**

---

## 🌐 Uygulama Portları

- **Development Port:** `5004`
- **URL:** `http://localhost:5004`
- **ÖNEMLİ:** Uygulama CLI'dan başlatılmamalı, kullanıcı kendi test eder.

---

## ⚠️ EN ÖNEMLİ KURAL: YALAN SÖYLEMEK YOK

- **"Bilmiyorum, kontrol edeyim"** de
- **"Emin değilim, bakayım"** de
- Kısmi bilgiyle kesin konuşma
- **Hızlı değil, DOĞRU iş yap**
- Yalan = zaman hırsızlığı (kullanıcı test eder, hata bulur, geri döner, düzeltirsin = 3x zaman)
- "Yaptım" demeden önce MUTLAKA kontrol et
- Bir şeyi "her yerde düzelttim" diyorsan, grep ile kontrol et

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

### ⚠️ ÖNEMLİ: Layout'a localization.js Dahil Edilmeli

`Localization.loadKeys()` kullanabilmek için layout dosyasında `localization.js` dahil edilmelidir:

```html
<!-- Layout dosyasında (örn: _Layout.cshtml, _CustomerLayout.cshtml) -->
<script src="~/js/shared/localization.js"></script>
```

**Bu olmadan `Localization is not defined` hatası alınır!**

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

## 15. Paralel Sayfalar (Aynı İşlevi Farklı Kullanıcılara Sunan Sayfalar)

Bazı sayfalar farklı kullanıcı grupları için aynı/benzer işlevi sunar. Bu sayfalar **birlikte güncellenmeli**:

### Evaluations (Değerlendirme) Sayfaları

| Sayfa | Layout | Kullanıcılar | JS Dosyası |
|-------|--------|--------------|------------|
| `/Evaluations/Index` | `_Layout.cshtml` | Admin, QualitySpecialist, FieldWorker | `Evaluations/Index.js` |
| `/CustomerPortal/Evaluations` | `_CustomerLayout.cshtml` | CustomerPersonnel | `CustomerPortal/evaluations.js` |

**⚠️ Bu iki sayfa neredeyse aynı yapıda!** Birine özellik eklendiğinde diğerine de eklenmeli:
- Atamalar listesi
- Değerlendirme ekleme/düzenleme modalı
- Proje dosyaları indirme modalı
- Filtreler ve arama

### Sidebar'lar

| Dosya | Kullanıcılar |
|-------|--------------|
| `_Sidebar.cshtml` (partial) | Admin, QualitySpecialist, FieldWorker |
| `_CustomerLayout.cshtml` (inline) | CustomerPersonnel |

### Kullanılmayan/Ölü Sayfalar

| Sayfa | Durum | Not |
|-------|-------|-----|
| `/MyAssignments` | ❌ Sidebar'da linki yok | FieldWorker için tasarlanmış ama kullanılmıyor |

---

## 19. Popup Pattern (Büyük Formlar İçin)

Modal yerine ayrı pencerede açılan popup pattern. Büyük ve karmaşık formlar için (örn: Email Şablonları, Personel Yönetimi) kullanılır.

### Ne Zaman Popup Kullanılmalı?

- Form çok fazla alan içeriyorsa (modal'a sığmıyorsa)
- WYSIWYG editor veya kod editörü gibi büyük bileşenler varsa
- Kullanıcının ana sayfayı görmesi gerekiyorsa (karşılaştırma için)
- Alt detay yönetimi için (Müşteri → Personel, Organizasyonlar gibi)

### Yapı

```
Views/
  ModuleName/
    Index.cshtml          # Liste sayfası (ana layout)
    Popup.cshtml          # Popup view (_LayoutPopup layout)
wwwroot/js/
  ModuleName/
    index.js              # Liste VM + popup açma
    popup.js              # Popup VM
```

### Index'ten Popup Açma

```javascript
// index.js
self.openPopup = function(item) {
    var url = '/ModuleName/Popup/' + item.id;
    var popup = window.open(url, 'module_' + item.id, 'width=1200,height=750,scrollbars=yes,resizable=yes');
    if (popup) popup.focus();
};

// Global refresh fonksiyonu - popup kapanınca çağrılır
function refreshList() {
    if (window.vm) {
        window.vm.loadItems();
    }
}

var vm = null;
$(document).ready(function() {
    vm = new IndexViewModel();
    ko.applyBindings(vm, document.getElementById('module-app'));
});
```

### Popup View Template

```html
@{
    Layout = "_LayoutPopup";
    ViewData["Title"] = "Popup Başlığı";
    var itemId = ViewBag.ItemId;
}

<div id="popup-app">
    <!-- Header -->
    <div class="popup-header">
        <h4>
            <i class="bi bi-icon me-2"></i>Popup Başlığı
        </h4>
        <div class="d-flex gap-2">
            <button class="btn btn-primary btn-sm" data-bind="click: save, disable: isSaving">
                <span data-bind="visible: !isSaving()"><i class="bi bi-save"></i> Kaydet</span>
                <span data-bind="visible: isSaving"><span class="spinner-border spinner-border-sm"></span></span>
            </button>
            <button class="btn btn-secondary btn-sm" onclick="window.close()">
                <i class="bi bi-x-lg"></i> Kapat
            </button>
        </div>
    </div>

    <!-- Content -->
    <div class="card shadow-sm">
        <!-- Form içeriği -->
    </div>
</div>

<script>
    window.popupConfig = {
        itemId: @(itemId ?? "null")
    };
</script>

@section Scripts {
    <script src="~/js/ModuleName/popup.js"></script>
}
```

### Popup JS Template

```javascript
// popup.js
function PopupViewModel() {
    var self = this;
    var config = window.popupConfig || {};

    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);

    // Save and notify opener
    self.save = function() {
        self.isSaving(true);
        fetch('/api/module/' + config.itemId, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ /* data */ }),
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.success) {
                toastr.success(result.message);
                // Opener'a bildir
                if (window.opener && window.opener.refreshList) {
                    window.opener.refreshList();
                }
                setTimeout(function() { window.close(); }, 1000);
            } else {
                toastr.error(result.message);
            }
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Initialize
    self.loadData();
}

$(document).ready(function() {
    ko.applyBindings(new PopupViewModel(), document.getElementById('popup-app'));
});
```

### Popup Pattern Kullanan Modüller

- **EmailTemplates** - Editor.cshtml (WYSIWYG editor içerir)
- **Customers/Personnel** - Personnel.cshtml
- **Customers/Organizations** - Organizations.cshtml
- **Customers/Dealers** - Dealers.cshtml
- **Survey/Form** - Halka açık anket formu

### Popup vs Modal Karşılaştırma

| Özellik | Modal | Popup |
|---------|-------|-------|
| Form boyutu | Küçük-orta | Büyük |
| Ana sayfa görünürlüğü | Kapalı (backdrop) | Açık (yan yana) |
| Çoklu açılabilirlik | Hayır | Evet |
| Layout | Aynı sayfa | _LayoutPopup |
| WYSIWYG/Kod editörü | Zor | Kolay |
| Kullanım alanı | Basit CRUD | Karmaşık formlar |

---

## ÖZET

1. **Her modül TEK Index.cshtml ile çalışır**
2. **Basit formlar MODAL ile, büyük/karmaşık formlar POPUP ile yapılır**
3. **ko.applyBindings MUTLAKA spesifik div'e bağlanır**
4. **Ayrı sayfa (Create.cshtml, Edit.cshtml, Detail.cshtml) OLMAZ** (Popup hariç)
5. **CDN KULLANILMAZ - Tüm kütüphaneler yerel olmalı**
6. **Native confirm() KULLANILMAZ - showConfirmModal() kullan**
7. **JS'de T() kullanımı için önce Localization.loadKeys() çağır**
8. **Inline alert KULLANILMAZ - Hata/başarı mesajları toastr ile sağ üstte gösterilir**
9. **Form input'larında AUTOCOMPLETE attribute'u MUTLAKA kullan:**
   - Password input: `autocomplete="new-password"`
   - Search/filter input: `autocomplete="off"`
   - Username input: `autocomplete="username"`
   - **NEDEN:** Tarayıcı autocomplete'i yanlış input'lara değer yazabilir ve KnockoutJS observable'ları bozabilir!
10. **Detay/Modal açma işlemlerinde LİSTE YENİLENMEZ:**
    - Detay görüntüleme sadece modal açar, liste değişmez
    - Delete sonrası: `self.items.remove(item)` kullan, `loadItems()` çağırma
    - Update sonrası: API'den dönen veriyle listedeki öğeyi güncelle
    - **NEDEN:** Gereksiz API çağrısı, kullanıcı deneyimini bozar, scroll pozisyonu kaybolur
11. **Tarih filtreleri için KISAYOL DROPDOWN kullan:**
    - Bugün, Dün, Bu Hafta, Geçen Hafta, Bu Ay, Geçen Ay, Son 3 Ay, Son 6 Ay, Bu Yıl, Geçen Yıl seçenekleri
    - Gruplu dropdown yapısı (Gün, Hafta, Ay, Yıl başlıklarıyla)
    - `calculateDateRange(rangeType)` helper fonksiyonu kullan
    - Kullanıcı deneyimini iyileştirir, manuel tarih girişi azaltır
12. **INLINE CSS YASAKTIR:**
    - Global stiller `app.css`'de
    - Layout stiller `css/layouts/` klasöründe
    - Sayfa-spesifik stiller `css/pages/` klasöründe
    - Widget stiller `css/widgets/` klasöründe
    - **`style=""` attribute'u kullanma - CSS class oluştur!**

---

## 19. Tarih Filtresi Kısayol Pattern'i

Tarih aralığı filtresi olan sayfalarda kısayol butonları ekle.

### İki Kullanım Şekli

| Pattern | Kullanım Yeri | Davranış |
|---------|---------------|----------|
| `setDateRange()` | Dropdown içi | Sadece değer atar, aramayı tetiklemez |
| `setQuickDateRange()` | Dropdown dışı butonlar | Değer atar VE aramayı tetikler |

### ⛔ AYNI SORGU İÇİN İKİSİNİ BİRDEN KULLANMA!

Bir sayfada hem "Filtre Ekle" dropdown'ı içinde tarih filtresi hem de ayrı "Hızlı Tarih" dropdown'ı OLMAMALI. İkisi de aynı işi yapar (tarih chip'i ekler). Kullanıcı için kafa karıştırıcıdır.

**DOĞRU:** Sadece "Filtre Ekle" dropdown'ı içinde `setDateRange()` kullan
**YANLIŞ:** Hem "Filtre Ekle" içinde tarih, hem ayrı "Hızlı Tarih" dropdown'ı

### JavaScript Helper Fonksiyonları

```javascript
// Tarih etiketleri
self.dateRangeLabels = {
    'today': 'Bugün',
    'yesterday': 'Dün',
    'thisWeek': 'Bu Hafta',
    'lastWeek': 'Geçen Hafta',
    'thisMonth': 'Bu Ay',
    'lastMonth': 'Geçen Ay',
    'last3Months': 'Son 3 Ay',
    'last6Months': 'Son 6 Ay',
    'thisYear': 'Bu Yıl',
    'lastYear': 'Geçen Yıl'
};

// Calculate date range from type
self.calculateDateRange = function(rangeType) {
    var today = new Date();
    var start, end;

    if (rangeType === 'today') {
        start = end = today.toISOString().split('T')[0];
    } else if (rangeType === 'yesterday') {
        var yesterday = new Date(today);
        yesterday.setDate(yesterday.getDate() - 1);
        start = end = yesterday.toISOString().split('T')[0];
    } else if (rangeType === 'thisWeek') {
        var dayOfWeek = today.getDay();
        var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
        var weekStart = new Date(today);
        weekStart.setDate(diff);
        start = weekStart.toISOString().split('T')[0];
        end = today.toISOString().split('T')[0];
    } else if (rangeType === 'lastWeek') {
        var dayOfWeek = today.getDay();
        var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
        var lastWeekEnd = new Date(today);
        lastWeekEnd.setDate(diff - 1);
        var lastWeekStart = new Date(lastWeekEnd);
        lastWeekStart.setDate(lastWeekEnd.getDate() - 6);
        start = lastWeekStart.toISOString().split('T')[0];
        end = lastWeekEnd.toISOString().split('T')[0];
    } else if (rangeType === 'thisMonth') {
        start = new Date(today.getFullYear(), today.getMonth(), 1).toISOString().split('T')[0];
        end = today.toISOString().split('T')[0];
    } else if (rangeType === 'lastMonth') {
        start = new Date(today.getFullYear(), today.getMonth() - 1, 1).toISOString().split('T')[0];
        end = new Date(today.getFullYear(), today.getMonth(), 0).toISOString().split('T')[0];
    } else if (rangeType === 'last3Months') {
        start = new Date(today.getFullYear(), today.getMonth() - 2, 1).toISOString().split('T')[0];
        end = today.toISOString().split('T')[0];
    } else if (rangeType === 'last6Months') {
        start = new Date(today.getFullYear(), today.getMonth() - 5, 1).toISOString().split('T')[0];
        end = today.toISOString().split('T')[0];
    } else if (rangeType === 'thisYear') {
        start = new Date(today.getFullYear(), 0, 1).toISOString().split('T')[0];
        end = today.toISOString().split('T')[0];
    } else if (rangeType === 'lastYear') {
        start = new Date(today.getFullYear() - 1, 0, 1).toISOString().split('T')[0];
        end = new Date(today.getFullYear() - 1, 11, 31).toISOString().split('T')[0];
    }

    return { start: start, end: end };
};

// Dropdown içi - sadece değer atar
self.setDateRange = function(rangeType) {
    var range = self.calculateDateRange(rangeType);
    self.tempFilter.startDate(range.start);
    self.tempFilter.endDate(range.end);
    self.tempFilter.dateRangeType(rangeType);
};

// Hızlı erişim butonları - değer atar VE aramayı tetikler
self.setQuickDateRange = function(rangeType) {
    var range = self.calculateDateRange(rangeType);
    var displayValue = self.dateRangeLabels[rangeType] || (range.start + ' - ' + range.end);

    self.activeFilters.push({
        type: 'dateRange',
        value: null,
        startDate: range.start,
        endDate: range.end,
        dateRangeType: rangeType,
        label: 'Tarih',
        displayValue: displayValue
    });

    self.search();  // Otomatik ara
};
```

### HTML - Hızlı Erişim Butonları (Dropdown Dışı)

```html
<!-- Quick Access Row - Filtre kartının içinde, en altta -->
<div class="border-top pt-2 mt-2">
    <div class="d-flex flex-wrap align-items-center gap-2">
        <!-- Quick CallId Search (opsiyonel) -->
        <div class="input-group input-group-sm" style="max-width: 200px;">
            <input type="text" class="form-control" data-bind="value: quickCallId, valueUpdate: 'afterkeydown'" placeholder="Çağrı ID...">
            <button class="btn btn-outline-success" type="button" data-bind="click: searchByCallId">
                <i class="bi bi-search"></i>
            </button>
        </div>
        <span class="text-muted">|</span>
        <!-- Quick Date Range Buttons -->
        <div class="dropdown">
            <button class="btn btn-outline-secondary btn-sm dropdown-toggle" type="button" data-bs-toggle="dropdown">Hızlı Tarih</button>
            <ul class="dropdown-menu">
                <li><h6 class="dropdown-header">Gün</h6></li>
                <li><a class="dropdown-item" href="#" data-bind="click: function() { setQuickDateRange('today'); }">Bugün</a></li>
                <li><a class="dropdown-item" href="#" data-bind="click: function() { setQuickDateRange('yesterday'); }">Dün</a></li>
                <li><hr class="dropdown-divider"></li>
                <li><h6 class="dropdown-header">Hafta</h6></li>
                <li><a class="dropdown-item" href="#" data-bind="click: function() { setQuickDateRange('thisWeek'); }">Bu Hafta</a></li>
                <li><a class="dropdown-item" href="#" data-bind="click: function() { setQuickDateRange('lastWeek'); }">Geçen Hafta</a></li>
                <li><hr class="dropdown-divider"></li>
                <li><h6 class="dropdown-header">Ay</h6></li>
                <li><a class="dropdown-item" href="#" data-bind="click: function() { setQuickDateRange('thisMonth'); }">Bu Ay</a></li>
                <li><a class="dropdown-item" href="#" data-bind="click: function() { setQuickDateRange('lastMonth'); }">Geçen Ay</a></li>
                <li><a class="dropdown-item" href="#" data-bind="click: function() { setQuickDateRange('last3Months'); }">Son 3 Ay</a></li>
                <li><a class="dropdown-item" href="#" data-bind="click: function() { setQuickDateRange('last6Months'); }">Son 6 Ay</a></li>
                <li><hr class="dropdown-divider"></li>
                <li><h6 class="dropdown-header">Yıl</h6></li>
                <li><a class="dropdown-item" href="#" data-bind="click: function() { setQuickDateRange('thisYear'); }">Bu Yıl</a></li>
                <li><a class="dropdown-item" href="#" data-bind="click: function() { setQuickDateRange('lastYear'); }">Geçen Yıl</a></li>
            </ul>
        </div>
    </div>
</div>
```

### HTML - Dropdown İçi Butonlar

```html
<!-- Date Range seçeneği içinde -->
<div data-bind="visible: selectedFilterType() === 'dateRange'" class="mb-2">
    <div class="d-flex gap-1 mb-1">
        <input type="date" class="form-control form-control-sm" data-bind="value: tempFilter.startDate">
        <span class="align-self-center">-</span>
        <input type="date" class="form-control form-control-sm" data-bind="value: tempFilter.endDate">
    </div>
    <div class="dropdown">
        <button class="btn btn-outline-secondary btn-sm dropdown-toggle" type="button" data-bs-toggle="dropdown">
            <span data-bind="text: tempFilter.dateRangeType() ? dateRangeLabels[tempFilter.dateRangeType()] : 'Hızlı Seç'"></span>
        </button>
        <ul class="dropdown-menu">
            <li><h6 class="dropdown-header">Gün</h6></li>
            <li><a class="dropdown-item" href="#" data-bind="click: function() { setDateRange('today'); }, css: { active: tempFilter.dateRangeType() === 'today' }">Bugün</a></li>
            <li><a class="dropdown-item" href="#" data-bind="click: function() { setDateRange('yesterday'); }, css: { active: tempFilter.dateRangeType() === 'yesterday' }">Dün</a></li>
            <li><hr class="dropdown-divider"></li>
            <li><h6 class="dropdown-header">Hafta</h6></li>
            <li><a class="dropdown-item" href="#" data-bind="click: function() { setDateRange('thisWeek'); }, css: { active: tempFilter.dateRangeType() === 'thisWeek' }">Bu Hafta</a></li>
            <li><a class="dropdown-item" href="#" data-bind="click: function() { setDateRange('lastWeek'); }, css: { active: tempFilter.dateRangeType() === 'lastWeek' }">Geçen Hafta</a></li>
            <li><hr class="dropdown-divider"></li>
            <li><h6 class="dropdown-header">Ay</h6></li>
            <li><a class="dropdown-item" href="#" data-bind="click: function() { setDateRange('thisMonth'); }, css: { active: tempFilter.dateRangeType() === 'thisMonth' }">Bu Ay</a></li>
            <li><a class="dropdown-item" href="#" data-bind="click: function() { setDateRange('lastMonth'); }, css: { active: tempFilter.dateRangeType() === 'lastMonth' }">Geçen Ay</a></li>
            <li><a class="dropdown-item" href="#" data-bind="click: function() { setDateRange('last3Months'); }, css: { active: tempFilter.dateRangeType() === 'last3Months' }">Son 3 Ay</a></li>
            <li><a class="dropdown-item" href="#" data-bind="click: function() { setDateRange('last6Months'); }, css: { active: tempFilter.dateRangeType() === 'last6Months' }">Son 6 Ay</a></li>
            <li><hr class="dropdown-divider"></li>
            <li><h6 class="dropdown-header">Yıl</h6></li>
            <li><a class="dropdown-item" href="#" data-bind="click: function() { setDateRange('thisYear'); }, css: { active: tempFilter.dateRangeType() === 'thisYear' }">Bu Yıl</a></li>
            <li><a class="dropdown-item" href="#" data-bind="click: function() { setDateRange('lastYear'); }, css: { active: tempFilter.dateRangeType() === 'lastYear' }">Geçen Yıl</a></li>
        </ul>
    </div>
</div>
```

### Hızlı CallId Arama (Opsiyonel)

```javascript
// Quick CallId search
self.quickCallId = ko.observable('');
self.searchByCallId = function() {
    var callId = self.quickCallId().trim();
    if (!callId) return;

    // Mevcut callId filtresini kaldır
    self.activeFilters.remove(function(f) { return f.type === 'callId'; });

    self.activeFilters.push({
        type: 'callId',
        value: callId,
        label: 'Çağrı ID',
        displayValue: callId
    });

    self.quickCallId('');
    self.search();
};
```

### Kullanan Sayfalar

- `/CustomerPortal/Evaluations` - İkinci sekme (Dinlemeler/Ziyaretler)
- `/CustomerPortal/ExternalEvaluations`
- `/CustomerPortal/InternalEvaluations`
- `/CustomerPortal/Penalties`
- `/CustomerPortal/Organizations`

---

## 16. PostgreSQL DateTime Kuralları (ÇOK ÖNEMLİ!)

### ⛔ HER YENİ DTO/ENTITY OLUŞTURURKEN BU KURALI UYGULA!

PostgreSQL `timestamp with time zone` tipi sadece UTC DateTime kabul eder. `DateTimeKind.Unspecified` ile kayıt yapmaya çalışırsan hata alırsın:

```
Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone', only UTC is supported.
```

### DOĞRU - DateTime'ı UTC olarak ayarla

```csharp
// DTO'dan gelen DateTime'ı UTC'ye çevir
var entity = new Assignment
{
    DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc),
    CreatedAt = DateTime.UtcNow,  // Yeni kayıtlarda UtcNow kullan
    UpdatedAt = DateTime.UtcNow
};
```

### YANLIŞ - DateTime'ı olduğu gibi kullanma

```csharp
var entity = new Assignment
{
    DueDate = dto.DueDate,  // YANLIŞ! Kind=Unspecified olabilir
    CreatedAt = DateTime.Now  // YANLIŞ! Local time
};
```

### Checklist

Her yeni entity/DTO oluştururken:
1. [ ] `CreatedAt` alanı `DateTime.UtcNow` ile set edilmeli
2. [ ] `UpdatedAt` alanı `DateTime.UtcNow` ile set edilmeli
3. [ ] Frontend'den gelen DateTime'lar `DateTime.SpecifyKind(date, DateTimeKind.Utc)` ile dönüştürülmeli
4. [ ] Nullable DateTime'lar için: `dto.Date.HasValue ? DateTime.SpecifyKind(dto.Date.Value, DateTimeKind.Utc) : null`

---

## 17. KnockoutJS Otomatik Arama Pattern

Arama kutusuna yazıldığında otomatik arama yapmak için Knockout'un binding özelliklerini kullan.

### DOĞRU - View'da event binding ile

```html
<input type="text" class="form-control"
       data-bind="value: searchText, valueUpdate: 'input', event: { input: loadItems }"
       placeholder="Ara..." />
```

**Açıklama:**
- `value: searchText` - Observable'a bağlar
- `valueUpdate: 'input'` - Her karakter değişikliğinde observable güncellenir
- `event: { input: loadItems }` - Her değişiklikte arama fonksiyonu çağrılır

### JavaScript (Sadece observable tanımı yeterli)

```javascript
self.searchText = ko.observable('');

self.loadItems = function() {
    var search = self.searchText();
    // API çağrısı...
};
```

### YANLIŞ - Subscribe ile manuel debounce

```javascript
// Bu gereksiz karmaşıklık!
self._searchTimeout = null;
self.searchText.subscribe(function() {
    if (self._searchTimeout) clearTimeout(self._searchTimeout);
    self._searchTimeout = setTimeout(function() {
        self.loadItems();
    }, 300);
});
```

### Not

Bu pattern her tuşta API çağrısı yapar. Performans kritik ise `rateLimit` extender kullanılabilir:

```javascript
self.searchText = ko.observable('').extend({ rateLimit: { timeout: 300, method: 'notifyWhenChangesStop' } });
```

---

## 18. TypeDefinitions Pattern (Enum'lar Yerine)

### Neden Enum Yerine TypeDefinitions?

- **Localization desteği:** Her tip için `NameResourceKey` ile çoklu dil desteği
- **Zengin metadata:** Icon, CSS class, description gibi ek bilgiler
- **UI için hazır:** Frontend'e doğrudan gönderilebilir
- **Tip güvenliği:** Ids inner class ile const int değerleri

### TypeItem Sınıfı

```csharp
public record TypeItem(
    int Id,
    string SystemName,
    string NameResourceKey,
    string Description,
    string Icon = "",
    string CssClass = "",
    int DisplayOrder = 0,
    bool IsDefault = false,
    bool IsActive = true,
    bool IsSystem = false
);
```

### DOĞRU - TypeDefinitions Pattern

```csharp
// Core/TypeDefinitions.cs içinde
public static class ChecklistTypes
{
    public static readonly TypeItem Standard = new(0, "Standard", "ChecklistType.Standard", "Standart Checklist", "bi-list-check", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Mystery = new(1, "Mystery", "ChecklistType.Mystery", "Gizli Müşteri", "bi-incognito", "bg-warning", 2);
    public static readonly TypeItem Survey = new(2, "Survey", "ChecklistType.Survey", "Anket", "bi-clipboard-data", "bg-info", 3);

    public static IEnumerable<TypeItem> All => new[] { Standard, Mystery, Survey };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string name) => All.FirstOrDefault(x => x.SystemName == name);

    // Sabit ID'ler - DB'de ve kodda kullanılır
    public static class Ids
    {
        public const int Standard = 0;
        public const int Mystery = 1;
        public const int Survey = 2;
    }
}
```

### YANLIŞ - Enum Kullanımı

```csharp
// BU ŞEKİLDE KULLANMA!
public enum ChecklistType
{
    Standard = 0,
    Mystery = 1,
    Survey = 2
}

public class Checklist
{
    public ChecklistType Type { get; set; }  // YANLIŞ
}
```

### Entity'de Kullanım

```csharp
public class Checklist : BaseEntity
{
    // YANLIŞ: public ChecklistType Type { get; set; }

    // DOĞRU: int property + Id suffix
    public int TypeId { get; set; }  // DB'de "TypeId" olarak saklanır
}
```

### DB Column Mapping (Mevcut Enum → TypeDefinitions Dönüşümü)

Mevcut enum column adını korumak için `HasColumnName` kullan:

```csharp
// ApplicationDbContext.cs - OnModelCreating içinde
modelBuilder.Entity<AuditLog>()
    .Property(a => a.LogTypeId).HasColumnName("LogType");

modelBuilder.Entity<AssignmentPeriod>()
    .Property(p => p.StatusId).HasColumnName("Status");

modelBuilder.Entity<AppSettings>()
    .Property(s => s.ValueTypeId).HasColumnName("ValueType");
```

### Service'de Kullanım - Type İsmi Alma (Localized)

```csharp
public class ChecklistService
{
    private readonly ILocalizationService _localizationService;

    // DOĞRU - Async helper metod
    private async Task<string> GetChecklistTypeNameAsync(int typeId)
    {
        var item = ChecklistTypes.GetById(typeId);
        if (item == null) return "";
        // NameResourceKey ile localized isim, Description fallback olarak
        return await _localizationService.GetResourceAsync(item.NameResourceKey, (int?)null, item.Description);
    }

    // YANLIŞ - .GetAwaiter().GetResult() KULLANMA!
    private string GetChecklistTypeName(int typeId)
    {
        var item = ChecklistTypes.GetById(typeId);
        return _localizationService.GetResourceAsync(item.NameResourceKey).GetAwaiter().GetResult(); // YANLIŞ!
    }

    // YANLIŞ - Sadece Description döndürme
    private string GetChecklistTypeName(int typeId)
    {
        var item = ChecklistTypes.GetById(typeId);
        return item?.Description ?? "";  // YANLIŞ! Localization yok
    }
}
```

### Async MapToDto Pattern

Collection içinde async işlem varsa `Select` yerine `foreach` kullan:

```csharp
// DOĞRU - Async MapToDto
private async Task<ChecklistDto> MapToDtoAsync(Checklist checklist)
{
    var questions = new List<QuestionDto>();
    foreach (var q in checklist.Questions.OrderBy(q => q.Order))
    {
        questions.Add(new QuestionDto
        {
            Id = q.Id,
            Text = q.Text,
            // Async localization çağrısı
            ScoringTypeName = await GetScoringTypeNameAsync(q.ScoringTypeId),
            PenaltyTypeName = await GetPenaltyTypeNameAsync(q.PenaltyTypeId)
        });
    }

    return new ChecklistDto
    {
        Id = checklist.Id,
        Name = checklist.Name,
        TypeName = await GetChecklistTypeNameAsync(checklist.TypeId),
        Questions = questions
    };
}

// YANLIŞ - LINQ Select içinde async
private ChecklistDto MapToDto(Checklist checklist)
{
    return new ChecklistDto
    {
        // Select içinde await YAPILAMAZ!
        Questions = checklist.Questions.Select(q => new QuestionDto
        {
            ScoringTypeName = GetScoringTypeName(q.ScoringTypeId)  // Sync metod = GetAwaiter().GetResult() gerekir = YANLIŞ
        }).ToList()
    };
}
```

### Mevcut TypeDefinitions

Projede tanımlı tipler (`Core/TypeDefinitions.cs`):

| Class | Açıklama |
|-------|----------|
| `ChecklistTypes` | Checklist türleri (Standard, Mystery, Survey) |
| `ScoringMethods` | Puanlama yöntemleri (Standard, Weighted, Percentage) |
| `ScoringTypes` | Puanlama türleri (YesNo, Scale, Options) |
| `PenaltyTypes` | Ceza türleri (None, Full, Half, Percentage) |
| `PeriodStatuses` | Dönem durumları (Open, Closed) |
| `LogTypes` | Log türleri (Info, Warning, Error, DataCreate, ...) |
| `SettingValueTypes` | Ayar değer türleri (String, Bool, Int, Decimal, Json, DateTime) |
| `UserRoles` | Kullanıcı rolleri (Admin, QualitySpecialist, FieldWorker, CustomerPortal) |
| `EvaluationStatuses` | Değerlendirme durumları (Draft, Submitted, Approved, Rejected) |
| `ProjectStatuses` | Proje durumları (Draft, Active, Paused, Completed, Cancelled) |

### Checklist: Yeni Enum → TypeDefinitions Dönüşümü

1. [ ] Entity'de enum property'yi `int` olarak değiştir (`EnumName` → `EnumNameId`)
2. [ ] `TypeDefinitions.cs`'e yeni static class ekle (All, GetById, GetBySystemName, Ids)
3. [ ] `ApplicationDbContext.cs`'de `HasColumnName` ile eski column adını koru
4. [ ] Interface'lerde parametre tipini güncelle (`EnumType` → `int`)
5. [ ] Service'lerde helper metod oluştur (`GetEnumNameAsync`)
6. [ ] Helper metod MUTLAKA async olmalı: `await _localizationService.GetResourceAsync(item.NameResourceKey, ...)`
7. [ ] MapToDto metodlarını async yap (`MapToDtoAsync`)
8. [ ] Tüm çağrıları async/await ile güncelle

---

## 14. Proje Tipi Bazlı Raporlama Pattern'i

**Her proje tipi için ayrı rapor controller'ı oluşturulur:**

| Proje Tipi | Controller | Sayfa | Not |
|------------|------------|-------|-----|
| CallAuditing (Çağrı Denetimi) | ReportsApiController | /Reports | Varsayılan (mevcut raporlar) |
| OnlineSurvey (Online Anket) | ReportsApiController (SurveyResults) | /Reports/SurveyResults | Ayrı sayfa |
| MysteryShopping | (ileride) ayrı controller | /Reports/MysteryShopping | - |
| PhysicalAudit | (ileride) ayrı controller | /Reports/PhysicalAudit | - |

### Kurallar:

1. **ReportService varsayılan olarak `ProjectTypeId == CallAuditing` filtreler**
2. Yeni proje tipi raporu eklendiğinde:
   - Yeni controller/action oluştur
   - `== ProjectTypeId` kullan (`!=` DEĞİL - **pozitif filtreleme**)
   - Sidebar'a menü linki ekle
3. **Mevcut Proje Tipleri** (`TypeDefinitions.cs`):
   ```
   MysteryShopping=1, CallAuditing=2, PhysicalAudit=3, OnlineSurvey=4
   CustomerSatisfaction=5, TrainingEvaluation=6, QualityControl=7
   ```

### Örnek Filter Kullanımı:

```csharp
// DOĞRU - Pozitif filtreleme
if (string.IsNullOrEmpty(filter.ProjectType) && !filter.ProjectId.HasValue)
{
    query = query.Where(e => e.Assignment.Project.ProjectTypeId == ProjectTypes.Ids.CallAuditing);
}

// YANLIŞ - Negatif filtreleme (kullanılmamalı)
query = query.Where(e => e.Assignment.Project.ProjectTypeId != ProjectTypes.Ids.OnlineSurvey);
```

---

## 20. Filter Standardization Pattern (ZORUNLU!)

### ⛔ SADECE ÇOĞUL PARAMETRELER KULLANILIR!

Tüm filter DTO'larında parametreler **ÇOĞUL (List<>)** olmalıdır. Tekil parametreler YASAKTIR.

### DOĞRU - FilterDto Yapısı

```csharp
public class ReportFilterDto
{
    // ✅ DOĞRU - Hepsi çoğul (List<>)
    public List<int>? CustomerIds { get; set; }
    public List<int>? ProjectIds { get; set; }
    public List<int>? OrganizationIds { get; set; }
    public List<int>? EvaluatorIds { get; set; }
    public List<string>? ProjectTypes { get; set; }

    // Pagination ve sorting - tekil kalabilir
    public string? SortField { get; set; }
    public string? SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

### YANLIŞ - Tekil Parametreler (KULLANMA!)

```csharp
public class ReportFilterDto
{
    // ❌ YANLIŞ - Tekil parametreler
    public int? CustomerId { get; set; }
    public int? ProjectId { get; set; }

    // ❌ YANLIŞ - İkisi birden (normalization gerektirir)
    public int? CustomerId { get; set; }
    public List<int>? CustomerIds { get; set; }
}
```

### Entity Query Pattern

```csharp
// ✅ DOĞRU - Contains() ve Any() kullan
if (filter.ProjectIds?.Any() == true)
    query = query.Where(e => filter.ProjectIds.Contains(e.ProjectId));

if (filter.CustomerIds?.Any() == true)
    query = query.Where(e => filter.CustomerIds.Contains(e.CustomerId));

// Nullable int için
if (filter.EvaluatorIds?.Any() == true)
    query = query.Where(e => e.EvaluatorId.HasValue && filter.EvaluatorIds.Contains(e.EvaluatorId.Value));

// String arama için (case-insensitive)
if (filter.CustomerIds?.Any() == true)
    query = query.Where(e => e.CustomerName != null &&
        filter.CustomerIds.Any(c => e.CustomerName.ToLower().Contains(c.ToString().ToLower())));
```

### YANLIŞ - Eski Pattern (KULLANMA!)

```csharp
// ❌ YANLIŞ - Tekil parametre kontrolü
if (filter.ProjectId.HasValue)
    query = query.Where(e => e.ProjectId == filter.ProjectId.Value);

// ❌ YANLIŞ - NormalizeFilters() metodu (artık gerekli değil!)
private void NormalizeFilters(FilterDto filter)
{
    if (filter.CustomerId.HasValue && (filter.CustomerIds == null || !filter.CustomerIds.Any()))
        filter.CustomerIds = new List<int> { filter.CustomerId.Value };
}
```

### Controller Pattern

```csharp
// ✅ DOĞRU - FilterDto parametre olarak
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] ReportFilterDto filter)
{
    var result = await _service.GetAllAsync(filter);
    return Ok(result);
}

// ❌ YANLIŞ - Ayrı ayrı tekil parametreler
[HttpGet]
public async Task<IActionResult> GetAll(
    [FromQuery] int? customerId,
    [FromQuery] int? projectId)
```

### Checklist: Yeni Filter Eklerken

1. [ ] DTO'da sadece çoğul parametreler (`List<>`) tanımla
2. [ ] Entity sorgularında `.Any()` ve `.Contains()` kullan
3. [ ] String aramalarda `.ToLower()` ile case-insensitive yap
4. [ ] Controller'da `[FromQuery] FilterDto filter` kullan
5. [ ] NormalizeFilters() metodu YAZMA - gerekli değil!
6. [ ] Nullable int için `.HasValue` kontrolü ekle
7. [ ] **JavaScript'te URLSearchParams kullan** (aşağıdaki pattern)

### ⛔ JavaScript - URLSearchParams ZORUNLU!

Çoklu değer gönderirken **URLSearchParams** kullanılmalıdır. Array + join() KULLANMA!

```javascript
// ✅ DOĞRU - URLSearchParams pattern
self.buildFilterParams = function() {
    var params = new URLSearchParams();

    self.activeFilters().forEach(function(filter) {
        switch (filter.type) {
            case 'project':
                params.append('projectIds', filter.value);  // append() ile çoklu değer
                break;
            case 'customer':
                params.append('customerIds', filter.value);
                break;
            case 'dateRange':
                if (filter.value.startDate) params.append('startDate', filter.value.startDate);
                if (filter.value.endDate) params.append('endDate', filter.value.endDate);
                break;
        }
    });

    return params.toString();  // "projectIds=1&projectIds=2&customerIds=5"
};

// URL oluşturma
var params = self.buildFilterParams();
if (params) {
    url += '?' + params;
}
```

```javascript
// ❌ YANLIŞ - Array + join() pattern (ASP.NET Core düzgün parse edemez!)
self.buildFilterParams = function() {
    var params = [];
    self.activeFilters().forEach(function(filter) {
        if (filter.type === 'project') {
            params.push('projectIds=' + filter.value);  // ❌ YANLIŞ!
        }
    });
    return params;  // Array döndürür
};

// ❌ YANLIŞ - join ile birleştirme
if (params.length > 0) {
    url += '?' + params.join('&');  // ❌ Çoklu değerler düzgün gönderilmez!
}
```

**NEDEN:** `URLSearchParams.append()` aynı parametre adını birden fazla kez ekler ve ASP.NET Core `List<int>` olarak doğru parse eder.

### Avantajları

- ✅ NormalizeFilters() metoduna gerek yok
- ✅ Aynı tip filtreden birden fazla eklenebilir (OR mantığı)
- ✅ Chip-based filter UI ile uyumlu
- ✅ Kod tekrarı azalır
- ✅ Frontend'de addFilter/removeFilter kolaylaşır

### Tarih Aralığı Filtresi - DateRangeFilter Pattern

Tarih aralığı filtreleri için `DateRangeFilter` sınıfı kullanılır:

```csharp
// Core/DTOs/Report/ReportDto.cs içinde tanımlı
public class DateRangeFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
```

### DOĞRU - FilterDto'da DateRanges Kullanımı

```csharp
public class MyFilterDto
{
    public List<int>? CustomerIds { get; set; }
    public List<int>? ProjectIds { get; set; }
    public List<DateRangeFilter>? DateRanges { get; set; }  // Sadece bu!
}
```

### YANLIŞ - Geriye Uyumluluk Property'leri (KULLANMA!)

```csharp
public class MyFilterDto
{
    public List<DateRangeFilter>? DateRanges { get; set; }

    // ❌ YANLIŞ - Geriye uyumluluk property'leri token israfı
    public DateTime? StartDate
    {
        get => DateRanges?.FirstOrDefault()?.StartDate;
        set { ... }
    }
}
```

### Controller'da DateRanges Oluşturma

```csharp
[HttpGet("export")]
public async Task<IActionResult> Export(
    [FromQuery] List<int>? customerIds = null,
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null)
{
    var filter = new MyFilterDto
    {
        CustomerIds = customerIds
    };

    // Tarih aralığı varsa DateRanges'a ekle (UTC dönüşümü SERVICE'de yapılır!)
    if (startDate.HasValue || endDate.HasValue)
    {
        filter.DateRanges = new List<DateRangeFilter>
        {
            new DateRangeFilter { StartDate = startDate, EndDate = endDate }
        };
    }

    return Ok(await _service.GetAsync(filter));
}
```

### Service'de DateRanges Kullanımı (UTC dönüşümü burada!)

```csharp
// Date Range filter (çoklu - OR mantığı)
if (filter.DateRanges?.Any() == true)
{
    var datePredicates = filter.DateRanges.Select(dr =>
    {
        DateTime? startUtc = dr.StartDate.HasValue
            ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
            : null;
        DateTime? endUtc = dr.EndDate.HasValue
            ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
            : null;
        return (Start: startUtc, End: endUtc);
    }).ToList();

    var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
    var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

    if (minStart != DateTime.MinValue)
        query = query.Where(e => e.CreatedAt >= minStart);
    if (maxEnd != DateTime.MaxValue)
        query = query.Where(e => e.CreatedAt <= maxEnd);
}
```

### Checklist: Tarih Filtresi Eklerken

1. [ ] DTO'da SADECE `List<DateRangeFilter>? DateRanges` kullan
2. [ ] Geriye uyumluluk property'leri EKLEME
3. [ ] Controller'da DateRanges listesine ekle (UTC dönüşümü YAPMA)
4. [ ] Service'de UTC dönüşümü yap ve OR mantığı uygula

---

## 21. CSS Organizasyon Pattern'i (ZORUNLU!)

### ⛔ INLINE CSS YASAKTIR!

Sayfalarda inline `<style>` blokları ve `style=""` attribute'ları kullanılmamalıdır. Tüm CSS'ler ayrı dosyalarda olmalıdır.

### CSS Dosya Yapısı

```
wwwroot/css/
├── app.css                          # Global stiller (tüm sayfalarda)
├── layouts/
│   ├── customer-layout.css          # Customer Portal layout
│   └── popup-layout.css             # Popup layout (_LayoutPopup)
├── pages/
│   ├── organizations.css            # CustomerOrganizations sayfası
│   ├── organizations-popup.css      # Customers/Organizations popup
│   ├── checklist-editor.css         # Checklist Editor
│   ├── email-template-editor.css    # Email Template Editor
│   └── survey.css                   # Survey/Anket sayfaları
└── widgets/
    └── support-request-widget.css   # Support Request widget
```

### Ne Nereye Gider?

| CSS Tipi | Dosya | Örnek |
|----------|-------|-------|
| Global utility class | `app.css` | `.spin`, `.sticky-col`, `.badge-sm` |
| Layout stilleri | `layouts/*.css` | `.customer-sidebar`, `.popup-container` |
| Sayfa-spesifik stiller | `pages/*.css` | `.question-card`, `.wizard-nav-btn` |
| Widget stilleri | `widgets/*.css` | `.support-request-btn`, `.support-badge` |
| Print stilleri | `app.css` | `@media print { ... }` |

### app.css'de Olması Gereken Global Stiller

```css
/* ===== Utility Classes ===== */
.spin { animation: spin 1s linear infinite; }
.white-space-pre-wrap { white-space: pre-wrap; }
.sticky-col { position: sticky; left: 0; z-index: 1; }
.badge-sm { font-size: 0.65rem; padding: 0.15rem 0.35rem; }
.table-success-light { background-color: rgba(25, 135, 84, 0.15) !important; }
.table-warning-light { background-color: rgba(255, 193, 7, 0.15) !important; }
.table-danger-light { background-color: rgba(220, 53, 69, 0.15) !important; }

/* ===== Filter Dropdown ===== */
.filter-dropdown { min-width: 320px; z-index: 1030; }

/* ===== Sortable Table Headers ===== */
th.sortable { cursor: pointer; user-select: none; }

/* ===== Print Styles ===== */
@media print {
    .bg-success { background-color: #198754 !important; -webkit-print-color-adjust: exact; }
    /* ... diğer print stilleri ... */
}
```

### Sayfa-Spesifik CSS Ekleme

```html
@section Styles {
    <link rel="stylesheet" href="~/css/pages/checklist-editor.css" />
}
```

### Layout CSS Ekleme

```html
<!-- _CustomerLayout.cshtml içinde -->
<link rel="stylesheet" href="~/css/app.css">
<link rel="stylesheet" href="~/css/layouts/customer-layout.css">
```

### DOĞRU - CSS Class Kullanımı

```html
<!-- ✅ DOĞRU - CSS class kullan -->
<div class="card-body questions-container">
<button class="btn wizard-nav-btn">
<div class="filter-dropdown">
```

### YANLIŞ - Inline Style

```html
<!-- ❌ YANLIŞ - Inline style kullanma -->
<div class="card-body" style="max-height: calc(100vh - 250px); overflow-y: auto;">
<button style="width:50px;height:50px;z-index:1000;">
<div style="min-width: 320px;">
```

### Checklist: Yeni Sayfa Eklerken

1. [ ] Sayfa-spesifik CSS varsa `pages/sayfa-adi.css` oluştur
2. [ ] Global utility class gerekiyorsa `app.css`'e ekle
3. [ ] Widget CSS'i `widgets/` klasörüne koy
4. [ ] `@section Styles` ile CSS dosyasını dahil et
5. [ ] **INLINE STYLE KULLANMA!**

### İstisna: Fonksiyonel Inline Style

JavaScript ile dinamik kontrol edilen stiller kabul edilebilir:

```html
<!-- ✅ Kabul edilebilir - JS visibility kontrolü -->
<div id="admin-banner" style="display: none !important;">
```

Bu durumda bile mümkünse CSS class kullanılmalıdır.
