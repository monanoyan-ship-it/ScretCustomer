using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.TrainingQuiz;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class TrainingQuizService : ITrainingQuizService
{
    private readonly ApplicationDbContext _context;

    public TrainingQuizService(ApplicationDbContext context)
    {
        _context = context;
    }

    #region Quiz CRUD

    public async Task<TrainingQuizDto?> GetByIdAsync(int id)
    {
        var quiz = await _context.TrainingQuizzes
            .Include(q => q.TrainingVideo)
            .Include(q => q.Questions.Where(qu => !qu.IsDeleted).OrderBy(qu => qu.Order))
                .ThenInclude(qu => qu.Options.Where(o => !o.IsDeleted).OrderBy(o => o.Order))
            .Include(q => q.Responses.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);

        return quiz == null ? null : MapToDto(quiz);
    }

    public async Task<IEnumerable<TrainingQuizListDto>> GetListAsync(TrainingQuizFilterDto? filter = null)
    {
        var query = _context.TrainingQuizzes
            .Include(q => q.TrainingVideo)
            .Include(q => q.Questions.Where(qu => !qu.IsDeleted))
            .Include(q => q.Responses.Where(r => !r.IsDeleted))
            .Where(q => !q.IsDeleted);

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(q => q.Title.ToLower().Contains(term) ||
                                        (q.Description != null && q.Description.ToLower().Contains(term)) ||
                                        q.TrainingVideo.Title.ToLower().Contains(term));
            }

            if (filter.VideoIds?.Any() == true)
                query = query.Where(q => q.TrainingVideoId.HasValue && filter.VideoIds.Contains(q.TrainingVideoId.Value));

            if (filter.IsRequired.HasValue)
                query = query.Where(q => q.IsRequired == filter.IsRequired.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(q => q.IsActive == filter.IsActive.Value);
        }

        var quizzes = await query.OrderByDescending(q => q.CreatedAt).ToListAsync();

        return quizzes.Select(q => new TrainingQuizListDto
        {
            Id = q.Id,
            TrainingVideoId = q.TrainingVideoId,
            TrainingVideoTitle = q.TrainingVideo?.Title ?? "",
            Title = q.Title,
            Description = q.Description,
            PassingScore = q.PassingScore,
            IsRequired = q.IsRequired,
            IsActive = q.IsActive,
            QuestionCount = q.Questions.Count,
            ResponseCount = q.Responses.Count,
            CreatedAt = q.CreatedAt
        });
    }

    public async Task<TrainingQuizDto?> GetByVideoIdAsync(int videoId)
    {
        var quiz = await _context.TrainingQuizzes
            .Include(q => q.TrainingVideo)
            .Include(q => q.Questions.Where(qu => !qu.IsDeleted).OrderBy(qu => qu.Order))
                .ThenInclude(qu => qu.Options.Where(o => !o.IsDeleted).OrderBy(o => o.Order))
            .Include(q => q.Responses.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(q => q.TrainingVideoId == videoId && !q.IsDeleted && q.IsActive);

        return quiz == null ? null : MapToDto(quiz);
    }

    public async Task<TrainingQuizDto> CreateAsync(CreateTrainingQuizDto dto)
    {
        // Quiz bağımsız oluşturulur, video sonradan quiz seçer
        var quiz = new TrainingQuiz
        {
            TrainingVideoId = dto.TrainingVideoId,
            Title = dto.Title,
            Description = dto.Description,
            PassingScore = dto.PassingScore,
            IsRequired = dto.IsRequired,
            ShuffleQuestions = dto.ShuffleQuestions,
            ShuffleOptions = dto.ShuffleOptions,
            ShowResults = dto.ShowResults,
            IsActive = true
        };

        // Soruları ekle
        var questionOrder = 0;
        foreach (var questionDto in dto.Questions)
        {
            var question = new TrainingQuizQuestion
            {
                Text = questionDto.Text,
                HelpText = questionDto.HelpText,
                Order = questionDto.Order > 0 ? questionDto.Order : ++questionOrder,
                QuestionTypeId = questionDto.QuestionTypeId
            };

            // Seçenekleri ekle
            var optionOrder = 0;
            foreach (var optionDto in questionDto.Options)
            {
                question.Options.Add(new TrainingQuizOption
                {
                    Text = optionDto.Text,
                    Order = optionDto.Order > 0 ? optionDto.Order : ++optionOrder,
                    WeightPoints = optionDto.WeightPoints,
                    IsCorrect = optionDto.IsCorrect
                });
            }

            quiz.Questions.Add(question);
        }

        _context.TrainingQuizzes.Add(quiz);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(quiz.Id))!;
    }

    public async Task<TrainingQuizDto> UpdateAsync(int id, UpdateTrainingQuizDto dto)
    {
        var quiz = await _context.TrainingQuizzes.FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);
        if (quiz == null)
            throw new Exception("Anket bulunamadı");

        quiz.Title = dto.Title;
        quiz.Description = dto.Description;
        quiz.PassingScore = dto.PassingScore;
        quiz.IsRequired = dto.IsRequired;
        quiz.ShuffleQuestions = dto.ShuffleQuestions;
        quiz.ShuffleOptions = dto.ShuffleOptions;
        quiz.ShowResults = dto.ShowResults;
        quiz.IsActive = dto.IsActive;
        quiz.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (await GetByIdAsync(quiz.Id))!;
    }

    public async Task DeleteAsync(int id)
    {
        var quiz = await _context.TrainingQuizzes.FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);
        if (quiz == null)
            throw new Exception("Anket bulunamadı");

        quiz.IsDeleted = true;
        quiz.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Sorular

    public async Task<TrainingQuizQuestionDto> AddQuestionAsync(int quizId, CreateTrainingQuizQuestionDto dto)
    {
        var quiz = await _context.TrainingQuizzes
            .Include(q => q.Questions.Where(qu => !qu.IsDeleted))
            .FirstOrDefaultAsync(q => q.Id == quizId && !q.IsDeleted);

        if (quiz == null)
            throw new Exception("Anket bulunamadı");

        var maxOrder = quiz.Questions.Any() ? quiz.Questions.Max(q => q.Order) : 0;

        var question = new TrainingQuizQuestion
        {
            TrainingQuizId = quizId,
            Text = dto.Text,
            HelpText = dto.HelpText,
            Order = dto.Order > 0 ? dto.Order : maxOrder + 1,
            QuestionTypeId = dto.QuestionTypeId
        };

        // Seçenekleri ekle
        var optionOrder = 0;
        foreach (var optionDto in dto.Options)
        {
            question.Options.Add(new TrainingQuizOption
            {
                Text = optionDto.Text,
                Order = optionDto.Order > 0 ? optionDto.Order : ++optionOrder,
                WeightPoints = optionDto.WeightPoints,
                IsCorrect = optionDto.IsCorrect
            });
        }

        _context.TrainingQuizQuestions.Add(question);
        await _context.SaveChangesAsync();

        return MapQuestionToDto(question);
    }

    public async Task<TrainingQuizQuestionDto> UpdateQuestionAsync(int questionId, UpdateTrainingQuizQuestionDto dto)
    {
        var question = await _context.TrainingQuizQuestions
            .Include(q => q.Options.Where(o => !o.IsDeleted))
            .FirstOrDefaultAsync(q => q.Id == questionId && !q.IsDeleted);

        if (question == null)
            throw new Exception("Soru bulunamadı");

        question.Text = dto.Text;
        question.HelpText = dto.HelpText;
        question.Order = dto.Order;
        question.QuestionTypeId = dto.QuestionTypeId;
        question.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapQuestionToDto(question);
    }

    public async Task DeleteQuestionAsync(int questionId)
    {
        var question = await _context.TrainingQuizQuestions.FirstOrDefaultAsync(q => q.Id == questionId && !q.IsDeleted);
        if (question == null)
            throw new Exception("Soru bulunamadı");

        question.IsDeleted = true;
        question.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task ReorderQuestionsAsync(int quizId, List<int> questionIds)
    {
        var questions = await _context.TrainingQuizQuestions
            .Where(q => q.TrainingQuizId == quizId && !q.IsDeleted && questionIds.Contains(q.Id))
            .ToListAsync();

        for (int i = 0; i < questionIds.Count; i++)
        {
            var question = questions.FirstOrDefault(q => q.Id == questionIds[i]);
            if (question != null)
            {
                question.Order = i + 1;
                question.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Seçenekler

    public async Task<TrainingQuizOptionDto> AddOptionAsync(int questionId, CreateTrainingQuizOptionDto dto)
    {
        var question = await _context.TrainingQuizQuestions
            .Include(q => q.Options.Where(o => !o.IsDeleted))
            .FirstOrDefaultAsync(q => q.Id == questionId && !q.IsDeleted);

        if (question == null)
            throw new Exception("Soru bulunamadı");

        var maxOrder = question.Options.Any() ? question.Options.Max(o => o.Order) : 0;

        var option = new TrainingQuizOption
        {
            TrainingQuizQuestionId = questionId,
            Text = dto.Text,
            Order = dto.Order > 0 ? dto.Order : maxOrder + 1,
            WeightPoints = dto.WeightPoints,
            IsCorrect = dto.IsCorrect
        };

        _context.TrainingQuizOptions.Add(option);
        await _context.SaveChangesAsync();

        return MapOptionToDto(option);
    }

    public async Task<TrainingQuizOptionDto> UpdateOptionAsync(int optionId, UpdateTrainingQuizOptionDto dto)
    {
        var option = await _context.TrainingQuizOptions.FirstOrDefaultAsync(o => o.Id == optionId && !o.IsDeleted);

        if (option == null)
            throw new Exception("Seçenek bulunamadı");

        option.Text = dto.Text;
        option.Order = dto.Order;
        option.WeightPoints = dto.WeightPoints;
        option.IsCorrect = dto.IsCorrect;
        option.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapOptionToDto(option);
    }

    public async Task DeleteOptionAsync(int optionId)
    {
        var option = await _context.TrainingQuizOptions.FirstOrDefaultAsync(o => o.Id == optionId && !o.IsDeleted);
        if (option == null)
            throw new Exception("Seçenek bulunamadı");

        option.IsDeleted = true;
        option.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task ReorderOptionsAsync(int questionId, List<int> optionIds)
    {
        var options = await _context.TrainingQuizOptions
            .Where(o => o.TrainingQuizQuestionId == questionId && !o.IsDeleted && optionIds.Contains(o.Id))
            .ToListAsync();

        for (int i = 0; i < optionIds.Count; i++)
        {
            var option = options.FirstOrDefault(o => o.Id == optionIds[i]);
            if (option != null)
            {
                option.Order = i + 1;
                option.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Katılımcı Tarafı

    public async Task<ParticipantQuizDto?> GetQuizForParticipantAsync(int participantId, bool isExternal = false)
    {
        // Katılımcının assignment'ını bul
        int videoId;
        if (isExternal)
        {
            var extParticipant = await _context.TrainingVideoExternalParticipants
                .Include(p => p.Assignment)
                .FirstOrDefaultAsync(p => p.Id == participantId && !p.IsDeleted);
            if (extParticipant == null) return null;
            videoId = extParticipant.Assignment.TrainingVideoId;
        }
        else
        {
            var participant = await _context.TrainingVideoParticipants
                .Include(p => p.Assignment)
                .FirstOrDefaultAsync(p => p.Id == participantId && !p.IsDeleted);
            if (participant == null) return null;
            videoId = participant.Assignment.TrainingVideoId;
        }

        // Video'ya bağlı aktif quiz'i bul
        var quiz = await _context.TrainingQuizzes
            .Include(q => q.Questions.Where(qu => !qu.IsDeleted))
            .Include(q => q.Responses.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(q => q.TrainingVideoId == videoId && !q.IsDeleted && q.IsActive);

        if (quiz == null) return null;

        // Katılımcının önceki denemeleri
        var previousResponses = isExternal
            ? quiz.Responses.Where(r => r.TrainingVideoExternalParticipantId == participantId).ToList()
            : quiz.Responses.Where(r => r.TrainingVideoParticipantId == participantId).ToList();

        var lastResponse = previousResponses.OrderByDescending(r => r.AttemptNumber).FirstOrDefault();

        return new ParticipantQuizDto
        {
            QuizId = quiz.Id,
            Title = quiz.Title,
            Description = quiz.Description,
            PassingScore = quiz.PassingScore,
            IsRequired = quiz.IsRequired,
            QuestionCount = quiz.Questions.Count,
            HasPreviousAttempt = previousResponses.Any(),
            AttemptCount = previousResponses.Count,
            LastAttemptResult = lastResponse != null ? new TrainingQuizResultDto
            {
                ResponseId = lastResponse.Id,
                QuizId = quiz.Id,
                QuizTitle = quiz.Title,
                TotalScore = lastResponse.TotalScore,
                MaxPossibleScore = lastResponse.MaxPossibleScore,
                ScorePercentage = lastResponse.ScorePercentage,
                PassingScore = quiz.PassingScore,
                IsPassed = lastResponse.IsPassed,
                ShowResults = quiz.ShowResults
            } : null
        };
    }

    public async Task<ParticipantQuizQuestionsDto?> StartQuizAsync(int participantId, bool isExternal = false)
    {
        var quizInfo = await GetQuizForParticipantAsync(participantId, isExternal);
        if (quizInfo == null) return null;

        var quiz = await _context.TrainingQuizzes
            .Include(q => q.Questions.Where(qu => !qu.IsDeleted).OrderBy(qu => qu.Order))
                .ThenInclude(qu => qu.Options.Where(o => !o.IsDeleted).OrderBy(o => o.Order))
            .FirstOrDefaultAsync(q => q.Id == quizInfo.QuizId);

        if (quiz == null) return null;

        var questions = quiz.Questions.ToList();
        var options = questions.SelectMany(q => q.Options).ToList();

        // Shuffle if enabled
        if (quiz.ShuffleQuestions)
        {
            var random = new Random();
            questions = questions.OrderBy(_ => random.Next()).ToList();
        }

        return new ParticipantQuizQuestionsDto
        {
            QuizId = quiz.Id,
            Title = quiz.Title,
            Description = quiz.Description,
            PassingScore = quiz.PassingScore,
            Questions = questions.Select(q =>
            {
                var qOptions = q.Options.ToList();
                if (quiz.ShuffleOptions)
                {
                    var random = new Random();
                    qOptions = qOptions.OrderBy(_ => random.Next()).ToList();
                }

                return new ParticipantQuizQuestionDto
                {
                    Id = q.Id,
                    Text = q.Text,
                    HelpText = q.HelpText,
                    Order = q.Order,
                    QuestionTypeId = q.QuestionTypeId,
                    QuestionTypeName = TrainingQuizQuestionTypes.GetById(q.QuestionTypeId)?.SystemName ?? "Unknown",
                    Options = qOptions.Select(o => new ParticipantQuizOptionDto
                    {
                        Id = o.Id,
                        Text = o.Text,
                        Order = o.Order
                    }).ToList()
                };
            }).ToList()
        };
    }

    public async Task<TrainingQuizResultDto> SubmitQuizAsync(SubmitTrainingQuizDto dto)
    {
        var quiz = await _context.TrainingQuizzes
            .Include(q => q.Questions.Where(qu => !qu.IsDeleted))
                .ThenInclude(qu => qu.Options.Where(o => !o.IsDeleted))
            .FirstOrDefaultAsync(q => q.Id == dto.QuizId && !q.IsDeleted);

        if (quiz == null)
            throw new Exception("Anket bulunamadı");

        // Önceki deneme sayısını bul
        var previousAttempts = await _context.TrainingQuizResponses
            .Where(r => r.TrainingQuizId == dto.QuizId && !r.IsDeleted &&
                       (dto.ParticipantId.HasValue ? r.TrainingVideoParticipantId == dto.ParticipantId : r.TrainingVideoExternalParticipantId == dto.ExternalParticipantId))
            .CountAsync();

        // Yeni response oluştur
        var response = new TrainingQuizResponse
        {
            TrainingQuizId = dto.QuizId,
            TrainingVideoParticipantId = dto.ParticipantId,
            TrainingVideoExternalParticipantId = dto.ExternalParticipantId,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StatusId = TrainingQuizResponseStatuses.Ids.Completed,
            AttemptNumber = previousAttempts + 1
        };

        decimal totalScore = 0;
        decimal maxPossibleScore = 0;
        int correctCount = 0;

        var answerResults = new List<TrainingQuizAnswerResultDto>();

        foreach (var question in quiz.Questions)
        {
            var answerDto = dto.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            var answer = new TrainingQuizAnswer
            {
                TrainingQuizQuestionId = question.Id
            };

            // Maksimum alabileceği puan (en yüksek puan veren doğru seçeneklerin toplamı)
            var correctOptions = question.Options.Where(o => o.IsCorrect).ToList();
            var questionMaxScore = correctOptions.Sum(o => o.WeightPoints);
            maxPossibleScore += questionMaxScore;

            decimal earnedPoints = 0;
            bool isCorrect = false;

            if (question.QuestionTypeId == TrainingQuizQuestionTypes.Ids.SingleChoice)
            {
                // Tekli seçim
                answer.SelectedOptionId = answerDto?.SelectedOptionId;

                if (answerDto?.SelectedOptionId.HasValue == true)
                {
                    var selectedOption = question.Options.FirstOrDefault(o => o.Id == answerDto.SelectedOptionId);
                    if (selectedOption != null)
                    {
                        earnedPoints = selectedOption.WeightPoints;
                        isCorrect = selectedOption.IsCorrect;
                    }
                }
            }
            else
            {
                // Çoklu seçim
                var selectedIds = answerDto?.SelectedOptionIds ?? new List<int>();
                answer.SelectedOptionIds = selectedIds.Any() ? string.Join(",", selectedIds) : null;

                // Her seçilen doğru seçenek puan verir
                foreach (var selectedId in selectedIds)
                {
                    var selectedOption = question.Options.FirstOrDefault(o => o.Id == selectedId);
                    if (selectedOption != null)
                    {
                        earnedPoints += selectedOption.WeightPoints;
                    }
                }

                // Tüm doğru seçenekler seçildi ve yanlış seçilmedi mi?
                var correctOptionIds = correctOptions.Select(o => o.Id).ToHashSet();
                isCorrect = selectedIds.Count == correctOptionIds.Count && selectedIds.All(id => correctOptionIds.Contains(id));
            }

            answer.EarnedPoints = earnedPoints;
            answer.IsCorrect = isCorrect;

            if (isCorrect) correctCount++;
            totalScore += earnedPoints;

            response.Answers.Add(answer);

            // Sonuç için
            var selectedOptionText = answerDto?.SelectedOptionId.HasValue == true
                ? question.Options.FirstOrDefault(o => o.Id == answerDto.SelectedOptionId)?.Text
                : null;

            answerResults.Add(new TrainingQuizAnswerResultDto
            {
                QuestionId = question.Id,
                QuestionText = question.Text,
                IsCorrect = isCorrect,
                EarnedPoints = earnedPoints,
                SelectedOptionId = answerDto?.SelectedOptionId,
                SelectedOptionText = selectedOptionText,
                SelectedOptionIds = answerDto?.SelectedOptionIds,
                Options = quiz.ShowResults ? question.Options.Select(o => new TrainingQuizOptionResultDto
                {
                    Id = o.Id,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect,
                    WasSelected = answerDto?.SelectedOptionId == o.Id ||
                                 (answerDto?.SelectedOptionIds?.Contains(o.Id) ?? false)
                }).ToList() : null
            });
        }

        // Puanları hesapla
        response.TotalScore = totalScore;
        response.MaxPossibleScore = maxPossibleScore;
        response.ScorePercentage = maxPossibleScore > 0 ? Math.Round((totalScore / maxPossibleScore) * 100, 2) : 0;
        response.IsPassed = !quiz.PassingScore.HasValue || response.ScorePercentage >= quiz.PassingScore.Value;

        _context.TrainingQuizResponses.Add(response);
        await _context.SaveChangesAsync();

        return new TrainingQuizResultDto
        {
            ResponseId = response.Id,
            QuizId = quiz.Id,
            QuizTitle = quiz.Title,
            TotalScore = quiz.ShowResults ? totalScore : null,
            MaxPossibleScore = quiz.ShowResults ? maxPossibleScore : null,
            ScorePercentage = quiz.ShowResults ? response.ScorePercentage : null,
            PassingScore = quiz.ShowResults ? quiz.PassingScore : null,
            IsPassed = response.IsPassed,
            CorrectCount = quiz.ShowResults ? correctCount : null,
            TotalQuestions = quiz.ShowResults ? quiz.Questions.Count : null,
            ShowResults = quiz.ShowResults,
            AnswerResults = quiz.ShowResults ? answerResults : null
        };
    }

    #endregion

    #region Quiz Yanıtları (Admin)

    public async Task<IEnumerable<TrainingQuizResponseListDto>> GetResponsesAsync(TrainingQuizResponseFilterDto? filter = null)
    {
        var query = _context.TrainingQuizResponses
            .Include(r => r.TrainingQuiz)
            .Include(r => r.Participant)
                .ThenInclude(p => p!.CustomerPersonnel)
            .Include(r => r.ExternalParticipant)
            .Where(r => !r.IsDeleted);

        if (filter != null)
        {
            if (filter.QuizId.HasValue)
                query = query.Where(r => r.TrainingQuizId == filter.QuizId.Value);

            if (filter.VideoId.HasValue)
                query = query.Where(r => r.TrainingQuiz.TrainingVideoId == filter.VideoId.Value);

            if (filter.IsPassed.HasValue)
                query = query.Where(r => r.IsPassed == filter.IsPassed.Value);

            if (filter.IsExternal.HasValue)
            {
                if (filter.IsExternal.Value)
                    query = query.Where(r => r.TrainingVideoExternalParticipantId.HasValue);
                else
                    query = query.Where(r => r.TrainingVideoParticipantId.HasValue);
            }

            if (filter.StatusId.HasValue)
                query = query.Where(r => r.StatusId == filter.StatusId.Value);
        }

        var responses = await query.OrderByDescending(r => r.CompletedAt ?? r.StartedAt).ToListAsync();

        return responses.Select(MapResponseToListDto);
    }

    public async Task<TrainingQuizResponseDto?> GetResponseByIdAsync(int responseId)
    {
        var response = await _context.TrainingQuizResponses
            .Include(r => r.TrainingQuiz)
            .Include(r => r.Participant)
                .ThenInclude(p => p!.CustomerPersonnel)
            .Include(r => r.ExternalParticipant)
            .Include(r => r.Answers.Where(a => !a.IsDeleted))
                .ThenInclude(a => a.Question)
            .Include(r => r.Answers.Where(a => !a.IsDeleted))
                .ThenInclude(a => a.SelectedOption)
            .FirstOrDefaultAsync(r => r.Id == responseId && !r.IsDeleted);

        if (response == null) return null;

        var dto = new TrainingQuizResponseDto
        {
            Id = response.Id,
            TrainingQuizId = response.TrainingQuizId,
            QuizTitle = response.TrainingQuiz?.Title ?? "",
            TrainingVideoParticipantId = response.TrainingVideoParticipantId,
            TrainingVideoExternalParticipantId = response.TrainingVideoExternalParticipantId,
            ParticipantName = GetParticipantName(response),
            ParticipantEmail = GetParticipantEmail(response),
            IsExternal = response.TrainingVideoExternalParticipantId.HasValue,
            TotalScore = response.TotalScore,
            MaxPossibleScore = response.MaxPossibleScore,
            ScorePercentage = response.ScorePercentage,
            IsPassed = response.IsPassed,
            StartedAt = response.StartedAt,
            CompletedAt = response.CompletedAt,
            StatusId = response.StatusId,
            StatusName = TrainingQuizResponseStatuses.GetById(response.StatusId)?.SystemName ?? "Unknown",
            AttemptNumber = response.AttemptNumber,
            Answers = response.Answers.Select(a => new TrainingQuizAnswerDto
            {
                Id = a.Id,
                TrainingQuizResponseId = a.TrainingQuizResponseId,
                TrainingQuizQuestionId = a.TrainingQuizQuestionId,
                QuestionText = a.Question?.Text ?? "",
                SelectedOptionId = a.SelectedOptionId,
                SelectedOptionText = a.SelectedOption?.Text,
                SelectedOptionIds = a.SelectedOptionIds,
                EarnedPoints = a.EarnedPoints,
                IsCorrect = a.IsCorrect
            }).ToList()
        };

        return dto;
    }

    public async Task<Core.DTOs.Report.ExcelExportDto> ExportResponsesAsync(TrainingQuizResponseFilterDto? filter = null)
    {
        var responses = await GetResponsesAsync(filter);
        var responseList = responses.ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Anket Yanıtları");

        // Headers
        var headers = new[] { "Katılımcı", "E-posta", "Tip", "Anket", "Puan", "Maks Puan", "Yüzde", "Sonuç", "Deneme", "Tamamlanma" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        int row = 2;
        foreach (var item in responseList)
        {
            worksheet.Cell(row, 1).Value = item.ParticipantName;
            worksheet.Cell(row, 2).Value = item.ParticipantEmail ?? "";
            worksheet.Cell(row, 3).Value = item.IsExternal ? "Dış" : "İç";
            worksheet.Cell(row, 4).Value = item.QuizTitle;
            worksheet.Cell(row, 5).Value = item.TotalScore;
            worksheet.Cell(row, 6).Value = item.MaxPossibleScore;
            worksheet.Cell(row, 7).Value = item.ScorePercentage;
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "0.00";
            worksheet.Cell(row, 8).Value = item.IsPassed ? "Geçti" : "Kaldı";
            worksheet.Cell(row, 9).Value = item.AttemptNumber;
            worksheet.Cell(row, 10).Value = item.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new Core.DTOs.Report.ExcelExportDto
        {
            FileName = $"Anket_Yanitlari_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    #endregion

    #region Video Quiz Durumu

    public async Task<VideoQuizStatusDto> GetVideoQuizStatusAsync(int videoId, int? participantId = null, int? externalParticipantId = null)
    {
        var quiz = await _context.TrainingQuizzes
            .Include(q => q.Responses.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(q => q.TrainingVideoId == videoId && !q.IsDeleted && q.IsActive);

        var status = new VideoQuizStatusDto
        {
            VideoId = videoId,
            HasQuiz = quiz != null
        };

        if (quiz != null)
        {
            status.QuizId = quiz.Id;
            status.QuizTitle = quiz.Title;
            status.IsRequired = quiz.IsRequired;
            status.PassingScore = quiz.PassingScore;

            // Katılımcı varsa durumunu kontrol et
            if (participantId.HasValue || externalParticipantId.HasValue)
            {
                var responses = participantId.HasValue
                    ? quiz.Responses.Where(r => r.TrainingVideoParticipantId == participantId).ToList()
                    : quiz.Responses.Where(r => r.TrainingVideoExternalParticipantId == externalParticipantId).ToList();

                status.HasAttempted = responses.Any();
                status.AttemptCount = responses.Count;

                if (responses.Any())
                {
                    var lastResponse = responses.OrderByDescending(r => r.AttemptNumber).First();
                    status.IsPassed = lastResponse.IsPassed;
                    status.LastScore = lastResponse.ScorePercentage;
                }
            }
        }

        return status;
    }

    #endregion

    #region Video-Quiz Atama

    public async Task<IEnumerable<TrainingQuizListDto>> GetAvailableQuizzesForVideoAsync(int? videoId = null)
    {
        // Video'ya atanabilecek quizleri getir:
        // - Henüz hiçbir videoya atanmamış (TrainingVideoId == null)
        // - VEYA bu video'ya zaten atanmış (TrainingVideoId == videoId)
        var quizzes = await _context.TrainingQuizzes
            .Include(q => q.Questions.Where(qu => !qu.IsDeleted))
            .Include(q => q.Responses.Where(r => !r.IsDeleted))
            .Where(q => !q.IsDeleted && q.IsActive &&
                       (q.TrainingVideoId == null || (videoId.HasValue && q.TrainingVideoId == videoId)))
            .OrderBy(q => q.Title)
            .ToListAsync();

        return quizzes.Select(q => new TrainingQuizListDto
        {
            Id = q.Id,
            TrainingVideoId = q.TrainingVideoId,
            TrainingVideoTitle = null,
            Title = q.Title,
            Description = q.Description,
            PassingScore = q.PassingScore,
            IsRequired = q.IsRequired,
            IsActive = q.IsActive,
            QuestionCount = q.Questions.Count,
            ResponseCount = q.Responses.Count,
            CreatedAt = q.CreatedAt
        });
    }

    public async Task AssignQuizToVideoAsync(int? quizId, int videoId)
    {
        // Önce bu video'ya atanmış mevcut quiz'i bul ve atamasını kaldır
        var currentQuiz = await _context.TrainingQuizzes
            .FirstOrDefaultAsync(q => q.TrainingVideoId == videoId && !q.IsDeleted);

        if (currentQuiz != null)
        {
            currentQuiz.TrainingVideoId = null;
            currentQuiz.UpdatedAt = DateTime.UtcNow;
        }

        // Yeni quiz atanacaksa
        if (quizId.HasValue)
        {
            var newQuiz = await _context.TrainingQuizzes
                .FirstOrDefaultAsync(q => q.Id == quizId.Value && !q.IsDeleted);

            if (newQuiz == null)
                throw new Exception("Anket bulunamadı");

            // Başka bir video'ya atanmış mı kontrol et
            if (newQuiz.TrainingVideoId.HasValue && newQuiz.TrainingVideoId != videoId)
                throw new Exception("Bu anket başka bir videoya atanmış");

            newQuiz.TrainingVideoId = videoId;
            newQuiz.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Helper Methods

    private TrainingQuizDto MapToDto(TrainingQuiz quiz)
    {
        return new TrainingQuizDto
        {
            Id = quiz.Id,
            TrainingVideoId = quiz.TrainingVideoId,
            TrainingVideoTitle = quiz.TrainingVideo?.Title ?? "",
            Title = quiz.Title,
            Description = quiz.Description,
            PassingScore = quiz.PassingScore,
            IsRequired = quiz.IsRequired,
            ShuffleQuestions = quiz.ShuffleQuestions,
            ShuffleOptions = quiz.ShuffleOptions,
            ShowResults = quiz.ShowResults,
            IsActive = quiz.IsActive,
            QuestionCount = quiz.Questions.Count,
            ResponseCount = quiz.Responses.Count,
            CreatedAt = quiz.CreatedAt,
            Questions = quiz.Questions.Select(MapQuestionToDto).ToList()
        };
    }

    private TrainingQuizQuestionDto MapQuestionToDto(TrainingQuizQuestion question)
    {
        return new TrainingQuizQuestionDto
        {
            Id = question.Id,
            TrainingQuizId = question.TrainingQuizId,
            Text = question.Text,
            HelpText = question.HelpText,
            Order = question.Order,
            QuestionTypeId = question.QuestionTypeId,
            QuestionTypeName = TrainingQuizQuestionTypes.GetById(question.QuestionTypeId)?.SystemName ?? "Unknown",
            Options = question.Options.Select(MapOptionToDto).ToList()
        };
    }

    private TrainingQuizOptionDto MapOptionToDto(TrainingQuizOption option)
    {
        return new TrainingQuizOptionDto
        {
            Id = option.Id,
            TrainingQuizQuestionId = option.TrainingQuizQuestionId,
            Text = option.Text,
            Order = option.Order,
            WeightPoints = option.WeightPoints,
            IsCorrect = option.IsCorrect
        };
    }

    private TrainingQuizResponseListDto MapResponseToListDto(TrainingQuizResponse response)
    {
        return new TrainingQuizResponseListDto
        {
            Id = response.Id,
            TrainingQuizId = response.TrainingQuizId,
            QuizTitle = response.TrainingQuiz?.Title ?? "",
            TrainingVideoParticipantId = response.TrainingVideoParticipantId,
            TrainingVideoExternalParticipantId = response.TrainingVideoExternalParticipantId,
            ParticipantName = GetParticipantName(response),
            ParticipantEmail = GetParticipantEmail(response),
            IsExternal = response.TrainingVideoExternalParticipantId.HasValue,
            TotalScore = response.TotalScore,
            MaxPossibleScore = response.MaxPossibleScore,
            ScorePercentage = response.ScorePercentage,
            IsPassed = response.IsPassed,
            StartedAt = response.StartedAt,
            CompletedAt = response.CompletedAt,
            StatusId = response.StatusId,
            StatusName = TrainingQuizResponseStatuses.GetById(response.StatusId)?.SystemName ?? "Unknown",
            AttemptNumber = response.AttemptNumber
        };
    }

    private string GetParticipantName(TrainingQuizResponse response)
    {
        if (response.Participant?.CustomerPersonnel != null)
            return $"{response.Participant.CustomerPersonnel.FirstName} {response.Participant.CustomerPersonnel.LastName}".Trim();

        if (response.ExternalParticipant != null)
            return response.ExternalParticipant.FullName ?? response.ExternalParticipant.Email;

        return "Bilinmeyen";
    }

    private string? GetParticipantEmail(TrainingQuizResponse response)
    {
        if (response.Participant?.CustomerPersonnel != null)
            return response.Participant.CustomerPersonnel.Email;

        return response.ExternalParticipant?.Email;
    }

    #endregion
}
