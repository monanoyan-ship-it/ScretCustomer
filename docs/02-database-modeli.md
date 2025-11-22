# Database Modeli ve Entity Framework Yapılandırması

## Database Şeması

### Entities (Varlıklar)

#### 1. User (Kullanıcı)
- **Id**: Guid (PK)
- **Username**: string (100) - Unique
- **Email**: string (255) - Unique
- **PasswordHash**: string
- **FirstName**: string (100)
- **LastName**: string (100)
- **Role**: UserRole (Enum)
- **BranchId**: Guid? (FK)
- Base fields: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted

**İlişkiler:**
- Branch (Many-to-One)
- Assignments (One-to-Many)
- Evaluations (One-to-Many)

#### 2. Branch (Şube)
- **Id**: Guid (PK)
- **Name**: string (255)
- **Code**: string (50)
- **Address**: string?
- **City**: string?
- **Region**: string?
- **IsActive**: bool

**İlişkiler:**
- Users (One-to-Many)
- Assignments (One-to-Many)

#### 3. Checklist (Kontrol Listesi)
- **Id**: Guid (PK)
- **Name**: string (255)
- **Description**: string (1000)
- **IsScored**: bool (puanlı/puansız)
- **IsActive**: bool
- **Version**: int

**İlişkiler:**
- Sections (One-to-Many)
- Assignments (One-to-Many)

#### 4. Section (Bölüm)
- **Id**: Guid (PK)
- **ChecklistId**: Guid (FK)
- **Name**: string (255)
- **Description**: string?
- **Order**: int

**İlişkiler:**
- Checklist (Many-to-One)
- Questions (One-to-Many)

#### 5. Question (Soru)
- **Id**: Guid (PK)
- **SectionId**: Guid (FK)
- **Text**: string (1000)
- **Type**: QuestionType (Enum)
- **Order**: int
- **Points**: int (puan değeri)
- **AllowNA**: bool (N/A seçeneği)
- **IsRequired**: bool
- **OptionsJson**: jsonb (PostgreSQL)

**İlişkiler:**
- Section (Many-to-One)
- Answers (One-to-Many)

#### 6. Project (Proje)
- **Id**: Guid (PK)
- **Name**: string (255)
- **Description**: string?
- **ChecklistId**: Guid (FK)
- **AssignmentType**: AssignmentType (Enum)
- **StartDate**: DateTime
- **EndDate**: DateTime
- **IsActive**: bool

**İlişkiler:**
- Checklist (Many-to-One)
- Assignments (One-to-Many)

#### 7. Assignment (Atama)
- **Id**: Guid (PK)
- **ProjectId**: Guid (FK)
- **ChecklistId**: Guid (FK)
- **BranchId**: Guid? (FK)
- **AssignedUserId**: Guid? (FK)
- **ExternalEmail**: string? (dış müşteri)
- **ExternalName**: string? (dış müşteri)
- **UniqueLink**: string (500) - Unique
- **DueDate**: DateTime
- **IsCompleted**: bool
- **CompletedAt**: DateTime?

**İlişkiler:**
- Project (Many-to-One)
- Checklist (Many-to-One)
- Branch (Many-to-One)
- AssignedUser (Many-to-One)
- Evaluations (One-to-Many)

#### 8. Evaluation (Değerlendirme)
- **Id**: Guid (PK)
- **AssignmentId**: Guid (FK)
- **EvaluatorId**: Guid? (FK)
- **Status**: EvaluationStatus (Enum)
- **TotalScore**: decimal(10,2)?
- **MaxScore**: decimal(10,2)?
- **ScorePercentage**: decimal(5,2)?
- **StartedAt**: DateTime?
- **CompletedAt**: DateTime?
- **Notes**: string (2000)?

**İlişkiler:**
- Assignment (Many-to-One)
- Evaluator/User (Many-to-One)
- Answers (One-to-Many)

#### 9. Answer (Cevap)
- **Id**: Guid (PK)
- **EvaluationId**: Guid (FK)
- **QuestionId**: Guid (FK)
- **AnswerText**: string (2000)?
- **AnswerNumeric**: int? (Likert, Star için)
- **IsNA**: bool
- **EarnedPoints**: decimal(10,2)?

**İlişkiler:**
- Evaluation (Many-to-One)
- Question (Many-to-One)

## Enums

### UserRole
```csharp
public enum UserRole
{
    Admin = 1,              // Sistem yöneticisi
    TeamLeader = 2,         // Takım lideri
    Evaluator = 3,          // Değerlendirici
    CustomerRepresentative = 4  // Müşteri temsilcisi
}
```

### QuestionType
```csharp
public enum QuestionType
{
    MultipleChoice = 1,     // Çoktan seçmeli
    Likert = 2,             // Likert ölçeği (1-5)
    Star = 3,               // Yıldızlı değerlendirme (1-5)
    Text = 4                // Metin alanı
}
```

### AssignmentType
```csharp
public enum AssignmentType
{
    Internal = 1,  // İç değerlendiricilere atama
    External = 2   // Dış müşterilere anket
}
```

### EvaluationStatus
```csharp
public enum EvaluationStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3
}
```

## EF Core Configurations

### DbContext Özellikleri
- **Soft Delete**: Global query filter ile silinmiş kayıtlar otomatik filtrelenir
- **Audit Fields**: CreatedAt, UpdatedAt otomatik güncellenir
- **PostgreSQL JSON**: OptionsJson alanı jsonb tipinde saklanır

### Migration Komutları

#### EF Core Tools Kurulumu
```bash
# Global tool
dotnet tool install --global dotnet-ef

# veya Local tool
dotnet new tool-manifest
dotnet tool install dotnet-ef
```

#### Migration Oluşturma
```bash
dotnet ef migrations add InitialCreate \
  --project Backend/SecretCustomer.Data/SecretCustomer.Data.csproj \
  --startup-project Backend/SecretCustomer.API/SecretCustomer.API.csproj
```

#### Database Güncelleme
```bash
dotnet ef database update \
  --project Backend/SecretCustomer.Data/SecretCustomer.Data.csproj \
  --startup-project Backend/SecretCustomer.API/SecretCustomer.API.csproj
```

## N/A Puan Yeniden Dağıtım Algoritması

N/A olarak işaretlenen sorular için puan hesaplama mantığı:

1. Tüm soruların toplam puanını hesapla
2. N/A işaretli soruların puanını toplam puandan çıkar
3. Kalan soruların orijinal ağırlık oranlarını koru
4. Yeni toplam puanı hesapla

**Örnek:**
```
Soru 1: 20 puan
Soru 2: 30 puan (N/A)
Soru 3: 50 puan

Toplam: 100 puan
N/A sonrası toplam: 70 puan

Soru 1 yeni ağırlık: (20/70) * 100 = 28.57%
Soru 3 yeni ağırlık: (50/70) * 100 = 71.43%
```

## Connection String Yapılandırması

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=SecretCustomerDB;Username=postgres;Password=yourpassword"
  }
}
```

**Program.cs:**
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

## Güvenlik Notları

1. **Soft Delete**: Veriler fiziksel olarak silinmez, `IsDeleted` flag'i kullanılır
2. **Audit Trail**: Tüm değişiklikler CreatedBy/UpdatedBy ile takip edilir
3. **Unique Constraints**: Username, Email ve UniqueLink alanları unique'tir
4. **Cascade Delete**: İlişkili verilerin silinme davranışları dikkatli yapılandırılmıştır

---
**Oluşturulma Tarihi**: 2025-11-22
**Versiyon**: 1.0
