using System.ComponentModel.DataAnnotations;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Sistem genelinde tüm olayları, hataları ve veri değişikliklerini kaydeden audit log
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    /// <summary>Log türü</summary>
    public int LogTypeId { get; set; } = LogTypes.Ids.Info;

    /// <summary>Log kategorisi (Controller adı, Service adı, vb.)</summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>İşlem/Olay açıklaması</summary>
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Detaylı açıklama veya hata mesajı</summary>
    public string? Details { get; set; }

    /// <summary>Etkilenen tablo adı (veri değişikliklerinde)</summary>
    [MaxLength(100)]
    public string? TableName { get; set; }

    /// <summary>Etkilenen kayıt ID'si</summary>
    public int? RecordId { get; set; }

    /// <summary>Eski değerler (JSON formatında)</summary>
    public string? OldValues { get; set; }

    /// <summary>Yeni değerler (JSON formatında)</summary>
    public string? NewValues { get; set; }

    /// <summary>İşlemi yapan kullanıcı ID'si</summary>
    public int? UserId { get; set; }

    /// <summary>İşlemi yapan kullanıcı adı</summary>
    [MaxLength(100)]
    public string? UserName { get; set; }

    /// <summary>İstemci IP adresi</summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>User Agent (tarayıcı bilgisi)</summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>İstek URL'i</summary>
    [MaxLength(500)]
    public string? RequestUrl { get; set; }

    /// <summary>HTTP Method (GET, POST, vb.)</summary>
    [MaxLength(10)]
    public string? HttpMethod { get; set; }

    /// <summary>Exception türü (hatalarda)</summary>
    [MaxLength(200)]
    public string? ExceptionType { get; set; }

    /// <summary>Stack trace (hatalarda)</summary>
    public string? StackTrace { get; set; }

    /// <summary>Log oluşturulma zamanı</summary>
    public DateTime CreatedAt { get; set; } = TurkeyTime.Now;
}
