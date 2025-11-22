# Database Setup ve Migration Rehberi

## Özet
PostgreSQL database kurulumu, EF Core migration oluşturma ve seed data ekleme rehberi.

## Önkoşullar

### 1. PostgreSQL Kurulumu
```bash
# PostgreSQL 15+ indir ve kur
# Windows: https://www.postgresql.org/download/windows/
# Linux: sudo apt-get install postgresql-15
# Mac: brew install postgresql@15
```

### 2. Database Oluşturma
```sql
-- PostgreSQL'e bağlan (psql veya pgAdmin ile)
CREATE DATABASE SecretCustomerDB;

-- Kullanıcı oluştur (isteğe bağlı)
CREATE USER secretcustomer_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE SecretCustomerDB TO secretcustomer_user;
```

### 3. Connection String Güncelleme
**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=SecretCustomerDB;Username=postgres;Password=your_password"
  }
}
```

**Güvenlik için appsettings.Development.json kullan:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=SecretCustomerDB;Username=postgres;Password=development_password"
  }
}
```

## EF Core Tools Kurulumu

### Opsiyon 1: Global Tool
```bash
dotnet tool install --global dotnet-ef
```

**Hata alırsanız:**
```bash
# Mevcut tool'u kaldır
dotnet tool uninstall --global dotnet-ef

# Tekrar kur
dotnet tool install --global dotnet-ef --version 9.0.0
```

### Opsiyon 2: Local Tool (Önerilen)
```bash
# Proje root directory'de
dotnet new tool-manifest  # Eğer yoksa

# dotnet-ef'i local tool olarak kur
dotnet tool install dotnet-ef

# Kullanım:
dotnet ef migrations add InitialCreate
```

### Opsiyon 3: Package Manager Console (Visual Studio)
```powershell
# Package Manager Console'da
Add-Migration InitialCreate
Update-Database
```

## Migration Oluşturma

### 1. İlk Migration
```bash
cd Backend/SecretCustomer.Data

# Migration oluştur
dotnet ef migrations add InitialCreate \
    --startup-project ../SecretCustomer.API/SecretCustomer.API.csproj \
    --context ApplicationDbContext \
    --output-dir Migrations

# veya local tool ile:
dotnet ef migrations add InitialCreate --startup-project ../SecretCustomer.API
```

### 2. Database'i Güncelle
```bash
# Migration'ları uygula
dotnet ef database update \
    --startup-project ../SecretCustomer.API/SecretCustomer.API.csproj \
    --context ApplicationDbContext

# veya local tool ile:
dotnet ef database update --startup-project ../SecretCustomer.API
```

### 3. Migration'ı Geri Al
```bash
# Son migration'ı geri al
dotnet ef migrations remove --startup-project ../SecretCustomer.API

# Belirli bir migration'a geri dön
dotnet ef database update PreviousMigrationName --startup-project ../SecretCustomer.API
```

## Manuel SQL Script Alternatifi

### Migration SQL Script Oluştur
```bash
# Migration'dan SQL script üret
dotnet ef migrations script \
    --startup-project ../SecretCustomer.API/SecretCustomer.API.csproj \
    --context ApplicationDbContext \
    --output migration.sql
```

**Oluşan migration.sql'i PostgreSQL'de çalıştır:**
```bash
psql -U postgres -d SecretCustomerDB -f migration.sql
```

## Seed Data Oluşturma

### 1. Seed Data Script
**Backend/SecretCustomer.Data/SeedData.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using BCrypt.Net;

namespace SecretCustomer.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Database'in oluşturulduğundan emin ol
        await context.Database.EnsureCreatedAsync();

        // Zaten data varsa skip et
        if (context.Users.Any()) return;

        // 1. Users (Admin, TeamLeader, Evaluator)
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@secretcustomer.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FirstName = "Admin",
            LastName = "User",
            Role = UserRole.Admin,
            IsActive = true
        };

        var teamLeader = new User
        {
            Id = Guid.NewGuid(),
            Username = "teamleader",
            Email = "teamleader@secretcustomer.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Leader@123"),
            FirstName = "Team",
            LastName = "Leader",
            Role = UserRole.TeamLeader,
            IsActive = true
        };

        var evaluator1 = new User
        {
            Id = Guid.NewGuid(),
            Username = "evaluator1",
            Email = "evaluator1@secretcustomer.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Eval@123"),
            FirstName = "John",
            LastName = "Evaluator",
            Role = UserRole.Evaluator,
            IsActive = true
        };

        context.Users.AddRange(adminUser, teamLeader, evaluator1);

        // 2. Branches
        var branch1 = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "İstanbul Kadıköy",
            Address = "Kadıköy, İstanbul",
            City = "İstanbul",
            Region = "Marmara"
        };

        var branch2 = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Ankara Kızılay",
            Address = "Kızılay, Ankara",
            City = "Ankara",
            Region = "İç Anadolu"
        };

        context.Branches.AddRange(branch1, branch2);

        // 3. Checklist
        var checklist = new Checklist
        {
            Id = Guid.NewGuid(),
            Name = "Restaurant Monthly Evaluation",
            Description = "Standart aylık restaurant değerlendirme formu",
            Version = 1,
            IsActive = true
        };

        context.Checklists.Add(checklist);
        await context.SaveChangesAsync(); // Save to get checklist ID

        // 4. Sections
        var section1 = new Section
        {
            Id = Guid.NewGuid(),
            ChecklistId = checklist.Id,
            Name = "Temizlik",
            Order = 1
        };

        var section2 = new Section
        {
            Id = Guid.NewGuid(),
            ChecklistId = checklist.Id,
            Name = "Hizmet Kalitesi",
            Order = 2
        };

        context.Sections.AddRange(section1, section2);
        await context.SaveChangesAsync();

        // 5. Questions
        var questions = new List<Question>
        {
            new Question
            {
                Id = Guid.NewGuid(),
                SectionId = section1.Id,
                Text = "Masalar temiz mi?",
                QuestionType = QuestionType.YesNo,
                Points = 5,
                AllowNA = false,
                Order = 1
            },
            new Question
            {
                Id = Guid.NewGuid(),
                SectionId = section1.Id,
                Text = "Genel temizlik puanı",
                QuestionType = QuestionType.Rating,
                Points = 10,
                AllowNA = true,
                Order = 2
            },
            new Question
            {
                Id = Guid.NewGuid(),
                SectionId = section2.Id,
                Text = "Karşılama nasıldı?",
                QuestionType = QuestionType.MultipleChoice,
                Points = 5,
                AllowNA = false,
                Options = "Mükemmel,İyi,Orta,Kötü",
                Order = 1
            },
            new Question
            {
                Id = Guid.NewGuid(),
                SectionId = section2.Id,
                Text = "Ek yorumlar",
                QuestionType = QuestionType.Text,
                Points = 0,
                AllowNA = false,
                Order = 2
            }
        };

        context.Questions.AddRange(questions);

        // 6. Project
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "2025 Q1 Restaurant Evaluation",
            Description = "2025 yılı 1. çeyrek restaurant değerlendirme projesi",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(3),
            IsActive = true
        };

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        Console.WriteLine("Seed data başarıyla oluşturuldu!");
    }
}
```

### 2. Seed Data'yı Program.cs'e Ekle
**Backend/SecretCustomer.API/Program.cs:**
```csharp
// ... existing code ...

var app = builder.Build();

// Seed data (Development environment için)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await SeedData.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Seed data oluşturulurken hata oluştu");
    }
}

// ... rest of the code ...
```

### 3. Seed Data Çalıştırma
```bash
cd Backend/SecretCustomer.API
dotnet run
```

**İlk çalıştırmada:**
- Database otomatik oluşturulur (eğer yoksa)
- Seed data eklenir
- Admin, TeamLeader, Evaluator kullanıcıları oluşturulur

## Verification (Doğrulama)

### 1. PostgreSQL'de Kontrol Et
```sql
-- Tables
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public';

-- User count
SELECT COUNT(*) FROM "Users";

-- Sample data
SELECT "Username", "Email", "Role" FROM "Users";
SELECT * FROM "Branches";
SELECT * FROM "Checklists";
```

### 2. API ile Test Et
```bash
# Login test
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin@123"
  }'

# Should return JWT token + user info
```

## Migration Best Practices

### 1. Naming Convention
```bash
# Feature-based names
dotnet ef migrations add AddUserEmailVerification
dotnet ef migrations add UpdateChecklistSchema
dotnet ef migrations add CreateAuditLogsTable

# Date-based names (alternative)
dotnet ef migrations add Migration_20250123_AddIndexes
```

### 2. Migration Review
```bash
# Migration'ı önce script olarak gör
dotnet ef migrations script --startup-project ../SecretCustomer.API

# Kontrol et, sonra uygula
dotnet ef database update --startup-project ../SecretCustomer.API
```

### 3. Rollback Plan
```bash
# Önceki migration'a geri dön
dotnet ef database update PreviousMigrationName --startup-project ../SecretCustomer.API

# Migration'ı sil (eğer henüz uygulanmadıysa)
dotnet ef migrations remove --startup-project ../SecretCustomer.API
```

## Common Issues & Solutions

### Issue 1: "Settings file 'DotnetToolSettings.xml' was not found"
**Solution:**
```bash
# NuGet cache temizle
dotnet nuget locals all --clear

# Tool'u tekrar kur
dotnet tool install --global dotnet-ef --version 9.0.0
```

### Issue 2: "No DbContext was found"
**Solution:**
```bash
# --context parametresi ekle
dotnet ef migrations add InitialCreate --context ApplicationDbContext
```

### Issue 3: "A network-related or instance-specific error"
**Solution:**
```bash
# PostgreSQL servisinin çalıştığından emin ol
# Windows: services.msc -> postgresql-x64-15
# Linux: sudo systemctl start postgresql
# Mac: brew services start postgresql@15

# Connection string'i kontrol et
# appsettings.json -> ConnectionStrings -> DefaultConnection
```

### Issue 4: "Password authentication failed"
**Solution:**
```bash
# PostgreSQL kullanıcı şifresi doğru mu?
psql -U postgres

# Şifre değiştir
ALTER USER postgres WITH PASSWORD 'new_password';
```

### Issue 5: "Database already exists"
**Solution:**
```bash
# Var olan database'i sil (DİKKATLİ!)
DROP DATABASE SecretCustomerDB;

# Yeniden oluştur
CREATE DATABASE SecretCustomerDB;

# Migration'ları uygula
dotnet ef database update
```

## Production Deployment

### 1. Migration Script Oluştur
```bash
# Production SQL script
dotnet ef migrations script \
    --startup-project ../SecretCustomer.API \
    --idempotent \
    --output production-migration.sql
```

**--idempotent flag**: Aynı script birden fazla çalıştırılabilir (güvenli).

### 2. Backup Al
```bash
# PostgreSQL backup
pg_dump -U postgres SecretCustomerDB > backup_$(date +%Y%m%d).sql

# Restore
psql -U postgres SecretCustomerDB < backup_20250123.sql
```

### 3. Migration'ı Uygula
```bash
# Production'da:
psql -U production_user -d SecretCustomerDB -f production-migration.sql
```

### 4. Seed Data (Production)
```csharp
// Program.cs - Production seed data (sadece admin)
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Sadece admin user oluştur (eğer yoksa)
    if (!await context.Users.AnyAsync())
    {
        var admin = new User
        {
            Username = "admin",
            Email = "admin@production.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Environment.GetEnvironmentVariable("ADMIN_PASSWORD")),
            FirstName = "Admin",
            LastName = "User",
            Role = UserRole.Admin,
            IsActive = true
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}
```

## Database Schema Özeti

### Tables (9 total)
1. **Users**: Kullanıcılar (Admin, TeamLeader, Evaluator, CustomerRepresentative)
2. **Branches**: Şubeler
3. **Checklists**: Kontrol listeleri
4. **Sections**: Kontrol listesi bölümleri
5. **Questions**: Sorular (4 tip: YesNo, Rating, Text, MultipleChoice)
6. **Projects**: Projeler
7. **Assignments**: Atamalar (Internal/External)
8. **Evaluations**: Değerlendirmeler
9. **Answers**: Cevaplar

### Relationships
```
Checklist 1--* Section 1--* Question
Project 1--* Assignment *--1 Branch
Assignment *--1 User (Evaluator)
Assignment 1--1 Evaluation 1--* Answer *--1 Question
```

### Indexes (Önerilen)
```sql
-- Performance için indexler ekle
CREATE INDEX idx_users_email ON "Users"("Email");
CREATE INDEX idx_users_role ON "Users"("Role");
CREATE INDEX idx_assignments_status ON "Assignments"("Status");
CREATE INDEX idx_evaluations_assignmentid ON "Evaluations"("AssignmentId");
CREATE INDEX idx_answers_questionid ON "Answers"("QuestionId");
```

## Next Steps

1. ✅ Database'i oluştur (PostgreSQL)
2. ✅ Connection string'i ayarla (appsettings.json)
3. ⏳ EF Core tools kur (global veya local)
4. ⏳ Migration oluştur (`dotnet ef migrations add InitialCreate`)
5. ⏳ Migration'ı uygula (`dotnet ef database update`)
6. ⏳ Seed data ekle (Program.cs)
7. ⏳ API'yi çalıştır ve test et (`dotnet run`)
8. ⏳ Frontend ile entegre et

## Kaynaklar

- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [EF Core Tools Reference](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
- [Connection Strings](https://www.connectionstrings.com/postgresql/)

---
**Database Setup Rehberi Hazır!**

Bu dokümantasyonu takip ederek database'inizi kurabilir ve migration'ları çalıştırabilirsiniz.

**Manuel olarak yapmanız gerekenler:**
1. PostgreSQL kur ve SecretCustomerDB oluştur
2. `dotnet ef` tool'u kur (global veya local)
3. Migration oluştur ve uygula
4. Seed data ekle
5. Test et

**dotnet-ef sorunu devam ederse:**
- Visual Studio Package Manager Console kullan
- Veya migration SQL script'i manuel oluştur ve psql ile çalıştır
