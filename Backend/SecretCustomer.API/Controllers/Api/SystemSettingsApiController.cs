using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.SystemSetting;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/system-settings")]
[Authorize(Roles = "Admin")]
public class SystemSettingsApiController : BaseApiController
{
    private readonly ISystemSettingService _settingService;
    private readonly IAuditLogService _auditLogService;

    public SystemSettingsApiController(
        ISystemSettingService settingService,
        IAuditLogService auditLogService,
        IConfiguration configuration) : base(configuration)
    {
        _settingService = settingService;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Tüm ayarları listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var settings = await _settingService.GetAllAsync();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading system settings", "SystemSettings", ex);
            return StatusCode(500, CreateErrorResponse("Ayarlar yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Belirli bir ayarı getir
    /// </summary>
    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key)
    {
        try
        {
            var setting = await _settingService.GetByKeyAsync(key);
            if (setting == null)
                return NotFound(CreateErrorResponse($"'{key}' ayarı bulunamadı"));

            return Ok(setting);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading system setting {key}", "SystemSettings", ex);
            return StatusCode(500, CreateErrorResponse("Ayar yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Yeni ayar oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSystemSettingDto dto)
    {
        try
        {
            var setting = await _settingService.CreateAsync(dto);
            return Ok(new { message = "Ayar başarıyla oluşturuldu", setting });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error creating system setting", "SystemSettings", ex);
            return StatusCode(500, CreateErrorResponse("Ayar oluşturulurken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Ayar güncelle
    /// </summary>
    [HttpPut("{key}")]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateSystemSettingDto dto)
    {
        try
        {
            var setting = await _settingService.UpdateAsync(key, dto);
            return Ok(new { message = "Ayar başarıyla güncellendi", setting });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating system setting {key}", "SystemSettings", ex);
            return StatusCode(500, CreateErrorResponse("Ayar güncellenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Ayar sil
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        try
        {
            var result = await _settingService.DeleteAsync(key);
            if (!result)
                return NotFound(CreateErrorResponse($"'{key}' ayarı bulunamadı"));

            return Ok(new { message = "Ayar başarıyla silindi" });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting system setting {key}", "SystemSettings", ex);
            return StatusCode(500, CreateErrorResponse("Ayar silinirken hata oluştu", ex));
        }
    }
}
