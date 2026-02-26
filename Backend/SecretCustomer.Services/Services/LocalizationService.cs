using System.Collections.Concurrent;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services;

public class LocalizationService : ILocalizationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    //private readonly IAuditLogService _auditLogService;
    private const string CACHE_KEY_PREFIX = "locale_";
    private const string LANGUAGES_CACHE_KEY = "all_languages";

    // Eksik key'leri takip et (aynı key'i tekrar tekrar loglamamak için)
    private static readonly ConcurrentDictionary<string, byte> _loggedMissingKeys = new();

    public LocalizationService(
        ApplicationDbContext context,
        IMemoryCache cache,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    #region Language Operations

    public async Task<IEnumerable<Language>> GetAllLanguagesAsync(bool onlyActive = true)
    {
        var cacheKey = $"{LANGUAGES_CACHE_KEY}_{onlyActive}";

        if (!_cache.TryGetValue(cacheKey, out IEnumerable<Language>? languages))
        {
            var query = _context.Languages.AsQueryable();
            if (onlyActive)
                query = query.Where(l => l.IsActive);

            languages = await query.OrderBy(l => l.DisplayOrder).ToListAsync();

            _cache.Set(cacheKey, languages, TimeSpan.FromHours(1));
        }

        return languages!;
    }

    public async Task<Language?> GetLanguageByIdAsync(int id)
    {
        return await _context.Languages.FindAsync(id);
    }

    public async Task<Language?> GetLanguageByCodeAsync(string code)
    {
        return await _context.Languages
            .FirstOrDefaultAsync(l => l.UniqueSeoCode == code || l.LanguageCulture == code);
    }

    public async Task<Language?> GetDefaultLanguageAsync()
    {
        return await _context.Languages.FirstOrDefaultAsync(l => l.IsDefault && l.IsActive)
            ?? await _context.Languages.FirstOrDefaultAsync(l => l.IsActive);
    }

    public async Task<Language> CreateLanguageAsync(Language language)
    {
        // Eğer varsayılan olarak işaretlendiyse, diğerlerini kaldır
        if (language.IsDefault)
        {
            var others = await _context.Languages.Where(l => l.IsDefault).ToListAsync();
            foreach (var other in others)
                other.IsDefault = false;
        }

        _context.Languages.Add(language);
        await _context.SaveChangesAsync();
        ClearLanguageCache();

        return language;
    }

    public async Task<Language> UpdateLanguageAsync(Language language)
    {
        var existing = await _context.Languages.FindAsync(language.Id);
        if (existing == null)
            throw new Exception("Dil bulunamadı.");

        // Eğer varsayılan olarak işaretlendiyse, diğerlerini kaldır
        if (language.IsDefault && !existing.IsDefault)
        {
            var others = await _context.Languages.Where(l => l.IsDefault && l.Id != language.Id).ToListAsync();
            foreach (var other in others)
                other.IsDefault = false;
        }

        existing.Name = language.Name;
        existing.LanguageCulture = language.LanguageCulture;
        existing.UniqueSeoCode = language.UniqueSeoCode;
        existing.FlagImageFileName = language.FlagImageFileName;
        existing.Rtl = language.Rtl;
        existing.IsDefault = language.IsDefault;
        existing.IsActive = language.IsActive;
        existing.DisplayOrder = language.DisplayOrder;
        existing.UpdatedAt = TurkeyTime.Now;

        await _context.SaveChangesAsync();
        ClearLanguageCache();

        return existing;
    }

    public async Task DeleteLanguageAsync(int id)
    {
        var language = await _context.Languages.FindAsync(id);
        if (language == null)
            return;

        if (language.IsDefault)
            throw new Exception("Varsayılan dil silinemez.");

        // İlgili kaynakları da sil
        var resources = await _context.LocaleStringResources
            .Where(r => r.LanguageId == id)
            .ToListAsync();
        _context.LocaleStringResources.RemoveRange(resources);

        _context.Languages.Remove(language);
        await _context.SaveChangesAsync();
        ClearLanguageCache();
    }

    #endregion

    #region Resource Operations

    public async Task<string> GetResourceAsync(string resourceName, int? languageId = null, string? defaultValue = null)
    {
        var langId = languageId ?? GetCurrentLanguageId();
        var cacheKey = $"{CACHE_KEY_PREFIX}{langId}_{resourceName}";

        if (!_cache.TryGetValue(cacheKey, out string? value))
        {
            var resource = await _context.LocaleStringResources
                .FirstOrDefaultAsync(r => r.LanguageId == langId && r.ResourceName == resourceName);

            value = resource?.ResourceValue;

            if (value != null)
            {
                _cache.Set(cacheKey, value, TimeSpan.FromMinutes(30));
            }
            else
            {
                // Eksik key'i logla (sadece ilk seferde)
                var missingKey = $"{langId}_{resourceName}";
            }
        }

        return value ?? defaultValue ?? resourceName;
    }

    public async Task<string> GetResourceAsync(string resourceName, string languageCode, string? defaultValue = null)
    {
        var language = await GetLanguageByCodeAsync(languageCode);
        if (language == null)
            return defaultValue ?? resourceName;

        return await GetResourceAsync(resourceName, language.Id, defaultValue);
    }

    public async Task<Dictionary<string, string>> GetAllResourcesAsync(int languageId)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}all_{languageId}";

        if (!_cache.TryGetValue(cacheKey, out Dictionary<string, string>? resources))
        {
            resources = await _context.LocaleStringResources
                .Where(r => r.LanguageId == languageId)
                .ToDictionaryAsync(r => r.ResourceName, r => r.ResourceValue);

            _cache.Set(cacheKey, resources, TimeSpan.FromMinutes(30));
        }

        return resources!;
    }

    public async Task<Dictionary<string, string>> GetResourcesByPrefixAsync(string prefix, int? languageId = null)
    {
        var langId = languageId ?? GetCurrentLanguageId();

        return await _context.LocaleStringResources
            .Where(r => r.LanguageId == langId && r.ResourceName.StartsWith(prefix))
            .ToDictionaryAsync(r => r.ResourceName, r => r.ResourceValue);
    }

    public async Task<LocaleStringResource?> GetResourceByNameAsync(string resourceName, int languageId)
    {
        return await _context.LocaleStringResources
            .FirstOrDefaultAsync(r => r.LanguageId == languageId && r.ResourceName == resourceName);
    }

    public async Task<IEnumerable<LocaleStringResource>> GetResourcesByLanguageAsync(int languageId)
    {
        return await _context.LocaleStringResources
            .Where(r => r.LanguageId == languageId)
            .OrderBy(r => r.ResourceName)
            .ToListAsync();
    }

    public async Task<LocaleStringResource> SetResourceAsync(int languageId, string resourceName, string resourceValue)
    {
        var existing = await GetResourceByNameAsync(resourceName, languageId);

        if (existing != null)
        {
            existing.ResourceValue = resourceValue;
            existing.UpdatedAt = TurkeyTime.Now;
        }
        else
        {
            existing = new LocaleStringResource
            {
                LanguageId = languageId,
                ResourceName = resourceName,
                ResourceValue = resourceValue
            };
            _context.LocaleStringResources.Add(existing);
        }

        await _context.SaveChangesAsync();
        ClearResourceCache(languageId, resourceName);

        return existing;
    }

    public async Task DeleteResourceAsync(int resourceId)
    {
        var resource = await _context.LocaleStringResources.FindAsync(resourceId);
        if (resource != null)
        {
            _context.LocaleStringResources.Remove(resource);
            await _context.SaveChangesAsync();
            ClearResourceCache(resource.LanguageId, resource.ResourceName);
        }
    }

    public async Task DeleteResourceByNameAsync(string resourceName, int languageId)
    {
        var resource = await GetResourceByNameAsync(resourceName, languageId);
        if (resource != null)
        {
            _context.LocaleStringResources.Remove(resource);
            await _context.SaveChangesAsync();
            ClearResourceCache(languageId, resourceName);
        }
    }

    public async Task<int> DeleteAllResourcesByLanguageAsync(int languageId)
    {
        var resources = await _context.LocaleStringResources
            .Where(r => r.LanguageId == languageId)
            .ToListAsync();

        var count = resources.Count;
        _context.LocaleStringResources.RemoveRange(resources);
        await _context.SaveChangesAsync();

        // Cache temizle
        _cache.Remove($"{CACHE_KEY_PREFIX}all_{languageId}");

        return count;
    }

    #endregion

    #region Bulk Operations

    public async Task ImportResourcesAsync(int languageId, Dictionary<string, string> resources)
    {
        foreach (var kvp in resources)
        {
            await SetResourceAsync(languageId, kvp.Key, kvp.Value);
        }
    }

    public async Task<Dictionary<string, string>> ExportResourcesAsync(int languageId)
    {
        return await GetAllResourcesAsync(languageId);
    }

    #endregion

    #region Current Language

    public int GetCurrentLanguageId()
    {
        // 1. Cookie'den dene (en öncelikli - kullanıcı seçimi)
        var cookie = _httpContextAccessor.HttpContext?.Request.Cookies["Language"];
        if (int.TryParse(cookie, out var cookieLangId))
            return cookieLangId;

        // 2. Kullanıcının kayıtlı dil tercihinden dene
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("UserId")?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            // Normal kullanıcı mı kontrol et
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user?.PreferredLanguageId != null)
                return user.PreferredLanguageId.Value;

            // Müşteri personeli mi kontrol et
            var customerPersonnel = _context.CustomerPersonnel.FirstOrDefault(cp => cp.Id == userId);
            if (customerPersonnel?.PreferredLanguageId != null)
                return customerPersonnel.PreferredLanguageId.Value;
        }

        // 3. Varsayılan dili getir
        var defaultLang = _context.Languages.FirstOrDefault(l => l.IsDefault && l.IsActive)
            ?? _context.Languages.FirstOrDefault(l => l.IsActive);

        return defaultLang?.Id ?? 0;
    }

    public string GetCurrentLanguageCode()
    {
        var langId = GetCurrentLanguageId();
        var lang = _context.Languages.Find(langId);
        return lang?.UniqueSeoCode ?? "tr";
    }

    public void SetCurrentLanguage(int languageId)
    {
        // Cookie'ye kaydet
        _httpContextAccessor.HttpContext?.Response.Cookies.Append("Language", languageId.ToString(),
            new CookieOptions { Expires = TurkeyTime.Now.AddYears(1) });

        // Kullanıcı giriş yapmışsa, tercihini veritabanına da kaydet
        SaveUserLanguagePreference(languageId);
    }

    public void SetCurrentLanguage(string languageCode)
    {
        var lang = _context.Languages.FirstOrDefault(l => l.UniqueSeoCode == languageCode || l.LanguageCulture == languageCode);
        if (lang != null)
            SetCurrentLanguage(lang.Id);
    }

    /// <summary>
    /// Kullanıcının dil tercihini veritabanına kaydeder
    /// </summary>
    private void SaveUserLanguagePreference(int languageId)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("UserId")?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
            return;

        // Normal kullanıcı mı kontrol et
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user != null)
        {
            user.PreferredLanguageId = languageId;
            _context.SaveChanges();
            return;
        }

        // Müşteri personeli mi kontrol et
        var customerPersonnel = _context.CustomerPersonnel.FirstOrDefault(cp => cp.Id == userId);
        if (customerPersonnel != null)
        {
            customerPersonnel.PreferredLanguageId = languageId;
            _context.SaveChanges();
        }
    }

    /// <summary>
    /// Kullanıcının kayıtlı dil tercihini cookie'ye uygular (login sonrası çağrılır)
    /// </summary>
    public void ApplyUserLanguagePreference(int userId)
    {
        // Normal kullanıcı mı kontrol et
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user?.PreferredLanguageId != null)
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Append("Language", user.PreferredLanguageId.Value.ToString(),
                new CookieOptions { Expires = TurkeyTime.Now.AddYears(1) });
            return;
        }

        // Müşteri personeli mi kontrol et
        var customerPersonnel = _context.CustomerPersonnel.FirstOrDefault(cp => cp.Id == userId);
        if (customerPersonnel?.PreferredLanguageId != null)
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Append("Language", customerPersonnel.PreferredLanguageId.Value.ToString(),
                new CookieOptions { Expires = TurkeyTime.Now.AddYears(1) });
        }
    }

    #endregion

    #region Cache Helpers

    private void ClearLanguageCache()
    {
        _cache.Remove($"{LANGUAGES_CACHE_KEY}_true");
        _cache.Remove($"{LANGUAGES_CACHE_KEY}_false");
    }

    private void ClearResourceCache(int languageId, string resourceName)
    {
        _cache.Remove($"{CACHE_KEY_PREFIX}{languageId}_{resourceName}");
        _cache.Remove($"{CACHE_KEY_PREFIX}all_{languageId}");
    }

    /// <summary>
    /// Tüm localization cache'ini temizle
    /// </summary>
    public async Task ClearAllCacheAsync()
    {
        // Dil cache'ini temizle
        ClearLanguageCache();

        // Tüm diller için resource cache'ini temizle
        var languages = await _context.Languages.ToListAsync();
        foreach (var lang in languages)
        {
            _cache.Remove($"{CACHE_KEY_PREFIX}all_{lang.Id}");
        }
    }

    #endregion

    #region Missing Keys Tracking

    /// <summary>
    /// Loglanan eksik çeviri key'lerini getirir (debug için)
    /// </summary>
    public IEnumerable<string> GetMissingKeys()
    {
        return _loggedMissingKeys.Keys.Select(k => k.Contains('_') ? k.Substring(k.IndexOf('_') + 1) : k).Distinct();
    }

    /// <summary>
    /// Eksik key loglarını temizler
    /// </summary>
    public void ClearMissingKeysLog()
    {
        _loggedMissingKeys.Clear();
    }

    #endregion

    #region XML Import/Export

    /// <summary>
    /// XML dosyasından çevirileri içe aktarır
    /// Format: NopCommerce tarzı LocaleResource XML
    /// </summary>
    public async Task<int> ImportFromXmlAsync(int languageId, string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        var resources = doc.Descendants("LocaleResource");
        int count = 0;

        foreach (var resource in resources)
        {
            var name = resource.Attribute("Name")?.Value;
            var value = resource.Element("Value")?.Value;

            if (!string.IsNullOrEmpty(name) && value != null)
            {
                await SetResourceAsync(languageId, name, value);
                count++;
            }
        }

        // Cache'i temizle
        _cache.Remove($"{CACHE_KEY_PREFIX}all_{languageId}");

        return count;
    }

    /// <summary>
    /// XML dosyasından çevirileri içe aktarır (dosya yolu ile)
    /// </summary>
    public async Task<int> ImportFromXmlFileAsync(int languageId, string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"XML dosyası bulunamadı: {filePath}");

        var xmlContent = await File.ReadAllTextAsync(filePath);
        return await ImportFromXmlAsync(languageId, xmlContent);
    }

    /// <summary>
    /// Dil koduna göre varsayılan XML dosyasından çevirileri içe aktarır
    /// App_Data/Localization/resources.{languageCode}.xml
    /// </summary>
    public async Task<int> ImportFromDefaultXmlAsync(int languageId, string basePath)
    {
        var language = await GetLanguageByIdAsync(languageId);
        if (language == null)
            throw new Exception("Dil bulunamadı.");

        var fileName = $"resources.{language.UniqueSeoCode}.xml";
        var filePath = Path.Combine(basePath, "App_Data", "Localization", fileName);

        return await ImportFromXmlFileAsync(languageId, filePath);
    }

    /// <summary>
    /// Çevirileri XML formatında dışa aktarır
    /// </summary>
    public async Task<string> ExportToXmlAsync(int languageId)
    {
        var language = await GetLanguageByIdAsync(languageId);
        if (language == null)
            throw new Exception("Dil bulunamadı.");

        var resources = await GetResourcesByLanguageAsync(languageId);

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Language",
                new XAttribute("Name", language.Name),
                new XAttribute("LanguageCulture", language.LanguageCulture),
                new XAttribute("UniqueSeoCode", language.UniqueSeoCode),
                resources.Select(r =>
                    new XElement("LocaleResource",
                        new XAttribute("Name", r.ResourceName),
                        new XElement("Value", r.ResourceValue)
                    )
                )
            )
        );

        return doc.ToString();
    }

    #endregion
}
