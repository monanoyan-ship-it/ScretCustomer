using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Data;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/email-templates")]
[ApiController]
[Authorize(Roles = "Admin")]
public class EmailTemplatesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmailTemplatesApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tüm email şablonlarını getir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? templateTypeId = null, [FromQuery] int? customerId = null)
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
            .Select(e => new EmailTemplateDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Subject = e.Subject,
                Body = e.Body,
                TemplateTypeId = e.TemplateTypeId,
                TemplateTypeName = EmailTemplateTypes.GetById(e.TemplateTypeId)!.Description,
                IsActive = e.IsActive,
                IsDefault = e.IsDefault,
                CustomerId = e.CustomerId,
                CustomerName = e.Customer != null ? e.Customer.CompanyName : null,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .ToListAsync();

        return Ok(templates);
    }

    /// <summary>
    /// Şablon detayı getir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var template = await _context.EmailTemplates
            .Include(e => e.Customer)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (template == null)
        {
            return NotFound(new { message = "Şablon bulunamadı." });
        }

        return Ok(new EmailTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Subject = template.Subject,
            Body = template.Body,
            TemplateTypeId = template.TemplateTypeId,
            TemplateTypeName = EmailTemplateTypes.GetById(template.TemplateTypeId)!.Description,
            IsActive = template.IsActive,
            IsDefault = template.IsDefault,
            CustomerId = template.CustomerId,
            CustomerName = template.Customer?.CompanyName,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        });
    }

    /// <summary>
    /// Yeni şablon oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmailTemplateDto dto)
    {
        // Aynı isimde şablon var mı?
        var exists = await _context.EmailTemplates
            .AnyAsync(e => e.Name == dto.Name && !e.IsDeleted);

        if (exists)
        {
            return BadRequest(new { message = "Bu isimde bir şablon zaten var." });
        }

        var template = new EmailTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            Subject = dto.Subject,
            Body = dto.Body,
            TemplateTypeId = dto.TemplateTypeId,
            IsActive = dto.IsActive,
            IsDefault = dto.IsDefault,
            CustomerId = dto.CustomerId,
            CreatedAt = DateTime.UtcNow
        };

        // Eğer varsayılan yapılıyorsa, diğer varsayılanları kaldır
        if (dto.IsDefault)
        {
            var othersDefault = await _context.EmailTemplates
                .Where(e => e.TemplateTypeId == dto.TemplateTypeId && e.IsDefault && !e.IsDeleted)
                .ToListAsync();

            foreach (var other in othersDefault)
            {
                other.IsDefault = false;
            }
        }

        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, id = template.Id, message = "Şablon oluşturuldu." });
    }

    /// <summary>
    /// Şablon güncelle
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmailTemplateDto dto)
    {
        var template = await _context.EmailTemplates.FindAsync(id);

        if (template == null || template.IsDeleted)
        {
            return NotFound(new { message = "Şablon bulunamadı." });
        }

        // Aynı isimde başka şablon var mı?
        var exists = await _context.EmailTemplates
            .AnyAsync(e => e.Id != id && e.Name == dto.Name && !e.IsDeleted);

        if (exists)
        {
            return BadRequest(new { message = "Bu isimde bir şablon zaten var." });
        }

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.Subject = dto.Subject;
        template.Body = dto.Body;
        template.TemplateTypeId = dto.TemplateTypeId;
        template.IsActive = dto.IsActive;
        template.CustomerId = dto.CustomerId;
        template.UpdatedAt = DateTime.UtcNow;

        // Varsayılan değişikliği
        if (dto.IsDefault && !template.IsDefault)
        {
            var othersDefault = await _context.EmailTemplates
                .Where(e => e.Id != id && e.TemplateTypeId == dto.TemplateTypeId && e.IsDefault && !e.IsDeleted)
                .ToListAsync();

            foreach (var other in othersDefault)
            {
                other.IsDefault = false;
            }
        }
        template.IsDefault = dto.IsDefault;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Şablon güncellendi." });
    }

    /// <summary>
    /// Şablon sil (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var template = await _context.EmailTemplates.FindAsync(id);

        if (template == null || template.IsDeleted)
        {
            return NotFound(new { message = "Şablon bulunamadı." });
        }

        template.IsDeleted = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Şablon silindi." });
    }

    /// <summary>
    /// Şablon kopyala
    /// </summary>
    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> Duplicate(int id)
    {
        var template = await _context.EmailTemplates.FindAsync(id);

        if (template == null || template.IsDeleted)
        {
            return NotFound(new { message = "Şablon bulunamadı." });
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
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailTemplates.Add(newTemplate);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, id = newTemplate.Id, message = "Şablon kopyalandı." });
    }

    /// <summary>
    /// Placeholder kategorilerini getir
    /// </summary>
    [HttpGet("placeholders")]
    [AllowAnonymous]
    public IActionResult GetPlaceholders()
    {
        var categories = EmailPlaceholders.GetAllCategories();
        return Ok(categories);
    }

    /// <summary>
    /// Şablon tiplerini getir
    /// </summary>
    [HttpGet("types")]
    [AllowAnonymous]
    public IActionResult GetTypes()
    {
        var types = EmailTemplateTypes.All.Select(t => new
        {
            t.Id,
            t.SystemName,
            displayName = t.Description,
            t.Description,
            t.Icon,
            t.CssClass
        });

        return Ok(types);
    }

    /// <summary>
    /// Şablon önizleme (placeholder'ları örnek verilerle doldur)
    /// </summary>
    [HttpPost("preview")]
    public IActionResult Preview([FromBody] PreviewEmailDto dto)
    {
        var body = dto.Body;

        // Örnek verilerle değiştir
        body = body.Replace(EmailPlaceholders.SurveyLink, "https://survey.example.com/abc123");
        body = body.Replace(EmailPlaceholders.SurveyQRCode, "<img src='data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==' alt='QR Code' style='width:150px;height:150px;border:1px solid #ccc;' />");
        body = body.Replace(EmailPlaceholders.CompanyName, "ABC Şirketi");
        body = body.Replace(EmailPlaceholders.OrganizationName, "İstanbul Şubesi");
        body = body.Replace(EmailPlaceholders.BranchName, "Kadıköy Mağazası");
        body = body.Replace(EmailPlaceholders.RecipientName, "Ahmet Yılmaz");
        body = body.Replace(EmailPlaceholders.RecipientFirstName, "Ahmet");
        body = body.Replace(EmailPlaceholders.RecipientLastName, "Yılmaz");
        body = body.Replace(EmailPlaceholders.RecipientEmail, "ahmet@example.com");
        body = body.Replace(EmailPlaceholders.ProjectName, "2025 Müşteri Memnuniyeti Anketi");
        body = body.Replace(EmailPlaceholders.SurveyName, "Hizmet Kalitesi Değerlendirmesi");
        body = body.Replace(EmailPlaceholders.DueDate, DateTime.Now.AddDays(7).ToString("dd.MM.yyyy"));
        body = body.Replace(EmailPlaceholders.StartDate, DateTime.Now.ToString("dd.MM.yyyy"));
        body = body.Replace(EmailPlaceholders.EndDate, DateTime.Now.AddMonths(1).ToString("dd.MM.yyyy"));
        body = body.Replace(EmailPlaceholders.CurrentDate, DateTime.Now.ToString("dd.MM.yyyy"));
        body = body.Replace(EmailPlaceholders.CurrentYear, DateTime.Now.Year.ToString());
        body = body.Replace(EmailPlaceholders.SystemName, "Secret Customer");

        var subject = dto.Subject;
        subject = subject.Replace(EmailPlaceholders.ProjectName, "2025 Müşteri Memnuniyeti Anketi");
        subject = subject.Replace(EmailPlaceholders.SurveyName, "Hizmet Kalitesi Değerlendirmesi");
        subject = subject.Replace(EmailPlaceholders.CompanyName, "ABC Şirketi");

        return Ok(new { subject, body });
    }
}

// DTOs
public class EmailTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int TemplateTypeId { get; set; }
    public string? TemplateTypeName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateEmailTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int TemplateTypeId { get; set; } = EmailTemplateTypes.Ids.SurveyInvitation;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public int? CustomerId { get; set; }
}

public class UpdateEmailTemplateDto : CreateEmailTemplateDto
{
}

public class PreviewEmailDto
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
