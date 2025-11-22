# Kontrol Listesi Modülü - Backend API

## Genel Bakış

Kontrol Listesi modülü, dinamik form oluşturma, düzenleme ve yönetme işlemlerini sağlar. Admin kullanıcılar farklı soru tiplerinde formlar oluşturabilir ve puanlama sistemi tanımlayabilir.

## API Endpoints

### Base URL
```
/api/checklist
```

### Endpoints

#### 1. Tüm Kontrol Listelerini Getir
```http
GET /api/checklist?includeInactive=false
```

**Response:**
```json
[
  {
    "id": "guid",
    "name": "Çağrı Merkezi Değerlendirme Formu",
    "description": "Müşteri temsilcisi değerlendirme formu",
    "isScored": true,
    "isActive": true,
    "version": 1,
    "createdAt": "2025-11-22T10:00:00Z",
    "sections": [...]
  }
]
```

#### 2. ID'ye Göre Kontrol Listesi Getir
```http
GET /api/checklist/{id}
```

**Response:**
```json
{
  "id": "guid",
  "name": "Çağrı Merkezi Değerlendirme Formu",
  "description": "Detaylı açıklama",
  "isScored": true,
  "isActive": true,
  "version": 1,
  "createdAt": "2025-11-22T10:00:00Z",
  "sections": [
    {
      "id": "guid",
      "name": "Karşılama",
      "description": "Müşteri karşılama bölümü",
      "order": 1,
      "questions": [
        {
          "id": "guid",
          "text": "Temsilci kendini tanıttı mı?",
          "type": "MultipleChoice",
          "order": 1,
          "points": 10,
          "allowNA": false,
          "isRequired": true,
          "options": [
            {
              "value": "yes",
              "label": "Evet",
              "points": 10
            },
            {
              "value": "no",
              "label": "Hayır",
              "points": 0
            }
          ]
        }
      ]
    }
  ]
}
```

#### 3. Yeni Kontrol Listesi Oluştur
```http
POST /api/checklist
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "Şube Değerlendirme Formu",
  "description": "Şube ziyaret değerlendirmesi",
  "isScored": true,
  "sections": [
    {
      "name": "Karşılama",
      "description": "Müşteri karşılama",
      "order": 1,
      "questions": [
        {
          "text": "Selamlama yapıldı mı?",
          "type": "Likert",
          "order": 1,
          "points": 20,
          "allowNA": true,
          "isRequired": true,
          "options": null
        }
      ]
    }
  ]
}
```

**Response:** Created checklist (201 Created)

#### 4. Kontrol Listesi Güncelle
```http
PUT /api/checklist/{id}
Content-Type: application/json
```

**Request Body:**
```json
{
  "id": "guid",
  "name": "Güncellenmiş Form",
  "description": "Açıklama",
  "isScored": true,
  "isActive": true,
  "sections": [...]
}
```

**Response:** Updated checklist (200 OK)

#### 5. Kontrol Listesi Sil
```http
DELETE /api/checklist/{id}
```

**Response:** 204 No Content

**Not:** Soft delete yapılır, `IsDeleted` flag'i true olur.

#### 6. Kontrol Listesi Klonla
```http
POST /api/checklist/{id}/clone
Content-Type: application/json
```

**Request Body:**
```json
{
  "newName": "Çağrı Merkezi Formu"
}
```

**Response:** Cloned checklist with auto-incremented version (201 Created)

## Soru Tipleri

### 1. MultipleChoice (Çoktan Seçmeli)
```json
{
  "type": "MultipleChoice",
  "options": [
    { "value": "option1", "label": "Seçenek 1", "points": 10 },
    { "value": "option2", "label": "Seçenek 2", "points": 5 }
  ]
}
```

### 2. Likert (Ölçek 1-5)
```json
{
  "type": "Likert",
  "options": null
}
```
**Not:** Frontend'de 1-5 arası otomatik ölçek gösterilir.

### 3. Star (Yıldız Değerlendirmesi 1-5)
```json
{
  "type": "Star",
  "options": null
}
```
**Not:** Frontend'de yıldız görsel ile gösterilir.

### 4. Text (Metin Alanı)
```json
{
  "type": "Text",
  "options": null
}
```
**Not:** Metin alanı puansızdır, sadece açıklama içindir.

## N/A (Gerekmedi) Özelliği

### Kullanım
`allowNA: true` olan sorularda kullanıcı "N/A" seçeneği işaretleyebilir.

### Puan Hesaplama Algoritması

N/A işaretlenen sorular için puan hesaplaması:

1. **Toplam Puan Hesabı:** Tüm soruların puanları toplanır
2. **N/A Çıkarımı:** N/A işaretli soruların puanı toplam puandan çıkarılır
3. **Ağırlık Dağılımı:** Kalan soruların orijinal ağırlık oranları korunur
4. **Yüzde Hesabı:** Final skoru yüzde olarak hesaplanır

**Örnek:**
```
Soru 1: 20 puan (cevaplandı: 15 puan kazanıldı)
Soru 2: 30 puan (N/A işaretlendi)
Soru 3: 50 puan (cevaplandı: 40 puan kazanıldı)

Orijinal Toplam: 100 puan
N/A Sonrası Toplam: 70 puan
Kazanılan: 55 puan

Başarı Yüzdesi: (55 / 70) * 100 = 78.57%
```

## Domain Modeli

### Checklist
- **Id**: Guid
- **Name**: string
- **Description**: string
- **IsScored**: bool (puanlı/puansız)
- **IsActive**: bool
- **Version**: int (versiyonlama için)
- **Sections**: List<Section>

### Section
- **Id**: Guid
- **Name**: string
- **Description**: string
- **Order**: int (sıralama)
- **Questions**: List<Question>

### Question
- **Id**: Guid
- **Text**: string
- **Type**: QuestionType enum
- **Order**: int
- **Points**: int
- **AllowNA**: bool
- **IsRequired**: bool
- **OptionsJson**: string (JSON format)

## Service Layer

### IChecklistService
```csharp
Task<ChecklistDto?> GetByIdAsync(Guid id);
Task<IEnumerable<ChecklistDto>> GetAllAsync(bool includeInactive);
Task<ChecklistDto> CreateAsync(CreateChecklistDto dto);
Task<ChecklistDto> UpdateAsync(UpdateChecklistDto dto);
Task<bool> DeleteAsync(Guid id);
Task<ChecklistDto> CloneChecklistAsync(Guid id, string newName);
```

### ChecklistService
- JSON serialization/deserialization (OptionsJson)
- Entity mapping (Entity ↔ DTO)
- Version management
- Cascade updates (sections, questions)

## Repository Layer

### IChecklistRepository
```csharp
Task<Checklist?> GetByIdAsync(Guid id, bool includeDetails);
Task<IEnumerable<Checklist>> GetAllAsync(bool includeInactive);
Task<Checklist> CreateAsync(Checklist checklist);
Task<Checklist> UpdateAsync(Checklist checklist);
Task<bool> DeleteAsync(Guid id);
Task<bool> ExistsAsync(Guid id);
Task<int> GetVersionCountAsync(string name);
```

### ChecklistRepository
- EF Core Include for eager loading
- Soft delete implementation
- Version counting for cloning

## Validation Rules

### CreateChecklistDto
- **Name**: Required, MaxLength(255)
- **Description**: MaxLength(1000)
- **Sections**: En az 1 section olmalı
- **Questions**: Her section'da en az 1 soru olmalı

### Question Validation
- **Text**: Required, MaxLength(1000)
- **Type**: Valid QuestionType enum value
- **Points**: Range(0, 1000)
- **Options**: MultipleChoice tipinde required, diğerlerinde null

## Error Handling

### 400 Bad Request
- Invalid model state
- Missing required fields
- Invalid enum values

### 404 Not Found
- Checklist ID not found
- Referenced entity not found

### 500 Internal Server Error
- Database connection errors
- Unexpected exceptions

## Kullanım Senaryoları

### 1. Yeni Form Oluşturma
Admin bir çağrı merkezi değerlendirme formu oluşturur:
- 3 bölüm (Karşılama, Ağırlama, Uğurlama)
- Her bölümde 5-10 soru
- Farklı soru tipleri (Likert, Star, MultipleChoice)
- Toplam 100 puan

### 2. Form Klonlama
Var olan formu kopyalayıp yeni versiyon oluşturur:
- Tüm sorular kopyalanır
- Versiyon numarası otomatik arttırılır
- Yeni isim verilir

### 3. Form Güncelleme
Var olan forma yeni sorular eklenir veya mevcut sorular güncellenir:
- Yeni section/question ekleme
- Mevcut section/question güncelleme
- Section/question silme (soft delete)

## Dependencies

### NuGet Packages
- Microsoft.EntityFrameworkCore (9.0.0)
- Npgsql.EntityFrameworkCore.PostgreSQL (9.0.2)
- System.Text.Json (built-in)

### Project References
- SecretCustomer.Core (Entities, DTOs, Interfaces)
- SecretCustomer.Data (DbContext, Repositories)
- SecretCustomer.Services (Business Logic)

---
**Oluşturulma Tarihi**: 2025-11-22
**Versiyon**: 1.0
**Sonraki Adım**: Atama Mantığı Modülü
