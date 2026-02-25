using SecretCustomer.Core.DTOs.Import;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IImportService
{
    /// <summary>
    /// CSV dosyasından personel verilerini import eder
    /// </summary>
    /// <param name="csvContent">CSV dosya içeriği</param>
    /// <param name="updateExisting">Mevcut kayıtları güncelle (false ise atla)</param>
    /// <returns>Import sonucu</returns>
    Task<ImportResultDto> ImportPersonnelFromCsvAsync(string csvContent, bool updateExisting = false);

    /// <summary>
    /// Stream'den personel verilerini import eder
    /// </summary>
    Task<ImportResultDto> ImportPersonnelFromCsvAsync(Stream csvStream, bool updateExisting = false);

    /// <summary>
    /// CSV dosyasından yeni checklist oluşturur ve soruları import eder
    /// </summary>
    /// <param name="csvStream">CSV dosya stream'i</param>
    /// <param name="checklistName">Yeni checklist adı</param>
    /// <param name="description">Açıklama (opsiyonel)</param>
    /// <returns>Import sonucu</returns>
    Task<ChecklistImportResultDto> ImportChecklistFromCsvAsync(Stream csvStream, string checklistName, string? description = null);

    /// <summary>
    /// CSV içeriğinden yeni checklist oluşturur ve soruları import eder
    /// </summary>
    Task<ChecklistImportResultDto> ImportChecklistFromCsvAsync(string csvContent, string checklistName, string? description = null);
}
