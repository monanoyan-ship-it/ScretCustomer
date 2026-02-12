using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class AnnouncementDataService : IAnnouncementDataService
{
    private readonly ApplicationDbContext _context;

    public AnnouncementDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Announcement?> GetByIdAsync(int id)
        => await _context.Announcements.FindAsync(id);

    public async Task<List<Announcement>> GetAllAsync()
        => await _context.Announcements.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(Announcement entity)
    {
        _context.Announcements.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Announcement entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.Announcements.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Announcements.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
