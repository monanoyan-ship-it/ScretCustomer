using SecretCustomer.Core.DTOs.Auth;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Services.Helpers;

namespace SecretCustomer.Services.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtHelper _jwtHelper;

    public AuthService(IUserRepository userRepository, JwtHelper jwtHelper)
    {
        _userRepository = userRepository;
        _jwtHelper = jwtHelper;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid username or password");

        // Simple password check (in production, use BCrypt or similar)
        if (!VerifyPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password");

        var token = _jwtHelper.GenerateToken(user);

        return new LoginResponseDto
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = user.Role.ToString(),
            BranchId = user.BranchId
        };
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterDto dto)
    {
        // Check if username or email already exists
        if (await _userRepository.ExistsByUsernameAsync(dto.Username))
            throw new InvalidOperationException("Username already exists");

        if (await _userRepository.ExistsByEmailAsync(dto.Email))
            throw new InvalidOperationException("Email already exists");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = Enum.Parse<UserRole>(dto.Role),
            BranchId = dto.BranchId
        };

        var created = await _userRepository.CreateAsync(user);
        var token = _jwtHelper.GenerateToken(created);

        return new LoginResponseDto
        {
            Token = token,
            UserId = created.Id,
            Username = created.Username,
            FullName = $"{created.FirstName} {created.LastName}",
            Role = created.Role.ToString(),
            BranchId = created.BranchId
        };
    }

    private string HashPassword(string password)
    {
        // PRODUCTION: Use BCrypt.Net-Next or ASP.NET Core Identity
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool VerifyPassword(string password, string hash)
    {
        // PRODUCTION: Use BCrypt.Net-Next or ASP.NET Core Identity
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
