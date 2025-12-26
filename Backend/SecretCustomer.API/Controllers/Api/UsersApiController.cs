using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.User;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersApiController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public UsersApiController(
        IUserService userService,
        ILogger<UsersApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _userService = userService;
        _logger = logger;
        _localizationService = localizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.LoadListError"), ex));
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var users = await _userService.GetActiveUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading active users");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.ActiveLoadError"), ex));
        }
    }

    [HttpGet("role/{role}")]
    public async Task<IActionResult> GetByRole(UserRole role)
    {
        try
        {
            var users = await _userService.GetByRoleAsync(role);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users by role");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.LoadListError"), ex));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.NotFound")));
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.LoadError"), ex));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating user");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.CreateError"), ex));
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _userService.UpdateAsync(id, dto);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating user {Id}", id);
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.UpdateError"), ex));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _userService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.DeleteError"), ex));
        }
    }

    [HttpGet("check-username/{username}")]
    public async Task<IActionResult> CheckUsername(string username)
    {
        try
        {
            var exists = await _userService.ExistsByUsernameAsync(username);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking username {Username}", username);
            return StatusCode(500, CreateErrorResponse("Kullanıcı adı kontrolü sırasında hata oluştu.", ex));
        }
    }

    [HttpGet("check-email/{email}")]
    public async Task<IActionResult> CheckEmail(string email)
    {
        try
        {
            var exists = await _userService.ExistsByEmailAsync(email);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email {Email}", email);
            return StatusCode(500, CreateErrorResponse("E-posta kontrolü sırasında hata oluştu.", ex));
        }
    }

    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (id != dto.UserId)
        {
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.IdMismatch")));
        }

        try
        {
            var result = await _userService.AdminChangePasswordAsync(id, dto.NewPassword);
            if (result)
            {
                return Ok(new { message = await _localizationService.GetResourceAsync("Api.User.PasswordChangeSuccess") });
            }

            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.PasswordChangeFailed")));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.PasswordChangeError"), ex));
        }
    }
}
