using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.FieldWorker;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class FieldWorkerService : IFieldWorkerService
{
    private readonly ApplicationDbContext _context;

    public FieldWorkerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FieldWorkerDashboardDto> GetDashboardAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Kullanıcı bulunamadı");

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);

        // Atanmış projeler
        var assignedProjects = await GetAssignedProjectsAsync(userId);

        // Toplam ziyaret sayısı (FieldWorker'ın yaptığı - VisitId olanlar)
        var totalVisits = await _context.Evaluations
            .Where(e => e.CreatedBy == user.Username && e.VisitId != null)
            .CountAsync();

        // Bugünkü ziyaretler
        var todayVisits = await _context.Evaluations
            .Where(e => e.CreatedBy == user.Username && e.VisitId != null && e.CreatedAt >= todayStart)
            .CountAsync();

        // Bu haftaki ziyaretler
        var thisWeekVisits = await _context.Evaluations
            .Where(e => e.CreatedBy == user.Username && e.VisitId != null && e.CreatedAt >= weekStart)
            .CountAsync();

        // Son 10 ziyaret
        var recentVisits = await _context.Evaluations
            .Include(e => e.CustomerDealer)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
                    .ThenInclude(p => p.Customer)
            .Where(e => e.CreatedBy == user.Username && e.VisitId != null)
            .OrderByDescending(e => e.CreatedAt)
            .Take(10)
            .Select(e => new VisitSummaryDto
            {
                EvaluationId = e.Id,
                VisitId = e.VisitId,
                CustomerDealerId = e.CustomerDealerId,
                CustomerDealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : null,
                CustomerDealerCity = e.CustomerDealer != null ? e.CustomerDealer.City : null,
                CustomerName = e.Assignment.Project.Customer.CompanyName,
                ProjectName = e.Assignment.Project.Name,
                ScorePercentage = e.ScorePercentage,
                StatusId = e.StatusId,
                CreatedAt = e.CreatedAt,
                ControlDate = e.ControlDate
            })
            .ToListAsync();

        return new FieldWorkerDashboardDto
        {
            UserId = userId,
            UserName = $"{user.FirstName} {user.LastName}",
            AssignedProjects = assignedProjects,
            TotalVisits = totalVisits,
            TodayVisits = todayVisits,
            ThisWeekVisits = thisWeekVisits,
            RecentVisits = recentVisits
        };
    }

    public async Task<List<AssignedProjectDto>> GetAssignedProjectsAsync(int userId)
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Customer)
            .Include(a => a.Checklist)
            .Where(a => a.AssignedUserId == userId && !a.IsCompleted && !a.IsDeleted)
            .ToListAsync();

        var projectIds = assignments.Select(a => a.ProjectId).Distinct().ToList();

        // Projelerin müşteri ID'lerini topla
        var customerIds = assignments
            .Where(a => a.Project?.CustomerId != null)
            .Select(a => a.Project.CustomerId!.Value)
            .Distinct()
            .ToList();

        // Her müşteri için bayi sayısı
        var dealerCounts = await _context.CustomerDealers
            .Where(d => customerIds.Contains(d.CustomerId))
            .GroupBy(d => d.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToListAsync();

        // Her proje için tamamlanan ziyaret sayısı
        var visitCounts = await _context.Evaluations
            .Where(e => projectIds.Contains(e.Assignment.ProjectId) && e.VisitId != null)
            .GroupBy(e => e.Assignment.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = new List<AssignedProjectDto>();
        foreach (var projectId in projectIds)
        {
            var assignment = assignments.First(a => a.ProjectId == projectId);
            var project = assignment.Project;
            var dealerCount = dealerCounts.FirstOrDefault(d => d.CustomerId == project.CustomerId)?.Count ?? 0;
            var completedVisits = visitCounts.FirstOrDefault(v => v.ProjectId == projectId)?.Count ?? 0;
            var totalAssignments = assignments.Count(a => a.ProjectId == projectId);

            result.Add(new AssignedProjectDto
            {
                ProjectId = projectId,
                ProjectName = project.Name,
                CustomerId = project.CustomerId ?? 0,
                CustomerName = project.Customer?.CompanyName ?? "",
                CustomerDealerCount = dealerCount,
                CompletedVisits = completedVisits,
                TotalAssignments = totalAssignments,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                AssignmentId = assignment.Id,
                ChecklistId = assignment.ChecklistId,
                ChecklistName = assignment.Checklist?.Name ?? ""
            });
        }

        return result;
    }

    public async Task<List<FieldWorkerDealerDto>> GetAccessibleDealersAsync(int userId, int? projectId = null)
    {
        // FieldWorker'ın atandığı projelerin müşterilerinin bayileri
        var assignedCustomerIds = await _context.Assignments
            .Include(a => a.Project)
            .Where(a => a.AssignedUserId == userId && !a.IsCompleted)
            .Where(a => projectId == null || a.ProjectId == projectId)
            .Select(a => a.Project.CustomerId)
            .Distinct()
            .ToListAsync();

        var dealers = await _context.CustomerDealers
            .Include(d => d.Customer)
            .Where(d => assignedCustomerIds.Contains(d.CustomerId) && d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync();

        // Her bayi için ziyaret istatistikleri
        var dealerIds = dealers.Select(d => d.Id).ToList();
        var visitStats = await _context.Evaluations
            .Where(e => e.CustomerDealerId != null && dealerIds.Contains(e.CustomerDealerId.Value) && e.VisitId != null)
            .GroupBy(e => e.CustomerDealerId)
            .Select(g => new
            {
                DealerId = g.Key,
                Count = g.Count(),
                LastDate = g.Max(e => e.CreatedAt),
                LastScore = g.OrderByDescending(e => e.CreatedAt).Select(e => e.ScorePercentage).FirstOrDefault()
            })
            .ToListAsync();

        return dealers.Select(d =>
        {
            var stats = visitStats.FirstOrDefault(s => s.DealerId == d.Id);
            return new FieldWorkerDealerDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Address = d.Address,
                City = d.City,
                District = d.District,
                Latitude = d.Latitude,
                Longitude = d.Longitude,
                Phone = d.Phone,
                ContactPerson = d.ContactPerson,
                DealerTypeId = d.DealerTypeId,
                CustomerId = d.CustomerId,
                CustomerName = d.Customer?.CompanyName ?? "",
                VisitCount = stats?.Count ?? 0,
                LastVisitDate = stats?.LastDate,
                LastVisitScore = stats?.LastScore
            };
        }).ToList();
    }

    public async Task<List<FieldWorkerDealerDto>> GetDealersForAssignmentAsync(int userId, int assignmentId)
    {
        // Atamayı bul
        var assignment = await _context.Assignments
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.AssignedUserId == userId && !a.IsDeleted);

        if (assignment == null)
            return new List<FieldWorkerDealerDto>();

        // Bu atama için zaten ziyaret edilmiş bayileri bul
        var visitedDealerIds = await _context.Evaluations
            .Where(e => e.AssignmentId == assignmentId && e.CustomerDealerId != null && !e.IsDeleted)
            .Select(e => e.CustomerDealerId!.Value)
            .Distinct()
            .ToListAsync();

        // Projenin müşterisinin bayilerini getir (ziyaret edilmemişler)
        var dealers = await _context.CustomerDealers
            .Include(d => d.Customer)
            .Where(d => d.CustomerId == assignment.Project.CustomerId && d.IsActive)
            .Where(d => !visitedDealerIds.Contains(d.Id))
            .OrderBy(d => d.Name)
            .ToListAsync();

        return dealers.Select(d => new FieldWorkerDealerDto
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name,
            Address = d.Address,
            City = d.City,
            District = d.District,
            Latitude = d.Latitude,
            Longitude = d.Longitude,
            Phone = d.Phone,
            ContactPerson = d.ContactPerson,
            DealerTypeId = d.DealerTypeId,
            CustomerId = d.CustomerId,
            CustomerName = d.Customer?.CompanyName ?? "",
            VisitCount = 0,
            LastVisitDate = null,
            LastVisitScore = null
        }).ToList();
    }

    public async Task<List<PendingDealerDto>> GetPendingDealersAsync(int userId)
    {
        // Kullanıcının aktif atamalarını al
        var assignments = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Customer)
            .Include(a => a.Checklist)
            .Where(a => a.AssignedUserId == userId && !a.IsDeleted && !a.IsCompleted)
            .ToListAsync();

        if (!assignments.Any())
            return new List<PendingDealerDto>();

        var result = new List<PendingDealerDto>();

        foreach (var assignment in assignments)
        {
            // Bu atama için ziyaret edilmiş bayi ID'leri
            var visitedDealerIds = await _context.Evaluations
                .Where(e => e.AssignmentId == assignment.Id && e.CustomerDealerId != null && !e.IsDeleted)
                .Select(e => e.CustomerDealerId!.Value)
                .Distinct()
                .ToListAsync();

            // Projenin müşterisinin ziyaret edilmemiş bayileri
            var pendingDealers = await _context.CustomerDealers
                .Where(d => d.CustomerId == assignment.Project.CustomerId && d.IsActive)
                .Where(d => !visitedDealerIds.Contains(d.Id))
                .OrderBy(d => d.City)
                .ThenBy(d => d.Name)
                .ToListAsync();

            foreach (var dealer in pendingDealers)
            {
                result.Add(new PendingDealerDto
                {
                    DealerId = dealer.Id,
                    DealerCode = dealer.Code,
                    DealerName = dealer.Name,
                    Address = dealer.Address,
                    City = dealer.City,
                    District = dealer.District,
                    Phone = dealer.Phone,
                    ContactPerson = dealer.ContactPerson,
                    Latitude = dealer.Latitude,
                    Longitude = dealer.Longitude,
                    AssignmentId = assignment.Id,
                    ProjectId = assignment.ProjectId,
                    ProjectName = assignment.Project?.Name ?? "",
                    CustomerName = assignment.Project?.Customer?.CompanyName ?? "",
                    ChecklistId = assignment.ChecklistId,
                    ChecklistName = assignment.Checklist?.Name ?? "",
                    ProjectStartDate = assignment.Project?.StartDate,
                    ProjectEndDate = assignment.Project?.EndDate
                });
            }
        }

        return result;
    }

    public async Task<PagedVisitResult> GetVisitsAsync(int userId, VisitFilterDto filter)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Kullanıcı bulunamadı");

        var query = _context.Evaluations
            .Include(e => e.CustomerDealer)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
                    .ThenInclude(p => p.Customer)
            .Where(e => e.CreatedBy == user.Username && e.VisitId != null);

        // Filters
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.CustomerDealerId.HasValue)
            query = query.Where(e => e.CustomerDealerId == filter.CustomerDealerId.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(e => e.Assignment.Project.CustomerId == filter.CustomerId.Value);

        if (filter.StatusId.HasValue)
            query = query.Where(e => e.StatusId == filter.StatusId.Value);

        // DateRanges pattern
        if (filter.DateRanges?.Any() == true)
        {
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(e => e.CreatedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(e => e.CreatedAt <= maxEnd);
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(e =>
                (e.VisitId != null && e.VisitId.ToLower().Contains(term)) ||
                (e.CustomerDealer != null && e.CustomerDealer.Name.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(e => new VisitSummaryDto
            {
                EvaluationId = e.Id,
                VisitId = e.VisitId,
                CustomerDealerId = e.CustomerDealerId,
                CustomerDealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : null,
                CustomerDealerCity = e.CustomerDealer != null ? e.CustomerDealer.City : null,
                CustomerName = e.Assignment.Project.Customer.CompanyName,
                ProjectName = e.Assignment.Project.Name,
                ScorePercentage = e.ScorePercentage,
                StatusId = e.StatusId,
                CreatedAt = e.CreatedAt,
                ControlDate = e.ControlDate
            })
            .ToListAsync();

        return new PagedVisitResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<VisitSummaryDto?> GetVisitByIdAsync(int userId, int evaluationId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

        return await _context.Evaluations
            .Include(e => e.CustomerDealer)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
                    .ThenInclude(p => p.Customer)
            .Where(e => e.Id == evaluationId && e.CreatedBy == user.Username)
            .Select(e => new VisitSummaryDto
            {
                EvaluationId = e.Id,
                VisitId = e.VisitId,
                CustomerDealerId = e.CustomerDealerId,
                CustomerDealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : null,
                CustomerDealerCity = e.CustomerDealer != null ? e.CustomerDealer.City : null,
                CustomerName = e.Assignment.Project.Customer.CompanyName,
                ProjectName = e.Assignment.Project.Name,
                ScorePercentage = e.ScorePercentage,
                StatusId = e.StatusId,
                CreatedAt = e.CreatedAt,
                ControlDate = e.ControlDate
            })
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateVisitAsync(int userId, CreateVisitDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Kullanıcı bulunamadı");

        // Atama kontrolü
        var assignment = await _context.Assignments
            .Include(a => a.Checklist)
                .ThenInclude(c => c.Questions.Where(q => !q.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId &&
                                      a.AssignedUserId == userId &&
                                      !a.IsDeleted);

        if (assignment == null)
            throw new InvalidOperationException("Bu atama için yetkiniz bulunmamaktadır.");

        // Mevcut evaluation var mı kontrol et (güncelleme)
        Evaluation? evaluation = null;
        if (dto.EvaluationId.HasValue && dto.EvaluationId.Value > 0)
        {
            evaluation = await _context.Evaluations
                .Include(e => e.Answers)
                .FirstOrDefaultAsync(e => e.Id == dto.EvaluationId.Value && !e.IsDeleted);
        }

        // Yeni evaluation oluştur veya mevcut olanı güncelle
        if (evaluation == null)
        {
            var visitId = await GenerateVisitIdAsync();
            evaluation = new Evaluation
            {
                AssignmentId = dto.AssignmentId,
                VisitId = visitId,
                EvaluatorId = userId,
                StatusId = EvaluationStatuses.Ids.InProgress,
                StartedAt = DateTime.UtcNow,
                CreatedBy = user.Username
            };
            _context.Evaluations.Add(evaluation);
        }

        // Checklist sorularını al
        var allQuestions = assignment.Checklist.Questions.Where(q => !q.IsDeleted).ToList();

        // Puan hesapla (Maximum scoring)
        var scoreResult = CalculateVisitScore(allQuestions, dto.Answers);

        // Mevcut cevapları temizle
        if (evaluation.Answers.Any())
        {
            _context.Answers.RemoveRange(evaluation.Answers);
        }

        // Cevapları ekle
        foreach (var answerDto in dto.Answers)
        {
            var question = allQuestions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
            if (question == null) continue;

            var answer = new Answer
            {
                EvaluationId = evaluation.Id,
                QuestionId = answerDto.QuestionId,
                AnswerText = answerDto.TextAnswer,
                AnswerNumeric = answerDto.NumericAnswer.HasValue ? (int?)answerDto.NumericAnswer.Value : null,
                GivenPoints = answerDto.GivenPoints,
                Notes = answerDto.Notes,
                RecommendationNotes = answerDto.RecommendationNotes,
                EarnedPoints = CalculateEarnedPoints(question, answerDto)
            };

            // Ceza uygulama
            if (answerDto.ApplyPenalty && !string.IsNullOrEmpty(answerDto.SelectedPenaltyType))
            {
                answer.IsPenaltyApplied = true;
                answer.AppliedPenaltyTypeId = PenaltyTypes.GetBySystemName(answerDto.SelectedPenaltyType)?.Id
                    ?? PenaltyTypes.Ids.None;
            }

            // Alt kriter seçimleri
            if (answerDto.SelectedSubCriteriaIds?.Any() == true)
            {
                foreach (var subCriteriaId in answerDto.SelectedSubCriteriaIds)
                {
                    answer.SubCriteriaSelections.Add(new AnswerSubCriteriaSelection
                    {
                        SubCriteriaId = subCriteriaId,
                        SelectedAt = DateTime.UtcNow
                    });
                }
            }

            evaluation.Answers.Add(answer);
        }

        // Evaluation alanlarını güncelle
        evaluation.CustomerDealerId = dto.CustomerDealerId;
        evaluation.StatusId = dto.IsDraft ? EvaluationStatuses.Ids.Draft : EvaluationStatuses.Ids.Completed;
        evaluation.ControlDate = dto.ControlDate.HasValue
            ? DateTime.SpecifyKind(dto.ControlDate.Value, DateTimeKind.Utc)
            : null;
        evaluation.ControlTime = dto.ControlTime;
        evaluation.EvaluationComment = dto.EvaluationComment;
        evaluation.DescriptionsJson = dto.Descriptions?.Any() == true
            ? System.Text.Json.JsonSerializer.Serialize(dto.Descriptions.Where(d => !string.IsNullOrWhiteSpace(d)))
            : null;
        evaluation.TotalScore = scoreResult.TotalEarned;
        evaluation.MaxScore = scoreResult.MaxPossible;
        evaluation.ScorePercentage = scoreResult.Percentage;
        evaluation.YellowCardCount = scoreResult.YellowCardCount;
        evaluation.RedCardCount = scoreResult.RedCardCount;
        evaluation.UpdatedAt = DateTime.UtcNow;

        if (!dto.IsDraft)
        {
            evaluation.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return evaluation.Id;
    }

    /// <summary>
    /// FieldWorker ziyareti için puan hesaplama (Maximum scoring)
    /// </summary>
    private VisitScoreResult CalculateVisitScore(List<Question> questions, List<VisitAnswerDto> answers)
    {
        decimal totalWeight = 0;
        decimal totalEarned = 0;
        int yellowCardCount = 0;
        int redCardCount = 0;

        foreach (var question in questions)
        {
            var answerDto = answers.FirstOrDefault(a => a.QuestionId == question.Id);

            // Dahil edilmemiş sorular atla
            if (answerDto != null && !answerDto.IsIncluded && !question.IsRequired)
                continue;

            // Puanlı sorular
            if (question.ScoringTypeId == ScoringTypes.Ids.Scored)
            {
                totalWeight += question.WeightPoints;

                if (answerDto != null)
                {
                    var earned = CalculateEarnedPoints(question, answerDto);
                    totalEarned += earned;
                }
            }
            // Cezalı sorular
            else if (question.ScoringTypeId == ScoringTypes.Ids.Penalty)
            {
                if (answerDto != null && answerDto.ApplyPenalty)
                {
                    var penaltyType = answerDto.SelectedPenaltyType;
                    if (penaltyType == "YellowCard")
                    {
                        yellowCardCount++;
                        totalEarned -= question.WeightPoints;
                    }
                    else if (penaltyType == "RedCard")
                    {
                        redCardCount++;
                        totalEarned -= question.WeightPoints;
                    }
                }
            }
        }

        var percentage = totalWeight > 0 ? (totalEarned / totalWeight) * 100 : 0;
        if (percentage < 0) percentage = 0;
        if (percentage > 100) percentage = 100;

        return new VisitScoreResult
        {
            TotalEarned = totalEarned,
            MaxPossible = totalWeight,
            Percentage = percentage,
            YellowCardCount = yellowCardCount,
            RedCardCount = redCardCount
        };
    }

    /// <summary>
    /// Tek soru için kazanılan puan hesapla
    /// </summary>
    private decimal CalculateEarnedPoints(Question question, VisitAnswerDto answerDto)
    {
        if (question.ScoringTypeId != ScoringTypes.Ids.Scored)
            return 0;

        // GivenPoints varsa onu kullan (manuel puan girişi)
        if (answerDto.GivenPoints.HasValue)
        {
            var ratio = question.MaxPoints > 0 ? answerDto.GivenPoints.Value / question.MaxPoints : 0;
            return question.WeightPoints * ratio;
        }

        // NumericAnswer'dan hesapla
        if (answerDto.NumericAnswer.HasValue)
        {
            var ratio = question.MaxPoints > 0 ? answerDto.NumericAnswer.Value / question.MaxPoints : 0;
            return question.WeightPoints * ratio;
        }

        return 0;
    }

    /// <summary>
    /// Ziyaret puan hesaplama sonucu
    /// </summary>
    private class VisitScoreResult
    {
        public decimal TotalEarned { get; set; }
        public decimal MaxPossible { get; set; }
        public decimal Percentage { get; set; }
        public int YellowCardCount { get; set; }
        public int RedCardCount { get; set; }
    }

    public async Task<bool> UpdateVisitAsync(int userId, int evaluationId, CreateVisitDto dto)
    {
        // CreateVisitAsync zaten güncelleme işlemini de yapıyor (EvaluationId varsa)
        // Bu metodu geriye dönük uyumluluk için tutuyoruz
        dto.EvaluationId = evaluationId;
        var result = await CreateVisitAsync(userId, dto);
        return result > 0;
    }

    public async Task<string> GenerateVisitIdAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"ZYR-{year}-";

        // Bu yılki son ziyaret ID'sini bul
        var lastVisit = await _context.Evaluations
            .Where(e => e.VisitId != null && e.VisitId.StartsWith(prefix))
            .OrderByDescending(e => e.VisitId)
            .Select(e => e.VisitId)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(lastVisit))
        {
            var numberPart = lastVisit.Replace(prefix, "");
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D5}";
    }
}
