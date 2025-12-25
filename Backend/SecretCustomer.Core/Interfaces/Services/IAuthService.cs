using SecretCustomer.Core.DTOs.Auth;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
    Task<LoginResponseDto> RegisterAsync(RegisterDto dto);

    // Profile methods
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<ChangePasswordResultDto> ChangePasswordAsync(int userId, ChangePasswordDto dto);

    // Password Reset methods
    Task<ForgotPasswordResultDto> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<ResetPasswordResultDto> ResetPasswordAsync(ResetPasswordDto dto);

    // ===== IMPERSONATION (Video 17 - Firma Olarak Görüntüle) =====

    /// <summary>
    /// Müşteri olarak görüntülemeye başla
    /// </summary>
    Task<ImpersonationResultDto> StartImpersonationAsync(int adminUserId, int customerId);

    /// <summary>
    /// Müşteri olarak görüntülemeyi sonlandır
    /// </summary>
    Task<ImpersonationResultDto> EndImpersonationAsync(int originalUserId);

    /// <summary>
    /// İmpersonation için müşteri listesini getir
    /// </summary>
    Task<IEnumerable<CustomerListItemDto>> GetCustomersForImpersonationAsync();
}
