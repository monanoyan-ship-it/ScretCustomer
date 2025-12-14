using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AppSettingsApiController : ControllerBase
{
    private readonly IAppSettingsService _settingsService;
    private readonly ILogger<AppSettingsApiController> _logger;

    public AppSettingsApiController(IAppSettingsService settingsService, ILogger<AppSettingsApiController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var settings = await _settingsService.GetAllAsync();
        return Ok(settings.OrderBy(s => s.Category).ThenBy(s => s.DisplayOrder).ThenBy(s => s.Key));
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var settings = await _settingsService.GetByCategoryAsync(category);
        return Ok(settings.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Key));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var settings = await _settingsService.GetAllAsync();
        var categories = settings.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
        return Ok(categories);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key)
    {
        var setting = await _settingsService.GetByKeyAsync(key);
        if (setting == null)
            return NotFound(new { message = "Ayar bulunamadı." });

        return Ok(setting);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AppSettingsDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Key))
            return BadRequest(new { message = "Ayar anahtarı gereklidir." });

        var existing = await _settingsService.GetByKeyAsync(dto.Key);
        if (existing != null)
            return BadRequest(new { message = "Bu anahtar zaten mevcut." });

        var setting = await _settingsService.SetAsync(
            dto.Key,
            dto.Value ?? "",
            dto.ValueType,
            dto.Category ?? "General",
            dto.Description
        );

        _logger.LogInformation("AppSetting created: {Key} by {User}", dto.Key, User.Identity?.Name);

        return Ok(setting);
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Update(string key, [FromBody] AppSettingsDto dto)
    {
        var existing = await _settingsService.GetByKeyAsync(key);
        if (existing == null)
            return NotFound(new { message = "Ayar bulunamadı." });

        var setting = await _settingsService.SetAsync(
            key,
            dto.Value ?? "",
            dto.ValueType,
            dto.Category ?? existing.Category,
            dto.Description ?? existing.Description
        );

        _logger.LogInformation("AppSetting updated: {Key} by {User}", key, User.Identity?.Name);

        return Ok(setting);
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        var existing = await _settingsService.GetByKeyAsync(key);
        if (existing == null)
            return NotFound(new { message = "Ayar bulunamadı." });

        if (existing.IsSystem)
            return BadRequest(new { message = "Sistem ayarları silinemez." });

        await _settingsService.DeleteAsync(key);

        _logger.LogInformation("AppSetting deleted: {Key} by {User}", key, User.Identity?.Name);

        return Ok(new { message = "Ayar silindi." });
    }

    public class AppSettingsDto
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public SettingValueType ValueType { get; set; } = SettingValueType.String;
        public string? Category { get; set; }
        public string? Description { get; set; }
    }
}
