using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
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
                RoleId = UserRoles.Ids.Admin,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
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
                RoleId = UserRoles.Ids.Admin,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var teamLeader = new User
            {
                Username = "teamleader",
                Email = "teamleader@secretcustomer.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Leader@123"),
                FirstName = "Team",
                LastName = "Leader",
                RoleId = UserRoles.Ids.QualitySpecialist,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var evaluator1 = new User
            {
                Username = "evaluator1",
                Email = "evaluator1@secretcustomer.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Eval@123"),
                FirstName = "John",
                LastName = "Evaluator",
                RoleId = UserRoles.Ids.QualitySpecialist,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var evaluator2 = new User
            {
                Username = "evaluator2",
                Email = "evaluator2@secretcustomer.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Eval@123"),
                FirstName = "Jane",
                LastName = "Evaluator",
                RoleId = UserRoles.Ids.QualitySpecialist,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
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
                RoleId = UserRoles.Ids.FieldWorker,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var fieldWorkerUser2 = new User
            {
                Username = "zeynep.yildiz",
                Email = "zeynep.yildiz@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Zeynep",
                LastName = "Yıldız",
                RoleId = UserRoles.Ids.FieldWorker,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var fieldWorkerUser3 = new User
            {
                Username = "murat.koc",
                Email = "murat.koc@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Murat",
                LastName = "Koç",
                RoleId = UserRoles.Ids.FieldWorker,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var fieldWorkerUser4 = new User
            {
                Username = "elif.sahin",
                Email = "elif.sahin@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Elif",
                LastName = "Şahin",
                RoleId = UserRoles.Ids.FieldWorker,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var fieldWorkerUser5 = new User
            {
                Username = "burak.tekin",
                Email = "burak.tekin@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Burak",
                LastName = "Tekin",
                RoleId = UserRoles.Ids.FieldWorker,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var fieldWorkerUser6 = new User
            {
                Username = "selin.aydin",
                Email = "selin.aydin@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Selin",
                LastName = "Aydın",
                RoleId = UserRoles.Ids.FieldWorker,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var fieldWorkerUser7 = new User
            {
                Username = "emre.celik",
                Email = "emre.celik@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Emre",
                LastName = "Çelik",
                RoleId = UserRoles.Ids.FieldWorker,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var fieldWorkerUser8 = new User
            {
                Username = "ayse.kara",
                Email = "ayse.kara@sahacalisani.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Field@123"),
                FirstName = "Ayşe",
                LastName = "Kara",
                RoleId = UserRoles.Ids.FieldWorker,
                IsActive = false,
                CreatedAt = TurkeyTime.Now
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
                CreatedAt = TurkeyTime.Now
            };

            context.Checklists.Add(checklist);
            await context.SaveChangesAsync();
            logger.LogInformation("Checklist created");

            // 3. Questions
            var questions = new List<Question>
            {
                // Temizlik ve Hijyen Kriterleri
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Masalar temiz mi?",
                    ScoringTypeId = ScoringTypes.Ids.Scored,
                    WeightPoints = 5,
                    MaxPoints = 4,
                    PenaltyTypeId = PenaltyTypes.Ids.None,
                    IsRequired = true,
                    Order = 1,
                    CreatedAt = TurkeyTime.Now
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Tuvalet temizliği nasıl?",
                    ScoringTypeId = ScoringTypes.Ids.Scored,
                    WeightPoints = 10,
                    MaxPoints = 4,
                    PenaltyTypeId = PenaltyTypes.Ids.None,
                    IsRequired = true,
                    Order = 2,
                    CreatedAt = TurkeyTime.Now
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Genel temizlik hakkında ek gözlemler",
                    ScoringTypeId = ScoringTypes.Ids.Unscored, // Puansız - sadece yorum
                    WeightPoints = 0,
                    MaxPoints = 1,
                    PenaltyTypeId = PenaltyTypes.Ids.None,
                    IsRequired = false,
                    Order = 3,
                    CreatedAt = TurkeyTime.Now
                },

                // Hizmet Kalitesi Kriterleri
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Karşılama nasıldı?",
                    ScoringTypeId = ScoringTypes.Ids.Scored,
                    WeightPoints = 5,
                    MaxPoints = 4,
                    PenaltyTypeId = PenaltyTypes.Ids.None,
                    IsRequired = true,
                    Order = 4,
                    HelpText = "Müşteri girişinde karşılama kalitesini değerlendirin",
                    CreatedAt = TurkeyTime.Now
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Sipariş alma süresi uygun muydu?",
                    ScoringTypeId = ScoringTypes.Ids.Scored,
                    WeightPoints = 5,
                    MaxPoints = 4,
                    PenaltyTypeId = PenaltyTypes.Ids.None,
                    IsRequired = true,
                    Order = 5,
                    CreatedAt = TurkeyTime.Now
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Personel ilgisi nasıldı?",
                    ScoringTypeId = ScoringTypes.Ids.Scored,
                    WeightPoints = 10,
                    MaxPoints = 4,
                    PenaltyTypeId = PenaltyTypes.Ids.None,
                    IsRequired = true,
                    Order = 6,
                    CreatedAt = TurkeyTime.Now
                },

                // Ürün Kalitesi Kriterleri
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Yemek sıcaklığı uygun muydu?",
                    ScoringTypeId = ScoringTypes.Ids.Scored,
                    WeightPoints = 5,
                    MaxPoints = 4,
                    PenaltyTypeId = PenaltyTypes.Ids.None,
                    IsRequired = true,
                    Order = 7,
                    CreatedAt = TurkeyTime.Now
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Yemek lezzeti nasıldı?",
                    ScoringTypeId = ScoringTypes.Ids.Scored,
                    WeightPoints = 15,
                    MaxPoints = 4,
                    PenaltyTypeId = PenaltyTypes.Ids.None,
                    IsRequired = true,
                    Order = 8,
                    CreatedAt = TurkeyTime.Now
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Porsiyon büyüklüğü uygun mu?",
                    ScoringTypeId = ScoringTypes.Ids.Penalty, // Cezalı soru örneği
                    WeightPoints = 10, // Ceza miktarı (eski PenaltyValue)
                    MaxPoints = 2,
                    PenaltyTypeId = PenaltyTypes.Ids.YellowCard,
                    IsRequired = true,
                    Order = 9,
                    HelpText = "SARI KART: Porsiyon standart boyutun altındaysa 10 puan düşülür",
                    CreatedAt = TurkeyTime.Now
                },

                // ========== CEZALI SORULAR ÖRNEKLERİ ==========
                // SARI KART: Hafif ihlaller - düşük puan kesintisi
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Personel isim kartı takıyor mu?",
                    ScoringTypeId = ScoringTypes.Ids.Penalty,
                    WeightPoints = 5, // Ceza miktarı: 5 puan düşürülür
                    MaxPoints = 2, // Evet/Hayır
                    PenaltyTypeId = PenaltyTypes.Ids.YellowCard,
                    IsRequired = true,
                    Order = 10,
                    HelpText = "SARI KART: İsim kartı yoksa toplam puandan 5 puan düşülür",
                    CreatedAt = TurkeyTime.Now
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Müşteriye güler yüzle yaklaşıldı mı?",
                    ScoringTypeId = ScoringTypes.Ids.Penalty,
                    WeightPoints = 8, // Ceza miktarı: 8 puan düşülür
                    MaxPoints = 2,
                    PenaltyTypeId = PenaltyTypes.Ids.YellowCard,
                    IsRequired = true,
                    Order = 11,
                    HelpText = "SARI KART: Güler yüz yoksa 8 puan düşülür",
                    CreatedAt = TurkeyTime.Now
                },

                // KIRMIZI KART: Ciddi ihlaller - yüksek puan kesintisi veya sıfırlama
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Hijyen kurallarına uyuluyor mu? (Eldiven, bone vb.)",
                    ScoringTypeId = ScoringTypes.Ids.Penalty,
                    WeightPoints = 25, // Ciddi ceza: 25 puan düşülür
                    MaxPoints = 2,
                    PenaltyTypeId = PenaltyTypes.Ids.RedCard,
                    IsRequired = true,
                    Order = 12,
                    HelpText = "KIRMIZI KART: Hijyen ihlali tespit edilirse 25 puan düşülür",
                    CreatedAt = TurkeyTime.Now
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Müşteriye saygısız davranış var mı?",
                    ScoringTypeId = ScoringTypes.Ids.Penalty,
                    WeightPoints = 50, // Çok ciddi ceza: 50 puan düşülür
                    MaxPoints = 2,
                    PenaltyTypeId = PenaltyTypes.Ids.RedCard,
                    IsRequired = true,
                    Order = 13,
                    HelpText = "KIRMIZI KART: Saygısız davranış tespit edilirse 50 puan düşülür",
                    CreatedAt = TurkeyTime.Now
                },
                new Question
                {
                    ChecklistId = checklist.Id,
                    Text = "Bozuk/bayat ürün servisi var mı?",
                    ScoringTypeId = ScoringTypes.Ids.Penalty,
                    WeightPoints = 100, // Tam sıfırlama: 100 puan düşülür
                    MaxPoints = 2,
                    PenaltyTypeId = PenaltyTypes.Ids.RedCard,
                    IsRequired = true,
                    Order = 14,
                    HelpText = "KIRMIZI KART: Bozuk ürün servisi = DEĞERLENDİRME SIFIRLANIR",
                    CreatedAt = TurkeyTime.Now
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
                AssignmentTypeId = AssignmentTypes.Ids.InternalBranch,
                StartDate = TurkeyTime.Now,
                EndDate = TurkeyTime.Now.AddMonths(3),
                IsActive = true,
                CreatedAt = TurkeyTime.Now
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
                DueDate = TurkeyTime.Now.AddDays(7),
                CreatedAt = TurkeyTime.Now
            };

            var assignment2 = new Assignment
            {
                ProjectId = project.Id,
                ChecklistId = checklist.Id,
                AssignedUserId = evaluator2.Id,
                DueDate = TurkeyTime.Now.AddDays(7),
                CreatedAt = TurkeyTime.Now
            };

            // Sample Assignment (External)
            var assignment3 = new Assignment
            {
                ProjectId = project.Id,
                ChecklistId = checklist.Id,
                UniqueLink = Guid.NewGuid().ToString("N"),
                DueDate = TurkeyTime.Now.AddDays(14),
                CreatedAt = TurkeyTime.Now
            };

            context.Assignments.AddRange(assignment1, assignment2, assignment3);
            await context.SaveChangesAsync();
            logger.LogInformation("Assignments created");

            // 7. Sample Evaluations
            var evaluation1 = new Evaluation
            {
                ProjectId = project.Id,
                EvaluatorId = evaluator1.Id,
                StatusId = EvaluationStatuses.Ids.Completed,
                TotalScore = 85.5m,
                MaxScore = 100m,
                ScorePercentage = 85.5m,
                StartedAt = TurkeyTime.Now.AddDays(-5),
                CompletedAt = TurkeyTime.Now.AddDays(-4),
                Notes = "Genel olarak iyi performans",
                CreatedAt = TurkeyTime.Now.AddDays(-5)
            };

            var evaluation2 = new Evaluation
            {
                ProjectId = project.Id,
                EvaluatorId = evaluator2.Id,
                StatusId = EvaluationStatuses.Ids.Completed,
                TotalScore = 92.0m,
                MaxScore = 100m,
                ScorePercentage = 92.0m,
                StartedAt = TurkeyTime.Now.AddDays(-3),
                CompletedAt = TurkeyTime.Now.AddDays(-2),
                Notes = "Mükemmel hizmet kalitesi",
                CreatedAt = TurkeyTime.Now.AddDays(-3)
            };

            var evaluation3 = new Evaluation
            {
                ProjectId = project.Id,
                StatusId = EvaluationStatuses.Ids.Completed,
                TotalScore = 78.0m,
                MaxScore = 100m,
                ScorePercentage = 78.0m,
                StartedAt = TurkeyTime.Now.AddDays(-1),
                CompletedAt = TurkeyTime.Now,
                Notes = "Temizlik konusunda iyileştirme gerekli",
                CreatedAt = TurkeyTime.Now.AddDays(-1)
            };

            // Geçmiş aylardan ek değerlendirmeler
            var evaluation4 = new Evaluation
            {
                ProjectId = project.Id,
                EvaluatorId = evaluator1.Id,
                StatusId = EvaluationStatuses.Ids.Completed,
                TotalScore = 88.0m,
                MaxScore = 100m,
                ScorePercentage = 88.0m,
                StartedAt = TurkeyTime.Now.AddMonths(-1).AddDays(-10),
                CompletedAt = TurkeyTime.Now.AddMonths(-1).AddDays(-9),
                Notes = "Geçen ayın değerlendirmesi",
                CreatedAt = TurkeyTime.Now.AddMonths(-1).AddDays(-10)
            };

            var evaluation5 = new Evaluation
            {
                ProjectId = project.Id,
                EvaluatorId = evaluator2.Id,
                StatusId = EvaluationStatuses.Ids.Completed,
                TotalScore = 75.5m,
                MaxScore = 100m,
                ScorePercentage = 75.5m,
                StartedAt = TurkeyTime.Now.AddMonths(-2).AddDays(-5),
                CompletedAt = TurkeyTime.Now.AddMonths(-2).AddDays(-4),
                Notes = "2 ay önceki değerlendirme",
                CreatedAt = TurkeyTime.Now.AddMonths(-2).AddDays(-5)
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
                ContractStartDate = TurkeyTime.Now.AddMonths(-6),
                ContractEndDate = TurkeyTime.Now.AddMonths(18),
                Notes = "Perakende sektöründe faaliyet gösteren büyük zincir",
                CreatedAt = TurkeyTime.Now
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
                ContractStartDate = TurkeyTime.Now.AddMonths(-3),
                ContractEndDate = TurkeyTime.Now.AddMonths(21),
                Notes = "Restoran zinciri - 15 şubesi var",
                CreatedAt = TurkeyTime.Now
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
                ContractStartDate = TurkeyTime.Now.AddMonths(-12),
                ContractEndDate = TurkeyTime.Now.AddMonths(12),
                Notes = "5 yıldızlı otel zinciri",
                CreatedAt = TurkeyTime.Now
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
                RoleId = CustomerPersonnelRoles.Ids.Manager,
                IsActive = true,
                Notes = "ABC Perakende kalite müdürü - tüm yetkilere sahip",
                CreatedAt = TurkeyTime.Now
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
                RoleId = CustomerPersonnelRoles.Ids.Supervisor,
                IsActive = true,
                Notes = "Şube denetimlerinden sorumlu",
                CreatedAt = TurkeyTime.Now
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
                RoleId = CustomerPersonnelRoles.Ids.Manager,
                IsActive = true,
                Notes = "XYZ Restaurant Grubu işletme müdürü",
                CreatedAt = TurkeyTime.Now
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
                RoleId = CustomerPersonnelRoles.Ids.Operator,
                IsActive = true,
                Notes = "Mutfak operasyonları sorumlusu",
                CreatedAt = TurkeyTime.Now
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
                RoleId = CustomerPersonnelRoles.Ids.Manager,
                IsActive = true,
                Notes = "Otel zinciri genel müdürü",
                CreatedAt = TurkeyTime.Now
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
                TaskTypeId = CustomerTaskTypes.Ids.Inspection,
                PriorityId = TaskPriorities.Ids.High,
                StartDate = TurkeyTime.Now.AddDays(-5),
                EndDate = TurkeyTime.Now.AddDays(25),
                StatusId = TaskStatuses.Ids.InProgress,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var taskList2 = new CustomerTaskList
            {
                CustomerId = customer2.Id,
                Name = "Hijyen ve Temizlik Kontrolü",
                Description = "Restaurant şubelerinin hijyen standartları kontrolü",
                TaskTypeId = CustomerTaskTypes.Ids.Audit,
                PriorityId = TaskPriorities.Ids.Critical,
                StartDate = TurkeyTime.Now,
                EndDate = TurkeyTime.Now.AddDays(14),
                StatusId = TaskStatuses.Ids.NotStarted,
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            context.CustomerTaskLists.AddRange(taskList1, taskList2);
            await context.SaveChangesAsync();
            logger.LogInformation("Customer Task Lists created");

            // 11. Customer Personnel Task Assignments
            var taskAssignment1 = new CustomerPersonnelTaskAssignment
            {
                PersonnelId = personnel2.Id,
                TaskListId = taskList1.Id,
                AssignmentRoleId = TaskAssignmentRoles.Ids.Owner,
                AssignedDate = TurkeyTime.Now.AddDays(-5),
                Notes = "Şube denetimlerinin koordinasyonundan sorumlu",
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var taskAssignment2 = new CustomerPersonnelTaskAssignment
            {
                PersonnelId = personnel3.Id,
                TaskListId = taskList2.Id,
                AssignmentRoleId = TaskAssignmentRoles.Ids.Owner,
                AssignedDate = TurkeyTime.Now,
                Notes = "Hijyen kontrollerinin takibi",
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            var taskAssignment3 = new CustomerPersonnelTaskAssignment
            {
                PersonnelId = personnel4.Id,
                TaskListId = taskList2.Id,
                AssignmentRoleId = TaskAssignmentRoles.Ids.Assistant,
                AssignedDate = TurkeyTime.Now,
                Notes = "Mutfak hijyeni kontrolü desteği",
                IsActive = true,
                CreatedAt = TurkeyTime.Now
            };

            context.CustomerPersonnelTaskAssignments.AddRange(taskAssignment1, taskAssignment2, taskAssignment3);
            await context.SaveChangesAsync();
            logger.LogInformation("Customer Personnel Task Assignments created");

            // 12. Link project to customer
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
            new Permission { Code = "Users.View", DisplayName = "Kullanıcıları Görüntüle", CategoryId = PermissionCategories.Ids.Users, SortOrder = 1, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Users.Create", DisplayName = "Kullanıcı Oluştur", CategoryId = PermissionCategories.Ids.Users, SortOrder = 2, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Users.Edit", DisplayName = "Kullanıcı Düzenle", CategoryId = PermissionCategories.Ids.Users, SortOrder = 3, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Users.Delete", DisplayName = "Kullanıcı Sil", CategoryId = PermissionCategories.Ids.Users, SortOrder = 4, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Users.Manage", DisplayName = "Kullanıcı Yönetimi (Tam Yetki)", CategoryId = PermissionCategories.Ids.Users, SortOrder = 5, CreatedAt = TurkeyTime.Now },

            // Projects
            new Permission { Code = "Projects.View", DisplayName = "Projeleri Görüntüle", CategoryId = PermissionCategories.Ids.Projects, SortOrder = 10, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Projects.Create", DisplayName = "Proje Oluştur", CategoryId = PermissionCategories.Ids.Projects, SortOrder = 11, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Projects.Edit", DisplayName = "Proje Düzenle", CategoryId = PermissionCategories.Ids.Projects, SortOrder = 12, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Projects.Delete", DisplayName = "Proje Sil", CategoryId = PermissionCategories.Ids.Projects, SortOrder = 13, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Projects.Manage", DisplayName = "Proje Yönetimi (Tam Yetki)", CategoryId = PermissionCategories.Ids.Projects, SortOrder = 14, CreatedAt = TurkeyTime.Now },

            // Assignments
            new Permission { Code = "Assignments.View", DisplayName = "Atamaları Görüntüle", CategoryId = PermissionCategories.Ids.Assignments, SortOrder = 20, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Assignments.Create", DisplayName = "Atama Oluştur", CategoryId = PermissionCategories.Ids.Assignments, SortOrder = 21, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Assignments.Edit", DisplayName = "Atama Düzenle", CategoryId = PermissionCategories.Ids.Assignments, SortOrder = 22, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Assignments.Delete", DisplayName = "Atama Sil", CategoryId = PermissionCategories.Ids.Assignments, SortOrder = 23, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Assignments.Manage", DisplayName = "Atama Yönetimi (Tam Yetki)", CategoryId = PermissionCategories.Ids.Assignments, SortOrder = 24, CreatedAt = TurkeyTime.Now },

            // Checklists
            new Permission { Code = "Checklists.View", DisplayName = "Kontrol Listelerini Görüntüle", CategoryId = PermissionCategories.Ids.Checklists, SortOrder = 30, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Checklists.Create", DisplayName = "Kontrol Listesi Oluştur", CategoryId = PermissionCategories.Ids.Checklists, SortOrder = 31, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Checklists.Edit", DisplayName = "Kontrol Listesi Düzenle", CategoryId = PermissionCategories.Ids.Checklists, SortOrder = 32, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Checklists.Delete", DisplayName = "Kontrol Listesi Sil", CategoryId = PermissionCategories.Ids.Checklists, SortOrder = 33, CreatedAt = TurkeyTime.Now },

            // Reports
            new Permission { Code = "Reports.View", DisplayName = "Raporları Görüntüle", CategoryId = PermissionCategories.Ids.Reports, SortOrder = 40, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Reports.Export", DisplayName = "Rapor Dışa Aktar", CategoryId = PermissionCategories.Ids.Reports, SortOrder = 41, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Reports.Create", DisplayName = "Rapor Oluştur", CategoryId = PermissionCategories.Ids.Reports, SortOrder = 42, CreatedAt = TurkeyTime.Now },

            // Dashboard
            new Permission { Code = "Dashboard.View", DisplayName = "Dashboard Görüntüle", CategoryId = PermissionCategories.Ids.Dashboard, SortOrder = 50, CreatedAt = TurkeyTime.Now },

            // Permissions Management
            new Permission { Code = "Permissions.View", DisplayName = "Yetkileri Görüntüle", CategoryId = PermissionCategories.Ids.Settings, SortOrder = 60, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Permissions.Manage", DisplayName = "Yetki Yönetimi", CategoryId = PermissionCategories.Ids.Settings, SortOrder = 61, CreatedAt = TurkeyTime.Now },

            // Evaluations
            new Permission { Code = "Evaluations.View", DisplayName = "Değerlendirmeleri Görüntüle", CategoryId = PermissionCategories.Ids.Evaluations, SortOrder = 70, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Evaluations.Create", DisplayName = "Değerlendirme Oluştur", CategoryId = PermissionCategories.Ids.Evaluations, SortOrder = 71, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Evaluations.Edit", DisplayName = "Değerlendirme Düzenle", CategoryId = PermissionCategories.Ids.Evaluations, SortOrder = 72, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Evaluations.Delete", DisplayName = "Değerlendirme Sil", CategoryId = PermissionCategories.Ids.Evaluations, SortOrder = 73, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Evaluations.RevertToDraft", DisplayName = "Taslağa Al", CategoryId = PermissionCategories.Ids.Evaluations, SortOrder = 74, CreatedAt = TurkeyTime.Now },

            // Customers
            new Permission { Code = "Customers.View", DisplayName = "Müşterileri Görüntüle", CategoryId = PermissionCategories.Ids.Customers, SortOrder = 80, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Customers.Create", DisplayName = "Müşteri Oluştur", CategoryId = PermissionCategories.Ids.Customers, SortOrder = 81, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Customers.Edit", DisplayName = "Müşteri Düzenle", CategoryId = PermissionCategories.Ids.Customers, SortOrder = 82, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Customers.Delete", DisplayName = "Müşteri Sil", CategoryId = PermissionCategories.Ids.Customers, SortOrder = 83, CreatedAt = TurkeyTime.Now },

            // Customer Organizations
            new Permission { Code = "CustomerOrganizations.View", DisplayName = "Müşteri Organizasyonlarını Görüntüle", CategoryId = PermissionCategories.Ids.CustomerOrganizations, SortOrder = 90, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "CustomerOrganizations.Create", DisplayName = "Müşteri Organizasyonu Oluştur", CategoryId = PermissionCategories.Ids.CustomerOrganizations, SortOrder = 91, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "CustomerOrganizations.Edit", DisplayName = "Müşteri Organizasyonu Düzenle", CategoryId = PermissionCategories.Ids.CustomerOrganizations, SortOrder = 92, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "CustomerOrganizations.Delete", DisplayName = "Müşteri Organizasyonu Sil", CategoryId = PermissionCategories.Ids.CustomerOrganizations, SortOrder = 93, CreatedAt = TurkeyTime.Now },

            // Customer Personnel
            new Permission { Code = "CustomerPersonnel.View", DisplayName = "Müşteri Personelini Görüntüle", CategoryId = PermissionCategories.Ids.CustomerPersonnel, SortOrder = 100, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "CustomerPersonnel.Create", DisplayName = "Müşteri Personeli Oluştur", CategoryId = PermissionCategories.Ids.CustomerPersonnel, SortOrder = 101, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "CustomerPersonnel.Edit", DisplayName = "Müşteri Personeli Düzenle", CategoryId = PermissionCategories.Ids.CustomerPersonnel, SortOrder = 102, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "CustomerPersonnel.Delete", DisplayName = "Müşteri Personeli Sil", CategoryId = PermissionCategories.Ids.CustomerPersonnel, SortOrder = 103, CreatedAt = TurkeyTime.Now },

            // Personnel (Şube Personeli)
            new Permission { Code = "Personnel.View", DisplayName = "Personeli Görüntüle", CategoryId = PermissionCategories.Ids.Personnel, SortOrder = 110, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Personnel.Create", DisplayName = "Personel Oluştur", CategoryId = PermissionCategories.Ids.Personnel, SortOrder = 111, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Personnel.Edit", DisplayName = "Personel Düzenle", CategoryId = PermissionCategories.Ids.Personnel, SortOrder = 112, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Personnel.Delete", DisplayName = "Personel Sil", CategoryId = PermissionCategories.Ids.Personnel, SortOrder = 113, CreatedAt = TurkeyTime.Now },

            // Languages
            new Permission { Code = "Languages.View", DisplayName = "Dilleri Görüntüle", CategoryId = PermissionCategories.Ids.Languages, SortOrder = 120, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Languages.Create", DisplayName = "Dil Oluştur", CategoryId = PermissionCategories.Ids.Languages, SortOrder = 121, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Languages.Edit", DisplayName = "Dil Düzenle", CategoryId = PermissionCategories.Ids.Languages, SortOrder = 122, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Languages.Delete", DisplayName = "Dil Sil", CategoryId = PermissionCategories.Ids.Languages, SortOrder = 123, CreatedAt = TurkeyTime.Now },

            // Trainings
            new Permission { Code = "Trainings.View", DisplayName = "Eğitimleri Görüntüle", CategoryId = PermissionCategories.Ids.Trainings, SortOrder = 130, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Trainings.Create", DisplayName = "Eğitim Oluştur", CategoryId = PermissionCategories.Ids.Trainings, SortOrder = 131, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Trainings.Edit", DisplayName = "Eğitim Düzenle", CategoryId = PermissionCategories.Ids.Trainings, SortOrder = 132, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Trainings.Delete", DisplayName = "Eğitim Sil", CategoryId = PermissionCategories.Ids.Trainings, SortOrder = 133, CreatedAt = TurkeyTime.Now },

            // Meetings
            new Permission { Code = "Meetings.View", DisplayName = "Toplantıları Görüntüle", CategoryId = PermissionCategories.Ids.Meetings, SortOrder = 140, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Meetings.Create", DisplayName = "Toplantı Oluştur", CategoryId = PermissionCategories.Ids.Meetings, SortOrder = 141, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Meetings.Edit", DisplayName = "Toplantı Düzenle", CategoryId = PermissionCategories.Ids.Meetings, SortOrder = 142, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Meetings.Delete", DisplayName = "Toplantı Sil", CategoryId = PermissionCategories.Ids.Meetings, SortOrder = 143, CreatedAt = TurkeyTime.Now },

            // Approvals
            new Permission { Code = "Approvals.View", DisplayName = "Onayları Görüntüle", CategoryId = PermissionCategories.Ids.Approvals, SortOrder = 150, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Approvals.Create", DisplayName = "Onay Oluştur", CategoryId = PermissionCategories.Ids.Approvals, SortOrder = 151, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Approvals.Edit", DisplayName = "Onay Düzenle", CategoryId = PermissionCategories.Ids.Approvals, SortOrder = 152, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Approvals.Delete", DisplayName = "Onay Sil", CategoryId = PermissionCategories.Ids.Approvals, SortOrder = 153, CreatedAt = TurkeyTime.Now },

            // Excel Templates
            new Permission { Code = "ExcelTemplates.View", DisplayName = "Excel Şablonlarını Görüntüle", CategoryId = PermissionCategories.Ids.ExcelTemplates, SortOrder = 160, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "ExcelTemplates.Create", DisplayName = "Excel Şablonu Oluştur", CategoryId = PermissionCategories.Ids.ExcelTemplates, SortOrder = 161, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "ExcelTemplates.Edit", DisplayName = "Excel Şablonu Düzenle", CategoryId = PermissionCategories.Ids.ExcelTemplates, SortOrder = 162, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "ExcelTemplates.Delete", DisplayName = "Excel Şablonu Sil", CategoryId = PermissionCategories.Ids.ExcelTemplates, SortOrder = 163, CreatedAt = TurkeyTime.Now },

            // Draft Requests (Taslağa Alma Talepleri)
            new Permission { Code = "DraftRequests.View", DisplayName = "Taslak Taleplerini Görüntüle", CategoryId = PermissionCategories.Ids.DraftRequests, SortOrder = 170, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "DraftRequests.Approve", DisplayName = "Taslak Talebini Onayla", CategoryId = PermissionCategories.Ids.DraftRequests, SortOrder = 171, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "DraftRequests.Reject", DisplayName = "Taslak Talebini Reddet", CategoryId = PermissionCategories.Ids.DraftRequests, SortOrder = 172, CreatedAt = TurkeyTime.Now },
        };

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();
        logger.LogInformation($"Created {permissions.Count} permissions");

        // Role-Permission mappings - Admin gets everything
        var adminRoleId = UserRoles.Ids.Admin;
        foreach (var permission in permissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRoleId,
                PermissionId = permission.Id,
                IsGranted = true,
                ScopeId = PermissionScopes.Ids.All,
                CreatedAt = TurkeyTime.Now
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
                RoleId = UserRoles.Ids.QualitySpecialist,
                PermissionId = permission.Id,
                IsGranted = true,
                ScopeId = PermissionScopes.Ids.Branch,
                CreatedAt = TurkeyTime.Now
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
                RoleId = UserRoles.Ids.FieldWorker,
                PermissionId = permission.Id,
                IsGranted = true,
                ScopeId = PermissionScopes.Ids.Own,
                CreatedAt = TurkeyTime.Now
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
                .AnyAsync(rp => rp.RoleId == UserRoles.Ids.Admin && rp.PermissionId == permission.Id);

            if (!existingAdminMapping)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = UserRoles.Ids.Admin,
                    PermissionId = permission.Id,
                    IsGranted = true,
                    ScopeId = PermissionScopes.Ids.All,
                    CreatedAt = TurkeyTime.Now
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
            new Permission { Code = "Users.View", DisplayName = "Kullanıcıları Görüntüle", CategoryId = PermissionCategories.Ids.Users, SortOrder = 1, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Users.Create", DisplayName = "Kullanıcı Oluştur", CategoryId = PermissionCategories.Ids.Users, SortOrder = 2, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Users.Edit", DisplayName = "Kullanıcı Düzenle", CategoryId = PermissionCategories.Ids.Users, SortOrder = 3, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Users.Delete", DisplayName = "Kullanıcı Sil", CategoryId = PermissionCategories.Ids.Users, SortOrder = 4, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Users.Manage", DisplayName = "Kullanıcı Yönetimi (Tam Yetki)", CategoryId = PermissionCategories.Ids.Users, SortOrder = 5, CreatedAt = TurkeyTime.Now },

            // Projects
            new Permission { Code = "Projects.View", DisplayName = "Projeleri Görüntüle", CategoryId = PermissionCategories.Ids.Projects, SortOrder = 10, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Projects.Create", DisplayName = "Proje Oluştur", CategoryId = PermissionCategories.Ids.Projects, SortOrder = 11, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Projects.Edit", DisplayName = "Proje Düzenle", CategoryId = PermissionCategories.Ids.Projects, SortOrder = 12, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Projects.Delete", DisplayName = "Proje Sil", CategoryId = PermissionCategories.Ids.Projects, SortOrder = 13, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Projects.Manage", DisplayName = "Proje Yönetimi (Tam Yetki)", CategoryId = PermissionCategories.Ids.Projects, SortOrder = 14, CreatedAt = TurkeyTime.Now },

            // Assignments
            new Permission { Code = "Assignments.View", DisplayName = "Atamaları Görüntüle", CategoryId = PermissionCategories.Ids.Assignments, SortOrder = 20, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Assignments.Create", DisplayName = "Atama Oluştur", CategoryId = PermissionCategories.Ids.Assignments, SortOrder = 21, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Assignments.Edit", DisplayName = "Atama Düzenle", CategoryId = PermissionCategories.Ids.Assignments, SortOrder = 22, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Assignments.Delete", DisplayName = "Atama Sil", CategoryId = PermissionCategories.Ids.Assignments, SortOrder = 23, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Assignments.Manage", DisplayName = "Atama Yönetimi (Tam Yetki)", CategoryId = PermissionCategories.Ids.Assignments, SortOrder = 24, CreatedAt = TurkeyTime.Now },

            // Checklists
            new Permission { Code = "Checklists.View", DisplayName = "Kontrol Listelerini Görüntüle", CategoryId = PermissionCategories.Ids.Checklists, SortOrder = 30, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Checklists.Create", DisplayName = "Kontrol Listesi Oluştur", CategoryId = PermissionCategories.Ids.Checklists, SortOrder = 31, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Checklists.Edit", DisplayName = "Kontrol Listesi Düzenle", CategoryId = PermissionCategories.Ids.Checklists, SortOrder = 32, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Checklists.Delete", DisplayName = "Kontrol Listesi Sil", CategoryId = PermissionCategories.Ids.Checklists, SortOrder = 33, CreatedAt = TurkeyTime.Now },

            // Reports
            new Permission { Code = "Reports.View", DisplayName = "Raporları Görüntüle", CategoryId = PermissionCategories.Ids.Reports, SortOrder = 40, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Reports.Export", DisplayName = "Rapor Dışa Aktar", CategoryId = PermissionCategories.Ids.Reports, SortOrder = 41, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Reports.Create", DisplayName = "Rapor Oluştur", CategoryId = PermissionCategories.Ids.Reports, SortOrder = 42, CreatedAt = TurkeyTime.Now },

            // Dashboard
            new Permission { Code = "Dashboard.View", DisplayName = "Dashboard Görüntüle", CategoryId = PermissionCategories.Ids.Dashboard, SortOrder = 50, CreatedAt = TurkeyTime.Now },

            // Permissions Management
            new Permission { Code = "Permissions.View", DisplayName = "Yetkileri Görüntüle", CategoryId = PermissionCategories.Ids.Settings, SortOrder = 60, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Permissions.Manage", DisplayName = "Yetki Yönetimi", CategoryId = PermissionCategories.Ids.Settings, SortOrder = 61, CreatedAt = TurkeyTime.Now },

            // Evaluations
            new Permission { Code = "Evaluations.View", DisplayName = "Değerlendirmeleri Görüntüle", CategoryId = PermissionCategories.Ids.Evaluations, SortOrder = 70, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Evaluations.Create", DisplayName = "Değerlendirme Oluştur", CategoryId = PermissionCategories.Ids.Evaluations, SortOrder = 71, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Evaluations.Edit", DisplayName = "Değerlendirme Düzenle", CategoryId = PermissionCategories.Ids.Evaluations, SortOrder = 72, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Evaluations.Delete", DisplayName = "Değerlendirme Sil", CategoryId = PermissionCategories.Ids.Evaluations, SortOrder = 73, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Evaluations.RevertToDraft", DisplayName = "Taslağa Al", CategoryId = PermissionCategories.Ids.Evaluations, SortOrder = 74, CreatedAt = TurkeyTime.Now },

            // Customers
            new Permission { Code = "Customers.View", DisplayName = "Müşterileri Görüntüle", CategoryId = PermissionCategories.Ids.Customers, SortOrder = 80, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Customers.Create", DisplayName = "Müşteri Oluştur", CategoryId = PermissionCategories.Ids.Customers, SortOrder = 81, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Customers.Edit", DisplayName = "Müşteri Düzenle", CategoryId = PermissionCategories.Ids.Customers, SortOrder = 82, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Customers.Delete", DisplayName = "Müşteri Sil", CategoryId = PermissionCategories.Ids.Customers, SortOrder = 83, CreatedAt = TurkeyTime.Now },

            // Customer Organizations
            new Permission { Code = "CustomerOrganizations.View", DisplayName = "Müşteri Organizasyonlarını Görüntüle", CategoryId = PermissionCategories.Ids.CustomerOrganizations, SortOrder = 90, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "CustomerOrganizations.Create", DisplayName = "Müşteri Organizasyonu Oluştur", CategoryId = PermissionCategories.Ids.CustomerOrganizations, SortOrder = 91, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "CustomerOrganizations.Edit", DisplayName = "Müşteri Organizasyonu Düzenle", CategoryId = PermissionCategories.Ids.CustomerOrganizations, SortOrder = 92, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "CustomerOrganizations.Delete", DisplayName = "Müşteri Organizasyonu Sil", CategoryId = PermissionCategories.Ids.CustomerOrganizations, SortOrder = 93, CreatedAt = TurkeyTime.Now },

            // Customer Personnel
            new Permission { Code = "CustomerPersonnel.View", DisplayName = "Müşteri Personelini Görüntüle", CategoryId = PermissionCategories.Ids.CustomerPersonnel, SortOrder = 100, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "CustomerPersonnel.Create", DisplayName = "Müşteri Personeli Oluştur", CategoryId = PermissionCategories.Ids.CustomerPersonnel, SortOrder = 101, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "CustomerPersonnel.Edit", DisplayName = "Müşteri Personeli Düzenle", CategoryId = PermissionCategories.Ids.CustomerPersonnel, SortOrder = 102, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "CustomerPersonnel.Delete", DisplayName = "Müşteri Personeli Sil", CategoryId = PermissionCategories.Ids.CustomerPersonnel, SortOrder = 103, CreatedAt = TurkeyTime.Now },

            // Personnel (Şube Personeli)
            new Permission { Code = "Personnel.View", DisplayName = "Personeli Görüntüle", CategoryId = PermissionCategories.Ids.Personnel, SortOrder = 110, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Personnel.Create", DisplayName = "Personel Oluştur", CategoryId = PermissionCategories.Ids.Personnel, SortOrder = 111, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Personnel.Edit", DisplayName = "Personel Düzenle", CategoryId = PermissionCategories.Ids.Personnel, SortOrder = 112, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Personnel.Delete", DisplayName = "Personel Sil", CategoryId = PermissionCategories.Ids.Personnel, SortOrder = 113, CreatedAt = TurkeyTime.Now },

            // Languages
            new Permission { Code = "Languages.View", DisplayName = "Dilleri Görüntüle", CategoryId = PermissionCategories.Ids.Languages, SortOrder = 120, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Languages.Create", DisplayName = "Dil Oluştur", CategoryId = PermissionCategories.Ids.Languages, SortOrder = 121, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Languages.Edit", DisplayName = "Dil Düzenle", CategoryId = PermissionCategories.Ids.Languages, SortOrder = 122, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Languages.Delete", DisplayName = "Dil Sil", CategoryId = PermissionCategories.Ids.Languages, SortOrder = 123, CreatedAt = TurkeyTime.Now },

            // Trainings
            new Permission { Code = "Trainings.View", DisplayName = "Eğitimleri Görüntüle", CategoryId = PermissionCategories.Ids.Trainings, SortOrder = 130, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Trainings.Create", DisplayName = "Eğitim Oluştur", CategoryId = PermissionCategories.Ids.Trainings, SortOrder = 131, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Trainings.Edit", DisplayName = "Eğitim Düzenle", CategoryId = PermissionCategories.Ids.Trainings, SortOrder = 132, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Trainings.Delete", DisplayName = "Eğitim Sil", CategoryId = PermissionCategories.Ids.Trainings, SortOrder = 133, CreatedAt = TurkeyTime.Now },

            // Meetings
            new Permission { Code = "Meetings.View", DisplayName = "Toplantıları Görüntüle", CategoryId = PermissionCategories.Ids.Meetings, SortOrder = 140, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Meetings.Create", DisplayName = "Toplantı Oluştur", CategoryId = PermissionCategories.Ids.Meetings, SortOrder = 141, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Meetings.Edit", DisplayName = "Toplantı Düzenle", CategoryId = PermissionCategories.Ids.Meetings, SortOrder = 142, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Meetings.Delete", DisplayName = "Toplantı Sil", CategoryId = PermissionCategories.Ids.Meetings, SortOrder = 143, CreatedAt = TurkeyTime.Now },

            // Approvals
            new Permission { Code = "Approvals.View", DisplayName = "Onayları Görüntüle", CategoryId = PermissionCategories.Ids.Approvals, SortOrder = 150, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Approvals.Create", DisplayName = "Onay Oluştur", CategoryId = PermissionCategories.Ids.Approvals, SortOrder = 151, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Approvals.Edit", DisplayName = "Onay Düzenle", CategoryId = PermissionCategories.Ids.Approvals, SortOrder = 152, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "Approvals.Delete", DisplayName = "Onay Sil", CategoryId = PermissionCategories.Ids.Approvals, SortOrder = 153, CreatedAt = TurkeyTime.Now },

            // Excel Templates
            new Permission { Code = "ExcelTemplates.View", DisplayName = "Excel Şablonlarını Görüntüle", CategoryId = PermissionCategories.Ids.ExcelTemplates, SortOrder = 160, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "ExcelTemplates.Create", DisplayName = "Excel Şablonu Oluştur", CategoryId = PermissionCategories.Ids.ExcelTemplates, SortOrder = 161, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "ExcelTemplates.Edit", DisplayName = "Excel Şablonu Düzenle", CategoryId = PermissionCategories.Ids.ExcelTemplates, SortOrder = 162, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "ExcelTemplates.Delete", DisplayName = "Excel Şablonu Sil", CategoryId = PermissionCategories.Ids.ExcelTemplates, SortOrder = 163, CreatedAt = TurkeyTime.Now },

            // Draft Requests (Taslağa Alma Talepleri)
            new Permission { Code = "DraftRequests.View", DisplayName = "Taslak Taleplerini Görüntüle", CategoryId = PermissionCategories.Ids.DraftRequests, SortOrder = 170, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "DraftRequests.Approve", DisplayName = "Taslak Talebini Onayla", CategoryId = PermissionCategories.Ids.DraftRequests, SortOrder = 171, CreatedAt = TurkeyTime.Now },
            new Permission { Code = "DraftRequests.Reject", DisplayName = "Taslak Talebini Reddet", CategoryId = PermissionCategories.Ids.DraftRequests, SortOrder = 172, CreatedAt = TurkeyTime.Now },
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
                ValueTypeId = SettingValueTypes.Ids.Bool,
                Category = "General",
                Description = "Demo modu aktif. True ise detaylı hata mesajları gösterilir.",
                DisplayOrder = 1,
                IsSystem = true
            },
            new AppSettings
            {
                Key = "General.AppName",
                Value = "NCAcademy",
                ValueTypeId = SettingValueTypes.Ids.String,
                Category = "General",
                Description = "Uygulama adı",
                DisplayOrder = 2,
                IsSystem = true
            },
            new AppSettings
            {
                Key = "General.Version",
                Value = "1.0.0",
                ValueTypeId = SettingValueTypes.Ids.String,
                Category = "General",
                Description = "Uygulama versiyonu",
                DisplayOrder = 3,
                IsSystem = true
            },
            new AppSettings
            {
                Key = "General.MaintenanceMode",
                Value = "false",
                ValueTypeId = SettingValueTypes.Ids.Bool,
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
                ValueTypeId = SettingValueTypes.Ids.Int,
                Category = "Security",
                Description = "Maksimum başarısız giriş denemesi sayısı",
                DisplayOrder = 1,
                IsSystem = false
            },
            new AppSettings
            {
                Key = "Security.LockoutDurationMinutes",
                Value = "15",
                ValueTypeId = SettingValueTypes.Ids.Int,
                Category = "Security",
                Description = "Hesap kilitleme süresi (dakika)",
                DisplayOrder = 2,
                IsSystem = false
            },
            new AppSettings
            {
                Key = "Security.SessionTimeoutMinutes",
                Value = "60",
                ValueTypeId = SettingValueTypes.Ids.Int,
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
                ValueTypeId = SettingValueTypes.Ids.Bool,
                Category = "CustomerPortal",
                Description = "Müşteri portalı aktif mi?",
                DisplayOrder = 1,
                IsSystem = false
            },
            new AppSettings
            {
                Key = "CustomerPortal.AllowSelfRegistration",
                Value = "false",
                ValueTypeId = SettingValueTypes.Ids.Bool,
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
                CreatedAt = TurkeyTime.Now
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
                CreatedAt = TurkeyTime.Now
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
                CreatedAt = TurkeyTime.Now
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
                CreatedAt = TurkeyTime.Now
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
                CreatedAt = TurkeyTime.Now
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.DefaultPeriodTarget,
                Value = "1000",
                ValueType = "int",
                Category = "Dashboard",
                Description = "Varsayılan dönem hedefi (AssignmentPeriod için)",
                CreatedAt = TurkeyTime.Now
            },
            // Evaluation Settings
            new SystemSetting
            {
                Key = "Evaluation.RequireOrganizationSelection",
                Value = "true",
                ValueType = "bool",
                Category = "Evaluation",
                Description = "Değerlendirmede organizasyon seçimi zorunlu mu?",
                CreatedAt = TurkeyTime.Now
            },
            new SystemSetting
            {
                Key = "Evaluation.AllowAutoSave",
                Value = "true",
                ValueType = "bool",
                Category = "Evaluation",
                Description = "Değerlendirmede otomatik kaydetme aktif mi?",
                CreatedAt = TurkeyTime.Now
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
                CreatedAt = TurkeyTime.Now
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
                CreatedAt = TurkeyTime.Now
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
                CreatedAt = TurkeyTime.Now
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
                CreatedAt = TurkeyTime.Now
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
                                CreatedAt = TurkeyTime.Now
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
