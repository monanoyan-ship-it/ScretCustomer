using System.ComponentModel.DataAnnotations;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Core.DTOs.AssignmentPeriod;

/// <summary>
/// Dönem response DTO
/// </summary>
public class AssignmentPeriodDto
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Open";
    public string StatusName { get; set; } = "Açık";
    public int TargetCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal? AverageScore { get; set; }
    public string? Notes { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }

    // İlerleme yüzdesi
    public decimal ProgressPercentage => TargetCount > 0 ? Math.Round((decimal)CompletedCount / TargetCount * 100, 1) : 0;

    // Kalan gün
    public int RemainingDays => (EndDate - TurkeyTime.Now).Days;

    // Dönem sona erdi mi
    public bool IsExpired => EndDate < TurkeyTime.Now;
}

/// <summary>
/// Dönem oluşturma DTO
/// </summary>
public class CreateAssignmentPeriodDto
{
    [Required]
    public int AssignmentId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int TargetCount { get; set; } = 25;

    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Dönem güncelleme DTO
/// </summary>
public class UpdateAssignmentPeriodDto
{
    [Required]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public string Status { get; set; } = "Open";

    public int TargetCount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Dönem özet DTO - Liste için
/// </summary>
public class AssignmentPeriodSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Open";
    public int TargetCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal? AverageScore { get; set; }
}
