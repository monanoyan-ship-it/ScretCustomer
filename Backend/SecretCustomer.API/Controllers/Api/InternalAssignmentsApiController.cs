using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Assignment;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// İç Değerlendirme Atamaları API Controller
/// Firma personeline checklist atama işlemleri
/// </summary>
[Route("api/internal-assignments")]
[ApiController]
[Authorize]
public class InternalAssignmentsApiController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;
    private readonly ICustomerService _customerService;
    private readonly IProjectService _projectService;
    private readonly ICustomerPersonnelService _customerPersonnelService;
    private readonly ILogger<InternalAssignmentsApiController> _logger;

    public InternalAssignmentsApiController(
        IAssignmentService assignmentService,
        ICustomerService customerService,
        IProjectService projectService,
        ICustomerPersonnelService customerPersonnelService,
        ILogger<InternalAssignmentsApiController> logger)
    {
        _assignmentService = assignmentService;
        _customerService = customerService;
        _projectService = projectService;
        _customerPersonnelService = customerPersonnelService;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcı CustomerPersonnel ise ve customerId parametresi varsa,
    /// sadece kendi şirketinin verisine erişebildiğini doğrular.
    /// Admin kullanıcılar için her zaman true döner.
    /// </summary>
    private bool ValidateCustomerAccess(int? requestedCustomerId)
    {
        var userType = User.FindFirst("UserType")?.Value;

        // Admin kullanıcılar her şeye erişebilir
        if (userType != "CustomerPersonnel")
            return true;

        // CustomerPersonnel için customerId zorunlu ve token'daki ile eşleşmeli
        var tokenCustomerId = User.FindFirst("CustomerId")?.Value;
        if (string.IsNullOrEmpty(tokenCustomerId))
            return false;

        // Eğer customerId parametresi verilmemişse, erişim yok
        if (!requestedCustomerId.HasValue)
            return false;

        return tokenCustomerId == requestedCustomerId.Value.ToString();
    }

    /// <summary>
    /// Kullanıcının CustomerPersonnel olup olmadığını döner
    /// </summary>
    private bool IsCustomerPersonnel()
    {
        return User.FindFirst("UserType")?.Value == "CustomerPersonnel";
    }

    /// <summary>
    /// Token'dan customer ID'yi alır (sadece CustomerPersonnel için)
    /// </summary>
    private int? GetTokenCustomerId()
    {
        var customerId = User.FindFirst("CustomerId")?.Value;
        return int.TryParse(customerId, out var id) ? id : null;
    }

    /// <summary>
    /// İç değerlendirme atamalarını listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] InternalAssignmentFilterDto filter)
    {
        try
        {
            // CustomerPersonnel güvenlik kontrolü
            if (!ValidateCustomerAccess(filter.CustomerId))
            {
                return Forbid();
            }

            var assignments = await _assignmentService.GetInternalAssignmentsAsync(filter);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting internal assignments");
            return StatusCode(500, new { message = "İç değerlendirme atamaları alınırken hata oluştu" });
        }
    }

    /// <summary>
    /// İç değerlendirme özeti
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int? customerId)
    {
        try
        {
            // CustomerPersonnel güvenlik kontrolü
            if (!ValidateCustomerAccess(customerId))
            {
                return Forbid();
            }

            var summary = await _assignmentService.GetInternalAssignmentSummaryAsync(customerId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting internal assignment summary");
            return StatusCode(500, new { message = "Özet bilgisi alınırken hata oluştu" });
        }
    }

    /// <summary>
    /// Toplu iç değerlendirme ataması oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInternalAssignmentsDto dto)
    {
        try
        {
            // CustomerPersonnel güvenlik kontrolü
            if (!ValidateCustomerAccess(dto.CustomerId))
            {
                return Forbid();
            }

            var result = await _assignmentService.CreateInternalAssignmentsAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found while creating internal assignments");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating internal assignments");
            var innerMessage = ExceptionHelper.GetFullExceptionChain(ex);
            return StatusCode(500, new { message = "Atamalar oluşturulurken hata oluştu: " + innerMessage });
        }
    }

    /// <summary>
    /// Atama için kullanılabilir müşterileri getir
    /// CustomerPersonnel kullanıcılar sadece kendi müşterisini görür
    /// </summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers()
    {
        try
        {
            // CustomerPersonnel ise sadece kendi müşterisini göster
            if (IsCustomerPersonnel())
            {
                var tokenCustomerId = GetTokenCustomerId();
                if (!tokenCustomerId.HasValue)
                    return Forbid();

                var customer = await _customerService.GetByIdAsync(tokenCustomerId.Value);
                if (customer == null)
                    return Ok(Array.Empty<object>());

                return Ok(new[] { new { id = customer.Id, name = customer.CompanyName, personnelCount = customer.PersonnelCount } });
            }

            var customers = await _customerService.GetAllAsync();
            var result = customers.Select(c => new
            {
                id = c.Id,
                name = c.CompanyName,
                personnelCount = c.PersonnelCount
            });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            return StatusCode(500, new { message = "Müşteriler alınırken hata oluştu" });
        }
    }

    /// <summary>
    /// Atama için kullanılabilir projeleri getir
    /// </summary>
    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects([FromQuery] int? customerId)
    {
        try
        {
            // CustomerPersonnel güvenlik kontrolü
            if (!ValidateCustomerAccess(customerId))
            {
                return Forbid();
            }

            IEnumerable<Core.DTOs.Project.ProjectDto> projects;

            if (customerId.HasValue)
            {
                projects = await _projectService.GetByCustomerIdAsync(customerId.Value);
            }
            else
            {
                projects = await _projectService.GetAllAsync();
            }

            // Sadece aktif ve checklist'i olan projeleri getir
            var result = projects
                .Where(p => p.IsActive && p.ChecklistId > 0)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    code = p.Code,
                    checklistName = p.ChecklistName,
                    customerId = p.CustomerId
                });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projects");
            return StatusCode(500, new { message = "Projeler alınırken hata oluştu" });
        }
    }

    /// <summary>
    /// Müşterinin personellerini getir (atama için)
    /// </summary>
    [HttpGet("customers/{customerId}/personnel")]
    public async Task<IActionResult> GetPersonnel(int customerId, [FromQuery] int? roleFilter, [FromQuery] int? organizationId)
    {
        try
        {
            // CustomerPersonnel güvenlik kontrolü
            if (!ValidateCustomerAccess(customerId))
            {
                return Forbid();
            }

            var personnel = await _customerPersonnelService.GetByCustomerIdAsync(customerId);

            // İç değerlendirme sadece Yönetici (1) ve Süpervizör (2) için yapılabilir
            // Operatörler (3) hariç tutuluyor
            personnel = personnel.Where(p => {
                var roleId = CustomerPersonnelRoles.GetBySystemName(p.Role)?.Id ?? 0;
                return roleId <= CustomerPersonnelRoles.Ids.Supervisor;
            });

            // Ek rol filtresi (kullanıcı seçimi)
            if (roleFilter.HasValue)
            {
                personnel = personnel.Where(p => {
                    var roleId = CustomerPersonnelRoles.GetBySystemName(p.Role)?.Id ?? 0;
                    return roleId == roleFilter.Value;
                });
            }

            // Organizasyon filtresi
            if (organizationId.HasValue)
            {
                personnel = personnel.Where(p => p.OrganizationIds.Contains(organizationId.Value));
            }

            var result = personnel.Select(p => new
            {
                id = p.Id,
                fullName = p.FullName,
                role = p.Role,
                roleName = p.RoleName,
                organizationName = p.OrganizationName,
                email = p.Email
            });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personnel for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Personeller alınırken hata oluştu" });
        }
    }
}
