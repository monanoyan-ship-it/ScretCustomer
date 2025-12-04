namespace SecretCustomer.Core.Interfaces.Services;

public interface IQRCodeService
{
    /// <summary>
    /// URL için QR kod PNG olarak üretir
    /// </summary>
    byte[] GenerateQRCode(string url, int pixelPerModule = 10);

    /// <summary>
    /// Assignment için QR kod üretir (frontend URL ile)
    /// </summary>
    byte[] GenerateAssignmentQRCode(string uniqueLink, string baseUrl);

    /// <summary>
    /// Base64 encoded QR kod string döner (HTML'de göstermek için)
    /// </summary>
    string GenerateQRCodeBase64(string url, int pixelPerModule = 10);
}
