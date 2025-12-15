using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Organization;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/delegations")]
[Authorize]
public class DelegationsApiController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ILocalizationService _localizationService;

    public DelegationsApiController(
        IOrganizationService organizationService,
        ILocalizationService localizationService)
    {
        _organizationService = organizationService;
        _localizationService = localizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DelegationFilterDto filter)
    {
        try
        {
            var result = await _organizationService.GetDelegationsAsync(filter);
            return Ok(result);
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
            var delegation = await _organizationService.GetDelegationByIdAsync(id);
            if (delegation == null)
            {
                return NotFound(new { message = await _localizationService.GetResourceAsync("Api.Delegation.NotFound") });
            }
            return Ok(delegation);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my/given")]
    public async Task<IActionResult> GetMyGivenDelegations()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var delegations = await _organizationService.GetDelegationsGivenByUserAsync(userId);
            return Ok(delegations);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my/received")]
    public async Task<IActionResult> GetMyReceivedDelegations()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var delegations = await _organizationService.GetDelegationsReceivedByUserAsync(userId);
            return Ok(delegations);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my/active")]
    public async Task<IActionResult> GetMyActiveDelegations()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var delegations = await _organizationService.GetActiveDelegationsForUserAsync(userId);
            return Ok(delegations);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDelegationDto dto)
    {
        try
        {
            // If delegator is not specified, use current user
            if (dto.DelegatorUserId == Guid.Empty)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }
                dto.DelegatorUserId = userId;
            }

            var delegation = await _organizationService.CreateDelegationAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = delegation.Id }, delegation);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDelegationDto dto)
    {
        try
        {
            var delegation = await _organizationService.UpdateDelegationAsync(id, dto);
            return Ok(delegation);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveDelegationDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var delegation = await _organizationService.ApproveDelegationAsync(id, userId, dto);
            return Ok(delegation);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _organizationService.DeleteDelegationAsync(id);
            if (result)
            {
                return Ok(new { message = await _localizationService.GetResourceAsync("Api.Delegation.DeleteSuccess") });
            }
            return NotFound(new { message = await _localizationService.GetResourceAsync("Api.Delegation.NotFound") });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("types")]
    public IActionResult GetDelegationTypes()
    {
        var types = Enum.GetValues<DelegationType>()
            .Select(t => new
            {
                value = (int)t,
                name = t.ToString(),
                displayName = GetDelegationTypeName(t)
            })
            .ToList();
        return Ok(types);
    }

    [HttpGet("reasons")]
    public IActionResult GetDelegationReasons()
    {
        var reasons = Enum.GetValues<DelegationReason>()
            .Select(r => new
            {
                value = (int)r,
                name = r.ToString(),
                displayName = GetDelegationReasonName(r)
            })
            .ToList();
        return Ok(reasons);
    }

    private string GetDelegationTypeName(DelegationType type)
    {
        return type switch
        {
            DelegationType.Full => "Tam Yetki",
            DelegationType.ReadOnly => "Salt Okuma",
            DelegationType.ApprovalOnly => "Sadece Onay",
            _ => type.ToString()
        };
    }

    private string GetDelegationReasonName(DelegationReason reason)
    {
        return reason switch
        {
            DelegationReason.AnnualLeave => "Yıllık İzin",
            DelegationReason.SickLeave => "Hastalık İzni",
            DelegationReason.BusinessTrip => "İş Seyahati",
            DelegationReason.Training => "Eğitim",
            DelegationReason.Other => "Diğer",
            _ => reason.ToString()
        };
    }
}
