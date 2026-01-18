using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.TrainingVideo;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class TrainingVideoService : ITrainingVideoService
{
    private readonly ApplicationDbContext _context;
    private readonly string _videoStoragePath;

    public TrainingVideoService(ApplicationDbContext context, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _context = context;
        _videoStoragePath = configuration["Storage:VideoPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Videos");

        // Klasör yoksa oluştur
        if (!Directory.Exists(_videoStoragePath))
            Directory.CreateDirectory(_videoStoragePath);
    }

    // Helper: DateTime'ı UTC'ye çevir
    private static DateTime ToUtc(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc) return dateTime;
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    private static DateTime? ToUtc(DateTime? dateTime)
    {
        if (!dateTime.HasValue) return null;
        return ToUtc(dateTime.Value);
    }

    #region Video CRUD

    public async Task<TrainingVideoDto?> GetByIdAsync(int id)
    {
        var video = await _context.TrainingVideos
            .Include(v => v.Scopes)
                .ThenInclude(s => s.Checklist)
            .Include(v => v.Scopes)
                .ThenInclude(s => s.Question)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        return video == null ? null : MapToDto(video);
    }

    public async Task<IEnumerable<TrainingVideoListDto>> GetListAsync(TrainingVideoFilterDto? filter = null)
    {
        var query = _context.TrainingVideos
            .Include(v => v.Scopes)
            .Include(v => v.Assignments)
                .ThenInclude(a => a.Participants)
            .Where(v => !v.IsDeleted);

        // Filtreler
        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(v => v.Title.ToLower().Contains(term) ||
                                        (v.Description != null && v.Description.ToLower().Contains(term)));
            }

            if (filter.IsActive.HasValue)
                query = query.Where(v => v.IsActive == filter.IsActive.Value);

            if (filter.ChecklistIds?.Any() == true)
                query = query.Where(v => v.Scopes.Any(s => s.ChecklistId.HasValue && filter.ChecklistIds.Contains(s.ChecklistId.Value)));

            if (filter.QuestionGroupNames?.Any() == true)
                query = query.Where(v => v.Scopes.Any(s => s.QuestionGroupName != null && filter.QuestionGroupNames.Contains(s.QuestionGroupName)));
        }

        var videos = await query.OrderByDescending(v => v.CreatedAt).ToListAsync();

        return videos.Select(v => new TrainingVideoListDto
        {
            Id = v.Id,
            Title = v.Title,
            Description = v.Description,
            DurationSeconds = v.DurationSeconds,
            FileSize = v.FileSize,
            IsActive = v.IsActive,
            CreatedAt = v.CreatedAt,
            ScopeCount = v.Scopes.Count(s => !s.IsDeleted),
            AssignmentCount = v.Assignments.Count(a => !a.IsDeleted),
            TotalParticipants = v.Assignments.Where(a => !a.IsDeleted).SelectMany(a => a.Participants).Count(p => !p.IsDeleted),
            CompletedParticipants = v.Assignments.Where(a => !a.IsDeleted).SelectMany(a => a.Participants).Count(p => !p.IsDeleted && p.IsCompleted),
            ThumbnailUrl = !string.IsNullOrEmpty(v.ThumbnailPath) ? $"/api/training-videos/{v.Id}/thumbnail" : null
        });
    }

    public async Task<TrainingVideoDto> CreateAsync(CreateTrainingVideoDto dto, Stream videoStream, string fileName, Stream? thumbnailStream = null)
    {
        // Video dosyasını kaydet
        var fileExtension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(_videoStoragePath, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await videoStream.CopyToAsync(fileStream);
        }

        var fileInfo = new FileInfo(filePath);

        // Thumbnail kaydet
        string? thumbnailPath = null;
        if (thumbnailStream != null)
        {
            var thumbnailFileName = $"{Guid.NewGuid()}.jpg";
            thumbnailPath = Path.Combine(_videoStoragePath, "thumbnails", thumbnailFileName);

            // Thumbnails klasörünü oluştur
            Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);

            using (var thumbStream = new FileStream(thumbnailPath, FileMode.Create))
            {
                await thumbnailStream.CopyToAsync(thumbStream);
            }
        }

        var video = new TrainingVideo
        {
            Title = dto.Title,
            Description = dto.Description,
            FileName = fileName,
            FilePath = filePath,
            FileSize = fileInfo.Length,
            DurationSeconds = dto.DurationSeconds, // Frontend'den gelen süre
            ThumbnailPath = thumbnailPath,
            IsActive = true
        };

        // Kapsamları ekle
        if (dto.Scopes?.Any() == true)
        {
            foreach (var scopeDto in dto.Scopes)
            {
                video.Scopes.Add(new TrainingVideoScope
                {
                    ScopeTypeId = scopeDto.ScopeTypeId,
                    ChecklistId = scopeDto.ChecklistId,
                    QuestionGroupName = scopeDto.QuestionGroupName,
                    QuestionId = scopeDto.QuestionId
                });
            }
        }

        _context.TrainingVideos.Add(video);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(video.Id))!;
    }

    public async Task<TrainingVideoDto> UpdateAsync(int id, UpdateTrainingVideoDto dto)
    {
        var video = await _context.TrainingVideos
            .Include(v => v.Scopes)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (video == null)
            throw new Exception("Video bulunamadı");

        video.Title = dto.Title;
        video.Description = dto.Description;
        video.IsActive = dto.IsActive;
        video.UpdatedAt = DateTime.UtcNow;

        // Kapsamları güncelle - önce sil sonra ekle
        foreach (var scope in video.Scopes.ToList())
        {
            scope.IsDeleted = true;
        }

        if (dto.Scopes?.Any() == true)
        {
            foreach (var scopeDto in dto.Scopes)
            {
                video.Scopes.Add(new TrainingVideoScope
                {
                    ScopeTypeId = scopeDto.ScopeTypeId,
                    ChecklistId = scopeDto.ChecklistId,
                    QuestionGroupName = scopeDto.QuestionGroupName,
                    QuestionId = scopeDto.QuestionId
                });
            }
        }

        await _context.SaveChangesAsync();

        return (await GetByIdAsync(video.Id))!;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var video = await _context.TrainingVideos.FindAsync(id);
        if (video == null || video.IsDeleted)
            return false;

        video.IsDeleted = true;
        video.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Kapsam Seçenekleri

    public async Task<TrainingVideoScopeOptionsDto> GetScopeOptionsAsync()
    {
        var checklists = await _context.Checklists
            .Where(c => !c.IsDeleted && c.IsActive)
            .Select(c => new ChecklistOptionDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();

        var questionGroups = await _context.Questions
            .Where(q => !q.IsDeleted && !string.IsNullOrEmpty(q.GroupName))
            .GroupBy(q => new { q.ChecklistId, q.GroupName })
            .Select(g => new QuestionGroupOptionDto
            {
                ChecklistId = g.Key.ChecklistId,
                GroupName = g.Key.GroupName!,
                QuestionCount = g.Count()
            })
            .ToListAsync();

        var questions = await _context.Questions
            .Include(q => q.Checklist)
            .Where(q => !q.IsDeleted)
            .Select(q => new QuestionOptionDto
            {
                Id = q.Id,
                Text = q.Text,
                GroupName = q.GroupName,
                ChecklistId = q.ChecklistId,
                ChecklistName = q.Checklist.Name
            })
            .ToListAsync();

        return new TrainingVideoScopeOptionsDto
        {
            Checklists = checklists,
            QuestionGroups = questionGroups,
            Questions = questions
        };
    }

    #endregion

    #region Atama CRUD

    public async Task<TrainingVideoAssignmentDto?> GetAssignmentByIdAsync(int id)
    {
        var assignment = await _context.TrainingVideoAssignments
            .Include(a => a.TrainingVideo)
            .Include(a => a.SourceProject)
            .Include(a => a.Participants)
                .ThenInclude(p => p.CustomerPersonnel)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        return assignment == null ? null : MapToAssignmentDto(assignment);
    }

    public async Task<IEnumerable<TrainingVideoAssignmentListDto>> GetAssignmentsAsync(TrainingVideoAssignmentFilterDto? filter = null)
    {
        var query = _context.TrainingVideoAssignments
            .Include(a => a.TrainingVideo)
            .Include(a => a.Participants)
            .Where(a => !a.IsDeleted);

        // Filtreler
        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(term) ||
                                        a.TrainingVideo.Title.ToLower().Contains(term));
            }

            if (filter.VideoIds?.Any() == true)
                query = query.Where(a => filter.VideoIds.Contains(a.TrainingVideoId));

            if (filter.ProjectIds?.Any() == true)
                query = query.Where(a => a.SourceProjectId.HasValue && filter.ProjectIds.Contains(a.SourceProjectId.Value));

            if (filter.IsActive.HasValue)
                query = query.Where(a => a.IsActive == filter.IsActive.Value);
        }

        var assignments = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();

        return assignments.Select(a =>
        {
            var participants = a.Participants.Where(p => !p.IsDeleted).ToList();
            var completed = participants.Count(p => p.IsCompleted);
            var inProgress = participants.Count(p => p.StatusId == TrainingVideoParticipantStatuses.Ids.InProgress);
            var total = participants.Count;

            return new TrainingVideoAssignmentListDto
            {
                Id = a.Id,
                Title = a.Title,
                TrainingVideoId = a.TrainingVideoId,
                TrainingVideoTitle = a.TrainingVideo.Title,
                StartDate = a.StartDate,
                DueDate = a.DueDate,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                TotalParticipants = total,
                CompletedParticipants = completed,
                InProgressParticipants = inProgress,
                CompletionPercentage = total > 0 ? Math.Round((decimal)completed / total * 100, 1) : 0
            };
        });
    }

    public async Task<TrainingVideoAssignmentDto> CreateAssignmentAsync(CreateTrainingVideoAssignmentDto dto)
    {
        var video = await _context.TrainingVideos
            .Include(v => v.Scopes)
            .FirstOrDefaultAsync(v => v.Id == dto.TrainingVideoId && !v.IsDeleted);

        if (video == null)
            throw new Exception("Video bulunamadı");

        var assignment = new TrainingVideoAssignment
        {
            Title = dto.Title,
            TrainingVideoId = dto.TrainingVideoId,
            StartDate = ToUtc(dto.StartDate),
            DueDate = ToUtc(dto.DueDate),
            IsActive = true,
            SourceProjectId = dto.ProjectId,
            ScoreThreshold = dto.ScoreThreshold,
            SourceStartDate = ToUtc(dto.SourceStartDate),
            SourceEndDate = ToUtc(dto.SourceEndDate)
        };

        // Manuel atama mı otomatik mi?
        if (dto.ManualUserIds?.Any() == true)
        {
            // Manuel atama - CustomerPersonnel ID'leri
            foreach (var cpId in dto.ManualUserIds)
            {
                assignment.Participants.Add(new TrainingVideoParticipant
                {
                    CustomerPersonnelId = cpId,
                    StatusId = TrainingVideoParticipantStatuses.Ids.Pending
                });
            }
        }
        else if (dto.ProjectId.HasValue && dto.ScoreThreshold.HasValue && dto.SourceStartDate.HasValue && dto.SourceEndDate.HasValue)
        {
            // Otomatik atama - kapsam bazlı (EvaluatedCustomerPersonnelId kullanılır)
            var personnelList = await GetCustomerPersonnelForAutoAssignmentAsync(video, dto);
            foreach (var cp in personnelList)
            {
                assignment.Participants.Add(new TrainingVideoParticipant
                {
                    CustomerPersonnelId = cp.CustomerPersonnelId,
                    StatusId = TrainingVideoParticipantStatuses.Ids.Pending
                });
            }
        }

        _context.TrainingVideoAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        return (await GetAssignmentByIdAsync(assignment.Id))!;
    }

    public async Task<bool> DeleteAssignmentAsync(int id)
    {
        var assignment = await _context.TrainingVideoAssignments.FindAsync(id);
        if (assignment == null || assignment.IsDeleted)
            return false;

        assignment.IsDeleted = true;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Otomatik Atama

    public async Task<AssignmentPreviewDto> PreviewAutoAssignmentAsync(CreateTrainingVideoAssignmentDto dto)
    {
        if (!dto.ProjectId.HasValue || !dto.ScoreThreshold.HasValue || !dto.SourceStartDate.HasValue || !dto.SourceEndDate.HasValue)
            return new AssignmentPreviewDto { TotalUsers = 0, Users = new List<AssignmentPreviewUserDto>() };

        var video = await _context.TrainingVideos
            .Include(v => v.Scopes)
            .FirstOrDefaultAsync(v => v.Id == dto.TrainingVideoId && !v.IsDeleted);

        if (video == null)
            return new AssignmentPreviewDto { TotalUsers = 0, Users = new List<AssignmentPreviewUserDto>() };

        var personnelList = await GetCustomerPersonnelForAutoAssignmentAsync(video, dto);

        return new AssignmentPreviewDto
        {
            TotalUsers = personnelList.Count,
            Users = personnelList.Select(p => new AssignmentPreviewUserDto
            {
                UserId = p.CustomerPersonnelId, // CustomerPersonnelId olarak kullanılıyor
                UserName = p.PersonnelName,
                Email = p.Email,
                ScopeScore = p.ScopeScore
            }).ToList()
        };
    }

    /// <summary>
    /// Internal DTO for CustomerPersonnel scores
    /// </summary>
    private class CustomerPersonnelScoreDto
    {
        public int CustomerPersonnelId { get; set; }
        public string PersonnelName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public decimal ScopeScore { get; set; }
    }

    private async Task<List<CustomerPersonnelScoreDto>> GetCustomerPersonnelForAutoAssignmentAsync(TrainingVideo video, CreateTrainingVideoAssignmentDto dto)
    {
        // Videonun kapsamını al
        var videoScopes = video.Scopes.Where(s => !s.IsDeleted).ToList();

        var questionGroupNames = videoScopes
            .Where(s => s.ScopeTypeId == TrainingVideoScopeTypes.Ids.QuestionGroup && !string.IsNullOrEmpty(s.QuestionGroupName))
            .Select(s => s.QuestionGroupName!)
            .ToList();

        var questionIds = videoScopes
            .Where(s => s.ScopeTypeId == TrainingVideoScopeTypes.Ids.Question && s.QuestionId.HasValue)
            .Select(s => s.QuestionId!.Value)
            .ToList();

        var checklistIds = videoScopes
            .Where(s => s.ScopeTypeId == TrainingVideoScopeTypes.Ids.Checklist && s.ChecklistId.HasValue)
            .Select(s => s.ChecklistId!.Value)
            .ToList();

        // Checklist kapsamındaki tüm soruları da ekle
        if (checklistIds.Any())
        {
            var checklistQuestionIds = await _context.Questions
                .Where(q => checklistIds.Contains(q.ChecklistId) && !q.IsDeleted)
                .Select(q => q.Id)
                .ToListAsync();
            questionIds.AddRange(checklistQuestionIds);
        }

        // Soru grubu kapsamındaki soruları da ekle
        if (questionGroupNames.Any())
        {
            var groupQuestionIds = await _context.Questions
                .Where(q => questionGroupNames.Contains(q.GroupName!) && !q.IsDeleted)
                .Select(q => q.Id)
                .ToListAsync();
            questionIds.AddRange(groupQuestionIds);
        }

        questionIds = questionIds.Distinct().ToList();

        var sourceStartUtc = ToUtc(dto.SourceStartDate!.Value);
        var sourceEndUtc = ToUtc(dto.SourceEndDate!.Value.Date.AddDays(1).AddTicks(-1));

        // Kapsam içindeki sorulardaki puanları hesapla - EvaluatedCustomerPersonnelId kullan
        var query = _context.Answers
            .Include(a => a.Question)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.EvaluatedCustomerPersonnel)
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Assignment)
            .Where(a => !a.IsDeleted)
            .Where(a => a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            .Where(a => a.Evaluation.Assignment.ProjectId == dto.ProjectId)
            .Where(a => a.Evaluation.CompletedAt >= sourceStartUtc)
            .Where(a => a.Evaluation.CompletedAt <= sourceEndUtc)
            .Where(a => a.Evaluation.EvaluatedCustomerPersonnelId != null); // Sadece CustomerPersonnel değerlendirmeleri

        if (questionIds.Any())
        {
            query = query.Where(a => questionIds.Contains(a.QuestionId));
        }

        var answers = await query.ToListAsync();

        // CustomerPersonnel bazında grupla ve ortalama puan hesapla
        var personnelScores = answers
            .GroupBy(a => a.Evaluation.EvaluatedCustomerPersonnelId!.Value)
            .Select(g =>
            {
                var totalPoints = g.Sum(a => a.GivenPoints ?? 0);
                var maxPoints = g.Sum(a => a.Question.MaxPoints);
                var score = maxPoints > 0 ? (totalPoints / maxPoints) * 100 : 0m;

                var firstAnswer = g.First();
                var cp = firstAnswer.Evaluation.EvaluatedCustomerPersonnel;
                return new CustomerPersonnelScoreDto
                {
                    CustomerPersonnelId = g.Key,
                    ScopeScore = score,
                    PersonnelName = cp != null ? $"{cp.FirstName} {cp.LastName}" : "",
                    Email = cp?.Email
                };
            })
            .Where(x => x.ScopeScore < dto.ScoreThreshold!.Value)
            .ToList();

        return personnelScores;
    }

    #endregion

    #region Katılımcı Yönetimi

    public async Task<IEnumerable<TrainingVideoParticipantDto>> GetParticipantsAsync(int assignmentId)
    {
        var participants = await _context.TrainingVideoParticipants
            .Include(p => p.CustomerPersonnel)
            .Where(p => p.TrainingVideoAssignmentId == assignmentId && !p.IsDeleted)
            .ToListAsync();

        return participants.Select(p => new TrainingVideoParticipantDto
        {
            Id = p.Id,
            UserId = p.CustomerPersonnelId, // CustomerPersonnelId kullanılıyor
            UserName = p.CustomerPersonnel.FullName,
            Email = p.CustomerPersonnel.Email,
            StatusId = p.StatusId,
            StatusName = TrainingVideoParticipantStatuses.GetById(p.StatusId)?.Description ?? "",
            StartedAt = p.StartedAt,
            CompletedAt = p.CompletedAt,
            WatchedSeconds = p.WatchedSeconds,
            IsCompleted = p.IsCompleted
        });
    }

    public async Task<bool> SendRemindersAsync(int assignmentId)
    {
        // TODO: Email gönderimi için EmailService kullanılacak
        // CustomerPersonnel için Notification sistemi farklı olabilir
        var participants = await _context.TrainingVideoParticipants
            .Include(p => p.CustomerPersonnel)
            .Include(p => p.Assignment)
            .Where(p => p.TrainingVideoAssignmentId == assignmentId && !p.IsDeleted && !p.IsCompleted)
            .ToListAsync();

        // TODO: CustomerPersonnel için bildirim/email gönderimi eklenecek
        // Şimdilik sadece count dönüyor
        return participants.Count > 0;
    }

    #endregion

    #region Kullanıcı Eğitimleri (CustomerPersonnel)

    public async Task<IEnumerable<MyTrainingDto>> GetMyTrainingsAsync(int customerPersonnelId)
    {
        var participants = await _context.TrainingVideoParticipants
            .Include(p => p.Assignment)
                .ThenInclude(a => a.TrainingVideo)
            .Where(p => p.CustomerPersonnelId == customerPersonnelId && !p.IsDeleted && !p.Assignment.IsDeleted)
            .OrderByDescending(p => p.Assignment.DueDate)
            .ToListAsync();

        var now = DateTime.UtcNow;

        return participants.Select(p => new MyTrainingDto
        {
            ParticipantId = p.Id,
            AssignmentId = p.TrainingVideoAssignmentId,
            AssignmentTitle = p.Assignment.Title,
            VideoId = p.Assignment.TrainingVideoId,
            VideoTitle = p.Assignment.TrainingVideo.Title,
            VideoDescription = p.Assignment.TrainingVideo.Description,
            VideoDurationSeconds = p.Assignment.TrainingVideo.DurationSeconds,
            StartDate = p.Assignment.StartDate,
            DueDate = p.Assignment.DueDate,
            StatusId = p.StatusId,
            StatusName = TrainingVideoParticipantStatuses.GetById(p.StatusId)?.Description ?? "",
            StartedAt = p.StartedAt,
            CompletedAt = p.CompletedAt,
            WatchedSeconds = p.WatchedSeconds,
            IsCompleted = p.IsCompleted,
            IsOverdue = !p.IsCompleted && p.Assignment.DueDate < now,
            DaysRemaining = Math.Max(0, (p.Assignment.DueDate - now).Days)
        });
    }

    public async Task<MyTrainingDto?> GetMyTrainingByIdAsync(int customerPersonnelId, int participantId)
    {
        var participant = await _context.TrainingVideoParticipants
            .Include(p => p.Assignment)
                .ThenInclude(a => a.TrainingVideo)
            .FirstOrDefaultAsync(p => p.Id == participantId && p.CustomerPersonnelId == customerPersonnelId && !p.IsDeleted);

        if (participant == null)
            return null;

        var now = DateTime.UtcNow;

        return new MyTrainingDto
        {
            ParticipantId = participant.Id,
            AssignmentId = participant.TrainingVideoAssignmentId,
            AssignmentTitle = participant.Assignment.Title,
            VideoId = participant.Assignment.TrainingVideoId,
            VideoTitle = participant.Assignment.TrainingVideo.Title,
            VideoDescription = participant.Assignment.TrainingVideo.Description,
            VideoDurationSeconds = participant.Assignment.TrainingVideo.DurationSeconds,
            StartDate = participant.Assignment.StartDate,
            DueDate = participant.Assignment.DueDate,
            StatusId = participant.StatusId,
            StatusName = TrainingVideoParticipantStatuses.GetById(participant.StatusId)?.Description ?? "",
            StartedAt = participant.StartedAt,
            CompletedAt = participant.CompletedAt,
            WatchedSeconds = participant.WatchedSeconds,
            IsCompleted = participant.IsCompleted,
            IsOverdue = !participant.IsCompleted && participant.Assignment.DueDate < now,
            DaysRemaining = Math.Max(0, (participant.Assignment.DueDate - now).Days)
        };
    }

    public async Task<bool> UpdateWatchProgressAsync(int participantId, UpdateWatchProgressDto dto)
    {
        var participant = await _context.TrainingVideoParticipants.FindAsync(participantId);
        if (participant == null || participant.IsDeleted)
            return false;

        // İlk kez izlemeye başlıyorsa
        if (participant.StatusId == TrainingVideoParticipantStatuses.Ids.Pending && dto.WatchedSeconds > 0)
        {
            participant.StatusId = TrainingVideoParticipantStatuses.Ids.InProgress;
            participant.StartedAt = DateTime.UtcNow;
        }

        participant.WatchedSeconds = dto.WatchedSeconds;

        // Tamamlandıysa
        if (dto.IsCompleted && !participant.IsCompleted)
        {
            participant.IsCompleted = true;
            participant.StatusId = TrainingVideoParticipantStatuses.Ids.Completed;
            participant.CompletedAt = DateTime.UtcNow;
        }

        participant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Video Streaming

    public async Task<Stream?> GetVideoStreamAsync(int videoId)
    {
        var video = await _context.TrainingVideos.FindAsync(videoId);
        if (video == null || video.IsDeleted || !File.Exists(video.FilePath))
            return null;

        return new FileStream(video.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public async Task<string?> GetVideoPathAsync(int videoId)
    {
        var video = await _context.TrainingVideos.FindAsync(videoId);
        if (video == null || video.IsDeleted)
            return null;

        return video.FilePath;
    }

    #endregion

    #region Mapping Helpers

    private TrainingVideoDto MapToDto(TrainingVideo video)
    {
        return new TrainingVideoDto
        {
            Id = video.Id,
            Title = video.Title,
            Description = video.Description,
            FileName = video.FileName,
            FilePath = video.FilePath,
            FileSize = video.FileSize,
            DurationSeconds = video.DurationSeconds,
            ThumbnailPath = video.ThumbnailPath,
            IsActive = video.IsActive,
            CreatedAt = video.CreatedAt,
            ScopeCount = video.Scopes.Count(s => !s.IsDeleted),
            AssignmentCount = 0,
            TotalParticipants = 0,
            CompletedParticipants = 0,
            Scopes = video.Scopes.Where(s => !s.IsDeleted).Select(s => new TrainingVideoScopeDto
            {
                Id = s.Id,
                ScopeTypeId = s.ScopeTypeId,
                ScopeTypeName = TrainingVideoScopeTypes.GetById(s.ScopeTypeId)?.Description ?? "",
                ChecklistId = s.ChecklistId,
                ChecklistName = s.Checklist?.Name,
                QuestionGroupName = s.QuestionGroupName,
                QuestionId = s.QuestionId,
                QuestionText = s.Question?.Text
            }).ToList()
        };
    }

    private TrainingVideoAssignmentDto MapToAssignmentDto(TrainingVideoAssignment assignment)
    {
        var participants = assignment.Participants.Where(p => !p.IsDeleted).ToList();
        var completed = participants.Count(p => p.IsCompleted);
        var inProgress = participants.Count(p => p.StatusId == TrainingVideoParticipantStatuses.Ids.InProgress);
        var total = participants.Count;

        return new TrainingVideoAssignmentDto
        {
            Id = assignment.Id,
            Title = assignment.Title,
            TrainingVideoId = assignment.TrainingVideoId,
            TrainingVideoTitle = assignment.TrainingVideo.Title,
            StartDate = assignment.StartDate,
            DueDate = assignment.DueDate,
            IsActive = assignment.IsActive,
            CreatedAt = assignment.CreatedAt,
            TotalParticipants = total,
            CompletedParticipants = completed,
            InProgressParticipants = inProgress,
            CompletionPercentage = total > 0 ? Math.Round((decimal)completed / total * 100, 1) : 0,
            SourceProjectId = assignment.SourceProjectId,
            SourceProjectName = assignment.SourceProject?.Name,
            ScoreThreshold = assignment.ScoreThreshold,
            SourceStartDate = assignment.SourceStartDate,
            SourceEndDate = assignment.SourceEndDate,
            Participants = participants.Select(p => new TrainingVideoParticipantDto
            {
                Id = p.Id,
                UserId = p.CustomerPersonnelId,
                UserName = p.CustomerPersonnel.FullName,
                Email = p.CustomerPersonnel.Email,
                StatusId = p.StatusId,
                StatusName = TrainingVideoParticipantStatuses.GetById(p.StatusId)?.Description ?? "",
                StartedAt = p.StartedAt,
                CompletedAt = p.CompletedAt,
                WatchedSeconds = p.WatchedSeconds,
                IsCompleted = p.IsCompleted
            }).ToList()
        };
    }

    #endregion
}
