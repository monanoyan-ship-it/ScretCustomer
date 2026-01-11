using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ILocalizationService
{
    // Dil işlemleri
    Task<IEnumerable<Language>> GetAllLanguagesAsync(bool onlyActive = true);
    Task<Language?> GetLanguageByIdAsync(int id);
    Task<Language?> GetLanguageByCodeAsync(string code);
    Task<Language?> GetDefaultLanguageAsync();
    Task<Language> CreateLanguageAsync(Language language);
    Task<Language> UpdateLanguageAsync(Language language);
    Task DeleteLanguageAsync(int id);

    // Çeviri işlemleri
    Task<string> GetResourceAsync(string resourceName, int? languageId = null, string? defaultValue = null);
    Task<string> GetResourceAsync(string resourceName, string languageCode, string? defaultValue = null);
    Task<Dictionary<string, string>> GetAllResourcesAsync(int languageId);
    Task<Dictionary<string, string>> GetResourcesByPrefixAsync(string prefix, int? languageId = null);

    // Kaynak yönetimi
    Task<LocaleStringResource?> GetResourceByNameAsync(string resourceName, int languageId);
    Task<IEnumerable<LocaleStringResource>> GetResourcesByLanguageAsync(int languageId);
    Task<LocaleStringResource> SetResourceAsync(int languageId, string resourceName, string resourceValue);
    Task DeleteResourceAsync(int resourceId);
    Task DeleteResourceByNameAsync(string resourceName, int languageId);
    Task<int> DeleteAllResourcesByLanguageAsync(int languageId);

    // Toplu işlemler
    Task ImportResourcesAsync(int languageId, Dictionary<string, string> resources);
    Task<Dictionary<string, string>> ExportResourcesAsync(int languageId);

    // XML Import/Export
    Task<int> ImportFromXmlAsync(int languageId, string xmlContent);
    Task<int> ImportFromXmlFileAsync(int languageId, string filePath);
    Task<int> ImportFromDefaultXmlAsync(int languageId, string basePath);
    Task<string> ExportToXmlAsync(int languageId);

    // Mevcut dil
    int GetCurrentLanguageId();
    string GetCurrentLanguageCode();
    void SetCurrentLanguage(int languageId);
    void SetCurrentLanguage(string languageCode);

    /// <summary>
    /// Kullanıcının kayıtlı dil tercihini cookie'ye uygular (login sonrası çağrılır)
    /// </summary>
    void ApplyUserLanguagePreference(int userId);

    /// <summary>
    /// Tüm localization cache'ini temizle
    /// </summary>
    Task ClearAllCacheAsync();

    // Eksik key takibi (debug için)
    /// <summary>
    /// Loglanan eksik çeviri key'lerini getirir
    /// </summary>
    IEnumerable<string> GetMissingKeys();

    /// <summary>
    /// Eksik key loglarını temizler
    /// </summary>
    void ClearMissingKeysLog();
}
