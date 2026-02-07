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
    private readonly ILogger<EvaluationNotificationJob> _logger;
    private readonly int _targetHour = 19; // 19:00
    private readonly int _targetMinute = 0;

    public EvaluationNotificationJob(
        IServiceProvider serviceProvider,
        ILogger<EvaluationNotificationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EvaluationNotificationJob started (target: 19:00 Turkey Time)");

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
                    _logger.LogInformation("Running scheduled evaluation notifications at {Time}", turkeyTime);

                    using var scope = _serviceProvider.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<IEvaluationNotificationService>();

                    await notificationService.ProcessScheduledNotificationsAsync();

                    _logger.LogInformation("Scheduled evaluation notifications completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EvaluationNotificationJob");
            }

            // Her dakika kontrol et
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("EvaluationNotificationJob stopped");
    }
}
