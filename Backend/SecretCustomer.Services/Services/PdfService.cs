using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class PdfService : IPdfService
{
    private readonly HttpClient _httpClient;
    private readonly IAuditLogService _auditLogService;
    private readonly string _pdfServiceUrl;
    private readonly string _scheduledTaskName;

    public PdfService(HttpClient httpClient, IConfiguration configuration, IAuditLogService auditLogService)
    {
        _httpClient = httpClient;
        _auditLogService = auditLogService;
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
                    await _auditLogService.LogErrorAsync($"PDF servisi {maxRetries} deneme sonrası erişilemedi: {_pdfServiceUrl}", "PdfService", ex);
                    throw new Exception("PDF servisi başlatılamadı. Lütfen daha sonra tekrar deneyin.", ex);
                }

                await _auditLogService.LogWarningAsync($"PDF servisi erişilemedi (deneme {attempt}/{maxRetries}), scheduled task ile yeniden başlatılıyor...", "PdfService");
                await StartScheduledTaskAsync();
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
            await _auditLogService.LogErrorAsync($"PDF generation failed: {response.StatusCode} - {error}", "PdfService");
            throw new Exception($"PDF generation failed: {error}");
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    private async Task StartScheduledTaskAsync()
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
            await _auditLogService.LogInfoAsync($"PdfService scheduled task '{_scheduledTaskName}' çalıştırıldı", "PdfService");
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Scheduled task '{_scheduledTaskName}' çalıştırılamadı", "PdfService", ex);
        }
    }
}
