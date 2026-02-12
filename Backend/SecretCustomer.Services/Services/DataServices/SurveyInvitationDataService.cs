using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class SurveyInvitationDataService : ISurveyInvitationDataService
{
    private readonly ApplicationDbContext _context;

    public SurveyInvitationDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SurveyInvitation?> GetByIdAsync(int id)
        => await _context.SurveyInvitations.FindAsync(id);

    public async Task<List<SurveyInvitation>> GetAllAsync()
        => await _context.SurveyInvitations.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(SurveyInvitation entity)
    {
        _context.SurveyInvitations.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SurveyInvitation entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.SurveyInvitations.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.SurveyInvitations.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
