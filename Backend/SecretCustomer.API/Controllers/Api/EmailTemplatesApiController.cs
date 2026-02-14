using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Services.Helpers;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/email-templates")]
[ApiController]
[Authorize(Roles = "Admin")]
public class EmailTemplatesApiController : ControllerBase
{
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IEmailService _emailService;
    private readonly IQRCodeService _qrCodeService;

    public EmailTemplatesApiController(IEmailTemplateService emailTemplateService, IEmailService emailService, IQRCodeService qrCodeService)
    {
        _emailTemplateService = emailTemplateService;
        _emailService = emailService;
        _qrCodeService = qrCodeService;
    }

    /// <summary>
    /// Tüm email şablonlarını getir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? templateTypeId = null, [FromQuery] int? customerId = null)
    {
        var templates = await _emailTemplateService.GetAllAsync(templateTypeId, customerId);
        return Ok(templates);
    }

    /// <summary>
    /// Şablon detayı getir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var template = await _emailTemplateService.GetByIdAsync(id);

        if (template == null)
        {
            return NotFound(new { message = "Şablon bulunamadı." });
        }

        return Ok(template);
    }

    /// <summary>
    /// Yeni şablon oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmailTemplateDto dto)
    {
        var result = await _emailTemplateService.CreateAsync(dto.Name, dto.Description, dto.Subject, dto.Body, dto.TemplateTypeId, dto.IsActive, dto.IsDefault, dto.CustomerId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { success = true, id = result.Id, message = result.Message });
    }

    /// <summary>
    /// Şablon güncelle
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmailTemplateDto dto)
    {
        var result = await _emailTemplateService.UpdateAsync(id, dto.Name, dto.Description, dto.Subject, dto.Body, dto.TemplateTypeId, dto.IsActive, dto.IsDefault, dto.CustomerId);

        if (!result.Success)
        {
            // "bulunamadı" ise NotFound, değilse BadRequest
            if (result.Message.Contains("bulunamadı"))
                return NotFound(new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }

    /// <summary>
    /// Şablon sil (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _emailTemplateService.DeleteAsync(id);

        if (!result.Success)
        {
            return NotFound(new { message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }

    /// <summary>
    /// Şablon kopyala
    /// </summary>
    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> Duplicate(int id)
    {
        var result = await _emailTemplateService.DuplicateAsync(id);

        if (!result.Success)
        {
            return NotFound(new { message = result.Message });
        }

        return Ok(new { success = true, id = result.Id, message = result.Message });
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
        var exampleUrl = "https://survey.example.com/abc123";
        body = body.Replace(EmailPlaceholders.SurveyUrl, exampleUrl);
        body = body.Replace(EmailPlaceholders.SurveyLink, $"<a href=\"{exampleUrl}\" target=\"_blank\">{exampleUrl}</a>");
        body = body.Replace(EmailPlaceholders.SurveyQRCode, "<img src='data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==' alt='QR Code' style='width:150px;height:150px;border:1px solid #ccc;' />");
        body = body.Replace(EmailPlaceholders.CompanyName, "ABC Şirketi");
        body = body.Replace(EmailPlaceholders.OrganizationName, "İstanbul Şubesi");
        body = body.Replace(EmailPlaceholders.RecipientName, "Ahmet Yılmaz");
        body = body.Replace(EmailPlaceholders.RecipientFirstName, "Ahmet");
        body = body.Replace(EmailPlaceholders.RecipientLastName, "Yılmaz");
        body = body.Replace(EmailPlaceholders.RecipientEmail, "ahmet@example.com");
        body = body.Replace(EmailPlaceholders.ProjectName, "2025 Müşteri Memnuniyeti Anketi");
        body = body.Replace(EmailPlaceholders.SurveyName, "Hizmet Kalitesi Değerlendirmesi");
        body = body.Replace(EmailPlaceholders.DueDate, TurkeyTime.Now.AddDays(7).ToString("dd.MM.yyyy"));
        body = body.Replace(EmailPlaceholders.StartDate, TurkeyTime.Now.ToString("dd.MM.yyyy"));
        body = body.Replace(EmailPlaceholders.EndDate, TurkeyTime.Now.AddMonths(1).ToString("dd.MM.yyyy"));
        body = body.Replace(EmailPlaceholders.CurrentDate, TurkeyTime.Now.ToString("dd.MM.yyyy"));
        body = body.Replace(EmailPlaceholders.CurrentYear, TurkeyTime.Now.Year.ToString());
        body = body.Replace(EmailPlaceholders.SystemName, "Secret Customer");

        var subject = dto.Subject;
        subject = subject.Replace(EmailPlaceholders.ProjectName, "2025 Müşteri Memnuniyeti Anketi");
        subject = subject.Replace(EmailPlaceholders.SurveyName, "Hizmet Kalitesi Değerlendirmesi");
        subject = subject.Replace(EmailPlaceholders.CompanyName, "ABC Şirketi");

        return Ok(new { subject, body });
    }

    /// <summary>
    /// Şablonu test emaili olarak gönder
    /// </summary>
    [HttpPost("{id}/send-test")]
    public async Task<IActionResult> SendTestEmail(int id, [FromBody] SendTestEmailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ToEmail))
        {
            return BadRequest(new { success = false, message = "Email adresi gerekli." });
        }

        var template = await _emailTemplateService.FindByIdAsync(id);
        if (template == null)
        {
            return NotFound(new { success = false, message = "Şablon bulunamadı." });
        }

        string body;
        string subject;

        // Gerçek proje ve personel seçilmişse gerçek verilerle doldur
        if (dto.ProjectId.HasValue && dto.PersonnelId.HasValue)
        {
            var project = await _emailTemplateService.GetTestProjectWithDetailsAsync(dto.ProjectId.Value);

            if (project == null)
            {
                return BadRequest(new { success = false, message = "Proje bulunamadı." });
            }

            var personnel = await _emailTemplateService.GetTestPersonnelWithDetailsAsync(dto.PersonnelId.Value);

            if (personnel == null)
            {
                return BadRequest(new { success = false, message = "Personel bulunamadı." });
            }

            // Gerçek token ve URL oluştur
            var token = EncryptionHelper.CreateSurveyToken(project.Id, personnel.Id);
            var baseUrl = dto.BaseUrl ?? $"{Request.Scheme}://{Request.Host}";
            var surveyUrl = $"{baseUrl.TrimEnd('/')}/Survey/Form?token={token}";

            // Gerçek QR kod oluştur
            var qrBase64 = _qrCodeService.GenerateQRCodeBase64(surveyUrl);
            var qrHtml = $"<img src='data:image/png;base64,{qrBase64}' alt='QR Code' style='width:150px;height:150px;' />";

            // Personelin ilk organizasyonunu al
            var personnelOrg = personnel.OrganizationAssignments?.FirstOrDefault()?.CustomerOrganization;

            // Placeholder'ları gerçek verilerle doldur
            body = template.Body;
            body = body.Replace(EmailPlaceholders.SurveyUrl, surveyUrl);
            body = body.Replace(EmailPlaceholders.SurveyLink, $"<a href=\"{surveyUrl}\" target=\"_blank\">{surveyUrl}</a>");
            body = body.Replace(EmailPlaceholders.SurveyQRCode, qrHtml);
            body = body.Replace(EmailPlaceholders.CompanyName, project.Customer?.CompanyName ?? "");
            body = body.Replace(EmailPlaceholders.OrganizationName, project.Organization?.Name ?? personnelOrg?.Name ?? "");
            body = body.Replace(EmailPlaceholders.RecipientName, $"{personnel.FirstName} {personnel.LastName}".Trim());
            body = body.Replace(EmailPlaceholders.RecipientFirstName, personnel.FirstName ?? "");
            body = body.Replace(EmailPlaceholders.RecipientLastName, personnel.LastName ?? "");
            body = body.Replace(EmailPlaceholders.RecipientEmail, dto.ToEmail);
            body = body.Replace(EmailPlaceholders.ProjectName, project.Name);
            body = body.Replace(EmailPlaceholders.SurveyName, project.Checklist?.Name ?? project.Name);
            body = body.Replace(EmailPlaceholders.DueDate, project.EndDate.ToString("dd.MM.yyyy"));
            body = body.Replace(EmailPlaceholders.StartDate, project.StartDate.ToString("dd.MM.yyyy"));
            body = body.Replace(EmailPlaceholders.EndDate, project.EndDate.ToString("dd.MM.yyyy"));
            body = body.Replace(EmailPlaceholders.CurrentDate, TurkeyTime.Now.ToString("dd.MM.yyyy"));
            body = body.Replace(EmailPlaceholders.CurrentYear, TurkeyTime.Now.Year.ToString());
            body = body.Replace(EmailPlaceholders.SystemName, "Secret Customer");

            subject = "[TEST] " + template.Subject;
            subject = subject.Replace(EmailPlaceholders.ProjectName, project.Name);
            subject = subject.Replace(EmailPlaceholders.SurveyName, project.Checklist?.Name ?? project.Name);
            subject = subject.Replace(EmailPlaceholders.CompanyName, project.Customer?.CompanyName ?? "");
        }
        else
        {
            // Örnek verilerle doldur (eski davranış)
            var testUrl = "https://survey.example.com/test123";
            body = template.Body;
            body = body.Replace(EmailPlaceholders.SurveyUrl, testUrl);
            body = body.Replace(EmailPlaceholders.SurveyLink, $"<a href=\"{testUrl}\" target=\"_blank\">{testUrl}</a>");
            body = body.Replace(EmailPlaceholders.SurveyQRCode, "<img src='data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==' alt='QR Code' style='width:150px;height:150px;border:1px solid #ccc;' />");
            body = body.Replace(EmailPlaceholders.CompanyName, "Test Şirketi");
            body = body.Replace(EmailPlaceholders.OrganizationName, "Test Şubesi");
            body = body.Replace(EmailPlaceholders.RecipientName, "Test Kullanıcı");
            body = body.Replace(EmailPlaceholders.RecipientFirstName, "Test");
            body = body.Replace(EmailPlaceholders.RecipientLastName, "Kullanıcı");
            body = body.Replace(EmailPlaceholders.RecipientEmail, dto.ToEmail);
            body = body.Replace(EmailPlaceholders.ProjectName, "Test Projesi");
            body = body.Replace(EmailPlaceholders.SurveyName, "Test Anketi");
            body = body.Replace(EmailPlaceholders.DueDate, TurkeyTime.Now.AddDays(7).ToString("dd.MM.yyyy"));
            body = body.Replace(EmailPlaceholders.StartDate, TurkeyTime.Now.ToString("dd.MM.yyyy"));
            body = body.Replace(EmailPlaceholders.EndDate, TurkeyTime.Now.AddMonths(1).ToString("dd.MM.yyyy"));
            body = body.Replace(EmailPlaceholders.CurrentDate, TurkeyTime.Now.ToString("dd.MM.yyyy"));
            body = body.Replace(EmailPlaceholders.CurrentYear, TurkeyTime.Now.Year.ToString());
            body = body.Replace(EmailPlaceholders.SystemName, "Secret Customer");

            subject = "[TEST] " + template.Subject;
            subject = subject.Replace(EmailPlaceholders.ProjectName, "Test Projesi");
            subject = subject.Replace(EmailPlaceholders.SurveyName, "Test Anketi");
            subject = subject.Replace(EmailPlaceholders.CompanyName, "Test Şirketi");
        }

        var result = await _emailService.SendEmailAsync(dto.ToEmail, subject, body, isHtml: true);

        if (result.Success)
        {
            return Ok(new { success = true, message = $"Test emaili {dto.ToEmail} adresine gönderildi." });
        }

        return BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Test için proje listesi getir (OnlineSurvey projeleri)
    /// </summary>
    [HttpGet("test-projects")]
    public async Task<IActionResult> GetTestProjects()
    {
        var projects = await _emailTemplateService.GetTestProjectsAsync();
        return Ok(projects);
    }

    /// <summary>
    /// Test için proje personel listesi getir
    /// </summary>
    [HttpGet("test-personnel/{projectId}")]
    public async Task<IActionResult> GetTestPersonnel(int projectId)
    {
        var result = await _emailTemplateService.GetTestPersonnelAsync(projectId);

        if (!result.Found)
        {
            return NotFound(new { message = "Proje bulunamadı." });
        }

        return Ok(result.Personnel);
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

public class SendTestEmailDto
{
    public string? ToEmail { get; set; }
    public int? ProjectId { get; set; }
    public int? PersonnelId { get; set; }
    public string? BaseUrl { get; set; }
}
