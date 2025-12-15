using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Organization;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/organizationunits")]
[Authorize]
public class OrganizationUnitsApiController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ILocalizationService _localizationService;

    public OrganizationUnitsApiController(
        IOrganizationService organizationService,
        ILocalizationService localizationService)
    {
        _organizationService = organizationService;
        _localizationService = localizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] OrganizationUnitFilterDto filter)
    {
        try
        {
            var units = await _organizationService.GetOrganizationUnitsAsync(filter);
            return Ok(units);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllUnits()
    {
        try
        {
            var units = await _organizationService.GetAllOrganizationUnitsAsync();
            return Ok(units);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        try
        {
            var tree = await _organizationService.GetOrganizationTreeAsync();
            return Ok(tree);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var unit = await _organizationService.GetOrganizationUnitByIdAsync(id);
            if (unit == null)
            {
                return NotFound(new { message = await _localizationService.GetResourceAsync("Api.OrganizationUnit.NotFound") });
            }
            return Ok(unit);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/children")]
    public async Task<IActionResult> GetChildren(Guid id)
    {
        try
        {
            var children = await _organizationService.GetChildrenAsync(id);
            return Ok(children);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationUnitDto dto)
    {
        try
        {
            var unit = await _organizationService.CreateOrganizationUnitAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = unit.Id }, unit);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationUnitDto dto)
    {
        try
        {
            var unit = await _organizationService.UpdateOrganizationUnitAsync(id, dto);
            return Ok(unit);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/move")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveOrganizationUnitRequest request)
    {
        try
        {
            var result = await _organizationService.MoveOrganizationUnitAsync(id, request.NewParentId);
            if (result)
            {
                return Ok(new { message = await _localizationService.GetResourceAsync("Api.OrganizationUnit.MoveSuccess") });
            }
            return NotFound(new { message = await _localizationService.GetResourceAsync("Api.OrganizationUnit.NotFound") });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _organizationService.DeleteOrganizationUnitAsync(id);
            if (result)
            {
                return Ok(new { message = await _localizationService.GetResourceAsync("Api.OrganizationUnit.DeleteSuccess") });
            }
            return NotFound(new { message = await _localizationService.GetResourceAsync("Api.OrganizationUnit.NotFound") });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("types")]
    public IActionResult GetUnitTypes()
    {
        var types = Enum.GetValues<OrganizationUnitType>()
            .Select(t => new
            {
                value = (int)t,
                name = t.ToString(),
                displayName = GetUnitTypeName(t)
            })
            .ToList();
        return Ok(types);
    }

    private string GetUnitTypeName(OrganizationUnitType type)
    {
        return type switch
        {
            OrganizationUnitType.Company => "Şirket",
            OrganizationUnitType.Department => "Departman",
            OrganizationUnitType.Division => "Bölüm",
            OrganizationUnitType.Team => "Takım",
            OrganizationUnitType.Unit => "Birim",
            _ => type.ToString()
        };
    }
}

public class MoveOrganizationUnitRequest
{
    public Guid? NewParentId { get; set; }
}
