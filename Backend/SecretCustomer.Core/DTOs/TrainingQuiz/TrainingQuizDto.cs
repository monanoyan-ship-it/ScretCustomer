namespace SecretCustomer.Core.DTOs.TrainingQuiz;

// ===== QUIZ DTO'LARI =====

/// <summary>
/// Quiz listesi için hafif DTO
/// </summary>
public class TrainingQuizListDto
{
    public int Id { get; set; }
    public int? TrainingVideoId { get; set; }
    public string? TrainingVideoTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PassingScore { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public int QuestionCount { get; set; }
    public int ResponseCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Quiz detay DTO (sorular dahil)
/// </summary>
public class TrainingQuizDto : TrainingQuizListDto
{
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public bool ShowResults { get; set; }
    public List<TrainingQuizQuestionDto> Questions { get; set; } = new();
}

/// <summary>
/// Quiz oluşturma DTO
/// </summary>
public class CreateTrainingQuizDto
{
    public int? TrainingVideoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PassingScore { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool ShuffleQuestions { get; set; } = false;
    public bool ShuffleOptions { get; set; } = false;
    public bool ShowResults { get; set; } = true;
    public List<CreateTrainingQuizQuestionDto> Questions { get; set; } = new();
}

/// <summary>
/// Quiz güncelleme DTO
/// </summary>
public class UpdateTrainingQuizDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PassingScore { get; set; }
    public bool IsRequired { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public bool ShowResults { get; set; }
    public bool IsActive { get; set; }
}

// ===== SORU DTO'LARI =====

/// <summary>
/// Quiz sorusu DTO
/// </summary>
public class TrainingQuizQuestionDto
{
    public int Id { get; set; }
    public int TrainingQuizId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public int Order { get; set; }
    public int QuestionTypeId { get; set; }
    public string QuestionTypeName { get; set; } = string.Empty;
    public List<TrainingQuizOptionDto> Options { get; set; } = new();
}

/// <summary>
/// Soru oluşturma DTO
/// </summary>
public class CreateTrainingQuizQuestionDto
{
    public string Text { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public int Order { get; set; }
    public int QuestionTypeId { get; set; } = 1; // Default: SingleChoice
    public List<CreateTrainingQuizOptionDto> Options { get; set; } = new();
}

/// <summary>
/// Soru güncelleme DTO
/// </summary>
public class UpdateTrainingQuizQuestionDto
{
    public string Text { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public int Order { get; set; }
    public int QuestionTypeId { get; set; }
}

// ===== SEÇENEK DTO'LARI =====

/// <summary>
/// Quiz seçeneği DTO
/// </summary>
public class TrainingQuizOptionDto
{
    public int Id { get; set; }
    public int TrainingQuizQuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    public decimal WeightPoints { get; set; }
    public bool IsCorrect { get; set; }
}

/// <summary>
/// Seçenek oluşturma DTO
/// </summary>
public class CreateTrainingQuizOptionDto
{
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    public decimal WeightPoints { get; set; }
    public bool IsCorrect { get; set; }
}

/// <summary>
/// Seçenek güncelleme DTO
/// </summary>
public class UpdateTrainingQuizOptionDto
{
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    public decimal WeightPoints { get; set; }
    public bool IsCorrect { get; set; }
}

// ===== YANIT (RESPONSE) DTO'LARI =====

/// <summary>
/// Quiz yanıtı listesi DTO
/// </summary>
public class TrainingQuizResponseListDto
{
    public int Id { get; set; }
    public int TrainingQuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int? TrainingVideoParticipantId { get; set; }
    public int? TrainingVideoExternalParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string? ParticipantEmail { get; set; }
    public bool IsExternal { get; set; }
    public decimal TotalScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public decimal ScorePercentage { get; set; }
    public bool IsPassed { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
}

/// <summary>
/// Quiz yanıtı detay DTO (cevaplar dahil)
/// </summary>
public class TrainingQuizResponseDto : TrainingQuizResponseListDto
{
    public List<TrainingQuizAnswerDto> Answers { get; set; } = new();
}

/// <summary>
/// Quiz yanıtı başlatma DTO
/// </summary>
public class StartTrainingQuizResponseDto
{
    public int TrainingQuizId { get; set; }
    public int? TrainingVideoParticipantId { get; set; }
    public int? TrainingVideoExternalParticipantId { get; set; }
}

// ===== CEVAP DTO'LARI =====

/// <summary>
/// Quiz cevabı DTO
/// </summary>
public class TrainingQuizAnswerDto
{
    public int Id { get; set; }
    public int TrainingQuizResponseId { get; set; }
    public int TrainingQuizQuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int? SelectedOptionId { get; set; }
    public string? SelectedOptionText { get; set; }
    public string? SelectedOptionIds { get; set; }
    public decimal EarnedPoints { get; set; }
    public bool IsCorrect { get; set; }
}

/// <summary>
/// Quiz cevabı gönderme DTO
/// </summary>
public class SubmitTrainingQuizAnswerDto
{
    public int QuestionId { get; set; }
    /// <summary>
    /// Tekli seçim için
    /// </summary>
    public int? SelectedOptionId { get; set; }
    /// <summary>
    /// Çoklu seçim için
    /// </summary>
    public List<int>? SelectedOptionIds { get; set; }
}

/// <summary>
/// Tüm quiz cevaplarını gönderme DTO
/// </summary>
public class SubmitTrainingQuizDto
{
    public int QuizId { get; set; }
    public int? ParticipantId { get; set; }
    public int? ExternalParticipantId { get; set; }
    public List<SubmitTrainingQuizAnswerDto> Answers { get; set; } = new();
}

/// <summary>
/// Quiz sonucu DTO
/// </summary>
public class TrainingQuizResultDto
{
    public int ResponseId { get; set; }
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public decimal? MaxPossibleScore { get; set; }
    public decimal? ScorePercentage { get; set; }
    public int? PassingScore { get; set; }
    public bool IsPassed { get; set; }
    public int? CorrectCount { get; set; }
    public int? TotalQuestions { get; set; }
    public bool ShowResults { get; set; }
    public List<TrainingQuizAnswerResultDto>? AnswerResults { get; set; }
}

/// <summary>
/// Cevap sonucu DTO (ShowResults = true ise doldurulur)
/// </summary>
public class TrainingQuizAnswerResultDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public decimal EarnedPoints { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? SelectedOptionText { get; set; }
    public List<int>? SelectedOptionIds { get; set; }
    public List<TrainingQuizOptionResultDto>? Options { get; set; }
}

/// <summary>
/// Seçenek sonucu DTO
/// </summary>
public class TrainingQuizOptionResultDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool WasSelected { get; set; }
}

// ===== KATILIMCI QUIZ DTO'LARI =====

/// <summary>
/// Katılımcı için quiz bilgisi (cevaplanmamış)
/// </summary>
public class ParticipantQuizDto
{
    public int QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PassingScore { get; set; }
    public bool IsRequired { get; set; }
    public int QuestionCount { get; set; }
    public bool HasPreviousAttempt { get; set; }
    public int AttemptCount { get; set; }
    public TrainingQuizResultDto? LastAttemptResult { get; set; }
}

/// <summary>
/// Katılımcının çözmesi için quiz (sorular dahil, doğru cevaplar gizli)
/// </summary>
public class ParticipantQuizQuestionsDto
{
    public int QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PassingScore { get; set; }
    public List<ParticipantQuizQuestionDto> Questions { get; set; } = new();
}

/// <summary>
/// Katılımcı için soru (doğru cevaplar gizli)
/// </summary>
public class ParticipantQuizQuestionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public int Order { get; set; }
    public int QuestionTypeId { get; set; }
    public string QuestionTypeName { get; set; } = string.Empty;
    public List<ParticipantQuizOptionDto> Options { get; set; } = new();
}

/// <summary>
/// Katılımcı için seçenek (ağırlık puanı ve doğru cevap gizli)
/// </summary>
public class ParticipantQuizOptionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
}

// ===== VIDEO İLE QUIZ İLİŞKİSİ =====

/// <summary>
/// Video için quiz durumu
/// </summary>
public class VideoQuizStatusDto
{
    public int VideoId { get; set; }
    public bool HasQuiz { get; set; }
    public int? QuizId { get; set; }
    public string? QuizTitle { get; set; }
    public bool? IsRequired { get; set; }
    public int? PassingScore { get; set; }

    // Katılımcının quiz durumu
    public bool? HasAttempted { get; set; }
    public bool? IsPassed { get; set; }
    public decimal? LastScore { get; set; }
    public int? AttemptCount { get; set; }
}

// ===== FİLTRELEME DTO'LARI =====

/// <summary>
/// Quiz filtreleme DTO
/// </summary>
public class TrainingQuizFilterDto
{
    public string? SearchTerm { get; set; }
    public List<int>? VideoIds { get; set; }
    public bool? IsRequired { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Quiz yanıtları filtreleme DTO
/// </summary>
public class TrainingQuizResponseFilterDto
{
    public int? QuizId { get; set; }
    public int? VideoId { get; set; }
    public bool? IsPassed { get; set; }
    public bool? IsExternal { get; set; }
    public int? StatusId { get; set; }
}

/// <summary>
/// Video'ya quiz atama DTO
/// </summary>
public class AssignQuizToVideoDto
{
    /// <summary>
    /// Atanacak quiz ID (null ise ataması kaldırılır)
    /// </summary>
    public int? QuizId { get; set; }
}
