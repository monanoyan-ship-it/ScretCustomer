using SecretCustomer.Core.DTOs.Auth;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
    Task<LoginResponseDto> RegisterAsync(RegisterDto dto);
}
