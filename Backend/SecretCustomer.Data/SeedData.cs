using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using System.Text.Json;

namespace SecretCustomer.Data;

public static class SeedData
{
    /// <summary>
    /// Production için temiz kurulum - sadece admin, diller ve temel ayarlar
    /// </summary>
    public static async Task InitializeProductionAsync(ApplicationDbContext context, ILogger logger, string basePath)
    {
        try
        {
            // Database migrations uygula
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied");

            // Zaten data varsa skip et
            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("Database already initialized");
                return;
            }

            logger.LogInformation("Starting PRODUCTION database initialization...");

            // 1. Admin User
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@secretcustomer.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FirstName = "Sistem",
                LastName = "Yöneticisi",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            logger.LogInformation("Admin user created: admin / Admin@123");

            // 2. Permissions
            await SeedPermissionsAsync(context, logger);

            // 3. App Settings
            await SeedAppSettingsAsync(context, logger);

            // 4. System Settings (Dashboard hedefleri, vb.)
            await SeedSystemSettingsAsync(context, logger);

            // 5. Languages with XML import
            await SeedLanguagesWithImportAsync(context, logger, basePath);

            logger.LogInformation("===========================================");
            logger.LogInformation("PRODUCTION database initialization completed!");
            logger.LogInformation("Admin Login: admin / Admin@123");
            logger.LogInformation("===========================================");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during production initialization");
            throw;
        }
    }

    /// <summary>
    /// Development/Test için örnek veriler dahil kurulum
    /// </summary>
    public static async Task InitializeAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Database migrations uygula
            await context.Database.MigrateAsync();

            // Zaten data varsa skip et
            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("Database already seeded");
                return;
            }

            logger.LogInformation("Starting DEVELOPMENT database seed...");

            // 1. Users (Admin, TeamLeader, Evaluator)
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@secretcustomer.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FirstName = "Admin",
                LastName = "User",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var teamLeader = new User
            {
                Username = "teamleader",
                Email = "teamleader@secretcustomer.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Leader@123"),
                FirstName = "Team",
                LastName = "Leader",
                Role = UserRole.QualitySpecialist,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var evaluator1 = new User
            {
                Username = "evaluator1",
                Email = "evaluator1@secretcustomer.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Eval@123"),
                FirstName = "John",
                LastName = "Evaluator",
                Role = UserRole.QualitySpecialist,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var evaluator2 = new User
            {
                Username = "evaluator2",
                Email = "evaluator2@secretcustomer.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Eval@123"),
                FirstName = "Jane",
                LastName = "Evaluator",
                Role = UserRole.QualitySpecialist,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(adminUser, teamLeader, evaluator1, evaluator2);
            await context.SaveChangesAsync();
            logger.LogInformation("Users created");

            // 1b. Field Worker Users - Saha çalışanları için sistem kullanıcıları
            var fieldWorkerUser1 = new User
            {
                Username = "ali.veli",
                Email = "ali.veli@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Ali",
                LastName = "Veli",
                Role = UserRole.FieldWorker,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorkerUser2 = new User
            {
                Username = "zeynep.yildiz",
                Email = "zeynep.yildiz@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Zeynep",
                LastName = "Yıldız",
                Role = UserRole.FieldWorker,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorkerUser3 = new User
            {
                Username = "murat.koc",
                Email = "murat.koc@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Murat",
                LastName = "Koç",
                Role = UserRole.FieldWorker,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorkerUser4 = new User
            {
                Username = "elif.sahin",
                Email = "elif.sahin@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Elif",
                LastName = "Şahin",
                Role = UserRole.FieldWorker,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorkerUser5 = new User
            {
                Username = "burak.tekin",
                Email = "burak.tekin@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Burak",
                LastName = "Tekin",
                Role = UserRole.FieldWorker,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorkerUser6 = new User
            {
                Username = "selin.aydin",
                Email = "selin.aydin@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Selin",
                LastName = "Aydın",
                Role = UserRole.FieldWorker,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorkerUser7 = new User
            {
                Username = "emre.celik",
                Email = "emre.celik@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Emre",
                LastName = "Çelik",
                Role = UserRole.FieldWorker,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorkerUser8 = new User
            {
                Username = "ayse.kara",
                Email = "ayse.kara@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Ayşe",
                LastName = "Kara",
                Role = UserRole.FieldWorker,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(fieldWorkerUser1, fieldWorkerUser2, fieldWorkerUser3,
                fieldWorkerUser4, fieldWorkerUser5, fieldWorkerUser6, fieldWorkerUser7, fieldWorkerUser8);
            await context.SaveChangesAsync();
            logger.LogInformation("Field Worker Users created");

            // 2. Checklist
            var checklist = new Checklist
            {
                Name = "Restaurant Monthly Evaluation",
                Description = "Standart aylık restaurant değerlendirme formu",
                Version = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Checklists.Add(checklist);
            await context.SaveChangesAsync();
            logger.LogInformation("Checklist created");

            // 3. Sections
            var section1 = new Section
            {
                ChecklistId = checklist.Id,
                Name = "Temizlik ve Hijyen",
                Order = 1,
                CreatedAt = DateTime.UtcNow
            };

            var section2 = new Section
            {
                ChecklistId = checklist.Id,
                Name = "Hizmet Kalitesi",
                Order = 2,
                CreatedAt = DateTime.UtcNow
            };

            var section3 = new Section
            {
                ChecklistId = checklist.Id,
                Name = "Ürün Kalitesi",
                Order = 3,
                CreatedAt = DateTime.UtcNow
            };

            context.Sections.AddRange(section1, section2, section3);
            await context.SaveChangesAsync();
            logger.LogInformation("Sections created");

            // 4. Questions - Direkt checklist'e bağlı
            var questions = new List<Question>
            {
                // Temizlik ve Hijyen Kriterleri
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section1.Id, // Geriye uyumluluk için
                    Text = "Masalar temiz mi?",
                    ScoringType = ScoringType.Scored,
                    WeightPoints = 5,
                    MaxPoints = 4,
                    PenaltyType = PenaltyType.None,
                    AllowNA = false,
                    IsRequired = true,
                    Order = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section1.Id,
                    Text = "Tuvalet temizliği nasıl?",
                    ScoringType = ScoringType.Scored,
                    WeightPoints = 10,
                    MaxPoints = 4,
                    PenaltyType = PenaltyType.None,
                    AllowNA = false,
                    IsRequired = true,
                    Order = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section1.Id,
                    Text = "Genel temizlik hakkında ek gözlemler",
                    ScoringType = ScoringType.Unscored, // Puansız - sadece yorum
                    WeightPoints = 0,
                    MaxPoints = 1,
                    PenaltyType = PenaltyType.None,
                    AllowNA = true,
                    IsRequired = false,
                    Order = 3,
                    CreatedAt = DateTime.UtcNow
                },

                // Hizmet Kalitesi Kriterleri
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section2.Id,
                    Text = "Karşılama nasıldı?",
                    ScoringType = ScoringType.Scored,
                    WeightPoints = 5,
                    MaxPoints = 4,
                    PenaltyType = PenaltyType.None,
                    AllowNA = false,
                    IsRequired = true,
                    Order = 4,
                    HelpText = "Müşteri girişinde karşılama kalitesini değerlendirin",
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section2.Id,
                    Text = "Sipariş alma süresi uygun muydu?",
                    ScoringType = ScoringType.Scored,
                    WeightPoints = 5,
                    MaxPoints = 4,
                    PenaltyType = PenaltyType.None,
                    AllowNA = true,
                    IsRequired = true,
                    Order = 5,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section2.Id,
                    Text = "Personel ilgisi nasıldı?",
                    ScoringType = ScoringType.Scored,
                    WeightPoints = 10,
                    MaxPoints = 4,
                    PenaltyType = PenaltyType.None,
                    AllowNA = false,
                    IsRequired = true,
                    Order = 6,
                    CreatedAt = DateTime.UtcNow
                },

                // Ürün Kalitesi Kriterleri
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section3.Id,
                    Text = "Yemek sıcaklığı uygun muydu?",
                    ScoringType = ScoringType.Scored,
                    WeightPoints = 5,
                    MaxPoints = 4,
                    PenaltyType = PenaltyType.None,
                    AllowNA = false,
                    IsRequired = true,
                    Order = 7,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section3.Id,
                    Text = "Yemek lezzeti nasıldı?",
                    ScoringType = ScoringType.Scored,
                    WeightPoints = 15,
                    MaxPoints = 4,
                    PenaltyType = PenaltyType.None,
                    AllowNA = false,
                    IsRequired = true,
                    Order = 8,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section3.Id,
                    Text = "Porsiyon büyüklüğü uygun mu?",
                    ScoringType = ScoringType.Penalty, // Cezalı soru örneği
                    WeightPoints = 10, // Ceza miktarı (eski PenaltyValue)
                    MaxPoints = 2,
                    PenaltyType = PenaltyType.YellowCard,
                    AllowNA = true,
                    IsRequired = true,
                    Order = 9,
                    HelpText = "SARI KART: Porsiyon standart boyutun altındaysa 10 puan düşülür",
                    CreatedAt = DateTime.UtcNow
                },

                // ========== CEZALI SORULAR ÖRNEKLERİ ==========
                // SARI KART: Hafif ihlaller - düşük puan kesintisi
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section1.Id, // Temizlik
                    Text = "Personel isim kartı takıyor mu?",
                    ScoringType = ScoringType.Penalty,
                    WeightPoints = 5, // Ceza miktarı: 5 puan düşürülür
                    MaxPoints = 2, // Evet/Hayır
                    PenaltyType = PenaltyType.YellowCard,
                    AllowNA = false,
                    IsRequired = true,
                    Order = 10,
                    HelpText = "SARI KART: İsim kartı yoksa toplam puandan 5 puan düşülür",
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section2.Id, // Hizmet
                    Text = "Müşteriye güler yüzle yaklaşıldı mı?",
                    ScoringType = ScoringType.Penalty,
                    WeightPoints = 8, // Ceza miktarı: 8 puan düşülür
                    MaxPoints = 2,
                    PenaltyType = PenaltyType.YellowCard,
                    AllowNA = false,
                    IsRequired = true,
                    Order = 11,
                    HelpText = "SARI KART: Güler yüz yoksa 8 puan düşülür",
                    CreatedAt = DateTime.UtcNow
                },

                // KIRMIZI KART: Ciddi ihlaller - yüksek puan kesintisi veya sıfırlama
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section1.Id, // Temizlik
                    Text = "Hijyen kurallarına uyuluyor mu? (Eldiven, bone vb.)",
                    ScoringType = ScoringType.Penalty,
                    WeightPoints = 25, // Ciddi ceza: 25 puan düşülür
                    MaxPoints = 2,
                    PenaltyType = PenaltyType.RedCard,
                    AllowNA = false,
                    IsRequired = true,
                    Order = 12,
                    HelpText = "KIRMIZI KART: Hijyen ihlali tespit edilirse 25 puan düşülür",
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section2.Id, // Hizmet
                    Text = "Müşteriye saygısız davranış var mı?",
                    ScoringType = ScoringType.Penalty,
                    WeightPoints = 50, // Çok ciddi ceza: 50 puan düşülür
                    MaxPoints = 2,
                    PenaltyType = PenaltyType.RedCard,
                    AllowNA = false,
                    IsRequired = true,
                    Order = 13,
                    HelpText = "KIRMIZI KART: Saygısız davranış tespit edilirse 50 puan düşülür",
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    SectionId = section3.Id, // Ürün
                    Text = "Bozuk/bayat ürün servisi var mı?",
                    ScoringType = ScoringType.Penalty,
                    WeightPoints = 100, // Tam sıfırlama: 100 puan düşülür
                    MaxPoints = 2,
                    PenaltyType = PenaltyType.RedCard,
                    AllowNA = false,
                    IsRequired = true,
                    Order = 14,
                    HelpText = "KIRMIZI KART: Bozuk ürün servisi = DEĞERLENDİRME SIFIRLANIR",
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Questions.AddRange(questions);
            await context.SaveChangesAsync();
            logger.LogInformation("Questions created");

            // 5. Project
            var project = new Project
            {
                Name = "2025 Q1 Restaurant Evaluation",
                Description = "2025 yılı 1. çeyrek restaurant değerlendirme projesi",
                ChecklistId = checklist.Id,
                AssignmentType = AssignmentType.InternalBranch,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(3),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Projects.Add(project);
            await context.SaveChangesAsync();
            logger.LogInformation("Project created");

            // 6. Sample Assignment (Internal)
            var assignment1 = new Assignment
            {
                ProjectId = project.Id,
                ChecklistId = checklist.Id,
                AssignedUserId = evaluator1.Id,
                DueDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            var assignment2 = new Assignment
            {
                ProjectId = project.Id,
                ChecklistId = checklist.Id,
                AssignedUserId = evaluator2.Id,
                DueDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            // Sample Assignment (External)
            var assignment3 = new Assignment
            {
                ProjectId = project.Id,
                ChecklistId = checklist.Id,
                UniqueLink = Guid.NewGuid().ToString("N"),
                DueDate = DateTime.UtcNow.AddDays(14),
                CreatedAt = DateTime.UtcNow
            };

            context.Assignments.AddRange(assignment1, assignment2, assignment3);
            await context.SaveChangesAsync();
            logger.LogInformation("Assignments created");

            // 7. Sample Evaluations
            var evaluation1 = new Evaluation
            {
                AssignmentId = assignment1.Id,
                EvaluatorId = evaluator1.Id,
                Status = Core.Enums.EvaluationStatus.Completed,
                TotalScore = 85.5m,
                MaxScore = 100m,
                ScorePercentage = 85.5m,
                StartedAt = DateTime.UtcNow.AddDays(-5),
                CompletedAt = DateTime.UtcNow.AddDays(-4),
                Notes = "Genel olarak iyi performans",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };

            var evaluation2 = new Evaluation
            {
                AssignmentId = assignment2.Id,
                EvaluatorId = evaluator2.Id,
                Status = Core.Enums.EvaluationStatus.Completed,
                TotalScore = 92.0m,
                MaxScore = 100m,
                ScorePercentage = 92.0m,
                StartedAt = DateTime.UtcNow.AddDays(-3),
                CompletedAt = DateTime.UtcNow.AddDays(-2),
                Notes = "Mükemmel hizmet kalitesi",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            };

            var evaluation3 = new Evaluation
            {
                AssignmentId = assignment3.Id,
                Status = Core.Enums.EvaluationStatus.Completed,
                TotalScore = 78.0m,
                MaxScore = 100m,
                ScorePercentage = 78.0m,
                StartedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow,
                Notes = "Temizlik konusunda iyileştirme gerekli",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            // Geçmiş aylardan ek değerlendirmeler
            var evaluation4 = new Evaluation
            {
                AssignmentId = assignment1.Id,
                EvaluatorId = evaluator1.Id,
                Status = Core.Enums.EvaluationStatus.Completed,
                TotalScore = 88.0m,
                MaxScore = 100m,
                ScorePercentage = 88.0m,
                StartedAt = DateTime.UtcNow.AddMonths(-1).AddDays(-10),
                CompletedAt = DateTime.UtcNow.AddMonths(-1).AddDays(-9),
                Notes = "Geçen ayın değerlendirmesi",
                CreatedAt = DateTime.UtcNow.AddMonths(-1).AddDays(-10)
            };

            var evaluation5 = new Evaluation
            {
                AssignmentId = assignment2.Id,
                EvaluatorId = evaluator2.Id,
                Status = Core.Enums.EvaluationStatus.Completed,
                TotalScore = 75.5m,
                MaxScore = 100m,
                ScorePercentage = 75.5m,
                StartedAt = DateTime.UtcNow.AddMonths(-2).AddDays(-5),
                CompletedAt = DateTime.UtcNow.AddMonths(-2).AddDays(-4),
                Notes = "2 ay önceki değerlendirme",
                CreatedAt = DateTime.UtcNow.AddMonths(-2).AddDays(-5)
            };

            context.Evaluations.AddRange(evaluation1, evaluation2, evaluation3, evaluation4, evaluation5);
            await context.SaveChangesAsync();
            logger.LogInformation("Evaluations created (5 sample evaluations)");

            // 8. Customers
            var customer1 = new Customer
            {
                CompanyName = "ABC Perakende A.Ş.",
                TaxNumber = "1234567890",
                Phone = "0212 123 4567",
                Email = "info@abc.com",
                Address = "Maslak Mahallesi, Büyükdere Caddesi No:123",
                City = "İstanbul",
                IsActive = true,
                ContractStartDate = DateTime.UtcNow.AddMonths(-6),
                ContractEndDate = DateTime.UtcNow.AddMonths(18),
                Notes = "Perakende sektöründe faaliyet gösteren büyük zincir",
                CreatedAt = DateTime.UtcNow
            };

            var customer2 = new Customer
            {
                CompanyName = "XYZ Restaurant Grubu Ltd.",
                TaxNumber = "9876543210",
                Phone = "0216 456 7890",
                Email = "contact@xyz.com",
                Address = "Kadıköy, Moda Caddesi No:45",
                City = "İstanbul",
                IsActive = true,
                ContractStartDate = DateTime.UtcNow.AddMonths(-3),
                ContractEndDate = DateTime.UtcNow.AddMonths(21),
                Notes = "Restoran zinciri - 15 şubesi var",
                CreatedAt = DateTime.UtcNow
            };

            var customer3 = new Customer
            {
                CompanyName = "Otel Zincirleri A.Ş.",
                TaxNumber = "5555666677",
                Phone = "0242 111 2233",
                Email = "info@otelzinciri.com",
                Address = "Konyaaltı, Sahil Yolu No:88",
                City = "Antalya",
                IsActive = true,
                ContractStartDate = DateTime.UtcNow.AddMonths(-12),
                ContractEndDate = DateTime.UtcNow.AddMonths(12),
                Notes = "5 yıldızlı otel zinciri",
                CreatedAt = DateTime.UtcNow
            };

            context.Customers.AddRange(customer1, customer2, customer3);
            await context.SaveChangesAsync();
            logger.LogInformation("Customers created");

            // 9. Customer Personnel
            var personnel1 = new CustomerPersonnel
            {
                CustomerId = customer1.Id,
                Username = "ahmet.yilmaz",
                Email = "ahmet.yilmaz@abc.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                PhoneNumber = "0532 111 2233",
                Department = "Kalite Kontrol",
                Title = "Kalite Müdürü",
                Role = CustomerPersonnelRole.CustomerManager,
                IsActive = true,
                Notes = "ABC Perakende kalite müdürü - tüm yetkilere sahip",
                CreatedAt = DateTime.UtcNow
            };

            var personnel2 = new CustomerPersonnel
            {
                CustomerId = customer1.Id,
                Username = "ayse.demir",
                Email = "ayse.demir@abc.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                FirstName = "Ayşe",
                LastName = "Demir",
                PhoneNumber = "0533 444 5566",
                Department = "Kalite Kontrol",
                Title = "Kalite Uzmanı",
                Role = CustomerPersonnelRole.CustomerSupervisor,
                IsActive = true,
                Notes = "Şube denetimlerinden sorumlu",
                CreatedAt = DateTime.UtcNow
            };

            var personnel3 = new CustomerPersonnel
            {
                CustomerId = customer2.Id,
                Username = "mehmet.kaya",
                Email = "mehmet.kaya@xyz.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                FirstName = "Mehmet",
                LastName = "Kaya",
                PhoneNumber = "0534 777 8899",
                Department = "İşletme",
                Title = "İşletme Müdürü",
                Role = CustomerPersonnelRole.CustomerManager,
                IsActive = true,
                Notes = "XYZ Restaurant Grubu işletme müdürü",
                CreatedAt = DateTime.UtcNow
            };

            var personnel4 = new CustomerPersonnel
            {
                CustomerId = customer2.Id,
                Username = "fatma.ozturk",
                Email = "fatma.ozturk@xyz.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                FirstName = "Fatma",
                LastName = "Öztürk",
                PhoneNumber = "0535 222 3344",
                Department = "Mutfak",
                Title = "Mutfak Şefi",
                Role = CustomerPersonnelRole.CustomerOperator,
                IsActive = true,
                Notes = "Mutfak operasyonları sorumlusu",
                CreatedAt = DateTime.UtcNow
            };

            var personnel5 = new CustomerPersonnel
            {
                CustomerId = customer3.Id,
                Username = "can.arslan",
                Email = "can.arslan@otelzinciri.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                FirstName = "Can",
                LastName = "Arslan",
                PhoneNumber = "0536 999 0011",
                Department = "Yönetim",
                Title = "Genel Müdür",
                Role = CustomerPersonnelRole.CustomerManager,
                IsActive = true,
                Notes = "Otel zinciri genel müdürü",
                CreatedAt = DateTime.UtcNow
            };

            context.CustomerPersonnel.AddRange(personnel1, personnel2, personnel3, personnel4, personnel5);
            await context.SaveChangesAsync();
            logger.LogInformation("Customer Personnel created");

            // 10. Customer Task Lists
            var taskList1 = new CustomerTaskList
            {
                CustomerId = customer1.Id,
                Name = "Ocak 2025 Şube Denetimleri",
                Description = "Ocak ayı için tüm şubelerin kalite denetimi",
                TaskType = CustomerTaskType.Inspection,
                Priority = TaskPriority.High,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(25),
                Status = SecretCustomer.Core.Enums.TaskStatus.InProgress,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var taskList2 = new CustomerTaskList
            {
                CustomerId = customer2.Id,
                Name = "Hijyen ve Temizlik Kontrolü",
                Description = "Restaurant şubelerinin hijyen standartları kontrolü",
                TaskType = CustomerTaskType.Audit,
                Priority = TaskPriority.Critical,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(14),
                Status = SecretCustomer.Core.Enums.TaskStatus.NotStarted,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.CustomerTaskLists.AddRange(taskList1, taskList2);
            await context.SaveChangesAsync();
            logger.LogInformation("Customer Task Lists created");

            // 11. Customer Personnel Task Assignments
            var taskAssignment1 = new CustomerPersonnelTaskAssignment
            {
                PersonnelId = personnel2.Id,
                TaskListId = taskList1.Id,
                AssignmentRole = TaskAssignmentRole.Owner,
                AssignedDate = DateTime.UtcNow.AddDays(-5),
                Notes = "Şube denetimlerinin koordinasyonundan sorumlu",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var taskAssignment2 = new CustomerPersonnelTaskAssignment
            {
                PersonnelId = personnel3.Id,
                TaskListId = taskList2.Id,
                AssignmentRole = TaskAssignmentRole.Owner,
                AssignedDate = DateTime.UtcNow,
                Notes = "Hijyen kontrollerinin takibi",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var taskAssignment3 = new CustomerPersonnelTaskAssignment
            {
                PersonnelId = personnel4.Id,
                TaskListId = taskList2.Id,
                AssignmentRole = TaskAssignmentRole.Assistant,
                AssignedDate = DateTime.UtcNow,
                Notes = "Mutfak hijyeni kontrolü desteği",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.CustomerPersonnelTaskAssignments.AddRange(taskAssignment1, taskAssignment2, taskAssignment3);
            await context.SaveChangesAsync();
            logger.LogInformation("Customer Personnel Task Assignments created");

            // 12. Sample Personnel (Şube Personeli - Değerlendirilen Kişiler)
            var branchPersonnel1 = new Personnel
            {
                FirstName = "Mehmet",
                LastName = "Özkan",
                TcKimlikNo = "11122233344",
                ErpNo = "ERP001",
                SicilNo = "SCL001",
                Title = "Müşteri Temsilcisi",
                Gender = Gender.Male,
                BirthDate = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                HireDate = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                Email = "mehmet.ozkan@sube.com",
                PhoneNumber = "0532 111 2233",
                Department = "Müşteri Hizmetleri",
                IsActive = true,
                CustomerId = customer1.Id,
                Notes = "Deneyimli müşteri temsilcisi"
            };

            var branchPersonnel2 = new Personnel
            {
                FirstName = "Fatma",
                LastName = "Yıldırım",
                TcKimlikNo = "22233344455",
                ErpNo = "ERP002",
                SicilNo = "SCL002",
                Title = "Kasa Görevlisi",
                Gender = Gender.Female,
                BirthDate = new DateTime(1995, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                HireDate = new DateTime(2021, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                Email = "fatma.yildirim@sube.com",
                PhoneNumber = "0533 222 3344",
                Department = "Kasa",
                IsActive = true,
                CustomerId = customer1.Id,
                Notes = "Kasa operasyonlarında uzman"
            };

            var branchPersonnel3 = new Personnel
            {
                FirstName = "Ali",
                LastName = "Kaya",
                TcKimlikNo = "33344455566",
                ErpNo = "ERP003",
                SicilNo = "SCL003",
                Title = "Şube Müdürü",
                Gender = Gender.Male,
                BirthDate = new DateTime(1985, 12, 10, 0, 0, 0, DateTimeKind.Utc),
                HireDate = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Email = "ali.kaya@sube.com",
                PhoneNumber = "0534 333 4455",
                Department = "Yönetim",
                IsActive = true,
                CustomerId = customer1.Id,
                Notes = "10 yıllık deneyimli şube müdürü"
            };

            var branchPersonnel4 = new Personnel
            {
                FirstName = "Selin",
                LastName = "Aydın",
                TcKimlikNo = "44455566677",
                ErpNo = "ERP004",
                SicilNo = "SCL004",
                Title = "Garson",
                Gender = Gender.Female,
                BirthDate = new DateTime(1998, 3, 25, 0, 0, 0, DateTimeKind.Utc),
                HireDate = new DateTime(2022, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                Email = "selin.aydin@sube.com",
                PhoneNumber = "0535 444 5566",
                Department = "Servis",
                IsActive = true,
                CustomerId = customer2.Id,
                Notes = "Yeni başlayan garson"
            };

            var branchPersonnel5 = new Personnel
            {
                FirstName = "Mustafa",
                LastName = "Çelik",
                TcKimlikNo = "55566677788",
                ErpNo = "ERP005",
                SicilNo = "SCL005",
                Title = "Aşçı",
                Gender = Gender.Male,
                BirthDate = new DateTime(1988, 7, 5, 0, 0, 0, DateTimeKind.Utc),
                HireDate = new DateTime(2018, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                Email = "mustafa.celik@sube.com",
                PhoneNumber = "0536 555 6677",
                Department = "Mutfak",
                IsActive = true,
                CustomerId = customer2.Id,
                Notes = "Deneyimli şef"
            };

            var branchPersonnel6 = new Personnel
            {
                FirstName = "Zehra",
                LastName = "Arslan",
                TcKimlikNo = "66677788899",
                ErpNo = "ERP006",
                SicilNo = "SCL006",
                Title = "Resepsiyonist",
                Gender = Gender.Female,
                BirthDate = new DateTime(1992, 11, 30, 0, 0, 0, DateTimeKind.Utc),
                HireDate = new DateTime(2019, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                Email = "zehra.arslan@sube.com",
                PhoneNumber = "0537 666 7788",
                Department = "Resepsiyon",
                IsActive = true,
                CustomerId = customer3.Id,
                Notes = "İngilizce ve Almanca biliyor"
            };

            context.Personnel.AddRange(branchPersonnel1, branchPersonnel2, branchPersonnel3, branchPersonnel4, branchPersonnel5, branchPersonnel6);
            await context.SaveChangesAsync();
            logger.LogInformation("Personnel created");

            // 13. Link project to customer
            project.CustomerId = customer1.Id;
            context.Projects.Update(project);
            await context.SaveChangesAsync();
            logger.LogInformation("Linked project to customer");

            // 14. Permissions - RBAC System
            await SeedPermissionsAsync(context, logger);

            // 15. App Settings - Varsayılan Ayarlar
            await SeedAppSettingsAsync(context, logger);

            // 16. System Settings - Dashboard hedefleri vb.
            await SeedSystemSettingsAsync(context, logger);

            // 17. Languages - Çoklu Dil Desteği
            await SeedLanguagesAsync(context, logger);

            logger.LogInformation("Database seed completed successfully!");
            logger.LogInformation("Test users:");
            logger.LogInformation("  Admin: admin / Admin@123");
            logger.LogInformation("  TeamLeader: teamleader / Leader@123");
            logger.LogInformation("  Evaluator1: evaluator1 / Eval@123");
            logger.LogInformation("  Evaluator2: evaluator2 / Eval@123");
            logger.LogInformation("Customer Personnel:");
            logger.LogInformation("  Customer Manager (ABC): ahmet.yilmaz / Customer@123");
            logger.LogInformation("  Customer Supervisor (ABC): ayse.demir / Customer@123");
            logger.LogInformation("  Customer Manager (XYZ): mehmet.kaya / Customer@123");
            logger.LogInformation("  Customer Operator (XYZ): fatma.ozturk / Customer@123");
            logger.LogInformation("  Customer Manager (Otel): can.arslan / Customer@123");
            logger.LogInformation("Field Workers:");
            logger.LogInformation("  Ali Veli: ali.veli / Field@123");
            logger.LogInformation("  Zeynep Yıldız: zeynep.yildiz / Field@123");
            logger.LogInformation("  Murat Koç: murat.koc / Field@123");
            logger.LogInformation("  Elif Şahin: elif.sahin / Field@123");
            logger.LogInformation("  Burak Tekin: burak.tekin / Field@123");
            logger.LogInformation("  Selin Aydın: selin.aydin / Field@123");
            logger.LogInformation("  Emre Çelik: emre.celik / Field@123");
            logger.LogInformation("  Ayşe Kara (inactive): ayse.kara / Field@123");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Permissions.AnyAsync())
        {
            logger.LogInformation("Permissions already seeded");
            return;
        }

        logger.LogInformation("Seeding permissions...");

        var permissions = new List<Permission>
        {
            // Users Management
            new Permission { Code = "Users.View", DisplayName = "Kullanıcıları Görüntüle", Category = PermissionCategory.Users, SortOrder = 1, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Users.Create", DisplayName = "Kullanıcı Oluştur", Category = PermissionCategory.Users, SortOrder = 2, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Users.Edit", DisplayName = "Kullanıcı Düzenle", Category = PermissionCategory.Users, SortOrder = 3, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Users.Delete", DisplayName = "Kullanıcı Sil", Category = PermissionCategory.Users, SortOrder = 4, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Users.Manage", DisplayName = "Kullanıcı Yönetimi (Tam Yetki)", Category = PermissionCategory.Users, SortOrder = 5, CreatedAt = DateTime.UtcNow },

            // Projects
            new Permission { Code = "Projects.View", DisplayName = "Projeleri Görüntüle", Category = PermissionCategory.Projects, SortOrder = 10, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Projects.Create", DisplayName = "Proje Oluştur", Category = PermissionCategory.Projects, SortOrder = 11, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Projects.Edit", DisplayName = "Proje Düzenle", Category = PermissionCategory.Projects, SortOrder = 12, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Projects.Delete", DisplayName = "Proje Sil", Category = PermissionCategory.Projects, SortOrder = 13, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Projects.Manage", DisplayName = "Proje Yönetimi (Tam Yetki)", Category = PermissionCategory.Projects, SortOrder = 14, CreatedAt = DateTime.UtcNow },

            // Assignments
            new Permission { Code = "Assignments.View", DisplayName = "Atamaları Görüntüle", Category = PermissionCategory.Assignments, SortOrder = 20, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Assignments.Create", DisplayName = "Atama Oluştur", Category = PermissionCategory.Assignments, SortOrder = 21, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Assignments.Edit", DisplayName = "Atama Düzenle", Category = PermissionCategory.Assignments, SortOrder = 22, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Assignments.Delete", DisplayName = "Atama Sil", Category = PermissionCategory.Assignments, SortOrder = 23, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Assignments.Manage", DisplayName = "Atama Yönetimi (Tam Yetki)", Category = PermissionCategory.Assignments, SortOrder = 24, CreatedAt = DateTime.UtcNow },

            // Checklists
            new Permission { Code = "Checklists.View", DisplayName = "Kontrol Listelerini Görüntüle", Category = PermissionCategory.Checklists, SortOrder = 30, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Checklists.Create", DisplayName = "Kontrol Listesi Oluştur", Category = PermissionCategory.Checklists, SortOrder = 31, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Checklists.Edit", DisplayName = "Kontrol Listesi Düzenle", Category = PermissionCategory.Checklists, SortOrder = 32, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Checklists.Delete", DisplayName = "Kontrol Listesi Sil", Category = PermissionCategory.Checklists, SortOrder = 33, CreatedAt = DateTime.UtcNow },

            // Reports
            new Permission { Code = "Reports.View", DisplayName = "Raporları Görüntüle", Category = PermissionCategory.Reports, SortOrder = 40, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Reports.Export", DisplayName = "Rapor Dışa Aktar", Category = PermissionCategory.Reports, SortOrder = 41, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Reports.Create", DisplayName = "Rapor Oluştur", Category = PermissionCategory.Reports, SortOrder = 42, CreatedAt = DateTime.UtcNow },

            // Dashboard
            new Permission { Code = "Dashboard.View", DisplayName = "Dashboard Görüntüle", Category = PermissionCategory.Dashboard, SortOrder = 50, CreatedAt = DateTime.UtcNow },

            // Permissions Management
            new Permission { Code = "Permissions.View", DisplayName = "Yetkileri Görüntüle", Category = PermissionCategory.Settings, SortOrder = 60, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Permissions.Manage", DisplayName = "Yetki Yönetimi", Category = PermissionCategory.Settings, SortOrder = 61, CreatedAt = DateTime.UtcNow },

            // Evaluations
            new Permission { Code = "Evaluations.View", DisplayName = "Değerlendirmeleri Görüntüle", Category = PermissionCategory.Evaluations, SortOrder = 70, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Evaluations.Create", DisplayName = "Değerlendirme Oluştur", Category = PermissionCategory.Evaluations, SortOrder = 71, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Evaluations.Edit", DisplayName = "Değerlendirme Düzenle", Category = PermissionCategory.Evaluations, SortOrder = 72, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Evaluations.Delete", DisplayName = "Değerlendirme Sil", Category = PermissionCategory.Evaluations, SortOrder = 73, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Evaluations.RevertToDraft", DisplayName = "Taslağa Al", Category = PermissionCategory.Evaluations, SortOrder = 74, CreatedAt = DateTime.UtcNow },

            // Customers
            new Permission { Code = "Customers.View", DisplayName = "Müşterileri Görüntüle", Category = PermissionCategory.Customers, SortOrder = 80, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Customers.Create", DisplayName = "Müşteri Oluştur", Category = PermissionCategory.Customers, SortOrder = 81, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Customers.Edit", DisplayName = "Müşteri Düzenle", Category = PermissionCategory.Customers, SortOrder = 82, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Customers.Delete", DisplayName = "Müşteri Sil", Category = PermissionCategory.Customers, SortOrder = 83, CreatedAt = DateTime.UtcNow },

            // Customer Organizations
            new Permission { Code = "CustomerOrganizations.View", DisplayName = "Müşteri Organizasyonlarını Görüntüle", Category = PermissionCategory.CustomerOrganizations, SortOrder = 90, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "CustomerOrganizations.Create", DisplayName = "Müşteri Organizasyonu Oluştur", Category = PermissionCategory.CustomerOrganizations, SortOrder = 91, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "CustomerOrganizations.Edit", DisplayName = "Müşteri Organizasyonu Düzenle", Category = PermissionCategory.CustomerOrganizations, SortOrder = 92, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "CustomerOrganizations.Delete", DisplayName = "Müşteri Organizasyonu Sil", Category = PermissionCategory.CustomerOrganizations, SortOrder = 93, CreatedAt = DateTime.UtcNow },

            // Customer Personnel
            new Permission { Code = "CustomerPersonnel.View", DisplayName = "Müşteri Personelini Görüntüle", Category = PermissionCategory.CustomerPersonnel, SortOrder = 100, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "CustomerPersonnel.Create", DisplayName = "Müşteri Personeli Oluştur", Category = PermissionCategory.CustomerPersonnel, SortOrder = 101, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "CustomerPersonnel.Edit", DisplayName = "Müşteri Personeli Düzenle", Category = PermissionCategory.CustomerPersonnel, SortOrder = 102, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "CustomerPersonnel.Delete", DisplayName = "Müşteri Personeli Sil", Category = PermissionCategory.CustomerPersonnel, SortOrder = 103, CreatedAt = DateTime.UtcNow },

            // Personnel (Şube Personeli)
            new Permission { Code = "Personnel.View", DisplayName = "Personeli Görüntüle", Category = PermissionCategory.Personnel, SortOrder = 110, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Personnel.Create", DisplayName = "Personel Oluştur", Category = PermissionCategory.Personnel, SortOrder = 111, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Personnel.Edit", DisplayName = "Personel Düzenle", Category = PermissionCategory.Personnel, SortOrder = 112, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Personnel.Delete", DisplayName = "Personel Sil", Category = PermissionCategory.Personnel, SortOrder = 113, CreatedAt = DateTime.UtcNow },

            // Languages
            new Permission { Code = "Languages.View", DisplayName = "Dilleri Görüntüle", Category = PermissionCategory.Languages, SortOrder = 120, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Languages.Create", DisplayName = "Dil Oluştur", Category = PermissionCategory.Languages, SortOrder = 121, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Languages.Edit", DisplayName = "Dil Düzenle", Category = PermissionCategory.Languages, SortOrder = 122, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Languages.Delete", DisplayName = "Dil Sil", Category = PermissionCategory.Languages, SortOrder = 123, CreatedAt = DateTime.UtcNow },

            // Trainings
            new Permission { Code = "Trainings.View", DisplayName = "Eğitimleri Görüntüle", Category = PermissionCategory.Trainings, SortOrder = 130, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Trainings.Create", DisplayName = "Eğitim Oluştur", Category = PermissionCategory.Trainings, SortOrder = 131, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Trainings.Edit", DisplayName = "Eğitim Düzenle", Category = PermissionCategory.Trainings, SortOrder = 132, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Trainings.Delete", DisplayName = "Eğitim Sil", Category = PermissionCategory.Trainings, SortOrder = 133, CreatedAt = DateTime.UtcNow },

            // Meetings
            new Permission { Code = "Meetings.View", DisplayName = "Toplantıları Görüntüle", Category = PermissionCategory.Meetings, SortOrder = 140, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Meetings.Create", DisplayName = "Toplantı Oluştur", Category = PermissionCategory.Meetings, SortOrder = 141, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Meetings.Edit", DisplayName = "Toplantı Düzenle", Category = PermissionCategory.Meetings, SortOrder = 142, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Meetings.Delete", DisplayName = "Toplantı Sil", Category = PermissionCategory.Meetings, SortOrder = 143, CreatedAt = DateTime.UtcNow },

            // Approvals
            new Permission { Code = "Approvals.View", DisplayName = "Onayları Görüntüle", Category = PermissionCategory.Approvals, SortOrder = 150, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Approvals.Create", DisplayName = "Onay Oluştur", Category = PermissionCategory.Approvals, SortOrder = 151, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Approvals.Edit", DisplayName = "Onay Düzenle", Category = PermissionCategory.Approvals, SortOrder = 152, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Approvals.Delete", DisplayName = "Onay Sil", Category = PermissionCategory.Approvals, SortOrder = 153, CreatedAt = DateTime.UtcNow },

            // Excel Templates
            new Permission { Code = "ExcelTemplates.View", DisplayName = "Excel Şablonlarını Görüntüle", Category = PermissionCategory.ExcelTemplates, SortOrder = 160, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "ExcelTemplates.Create", DisplayName = "Excel Şablonu Oluştur", Category = PermissionCategory.ExcelTemplates, SortOrder = 161, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "ExcelTemplates.Edit", DisplayName = "Excel Şablonu Düzenle", Category = PermissionCategory.ExcelTemplates, SortOrder = 162, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "ExcelTemplates.Delete", DisplayName = "Excel Şablonu Sil", Category = PermissionCategory.ExcelTemplates, SortOrder = 163, CreatedAt = DateTime.UtcNow },

            // Draft Requests (Taslağa Alma Talepleri)
            new Permission { Code = "DraftRequests.View", DisplayName = "Taslak Taleplerini Görüntüle", Category = PermissionCategory.DraftRequests, SortOrder = 170, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "DraftRequests.Approve", DisplayName = "Taslak Talebini Onayla", Category = PermissionCategory.DraftRequests, SortOrder = 171, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "DraftRequests.Reject", DisplayName = "Taslak Talebini Reddet", Category = PermissionCategory.DraftRequests, SortOrder = 172, CreatedAt = DateTime.UtcNow },
        };

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();
        logger.LogInformation($"Created {permissions.Count} permissions");

        // Role-Permission mappings - Admin gets everything
        var adminRole = UserRole.Admin;
        foreach (var permission in permissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                Role = adminRole,
                PermissionId = permission.Id,
                IsGranted = true,
                Scope = PermissionScope.All,
                CreatedAt = DateTime.UtcNow
            });
        }

        // QualitySpecialist permissions (hem TeamLeader hem Evaluator için)
        var qualitySpecialistPermissions = permissions.Where(p =>
            p.Code.StartsWith("Projects.") ||
            p.Code.StartsWith("Assignments.") ||
            p.Code.StartsWith("Checklists.View") ||
            p.Code.StartsWith("Reports.") ||
            p.Code.StartsWith("Dashboard.")).ToList();

        foreach (var permission in qualitySpecialistPermissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                Role = UserRole.QualitySpecialist,
                PermissionId = permission.Id,
                IsGranted = true,
                Scope = PermissionScope.Branch,
                CreatedAt = DateTime.UtcNow
            });
        }

        // FieldWorker permissions
        var fieldWorkerPermissions = permissions.Where(p =>
            p.Code == "Assignments.View" ||
            p.Code == "Dashboard.View").ToList();

        foreach (var permission in fieldWorkerPermissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                Role = UserRole.FieldWorker,
                PermissionId = permission.Id,
                IsGranted = true,
                Scope = PermissionScope.Own,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Role-Permission mappings created");
        logger.LogInformation("  - Admin: Full access to all permissions");
        logger.LogInformation("  - TeamLeader: Project, Assignment, Report management");
        logger.LogInformation("  - Evaluator: View checklists and own assignments");
        logger.LogInformation("  - FieldWorker: View own assignments only");
    }

    /// <summary>
    /// Mevcut database'e yeni permission'ları ekler (mevcutları silmeden)
    /// </summary>
    public static async Task<int> SyncPermissionsAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Syncing permissions...");

        // Tüm tanımlı permission'lar
        var allPermissions = GetAllPermissionDefinitions();

        // Mevcut permission code'ları
        var existingCodes = await context.Permissions.Select(p => p.Code).ToListAsync();

        // Eksik permission'ları bul
        var newPermissions = allPermissions.Where(p => !existingCodes.Contains(p.Code)).ToList();

        if (!newPermissions.Any())
        {
            logger.LogInformation("No new permissions to add");
            return 0;
        }

        // Yeni permission'ları ekle
        context.Permissions.AddRange(newPermissions);
        await context.SaveChangesAsync();
        logger.LogInformation($"Added {newPermissions.Count} new permissions");

        // Admin rolüne yeni permission'ları ver
        foreach (var permission in newPermissions)
        {
            // Admin rolü için kontrol et
            var existingAdminMapping = await context.RolePermissions
                .AnyAsync(rp => rp.Role == UserRole.Admin && rp.PermissionId == permission.Id);

            if (!existingAdminMapping)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    Role = UserRole.Admin,
                    PermissionId = permission.Id,
                    IsGranted = true,
                    Scope = PermissionScope.All,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation($"Admin role updated with {newPermissions.Count} new permissions");

        return newPermissions.Count;
    }

    /// <summary>
    /// Tüm permission tanımlarını döner
    /// </summary>
    private static List<Permission> GetAllPermissionDefinitions()
    {
        return new List<Permission>
        {
            // Users Management
            new Permission { Code = "Users.View", DisplayName = "Kullanıcıları Görüntüle", Category = PermissionCategory.Users, SortOrder = 1, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Users.Create", DisplayName = "Kullanıcı Oluştur", Category = PermissionCategory.Users, SortOrder = 2, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Users.Edit", DisplayName = "Kullanıcı Düzenle", Category = PermissionCategory.Users, SortOrder = 3, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Users.Delete", DisplayName = "Kullanıcı Sil", Category = PermissionCategory.Users, SortOrder = 4, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Users.Manage", DisplayName = "Kullanıcı Yönetimi (Tam Yetki)", Category = PermissionCategory.Users, SortOrder = 5, CreatedAt = DateTime.UtcNow },

            // Projects
            new Permission { Code = "Projects.View", DisplayName = "Projeleri Görüntüle", Category = PermissionCategory.Projects, SortOrder = 10, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Projects.Create", DisplayName = "Proje Oluştur", Category = PermissionCategory.Projects, SortOrder = 11, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Projects.Edit", DisplayName = "Proje Düzenle", Category = PermissionCategory.Projects, SortOrder = 12, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Projects.Delete", DisplayName = "Proje Sil", Category = PermissionCategory.Projects, SortOrder = 13, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Projects.Manage", DisplayName = "Proje Yönetimi (Tam Yetki)", Category = PermissionCategory.Projects, SortOrder = 14, CreatedAt = DateTime.UtcNow },

            // Assignments
            new Permission { Code = "Assignments.View", DisplayName = "Atamaları Görüntüle", Category = PermissionCategory.Assignments, SortOrder = 20, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Assignments.Create", DisplayName = "Atama Oluştur", Category = PermissionCategory.Assignments, SortOrder = 21, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Assignments.Edit", DisplayName = "Atama Düzenle", Category = PermissionCategory.Assignments, SortOrder = 22, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Assignments.Delete", DisplayName = "Atama Sil", Category = PermissionCategory.Assignments, SortOrder = 23, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Assignments.Manage", DisplayName = "Atama Yönetimi (Tam Yetki)", Category = PermissionCategory.Assignments, SortOrder = 24, CreatedAt = DateTime.UtcNow },

            // Checklists
            new Permission { Code = "Checklists.View", DisplayName = "Kontrol Listelerini Görüntüle", Category = PermissionCategory.Checklists, SortOrder = 30, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Checklists.Create", DisplayName = "Kontrol Listesi Oluştur", Category = PermissionCategory.Checklists, SortOrder = 31, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Checklists.Edit", DisplayName = "Kontrol Listesi Düzenle", Category = PermissionCategory.Checklists, SortOrder = 32, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Checklists.Delete", DisplayName = "Kontrol Listesi Sil", Category = PermissionCategory.Checklists, SortOrder = 33, CreatedAt = DateTime.UtcNow },

            // Reports
            new Permission { Code = "Reports.View", DisplayName = "Raporları Görüntüle", Category = PermissionCategory.Reports, SortOrder = 40, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Reports.Export", DisplayName = "Rapor Dışa Aktar", Category = PermissionCategory.Reports, SortOrder = 41, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Reports.Create", DisplayName = "Rapor Oluştur", Category = PermissionCategory.Reports, SortOrder = 42, CreatedAt = DateTime.UtcNow },

            // Dashboard
            new Permission { Code = "Dashboard.View", DisplayName = "Dashboard Görüntüle", Category = PermissionCategory.Dashboard, SortOrder = 50, CreatedAt = DateTime.UtcNow },

            // Permissions Management
            new Permission { Code = "Permissions.View", DisplayName = "Yetkileri Görüntüle", Category = PermissionCategory.Settings, SortOrder = 60, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Permissions.Manage", DisplayName = "Yetki Yönetimi", Category = PermissionCategory.Settings, SortOrder = 61, CreatedAt = DateTime.UtcNow },

            // Evaluations
            new Permission { Code = "Evaluations.View", DisplayName = "Değerlendirmeleri Görüntüle", Category = PermissionCategory.Evaluations, SortOrder = 70, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Evaluations.Create", DisplayName = "Değerlendirme Oluştur", Category = PermissionCategory.Evaluations, SortOrder = 71, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Evaluations.Edit", DisplayName = "Değerlendirme Düzenle", Category = PermissionCategory.Evaluations, SortOrder = 72, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Evaluations.Delete", DisplayName = "Değerlendirme Sil", Category = PermissionCategory.Evaluations, SortOrder = 73, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Evaluations.RevertToDraft", DisplayName = "Taslağa Al", Category = PermissionCategory.Evaluations, SortOrder = 74, CreatedAt = DateTime.UtcNow },

            // Customers
            new Permission { Code = "Customers.View", DisplayName = "Müşterileri Görüntüle", Category = PermissionCategory.Customers, SortOrder = 80, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Customers.Create", DisplayName = "Müşteri Oluştur", Category = PermissionCategory.Customers, SortOrder = 81, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Customers.Edit", DisplayName = "Müşteri Düzenle", Category = PermissionCategory.Customers, SortOrder = 82, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Customers.Delete", DisplayName = "Müşteri Sil", Category = PermissionCategory.Customers, SortOrder = 83, CreatedAt = DateTime.UtcNow },

            // Customer Organizations
            new Permission { Code = "CustomerOrganizations.View", DisplayName = "Müşteri Organizasyonlarını Görüntüle", Category = PermissionCategory.CustomerOrganizations, SortOrder = 90, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "CustomerOrganizations.Create", DisplayName = "Müşteri Organizasyonu Oluştur", Category = PermissionCategory.CustomerOrganizations, SortOrder = 91, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "CustomerOrganizations.Edit", DisplayName = "Müşteri Organizasyonu Düzenle", Category = PermissionCategory.CustomerOrganizations, SortOrder = 92, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "CustomerOrganizations.Delete", DisplayName = "Müşteri Organizasyonu Sil", Category = PermissionCategory.CustomerOrganizations, SortOrder = 93, CreatedAt = DateTime.UtcNow },

            // Customer Personnel
            new Permission { Code = "CustomerPersonnel.View", DisplayName = "Müşteri Personelini Görüntüle", Category = PermissionCategory.CustomerPersonnel, SortOrder = 100, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "CustomerPersonnel.Create", DisplayName = "Müşteri Personeli Oluştur", Category = PermissionCategory.CustomerPersonnel, SortOrder = 101, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "CustomerPersonnel.Edit", DisplayName = "Müşteri Personeli Düzenle", Category = PermissionCategory.CustomerPersonnel, SortOrder = 102, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "CustomerPersonnel.Delete", DisplayName = "Müşteri Personeli Sil", Category = PermissionCategory.CustomerPersonnel, SortOrder = 103, CreatedAt = DateTime.UtcNow },

            // Personnel (Şube Personeli)
            new Permission { Code = "Personnel.View", DisplayName = "Personeli Görüntüle", Category = PermissionCategory.Personnel, SortOrder = 110, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Personnel.Create", DisplayName = "Personel Oluştur", Category = PermissionCategory.Personnel, SortOrder = 111, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Personnel.Edit", DisplayName = "Personel Düzenle", Category = PermissionCategory.Personnel, SortOrder = 112, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Personnel.Delete", DisplayName = "Personel Sil", Category = PermissionCategory.Personnel, SortOrder = 113, CreatedAt = DateTime.UtcNow },

            // Languages
            new Permission { Code = "Languages.View", DisplayName = "Dilleri Görüntüle", Category = PermissionCategory.Languages, SortOrder = 120, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Languages.Create", DisplayName = "Dil Oluştur", Category = PermissionCategory.Languages, SortOrder = 121, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Languages.Edit", DisplayName = "Dil Düzenle", Category = PermissionCategory.Languages, SortOrder = 122, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Languages.Delete", DisplayName = "Dil Sil", Category = PermissionCategory.Languages, SortOrder = 123, CreatedAt = DateTime.UtcNow },

            // Trainings
            new Permission { Code = "Trainings.View", DisplayName = "Eğitimleri Görüntüle", Category = PermissionCategory.Trainings, SortOrder = 130, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Trainings.Create", DisplayName = "Eğitim Oluştur", Category = PermissionCategory.Trainings, SortOrder = 131, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Trainings.Edit", DisplayName = "Eğitim Düzenle", Category = PermissionCategory.Trainings, SortOrder = 132, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Trainings.Delete", DisplayName = "Eğitim Sil", Category = PermissionCategory.Trainings, SortOrder = 133, CreatedAt = DateTime.UtcNow },

            // Meetings
            new Permission { Code = "Meetings.View", DisplayName = "Toplantıları Görüntüle", Category = PermissionCategory.Meetings, SortOrder = 140, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Meetings.Create", DisplayName = "Toplantı Oluştur", Category = PermissionCategory.Meetings, SortOrder = 141, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Meetings.Edit", DisplayName = "Toplantı Düzenle", Category = PermissionCategory.Meetings, SortOrder = 142, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Meetings.Delete", DisplayName = "Toplantı Sil", Category = PermissionCategory.Meetings, SortOrder = 143, CreatedAt = DateTime.UtcNow },

            // Approvals
            new Permission { Code = "Approvals.View", DisplayName = "Onayları Görüntüle", Category = PermissionCategory.Approvals, SortOrder = 150, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Approvals.Create", DisplayName = "Onay Oluştur", Category = PermissionCategory.Approvals, SortOrder = 151, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Approvals.Edit", DisplayName = "Onay Düzenle", Category = PermissionCategory.Approvals, SortOrder = 152, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "Approvals.Delete", DisplayName = "Onay Sil", Category = PermissionCategory.Approvals, SortOrder = 153, CreatedAt = DateTime.UtcNow },

            // Excel Templates
            new Permission { Code = "ExcelTemplates.View", DisplayName = "Excel Şablonlarını Görüntüle", Category = PermissionCategory.ExcelTemplates, SortOrder = 160, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "ExcelTemplates.Create", DisplayName = "Excel Şablonu Oluştur", Category = PermissionCategory.ExcelTemplates, SortOrder = 161, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "ExcelTemplates.Edit", DisplayName = "Excel Şablonu Düzenle", Category = PermissionCategory.ExcelTemplates, SortOrder = 162, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "ExcelTemplates.Delete", DisplayName = "Excel Şablonu Sil", Category = PermissionCategory.ExcelTemplates, SortOrder = 163, CreatedAt = DateTime.UtcNow },

            // Draft Requests (Taslağa Alma Talepleri)
            new Permission { Code = "DraftRequests.View", DisplayName = "Taslak Taleplerini Görüntüle", Category = PermissionCategory.DraftRequests, SortOrder = 170, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "DraftRequests.Approve", DisplayName = "Taslak Talebini Onayla", Category = PermissionCategory.DraftRequests, SortOrder = 171, CreatedAt = DateTime.UtcNow },
            new Permission { Code = "DraftRequests.Reject", DisplayName = "Taslak Talebini Reddet", Category = PermissionCategory.DraftRequests, SortOrder = 172, CreatedAt = DateTime.UtcNow },
        };
    }

    private static async Task SeedAppSettingsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.AppSettings.AnyAsync())
        {
            logger.LogInformation("App settings already exist, skipping...");
            return;
        }

        var settings = new List<AppSettings>
        {
            // General Settings
            new AppSettings
            {
                Key = "General.DemoMode",
                Value = "true",
                ValueType = SettingValueType.Bool,
                Category = "General",
                Description = "Demo modu aktif. True ise detaylı hata mesajları gösterilir.",
                DisplayOrder = 1,
                IsSystem = true
            },
            new AppSettings
            {
                Key = "General.AppName",
                Value = "Gizli Müşteri Değerlendirme Sistemi",
                ValueType = SettingValueType.String,
                Category = "General",
                Description = "Uygulama adı",
                DisplayOrder = 2,
                IsSystem = true
            },
            new AppSettings
            {
                Key = "General.Version",
                Value = "1.0.0",
                ValueType = SettingValueType.String,
                Category = "General",
                Description = "Uygulama versiyonu",
                DisplayOrder = 3,
                IsSystem = true
            },
            new AppSettings
            {
                Key = "General.MaintenanceMode",
                Value = "false",
                ValueType = SettingValueType.Bool,
                Category = "General",
                Description = "Bakım modu aktif mi?",
                DisplayOrder = 4,
                IsSystem = true
            },

            // Security Settings
            new AppSettings
            {
                Key = "Security.MaxLoginAttempts",
                Value = "5",
                ValueType = SettingValueType.Int,
                Category = "Security",
                Description = "Maksimum başarısız giriş denemesi sayısı",
                DisplayOrder = 1,
                IsSystem = false
            },
            new AppSettings
            {
                Key = "Security.LockoutDurationMinutes",
                Value = "15",
                ValueType = SettingValueType.Int,
                Category = "Security",
                Description = "Hesap kilitleme süresi (dakika)",
                DisplayOrder = 2,
                IsSystem = false
            },
            new AppSettings
            {
                Key = "Security.SessionTimeoutMinutes",
                Value = "60",
                ValueType = SettingValueType.Int,
                Category = "Security",
                Description = "Oturum zaman aşımı süresi (dakika)",
                DisplayOrder = 3,
                IsSystem = false
            },

            // Customer Portal Settings
            new AppSettings
            {
                Key = "CustomerPortal.Enabled",
                Value = "true",
                ValueType = SettingValueType.Bool,
                Category = "CustomerPortal",
                Description = "Müşteri portalı aktif mi?",
                DisplayOrder = 1,
                IsSystem = false
            },
            new AppSettings
            {
                Key = "CustomerPortal.AllowSelfRegistration",
                Value = "false",
                ValueType = SettingValueType.Bool,
                Category = "CustomerPortal",
                Description = "Müşteri personeli kendi kaydını oluşturabilir mi?",
                DisplayOrder = 2,
                IsSystem = false
            }
        };

        context.AppSettings.AddRange(settings);
        await context.SaveChangesAsync();
        logger.LogInformation("App settings created ({Count} settings)", settings.Count);
    }

    private static async Task SeedLanguagesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Languages.AnyAsync())
        {
            logger.LogInformation("Languages already exist, skipping...");
            return;
        }

        var languages = new List<Language>
        {
            new Language
            {
                Name = "Türkçe",
                LanguageCulture = "tr-TR",
                UniqueSeoCode = "tr",
                FlagImageFileName = "tr.png",
                Rtl = false,
                IsDefault = true,
                IsActive = true,
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow
            },
            new Language
            {
                Name = "English",
                LanguageCulture = "en-US",
                UniqueSeoCode = "en",
                FlagImageFileName = "en.png",
                Rtl = false,
                IsDefault = false,
                IsActive = true,
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow
            },
            new Language
            {
                Name = "Español",
                LanguageCulture = "es-ES",
                UniqueSeoCode = "es",
                FlagImageFileName = "es.png",
                Rtl = false,
                IsDefault = false,
                IsActive = true,
                DisplayOrder = 3,
                CreatedAt = DateTime.UtcNow
            },
            new Language
            {
                Name = "Deutsch",
                LanguageCulture = "de-DE",
                UniqueSeoCode = "de",
                FlagImageFileName = "de.png",
                Rtl = false,
                IsDefault = false,
                IsActive = true,
                DisplayOrder = 4,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Languages.AddRange(languages);
        await context.SaveChangesAsync();
        logger.LogInformation("Languages created ({Count} languages): {Names}",
            languages.Count,
            string.Join(", ", languages.Select(l => l.Name)));
    }

    /// <summary>
    /// Dilleri oluşturur ve XML dosyalarından çevirileri import eder
    /// </summary>
    private static async Task SeedSystemSettingsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.SystemSettings.AnyAsync())
        {
            logger.LogInformation("System settings already exist, skipping...");
            return;
        }

        var settings = new List<SystemSetting>
        {
            // Dashboard Settings
            new SystemSetting
            {
                Key = SystemSettingKeys.DailyEvaluationTarget,
                Value = "55",
                ValueType = "int",
                Category = "Dashboard",
                Description = "Günlük değerlendirme hedefi",
                CreatedAt = DateTime.UtcNow
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.DefaultPeriodTarget,
                Value = "1000",
                ValueType = "int",
                Category = "Dashboard",
                Description = "Varsayılan dönem hedefi (AssignmentPeriod için)",
                CreatedAt = DateTime.UtcNow
            },
            // Evaluation Settings
            new SystemSetting
            {
                Key = "Evaluation.RequireOrganizationSelection",
                Value = "true",
                ValueType = "bool",
                Category = "Evaluation",
                Description = "Değerlendirmede organizasyon seçimi zorunlu mu?",
                CreatedAt = DateTime.UtcNow
            },
            new SystemSetting
            {
                Key = "Evaluation.AllowAutoSave",
                Value = "true",
                ValueType = "bool",
                Category = "Evaluation",
                Description = "Değerlendirmede otomatik kaydetme aktif mi?",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.SystemSettings.AddRange(settings);
        await context.SaveChangesAsync();
        logger.LogInformation("System settings created ({Count} settings)", settings.Count);
    }

    private static async Task SeedLanguagesWithImportAsync(ApplicationDbContext context, ILogger logger, string basePath)
    {
        if (await context.Languages.AnyAsync())
        {
            logger.LogInformation("Languages already exist, skipping...");
            return;
        }

        var languages = new List<Language>
        {
            new Language
            {
                Name = "Türkçe",
                LanguageCulture = "tr-TR",
                UniqueSeoCode = "tr",
                FlagImageFileName = "tr.png",
                Rtl = false,
                IsDefault = true,
                IsActive = true,
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow
            },
            new Language
            {
                Name = "English",
                LanguageCulture = "en-US",
                UniqueSeoCode = "en",
                FlagImageFileName = "en.png",
                Rtl = false,
                IsDefault = false,
                IsActive = true,
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow
            },
            new Language
            {
                Name = "Español",
                LanguageCulture = "es-ES",
                UniqueSeoCode = "es",
                FlagImageFileName = "es.png",
                Rtl = false,
                IsDefault = false,
                IsActive = true,
                DisplayOrder = 3,
                CreatedAt = DateTime.UtcNow
            },
            new Language
            {
                Name = "Deutsch",
                LanguageCulture = "de-DE",
                UniqueSeoCode = "de",
                FlagImageFileName = "de.png",
                Rtl = false,
                IsDefault = false,
                IsActive = true,
                DisplayOrder = 4,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Languages.AddRange(languages);
        await context.SaveChangesAsync();
        logger.LogInformation("Languages created ({Count} languages)", languages.Count);

        // XML dosyalarından çevirileri import et
        var localizationPath = Path.Combine(basePath, "App_Data", "Localization");

        foreach (var language in languages)
        {
            var xmlFile = Path.Combine(localizationPath, $"resources.{language.UniqueSeoCode}.xml");

            if (File.Exists(xmlFile))
            {
                try
                {
                    var xmlContent = await File.ReadAllTextAsync(xmlFile);
                    var doc = System.Xml.Linq.XDocument.Parse(xmlContent);
                    var resources = doc.Descendants("LocaleResource");
                    int count = 0;

                    foreach (var resource in resources)
                    {
                        var name = resource.Attribute("Name")?.Value;
                        var value = resource.Element("Value")?.Value;

                        if (!string.IsNullOrEmpty(name) && value != null)
                        {
                            context.LocaleStringResources.Add(new LocaleStringResource
                            {
                                LanguageId = language.Id,
                                ResourceName = name,
                                ResourceValue = value,
                                CreatedAt = DateTime.UtcNow
                            });
                            count++;
                        }
                    }

                    await context.SaveChangesAsync();
                    logger.LogInformation("Imported {Count} translations for {Language} from {File}",
                        count, language.Name, xmlFile);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to import translations for {Language} from {File}",
                        language.Name, xmlFile);
                }
            }
            else
            {
                logger.LogWarning("XML file not found for {Language}: {File}", language.Name, xmlFile);
            }
        }
    }
}
