using SecretCustomer.Core.DTOs.TrainingQuiz;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ITrainingQuizService
{
    // Quiz CRUD
    Task<TrainingQuizDto?> GetByIdAsync(int id);
    Task<IEnumerable<TrainingQuizListDto>> GetListAsync(TrainingQuizFilterDto? filter = null);
    Task<TrainingQuizDto?> GetByVideoIdAsync(int videoId);
    Task<TrainingQuizDto> CreateAsync(CreateTrainingQuizDto dto);
    Task<TrainingQuizDto> UpdateAsync(int id, UpdateTrainingQuizDto dto);
    Task DeleteAsync(int id);

    // Sorular
    Task<TrainingQuizQuestionDto> AddQuestionAsync(int quizId, CreateTrainingQuizQuestionDto dto);
    Task<TrainingQuizQuestionDto> UpdateQuestionAsync(int questionId, UpdateTrainingQuizQuestionDto dto);
    Task DeleteQuestionAsync(int questionId);
    Task ReorderQuestionsAsync(int quizId, List<int> questionIds);

    // Seçenekler
    Task<TrainingQuizOptionDto> AddOptionAsync(int questionId, CreateTrainingQuizOptionDto dto);
    Task<TrainingQuizOptionDto> UpdateOptionAsync(int optionId, UpdateTrainingQuizOptionDto dto);
    Task DeleteOptionAsync(int optionId);
    Task ReorderOptionsAsync(int questionId, List<int> optionIds);

    // Katılımcı tarafı
    Task<ParticipantQuizDto?> GetQuizForParticipantAsync(int participantId, bool isExternal = false);
    Task<ParticipantQuizQuestionsDto?> StartQuizAsync(int participantId, bool isExternal = false);
    Task<TrainingQuizResultDto> SubmitQuizAsync(SubmitTrainingQuizDto dto);

    // Quiz yanıtları (Admin)
    Task<IEnumerable<TrainingQuizResponseListDto>> GetResponsesAsync(TrainingQuizResponseFilterDto? filter = null);
    Task<TrainingQuizResponseDto?> GetResponseByIdAsync(int responseId);

    // Video quiz durumu
    Task<VideoQuizStatusDto> GetVideoQuizStatusAsync(int videoId, int? participantId = null, int? externalParticipantId = null);

    // Video-Quiz atama
    Task<IEnumerable<TrainingQuizListDto>> GetAvailableQuizzesForVideoAsync(int? videoId = null);
    Task AssignQuizToVideoAsync(int? quizId, int videoId);
}
