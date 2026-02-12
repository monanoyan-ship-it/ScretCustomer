using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class TrainingVideoDataService : ITrainingVideoDataService
{
    private readonly ApplicationDbContext _context;

    public TrainingVideoDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingVideo?> GetByIdAsync(int id)
        => await _context.TrainingVideos.FindAsync(id);

    public async Task<List<TrainingVideo>> GetAllAsync()
        => await _context.TrainingVideos.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(TrainingVideo entity)
    {
        _context.TrainingVideos.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingVideo entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.TrainingVideos.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TrainingVideos.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
