using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly ApplicationDbContext _context;

    public EmailTemplateService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<object>> GetAllAsync(int? templateTypeId, int? customerId)
    {
        var query = _context.EmailTemplates
            .Include(e => e.Customer)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        if (templateTypeId.HasValue)
        {
            query = query.Where(e => e.TemplateTypeId == templateTypeId.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(e => e.CustomerId == customerId.Value || e.CustomerId == null);
        }

        var templates = await query
            .OrderByDescending(e => e.IsDefault)
            .ThenBy(e => e.Name)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.Description,
                e.Subject,
                e.Body,
                e.TemplateTypeId,
                TemplateTypeName = EmailTemplateTypes.GetById(e.TemplateTypeId)!.Description,
                e.IsActive,
                e.IsDefault,
                e.CustomerId,
                CustomerName = e.Customer != null ? e.Customer.CompanyName : (string?)null,
                e.CreatedAt,
                e.UpdatedAt
            })
            .ToListAsync();

        return templates.Cast<object>().ToList();
    }

    public async Task<object?> GetByIdAsync(int id)
    {
        var template = await _context.EmailTemplates
            .Include(e => e.Customer)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (template == null)
        {
            return null;
        }

        return new
        {
            template.Id,
            template.Name,
            template.Description,
            template.Subject,
            template.Body,
            template.TemplateTypeId,
            TemplateTypeName = EmailTemplateTypes.GetById(template.TemplateTypeId)!.Description,
            template.IsActive,
            template.IsDefault,
            template.CustomerId,
            CustomerName = template.Customer?.CompanyName,
            template.CreatedAt,
            template.UpdatedAt
        };
    }

    public async Task<(bool Success, int? Id, string Message)> CreateAsync(string name, string? description, string subject, string body, int templateTypeId, bool isActive, bool isDefault, int? customerId)
    {
        // Aynı isimde şablon var mı?
        var exists = await _context.EmailTemplates
            .AnyAsync(e => e.Name == name && !e.IsDeleted);

        if (exists)
        {
            return (false, null, "Bu isimde bir şablon zaten var.");
        }

        var template = new EmailTemplate
        {
            Name = name,
            Description = description,
            Subject = subject,
            Body = body,
            TemplateTypeId = templateTypeId,
            IsActive = isActive,
            IsDefault = isDefault,
            CustomerId = customerId,
            CreatedAt = TurkeyTime.Now
        };

        // Eğer varsayılan yapılıyorsa, diğer varsayılanları kaldır
        if (isDefault)
        {
            var othersDefault = await _context.EmailTemplates
                .Where(e => e.TemplateTypeId == templateTypeId && e.IsDefault && !e.IsDeleted)
                .ToListAsync();

            foreach (var other in othersDefault)
            {
                other.IsDefault = false;
            }
        }

        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        return (true, template.Id, "Şablon oluşturuldu.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(int id, string name, string? description, string subject, string body, int templateTypeId, bool isActive, bool isDefault, int? customerId)
    {
        var template = await _context.EmailTemplates.FindAsync(id);

        if (template == null || template.IsDeleted)
        {
            return (false, "Şablon bulunamadı.");
        }

        // Aynı isimde başka şablon var mı?
        var exists = await _context.EmailTemplates
            .AnyAsync(e => e.Id != id && e.Name == name && !e.IsDeleted);

        if (exists)
        {
            return (false, "Bu isimde bir şablon zaten var.");
        }

        template.Name = name;
        template.Description = description;
        template.Subject = subject;
        template.Body = body;
        template.TemplateTypeId = templateTypeId;
        template.IsActive = isActive;
        template.CustomerId = customerId;
        template.UpdatedAt = TurkeyTime.Now;

        // Varsayılan değişikliği
        if (isDefault && !template.IsDefault)
        {
            var othersDefault = await _context.EmailTemplates
                .Where(e => e.Id != id && e.TemplateTypeId == templateTypeId && e.IsDefault && !e.IsDeleted)
                .ToListAsync();

            foreach (var other in othersDefault)
            {
                other.IsDefault = false;
            }
        }
        template.IsDefault = isDefault;

        await _context.SaveChangesAsync();

        return (true, "Şablon güncellendi.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var template = await _context.EmailTemplates.FindAsync(id);

        if (template == null || template.IsDeleted)
        {
            return (false, "Şablon bulunamadı.");
        }

        template.IsDeleted = true;
        template.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        return (true, "Şablon silindi.");
    }

    public async Task<(bool Success, int? Id, string Message)> DuplicateAsync(int id)
    {
        var template = await _context.EmailTemplates.FindAsync(id);

        if (template == null || template.IsDeleted)
        {
            return (false, null, "Şablon bulunamadı.");
        }

        var newTemplate = new EmailTemplate
        {
            Name = template.Name + " (Kopya)",
            Description = template.Description,
            Subject = template.Subject,
            Body = template.Body,
            TemplateTypeId = template.TemplateTypeId,
            IsActive = false,
            IsDefault = false,
            CustomerId = template.CustomerId,
            CreatedAt = TurkeyTime.Now
        };

        _context.EmailTemplates.Add(newTemplate);
        await _context.SaveChangesAsync();

        return (true, newTemplate.Id, "Şablon kopyalandı.");
    }

    public async Task<EmailTemplate?> FindByIdAsync(int id)
    {
        var template = await _context.EmailTemplates.FindAsync(id);
        if (template == null || template.IsDeleted)
            return null;
        return template;
    }

    public async Task<List<object>> GetTestProjectsAsync()
    {
        var projects = await _context.Projects
            .Include(p => p.Customer)
            .Where(p => !p.IsDeleted && p.ProjectTypeId == ProjectTypes.Ids.OnlineSurvey)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Name,
                CustomerName = p.Customer != null ? p.Customer.CompanyName : (string?)null
            })
            .Take(50)
            .ToListAsync();

        return projects.Cast<object>().ToList();
    }

    public async Task<(bool Found, List<object> Personnel)> GetTestPersonnelAsync(int projectId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            return (false, new List<object>());
        }

        // Projenin müşterisine ait personelleri getir
        var query = _context.CustomerPersonnel
            .Include(cp => cp.OrganizationAssignments)
                .ThenInclude(oa => oa.CustomerOrganization)
            .Where(cp => !cp.IsDeleted && cp.Email != null && cp.Email != "");

        if (project.CustomerId.HasValue)
        {
            query = query.Where(cp => cp.CustomerId == project.CustomerId.Value);
        }

        if (project.OrganizationId.HasValue)
        {
            query = query.Where(cp => cp.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == project.OrganizationId.Value));
        }

        var personnelList = await query
            .OrderBy(cp => cp.FirstName)
            .ThenBy(cp => cp.LastName)
            .Take(100)
            .ToListAsync();

        var personnel = personnelList.Select(cp => (object)new
        {
            cp.Id,
            FullName = $"{cp.FirstName} {cp.LastName}".Trim(),
            cp.Email,
            OrganizationName = cp.OrganizationAssignments?.FirstOrDefault()?.CustomerOrganization?.Name
        }).ToList();

        return (true, personnel);
    }

    public async Task<Project?> GetTestProjectWithDetailsAsync(int projectId)
    {
        return await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Organization)
            .Include(p => p.Checklist)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
    }

    public async Task<CustomerPersonnel?> GetTestPersonnelWithDetailsAsync(int personnelId)
    {
        return await _context.CustomerPersonnel
            .Include(cp => cp.OrganizationAssignments)
                .ThenInclude(oa => oa.CustomerOrganization)
            .FirstOrDefaultAsync(cp => cp.Id == personnelId && !cp.IsDeleted);
    }
}
