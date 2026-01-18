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
    Task<bool> DeleteAssignmentAsync(int id);

    // ===== OTOMATİK ATAMA =====
    Task<AssignmentPreviewDto> PreviewAutoAssignmentAsync(CreateTrainingVideoAssignmentDto dto);

    // ===== KATILIMCI YÖNETİMİ =====
    Task<IEnumerable<TrainingVideoParticipantDto>> GetParticipantsAsync(int assignmentId);
    Task<bool> SendRemindersAsync(int assignmentId);

    // ===== KULLANICI EĞİTİMLERİ =====
    Task<IEnumerable<MyTrainingDto>> GetMyTrainingsAsync(int userId);
    Task<MyTrainingDto?> GetMyTrainingByIdAsync(int userId, int participantId);
    Task<bool> UpdateWatchProgressAsync(int participantId, UpdateWatchProgressDto dto);

    // ===== VİDEO STREAMING =====
    Task<Stream?> GetVideoStreamAsync(int videoId);
    Task<string?> GetVideoPathAsync(int videoId);
}
