using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id, bool includeDetails = false)
    {
        var query = _context.Customers
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(c => c.Personnel)
                .Include(c => c.Projects)
                .Include(c => c.TaskLists);
        }

        return await query.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Customer>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Customers
            .Where(c => !c.IsDeleted)
            .Include(c => c.Personnel)
            .Include(c => c.Organizations)
            .Include(c => c.Projects)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.CompanyName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Customer>> GetActiveAsync()
    {
        return await _context.Customers
            .Where(c => !c.IsDeleted && c.IsActive)
            .Include(c => c.Personnel)
            .Include(c => c.Projects)
            .OrderBy(c => c.CompanyName)
            .ToListAsync();
    }

    public async Task<Customer?> GetByTaxNumberAsync(string taxNumber)
    {
        return await _context.Customers
            .Where(c => !c.IsDeleted)
            .Include(c => c.Personnel)
            .FirstOrDefaultAsync(c => c.TaxNumber == taxNumber);
    }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        return await _context.Customers
            .Where(c => !c.IsDeleted)
            .Include(c => c.Personnel)
            .FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<Customer?> GetByNameAsync(string companyName)
    {
        return await _context.Customers
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.CompanyName.ToLower() == companyName.ToLower());
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            customer.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByTaxNumberAsync(string taxNumber, int? excludeId = null)
    {
        return await _context.Customers
            .AnyAsync(c => !c.IsDeleted && c.TaxNumber == taxNumber && (excludeId == null || c.Id != excludeId));
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
    {
        if (string.IsNullOrEmpty(email)) return false;

        return await _context.Customers
            .AnyAsync(c => !c.IsDeleted && c.Email == email && (excludeId == null || c.Id != excludeId));
    }
}
