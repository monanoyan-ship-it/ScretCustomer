using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class CustomerTaskListDataService : ICustomerTaskListDataService
{
    private readonly ApplicationDbContext _context;

    public CustomerTaskListDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerTaskList?> GetByIdAsync(int id)
        => await _context.CustomerTaskLists.FindAsync(id);

    public async Task<List<CustomerTaskList>> GetAllAsync()
        => await _context.CustomerTaskLists.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(CustomerTaskList entity)
    {
        _context.CustomerTaskLists.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerTaskList entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.CustomerTaskLists.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.CustomerTaskLists.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
