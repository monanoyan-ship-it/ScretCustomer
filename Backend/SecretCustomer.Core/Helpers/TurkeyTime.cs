namespace SecretCustomer.Core.Helpers;

/// <summary>
/// Türkiye saat diliminde (UTC+3) tarih/saat sağlar.
/// DateTime.UtcNow ve DateTime.Now yerine bu sınıfı kullanın.
/// </summary>
public static class TurkeyTime
{
    private static readonly TimeZoneInfo TurkeyTimeZone = GetTurkeyTimeZone();

    private static TimeZoneInfo GetTurkeyTimeZone()
    {
        // Windows: "Turkey Standard Time", Linux: "Europe/Istanbul"
        try { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
    }

    /// <summary>
    /// Türkiye saat diliminde şu anki tarih ve saat.
    /// </summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTimeZone);

    /// <summary>
    /// Türkiye saat diliminde bugünün tarihi (saat 00:00:00).
    /// </summary>
    public static DateTime Today => Now.Date;
}
