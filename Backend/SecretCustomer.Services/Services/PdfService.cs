using System.Diagnostics;
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
    private readonly string _scheduledTaskName;

    public PdfService(HttpClient httpClient, IConfiguration configuration, ILogger<PdfService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _pdfServiceUrl = configuration["PdfService:Url"] ?? "http://localhost:5060";
        _scheduledTaskName = configuration["PdfService:TaskName"] ?? "PdfService";
    }

    public async Task<byte[]> GeneratePdfFromHtmlAsync(string html, string? css = null)
    {
        var request = new
        {
            html = html,
            css = css,
            filename = "report.pdf"
        };

        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await SendPdfRequestAsync(request);
            }
            catch (HttpRequestException ex)
            {
                if (attempt == maxRetries)
                {
                    _logger.LogError(ex, "PDF servisi {MaxRetries} deneme sonrası erişilemedi: {Url}", maxRetries, _pdfServiceUrl);
                    throw new Exception("PDF servisi başlatılamadı. Lütfen daha sonra tekrar deneyin.", ex);
                }

                _logger.LogWarning("PDF servisi erişilemedi (deneme {Attempt}/{MaxRetries}), scheduled task ile yeniden başlatılıyor...", attempt, maxRetries);
                StartScheduledTask();
                await Task.Delay(5000);
            }
        }

        throw new Exception("PDF servisi başlatılamadı.");
    }

    private async Task<byte[]> SendPdfRequestAsync(object request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_pdfServiceUrl}/generate", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("PDF generation failed: {StatusCode} - {Error}", response.StatusCode, error);
            throw new Exception($"PDF generation failed: {error}");
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    private void StartScheduledTask()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/run /tn \"{_scheduledTaskName}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            process?.WaitForExit(5000);
            _logger.LogInformation("PdfService scheduled task '{TaskName}' çalıştırıldı", _scheduledTaskName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled task '{TaskName}' çalıştırılamadı", _scheduledTaskName);
        }
    }
}
