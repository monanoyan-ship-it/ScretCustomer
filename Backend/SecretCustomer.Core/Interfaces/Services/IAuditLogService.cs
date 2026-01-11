using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IAuditLogService
{
    // Genel loglama
    Task LogAsync(int logTypeId, string message, string? category = null, string? details = null);

    // Bilgi logu
    Task LogInfoAsync(string message, string? category = null, string? details = null);

    // Uyarı logu
    Task LogWarningAsync(string message, string? category = null, string? details = null);

    // Hata logu
    Task LogErrorAsync(string message, string? category = null, Exception? exception = null);

    // Veri değişikliği logu
    Task LogDataChangeAsync(int logTypeId, string tableName, int recordId,
        string? oldValues = null, string? newValues = null, string? message = null);

    // Kullanıcı işlemleri logu
    Task LogLoginAsync(int userId, string userName, bool success, string? failReason = null);
    Task LogLogoutAsync(int userId, string userName);
    Task LogAccessDeniedAsync(string resource, string? reason = null);

    // Log sorgulama
    Task<IEnumerable<AuditLog>> GetLogsAsync(
        int? logTypeId = null,
        string? category = null,
        int? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50);

    Task<int> GetLogsCountAsync(
        int? logTypeId = null,
        string? category = null,
        int? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    Task<AuditLog?> GetByIdAsync(int id);

    // Temizlik
    Task<int> DeleteOldLogsAsync(int daysToKeep);
}
