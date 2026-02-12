using System.Security.Cryptography;
using System.Text;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Helpers;

/// <summary>
/// AES şifreleme yardımcısı - URL-safe token oluşturma
/// </summary>
public static class EncryptionHelper
{
    // 32 byte = 256 bit key for AES-256
    private static readonly byte[] DefaultKey = Encoding.UTF8.GetBytes("SecretCustomer2024SurveyTokenKey"); // Exactly 32 chars

    /// <summary>
    /// Payload'ı şifrele ve URL-safe Base64 token döndür
    /// </summary>
    public static string Encrypt(string payload, byte[]? key = null)
    {
        key ??= DefaultKey;

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var encryptedBytes = encryptor.TransformFinalBlock(payloadBytes, 0, payloadBytes.Length);

        // IV + EncryptedData birleştir
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        // URL-safe Base64
        return Convert.ToBase64String(result)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    /// <summary>
    /// URL-safe Base64 token'ı çöz ve payload döndür
    /// </summary>
    public static string? Decrypt(string token, byte[]? key = null)
    {
        try
        {
            key ??= DefaultKey;

            // URL-safe Base64'ü normale çevir
            var base64 = token
                .Replace("-", "+")
                .Replace("_", "/");

            // Padding ekle
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            var fullCipher = Convert.FromBase64String(base64);

            using var aes = Aes.Create();
            aes.Key = key;

            // IV'yi ayır (ilk 16 byte)
            var iv = new byte[16];
            var cipherBytes = new byte[fullCipher.Length - 16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
            Buffer.BlockCopy(fullCipher, 16, cipherBytes, 0, cipherBytes.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Survey token oluştur: projectId:personnelId:timestamp
    /// </summary>
    public static string CreateSurveyToken(int projectId, int personnelId)
    {
        var payload = $"{projectId}:{personnelId}:{TurkeyTime.Now.Ticks}";
        return Encrypt(payload);
    }

    /// <summary>
    /// Survey token'ı çöz
    /// </summary>
    public static SurveyTokenData? ParseSurveyToken(string token)
    {
        var payload = Decrypt(token);
        if (string.IsNullOrEmpty(payload)) return null;

        var parts = payload.Split(':');
        if (parts.Length != 3) return null;

        if (!int.TryParse(parts[0], out var projectId)) return null;
        if (!int.TryParse(parts[1], out var personnelId)) return null;
        if (!long.TryParse(parts[2], out var ticks)) return null;

        return new SurveyTokenData
        {
            ProjectId = projectId,
            PersonnelId = personnelId,
            CreatedAt = new DateTime(ticks, DateTimeKind.Utc)
        };
    }
}

/// <summary>
/// Survey token verisi
/// </summary>
public class SurveyTokenData
{
    public int ProjectId { get; set; }
    public int PersonnelId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Token süresi dolmuş mu?
    /// </summary>
    public bool IsExpired(int expirationDays = 30)
    {
        return CreatedAt.AddDays(expirationDays) < TurkeyTime.Now;
    }

    /// <summary>
    /// Belirli tarihe göre süresi dolmuş mu?
    /// </summary>
    public bool IsExpiredByDate(DateTime? expirationDate)
    {
        if (!expirationDate.HasValue) return false;
        return TurkeyTime.Now > expirationDate.Value;
    }
}
