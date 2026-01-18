using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.DTOs.Training;

/// <summary>
/// Eğitim DTO
/// </summary>
public class TrainingDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TrainingType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Location { get; set; }
    public string? OnlineLink { get; set; }
    public int? MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public bool IsMandatory { get; set; }
    public bool HasCertificate { get; set; }
    public int? PassingScore { get; set; }
    public string? Curriculum { get; set; }
    public string? Prerequisites { get; set; }
    public string? Objectives { get; set; }
    public int? TrainerId { get; set; }
    public string? TrainerName { get; set; }
    public string? ExternalTrainerName { get; set; }
    public string? ExternalTrainerCompany { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TrainingParticipantDto> Participants { get; set; } = new();
    public List<TrainingMaterialDto> Materials { get; set; } = new();
    public string DurationFormatted => DurationMinutes.HasValue
        ? $"{DurationMinutes.Value / 60}s {DurationMinutes.Value % 60}dk"
        : "-";
}

/// <summary>
/// Eğitim listesi için özet DTO
/// </summary>
public class TrainingListDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TrainingType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Location { get; set; }
    public int? MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public bool IsMandatory { get; set; }
    public string? TrainerName { get; set; }
    public string? ProjectName { get; set; }
}

/// <summary>
/// Eğitim oluşturma DTO
/// </summary>
public class CreateTrainingDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TrainingType { get; set; } = "InPerson";
    public string Category { get; set; } = "General";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Location { get; set; }
    public string? OnlineLink { get; set; }
    public int? MaxParticipants { get; set; }
    public bool IsMandatory { get; set; }
    public bool HasCertificate { get; set; }
    public int? PassingScore { get; set; }
    public string? Curriculum { get; set; }
    public string? Prerequisites { get; set; }
    public string? Objectives { get; set; }
    public int? TrainerId { get; set; }
    public string? ExternalTrainerName { get; set; }
    public string? ExternalTrainerCompany { get; set; }
    public int? ProjectId { get; set; }
    public int? CustomerId { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }
    public string? Tags { get; set; }
}

/// <summary>
/// Eğitim güncelleme DTO
/// </summary>
public class UpdateTrainingDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TrainingType { get; set; } = "InPerson";
    public string Status { get; set; } = "Draft";
    public string Category { get; set; } = "General";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Location { get; set; }
    public string? OnlineLink { get; set; }
    public int? MaxParticipants { get; set; }
    public bool IsMandatory { get; set; }
    public bool HasCertificate { get; set; }
    public int? PassingScore { get; set; }
    public string? Curriculum { get; set; }
    public string? Prerequisites { get; set; }
    public string? Objectives { get; set; }
    public int? TrainerId { get; set; }
    public string? ExternalTrainerName { get; set; }
    public string? ExternalTrainerCompany { get; set; }
    public int? ProjectId { get; set; }
    public int? CustomerId { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }
    public string? Tags { get; set; }
}

/// <summary>
/// Eğitim tamamlama DTO
/// </summary>
public class CompleteTrainingDto
{
    public string? Notes { get; set; }
}

/// <summary>
/// Eğitim katılımcısı DTO
/// </summary>
public class TrainingParticipantDto
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? ExternalName { get; set; }
    public string? ExternalEmail { get; set; }
    public string? ExternalPhone { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AttendanceDate { get; set; }
    public int? AttendanceMinutes { get; set; }
    public int? Score { get; set; }
    public bool CertificateIssued { get; set; }
    public DateTime? CertificateDate { get; set; }
    public string? Feedback { get; set; }
    public int? Rating { get; set; }
    public string? Notes { get; set; }
    public string ParticipantName => !string.IsNullOrEmpty(UserName) ? UserName : ExternalName ?? "-";
}

/// <summary>
/// Katılımcı ekleme DTO
/// </summary>
public class AddTrainingParticipantDto
{
    public int? UserId { get; set; }
    public string? ExternalName { get; set; }
    public string? ExternalEmail { get; set; }
    public string? ExternalPhone { get; set; }
}

/// <summary>
/// Katılımcı durumu güncelleme DTO
/// </summary>
public class UpdateParticipantStatusDto
{
    public string Status { get; set; } = "Attended";
    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public int? Rating { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Eğitim materyali DTO
/// </summary>
public class TrainingMaterialDto
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? ExternalUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DownloadUrl => $"/api/trainings/{TrainingId}/materials/{Id}/download";
}

/// <summary>
/// Materyal ekleme DTO
/// </summary>
public class AddTrainingMaterialDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ExternalUrl { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsRequired { get; set; } = false;
}

/// <summary>
/// Eğitim filtre DTO
/// </summary>
public class TrainingFilterDto
{
    // Çoklu filtreler (OR mantığı)
    public List<int>? ProjectIds { get; set; }
    public List<int>? CustomerIds { get; set; }
    public List<int>? TrainerIds { get; set; }
    public List<string>? TrainingTypes { get; set; }
    public List<string>? Statuses { get; set; }
    public List<string>? Categories { get; set; }

    // Tarih filtresi (çoklu)
    public List<DateRangeFilter>? DateRanges { get; set; }
    public bool? IsMandatory { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // Sorting
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Dashboard için eğitim özeti
/// </summary>
public class TrainingSummaryDto
{
    public int TotalTrainings { get; set; }
    public int PlannedTrainings { get; set; }
    public int InProgressTrainings { get; set; }
    public int CompletedTrainings { get; set; }
    public int TotalParticipants { get; set; }
    public int CertificatesIssued { get; set; }
    public double AverageRating { get; set; }
    public int UpcomingTrainings { get; set; }
    public List<TrainingListDto> RecentTrainings { get; set; } = new();
}
