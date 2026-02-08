using System.Text.Json;
using System.Text.Json.Serialization;

namespace SecretCustomer.Core.Helpers;

/// <summary>
/// Tüm DateTime değerlerini Turkey timezone (UTC+3) olarak JSON'a serialize eder.
/// API yanıtlarındaki tarihlerin doğru saat diliminde gitmesini sağlar.
/// </summary>
public class TurkeyDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly TimeZoneInfo TurkeyTimeZone = GetTurkeyTimeZone();

    private static TimeZoneInfo GetTurkeyTimeZone()
    {
        // Windows: "Turkey Standard Time", Linux: "Europe/Istanbul"
        try { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
    }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // UTC ise Turkey time'a çevir, offset ile yaz
        if (value.Kind == DateTimeKind.Utc)
        {
            var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(value, TurkeyTimeZone);
            var offset = TurkeyTimeZone.GetUtcOffset(turkeyTime);
            var dto = new DateTimeOffset(turkeyTime, offset);
            writer.WriteStringValue(dto.ToString("yyyy-MM-ddTHH:mm:sszzz"));
        }
        else
        {
            // Local veya Unspecified - Turkey time olarak kabul et
            var offset = TurkeyTimeZone.GetUtcOffset(value);
            var dto = new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), offset);
            writer.WriteStringValue(dto.ToString("yyyy-MM-ddTHH:mm:sszzz"));
        }
    }
}

/// <summary>
/// Nullable DateTime için Turkey timezone converter.
/// </summary>
public class TurkeyNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly TurkeyDateTimeConverter Inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        return Inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            Inner.Write(writer, value.Value, options);
    }
}
