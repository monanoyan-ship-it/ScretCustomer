# Saha Çalışanları Modülü

Bu dokümanda, saha çalışanları CRUD modülü detaylandırılmıştır.

## 📋 Modül Özellikleri

- Saha çalışanı ekleme, düzenleme, silme (CRUD)
- Telefon numarası, adres ve temel bilgiler
- Aktif/Pasif durum yönetimi
- Telefon numarası benzersizlik kontrolü
- Admin yetkisi gerektiren modül

## 🗂️ Dosya Yapısı

### Backend Entities
```
Backend/SecretCustomer.Core/Entities/
└── FieldWorker.cs
```

**FieldWorker Entity Alanları:**
- `Id` - Guid (Primary Key)
- `FirstName` - string (Ad)
- `LastName` - string (Soyad)
- `PhoneNumber` - string (Telefon)
- `Address` - string (Adres)
- `Email` - string? (E-posta, opsiyonel)
- `IsActive` - bool (Aktif/Pasif)
- `Notes` - string? (Notlar, opsiyonel)
- `CreatedAt`, `UpdatedAt`, `IsDeleted` - BaseEntity'den miras

### Backend DTOs
```
Backend/SecretCustomer.Core/DTOs/FieldWorker/
├── FieldWorkerDto.cs          # Read DTO
├── CreateFieldWorkerDto.cs    # Create DTO (validasyon ile)
└── UpdateFieldWorkerDto.cs    # Update DTO (validasyon ile)
```

**Validasyon Kuralları:**
- Ad: Zorunlu, max 100 karakter
- Soyad: Zorunlu, max 100 karakter
- Telefon: Zorunlu, telefon formatı, max 20 karakter
- Adres: Zorunlu, max 500 karakter
- E-posta: Opsiyonel, e-posta formatı, max 255 karakter
- Notlar: Opsiyonel, max 1000 karakter

### Backend Repository
```
Backend/SecretCustomer.Core/Interfaces/Repositories/
└── IFieldWorkerRepository.cs

Backend/SecretCustomer.Data/Repositories/
└── FieldWorkerRepository.cs
```

**Repository Metotları:**
- `GetByIdAsync(Guid id)` - ID'ye göre getir
- `GetAllAsync(bool includeInactive)` - Tümünü getir
- `GetActiveAsync()` - Sadece aktif olanları getir
- `CreateAsync(FieldWorker)` - Yeni oluştur
- `UpdateAsync(FieldWorker)` - Güncelle
- `DeleteAsync(Guid id)` - Sil (soft delete)
- `ExistsByPhoneNumberAsync(string, Guid?)` - Telefon numarası kontrolü

### Backend Service
```
Backend/SecretCustomer.Core/Interfaces/Services/
└── IFieldWorkerService.cs

Backend/SecretCustomer.Services/Services/
└── FieldWorkerService.cs
```

**Service Özellikleri:**
- DTO mapping (Entity ↔ DTO dönüşümleri)
- Business logic (telefon numarası benzersizlik kontrolü)
- Exception handling

### Backend Controllers
```
Backend/SecretCustomer.API/Controllers/Api/
└── FieldWorkersApiController.cs        # API endpoints

Backend/SecretCustomer.API/Controllers/
└── FieldWorkersController.cs           # MVC controller (sadece Index action)
```

**API Endpoints:**
- `GET /api/fieldworkers` - Tüm saha çalışanları
- `GET /api/fieldworkers/{id}` - Detay
- `POST /api/fieldworkers` - Yeni oluştur
- `PUT /api/fieldworkers/{id}` - Güncelle
- `DELETE /api/fieldworkers/{id}` - Sil

**Authorization:** Tüm endpoint'ler `[Authorize(Roles = "Admin")]` ile korunur.

### Frontend View
```
Backend/SecretCustomer.API/Views/FieldWorkers/
└── Index.cshtml
```

**View Özellikleri:**
- KnockoutJS ile SPA yapısı
- Modal içinde Create/Edit formu
- Tablo listesi
- Loading ve hata/başarı mesajları
- Responsive tasarım (Bootstrap 5)

## 🎨 Frontend Yapısı (KnockoutJS)

### ViewModel Yapısı

```javascript
function FieldWorkerEditViewModel(data) {
    var self = this;

    self.id = data.id || null;
    self.firstName = ko.observable(data.firstName || '');
    self.lastName = ko.observable(data.lastName || '');
    self.phoneNumber = ko.observable(data.phoneNumber || '');
    self.email = ko.observable(data.email || '');
    self.address = ko.observable(data.address || '');
    self.notes = ko.observable(data.notes || '');
    self.isActive = ko.observable(data.isActive !== undefined ? data.isActive : true);
}

function FieldWorkersViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');

    // Data
    self.fieldWorkers = ko.observableArray([]);
    self.editingFieldWorker = ko.observable(null);

    // Modal state
    self.isModalOpen = ko.observable(false);

    // CRUD fonksiyonları...
}
```

### Pattern: Editing Pattern

Proje mimarisine uygun olarak **Editing Pattern** kullanılır:

1. `createNew()` - Boş FieldWorkerEditViewModel oluşturur
2. `editFieldWorker(item)` - Mevcut item'dan ViewModel oluşturur (deep copy)
3. `saveFieldWorker()` - DTO'ya çevirir ve API'ye POST/PUT yapar
4. `deleteFieldWorker(item)` - API'ye DELETE yapar
5. `closeModal()` - Modal'ı kapatır ve state'i temizler

## 🔧 Kurulum Adımları

### 1. Database Migration

```bash
cd Backend/SecretCustomer.Data
dotnet ef migrations add AddFieldWorker --startup-project ../SecretCustomer.API
dotnet ef database update --startup-project ../SecretCustomer.API
```

### 2. Dependency Injection (Program.cs)

```csharp
// Repository Registration
builder.Services.AddScoped<IFieldWorkerRepository, FieldWorkerRepository>();

// Service Registration
builder.Services.AddScoped<IFieldWorkerService, FieldWorkerService>();
```

### 3. DbContext Configuration

```csharp
public DbSet<FieldWorker> FieldWorkers { get; set; }

// Global query filter for soft delete
modelBuilder.Entity<FieldWorker>().HasQueryFilter(e => !e.IsDeleted);
```

### 4. Sidebar Menü (_Sidebar.cshtml)

```html
<li data-bind="visible: isAdmin">
    <a href="/FieldWorkers/Index" class="nav-link text-white" data-bind="click: closeSidebar">
        <i class="bi bi-people me-2"></i>
        Saha Çalışanları
    </a>
</li>
```

## 📝 Kullanım

### Admin Panelinden

1. Admin kullanıcısı ile giriş yap
2. Sol menüden "Saha Çalışanları" linkine tıkla
3. "Yeni Saha Çalışanı" butonuna tıkla
4. Formu doldur:
   - Ad *
   - Soyad *
   - Telefon *
   - E-posta (opsiyonel)
   - Adres *
   - Notlar (opsiyonel)
   - Aktif/Pasif durumu
5. "Kaydet" butonuna tıkla

### Düzenleme

1. Listede "Düzenle" butonuna tıkla
2. Formu güncelle
3. "Kaydet" butonuna tıkla

### Silme

1. Listede "Sil" butonuna tıkla
2. Onay ver
3. Kayıt soft delete ile silinir (IsDeleted = true)

## 🔒 Güvenlik

- **Authorization**: Sadece Admin rolüne sahip kullanıcılar erişebilir
- **Validation**: Hem client-side hem server-side validasyon
- **Soft Delete**: Kayıtlar fiziksel olarak silinmez, IsDeleted flag'i kullanılır
- **Unique Constraint**: Telefon numarası benzersiz olmalıdır

## 🚀 API Kullanımı

### Tüm Saha Çalışanlarını Getir

```http
GET /api/fieldworkers
Authorization: Bearer {token}
```

**Response:**
```json
[
  {
    "id": "guid",
    "firstName": "Ahmet",
    "lastName": "Yılmaz",
    "phoneNumber": "+90 555 123 45 67",
    "email": "ahmet@example.com",
    "address": "İstanbul, Türkiye",
    "isActive": true,
    "notes": "Deneyimli saha çalışanı",
    "createdAt": "2025-01-24T10:00:00Z",
    "updatedAt": null
  }
]
```

### Yeni Saha Çalışanı Oluştur

```http
POST /api/fieldworkers
Authorization: Bearer {token}
Content-Type: application/json

{
  "firstName": "Ahmet",
  "lastName": "Yılmaz",
  "phoneNumber": "+90 555 123 45 67",
  "email": "ahmet@example.com",
  "address": "İstanbul, Türkiye",
  "isActive": true,
  "notes": "Deneyimli saha çalışanı"
}
```

### Saha Çalışanı Güncelle

```http
PUT /api/fieldworkers/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "firstName": "Ahmet",
  "lastName": "Yılmaz",
  "phoneNumber": "+90 555 123 45 67",
  "email": "ahmet.yilmaz@example.com",
  "address": "Ankara, Türkiye",
  "isActive": true,
  "notes": "Güncellendi"
}
```

### Saha Çalışanı Sil

```http
DELETE /api/fieldworkers/{id}
Authorization: Bearer {token}
```

**Response:** 204 No Content

## ⚠️ Hata Mesajları

### Telefon Numarası Zaten Kullanılıyor

```json
{
  "message": "Bu telefon numarası zaten kullanılıyor."
}
```

### Validation Hataları

```json
{
  "errors": {
    "FirstName": ["Ad alanı zorunludur."],
    "PhoneNumber": ["Geçerli bir telefon numarası giriniz."]
  }
}
```

## 📊 Veritabanı Şeması

```sql
CREATE TABLE "FieldWorkers" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "FirstName" text NOT NULL,
    "LastName" text NOT NULL,
    "PhoneNumber" text NOT NULL,
    "Address" text NOT NULL,
    "Email" text,
    "IsActive" boolean NOT NULL,
    "Notes" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL
);
```

## 🎯 Gelecek Geliştirmeler

- [ ] Fotoğraf yükleme
- [ ] Export to Excel
- [ ] Saha çalışanı performans raporları
- [ ] Saha çalışanı-atama ilişkilendirmesi
- [ ] Çoklu silme
- [ ] Gelişmiş filtreleme (ad, telefon, şehir vb.)
- [ ] Sayfalama (pagination)

---

**Tarih:** 2025-01-24
**Versiyon:** 1.0
**Durum:** Aktif ve Çalışır Durumda
