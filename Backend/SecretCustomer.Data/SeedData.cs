using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;

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
                    QuestionType = QuestionType.YesNo,
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
                    QuestionType = QuestionType.Rating,
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
                    QuestionType = QuestionType.Text,
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
                    QuestionType = QuestionType.MultipleChoice,
                    Points = 5,
                    AllowNA = false,
                    Options = "Mükemmel,İyi,Orta,Kötü",
                    Order = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    Id = Guid.NewGuid(),
                    SectionId = section2.Id,
                    Text = "Sipariş alma süresi uygun muydu?",
                    QuestionType = QuestionType.YesNo,
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
                    QuestionType = QuestionType.Rating,
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
                    QuestionType = QuestionType.YesNo,
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
                    QuestionType = QuestionType.Rating,
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
                    QuestionType = QuestionType.MultipleChoice,
                    Points = 5,
                    AllowNA = true,
                    Options = "Çok Büyük,Uygun,Küçük,Çok Küçük",
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
                EvaluatorId = evaluator1.Id,
                AssignmentType = AssignmentType.Internal,
                Status = EvaluationStatus.Pending,
                Deadline = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            var assignment2 = new Assignment
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                BranchId = branch2.Id,
                ChecklistId = checklist.Id,
                EvaluatorId = evaluator2.Id,
                AssignmentType = AssignmentType.Internal,
                Status = EvaluationStatus.Pending,
                Deadline = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            // Sample Assignment (External)
            var assignment3 = new Assignment
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                BranchId = branch3.Id,
                ChecklistId = checklist.Id,
                AssignmentType = AssignmentType.External,
                Status = EvaluationStatus.Pending,
                UniqueLink = Guid.NewGuid().ToString("N"),
                Deadline = DateTime.UtcNow.AddDays(14),
                CreatedAt = DateTime.UtcNow
            };

            context.Assignments.AddRange(assignment1, assignment2, assignment3);
            await context.SaveChangesAsync();
            logger.LogInformation("Assignments created");

            logger.LogInformation("Database seed completed successfully!");
            logger.LogInformation("Test users:");
            logger.LogInformation("  Admin: admin / Admin@123");
            logger.LogInformation("  TeamLeader: teamleader / Leader@123");
            logger.LogInformation("  Evaluator1: evaluator1 / Eval@123");
            logger.LogInformation("  Evaluator2: evaluator2 / Eval@123");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
