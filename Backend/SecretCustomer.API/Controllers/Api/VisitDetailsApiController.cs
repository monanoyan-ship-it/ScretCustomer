using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Visit;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// Ziyaret Detayları API - Dinamik alan bazlı sistem
/// Sektör, alan tanımı ve değer yönetimi
/// </summary>
[ApiController]
[Route("api/visit-details")]
[Authorize]
public class VisitDetailsApiController : BaseApiController
{
    private readonly IVisitDetailService _visitDetailService;
    private readonly ILogger<VisitDetailsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public VisitDetailsApiController(
        IVisitDetailService visitDetailService,
        ILogger<VisitDetailsApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _visitDetailService = visitDetailService;
        _logger = logger;
        _localizationService = localizationService;
    }

    #region Sektör İşlemleri

    /// <summary>
    /// Tüm sektörleri getirir
    /// </summary>
    [HttpGet("sectors")]
    public async Task<IActionResult> GetAllSectors()
    {
        try
        {
            var sectors = await _visitDetailService.GetAllSectorsAsync();
            return Ok(sectors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sectors");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.SectorsLoadError"), ex));
        }
    }

    /// <summary>
    /// Sektör detayını getirir
    /// </summary>
    [HttpGet("sectors/{id}")]
    public async Task<IActionResult> GetSectorById(Guid id)
    {
        try
        {
            var sector = await _visitDetailService.GetSectorByIdAsync(id);
            if (sector == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.SectorNotFound")));
            return Ok(sector);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sector {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.SectorLoadError"), ex));
        }
    }

    /// <summary>
    /// Sektör oluşturur
    /// </summary>
    [HttpPost("sectors")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSector([FromBody] SaveVisitSectorDto dto)
    {
        try
        {
            var sector = await _visitDetailService.CreateSectorAsync(dto);
            return Ok(sector);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sector");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.SectorCreateError"), ex));
        }
    }

    /// <summary>
    /// Sektör günceller
    /// </summary>
    [HttpPut("sectors/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSector(Guid id, [FromBody] SaveVisitSectorDto dto)
    {
        try
        {
            var sector = await _visitDetailService.UpdateSectorAsync(id, dto);
            return Ok(sector);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sector {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.SectorUpdateError"), ex));
        }
    }

    /// <summary>
    /// Sektör siler
    /// </summary>
    [HttpDelete("sectors/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSector(Guid id)
    {
        try
        {
            await _visitDetailService.DeleteSectorAsync(id);
            return Ok(new { message = await _localizationService.GetResourceAsync("Api.VisitDetail.SectorDeleteSuccess") });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sector {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.SectorDeleteError"), ex));
        }
    }

    #endregion

    #region Alan Tanımları

    /// <summary>
    /// Tüm alan tanımlarını getirir
    /// </summary>
    [HttpGet("fields")]
    public async Task<IActionResult> GetAllFieldDefinitions()
    {
        try
        {
            var fields = await _visitDetailService.GetAllFieldDefinitionsAsync();
            return Ok(fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting field definitions");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.FieldsLoadError"), ex));
        }
    }

    /// <summary>
    /// Sektöre göre alan tanımlarını getirir
    /// </summary>
    [HttpGet("fields/sector/{sectorId}")]
    public async Task<IActionResult> GetFieldDefinitionsBySector(Guid sectorId)
    {
        try
        {
            var fields = await _visitDetailService.GetFieldDefinitionsBySectorAsync(sectorId);
            return Ok(fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting field definitions for sector {SectorId}", sectorId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.FieldsLoadError"), ex));
        }
    }

    /// <summary>
    /// Ortak alan tanımlarını getirir (sektöre bağlı olmayan)
    /// </summary>
    [HttpGet("fields/common")]
    public async Task<IActionResult> GetCommonFieldDefinitions()
    {
        try
        {
            var fields = await _visitDetailService.GetFieldDefinitionsBySectorAsync(null);
            return Ok(fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting common field definitions");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.FieldsLoadError"), ex));
        }
    }

    /// <summary>
    /// Bir ziyaret için geçerli alanları getirir (ortak + sektöre özel)
    /// </summary>
    [HttpGet("fields/for-visit")]
    public async Task<IActionResult> GetFieldDefinitionsForVisit([FromQuery] Guid? sectorId)
    {
        try
        {
            var fields = await _visitDetailService.GetFieldDefinitionsForVisitAsync(sectorId);
            return Ok(fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting field definitions for visit");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.FieldsLoadError"), ex));
        }
    }

    /// <summary>
    /// Kategorilere göre gruplu alan tanımlarını getirir
    /// </summary>
    [HttpGet("fields/grouped")]
    public async Task<IActionResult> GetFieldDefinitionsGrouped([FromQuery] Guid? sectorId)
    {
        try
        {
            var fields = await _visitDetailService.GetFieldDefinitionsGroupedAsync(sectorId);
            return Ok(fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grouped field definitions");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.FieldsLoadError"), ex));
        }
    }

    /// <summary>
    /// Alan tanımı detayını getirir
    /// </summary>
    [HttpGet("fields/{id}")]
    public async Task<IActionResult> GetFieldDefinitionById(Guid id)
    {
        try
        {
            var field = await _visitDetailService.GetFieldDefinitionByIdAsync(id);
            if (field == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.FieldNotFound")));
            return Ok(field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting field definition {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.FieldLoadError"), ex));
        }
    }

    /// <summary>
    /// Alan tanımı oluşturur
    /// </summary>
    [HttpPost("fields")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateFieldDefinition([FromBody] SaveVisitFieldDefinitionDto dto)
    {
        try
        {
            var field = await _visitDetailService.CreateFieldDefinitionAsync(dto);
            return Ok(field);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating field definition");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.FieldCreateError"), ex));
        }
    }

    /// <summary>
    /// Alan tanımı günceller
    /// </summary>
    [HttpPut("fields/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateFieldDefinition(Guid id, [FromBody] SaveVisitFieldDefinitionDto dto)
    {
        try
        {
            var field = await _visitDetailService.UpdateFieldDefinitionAsync(id, dto);
            return Ok(field);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating field definition {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.FieldUpdateError"), ex));
        }
    }

    /// <summary>
    /// Alan tanımı siler
    /// </summary>
    [HttpDelete("fields/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteFieldDefinition(Guid id)
    {
        try
        {
            await _visitDetailService.DeleteFieldDefinitionAsync(id);
            return Ok(new { message = await _localizationService.GetResourceAsync("Api.VisitDetail.FieldDeleteSuccess") });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting field definition {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.FieldDeleteError"), ex));
        }
    }

    #endregion

    #region Değer İşlemleri

    /// <summary>
    /// Bir ziyaretin tüm detaylarını getirir
    /// </summary>
    [HttpGet("values/{customerVisitId}")]
    public async Task<IActionResult> GetVisitDetails(Guid customerVisitId)
    {
        try
        {
            var details = await _visitDetailService.GetVisitDetailsAsync(customerVisitId);
            return Ok(details);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visit details for {CustomerVisitId}", customerVisitId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.DetailsLoadError"), ex));
        }
    }

    /// <summary>
    /// Bir ziyaretin detay özetini getirir
    /// </summary>
    [HttpGet("values/{customerVisitId}/summary")]
    public async Task<IActionResult> GetVisitSummary(Guid customerVisitId)
    {
        try
        {
            var summary = await _visitDetailService.GetVisitSummaryAsync(customerVisitId);
            return Ok(summary);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visit summary for {CustomerVisitId}", customerVisitId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.SummaryLoadError"), ex));
        }
    }

    /// <summary>
    /// Ziyaret detaylarını kaydeder (toplu)
    /// </summary>
    [HttpPost("values")]
    [Authorize(Roles = "Admin,TeamLeader,CustomerRepresentative,FieldWorker")]
    public async Task<IActionResult> SaveVisitDetails([FromBody] SaveVisitDetailsDto dto)
    {
        try
        {
            await _visitDetailService.SaveVisitDetailsAsync(dto);
            _logger.LogInformation("Visit details saved for {CustomerVisitId}, {Count} values",
                dto.CustomerVisitId, dto.Values.Count);
            return Ok(new { message = await _localizationService.GetResourceAsync("Api.VisitDetail.DetailsSaveSuccess"), count = dto.Values.Count });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving visit details for {CustomerVisitId}", dto.CustomerVisitId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.DetailsSaveError"), ex));
        }
    }

    /// <summary>
    /// Tek bir alan değerini günceller
    /// </summary>
    [HttpPut("values/{customerVisitId}/field/{fieldDefinitionId}")]
    [Authorize(Roles = "Admin,TeamLeader,CustomerRepresentative,FieldWorker")]
    public async Task<IActionResult> UpdateFieldValue(Guid customerVisitId, Guid fieldDefinitionId, [FromBody] object? value)
    {
        try
        {
            await _visitDetailService.UpdateFieldValueAsync(customerVisitId, fieldDefinitionId, value);
            _logger.LogInformation("Field {FieldDefinitionId} updated for visit {CustomerVisitId}", fieldDefinitionId, customerVisitId);
            return Ok(new { message = await _localizationService.GetResourceAsync("Api.VisitDetail.FieldValueUpdateSuccess") });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating field {FieldDefinitionId} for visit {CustomerVisitId}", fieldDefinitionId, customerVisitId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.FieldValueUpdateError"), ex));
        }
    }

    #endregion

    #region Sorgulama ve İstatistik

    /// <summary>
    /// Belirli bir alandaki değere göre ziyaretleri filtreler
    /// </summary>
    [HttpGet("filter")]
    public async Task<IActionResult> FilterVisitsByFieldValue(
        [FromQuery] Guid fieldDefinitionId,
        [FromQuery] string? value,
        [FromQuery] string @operator = "eq")
    {
        try
        {
            var visitIds = await _visitDetailService.FilterVisitsByFieldValueAsync(fieldDefinitionId, value, @operator);
            return Ok(visitIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering visits by field {FieldDefinitionId}", fieldDefinitionId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.FilterError"), ex));
        }
    }

    /// <summary>
    /// Belirli bir alan için istatistikleri getirir
    /// </summary>
    [HttpGet("statistics/{fieldDefinitionId}")]
    public async Task<IActionResult> GetFieldStatistics(
        Guid fieldDefinitionId,
        [FromQuery] Guid? projectId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var stats = await _visitDetailService.GetFieldStatisticsAsync(fieldDefinitionId, projectId, fromDate, toDate);
            return Ok(stats);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for field {FieldDefinitionId}", fieldDefinitionId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.StatisticsLoadError"), ex));
        }
    }

    /// <summary>
    /// Birden fazla ziyareti karşılaştırır
    /// </summary>
    [HttpPost("compare")]
    public async Task<IActionResult> CompareVisits([FromBody] IEnumerable<Guid> customerVisitIds)
    {
        try
        {
            var comparison = await _visitDetailService.CompareVisitsAsync(customerVisitIds);
            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing visits");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.VisitDetail.CompareError"), ex));
        }
    }

    #endregion
}
