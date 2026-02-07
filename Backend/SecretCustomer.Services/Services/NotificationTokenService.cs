using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class NotificationTokenService : INotificationTokenService
{
    private readonly byte[] _key;
    private readonly ILogger<NotificationTokenService> _logger;

    public NotificationTokenService(IConfiguration configuration, ILogger<NotificationTokenService> logger)
    {
        _logger = logger;
        var keyString = configuration["NotificationToken:EncryptionKey"]
            ?? throw new InvalidOperationException("NotificationToken:EncryptionKey is not configured");

        // Key'i 32 byte (256 bit) olacak şekilde hash'le
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
    }

    public string GenerateSingleToken(int evaluationId, DateTime expiresAt)
    {
        var payload = new NotificationTokenPayload
        {
            Type = "single",
            EvaluationId = evaluationId,
            ExpiresAt = expiresAt
        };
        return Encrypt(payload);
    }

    public string GenerateBulkToken(int customerId, DateTime startDate, DateTime endDate, DateTime expiresAt)
    {
        var payload = new NotificationTokenPayload
        {
            Type = "bulk",
            CustomerId = customerId,
            StartDate = startDate,
            EndDate = endDate,
            ExpiresAt = expiresAt
        };
        return Encrypt(payload);
    }

    public string GeneratePersonnelToken(int customerPersonnelId, DateTime startDate, DateTime endDate, DateTime expiresAt)
    {
        var payload = new NotificationTokenPayload
        {
            Type = "personnel",
            CustomerPersonnelId = customerPersonnelId,
            StartDate = startDate,
            EndDate = endDate,
            ExpiresAt = expiresAt
        };
        return Encrypt(payload);
    }

    public NotificationTokenPayload? DecryptToken(string token)
    {
        try
        {
            var payload = Decrypt(token);
            if (payload == null) return null;

            if (payload.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Notification token expired. ExpiresAt: {ExpiresAt}", payload.ExpiresAt);
                return null;
            }

            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt notification token");
            return null;
        }
    }

    private string Encrypt(NotificationTokenPayload payload)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(json);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // IV + encrypted data birleştir
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        // URL-safe Base64
        return Convert.ToBase64String(result)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private NotificationTokenPayload? Decrypt(string token)
    {
        // URL-safe Base64 geri çevir
        var base64 = token
            .Replace('-', '+')
            .Replace('_', '/');

        // Padding ekle
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        var allBytes = Convert.FromBase64String(base64);

        using var aes = Aes.Create();
        aes.Key = _key;

        // İlk 16 byte IV
        var iv = new byte[16];
        Buffer.BlockCopy(allBytes, 0, iv, 0, 16);
        aes.IV = iv;

        var encryptedBytes = new byte[allBytes.Length - 16];
        Buffer.BlockCopy(allBytes, 16, encryptedBytes, 0, encryptedBytes.Length);

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        var json = Encoding.UTF8.GetString(decryptedBytes);

        return JsonSerializer.Deserialize<NotificationTokenPayload>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
