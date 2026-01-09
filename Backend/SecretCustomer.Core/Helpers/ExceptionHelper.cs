namespace SecretCustomer.Core.Helpers;

/// <summary>
/// Exception işlemleri için yardımcı sınıf
/// </summary>
public static class ExceptionHelper
{
    /// <summary>
    /// En içteki exception'ı bulur
    /// </summary>
    public static Exception GetInnermostException(Exception ex)
    {
        while (ex.InnerException != null)
        {
            ex = ex.InnerException;
        }
        return ex;
    }

    /// <summary>
    /// Tüm exception zincirini string olarak döndürür
    /// </summary>
    public static string GetFullExceptionChain(Exception ex)
    {
        var messages = new List<string>();
        var current = ex;
        var depth = 0;

        while (current != null)
        {
            var prefix = depth == 0 ? "Hata" : $"İç Hata ({depth})";
            messages.Add($"{prefix}: {current.Message}");
            current = current.InnerException;
            depth++;
        }

        return string.Join(" → ", messages);
    }
}
