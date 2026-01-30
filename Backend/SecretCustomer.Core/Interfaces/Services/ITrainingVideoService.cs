using SecretCustomer.Core.DTOs.TrainingVideo;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ITrainingVideoService
{
    // ===== VIDEO CRUD =====
    Task<TrainingVideoDto?> GetByIdAsync(int id);
    Task<IEnumerable<TrainingVideoListDto>> GetListAsync(TrainingVideoFilterDto? filter = null);
    Task<TrainingVideoDto> CreateAsync(CreateTrainingVideoDto dto, Stream videoStream, string fileName, Stream? thumbnailStream = null);
    Task<TrainingVideoDto> UpdateAsync(int id, UpdateTrainingVideoDto dto);
    Task<bool> DeleteAsync(int id);

    // ===== VIDEO KAPSAM SEÇENEKLERİ =====
    Task<TrainingVideoScopeOptionsDto> GetScopeOptionsAsync();

    // ===== ATAMA CRUD =====
    Task<TrainingVideoAssignmentDto?> GetAssignmentByIdAsync(int id);
    Task<IEnumerable<TrainingVideoAssignmentListDto>> GetAssignmentsAsync(TrainingVideoAssignmentFilterDto? filter = null);
    Task<TrainingVideoAssignmentDto> CreateAssignmentAsync(CreateTrainingVideoAssignmentDto dto);
    Task<TrainingVideoAssignmentDto> UpdateAssignmentAsync(int id, UpdateTrainingVideoAssignmentDto dto);
    Task<bool> DeleteAssignmentAsync(int id);

    // ===== OTOMATİK ATAMA =====
    Task<AssignmentPreviewDto> PreviewAutoAssignmentAsync(CreateTrainingVideoAssignmentDto dto);

    // ===== KATILIMCI YÖNETİMİ =====
    Task<IEnumerable<TrainingVideoParticipantDto>> GetParticipantsAsync(int assignmentId);
    Task<IEnumerable<AllParticipantDto>> GetAllParticipantsAsync();
    Task<int> SendEmailsAsync(SendTrainingEmailDto dto, int? sentByUserId = null, int emailTypeId = 1);
    Task<bool> SendRemindersAsync(int assignmentId);

    // ===== KULLANICI EĞİTİMLERİ =====
    Task<IEnumerable<MyTrainingDto>> GetMyTrainingsAsync(int userId);
    Task<MyTrainingDto?> GetMyTrainingByIdAsync(int userId, int participantId);
    Task<bool> UpdateWatchProgressAsync(int participantId, UpdateWatchProgressDto dto);

    // ===== VİDEO STREAMING =====
    Task<Stream?> GetVideoStreamAsync(int videoId);
    Task<string?> GetVideoPathAsync(int videoId);

    // ===== SCOPE-BASED DATA =====
    Task<IEnumerable<object>> GetScopeCustomersAsync(int videoId);
    Task<IEnumerable<ScopePersonnelSearchResultDto>> SearchScopePersonnelAsync(int videoId, string? searchTerm, int maxResults = 20);

    // ===== DIŞ KATILIMCI YÖNETİMİ =====
    Task<IEnumerable<TrainingVideoExternalParticipantDto>> GetExternalParticipantsAsync(int assignmentId);
    Task<AddExternalParticipantsResultDto> AddExternalParticipantsAsync(AddExternalParticipantsDto dto, int? sentByUserId = null);
    Task<bool> DeleteExternalParticipantAsync(int participantId);
    Task<int> SendExternalEmailsAsync(SendExternalEmailsDto dto, int? sentByUserId = null);

    // ===== DIŞ KATILIMCI VİDEO İZLEME (TOKEN İLE) =====
    Task<ExternalVideoWatchDto?> GetExternalVideoByTokenAsync(string token);
    Task<bool> UpdateExternalWatchProgressAsync(string token, UpdateWatchProgressDto dto);
    Task<Stream?> GetExternalVideoStreamAsync(string token);
}
