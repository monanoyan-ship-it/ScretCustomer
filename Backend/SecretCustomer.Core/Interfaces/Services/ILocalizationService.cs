using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ILocalizationService
{
    // Dil işlemleri
    Task<IEnumerable<Language>> GetAllLanguagesAsync(bool onlyActive = true);
    Task<Language?> GetLanguageByIdAsync(Guid id);
    Task<Language?> GetLanguageByCodeAsync(string code);
    Task<Language?> GetDefaultLanguageAsync();
    Task<Language> CreateLanguageAsync(Language language);
    Task<Language> UpdateLanguageAsync(Language language);
    Task DeleteLanguageAsync(Guid id);

    // Çeviri işlemleri
    Task<string> GetResourceAsync(string resourceName, Guid? languageId = null, string? defaultValue = null);
    Task<string> GetResourceAsync(string resourceName, string languageCode, string? defaultValue = null);
    Task<Dictionary<string, string>> GetAllResourcesAsync(Guid languageId);
    Task<Dictionary<string, string>> GetResourcesByPrefixAsync(string prefix, Guid? languageId = null);

    // Kaynak yönetimi
    Task<LocaleStringResource?> GetResourceByNameAsync(string resourceName, Guid languageId);
    Task<IEnumerable<LocaleStringResource>> GetResourcesByLanguageAsync(Guid languageId);
    Task<LocaleStringResource> SetResourceAsync(Guid languageId, string resourceName, string resourceValue);
    Task DeleteResourceAsync(Guid resourceId);
    Task DeleteResourceByNameAsync(string resourceName, Guid languageId);

    // Toplu işlemler
    Task ImportResourcesAsync(Guid languageId, Dictionary<string, string> resources);
    Task<Dictionary<string, string>> ExportResourcesAsync(Guid languageId);

    // XML Import/Export
    Task<int> ImportFromXmlAsync(Guid languageId, string xmlContent);
    Task<int> ImportFromXmlFileAsync(Guid languageId, string filePath);
    Task<int> ImportFromDefaultXmlAsync(Guid languageId, string basePath);
    Task<string> ExportToXmlAsync(Guid languageId);

    // Mevcut dil
    Guid GetCurrentLanguageId();
    string GetCurrentLanguageCode();
    void SetCurrentLanguage(Guid languageId);
    void SetCurrentLanguage(string languageCode);
}
