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

        // Bekleyen talepler
        var pendingRequests = await _context.DealerRequests
            .Where(r => r.RequestedByUserId == userId && r.StatusId == RequestStatuses.Ids.Pending)
            .CountAsync();

        // Son 10 ziyaret
        var recentVisits = await _context.Evaluations
            .Include(e => e.Dealer)
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
                DealerId = e.DealerId,
                DealerName = e.Dealer != null ? e.Dealer.Name : null,
                DealerCity = e.Dealer != null ? e.Dealer.City : null,
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
            PendingRequests = pendingRequests,
            RecentVisits = recentVisits
        };
    }

    public async Task<List<AssignedProjectDto>> GetAssignedProjectsAsync(int userId)
    {
        var assignments = await _context.Assignments
            .Include(a => a.Project)
                .ThenInclude(p => p.Customer)
            .Where(a => a.AssignedUserId == userId && !a.IsCompleted)
            .ToListAsync();

        var projectIds = assignments.Select(a => a.ProjectId).Distinct().ToList();

        // Her proje için bayi sayısı
        var dealerCounts = await _context.Dealers
            .Where(d => projectIds.Select(pid =>
                _context.Projects.Where(p => p.Id == pid).Select(p => p.CustomerId).FirstOrDefault()
            ).Contains(d.CustomerId))
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
                DealerCount = dealerCount,
                CompletedVisits = completedVisits,
                TotalAssignments = totalAssignments,
                StartDate = project.StartDate,
                EndDate = project.EndDate
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

        var dealers = await _context.Dealers
            .Include(d => d.Customer)
            .Where(d => assignedCustomerIds.Contains(d.CustomerId) && d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync();

        // Her bayi için ziyaret istatistikleri
        var dealerIds = dealers.Select(d => d.Id).ToList();
        var visitStats = await _context.Evaluations
            .Where(e => e.DealerId != null && dealerIds.Contains(e.DealerId.Value) && e.VisitId != null)
            .GroupBy(e => e.DealerId)
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

    public async Task<PagedVisitResult> GetVisitsAsync(int userId, VisitFilterDto filter)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Kullanıcı bulunamadı");

        var query = _context.Evaluations
            .Include(e => e.Dealer)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
                    .ThenInclude(p => p.Customer)
            .Where(e => e.CreatedBy == user.Username && e.VisitId != null);

        // Filters
        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.DealerId.HasValue)
            query = query.Where(e => e.DealerId == filter.DealerId.Value);

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
                (e.Dealer != null && e.Dealer.Name.ToLower().Contains(term)));
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
                DealerId = e.DealerId,
                DealerName = e.Dealer != null ? e.Dealer.Name : null,
                DealerCity = e.Dealer != null ? e.Dealer.City : null,
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
            .Include(e => e.Dealer)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
                    .ThenInclude(p => p.Customer)
            .Where(e => e.Id == evaluationId && e.CreatedBy == user.Username)
            .Select(e => new VisitSummaryDto
            {
                EvaluationId = e.Id,
                VisitId = e.VisitId,
                DealerId = e.DealerId,
                DealerName = e.Dealer != null ? e.Dealer.Name : null,
                DealerCity = e.Dealer != null ? e.Dealer.City : null,
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

        // Atama kontrolü - kullanıcının bu projeye atanmış olması gerekir
        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(a => a.ProjectId == dto.ProjectId &&
                                      a.ChecklistId == dto.ChecklistId &&
                                      a.AssignedUserId == userId);

        if (assignment == null)
            throw new InvalidOperationException("Bu proje için atamanız bulunmamaktadır.");

        // VisitId üret
        var visitId = await GenerateVisitIdAsync();

        var evaluation = new Evaluation
        {
            AssignmentId = assignment.Id,
            VisitId = visitId,
            DealerId = dto.DealerId,
            StatusId = dto.IsDraft ? EvaluationStatuses.Ids.Draft : EvaluationStatuses.Ids.Completed,
            ControlDate = dto.ControlDate,
            ControlTime = dto.ControlTime,
            EvaluationComment = dto.AuditorComment,
            CreatedBy = user.Username
        };

        _context.Evaluations.Add(evaluation);
        await _context.SaveChangesAsync();

        // Cevapları ekle (basit format - detaylı form Evaluations sayfasından doldurulacak)
        if (dto.Answers.Any())
        {
            foreach (var answerDto in dto.Answers)
            {
                var answer = new Answer
                {
                    EvaluationId = evaluation.Id,
                    QuestionId = answerDto.QuestionId,
                    AnswerText = answerDto.TextAnswer,
                    AnswerNumeric = answerDto.NumericAnswer.HasValue ? (int?)answerDto.NumericAnswer.Value : null,
                    Notes = answerDto.Notes
                };

                _context.Answers.Add(answer);
            }
            await _context.SaveChangesAsync();
        }

        return evaluation.Id;
    }

    public async Task<bool> UpdateVisitAsync(int userId, int evaluationId, CreateVisitDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        var evaluation = await _context.Evaluations
            .Include(e => e.Answers)
            .FirstOrDefaultAsync(e => e.Id == evaluationId && e.CreatedBy == user.Username);

        if (evaluation == null)
            return false;

        // Sadece taslak durumundakiler güncellenebilir
        if (evaluation.StatusId != EvaluationStatuses.Ids.Draft)
            throw new InvalidOperationException("Sadece taslak durumundaki ziyaretler güncellenebilir.");

        evaluation.DealerId = dto.DealerId;
        evaluation.StatusId = dto.IsDraft ? EvaluationStatuses.Ids.Draft : EvaluationStatuses.Ids.Completed;
        evaluation.ControlDate = dto.ControlDate;
        evaluation.ControlTime = dto.ControlTime;
        evaluation.EvaluationComment = dto.AuditorComment;

        // Mevcut cevapları sil ve yenilerini ekle
        _context.Answers.RemoveRange(evaluation.Answers);

        foreach (var answerDto in dto.Answers)
        {
            var answer = new Answer
            {
                EvaluationId = evaluation.Id,
                QuestionId = answerDto.QuestionId,
                AnswerText = answerDto.TextAnswer,
                AnswerNumeric = answerDto.NumericAnswer.HasValue ? (int?)answerDto.NumericAnswer.Value : null,
                Notes = answerDto.Notes
            };

            _context.Answers.Add(answer);
        }

        await _context.SaveChangesAsync();
        return true;
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
