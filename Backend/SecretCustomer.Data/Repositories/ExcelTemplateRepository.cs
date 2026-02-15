using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class ExcelTemplateRepository : IExcelTemplateRepository
{
    private readonly ApplicationDbContext _context;

    public ExcelTemplateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExcelTemplate?> GetByIdAsync(int id, bool includeColumns = false)
    {
        var query = _context.ExcelTemplates.AsQueryable();

        if (includeColumns)
        {
            query = query
                .Include(t => t.Columns.OrderBy(c => c.Order))
                .Include(t => t.Filters.OrderBy(f => f.Order));
        }

        return await query.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<ExcelTemplate>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.ExcelTemplates
            .Include(t => t.Columns.OrderBy(c => c.Order))
            .Include(t => t.Filters.OrderBy(f => f.Order))
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ExcelTemplate>> GetByEntityTypeAsync(string entityType)
    {
        return await _context.ExcelTemplates
            .Include(t => t.Columns.OrderBy(c => c.Order))
            .Include(t => t.Filters.OrderBy(f => f.Order))
            .Where(t => t.EntityType == entityType && t.IsActive)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<ExcelTemplate> CreateAsync(ExcelTemplate template)
    {
        template.CreatedAt = TurkeyTime.Now;
        template.UpdatedAt = TurkeyTime.Now;

        // Set CreatedAt for columns as well
        foreach (var column in template.Columns)
        {
            column.CreatedAt = TurkeyTime.Now;
            column.UpdatedAt = TurkeyTime.Now;
        }

        _context.ExcelTemplates.Add(template);
        await _context.SaveChangesAsync();

        return template;
    }

    public async Task<ExcelTemplate> UpdateAsync(ExcelTemplate template)
    {
        template.UpdatedAt = TurkeyTime.Now;

        // Get existing template with columns
        var existingTemplate = await _context.ExcelTemplates
            .Include(t => t.Columns)
            .FirstOrDefaultAsync(t => t.Id == template.Id);

        if (existingTemplate == null)
        {
            throw new InvalidOperationException($"ExcelTemplate with id {template.Id} not found");
        }

        // Update template properties
        existingTemplate.Name = template.Name;
        existingTemplate.Description = template.Description;
        existingTemplate.EntityType = template.EntityType;
        existingTemplate.IsActive = template.IsActive;
        existingTemplate.SheetName = template.SheetName;
        existingTemplate.HasHeader = template.HasHeader;
        existingTemplate.GroupByPropertyName = template.GroupByPropertyName;
        existingTemplate.UpdatedAt = template.UpdatedAt;

        // Remove columns that are no longer present
        var columnsToRemove = existingTemplate.Columns
            .Where(ec => !template.Columns.Any(c => c.Id == ec.Id))
            .ToList();

        foreach (var column in columnsToRemove)
        {
            _context.ExcelColumns.Remove(column);
        }

        // Update or add columns
        foreach (var column in template.Columns)
        {
            var existingColumn = existingTemplate.Columns.FirstOrDefault(c => c.Id == column.Id);

            if (existingColumn != null)
            {
                // Update existing column
                existingColumn.ColumnName = column.ColumnName;
                existingColumn.PropertyName = column.PropertyName;
                existingColumn.ColumnTypeId = column.ColumnTypeId;
                existingColumn.AggregateTypeId = column.AggregateTypeId;
                existingColumn.Order = column.Order;
                existingColumn.IsRequired = column.IsRequired;
                existingColumn.ValidationRules = column.ValidationRules;
                existingColumn.DropdownOptions = column.DropdownOptions;
                existingColumn.SampleValue = column.SampleValue;
                existingColumn.Description = column.Description;
                existingColumn.UpdatedAt = TurkeyTime.Now;
            }
            else
            {
                // Add new column
                column.ExcelTemplateId = template.Id;
                column.CreatedAt = TurkeyTime.Now;
                column.UpdatedAt = TurkeyTime.Now;
                existingTemplate.Columns.Add(column);
            }
        }

        await _context.SaveChangesAsync();

        return existingTemplate;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var template = await _context.ExcelTemplates.FindAsync(id);

        if (template == null)
        {
            return false;
        }

        // Soft delete
        template.IsDeleted = true;
        template.UpdatedAt = TurkeyTime.Now;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.ExcelTemplates.AnyAsync(t => t.Id == id);
    }
}
