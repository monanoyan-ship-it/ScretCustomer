using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.BackgroundServices;

/// <summary>
/// Değerlendirme bildirimlerini zamanlı olarak gönderen background job
/// Her gün 08:00'da çalışır
/// - Günlük bildirimler: Her gün
/// - Haftalık bildirimler: Cuma günleri
/// - Aylık bildirimler: Ayın son günü
/// </summary>
public class EvaluationNotificationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EvaluationNotificationJob> _logger;
    private readonly int _targetHour = 8; // 08:00
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
        _logger.LogInformation("EvaluationNotificationJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(now,
                    TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"));

                // Hedef saate (08:00) ulaşıldı mı?
                if (turkeyTime.Hour == _targetHour && turkeyTime.Minute == _targetMinute)
                {
                    _logger.LogInformation("Running scheduled evaluation notifications at {Time}", turkeyTime);

                    using var scope = _serviceProvider.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<IEvaluationNotificationService>();

                    // Günlük bildirimler - her gün
                    await notificationService.SendDailyNotificationsAsync();

                    // Haftalık bildirimler - Cuma
                    if (turkeyTime.DayOfWeek == DayOfWeek.Friday)
                    {
                        _logger.LogInformation("Running weekly notifications (Friday)");
                        await notificationService.SendWeeklyNotificationsAsync();
                    }

                    // Aylık bildirimler - Ayın son günü
                    if (turkeyTime.Day == DateTime.DaysInMonth(turkeyTime.Year, turkeyTime.Month))
                    {
                        _logger.LogInformation("Running monthly notifications (Last day of month)");
                        await notificationService.SendMonthlyNotificationsAsync();
                    }

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
