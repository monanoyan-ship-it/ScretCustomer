using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Project;

/// <summary>
/// Proje dosyasi DTO
/// </summary>
public class ProjectFileDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public int? UploadedById { get; set; }
    public string? UploadedByName { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Dosya boyutunu okunabilir formatta dondurur (KB, MB, GB)
    /// </summary>
    public string FileSizeDisplay
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024.0 * 1024.0):F1} MB";
            return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }
}

/// <summary>
/// Dosya yukleme DTO
/// </summary>
public class UploadProjectFileDto
{
    [Required]
    public int ProjectId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Dosya aciklamasi guncelleme DTO
/// </summary>
public class UpdateProjectFileDescriptionDto
{
    [MaxLength(500)]
    public string? Description { get; set; }
}
