using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/localization")]
[ApiController]
[Authorize]
public class LocalizationApiController : ControllerBase
{
    private readonly ILocalizationService _localizationService;
    private readonly IWebHostEnvironment _environment;

    public LocalizationApiController(
        ILocalizationService localizationService,
        IWebHostEnvironment environment)
    {
        _localizationService = localizationService;
        _environment = environment;
    }

    #region Languages

    [HttpGet("languages")]
    public async Task<IActionResult> GetLanguages([FromQuery] bool onlyActive = true)
    {
        var languages = await _localizationService.GetAllLanguagesAsync(onlyActive);
        return Ok(languages.Select(l => new
        {
            l.Id,
            l.Name,
            l.LanguageCulture,
            l.UniqueSeoCode,
            l.FlagImageFileName,
            l.Rtl,
            l.IsDefault,
            l.IsActive,
            l.DisplayOrder
        }));
    }

    [HttpGet("languages/{id}")]
    public async Task<IActionResult> GetLanguage(Guid id)
    {
        var language = await _localizationService.GetLanguageByIdAsync(id);
        if (language == null)
            return NotFound(new { message = "Dil bulunamadı." });

        return Ok(new
        {
            language.Id,
            language.Name,
            language.LanguageCulture,
            language.UniqueSeoCode,
            language.FlagImageFileName,
            language.Rtl,
            language.IsDefault,
            language.IsActive,
            language.DisplayOrder
        });
    }

    [HttpPost("languages")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateLanguage([FromBody] CreateLanguageDto dto)
    {
        var language = new Language
        {
            Name = dto.Name,
            LanguageCulture = dto.LanguageCulture,
            UniqueSeoCode = dto.UniqueSeoCode,
            FlagImageFileName = dto.FlagImageFileName,
            Rtl = dto.Rtl,
            IsDefault = dto.IsDefault,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder
        };

        var result = await _localizationService.CreateLanguageAsync(language);
        return Ok(new { message = "Dil oluşturuldu.", id = result.Id });
    }

    [HttpPut("languages/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateLanguage(Guid id, [FromBody] UpdateLanguageDto dto)
    {
        var language = new Language
        {
            Id = id,
            Name = dto.Name,
            LanguageCulture = dto.LanguageCulture,
            UniqueSeoCode = dto.UniqueSeoCode,
            FlagImageFileName = dto.FlagImageFileName,
            Rtl = dto.Rtl,
            IsDefault = dto.IsDefault,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder
        };

        await _localizationService.UpdateLanguageAsync(language);
        return Ok(new { message = "Dil güncellendi." });
    }

    [HttpDelete("languages/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLanguage(Guid id)
    {
        await _localizationService.DeleteLanguageAsync(id);
        return Ok(new { message = "Dil silindi." });
    }

    [HttpPost("languages/seed-defaults")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SeedDefaultLanguages()
    {
        var existingLanguages = await _localizationService.GetAllLanguagesAsync(false);
        if (existingLanguages.Any())
        {
            return Ok(new { message = "Diller zaten mevcut.", count = existingLanguages.Count() });
        }

        var defaultLanguages = new List<(string Name, string Culture, string Seo, bool IsDefault)>
        {
            ("Türkçe", "tr-TR", "tr", true),
            ("English", "en-US", "en", false),
            ("Español", "es-ES", "es", false),
            ("Deutsch", "de-DE", "de", false)
        };

        var createdCount = 0;
        var importedResources = 0;
        var basePath = _environment.ContentRootPath;

        foreach (var (name, culture, seo, isDefault) in defaultLanguages)
        {
            var language = new Language
            {
                Name = name,
                LanguageCulture = culture,
                UniqueSeoCode = seo,
                FlagImageFileName = $"{seo}.png",
                Rtl = false,
                IsDefault = isDefault,
                IsActive = true,
                DisplayOrder = createdCount + 1
            };

            var createdLang = await _localizationService.CreateLanguageAsync(language);
            createdCount++;

            // XML dosyası varsa otomatik import et
            try
            {
                var count = await _localizationService.ImportFromDefaultXmlAsync(createdLang.Id, basePath);
                importedResources += count;
            }
            catch (FileNotFoundException)
            {
                // XML dosyası yoksa devam et
            }
        }

        return Ok(new {
            message = $"{createdCount} dil oluşturuldu, {importedResources} çeviri içe aktarıldı.",
            languageCount = createdCount,
            resourceCount = importedResources
        });
    }

    #endregion

    #region Resources

    [HttpGet("resources/{languageId}")]
    public async Task<IActionResult> GetResources(Guid languageId)
    {
        var resources = await _localizationService.GetResourcesByLanguageAsync(languageId);
        return Ok(resources.Select(r => new
        {
            r.Id,
            r.ResourceName,
            r.ResourceValue
        }));
    }

    [HttpGet("resources/{languageId}/all")]
    public async Task<IActionResult> GetAllResources(Guid languageId)
    {
        var resources = await _localizationService.GetAllResourcesAsync(languageId);
        return Ok(resources);
    }

    [HttpGet("resources/{languageId}/prefix/{prefix}")]
    public async Task<IActionResult> GetResourcesByPrefix(Guid languageId, string prefix)
    {
        var resources = await _localizationService.GetResourcesByPrefixAsync(prefix, languageId);
        return Ok(resources);
    }

    [HttpPost("resources/{languageId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetResource(Guid languageId, [FromBody] SetResourceDto dto)
    {
        var result = await _localizationService.SetResourceAsync(languageId, dto.ResourceName, dto.ResourceValue);
        return Ok(new { message = "Kaynak kaydedildi.", id = result.Id });
    }

    [HttpDelete("resources/{resourceId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteResource(Guid resourceId)
    {
        await _localizationService.DeleteResourceAsync(resourceId);
        return Ok(new { message = "Kaynak silindi." });
    }

    #endregion

    #region Import/Export

    [HttpPost("import/{languageId}/xml")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportFromXml(Guid languageId, [FromBody] ImportXmlDto dto)
    {
        try
        {
            var count = await _localizationService.ImportFromXmlAsync(languageId, dto.XmlContent);
            return Ok(new { message = $"{count} çeviri içe aktarıldı.", count });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("import/{languageId}/default")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportFromDefaultXml(Guid languageId)
    {
        try
        {
            var basePath = _environment.ContentRootPath;
            var count = await _localizationService.ImportFromDefaultXmlAsync(languageId, basePath);
            return Ok(new { message = $"{count} çeviri içe aktarıldı.", count });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("export/{languageId}/xml")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportToXml(Guid languageId)
    {
        try
        {
            var xml = await _localizationService.ExportToXmlAsync(languageId);
            var language = await _localizationService.GetLanguageByIdAsync(languageId);
            var fileName = $"resources.{language?.UniqueSeoCode ?? "unknown"}.xml";

            return File(
                System.Text.Encoding.UTF8.GetBytes(xml),
                "application/xml",
                fileName
            );
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("available-xml-files")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAvailableXmlFiles()
    {
        var localizationPath = Path.Combine(_environment.ContentRootPath, "App_Data", "Localization");

        if (!Directory.Exists(localizationPath))
            return Ok(new List<object>());

        var files = Directory.GetFiles(localizationPath, "resources.*.xml")
            .Select(f => new
            {
                FileName = Path.GetFileName(f),
                LanguageCode = Path.GetFileNameWithoutExtension(f).Replace("resources.", "")
            })
            .ToList();

        return Ok(files);
    }

    #endregion

    #region Current Language

    [HttpGet("current")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCurrentLanguage()
    {
        var langId = _localizationService.GetCurrentLanguageId();
        var language = await _localizationService.GetLanguageByIdAsync(langId);

        if (language == null)
        {
            language = await _localizationService.GetDefaultLanguageAsync();
        }

        return Ok(new
        {
            language?.Id,
            language?.Name,
            language?.LanguageCulture,
            language?.UniqueSeoCode,
            language?.Rtl
        });
    }

    [HttpPost("current/{languageId}")]
    [AllowAnonymous]
    public IActionResult SetCurrentLanguage(Guid languageId)
    {
        _localizationService.SetCurrentLanguage(languageId);
        return Ok(new { message = "Dil değiştirildi." });
    }

    [HttpPost("current/code/{languageCode}")]
    [AllowAnonymous]
    public IActionResult SetCurrentLanguageByCode(string languageCode)
    {
        _localizationService.SetCurrentLanguage(languageCode);
        return Ok(new { message = "Dil değiştirildi." });
    }

    [HttpGet("translate/{resourceName}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTranslation(string resourceName, [FromQuery] Guid? languageId = null)
    {
        var value = await _localizationService.GetResourceAsync(resourceName, languageId);
        return Ok(new { resourceName, value });
    }

    #endregion
}

#region DTOs

public class CreateLanguageDto
{
    public string Name { get; set; } = string.Empty;
    public string LanguageCulture { get; set; } = string.Empty;
    public string UniqueSeoCode { get; set; } = string.Empty;
    public string? FlagImageFileName { get; set; }
    public bool Rtl { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public class UpdateLanguageDto
{
    public string Name { get; set; } = string.Empty;
    public string LanguageCulture { get; set; } = string.Empty;
    public string UniqueSeoCode { get; set; } = string.Empty;
    public string? FlagImageFileName { get; set; }
    public bool Rtl { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class SetResourceDto
{
    public string ResourceName { get; set; } = string.Empty;
    public string ResourceValue { get; set; } = string.Empty;
}

public class ImportXmlDto
{
    public string XmlContent { get; set; } = string.Empty;
}

#endregion
