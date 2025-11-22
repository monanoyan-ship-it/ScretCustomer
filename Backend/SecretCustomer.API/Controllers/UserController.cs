using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.User;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm kullanıcıları getirir (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        try
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, "An error occurred while retrieving users");
        }
    }

    /// <summary>
    /// ID'ye göre kullanıcı getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500, "An error occurred while retrieving the user");
        }
    }

    /// <summary>
    /// Role göre kullanıcıları getirir (Admin, TeamLeader)
    /// </summary>
    [HttpGet("role/{role}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetByRole(UserRole role)
    {
        try
        {
            var users = await _userService.GetByRoleAsync(role);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by role {Role}", role);
            return StatusCode(500, "An error occurred while retrieving users");
        }
    }

    /// <summary>
    /// Şubeye göre kullanıcıları getirir (Admin, TeamLeader)
    /// </summary>
    [HttpGet("branch/{branchId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetByBranch(Guid branchId)
    {
        try
        {
            var users = await _userService.GetByBranchAsync(branchId);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by branch {BranchId}", branchId);
            return StatusCode(500, "An error occurred while retrieving users");
        }
    }

    /// <summary>
    /// Aktif kullanıcıları getirir (Admin, TeamLeader)
    /// </summary>
    [HttpGet("active")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetActiveUsers()
    {
        try
        {
            var users = await _userService.GetActiveUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users");
            return StatusCode(500, "An error occurred while retrieving active users");
        }
    }

    /// <summary>
    /// Yeni kullanıcı oluşturur (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto createUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.CreateAsync(createUserDto);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating user");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, "An error occurred while creating the user");
        }
    }

    /// <summary>
    /// Kullanıcı bilgilerini günceller (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserDto updateUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.UpdateAsync(id, updateUserDto);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found for update", id);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating user {UserId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, "An error occurred while updating the user");
        }
    }

    /// <summary>
    /// Kullanıcıyı siler (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            await _userService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found for deletion", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, "An error occurred while deleting the user");
        }
    }

    /// <summary>
    /// Kullanıcı şifresini değiştirir
    /// </summary>
    [HttpPost("{id}/change-password")]
    public async Task<ActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _userService.ChangePasswordAsync(id, changePasswordDto);
            if (success)
                return Ok(new { message = "Password changed successfully" });

            return BadRequest("Failed to change password");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found for password change", id);
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized password change attempt for user {UserId}", id);
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", id);
            return StatusCode(500, "An error occurred while changing password");
        }
    }

    /// <summary>
    /// Kullanıcıyı şubeye atar (Admin, TeamLeader)
    /// </summary>
    [HttpPost("{userId}/assign-branch/{branchId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<UserDto>> AssignToBranch(Guid userId, Guid branchId)
    {
        try
        {
            var user = await _userService.AssignToBranchAsync(userId, branchId);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found for branch assignment", userId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning user {UserId} to branch {BranchId}", userId, branchId);
            return StatusCode(500, "An error occurred while assigning user to branch");
        }
    }

    /// <summary>
    /// Kullanıcıyı şubeden çıkarır (Admin, TeamLeader)
    /// </summary>
    [HttpPost("{userId}/remove-branch")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<UserDto>> RemoveFromBranch(Guid userId)
    {
        try
        {
            var user = await _userService.RemoveFromBranchAsync(userId);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found for branch removal", userId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from branch", userId);
            return StatusCode(500, "An error occurred while removing user from branch");
        }
    }

    /// <summary>
    /// Birden fazla kullanıcıyı şubeye atar (Admin, TeamLeader)
    /// </summary>
    [HttpPost("assign-multiple-to-branch/{branchId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<IEnumerable<UserDto>>> AssignMultipleToBranch(
        Guid branchId,
        [FromBody] List<Guid> userIds)
    {
        try
        {
            if (userIds == null || userIds.Count == 0)
                return BadRequest("User IDs list cannot be empty");

            var users = await _userService.AssignMultipleToBranchAsync(userIds, branchId);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning multiple users to branch {BranchId}", branchId);
            return StatusCode(500, "An error occurred while assigning users to branch");
        }
    }
}
