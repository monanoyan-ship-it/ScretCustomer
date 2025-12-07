using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class FieldWorkerRepository : IFieldWorkerRepository
{
    private readonly ApplicationDbContext _context;

    public FieldWorkerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FieldWorker?> GetByIdAsync(Guid id)
    {
        return await _context.FieldWorkers
            .Include(fw => fw.User)
            .FirstOrDefaultAsync(fw => fw.Id == id);
    }

    public async Task<FieldWorker?> GetByUserIdAsync(Guid userId)
    {
        return await _context.FieldWorkers
            .Include(fw => fw.User)
            .FirstOrDefaultAsync(fw => fw.UserId == userId);
    }

    public async Task<IEnumerable<FieldWorker>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.FieldWorkers
            .Include(fw => fw.User)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(fw => fw.IsActive);
        }

        return await query
            .OrderBy(fw => fw.FirstName)
            .ThenBy(fw => fw.LastName)
            .ToListAsync();
    }

    public async Task<IEnumerable<FieldWorker>> GetActiveAsync()
    {
        return await _context.FieldWorkers
            .Include(fw => fw.User)
            .Where(fw => fw.IsActive)
            .OrderBy(fw => fw.FirstName)
            .ThenBy(fw => fw.LastName)
            .ToListAsync();
    }

    public async Task<FieldWorker> CreateAsync(FieldWorker fieldWorker)
    {
        _context.FieldWorkers.Add(fieldWorker);
        await _context.SaveChangesAsync();
        return fieldWorker;
    }

    public async Task<FieldWorker> UpdateAsync(FieldWorker fieldWorker)
    {
        fieldWorker.UpdatedAt = DateTime.UtcNow;
        _context.FieldWorkers.Update(fieldWorker);
        await _context.SaveChangesAsync();
        return fieldWorker;
    }

    public async Task DeleteAsync(Guid id)
    {
        var fieldWorker = await _context.FieldWorkers.FindAsync(id);
        if (fieldWorker != null)
        {
            fieldWorker.IsDeleted = true;
            fieldWorker.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, Guid? excludeId = null)
    {
        var query = _context.FieldWorkers.Where(fw => fw.PhoneNumber == phoneNumber);

        if (excludeId.HasValue)
        {
            query = query.Where(fw => fw.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
