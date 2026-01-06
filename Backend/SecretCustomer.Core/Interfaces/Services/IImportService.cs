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
}
