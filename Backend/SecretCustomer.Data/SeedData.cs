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

            // 1b. Field Worker Users - Saha çalışanları için sistem kullanıcıları
            var fieldWorkerUser1 = new User
            {
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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
                Id = Guid.NewGuid(),
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

            // 12. Sample Field Workers (Saha Çalışanları)
            var fieldWorker1 = new FieldWorker
            {
                Id = Guid.NewGuid(),
                UserId = fieldWorkerUser1.Id,
                FirstName = "Ali",
                LastName = "Veli",
                Email = "ali.veli@sahacalisani.com",
                PhoneNumber = "0542 111 2233",
                Address = "Beşiktaş, İstanbul",
                Notes = "Deneyimli saha çalışanı, restaurant değerlendirmelerinde uzman",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorker2 = new FieldWorker
            {
                Id = Guid.NewGuid(),
                UserId = fieldWorkerUser2.Id,
                FirstName = "Zeynep",
                LastName = "Yıldız",
                Email = "zeynep.yildiz@sahacalisani.com",
                PhoneNumber = "0533 444 5566",
                Address = "Çankaya, Ankara",
                Notes = "Perakende ve hizmet sektöründe geniş deneyim, 5 yıllık tecrübe",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorker3 = new FieldWorker
            {
                Id = Guid.NewGuid(),
                UserId = fieldWorkerUser3.Id,
                FirstName = "Murat",
                LastName = "Koç",
                Email = "murat.koc@sahacalisani.com",
                PhoneNumber = "0544 777 8899",
                Address = "Karşıyaka, İzmir",
                Notes = "İzmir bölgesinde aktif çalışan, otel değerlendirmelerinde deneyimli",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorker4 = new FieldWorker
            {
                Id = Guid.NewGuid(),
                UserId = fieldWorkerUser4.Id,
                FirstName = "Elif",
                LastName = "Şahin",
                Email = "elif.sahin@sahacalisani.com",
                PhoneNumber = "0535 222 3344",
                Address = "Bornova, İzmir",
                Notes = "Part-time çalışan, hafta sonları uygun",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorker5 = new FieldWorker
            {
                Id = Guid.NewGuid(),
                UserId = fieldWorkerUser5.Id,
                FirstName = "Burak",
                LastName = "Tekin",
                Email = "burak.tekin@sahacalisani.com",
                PhoneNumber = "0536 999 0011",
                Address = "Keçiören, Ankara",
                Notes = "Yüksek lisans mezunu, detaylı analiz yapabiliyor, 6 yıllık deneyim",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorker6 = new FieldWorker
            {
                Id = Guid.NewGuid(),
                UserId = fieldWorkerUser6.Id,
                FirstName = "Selin",
                LastName = "Aydın",
                Email = "selin.aydin@sahacalisani.com",
                PhoneNumber = "0537 333 4455",
                Address = "Beşiktaş, İstanbul",
                Notes = "Restaurant ve cafe değerlendirmelerinde uzman",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorker7 = new FieldWorker
            {
                Id = Guid.NewGuid(),
                UserId = fieldWorkerUser7.Id,
                FirstName = "Emre",
                LastName = "Çelik",
                Email = "emre.celik@sahacalisani.com",
                PhoneNumber = "0538 666 7788",
                Address = "Kadıköy, İstanbul",
                Notes = "Hızlı ve güvenilir raporlama yapan çalışan",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var fieldWorker8 = new FieldWorker
            {
                Id = Guid.NewGuid(),
                UserId = fieldWorkerUser8.Id,
                FirstName = "Ayşe",
                LastName = "Kara",
                Email = "ayse.kara@sahacalisani.com",
                PhoneNumber = "0539 888 9900",
                Address = "Kızılay, Ankara",
                Notes = "Müşteri hizmetleri değerlendirmelerinde deneyimli",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            context.FieldWorkers.AddRange(fieldWorker1, fieldWorker2, fieldWorker3, fieldWorker4, 
                fieldWorker5, fieldWorker6, fieldWorker7, fieldWorker8);
            await context.SaveChangesAsync();
            logger.LogInformation("Field Workers created");

            // 13. Link some branches and projects to customers
            branch1.CustomerId = customer1.Id;
            branch2.CustomerId = customer2.Id;
            project.CustomerId = customer1.Id;
            await context.SaveChangesAsync();
            logger.LogInformation("Linked branches and projects to customers");

            // 14. Permissions - RBAC System
            await SeedPermissionsAsync(context, logger);

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
            new Permission { Id = Guid.NewGuid(), Code = "Users.View", DisplayName = "Kullanıcıları Görüntüle", Category = PermissionCategory.Users, SortOrder = 1, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Users.Create", DisplayName = "Kullanıcı Oluştur", Category = PermissionCategory.Users, SortOrder = 2, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Users.Edit", DisplayName = "Kullanıcı Düzenle", Category = PermissionCategory.Users, SortOrder = 3, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Users.Delete", DisplayName = "Kullanıcı Sil", Category = PermissionCategory.Users, SortOrder = 4, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Users.Manage", DisplayName = "Kullanıcı Yönetimi (Tam Yetki)", Category = PermissionCategory.Users, SortOrder = 5, CreatedAt = DateTime.UtcNow },

            // Projects
            new Permission { Id = Guid.NewGuid(), Code = "Projects.View", DisplayName = "Projeleri Görüntüle", Category = PermissionCategory.Projects, SortOrder = 10, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Projects.Create", DisplayName = "Proje Oluştur", Category = PermissionCategory.Projects, SortOrder = 11, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Projects.Edit", DisplayName = "Proje Düzenle", Category = PermissionCategory.Projects, SortOrder = 12, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Projects.Delete", DisplayName = "Proje Sil", Category = PermissionCategory.Projects, SortOrder = 13, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Projects.Manage", DisplayName = "Proje Yönetimi (Tam Yetki)", Category = PermissionCategory.Projects, SortOrder = 14, CreatedAt = DateTime.UtcNow },

            // Assignments
            new Permission { Id = Guid.NewGuid(), Code = "Assignments.View", DisplayName = "Atamaları Görüntüle", Category = PermissionCategory.Assignments, SortOrder = 20, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Assignments.Create", DisplayName = "Atama Oluştur", Category = PermissionCategory.Assignments, SortOrder = 21, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Assignments.Edit", DisplayName = "Atama Düzenle", Category = PermissionCategory.Assignments, SortOrder = 22, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Assignments.Delete", DisplayName = "Atama Sil", Category = PermissionCategory.Assignments, SortOrder = 23, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Assignments.Manage", DisplayName = "Atama Yönetimi (Tam Yetki)", Category = PermissionCategory.Assignments, SortOrder = 24, CreatedAt = DateTime.UtcNow },

            // Checklists
            new Permission { Id = Guid.NewGuid(), Code = "Checklists.View", DisplayName = "Kontrol Listelerini Görüntüle", Category = PermissionCategory.Checklists, SortOrder = 30, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Checklists.Create", DisplayName = "Kontrol Listesi Oluştur", Category = PermissionCategory.Checklists, SortOrder = 31, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Checklists.Edit", DisplayName = "Kontrol Listesi Düzenle", Category = PermissionCategory.Checklists, SortOrder = 32, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Checklists.Delete", DisplayName = "Kontrol Listesi Sil", Category = PermissionCategory.Checklists, SortOrder = 33, CreatedAt = DateTime.UtcNow },

            // Reports
            new Permission { Id = Guid.NewGuid(), Code = "Reports.View", DisplayName = "Raporları Görüntüle", Category = PermissionCategory.Reports, SortOrder = 40, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Reports.Export", DisplayName = "Rapor Dışa Aktar", Category = PermissionCategory.Reports, SortOrder = 41, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Reports.Create", DisplayName = "Rapor Oluştur", Category = PermissionCategory.Reports, SortOrder = 42, CreatedAt = DateTime.UtcNow },

            // Dashboard
            new Permission { Id = Guid.NewGuid(), Code = "Dashboard.View", DisplayName = "Dashboard Görüntüle", Category = PermissionCategory.Dashboard, SortOrder = 50, CreatedAt = DateTime.UtcNow },

            // Permissions Management
            new Permission { Id = Guid.NewGuid(), Code = "Permissions.View", DisplayName = "Yetkileri Görüntüle", Category = PermissionCategory.Settings, SortOrder = 60, CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Code = "Permissions.Manage", DisplayName = "Yetki Yönetimi", Category = PermissionCategory.Settings, SortOrder = 61, CreatedAt = DateTime.UtcNow },
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
                Id = Guid.NewGuid(),
                Role = adminRole,
                PermissionId = permission.Id,
                IsGranted = true,
                Scope = PermissionScope.All,
                CreatedAt = DateTime.UtcNow
            });
        }

        // TeamLeader permissions
        var teamLeaderPermissions = permissions.Where(p =>
            p.Code.StartsWith("Projects.") ||
            p.Code.StartsWith("Assignments.") ||
            p.Code.StartsWith("Checklists.View") ||
            p.Code.StartsWith("Reports.") ||
            p.Code.StartsWith("Dashboard.")).ToList();

        foreach (var permission in teamLeaderPermissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                Role = UserRole.TeamLeader,
                PermissionId = permission.Id,
                IsGranted = true,
                Scope = PermissionScope.Branch,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Evaluator permissions
        var evaluatorPermissions = permissions.Where(p =>
            p.Code == "Checklists.View" ||
            p.Code == "Assignments.View" ||
            p.Code == "Dashboard.View").ToList();

        foreach (var permission in evaluatorPermissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                Role = UserRole.Evaluator,
                PermissionId = permission.Id,
                IsGranted = true,
                Scope = PermissionScope.Own,
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
                Id = Guid.NewGuid(),
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
}
