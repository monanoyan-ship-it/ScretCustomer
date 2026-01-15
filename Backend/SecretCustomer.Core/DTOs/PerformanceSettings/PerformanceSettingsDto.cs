namespace SecretCustomer.Core.DTOs.PerformanceSettings;

/// <summary>
/// Performans ayarları listesi için DTO
/// </summary>
public class PerformanceSettingsListDto
{
    public int Id { get; set; }
    public int ProjectTypeId { get; set; }
    public string ProjectTypeName { get; set; } = string.Empty;
    public string ProjectTypeIcon { get; set; } = string.Empty;
    public string ProjectTypeColor { get; set; } = string.Empty;
    public int? DailyTarget { get; set; }
    public int? WeeklyTarget { get; set; }
    public int? MonthlyTarget { get; set; }
    public int? YearlyTarget { get; set; }
    public decimal? SuccessThreshold { get; set; }
    public decimal? WarningThreshold { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Performans ayarları detay için DTO
/// </summary>
public class PerformanceSettingsDto : PerformanceSettingsListDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Performans ayarları oluşturma/güncelleme için DTO
/// </summary>
public class SavePerformanceSettingsDto
{
    public int ProjectTypeId { get; set; }
    public int? DailyTarget { get; set; }
    public int? WeeklyTarget { get; set; }
    public int? MonthlyTarget { get; set; }
    public int? YearlyTarget { get; set; }
    public decimal? SuccessThreshold { get; set; } = 80;
    public decimal? WarningThreshold { get; set; } = 60;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Tüm proje tipleri için ayarları toplu güncelleme
/// </summary>
public class BulkSavePerformanceSettingsDto
{
    public List<SavePerformanceSettingsDto> Settings { get; set; } = new();
}
