# Mimari Yaklaşım - Single Page Application (SPA) Yapısı

Bu dokümanda, projemizin mimari yaklaşımı ve uygulama prensipleri açıklanmaktadır.

## 🎯 Temel Prensipler

### 1. Single Page Application (SPA) Mantığı

Projemiz tamamen **Single Page Application** mantığında çalışır:
- ✅ Her modül için **tek bir Index view** vardır
- ❌ Separate Create, Edit, View sayfaları **YOKTUR**
- ✅ Tüm CRUD işlemleri **modal içinde** yapılır
- ✅ Form submit yerine **AJAX POST** kullanılır

### 2. View Yapısı

**DOĞRU YAPILANMA:**
```
Views/
├── Checklists/
│   └── Index.cshtml          # Tek view - Liste + Modal
├── Projects/
│   └── Index.cshtml          # Tek view - Liste + Modal
├── Assignments/
│   └── Index.cshtml          # Tek view - Liste + Modal
├── Evaluations/
│   ├── Index.cshtml          # Liste
│   └── Evaluate.cshtml       # Özel durum - External evaluation için
└── Account/
    └── Login.cshtml          # Tek istisna - Form submit kullanır
```

**YANLIŞ YAPILANMA (KULLANILMAZ):**
```
❌ Views/Checklists/Create.cshtml
❌ Views/Checklists/Edit.cshtml
❌ Views/Checklists/CreateEdit.cshtml
❌ Views/Checklists/View.cshtml
```

### 3. Controller Yapısı

**MVC Controller'lar:**
- Sadece `Index()` action'ı vardır
- View render eder, başka bir şey yapmaz
- ❌ Create, Edit, Delete action'ları **YOKTUR**

**Örnek:**
```csharp
[Authorize(Roles = "Admin")]
public class ChecklistsController : Controller
{
    public IActionResult Index()
    {
        return View();  // Sadece View döndür
    }

    // ❌ Bu tür action'lar yok:
    // public IActionResult Create() { }
    // public IActionResult Edit(Guid id) { }
    // [HttpPost] public IActionResult Save(Model model) { }
}
```

**API Controller'lar:**
- `/api/` route prefix'i kullanır
- Tüm CRUD işlemleri burada yapılır
- JSON döndürür

**Örnek:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class ChecklistsApiController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() { }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id) { }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChecklistDto dto) { }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ChecklistDto dto) { }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id) { }
}
```

### 4. KnockoutJS "Editing" Pattern

**Temel Pattern:**
```javascript
function MyModuleViewModel() {
    var self = this;

    // Liste verileri
    self.items = ko.observableArray([]);

    // Editing state
    self.editingItem = ko.observable(null);
    self.isEditing = ko.computed(function() {
        return self.editingItem() !== null;
    });

    // Modal state
    self.isModalOpen = ko.observable(false);

    // Create new item
    self.createNew = function() {
        self.editingItem({
            id: null,
            name: ko.observable(''),
            description: ko.observable(''),
            // ... diğer alanlar
        });
        self.isModalOpen(true);
    };

    // Edit existing item
    self.edit = function(item) {
        // Deep copy - orijinali değiştirmemek için
        self.editingItem({
            id: item.id,
            name: ko.observable(item.name),
            description: ko.observable(item.description),
            // ... diğer alanlar
        });
        self.isModalOpen(true);
    };

    // Save (create or update)
    self.save = function() {
        var item = self.editingItem();

        // DTO preparation
        var dto = {
            id: item.id,
            name: item.name(),
            description: item.description(),
            // ... diğer alanlar
        };

        // API call
        var endpoint = item.id ? '/api/mymodule/' + item.id : '/api/mymodule';
        var method = item.id ? 'PUT' : 'POST';

        apiService[method.toLowerCase()](endpoint, dto)
            .then(function(response) {
                // Başarılı - listeyi güncelle
                if (item.id) {
                    // Update - mevcut item'ı güncelle
                    var existingItem = self.items().find(x => x.id === item.id);
                    if (existingItem) {
                        existingItem.name = item.name();
                        existingItem.description = item.description();
                        // ... diğer alanlar
                    }
                } else {
                    // Create - listeye ekle
                    self.items.push(response);
                }

                self.closeModal();
                alert('Kayıt başarılı!');
            })
            .catch(function(error) {
                alert('Hata: ' + error.message);
            });
    };

    // Cancel editing
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingItem(null);
    };

    // Delete item
    self.delete = function(item) {
        if (!confirm('Silmek istediğinizden emin misiniz?')) return;

        apiService.delete('/api/mymodule/' + item.id)
            .then(function() {
                self.items.remove(item);
                alert('Silme başarılı!');
            })
            .catch(function(error) {
                alert('Hata: ' + error.message);
            });
    };

    // Load data
    self.loadData = function() {
        apiService.get('/api/mymodule')
            .then(function(data) {
                self.items(data);
            });
    };

    // Initialize
    self.loadData();
}
```

**HTML Template:**
```html
@{
    ViewData["Title"] = "My Module";
}

<!-- ⚠️ Her sayfa için unique ID! -->
<div id="mymodule-app" class="container-fluid">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h2>My Module</h2>
        <button class="btn btn-primary" data-bind="click: createNew">
            <i class="bi bi-plus-circle"></i> Yeni Oluştur
        </button>
    </div>

    <!-- Liste -->
    <div class="card shadow">
        <div class="card-body">
            <table class="table" data-bind="visible: items().length > 0">
                <thead>
                    <tr>
                        <th>Ad</th>
                        <th>Açıklama</th>
                        <th>İşlemler</th>
                    </tr>
                </thead>
                <tbody data-bind="foreach: items">
                    <tr>
                        <td data-bind="text: name"></td>
                        <td data-bind="text: description"></td>
                        <td>
                            <button class="btn btn-sm btn-warning" data-bind="click: $parent.edit">
                                <i class="bi bi-pencil"></i> Düzenle
                            </button>
                            <button class="btn btn-sm btn-danger" data-bind="click: $parent.delete">
                                <i class="bi bi-trash"></i> Sil
                            </button>
                        </td>
                    </tr>
                </tbody>
            </table>

            <div data-bind="visible: items().length === 0" class="text-center text-muted py-4">
                Henüz kayıt bulunmamaktadır.
            </div>
        </div>
    </div>

    <!-- Modal (Bootstrap 5) - ⚠️ Modal'lar div içinde olmalı! -->
    <div class="modal fade" data-bind="css: { show: isModalOpen }, style: { display: isModalOpen() ? 'block' : 'none' }">
        <div class="modal-dialog modal-lg">
            <div class="modal-content" data-bind="with: editingItem">
                <div class="modal-header">
                    <h5 class="modal-title">
                        <span data-bind="visible: !id">Yeni Oluştur</span>
                        <span data-bind="visible: id">Düzenle</span>
                    </h5>
                    <button type="button" class="btn-close" data-bind="click: $parent.closeModal"></button>
                </div>
                <div class="modal-body">
                    <div class="mb-3">
                        <label class="form-label">Ad *</label>
                        <input type="text" class="form-control" data-bind="value: name" />
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Açıklama</label>
                        <textarea class="form-control" data-bind="value: description" rows="3"></textarea>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bind="click: $parent.closeModal">
                        İptal
                    </button>
                    <button type="button" class="btn btn-primary" data-bind="click: $parent.save">
                        <i class="bi bi-save"></i> Kaydet
                    </button>
                </div>
            </div>
        </div>
    </div>
    <div class="modal-backdrop fade" data-bind="css: { show: isModalOpen }, style: { display: isModalOpen() ? 'block' : 'none' }"></div>

</div>  <!-- End of mymodule-app - ⚠️ Modal'lardan SONRA kapat! -->

@section Scripts {
    <script>
        // Apply bindings - ⚠️ Sadece bu div'e bind et!
        $(document).ready(function() {
            ko.applyBindings(
                new MyModuleViewModel(),
                document.getElementById('mymodule-app')  // ✅ Spesifik div ID
            );
        });
    </script>
}
```

### 5. Form Submit vs AJAX POST

**❌ KULLANILMAZ (Login hariç):**
```html
<form asp-action="Create" method="post">
    <input asp-for="Name" />
    <button type="submit">Kaydet</button>
</form>
```

**✅ KULLANILIR:**
```html
<form data-bind="submit: save">  <!-- KnockoutJS submit binding -->
    <input type="text" data-bind="value: editingItem().name" />
    <button type="submit">Kaydet</button>
</form>
```

**veya:**
```html
<button data-bind="click: save">Kaydet</button>
```

### 6. View Binding

**❌ KULLANILMAZ:**
```html
@model ChecklistDto
<h2>@Model.Name</h2>
<input asp-for="Name" />
```

**✅ KULLANILIR:**
```html
<!-- KnockoutJS data-bind -->
<h2 data-bind="text: editingItem().name"></h2>
<input type="text" data-bind="value: editingItem().name" />
```

### 7. Routing Yapısı

**MVC Routes:**
```
/Checklists           → ChecklistsController.Index() → Views/Checklists/Index.cshtml
/Projects             → ProjectsController.Index()   → Views/Projects/Index.cshtml
/Assignments          → AssignmentsController.Index() → Views/Assignments/Index.cshtml
/Dashboard            → DashboardController.Index()   → Views/Dashboard/Index.cshtml
/Account/Login        → AccountController.Login()     → Views/Account/Login.cshtml
```

**API Routes:**
```
GET    /api/checklists           → List all
GET    /api/checklists/{id}      → Get by ID
POST   /api/checklists           → Create
PUT    /api/checklists/{id}      → Update
DELETE /api/checklists/{id}      → Delete
```

### 8. İstisna Durumlar

**Login:**
- ✅ `@model LoginViewModel` kullanır
- ✅ `<form asp-action="Login" method="post">` kullanır
- ✅ `[HttpPost]` action vardır
- **Neden?** Authentication cookie'si server-side set edilmelidir

**External Evaluation (Opsiyonel):**
- ✅ `/Evaluations/Evaluate/{id}` gibi özel route olabilir
- **Neden?** Dış değerlendiriciler için authentication olmadan erişilebilir link

### 9. Deep Copy Pattern

**Editing sırasında orijinal veriyi korumak için:**

```javascript
// ❌ YANLIŞ - Referans kopyası
self.edit = function(item) {
    self.editingItem(item);  // Orijinali değiştirir!
};

// ✅ DOĞRU - Deep copy
self.edit = function(item) {
    self.editingItem({
        id: item.id,
        name: ko.observable(item.name),
        description: ko.observable(item.description),
        // Her alan için yeni observable oluştur
    });
};
```

**Neden önemli?**
- Kullanıcı modal'da değişiklik yapar, sonra Cancel'e basar
- Deep copy yapmazsak, orijinal liste item'ı da değişir
- Deep copy yaparsak, Cancel'da değişiklikler kaybolur

### 10. KnockoutJS Binding - Önemli Kural ⚠️

**HER SAYFADA AYRI DIV ID İLE BINDING YAPILMALIDIR!**

KnockoutJS'te aynı element'e birden fazla `ko.applyBindings()` yapılamaz. Her sayfa için:

**❌ YANLIŞ - Tüm sayfaya binding:**
```javascript
$(document).ready(function() {
    ko.applyBindings(new ChecklistsViewModel());  // Hata verir!
});
```

**✅ DOĞRU - Spesifik div'e binding:**
```html
<!-- Her sayfa için unique ID -->
<div id="checklists-app" class="container-fluid">
    <!-- Sayfa içeriği -->
</div>
```

```javascript
$(document).ready(function() {
    ko.applyBindings(
        new ChecklistsViewModel(),
        document.getElementById('checklists-app')  // ✅ Sadece bu div'e bind et
    );
});
```

**ID Naming Convention:**
- `checklists-app` → Checklists sayfası
- `projects-app` → Projects sayfası
- `assignments-app` → Assignments sayfası
- `dashboard-app` → Dashboard sayfası
- `evaluations-app` → Evaluations sayfası

**Neden Önemli?**
- Layout'ta başka binding'ler olabilir
- Aynı element'e çift binding yapılamaz
- Hata: "You cannot apply bindings multiple times to the same element"

### 11. Modal State Management

**Bootstrap 5 Modal - KnockoutJS ile:**

```javascript
// Modal state
self.isModalOpen = ko.observable(false);

// Açma
self.openModal = function() {
    self.isModalOpen(true);
};

// Kapama
self.closeModal = function() {
    self.isModalOpen(false);
    self.editingItem(null);  // State'i temizle
};
```

```html
<!-- Modal visibility binding -->
<div class="modal fade"
     data-bind="css: { show: isModalOpen },
                style: { display: isModalOpen() ? 'block' : 'none' }">
    <!-- Modal content -->
</div>

<!-- Backdrop -->
<div class="modal-backdrop fade"
     data-bind="css: { show: isModalOpen },
                style: { display: isModalOpen() ? 'block' : 'none' }">
</div>
```

**NOT:** Bootstrap modal JavaScript API'sini kullanmıyoruz, sadece CSS class'larını KnockoutJS ile yönetiyoruz.

### 11. Validation

**Client-side validation:**
```javascript
self.save = function() {
    var item = self.editingItem();

    // Manuel validation
    if (!item.name() || item.name().trim() === '') {
        alert('Ad alanı zorunludur!');
        return;
    }

    if (item.name().length < 3) {
        alert('Ad en az 3 karakter olmalıdır!');
        return;
    }

    // API call...
};
```

**Server-side validation:**
```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] ChecklistDto dto)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    // ... işlemler
}
```

### 12. Error Handling

**API Service level:**
```javascript
apiService.post('/api/checklists', dto)
    .then(function(response) {
        // Başarılı
        alert('Kayıt başarılı!');
    })
    .catch(function(error) {
        // Hata
        console.error('API Error:', error);

        if (error.status === 400) {
            alert('Geçersiz veri: ' + error.message);
        } else if (error.status === 401) {
            // Unauthorized - apiService.js otomatik logout yapar
        } else {
            alert('Bir hata oluştu: ' + error.message);
        }
    });
```

### 13. Loading States

```javascript
function MyViewModel() {
    var self = this;

    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);

    self.loadData = function() {
        self.isLoading(true);

        apiService.get('/api/mymodule')
            .then(function(data) {
                self.items(data);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.save = function() {
        self.isSaving(true);

        apiService.post('/api/mymodule', dto)
            .then(function() {
                // Success
            })
            .finally(function() {
                self.isSaving(false);
            });
    };
}
```

```html
<!-- Loading spinner -->
<div data-bind="visible: isLoading" class="text-center">
    <div class="spinner-border" role="status"></div>
</div>

<!-- Disable button during save -->
<button data-bind="click: save, disable: isSaving">
    <span data-bind="visible: !isSaving()">Kaydet</span>
    <span data-bind="visible: isSaving">
        <span class="spinner-border spinner-border-sm"></span>
        Kaydediliyor...
    </span>
</button>
```

## 📋 Checklist (Yeni Modül Eklerken)

Yeni bir modül eklerken bu adımları takip edin:

- [ ] **MVC Controller** - Sadece `Index()` action'ı
- [ ] **API Controller** - GET, POST, PUT, DELETE action'ları
- [ ] **View (Index.cshtml)** - Liste + Modal yapısı
- [ ] **⚠️ Unique div ID ekle** - `<div id="modulename-app">`
- [ ] **⚠️ Spesifik binding yap** - `ko.applyBindings(vm, document.getElementById('modulename-app'))`
- [ ] **KnockoutJS ViewModel** - `Editing` pattern ile
- [ ] **API Service** - HTTP request helper'lar zaten var
- [ ] ❌ **Create.cshtml, Edit.cshtml, View.cshtml gibi dosyalar oluşturma!**
- [ ] ❌ **@model direktifi kullanma! (Login hariç)**
- [ ] ❌ **Form submit kullanma! (Login hariç)**
- [ ] ❌ **Tüm sayfaya binding yapma!** - Sadece kendi div'ine

## 🚫 Yaygın Hatalar

### 1. Multiple Binding Hatası (En Yaygın!) ⚠️
```javascript
❌ Hata: "You cannot apply bindings multiple times to the same element"

Neden oluyor?
ko.applyBindings(new ViewModel());  // Tüm sayfaya bind ediyor

✅ Çözüm:
<div id="mypage-app">
    <!-- Sayfa içeriği -->
    <!-- Modal'lar -->
</div>  <!-- ⚠️ Modal'lardan SONRA kapat! -->

ko.applyBindings(new ViewModel(), document.getElementById('mypage-app'));
```

**Dikkat:** Modal'lar div'in IÇINDE olmalı, DIŞINDA değil!

### 2. Yeni View Dosyası Oluşturma
```
❌ Views/Checklists/Create.cshtml oluşturdum
✅ Modal'ı Index.cshtml içinde oluşturduk
```

### 2. MVC Controller'da Form Submit
```csharp
❌ [HttpPost]
   public IActionResult Create(ChecklistDto dto) { }

✅ API Controller'da:
   [HttpPost]
   public async Task<IActionResult> Create([FromBody] ChecklistDto dto) { }
```

### 3. View Binding Kullanımı
```html
❌ <input asp-for="Name" />
✅ <input data-bind="value: editingItem().name" />
```

### 4. Referans Kopyası
```javascript
❌ self.editingItem(item);  // Orijinali değiştirir!
✅ self.editingItem({ ...deep copy... });  // Kopya oluşturur
```

### 5. Modal Yönetimi
```javascript
❌ $('#myModal').modal('show');  // jQuery modal API
✅ self.isModalOpen(true);  // KnockoutJS state
```

## 📚 İlgili Dokümantasyon

- **13-knockoutjs-kullanimi.md** - KnockoutJS temel kullanım
- **07-frontend-temel-yapi.md** - Frontend mimarisi
- **API Controller örnekleri** - `Controllers/Api/` klasörü

## 🎯 Özet

**3 Temel Kural:**
1. **Tek View Per Module** - Index.cshtml
2. **Modal'da CRUD** - Create/Edit/View modal içinde
3. **AJAX POST** - Form submit yok, AJAX var

**İstisna:**
- Login sayfası - Server-side authentication için form submit gerekli

---

**Tarih:** 2025-01-24
**Versiyon:** 1.0
**Durum:** Aktif Mimari
