using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using System.Text.Json;

namespace SecretCustomer.Data;

public static class SeedData
{
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

            logger.LogInformation("Starting database seed...");

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
                IsActive = true,
                CreatedAt = DateTime.UtcNow
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
                IsActive = true,
                CreatedAt = DateTime.UtcNow
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
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var evaluator2 = new User
            {
                Id = Guid.NewGuid(),
                Username = "evaluator2",
                Email = "evaluator2@secretcustomer.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Eval@123"),
                FirstName = "Jane",
                LastName = "Evaluator",
                Role = UserRole.Evaluator,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(adminUser, teamLeader, evaluator1, evaluator2);
            await context.SaveChangesAsync();
            logger.LogInformation("Users created");

            // 2. Branches
            var branch1 = new Branch
            {
                Id = Guid.NewGuid(),
                Name = "İstanbul Kadıköy",
                Address = "Kadıköy Meydanı No:1, Kadıköy",
                City = "İstanbul",
                Region = "Marmara",
                CreatedAt = DateTime.UtcNow
            };

            var branch2 = new Branch
            {
                Id = Guid.NewGuid(),
                Name = "Ankara Kızılay",
                Address = "Kızılay Meydanı No:5, Çankaya",
                City = "Ankara",
                Region = "İç Anadolu",
                CreatedAt = DateTime.UtcNow
            };

            var branch3 = new Branch
            {
                Id = Guid.NewGuid(),
                Name = "İzmir Alsancak",
                Address = "Alsancak Sahil No:10, Konak",
                City = "İzmir",
                Region = "Ege",
                CreatedAt = DateTime.UtcNow
            };

            context.Branches.AddRange(branch1, branch2, branch3);
            await context.SaveChangesAsync();
            logger.LogInformation("Branches created");

            // 3. Checklist
            var checklist = new Checklist
            {
                Id = Guid.NewGuid(),
                Name = "Restaurant Monthly Evaluation",
                Description = "Standart aylık restaurant değerlendirme formu",
                Version = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Checklists.Add(checklist);
            await context.SaveChangesAsync();
            logger.LogInformation("Checklist created");

            // 4. Sections
            var section1 = new Section
            {
                Id = Guid.NewGuid(),
                ChecklistId = checklist.Id,
                Name = "Temizlik ve Hijyen",
                Order = 1,
                CreatedAt = DateTime.UtcNow
            };

            var section2 = new Section
            {
                Id = Guid.NewGuid(),
                ChecklistId = checklist.Id,
                Name = "Hizmet Kalitesi",
                Order = 2,
                CreatedAt = DateTime.UtcNow
            };

            var section3 = new Section
            {
                Id = Guid.NewGuid(),
                ChecklistId = checklist.Id,
                Name = "Ürün Kalitesi",
                Order = 3,
                CreatedAt = DateTime.UtcNow
            };

            context.Sections.AddRange(section1, section2, section3);
            await context.SaveChangesAsync();
            logger.LogInformation("Sections created");

            // 5. Questions
            var questions = new List<Question>
            {
                // Temizlik ve Hijyen
                new Question
                {
                    Id = Guid.NewGuid(),
                    SectionId = section1.Id,
                    Text = "Masalar temiz mi?",
                    Type = QuestionType.Likert,
                    Points = 5,
                    AllowNA = false,
                    Order = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    Id = Guid.NewGuid(),
                    SectionId = section1.Id,
                    Text = "Tuvalet temizliği nasıl?",
                    Type = QuestionType.Star,
                    Points = 10,
                    AllowNA = false,
                    Order = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    Id = Guid.NewGuid(),
                    SectionId = section1.Id,
                    Text = "Genel temizlik hakkında ek gözlemler",
                    Type = QuestionType.Text,
                    Points = 0,
                    AllowNA = false,
                    Order = 3,
                    CreatedAt = DateTime.UtcNow
                },

                // Hizmet Kalitesi
                new Question
                {
                    Id = Guid.NewGuid(),
                    SectionId = section2.Id,
                    Text = "Karşılama nasıldı?",
                    Type = QuestionType.MultipleChoice,
                    Points = 5,
                    AllowNA = false,
                    OptionsJson = JsonSerializer.Serialize(new[] { "Mükemmel", "İyi", "Orta", "Kötü" }),
                    Order = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    Id = Guid.NewGuid(),
                    SectionId = section2.Id,
                    Text = "Sipariş alma süresi uygun muydu?",
                    Type = QuestionType.Likert,
                    Points = 5,
                    AllowNA = true,
                    Order = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    Id = Guid.NewGuid(),
                    SectionId = section2.Id,
                    Text = "Personel ilgisi nasıldı?",
                    Type = QuestionType.Star,
                    Points = 10,
                    AllowNA = false,
                    Order = 3,
                    CreatedAt = DateTime.UtcNow
                },

                // Ürün Kalitesi
                new Question
                {
                    Id = Guid.NewGuid(),
                    SectionId = section3.Id,
                    Text = "Yemek sıcaklığı uygun muydu?",
                    Type = QuestionType.Likert,
                    Points = 5,
                    AllowNA = false,
                    Order = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    Id = Guid.NewGuid(),
                    SectionId = section3.Id,
                    Text = "Yemek lezzeti nasıldı?",
                    Type = QuestionType.Star,
                    Points = 15,
                    AllowNA = false,
                    Order = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    Id = Guid.NewGuid(),
                    SectionId = section3.Id,
                    Text = "Porsiyon büyüklüğü nasıldı?",
                    Type = QuestionType.MultipleChoice,
                    Points = 5,
                    AllowNA = true,
                    OptionsJson = JsonSerializer.Serialize(new[] { "Çok Büyük", "Uygun", "Küçük", "Çok Küçük" }),
                    Order = 3,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Questions.AddRange(questions);
            await context.SaveChangesAsync();
            logger.LogInformation("Questions created");

            // 6. Project
            var project = new Project
            {
                Id = Guid.NewGuid(),
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

            // 7. Sample Assignment (Internal)
            var assignment1 = new Assignment
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                BranchId = branch1.Id,
                ChecklistId = checklist.Id,
                AssignedUserId = evaluator1.Id,
                DueDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            var assignment2 = new Assignment
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                BranchId = branch2.Id,
                ChecklistId = checklist.Id,
                AssignedUserId = evaluator2.Id,
                DueDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            // Sample Assignment (External)
            var assignment3 = new Assignment
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                BranchId = branch3.Id,
                ChecklistId = checklist.Id,
                UniqueLink = Guid.NewGuid().ToString("N"),
                DueDate = DateTime.UtcNow.AddDays(14),
                CreatedAt = DateTime.UtcNow
            };

            context.Assignments.AddRange(assignment1, assignment2, assignment3);
            await context.SaveChangesAsync();
            logger.LogInformation("Assignments created");

            // 8. Sample Customers
            var customer1 = new Customer
            {
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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

            // 9. Sample Customer Personnel
            var personnel1 = new CustomerPersonnel
            {
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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

            // 10. Sample Customer Task Lists
            var taskList1 = new CustomerTaskList
            {
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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

            // 11. Sample Customer Personnel Task Assignments
            var taskAssignment1 = new CustomerPersonnelTaskAssignment
            {
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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

            // 12. Link some branches and projects to customers
            branch1.CustomerId = customer1.Id;
            branch2.CustomerId = customer2.Id;
            project.CustomerId = customer1.Id;
            await context.SaveChangesAsync();
            logger.LogInformation("Linked branches and projects to customers");

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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
