using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class SurveyExternalInvitationDataService : ISurveyExternalInvitationDataService
{
    private readonly ApplicationDbContext _context;

    public SurveyExternalInvitationDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SurveyExternalInvitation?> GetByIdAsync(int id)
        => await _context.SurveyExternalInvitations.FindAsync(id);

    public async Task<List<SurveyExternalInvitation>> GetAllAsync()
        => await _context.SurveyExternalInvitations.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(SurveyExternalInvitation entity)
    {
        _context.SurveyExternalInvitations.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SurveyExternalInvitation entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.SurveyExternalInvitations.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.SurveyExternalInvitations.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
