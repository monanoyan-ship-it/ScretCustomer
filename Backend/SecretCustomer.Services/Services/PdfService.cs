using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class PdfService : IPdfService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PdfService> _logger;
    private readonly string _pdfServiceUrl;

    public PdfService(HttpClient httpClient, IConfiguration configuration, ILogger<PdfService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _pdfServiceUrl = configuration["PdfService:Url"] ?? "http://localhost:5060";
    }

    public async Task<byte[]> GeneratePdfFromHtmlAsync(string html, string? css = null)
    {
        try
        {
            var request = new
            {
                html = html,
                css = css,
                filename = "report.pdf"
            };

            var response = await _httpClient.PostAsJsonAsync($"{_pdfServiceUrl}/generate", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("PDF generation failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new Exception($"PDF generation failed: {error}");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to PDF service at {Url}", _pdfServiceUrl);
            throw new Exception("PDF servisi şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyin.", ex);
        }
    }
}
