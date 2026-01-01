using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Auth;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Services.Helpers;

namespace SecretCustomer.Services.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtHelper _jwtHelper;
    private readonly ApplicationDbContext _context;

    public AuthService(IUserRepository userRepository, JwtHelper jwtHelper, ApplicationDbContext context)
    {
        _userRepository = userRepository;
        _jwtHelper = jwtHelper;
        _context = context;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        // Önce User tablosunu kontrol et (bizim personelimiz)
        var user = await _userRepository.GetByUsernameAsync(dto.Username);
        if (user != null)
        {
            if (!VerifyPassword(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Kullanıcı adı veya şifre hatalı");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Hesabınız pasif durumda. Yöneticinize başvurun.");

            // Son giriş tarihini güncelle
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            var token = _jwtHelper.GenerateToken(user);

            return new LoginResponseDto
            {
                Token = token,
                UserId = user.Id,
                Username = user.Username,
                FullName = $"{user.FirstName} {user.LastName}",
                Role = user.Role.ToString()
            };
        }

        // User bulunamadıysa CustomerPersonnel tablosunu kontrol et (müşteri personeli)
        var customerPersonnel = await _context.CustomerPersonnel
            .Include(cp => cp.Customer)
            .FirstOrDefaultAsync(cp => cp.Username == dto.Username && !cp.IsDeleted);

        if (customerPersonnel != null)
        {
            if (!VerifyPassword(dto.Password, customerPersonnel.PasswordHash))
                throw new UnauthorizedAccessException("Kullanıcı adı veya şifre hatalı");

            if (!customerPersonnel.IsActive)
                throw new UnauthorizedAccessException("Hesabınız pasif durumda. Yöneticinize başvurun.");

            var token = _jwtHelper.GenerateCustomerPersonnelToken(customerPersonnel);

            return new LoginResponseDto
            {
                Token = token,
                UserId = customerPersonnel.Id,
                Username = customerPersonnel.Username,
                FullName = customerPersonnel.FullName,
                Role = customerPersonnel.Role.ToString(),
                CustomerId = customerPersonnel.CustomerId,
                CustomerName = customerPersonnel.Customer?.CompanyName
            };
        }

        throw new UnauthorizedAccessException("Kullanıcı adı veya şifre hatalı");
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
            Role = Enum.Parse<UserRole>(dto.Role)
        };

        var created = await _userRepository.CreateAsync(user);
        var token = _jwtHelper.GenerateToken(created);

        return new LoginResponseDto
        {
            Token = token,
            UserId = created.Id,
            Username = created.Username,
            FullName = $"{created.FirstName} {created.LastName}",
            Role = created.Role.ToString()
        };
    }

    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return null;

        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return null;

        // Update only allowed fields
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;

        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
        {
            // Check if email already exists
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
                throw new InvalidOperationException("Bu e-posta adresi zaten kullanılıyor.");
            user.Email = dto.Email;
        }

        if (dto.PhoneNumber != null)
            user.PhoneNumber = dto.PhoneNumber;

        await _userRepository.UpdateAsync(user);

        return await GetProfileAsync(userId);
    }

    public async Task<ChangePasswordResultDto> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return new ChangePasswordResultDto
            {
                Success = false,
                Message = "Kullanıcı bulunamadı."
            };

        // Verify current password
        if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            return new ChangePasswordResultDto
            {
                Success = false,
                Message = "Mevcut şifre yanlış."
            };

        // Validate new password
        if (dto.NewPassword != dto.ConfirmPassword)
            return new ChangePasswordResultDto
            {
                Success = false,
                Message = "Yeni şifreler eşleşmiyor."
            };

        if (dto.NewPassword.Length < 6)
            return new ChangePasswordResultDto
            {
                Success = false,
                Message = "Yeni şifre en az 6 karakter olmalıdır."
            };

        // Update password
        user.PasswordHash = HashPassword(dto.NewPassword);
        await _userRepository.UpdateAsync(user);

        return new ChangePasswordResultDto
        {
            Success = true,
            Message = "Şifreniz başarıyla değiştirildi."
        };
    }

    public async Task<ForgotPasswordResultDto> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        // Güvenlik: Kullanıcı bulunamasa bile aynı mesajı döndür
        if (user == null)
        {
            return new ForgotPasswordResultDto
            {
                Success = true,
                Message = "Eğer bu e-posta adresi sistemde kayıtlıysa, şifre sıfırlama bağlantısı gönderildi."
            };
        }

        // Token oluştur (6 haneli kod)
        var resetToken = GenerateResetToken();
        var tokenExpiry = DateTime.UtcNow.AddHours(1); // 1 saat geçerli

        // Token'ı kaydet
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = tokenExpiry;
        await _userRepository.UpdateAsync(user);

        // TODO: Production'da e-posta gönder
        // await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken);

        return new ForgotPasswordResultDto
        {
            Success = true,
            Message = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.",
            ResetToken = resetToken // Development için - Production'da kaldırılmalı
        };
    }

    public async Task<ResetPasswordResultDto> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        if (user == null)
        {
            return new ResetPasswordResultDto
            {
                Success = false,
                Message = "Geçersiz veya süresi dolmuş token."
            };
        }

        // Token kontrolü
        if (user.PasswordResetToken != dto.Token)
        {
            return new ResetPasswordResultDto
            {
                Success = false,
                Message = "Geçersiz veya süresi dolmuş token."
            };
        }

        // Token süresi kontrolü
        if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            return new ResetPasswordResultDto
            {
                Success = false,
                Message = "Şifre sıfırlama bağlantısının süresi dolmuş. Lütfen yeni bir istek oluşturun."
            };
        }

        // Şifre kontrolü
        if (dto.NewPassword != dto.ConfirmPassword)
        {
            return new ResetPasswordResultDto
            {
                Success = false,
                Message = "Şifreler eşleşmiyor."
            };
        }

        if (dto.NewPassword.Length < 6)
        {
            return new ResetPasswordResultDto
            {
                Success = false,
                Message = "Şifre en az 6 karakter olmalıdır."
            };
        }

        // Şifreyi güncelle ve token'ı temizle
        user.PasswordHash = HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        await _userRepository.UpdateAsync(user);

        return new ResetPasswordResultDto
        {
            Success = true,
            Message = "Şifreniz başarıyla değiştirildi. Yeni şifrenizle giriş yapabilirsiniz."
        };
    }

    private string GenerateResetToken()
    {
        // 6 haneli rastgele kod
        var random = new Random();
        return random.Next(100000, 999999).ToString();
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

    // ===== IMPERSONATION (Video 17 - Firma Olarak Görüntüle) =====

    public async Task<ImpersonationResultDto> StartImpersonationAsync(int adminUserId, int customerId)
    {
        // Kullaniciyi getir
        var user = await _userRepository.GetByIdAsync(adminUserId);
        if (user == null)
        {
            return new ImpersonationResultDto
            {
                Success = false,
                Message = "Kullanici bulunamadi."
            };
        }

        // Sadece Admin impersonate edebilir
        if (user.Role != UserRole.Admin)
        {
            return new ImpersonationResultDto
            {
                Success = false,
                Message = "Bu islemi yapmaya yetkiniz yok."
            };
        }

        // Musteriyi getir
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted);

        if (customer == null)
        {
            return new ImpersonationResultDto
            {
                Success = false,
                Message = "Musteri bulunamadi."
            };
        }

        // Impersonation token olustur
        var token = _jwtHelper.GenerateImpersonationToken(user, customerId, customer.CompanyName);

        return new ImpersonationResultDto
        {
            Success = true,
            Message = $"'{customer.CompanyName}' olarak goruntuluyor.",
            Token = token,
            ImpersonationInfo = new ImpersonationInfoDto
            {
                IsImpersonating = true,
                OriginalUserId = user.Id,
                OriginalUserName = $"{user.FirstName} {user.LastName}",
                OriginalRole = user.Role.ToString(),
                CustomerId = customerId,
                CustomerName = customer.CompanyName
            }
        };
    }

    public async Task<ImpersonationResultDto> EndImpersonationAsync(int originalUserId)
    {
        // Orijinal kullaniciyi getir
        var user = await _userRepository.GetByIdAsync(originalUserId);
        if (user == null)
        {
            return new ImpersonationResultDto
            {
                Success = false,
                Message = "Orijinal kullanici bulunamadi."
            };
        }

        // Normal token olustur
        var token = _jwtHelper.GenerateToken(user);

        return new ImpersonationResultDto
        {
            Success = true,
            Message = "Normal gorunume donuldu.",
            Token = token,
            ImpersonationInfo = new ImpersonationInfoDto
            {
                IsImpersonating = false
            }
        };
    }

    public async Task<IEnumerable<CustomerListItemDto>> GetCustomersForImpersonationAsync()
    {
        return await _context.Customers
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderBy(c => c.CompanyName)
            .Select(c => new CustomerListItemDto
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
                TaxNumber = c.TaxNumber,
                IsActive = c.IsActive
            })
            .ToListAsync();
    }
}
