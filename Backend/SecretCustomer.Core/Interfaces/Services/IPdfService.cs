namespace SecretCustomer.Core.Interfaces.Services;

/// <summary>
/// PDF oluşturma servisi interface
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// HTML içeriğinden PDF oluşturur
    /// </summary>
    /// <param name="html">PDF'e dönüştürülecek HTML içeriği</param>
    /// <param name="css">Opsiyonel CSS stilleri</param>
    /// <returns>PDF dosyası byte dizisi</returns>
    Task<byte[]> GeneratePdfFromHtmlAsync(string html, string? css = null);
}
