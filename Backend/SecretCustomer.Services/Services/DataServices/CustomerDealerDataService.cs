using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class CustomerDealerDataService : ICustomerDealerDataService
{
    private readonly ApplicationDbContext _context;

    public CustomerDealerDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerDealer?> GetByIdAsync(int id)
        => await _context.CustomerDealers.FindAsync(id);

    public async Task<List<CustomerDealer>> GetAllAsync()
        => await _context.CustomerDealers.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(CustomerDealer entity)
    {
        _context.CustomerDealers.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerDealer entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.CustomerDealers.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.CustomerDealers.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
