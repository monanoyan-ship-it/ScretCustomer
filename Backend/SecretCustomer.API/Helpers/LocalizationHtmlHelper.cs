using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Helpers;

public static class LocalizationHtmlHelper
{
    /// <summary>
    /// T() helper - NopCommerce tarzı çeviri fonksiyonu
    /// Kullanım: @Html.T("Common.Save") veya @Html.T("Login.Title", "Giriş Yap")
    /// </summary>
    public static IHtmlContent T(this IHtmlHelper htmlHelper, string resourceName, string? defaultValue = null)
    {
        var localizationService = htmlHelper.ViewContext.HttpContext.RequestServices
            .GetService<ILocalizationService>();

        if (localizationService == null)
            return new HtmlString(defaultValue ?? resourceName);

        Guid? langId = null; // Explicitly typed as Guid?
        var value = localizationService.GetResourceAsync(resourceName, langId, defaultValue).GetAwaiter().GetResult();
        return new HtmlString(value);
    }

    /// <summary>
    /// Belirli bir dil için çeviri
    /// </summary>
    public static IHtmlContent T(this IHtmlHelper htmlHelper, string resourceName, Guid languageId, string? defaultValue = null)
    {
        var localizationService = htmlHelper.ViewContext.HttpContext.RequestServices
            .GetService<ILocalizationService>();

        if (localizationService == null)
            return new HtmlString(defaultValue ?? resourceName);

        var value = localizationService.GetResourceAsync(resourceName, languageId, defaultValue).GetAwaiter().GetResult();
        return new HtmlString(value);
    }

    /// <summary>
    /// Format destekli çeviri
    /// Kullanım: @Html.TFormat("Validation.MinLength", 6) => "En az 6 karakter olmalıdır"
    /// </summary>
    public static IHtmlContent TFormat(this IHtmlHelper htmlHelper, string resourceName, params object[] args)
    {
        var localizationService = htmlHelper.ViewContext.HttpContext.RequestServices
            .GetService<ILocalizationService>();

        if (localizationService == null)
            return new HtmlString(resourceName);

        var value = localizationService.GetResourceAsync(resourceName).GetAwaiter().GetResult();

        try
        {
            var formatted = string.Format(value, args);
            return new HtmlString(formatted);
        }
        catch
        {
            return new HtmlString(value);
        }
    }

    /// <summary>
    /// Mevcut dil kodunu al
    /// </summary>
    public static string CurrentLanguageCode(this IHtmlHelper htmlHelper)
    {
        var localizationService = htmlHelper.ViewContext.HttpContext.RequestServices
            .GetService<ILocalizationService>();

        return localizationService?.GetCurrentLanguageCode() ?? "tr";
    }

    /// <summary>
    /// Mevcut dil ID'sini al
    /// </summary>
    public static Guid CurrentLanguageId(this IHtmlHelper htmlHelper)
    {
        var localizationService = htmlHelper.ViewContext.HttpContext.RequestServices
            .GetService<ILocalizationService>();

        return localizationService?.GetCurrentLanguageId() ?? Guid.Empty;
    }

    /// <summary>
    /// Dil RTL mi?
    /// </summary>
    public static bool IsRtl(this IHtmlHelper htmlHelper)
    {
        var localizationService = htmlHelper.ViewContext.HttpContext.RequestServices
            .GetService<ILocalizationService>();

        if (localizationService == null)
            return false;

        var langId = localizationService.GetCurrentLanguageId();
        var language = localizationService.GetLanguageByIdAsync(langId).GetAwaiter().GetResult();

        return language?.Rtl ?? false;
    }
}
