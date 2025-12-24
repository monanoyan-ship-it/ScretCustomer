using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Personnel;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class PersonnelService : IPersonnelService
{
    private readonly ApplicationDbContext _context;

    public PersonnelService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Helper: DateTime'ı UTC'ye çevir (PostgreSQL için gerekli)
    private static DateTime? ToUtc(DateTime? dateTime)
    {
        if (!dateTime.HasValue) return null;
        if (dateTime.Value.Kind == DateTimeKind.Utc) return dateTime;
        return DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
    }

    public async Task<PagedPersonnelResult> GetAllAsync(PersonnelFilterDto filter)
    {
        var query = _context.Personnel
            .Include(p => p.Branch)
            .Include(p => p.Customer)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(term) ||
                p.LastName.ToLower().Contains(term) ||
                (p.TcKimlikNo != null && p.TcKimlikNo.Contains(term)) ||
                (p.ErpNo != null && p.ErpNo.ToLower().Contains(term)) ||
                (p.SicilNo != null && p.SicilNo.ToLower().Contains(term)) ||
                (p.Email != null && p.Email.ToLower().Contains(term)));
        }

        if (filter.BranchId.HasValue)
            query = query.Where(p => p.BranchId == filter.BranchId.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(p => p.CustomerId == filter.CustomerId.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(p => p.IsActive == filter.IsActive.Value);

        if (filter.Gender.HasValue)
            query = query.Where(p => p.Gender == filter.Gender.Value);

        if (!string.IsNullOrEmpty(filter.Department))
            query = query.Where(p => p.Department == filter.Department);

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var personnel = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        // Get evaluation stats
        var personnelIds = personnel.Select(p => p.Id).ToList();
        var evaluationStats = await _context.Evaluations
            .Where(e => e.EvaluatedPersonnelId != null && personnelIds.Contains(e.EvaluatedPersonnelId.Value))
            .GroupBy(e => e.EvaluatedPersonnelId)
            .Select(g => new
            {
                PersonnelId = g.Key,
                Count = g.Count(),
                AvgScore = g.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage)
            })
            .ToListAsync();

        var items = personnel.Select(p =>
        {
            var stats = evaluationStats.FirstOrDefault(s => s.PersonnelId == p.Id);
            return MapToDto(p, stats?.Count ?? 0, stats?.AvgScore);
        }).ToList();

        return new PagedPersonnelResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PersonnelDto?> GetByIdAsync(Guid id)
    {
        var personnel = await _context.Personnel
            .Include(p => p.Branch)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (personnel == null)
            return null;

        // Get evaluation stats
        var stats = await _context.Evaluations
            .Where(e => e.EvaluatedPersonnelId == id)
            .GroupBy(e => e.EvaluatedPersonnelId)
            .Select(g => new
            {
                Count = g.Count(),
                AvgScore = g.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage)
            })
            .FirstOrDefaultAsync();

        return MapToDto(personnel, stats?.Count ?? 0, stats?.AvgScore);
    }

    public async Task<PersonnelDto> CreateAsync(CreatePersonnelDto dto)
    {
        // Validate unique constraints
        if (!string.IsNullOrEmpty(dto.TcKimlikNo) && await ExistsByTcKimlikNoAsync(dto.TcKimlikNo))
            throw new InvalidOperationException("Bu TC Kimlik No ile kayıtlı personel zaten var.");

        if (!string.IsNullOrEmpty(dto.SicilNo) && await ExistsBySicilNoAsync(dto.SicilNo))
            throw new InvalidOperationException("Bu Sicil No ile kayıtlı personel zaten var.");

        var personnel = new Personnel
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            TcKimlikNo = dto.TcKimlikNo,
            ErpNo = dto.ErpNo,
            SicilNo = dto.SicilNo,
            Title = dto.Title,
            Gender = dto.Gender,
            BirthDate = ToUtc(dto.BirthDate),
            HireDate = ToUtc(dto.HireDate),
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Department = dto.Department,
            IsActive = dto.IsActive,
            Notes = dto.Notes,
            BranchId = dto.BranchId,
            CustomerId = dto.CustomerId
        };

        _context.Personnel.Add(personnel);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(personnel.Id))!;
    }

    public async Task<PersonnelDto?> UpdateAsync(Guid id, UpdatePersonnelDto dto)
    {
        var personnel = await _context.Personnel.FindAsync(id);
        if (personnel == null)
            return null;

        // Validate unique constraints
        if (!string.IsNullOrEmpty(dto.TcKimlikNo) && await ExistsByTcKimlikNoAsync(dto.TcKimlikNo, id))
            throw new InvalidOperationException("Bu TC Kimlik No ile kayıtlı başka bir personel zaten var.");

        if (!string.IsNullOrEmpty(dto.SicilNo) && await ExistsBySicilNoAsync(dto.SicilNo, id))
            throw new InvalidOperationException("Bu Sicil No ile kayıtlı başka bir personel zaten var.");

        personnel.FirstName = dto.FirstName;
        personnel.LastName = dto.LastName;
        personnel.TcKimlikNo = dto.TcKimlikNo;
        personnel.ErpNo = dto.ErpNo;
        personnel.SicilNo = dto.SicilNo;
        personnel.Title = dto.Title;
        personnel.Gender = dto.Gender;
        personnel.BirthDate = ToUtc(dto.BirthDate);
        personnel.HireDate = ToUtc(dto.HireDate);
        personnel.Email = dto.Email;
        personnel.PhoneNumber = dto.PhoneNumber;
        personnel.Department = dto.Department;
        personnel.IsActive = dto.IsActive;
        personnel.Notes = dto.Notes;
        personnel.BranchId = dto.BranchId;
        personnel.CustomerId = dto.CustomerId;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var personnel = await _context.Personnel.FindAsync(id);
        if (personnel == null)
            return false;

        personnel.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<PersonnelDto>> GetByBranchAsync(Guid branchId)
    {
        var personnel = await _context.Personnel
            .Include(p => p.Branch)
            .Include(p => p.Customer)
            .Where(p => p.BranchId == branchId && p.IsActive)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return personnel.Select(p => MapToDto(p, 0, null)).ToList();
    }

    public async Task<List<PersonnelDto>> GetByCustomerAsync(Guid customerId)
    {
        var personnel = await _context.Personnel
            .Include(p => p.Branch)
            .Include(p => p.Customer)
            .Where(p => p.CustomerId == customerId && p.IsActive)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return personnel.Select(p => MapToDto(p, 0, null)).ToList();
    }

    public async Task<bool> ExistsByTcKimlikNoAsync(string tcKimlikNo, Guid? excludeId = null)
    {
        var query = _context.Personnel.Where(p => p.TcKimlikNo == tcKimlikNo);
        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<bool> ExistsBySicilNoAsync(string sicilNo, Guid? excludeId = null)
    {
        var query = _context.Personnel.Where(p => p.SicilNo == sicilNo);
        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    private static PersonnelDto MapToDto(Personnel personnel, int evaluationCount, decimal? averageScore)
    {
        return new PersonnelDto
        {
            Id = personnel.Id,
            FirstName = personnel.FirstName,
            LastName = personnel.LastName,
            TcKimlikNo = personnel.TcKimlikNo,
            ErpNo = personnel.ErpNo,
            SicilNo = personnel.SicilNo,
            Title = personnel.Title,
            Gender = personnel.Gender,
            BirthDate = personnel.BirthDate,
            HireDate = personnel.HireDate,
            Email = personnel.Email,
            PhoneNumber = personnel.PhoneNumber,
            Department = personnel.Department,
            IsActive = personnel.IsActive,
            Notes = personnel.Notes,
            BranchId = personnel.BranchId,
            BranchName = personnel.Branch?.Name,
            CustomerId = personnel.CustomerId,
            CustomerName = personnel.Customer?.CompanyName,
            EvaluationCount = evaluationCount,
            AverageScore = averageScore.HasValue ? Math.Round(averageScore.Value, 2) : null,
            CreatedAt = personnel.CreatedAt
        };
    }
}
