using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class EmailTemplateDataService : IEmailTemplateDataService
{
    private readonly ApplicationDbContext _context;

    public EmailTemplateDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmailTemplate?> GetByIdAsync(int id)
        => await _context.EmailTemplates.FindAsync(id);

    public async Task<List<EmailTemplate>> GetAllAsync()
        => await _context.EmailTemplates.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(EmailTemplate entity)
    {
        _context.EmailTemplates.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(EmailTemplate entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.EmailTemplates.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.EmailTemplates.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
