using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.BankVisit;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

/// <summary>
/// Banka Gizli Müşteri Ziyareti Servisi (GBF - Gizli Banka Formu)
/// </summary>
public class BankVisitService : IBankVisitService
{
    private readonly ApplicationDbContext _context;

    public BankVisitService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BankVisitDetailsDto?> GetByIdAsync(Guid id)
    {
        var bankVisit = await _context.BankVisitDetails
            .Include(b => b.CustomerVisit)
                .ThenInclude(cv => cv.Customer)
            .Include(b => b.CustomerVisit)
                .ThenInclude(cv => cv.Branch)
            .Include(b => b.CustomerVisit)
                .ThenInclude(cv => cv.VisitorUser)
            .FirstOrDefaultAsync(b => b.Id == id);

        return bankVisit == null ? null : MapToDto(bankVisit);
    }

    public async Task<BankVisitDetailsDto?> GetByCustomerVisitIdAsync(Guid customerVisitId)
    {
        var bankVisit = await _context.BankVisitDetails
            .Include(b => b.CustomerVisit)
                .ThenInclude(cv => cv.Customer)
            .Include(b => b.CustomerVisit)
                .ThenInclude(cv => cv.Branch)
            .Include(b => b.CustomerVisit)
                .ThenInclude(cv => cv.VisitorUser)
            .FirstOrDefaultAsync(b => b.CustomerVisitId == customerVisitId);

        return bankVisit == null ? null : MapToDto(bankVisit);
    }

    public async Task<IEnumerable<BankVisitSummaryDto>> GetAllAsync()
    {
        var bankVisits = await GetBankVisitsQuery()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return bankVisits.Select(MapToSummaryDto);
    }

    public async Task<IEnumerable<BankVisitSummaryDto>> GetFilteredAsync(BankVisitFilterDto filter)
    {
        var query = GetBankVisitsQuery();

        if (filter.CustomerId.HasValue)
            query = query.Where(b => b.CustomerVisit.CustomerId == filter.CustomerId);

        if (filter.BranchId.HasValue)
            query = query.Where(b => b.CustomerVisit.BranchId == filter.BranchId);

        if (filter.VisitorUserId.HasValue)
            query = query.Where(b => b.CustomerVisit.VisitorUserId == filter.VisitorUserId);

        if (filter.Scenario.HasValue)
            query = query.Where(b => b.Scenario == filter.Scenario);

        if (filter.ScenarioCompleted.HasValue)
            query = query.Where(b => b.ScenarioCompleted == filter.ScenarioCompleted);

        if (filter.Status.HasValue)
            query = query.Where(b => b.CustomerVisit.Status == filter.Status);

        if (filter.FromDate.HasValue)
            query = query.Where(b => b.CustomerVisit.PlannedDate >= filter.FromDate);

        if (filter.ToDate.HasValue)
            query = query.Where(b => b.CustomerVisit.PlannedDate <= filter.ToDate);

        if (filter.MinSatisfactionRating.HasValue)
            query = query.Where(b => b.OverallSatisfactionRating >= filter.MinSatisfactionRating);

        if (filter.MaxSatisfactionRating.HasValue)
            query = query.Where(b => b.OverallSatisfactionRating <= filter.MaxSatisfactionRating);

        var bankVisits = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return bankVisits.Select(MapToSummaryDto);
    }

    public async Task<IEnumerable<BankVisitSummaryDto>> GetByCustomerIdAsync(Guid customerId)
    {
        var bankVisits = await GetBankVisitsQuery()
            .Where(b => b.CustomerVisit.CustomerId == customerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return bankVisits.Select(MapToSummaryDto);
    }

    public async Task<IEnumerable<BankVisitSummaryDto>> GetByBranchIdAsync(Guid branchId)
    {
        var bankVisits = await GetBankVisitsQuery()
            .Where(b => b.CustomerVisit.BranchId == branchId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return bankVisits.Select(MapToSummaryDto);
    }

    public async Task<BankVisitDetailsDto> CreateAsync(CreateBankVisitDetailsDto dto)
    {
        // Verify CustomerVisit exists
        var customerVisit = await _context.CustomerVisits.FindAsync(dto.CustomerVisitId);
        if (customerVisit == null)
        {
            throw new KeyNotFoundException($"Müşteri ziyareti bulunamadı (ID: {dto.CustomerVisitId})");
        }

        // Check if bank visit details already exist for this customer visit
        var existingDetails = await _context.BankVisitDetails
            .FirstOrDefaultAsync(b => b.CustomerVisitId == dto.CustomerVisitId);
        if (existingDetails != null)
        {
            throw new InvalidOperationException("Bu ziyaret için banka detayları zaten mevcut");
        }

        var bankVisit = new BankVisitDetails
        {
            CustomerVisitId = dto.CustomerVisitId,
            Scenario = dto.Scenario,
            ScenarioDescription = dto.ScenarioDescription,
            ScenarioCompleted = dto.ScenarioCompleted,
            ProductOffered = dto.ProductOffered,
            CrossSellOffered = dto.CrossSellOffered,
            EntryTime = dto.EntryTime,
            ExitTime = dto.ExitTime,
            QueueWaitMinutes = dto.QueueWaitMinutes,
            ServiceDurationMinutes = dto.ServiceDurationMinutes,
            QueueTicketTaken = dto.QueueTicketTaken,
            QueueNumber = dto.QueueNumber,
            StaffName = dto.StaffName,
            StaffHasNameTag = dto.StaffHasNameTag,
            GreetingReceived = dto.GreetingReceived,
            FarewellReceived = dto.FarewellReceived,
            StaffAppearanceRating = dto.StaffAppearanceRating,
            StaffKnowledgeRating = dto.StaffKnowledgeRating,
            StaffAttentivenessRating = dto.StaffAttentivenessRating,
            StaffCommunicationRating = dto.StaffCommunicationRating,
            StaffCountObserved = dto.StaffCountObserved,
            BusyCountersCount = dto.BusyCountersCount,
            TotalCountersCount = dto.TotalCountersCount,
            EntranceAreaRating = dto.EntranceAreaRating,
            AtmAreaRating = dto.AtmAreaRating,
            WaitingAreaRating = dto.WaitingAreaRating,
            CounterAreaRating = dto.CounterAreaRating,
            ManagerAreaRating = dto.ManagerAreaRating,
            CleanlinessRating = dto.CleanlinessRating,
            LightingRating = dto.LightingRating,
            AirConditioningRating = dto.AirConditioningRating,
            SignageRating = dto.SignageRating,
            BrochuresAvailable = dto.BrochuresAvailable,
            QueueSystemAvailable = dto.QueueSystemAvailable,
            DisabledAccessAvailable = dto.DisabledAccessAvailable,
            SecurityPersonnelPresent = dto.SecurityPersonnelPresent,
            AtmCount = dto.AtmCount,
            WorkingAtmCount = dto.WorkingAtmCount,
            AtmCleanlinessRating = dto.AtmCleanlinessRating,
            AtmUsabilityRating = dto.AtmUsabilityRating,
            OverallSatisfactionRating = dto.OverallSatisfactionRating,
            RecommendationScore = dto.RecommendationScore,
            WouldVisitAgain = dto.WouldVisitAgain,
            Strengths = dto.Strengths,
            ImprovementAreas = dto.ImprovementAreas,
            AdditionalNotes = dto.AdditionalNotes,
            CreatedAt = DateTime.UtcNow
        };

        _context.BankVisitDetails.Add(bankVisit);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(bankVisit.Id) ?? throw new InvalidOperationException("Banka ziyareti oluşturuldu ancak bulunamadı");
    }

    public async Task<BankVisitDetailsDto> UpdateAsync(Guid id, UpdateBankVisitDetailsDto dto)
    {
        var bankVisit = await _context.BankVisitDetails.FindAsync(id);
        if (bankVisit == null)
        {
            throw new KeyNotFoundException($"Banka ziyaret detayı bulunamadı (ID: {id})");
        }

        bankVisit.Scenario = dto.Scenario;
        bankVisit.ScenarioDescription = dto.ScenarioDescription;
        bankVisit.ScenarioCompleted = dto.ScenarioCompleted;
        bankVisit.ProductOffered = dto.ProductOffered;
        bankVisit.CrossSellOffered = dto.CrossSellOffered;
        bankVisit.EntryTime = dto.EntryTime;
        bankVisit.ExitTime = dto.ExitTime;
        bankVisit.QueueWaitMinutes = dto.QueueWaitMinutes;
        bankVisit.ServiceDurationMinutes = dto.ServiceDurationMinutes;
        bankVisit.QueueTicketTaken = dto.QueueTicketTaken;
        bankVisit.QueueNumber = dto.QueueNumber;
        bankVisit.StaffName = dto.StaffName;
        bankVisit.StaffHasNameTag = dto.StaffHasNameTag;
        bankVisit.GreetingReceived = dto.GreetingReceived;
        bankVisit.FarewellReceived = dto.FarewellReceived;
        bankVisit.StaffAppearanceRating = dto.StaffAppearanceRating;
        bankVisit.StaffKnowledgeRating = dto.StaffKnowledgeRating;
        bankVisit.StaffAttentivenessRating = dto.StaffAttentivenessRating;
        bankVisit.StaffCommunicationRating = dto.StaffCommunicationRating;
        bankVisit.StaffCountObserved = dto.StaffCountObserved;
        bankVisit.BusyCountersCount = dto.BusyCountersCount;
        bankVisit.TotalCountersCount = dto.TotalCountersCount;
        bankVisit.EntranceAreaRating = dto.EntranceAreaRating;
        bankVisit.AtmAreaRating = dto.AtmAreaRating;
        bankVisit.WaitingAreaRating = dto.WaitingAreaRating;
        bankVisit.CounterAreaRating = dto.CounterAreaRating;
        bankVisit.ManagerAreaRating = dto.ManagerAreaRating;
        bankVisit.CleanlinessRating = dto.CleanlinessRating;
        bankVisit.LightingRating = dto.LightingRating;
        bankVisit.AirConditioningRating = dto.AirConditioningRating;
        bankVisit.SignageRating = dto.SignageRating;
        bankVisit.BrochuresAvailable = dto.BrochuresAvailable;
        bankVisit.QueueSystemAvailable = dto.QueueSystemAvailable;
        bankVisit.DisabledAccessAvailable = dto.DisabledAccessAvailable;
        bankVisit.SecurityPersonnelPresent = dto.SecurityPersonnelPresent;
        bankVisit.AtmCount = dto.AtmCount;
        bankVisit.WorkingAtmCount = dto.WorkingAtmCount;
        bankVisit.AtmCleanlinessRating = dto.AtmCleanlinessRating;
        bankVisit.AtmUsabilityRating = dto.AtmUsabilityRating;
        bankVisit.OverallSatisfactionRating = dto.OverallSatisfactionRating;
        bankVisit.RecommendationScore = dto.RecommendationScore;
        bankVisit.WouldVisitAgain = dto.WouldVisitAgain;
        bankVisit.Strengths = dto.Strengths;
        bankVisit.ImprovementAreas = dto.ImprovementAreas;
        bankVisit.AdditionalNotes = dto.AdditionalNotes;
        bankVisit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(bankVisit.Id) ?? throw new InvalidOperationException("Banka ziyareti güncellendi ancak bulunamadı");
    }

    public async Task DeleteAsync(Guid id)
    {
        var bankVisit = await _context.BankVisitDetails.FindAsync(id);
        if (bankVisit == null)
        {
            throw new KeyNotFoundException($"Banka ziyaret detayı bulunamadı (ID: {id})");
        }

        bankVisit.IsDeleted = true;
        bankVisit.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<BankVisitStatisticsDto> GetStatisticsAsync(BankVisitFilterDto? filter = null)
    {
        var query = GetBankVisitsQuery();

        if (filter != null)
        {
            if (filter.CustomerId.HasValue)
                query = query.Where(b => b.CustomerVisit.CustomerId == filter.CustomerId);

            if (filter.BranchId.HasValue)
                query = query.Where(b => b.CustomerVisit.BranchId == filter.BranchId);

            if (filter.FromDate.HasValue)
                query = query.Where(b => b.CustomerVisit.PlannedDate >= filter.FromDate);

            if (filter.ToDate.HasValue)
                query = query.Where(b => b.CustomerVisit.PlannedDate <= filter.ToDate);
        }

        var bankVisits = await query.ToListAsync();

        if (!bankVisits.Any())
        {
            return new BankVisitStatisticsDto();
        }

        var stats = new BankVisitStatisticsDto
        {
            TotalVisits = bankVisits.Count,
            CompletedScenarios = bankVisits.Count(b => b.ScenarioCompleted),
            ScenarioCompletionRate = bankVisits.Count > 0
                ? (decimal)bankVisits.Count(b => b.ScenarioCompleted) / bankVisits.Count * 100
                : 0,
            AverageWaitTime = (decimal)bankVisits.Where(b => b.QueueWaitMinutes.HasValue)
                .Select(b => b.QueueWaitMinutes!.Value)
                .DefaultIfEmpty(0)
                .Average(),
            AverageServiceTime = (decimal)bankVisits.Where(b => b.ServiceDurationMinutes.HasValue)
                .Select(b => b.ServiceDurationMinutes!.Value)
                .DefaultIfEmpty(0)
                .Average(),
            AverageTotalTime = bankVisits
                .Where(b => b.EntryTime.HasValue && b.ExitTime.HasValue)
                .Select(b => (decimal)(b.ExitTime!.Value - b.EntryTime!.Value).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average(),
            GreetingReceivedCount = bankVisits.Count(b => b.GreetingReceived),
            FarewellReceivedCount = bankVisits.Count(b => b.FarewellReceived),
            GreetingRate = bankVisits.Count > 0
                ? (decimal)bankVisits.Count(b => b.GreetingReceived) / bankVisits.Count * 100
                : 0,
            FarewellRate = bankVisits.Count > 0
                ? (decimal)bankVisits.Count(b => b.FarewellReceived) / bankVisits.Count * 100
                : 0
        };

        // Calculate average ratings
        var staffRatings = bankVisits
            .SelectMany(b => new[] { b.StaffAppearanceRating, b.StaffKnowledgeRating, b.StaffAttentivenessRating, b.StaffCommunicationRating })
            .Where(r => r.HasValue)
            .Select(r => r!.Value)
            .ToList();
        stats.AverageStaffRating = staffRatings.Any() ? (decimal)staffRatings.Average() : 0;

        var areaRatings = bankVisits
            .SelectMany(b => new[] { b.EntranceAreaRating, b.AtmAreaRating, b.WaitingAreaRating, b.CounterAreaRating, b.ManagerAreaRating })
            .Where(r => r.HasValue)
            .Select(r => r!.Value)
            .ToList();
        stats.AverageAreaRating = areaRatings.Any() ? (decimal)areaRatings.Average() : 0;

        var facilityRatings = bankVisits
            .SelectMany(b => new[] { b.CleanlinessRating, b.LightingRating, b.AirConditioningRating, b.SignageRating })
            .Where(r => r.HasValue)
            .Select(r => r!.Value)
            .ToList();
        stats.AverageFacilityRating = facilityRatings.Any() ? (decimal)facilityRatings.Average() : 0;

        var satisfactionRatings = bankVisits
            .Where(b => b.OverallSatisfactionRating.HasValue)
            .Select(b => b.OverallSatisfactionRating!.Value)
            .ToList();
        stats.AverageOverallSatisfaction = satisfactionRatings.Any() ? (decimal)satisfactionRatings.Average() : 0;

        var recommendationScores = bankVisits
            .Where(b => b.RecommendationScore.HasValue)
            .Select(b => b.RecommendationScore!.Value)
            .ToList();
        stats.AverageRecommendationScore = recommendationScores.Any() ? (decimal)recommendationScores.Average() : 0;

        // Scenario distribution
        stats.ScenarioDistribution = bankVisits
            .GroupBy(b => b.Scenario)
            .ToDictionary(g => g.Key, g => g.Count());

        // Satisfaction distribution (1-10)
        stats.SatisfactionDistribution = bankVisits
            .Where(b => b.OverallSatisfactionRating.HasValue)
            .GroupBy(b => b.OverallSatisfactionRating!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        return stats;
    }

    public async Task<Dictionary<Guid, BankVisitStatisticsDto>> GetBranchStatisticsAsync(Guid customerId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = GetBankVisitsQuery()
            .Where(b => b.CustomerVisit.CustomerId == customerId && b.CustomerVisit.BranchId.HasValue);

        if (fromDate.HasValue)
            query = query.Where(b => b.CustomerVisit.PlannedDate >= fromDate);

        if (toDate.HasValue)
            query = query.Where(b => b.CustomerVisit.PlannedDate <= toDate);

        var bankVisits = await query.ToListAsync();

        var branchGroups = bankVisits
            .GroupBy(b => b.CustomerVisit.BranchId!.Value);

        var result = new Dictionary<Guid, BankVisitStatisticsDto>();

        foreach (var group in branchGroups)
        {
            var branchVisits = group.ToList();
            result[group.Key] = CalculateStatistics(branchVisits);
        }

        return result;
    }

    public async Task<IEnumerable<BankVisitDetailsDto>> GetForExportAsync(BankVisitFilterDto? filter = null)
    {
        var query = GetBankVisitsQuery();

        if (filter != null)
        {
            if (filter.CustomerId.HasValue)
                query = query.Where(b => b.CustomerVisit.CustomerId == filter.CustomerId);

            if (filter.BranchId.HasValue)
                query = query.Where(b => b.CustomerVisit.BranchId == filter.BranchId);

            if (filter.FromDate.HasValue)
                query = query.Where(b => b.CustomerVisit.PlannedDate >= filter.FromDate);

            if (filter.ToDate.HasValue)
                query = query.Where(b => b.CustomerVisit.PlannedDate <= filter.ToDate);
        }

        var bankVisits = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return bankVisits.Select(MapToDto);
    }

    private IQueryable<BankVisitDetails> GetBankVisitsQuery()
    {
        return _context.BankVisitDetails
            .Include(b => b.CustomerVisit)
                .ThenInclude(cv => cv.Customer)
            .Include(b => b.CustomerVisit)
                .ThenInclude(cv => cv.Branch)
            .Include(b => b.CustomerVisit)
                .ThenInclude(cv => cv.VisitorUser)
            .Where(b => b.CustomerVisit.VisitType == VisitType.BankMysteryShop ||
                        b.CustomerVisit.VisitType == VisitType.MysteryShop);
    }

    private static BankVisitDetailsDto MapToDto(BankVisitDetails bankVisit)
    {
        return new BankVisitDetailsDto
        {
            Id = bankVisit.Id,
            CustomerVisitId = bankVisit.CustomerVisitId,
            Scenario = bankVisit.Scenario,
            ScenarioDescription = bankVisit.ScenarioDescription,
            ScenarioCompleted = bankVisit.ScenarioCompleted,
            ProductOffered = bankVisit.ProductOffered,
            CrossSellOffered = bankVisit.CrossSellOffered,
            EntryTime = bankVisit.EntryTime,
            ExitTime = bankVisit.ExitTime,
            QueueWaitMinutes = bankVisit.QueueWaitMinutes,
            ServiceDurationMinutes = bankVisit.ServiceDurationMinutes,
            QueueTicketTaken = bankVisit.QueueTicketTaken,
            QueueNumber = bankVisit.QueueNumber,
            StaffName = bankVisit.StaffName,
            StaffHasNameTag = bankVisit.StaffHasNameTag,
            GreetingReceived = bankVisit.GreetingReceived,
            FarewellReceived = bankVisit.FarewellReceived,
            StaffAppearanceRating = bankVisit.StaffAppearanceRating,
            StaffKnowledgeRating = bankVisit.StaffKnowledgeRating,
            StaffAttentivenessRating = bankVisit.StaffAttentivenessRating,
            StaffCommunicationRating = bankVisit.StaffCommunicationRating,
            StaffCountObserved = bankVisit.StaffCountObserved,
            BusyCountersCount = bankVisit.BusyCountersCount,
            TotalCountersCount = bankVisit.TotalCountersCount,
            EntranceAreaRating = bankVisit.EntranceAreaRating,
            AtmAreaRating = bankVisit.AtmAreaRating,
            WaitingAreaRating = bankVisit.WaitingAreaRating,
            CounterAreaRating = bankVisit.CounterAreaRating,
            ManagerAreaRating = bankVisit.ManagerAreaRating,
            CleanlinessRating = bankVisit.CleanlinessRating,
            LightingRating = bankVisit.LightingRating,
            AirConditioningRating = bankVisit.AirConditioningRating,
            SignageRating = bankVisit.SignageRating,
            BrochuresAvailable = bankVisit.BrochuresAvailable,
            QueueSystemAvailable = bankVisit.QueueSystemAvailable,
            DisabledAccessAvailable = bankVisit.DisabledAccessAvailable,
            SecurityPersonnelPresent = bankVisit.SecurityPersonnelPresent,
            AtmCount = bankVisit.AtmCount,
            WorkingAtmCount = bankVisit.WorkingAtmCount,
            AtmCleanlinessRating = bankVisit.AtmCleanlinessRating,
            AtmUsabilityRating = bankVisit.AtmUsabilityRating,
            OverallSatisfactionRating = bankVisit.OverallSatisfactionRating,
            RecommendationScore = bankVisit.RecommendationScore,
            WouldVisitAgain = bankVisit.WouldVisitAgain,
            Strengths = bankVisit.Strengths,
            ImprovementAreas = bankVisit.ImprovementAreas,
            AdditionalNotes = bankVisit.AdditionalNotes,
            CreatedAt = bankVisit.CreatedAt
        };
    }

    private static BankVisitSummaryDto MapToSummaryDto(BankVisitDetails bankVisit)
    {
        var staffRatings = new List<int?> {
            bankVisit.StaffAppearanceRating,
            bankVisit.StaffKnowledgeRating,
            bankVisit.StaffAttentivenessRating,
            bankVisit.StaffCommunicationRating
        };
        var validStaffRatings = staffRatings.Where(r => r.HasValue).Select(r => r!.Value).ToList();

        int? totalTimeMinutes = null;
        if (bankVisit.EntryTime.HasValue && bankVisit.ExitTime.HasValue)
        {
            totalTimeMinutes = (int)(bankVisit.ExitTime.Value - bankVisit.EntryTime.Value).TotalMinutes;
        }

        return new BankVisitSummaryDto
        {
            Id = bankVisit.Id,
            CustomerVisitId = bankVisit.CustomerVisitId,
            CustomerName = bankVisit.CustomerVisit?.Customer?.CompanyName,
            BranchName = bankVisit.CustomerVisit?.Branch?.Name,
            VisitorName = bankVisit.CustomerVisit?.VisitorUser != null
                ? $"{bankVisit.CustomerVisit.VisitorUser.FirstName} {bankVisit.CustomerVisit.VisitorUser.LastName}"
                : null,
            Scenario = bankVisit.Scenario,
            ScenarioCompleted = bankVisit.ScenarioCompleted,
            EntryTime = bankVisit.EntryTime,
            TotalTimeMinutes = totalTimeMinutes,
            QueueWaitMinutes = bankVisit.QueueWaitMinutes,
            OverallSatisfactionRating = bankVisit.OverallSatisfactionRating,
            AverageStaffRating = validStaffRatings.Any() ? (decimal)validStaffRatings.Average() : null,
            VisitStatus = bankVisit.CustomerVisit?.Status ?? VisitStatus.Planned,
            PlannedDate = bankVisit.CustomerVisit?.PlannedDate ?? DateTime.MinValue,
            CreatedAt = bankVisit.CreatedAt
        };
    }

    private static BankVisitStatisticsDto CalculateStatistics(List<BankVisitDetails> bankVisits)
    {
        if (!bankVisits.Any())
        {
            return new BankVisitStatisticsDto();
        }

        var stats = new BankVisitStatisticsDto
        {
            TotalVisits = bankVisits.Count,
            CompletedScenarios = bankVisits.Count(b => b.ScenarioCompleted),
            ScenarioCompletionRate = (decimal)bankVisits.Count(b => b.ScenarioCompleted) / bankVisits.Count * 100,
            AverageWaitTime = (decimal)bankVisits.Where(b => b.QueueWaitMinutes.HasValue)
                .Select(b => b.QueueWaitMinutes!.Value)
                .DefaultIfEmpty(0)
                .Average(),
            GreetingReceivedCount = bankVisits.Count(b => b.GreetingReceived),
            FarewellReceivedCount = bankVisits.Count(b => b.FarewellReceived),
            GreetingRate = (decimal)bankVisits.Count(b => b.GreetingReceived) / bankVisits.Count * 100,
            FarewellRate = (decimal)bankVisits.Count(b => b.FarewellReceived) / bankVisits.Count * 100
        };

        var satisfactionRatings = bankVisits
            .Where(b => b.OverallSatisfactionRating.HasValue)
            .Select(b => b.OverallSatisfactionRating!.Value)
            .ToList();
        stats.AverageOverallSatisfaction = satisfactionRatings.Any() ? (decimal)satisfactionRatings.Average() : 0;

        return stats;
    }
}
