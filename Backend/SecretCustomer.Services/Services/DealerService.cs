using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Dealer;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class DealerService : IDealerService
{
    private readonly ApplicationDbContext _context;

    public DealerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedDealerResult> GetAllAsync(DealerFilterDto filter)
    {
        var query = _context.Dealers
            .Include(d => d.Customer)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(term) ||
                d.Code.ToLower().Contains(term) ||
                (d.Address != null && d.Address.ToLower().Contains(term)) ||
                (d.City != null && d.City.ToLower().Contains(term)) ||
                (d.District != null && d.District.ToLower().Contains(term)) ||
                (d.ContactPerson != null && d.ContactPerson.ToLower().Contains(term)) ||
                (d.Phone != null && d.Phone.Contains(term)));
        }

        // Çoklu müşteri filtresi (OR mantığı)
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(d => filter.CustomerIds.Contains(d.CustomerId));

        if (filter.IsActive.HasValue)
            query = query.Where(d => d.IsActive == filter.IsActive.Value);

        // Çoklu bayi tipi filtresi (OR mantığı)
        if (filter.DealerTypeIds?.Any() == true)
            query = query.Where(d => filter.DealerTypeIds.Contains(d.DealerTypeId));

        // Çoklu şehir filtresi (OR mantığı, partial match, case-insensitive)
        if (filter.Cities?.Any() == true)
        {
            var citiesLower = filter.Cities.Select(c => c.ToLower()).ToList();
            query = query.Where(d => d.City != null && citiesLower.Any(city => d.City.ToLower().Contains(city)));
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var dealers = await query
            .OrderBy(d => d.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        // Get evaluation stats
        var dealerIds = dealers.Select(d => d.Id).ToList();
        var evaluationStats = await _context.Evaluations
            .Where(e => e.DealerId != null && dealerIds.Contains(e.DealerId.Value))
            .GroupBy(e => e.DealerId)
            .Select(g => new
            {
                DealerId = g.Key,
                Count = g.Count(),
                AvgScore = g.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage)
            })
            .ToListAsync();

        var items = dealers.Select(d =>
        {
            var stats = evaluationStats.FirstOrDefault(s => s.DealerId == d.Id);
            return MapToDto(d, stats?.Count ?? 0, stats?.AvgScore);
        }).ToList();

        return new PagedDealerResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DealerDto?> GetByIdAsync(int id)
    {
        var dealer = await _context.Dealers
            .Include(d => d.Customer)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dealer == null)
            return null;

        // Get evaluation stats
        var stats = await _context.Evaluations
            .Where(e => e.DealerId == id)
            .GroupBy(e => e.DealerId)
            .Select(g => new
            {
                Count = g.Count(),
                AvgScore = g.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage)
            })
            .FirstOrDefaultAsync();

        return MapToDto(dealer, stats?.Count ?? 0, stats?.AvgScore);
    }

    public async Task<DealerDto> CreateAsync(CreateDealerDto dto)
    {
        // Generate code
        var code = await GenerateCodeAsync(dto.CustomerId);

        var dealer = new Dealer
        {
            Code = code,
            Name = dto.Name,
            Address = dto.Address,
            City = dto.City,
            District = dto.District,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Phone = dto.Phone,
            Email = dto.Email,
            ContactPerson = dto.ContactPerson,
            WorkingHoursJson = dto.WorkingHoursJson,
            DealerTypeId = dto.DealerTypeId,
            IsActive = dto.IsActive,
            Notes = dto.Notes,
            CustomerId = dto.CustomerId
        };

        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(dealer.Id))!;
    }

    public async Task<DealerDto?> UpdateAsync(int id, UpdateDealerDto dto)
    {
        var dealer = await _context.Dealers.FindAsync(id);
        if (dealer == null)
            return null;

        dealer.Name = dto.Name;
        dealer.Address = dto.Address;
        dealer.City = dto.City;
        dealer.District = dto.District;
        dealer.Latitude = dto.Latitude;
        dealer.Longitude = dto.Longitude;
        dealer.Phone = dto.Phone;
        dealer.Email = dto.Email;
        dealer.ContactPerson = dto.ContactPerson;
        dealer.WorkingHoursJson = dto.WorkingHoursJson;
        dealer.DealerTypeId = dto.DealerTypeId;
        dealer.IsActive = dto.IsActive;
        dealer.Notes = dto.Notes;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var dealer = await _context.Dealers.FindAsync(id);
        if (dealer == null)
            return false;

        dealer.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<DealerDto>> GetByCustomerAsync(int customerId)
    {
        var dealers = await _context.Dealers
            .Include(d => d.Customer)
            .Where(d => d.CustomerId == customerId)
            .OrderBy(d => d.Name)
            .ToListAsync();

        // Get evaluation stats
        var dealerIds = dealers.Select(d => d.Id).ToList();
        var evaluationStats = await _context.Evaluations
            .Where(e => e.DealerId != null && dealerIds.Contains(e.DealerId.Value))
            .GroupBy(e => e.DealerId)
            .Select(g => new
            {
                DealerId = g.Key,
                Count = g.Count(),
                AvgScore = g.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage)
            })
            .ToListAsync();

        return dealers.Select(d =>
        {
            var stats = evaluationStats.FirstOrDefault(s => s.DealerId == d.Id);
            return MapToDto(d, stats?.Count ?? 0, stats?.AvgScore);
        }).ToList();
    }

    public async Task<string> GenerateCodeAsync(int customerId)
    {
        // Get last dealer code for this customer
        var lastDealer = await _context.Dealers
            .Where(d => d.CustomerId == customerId)
            .OrderByDescending(d => d.Code)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastDealer != null && lastDealer.Code.StartsWith("BAY-"))
        {
            var codeNumber = lastDealer.Code.Replace("BAY-", "");
            if (int.TryParse(codeNumber, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"BAY-{nextNumber:D3}";
    }

    private static DealerDto MapToDto(Dealer dealer, int evaluationCount = 0, decimal? averageScore = null)
    {
        return new DealerDto
        {
            Id = dealer.Id,
            Code = dealer.Code,
            Name = dealer.Name,
            Address = dealer.Address,
            City = dealer.City,
            District = dealer.District,
            Latitude = dealer.Latitude,
            Longitude = dealer.Longitude,
            Phone = dealer.Phone,
            Email = dealer.Email,
            ContactPerson = dealer.ContactPerson,
            WorkingHoursJson = dealer.WorkingHoursJson,
            DealerTypeId = dealer.DealerTypeId,
            IsActive = dealer.IsActive,
            Notes = dealer.Notes,
            CustomerId = dealer.CustomerId,
            CustomerName = dealer.Customer?.CompanyName,
            EvaluationCount = evaluationCount,
            AverageScore = averageScore,
            CreatedAt = dealer.CreatedAt
        };
    }
}
