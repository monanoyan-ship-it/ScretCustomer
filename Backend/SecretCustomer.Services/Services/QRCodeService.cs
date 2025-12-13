using QRCoder;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class QRCodeService : IQRCodeService
{
    public byte[] GenerateQRCode(string url, int pixelPerModule = 10)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(pixelPerModule);
    }

    public byte[] GenerateAssignmentQRCode(string uniqueLink, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(uniqueLink))
            throw new ArgumentException("Unique link cannot be empty", nameof(uniqueLink));

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be empty", nameof(baseUrl));

        // Frontend form URL'ini oluştur
        var fullUrl = $"{baseUrl.TrimEnd('/')}/form/{uniqueLink}";

        return GenerateQRCode(fullUrl, pixelPerModule: 10);
    }

    public string GenerateQRCodeBase64(string url, int pixelPerModule = 10)
    {
        var qrBytes = GenerateQRCode(url, pixelPerModule);
        return Convert.ToBase64String(qrBytes);
    }
}
