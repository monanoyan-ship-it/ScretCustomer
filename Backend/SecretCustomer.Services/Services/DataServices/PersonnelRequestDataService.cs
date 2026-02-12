using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class PersonnelRequestDataService : IPersonnelRequestDataService
{
    private readonly ApplicationDbContext _context;

    public PersonnelRequestDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PersonnelRequest?> GetByIdAsync(int id)
        => await _context.PersonnelRequests.FindAsync(id);

    public async Task<List<PersonnelRequest>> GetAllAsync()
        => await _context.PersonnelRequests.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(PersonnelRequest entity)
    {
        _context.PersonnelRequests.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PersonnelRequest entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.PersonnelRequests.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.PersonnelRequests.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
