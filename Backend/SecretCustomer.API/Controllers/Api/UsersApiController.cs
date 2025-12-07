using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.User;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersApiController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersApiController> _logger;

    public UsersApiController(IUserService userService, ILogger<UsersApiController> logger)
    {
        _userService = userService;
        _logger = logger;
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
            return StatusCode(500, new { message = "Kullanıcılar yüklenirken bir hata oluştu." });
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
            return StatusCode(500, new { message = "Aktif kullanıcılar yüklenirken bir hata oluştu." });
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
            return StatusCode(500, new { message = "Kullanıcılar yüklenirken bir hata oluştu." });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı." });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user {Id}", id);
            return StatusCode(500, new { message = "Kullanıcı yüklenirken bir hata oluştu." });
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
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Kullanıcı oluşturulurken bir hata oluştu." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
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
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating user {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {Id}", id);
            return StatusCode(500, new { message = "Kullanıcı güncellenirken bir hata oluştu." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _userService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {Id}", id);
            return StatusCode(500, new { message = "Kullanıcı silinirken bir hata oluştu." });
        }
    }

    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (id != dto.UserId)
        {
            return BadRequest(new { message = "User ID mismatch." });
        }

        try
        {
            var result = await _userService.AdminChangePasswordAsync(id, dto.NewPassword);
            if (result)
            {
                return Ok(new { message = "Şifre başarıyla değiştirildi." });
            }

            return BadRequest(new { message = "Şifre değiştirilemedi." });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {Id}", id);
            return StatusCode(500, new { message = "Şifre değiştirilirken bir hata oluştu." });
        }
    }
}
