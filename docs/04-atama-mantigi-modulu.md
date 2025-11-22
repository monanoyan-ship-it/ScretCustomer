# Atama Mantığı Modülü - Backend API

## Genel Bakış

Atama Mantığı modülü, projelerin oluşturulması ve değerlendirmelerin iç/dış kullanıcılara atanmasını sağlar. İki tür atama senaryosu desteklenir:

1. **Internal (İç)**: Kendi personeline (değerlendiricilere) atama
2. **External (Dış)**: Müşterilere anket linki gönderimi

## API Endpoints

### Project Endpoints

#### Base URL
```
/api/project
```

#### 1. Tüm Projeleri Getir
```http
GET /api/project?includeInactive=false
```

**Response:**
```json
[
  {
    "id": "guid",
    "name": "2026 Q1 Gizli Müşteri Projesi",
    "description": "İlk çeyrek değerlendirmeleri",
    "checklistId": "guid",
    "checklistName": "Çağrı Merkezi Formu",
    "assignmentType": "Internal",
    "startDate": "2026-01-01T00:00:00Z",
    "endDate": "2026-03-31T23:59:59Z",
    "isActive": true,
    "createdAt": "2025-11-22T10:00:00Z",
    "totalAssignments": 150,
    "completedAssignments": 45
  }
]
```

#### 2. Proje Oluştur
```http
POST /api/project
Content-Type: application/json
```

**Request:**
```json
{
  "name": "2026 Q1 Gizli Müşteri Projesi",
  "description": "İlk çeyrek değerlendirmeleri",
  "checklistId": "guid",
  "assignmentType": "Internal",
  "startDate": "2026-01-01",
  "endDate": "2026-03-31"
}
```

#### 3. Proje Güncelle
```http
PUT /api/project/{id}
```

#### 4. Proje Sil
```http
DELETE /api/project/{id}
```

#### 5. Projeyi Kapat
```http
POST /api/project/{id}/close
```

### Assignment Endpoints

#### Base URL
```
/api/assignment
```

#### 1. ID'ye Göre Atama Getir
```http
GET /api/assignment/{id}
```

#### 2. Unique Link ile Atama Getir
```http
GET /api/assignment/link/{uniqueLink}
```

**Kullanım:** Dış müşterilere gönderilen link için

**Response:**
```json
{
  "id": "guid",
  "projectId": "guid",
  "projectName": "Memnuniyet Anketi",
  "checklistId": "guid",
  "checklistName": "Anket Formu",
  "branchId": null,
  "branchName": null,
  "assignedUserId": null,
  "assignedUserName": null,
  "externalEmail": "musteri@example.com",
  "externalName": "Ahmet Yılmaz",
  "uniqueLink": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "dueDate": "2026-02-15T00:00:00Z",
  "isCompleted": false,
  "completedAt": null,
  "createdAt": "2025-11-22T10:00:00Z"
}
```

#### 3. Proje Bazlı Atamaları Getir
```http
GET /api/assignment/project/{projectId}
```

#### 4. Kullanıcı Bazlı Atamaları Getir
```http
GET /api/assignment/user/{userId}
```

**Kullanım:** Değerlendirici kendi atamalarını görür

#### 5. Şube Bazlı Atamaları Getir
```http
GET /api/assignment/branch/{branchId}
```

#### 6. Tekil Atama Oluştur
```http
POST /api/assignment
Content-Type: application/json
```

**İç Değerlendirici İçin:**
```json
{
  "projectId": "guid",
  "checklistId": "guid",
  "branchId": "guid",
  "assignedUserId": "guid",
  "externalEmail": null,
  "externalName": null,
  "dueDate": "2026-02-15"
}
```

**Dış Müşteri İçin:**
```json
{
  "projectId": "guid",
  "checklistId": "guid",
  "branchId": null,
  "assignedUserId": null,
  "externalEmail": "musteri@example.com",
  "externalName": "Ahmet Yılmaz",
  "dueDate": "2026-02-15"
}
```

#### 7. Toplu Atama Oluştur
```http
POST /api/assignment/bulk
Content-Type: application/json
```

**Request:**
```json
{
  "projectId": "guid",
  "checklistId": "guid",
  "assignments": [
    {
      "branchId": "guid-1",
      "assignedUserId": "user-guid-1",
      "externalEmail": null,
      "externalName": null,
      "dueDate": "2026-02-15"
    },
    {
      "branchId": "guid-2",
      "assignedUserId": "user-guid-2",
      "externalEmail": null,
      "externalName": null,
      "dueDate": "2026-02-20"
    }
  ]
}
```

**Kullanım:** Excel'den import edilen çoklu atamalar için

#### 8. Atamayı Sil
```http
DELETE /api/assignment/{id}
```

#### 9. Atamayı Tamamla
```http
POST /api/assignment/{id}/complete
```

**Kullanım:** Değerlendirme tamamlandığında otomatik çağrılır

## Atama Senaryoları

### Senaryo 1: İç Değerlendiriciye Atama

**Akış:**
1. Admin bir proje oluşturur (`AssignmentType: Internal`)
2. Değerlendiricilere şube bazlı atama yapar
3. Değerlendirici kendi panelinde atamaları görür
4. Değerlendirici şubeyi ziyaret edip formu doldurur
5. Sistem otomatik olarak atamayı "completed" yapar

**Excel Import Desteği:**
```csv
BranchId,AssignedUserId,DueDate
guid-1,user-guid-1,2026-02-15
guid-2,user-guid-2,2026-02-20
```

Excel verisi `/api/assignment/bulk` endpoint'ine gönderilir.

### Senaryo 2: Dış Müşteriye Anket Gönderimi

**Akış:**
1. Admin bir proje oluşturur (`AssignmentType: External`)
2. Müşteri email adreslerine atama yapar
3. Sistem her müşteri için unique link oluşturur
4. Link email veya SMS ile gönderilir
5. Müşteri linke tıklayıp formu doldurur
6. Sistem otomatik olarak atamayı "completed" yapar

**Önemli:**
- Aynı proje içinde aynı email'e birden fazla **aktif** atama yapılamaz
- Müşteri formu tamamladıktan sonra (`isCompleted: true`) link tekrar kullanılamaz
- Admin yanlışlık durumunda atamayı silebilir ve yeniden oluşturabilir

**Link Formatı:**
```
https://yourdomain.com/survey/{uniqueLink}
```

## İş Kuralları

### 1. Proje Kuralları
- Start date < End date olmalı
- Checklist aktif olmalı
- Proje kapanınca (`IsActive: false`) yeni atama yapılamaz

### 2. Atama Kuralları
- **İç Atama**: `AssignedUserId` zorunlu
- **Dış Atama**: `ExternalEmail` zorunlu
- İkisi de boş olamaz
- Due date project end date'inden sonra olamaz

### 3. Tekil Kullanım (External)
- Aynı email'e aynı proje içinde birden fazla aktif atama yapılamaz
- `ExistsByEmailAsync` kontrolü ile sağlanır
- Completed olan atamalara tekrar atama yapılabilir

### 4. Toplu Atama
- Maksimum 1000 atama bir seferde
- Her atama için unique link oluşturulur
- Transaction içinde çalışır (hepsi başarılı veya hepsi fail)

## Domain Modeli

### Project
- **Id**: Guid
- **Name**: string
- **Description**: string?
- **ChecklistId**: Guid (FK)
- **AssignmentType**: AssignmentType enum
- **StartDate**: DateTime
- **EndDate**: DateTime
- **IsActive**: bool

### Assignment
- **Id**: Guid
- **ProjectId**: Guid (FK)
- **ChecklistId**: Guid (FK)
- **BranchId**: Guid? (FK)
- **AssignedUserId**: Guid? (FK)
- **ExternalEmail**: string?
- **ExternalName**: string?
- **UniqueLink**: string (unique)
- **DueDate**: DateTime
- **IsCompleted**: bool
- **CompletedAt**: DateTime?

## Service Layer

### IProjectService
```csharp
Task<ProjectDto?> GetByIdAsync(Guid id);
Task<IEnumerable<ProjectDto>> GetAllAsync(bool includeInactive);
Task<ProjectDto> CreateAsync(CreateProjectDto dto);
Task<ProjectDto> UpdateAsync(Guid id, CreateProjectDto dto);
Task<bool> DeleteAsync(Guid id);
Task<ProjectDto> CloseProjectAsync(Guid id);
```

### IAssignmentService
```csharp
Task<AssignmentDto?> GetByIdAsync(Guid id);
Task<AssignmentDto?> GetByUniqueLinkAsync(string uniqueLink);
Task<IEnumerable<AssignmentDto>> GetByProjectIdAsync(Guid projectId);
Task<IEnumerable<AssignmentDto>> GetByUserIdAsync(Guid userId);
Task<IEnumerable<AssignmentDto>> GetByBranchIdAsync(Guid branchId);
Task<AssignmentDto> CreateAsync(CreateAssignmentDto dto);
Task<IEnumerable<AssignmentDto>> CreateBulkAsync(BulkAssignmentDto dto);
Task<bool> DeleteAsync(Guid id);
Task<AssignmentDto> CompleteAssignmentAsync(Guid id);
```

## Repository Layer

### IProjectRepository
```csharp
Task<Project?> GetByIdAsync(Guid id, bool includeDetails);
Task<IEnumerable<Project>> GetAllAsync(bool includeInactive);
Task<Project> CreateAsync(Project project);
Task<Project> UpdateAsync(Project project);
Task<bool> DeleteAsync(Guid id);
Task<bool> ExistsAsync(Guid id);
```

### IAssignmentRepository
```csharp
Task<Assignment?> GetByIdAsync(Guid id, bool includeDetails);
Task<Assignment?> GetByUniqueLinkAsync(string uniqueLink, bool includeDetails);
Task<IEnumerable<Assignment>> GetByProjectIdAsync(Guid projectId);
Task<IEnumerable<Assignment>> GetByUserIdAsync(Guid userId);
Task<IEnumerable<Assignment>> GetByBranchIdAsync(Guid branchId);
Task<Assignment> CreateAsync(Assignment assignment);
Task<IEnumerable<Assignment>> CreateBulkAsync(IEnumerable<Assignment> assignments);
Task<Assignment> UpdateAsync(Assignment assignment);
Task<bool> DeleteAsync(Guid id);
Task<bool> ExistsByEmailAsync(Guid projectId, string email);
```

## Excel Import İşlemi

Excel import işlemi için Python servisi kullanılacak (sonraki modülde detaylandırılacak).

**Örnek Excel Formatı:**

### İç Atama:
| BranchId | AssignedUserId | DueDate |
|----------|---------------|---------|
| guid-1   | user-guid-1   | 2026-02-15 |
| guid-2   | user-guid-2   | 2026-02-20 |

### Dış Atama:
| ExternalEmail | ExternalName | DueDate |
|--------------|--------------|---------|
| ahmet@example.com | Ahmet Yılmaz | 2026-02-15 |
| mehmet@example.com | Mehmet Demir | 2026-02-20 |

**İşlem Akışı:**
1. Admin Excel dosyasını yükler
2. Python servisi Excel'i parse eder
3. JSON array oluşturur
4. `/api/assignment/bulk` endpoint'ine POST yapar
5. Tüm atamalar transaction içinde oluşturulur

## Validation Rules

### CreateProjectDto
- **Name**: Required, MaxLength(255)
- **ChecklistId**: Required, must exist
- **AssignmentType**: Valid enum value (Internal, External)
- **StartDate < EndDate**: Required

### CreateAssignmentDto
- **ProjectId**: Required, must exist
- **ChecklistId**: Required, must exist
- **AssignedUserId OR ExternalEmail**: One of them required
- **ExternalEmail**: Valid email format
- **DueDate**: Cannot be in past

## Error Handling

### 400 Bad Request
- Invalid model state
- Missing required fields
- Duplicate email (external assignment)
- Invalid date range

### 404 Not Found
- Project not found
- Checklist not found
- Assignment not found

### 500 Internal Server Error
- Database errors
- Transaction rollback

## Performance Considerations

### Bulk Operations
- Bulk insert için EF Core `AddRange` kullanılır
- Transaction içinde çalışır
- Maksimum 1000 kayıt limiti

### Eager Loading
- `GetByUniqueLinkAsync` için checklist detayları eager load edilir
- Unnecessary joins önlenir
- Include stratejisi query'ye göre optimize edilir

---
**Oluşturulma Tarihi**: 2025-11-22
**Versiyon**: 1.0
**Sonraki Adım**: Dashboard ve Raporlama Modülü
