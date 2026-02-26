using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.BackgroundServices;

/// <summary>
/// Değerlendirme bildirimlerini zamanlı olarak gönderen background job.
/// Her gün 19:00'da (Türkiye saati) çalışır.
/// Kural bazlı: Her kuralın sıklığı ve gün ayarına göre bildirim gönderilir.
/// </summary>
public class EvaluationNotificationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly int _targetHour = 19; // 19:00
    private readonly int _targetMinute = 0;

    public EvaluationNotificationJob(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
            await auditLogService.LogInfoAsync("EvaluationNotificationJob started (target: 19:00 Turkey Time)", "BackgroundJob");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(now,
                    TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"));

                // Hedef saate (19:00) ulaşıldı mı?
                if (turkeyTime.Hour == _targetHour && turkeyTime.Minute == _targetMinute)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
                    await auditLogService.LogInfoAsync($"Running scheduled evaluation notifications at {turkeyTime}", "BackgroundJob");

                    var notificationService = scope.ServiceProvider.GetRequiredService<IEvaluationNotificationService>();

                    await notificationService.ProcessScheduledNotificationsAsync();

                    await auditLogService.LogInfoAsync("Scheduled evaluation notifications completed", "BackgroundJob");
                }
            }
            catch (Exception ex)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
                    await auditLogService.LogErrorAsync("Error in EvaluationNotificationJob", "BackgroundJob", ex);
                }
                catch
                {
                    // Loglama başarısız olursa sessizce devam et
                }
            }

            // Her dakika kontrol et
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
            await auditLogService.LogInfoAsync("EvaluationNotificationJob stopped", "BackgroundJob");
        }
        catch
        {
            // Loglama başarısız olursa sessizce devam et
        }
    }
}
