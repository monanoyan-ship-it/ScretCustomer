using Microsoft.AspNetCore.Http;
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
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _videoStoragePath;

    public TrainingVideoService(ApplicationDbContext context, IEmailService emailService, IHttpContextAccessor httpContextAccessor, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
        _videoStoragePath = configuration["Storage:VideoPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Videos");

        // Klasör yoksa oluştur
        if (!Directory.Exists(_videoStoragePath))
            Directory.CreateDirectory(_videoStoragePath);
    }

    /// <summary>
    /// Mevcut request'ten base URL oluşturur (scheme://host)
    /// </summary>
    private string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request != null)
        {
            return $"{request.Scheme}://{request.Host}";
        }
        return "https://localhost";
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
            ThumbnailUrl = !string.IsNullOrEmpty(v.ThumbnailPath) ? $"/api/training-videos/{v.Id}/thumbnail" : null,
            // İzleme kontrol ayarları
            MinWatchPercentage = v.MinWatchPercentage,
            AllowSkipping = v.AllowSkipping,
            MaxPlaybackSpeed = v.MaxPlaybackSpeed
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
            IsActive = true,
            // İzleme kontrol ayarları
            MinWatchPercentage = dto.MinWatchPercentage,
            AllowSkipping = dto.AllowSkipping,
            MaxPlaybackSpeed = dto.MaxPlaybackSpeed
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

        // İzleme kontrol ayarları
        video.MinWatchPercentage = dto.MinWatchPercentage;
        video.AllowSkipping = dto.AllowSkipping;
        video.MaxPlaybackSpeed = dto.MaxPlaybackSpeed;

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
            .Include(a => a.ExternalParticipants)
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

            // DateRanges pattern (KURALLAR.md Section 20)
            // Birden fazla tarih filtresi desteklenir (start ve due ayrı ayrı)
            if (filter.DateRanges?.Any() == true)
            {
                foreach (var dateRange in filter.DateRanges)
                {
                    var filterType = dateRange.FilterType ?? filter.DateFilterType ?? "start";

                    if (dateRange.StartDate.HasValue)
                    {
                        var startUtc = DateTime.SpecifyKind(dateRange.StartDate.Value.Date, DateTimeKind.Utc);
                        if (filterType == "due")
                            query = query.Where(a => a.DueDate >= startUtc);
                        else
                            query = query.Where(a => a.StartDate >= startUtc);
                    }

                    if (dateRange.EndDate.HasValue)
                    {
                        var endUtc = DateTime.SpecifyKind(dateRange.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                        if (filterType == "due")
                            query = query.Where(a => a.DueDate <= endUtc);
                        else
                            query = query.Where(a => a.StartDate <= endUtc);
                    }
                }
            }
        }

        var assignments = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();

        return assignments.Select(a =>
        {
            // İç katılımcılar
            var internalParticipants = a.Participants.Where(p => !p.IsDeleted).ToList();
            var internalCompleted = internalParticipants.Count(p => p.IsCompleted);
            var internalInProgress = internalParticipants.Count(p => p.StatusId == TrainingVideoParticipantStatuses.Ids.InProgress);
            var internalNotStarted = internalParticipants.Count(p => p.StatusId == TrainingVideoParticipantStatuses.Ids.Pending);
            var internalEmailSent = internalParticipants.Count(p => p.EmailSentCount > 0);

            // Dış katılımcılar
            var externalParticipants = a.ExternalParticipants?.Where(p => !p.IsDeleted).ToList() ?? new List<TrainingVideoExternalParticipant>();
            var externalCompleted = externalParticipants.Count(p => p.IsCompleted);
            var externalInProgress = externalParticipants.Count(p => p.StatusId == TrainingVideoParticipantStatuses.Ids.InProgress);
            var externalNotStarted = externalParticipants.Count(p => p.StatusId == TrainingVideoParticipantStatuses.Ids.Pending);
            var externalEmailSent = externalParticipants.Count(p => p.EmailSentCount > 0);

            // Toplam
            var total = internalParticipants.Count + externalParticipants.Count;
            var completed = internalCompleted + externalCompleted;
            var inProgress = internalInProgress + externalInProgress;
            var notStarted = internalNotStarted + externalNotStarted;
            var emailSent = internalEmailSent + externalEmailSent;

            return new TrainingVideoAssignmentListDto
            {
                Id = a.Id,
                Title = a.Title,
                TrainingVideoId = a.TrainingVideoId,
                TrainingVideoTitle = a.TrainingVideo.Title,
                StartDate = a.StartDate,
                DueDate = a.DueDate,
                IsActive = a.IsActive,
                IsExternal = a.IsExternal,
                CreatedAt = a.CreatedAt,
                TotalParticipants = total,
                CompletedParticipants = completed,
                InProgressParticipants = inProgress,
                CompletionPercentage = total > 0 ? Math.Round((decimal)completed / total * 100, 1) : 0,
                EmailSentParticipants = emailSent,
                NotStartedParticipants = notStarted
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
            IsExternal = dto.IsExternal,
            SourceProjectId = dto.ProjectId,
            ScoreThreshold = dto.ScoreThreshold,
            SourceStartDate = ToUtc(dto.SourceStartDate),
            SourceEndDate = ToUtc(dto.SourceEndDate),
            // İzleme ayarları
            MinWatchCount = dto.MinWatchCount,
            MaxWatchCount = dto.MaxWatchCount,
            AllowSpeedChange = dto.AllowSpeedChange,
            AllowSeeking = dto.AllowSeeking,
            EmailTemplateId = dto.EmailTemplateId
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

    public async Task<TrainingVideoAssignmentDto> UpdateAssignmentAsync(int id, UpdateTrainingVideoAssignmentDto dto)
    {
        var assignment = await _context.TrainingVideoAssignments
            .Include(a => a.Participants)
            .Include(a => a.ExternalParticipants)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (assignment == null)
            throw new KeyNotFoundException($"Assignment with id {id} not found");

        // Temel alanları güncelle
        assignment.Title = dto.Title;
        assignment.StartDate = dto.StartDate;
        assignment.DueDate = dto.DueDate;
        assignment.IsActive = dto.IsActive;
        assignment.EmailTemplateId = dto.EmailTemplateId;
        assignment.MinWatchCount = dto.MinWatchCount;
        assignment.MaxWatchCount = dto.MaxWatchCount;
        assignment.AllowSpeedChange = dto.AllowSpeedChange;
        assignment.AllowSeeking = dto.AllowSeeking;
        assignment.UpdatedAt = DateTime.UtcNow;

        // İç katılımcıları kaldır
        if (dto.RemoveParticipantIds?.Any() == true)
        {
            var participantsToRemove = assignment.Participants
                .Where(p => dto.RemoveParticipantIds.Contains(p.Id))
                .ToList();

            foreach (var participant in participantsToRemove)
            {
                participant.IsDeleted = true;
                participant.UpdatedAt = DateTime.UtcNow;
            }
        }

        // İç katılımcıları ekle
        if (dto.AddParticipantIds?.Any() == true)
        {
            var existingPersonnelIds = assignment.Participants
                .Where(p => !p.IsDeleted)
                .Select(p => p.CustomerPersonnelId)
                .ToHashSet();

            foreach (var personnelId in dto.AddParticipantIds)
            {
                if (!existingPersonnelIds.Contains(personnelId))
                {
                    assignment.Participants.Add(new TrainingVideoParticipant
                    {
                        CustomerPersonnelId = personnelId,
                        StatusId = 1,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        // Dış katılımcıları kaldır
        if (dto.RemoveExternalParticipantIds?.Any() == true)
        {
            var externalToRemove = assignment.ExternalParticipants
                .Where(p => dto.RemoveExternalParticipantIds.Contains(p.Id))
                .ToList();

            foreach (var external in externalToRemove)
            {
                external.IsDeleted = true;
                external.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Dış katılımcıları ekle
        if (dto.AddExternalParticipants?.Any() == true)
        {
            var existingEmails = assignment.ExternalParticipants
                .Where(p => !p.IsDeleted)
                .Select(p => p.Email.ToLowerInvariant())
                .ToHashSet();

            foreach (var external in dto.AddExternalParticipants)
            {
                if (!string.IsNullOrWhiteSpace(external.Email) && !existingEmails.Contains(external.Email.ToLowerInvariant()))
                {
                    assignment.ExternalParticipants.Add(new TrainingVideoExternalParticipant
                    {
                        Email = external.Email,
                        FirstName = external.FirstName,
                        LastName = external.LastName,
                        Token = Guid.NewGuid().ToString("N"),
                        StatusId = 1,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

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
        // SourceStartDate ve SourceEndDate zorunlu, diğer filtreler opsiyonel
        if (!dto.SourceStartDate.HasValue || !dto.SourceEndDate.HasValue)
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
                CustomerName = p.CustomerName,
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
        public string? CustomerName { get; set; }
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
                    .ThenInclude(cp => cp!.Customer)
            .Include(a => a.Evaluation)
            .Where(a => !a.IsDeleted)
            .Where(a => a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
            .Where(a => a.Evaluation.CompletedAt >= sourceStartUtc)
            .Where(a => a.Evaluation.CompletedAt <= sourceEndUtc)
            .Where(a => a.Evaluation.EvaluatedCustomerPersonnelId != null); // Sadece CustomerPersonnel değerlendirmeleri

        // Opsiyonel filtreler
        if (dto.ProjectId.HasValue)
        {
            query = query.Where(a => a.Evaluation.ProjectId == dto.ProjectId);
        }

        if (dto.CustomerId.HasValue)
        {
            query = query.Where(a => a.Evaluation.EvaluatedCustomerPersonnel!.CustomerId == dto.CustomerId);
        }

        if (dto.OrganizationId.HasValue)
        {
            // CustomerPersonnel'in organizasyon atamasını kontrol et
            var orgPersonnelIds = await _context.CustomerPersonnelOrganizations
                .Where(cpo => cpo.CustomerOrganizationId == dto.OrganizationId && !cpo.IsDeleted)
                .Select(cpo => cpo.CustomerPersonnelId)
                .Distinct()
                .ToListAsync();

            query = query.Where(a => orgPersonnelIds.Contains(a.Evaluation.EvaluatedCustomerPersonnelId!.Value));
        }

        if (questionIds.Any())
        {
            query = query.Where(a => questionIds.Contains(a.QuestionId));
        }

        var answers = await query.ToListAsync();

        // CustomerPersonnel bazında grupla ve ortalama puan hesapla
        var scoreThreshold = dto.ScoreThreshold ?? 100m; // Eğer threshold yoksa tüm sonuçları getir
        var personnelScores = answers
            .GroupBy(a => a.Evaluation.EvaluatedCustomerPersonnelId!.Value)
            .Select(g =>
            {
                var totalPoints = g.Sum(a => a.GivenPoints ?? 0m);
                var maxPoints = g.Sum(a => (decimal)a.Question.MaxPoints);
                var score = maxPoints > 0 ? (totalPoints / maxPoints) * 100m : 0m;

                var firstAnswer = g.First();
                var cp = firstAnswer.Evaluation.EvaluatedCustomerPersonnel;
                return new CustomerPersonnelScoreDto
                {
                    CustomerPersonnelId = g.Key,
                    ScopeScore = score,
                    PersonnelName = cp != null ? $"{cp.FirstName} {cp.LastName}" : "",
                    Email = cp?.Email,
                    CustomerName = cp?.Customer?.CompanyName
                };
            })
            .Where(x => x.ScopeScore <= scoreThreshold)
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
            IsCompleted = p.IsCompleted,
            // Email durumu
            FirstEmailSentAt = p.FirstEmailSentAt,
            LastEmailSentAt = p.LastEmailSentAt,
            EmailSentCount = p.EmailSentCount
        });
    }

    public async Task<IEnumerable<AllParticipantDto>> GetAllParticipantsAsync()
    {
        var participants = await _context.TrainingVideoParticipants
            .Include(p => p.CustomerPersonnel)
                .ThenInclude(cp => cp.Customer)
            .Include(p => p.Assignment)
                .ThenInclude(a => a.TrainingVideo)
            .Where(p => !p.IsDeleted && !p.Assignment.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return participants.Select(p => {
            var videoDuration = p.Assignment.TrainingVideo.DurationSeconds;
            var watchProgress = videoDuration > 0
                ? (int)Math.Round((decimal)p.WatchedSeconds / videoDuration * 100)
                : 0;

            return new AllParticipantDto
            {
                Id = p.Id,
                UserId = p.CustomerPersonnelId,
                UserName = p.CustomerPersonnel.FullName,
                Email = p.CustomerPersonnel.Email,
                CustomerName = p.CustomerPersonnel.Customer?.CompanyName,
                VideoId = p.Assignment.TrainingVideoId,
                VideoTitle = p.Assignment.TrainingVideo.Title,
                AssignmentId = p.TrainingVideoAssignmentId,
                AssignmentTitle = p.Assignment.Title,
                StatusId = p.StatusId,
                StatusName = TrainingVideoParticipantStatuses.GetById(p.StatusId)?.Description ?? "",
                StartedAt = p.StartedAt,
                CompletedAt = p.CompletedAt,
                WatchedSeconds = p.WatchedSeconds,
                VideoDurationSeconds = videoDuration,
                WatchProgress = Math.Min(watchProgress, 100)
            };
        });
    }

    public async Task<int> SendEmailsAsync(SendTrainingEmailDto dto, int? sentByUserId = null, int emailTypeId = 1)
    {
        var assignment = await _context.TrainingVideoAssignments
            .Include(a => a.TrainingVideo)
            .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId && !a.IsDeleted);

        if (assignment == null)
            return 0;

        var participants = await _context.TrainingVideoParticipants
            .Include(p => p.CustomerPersonnel)
            .Where(p => dto.ParticipantIds.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync();

        if (!participants.Any())
            return 0;

        // Email şablonunu al - Öncelik sırası:
        // 1. DTO'da belirtilen şablon
        // 2. Atamaya kaydedilen şablon
        // 3. Varsayılan training video şablonu
        EmailTemplate? template = null;

        // 1. DTO'da şablon belirtilmişse onu kullan
        if (dto.EmailTemplateId.HasValue)
        {
            template = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.Id == dto.EmailTemplateId.Value && !t.IsDeleted);
        }

        // 2. Atamaya kaydedilen şablonu kullan
        if (template == null && assignment.EmailTemplateId.HasValue)
        {
            template = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.Id == assignment.EmailTemplateId.Value && !t.IsDeleted);
        }

        // 3. Varsayılan training video şablonunu kullan
        if (template == null)
        {
            template = await _context.EmailTemplates
                .Where(t => t.TemplateTypeId == EmailTemplateTypes.Ids.TrainingVideo && !t.IsDeleted)
                .OrderBy(t => t.IsDefault ? 0 : 1)
                .FirstOrDefaultAsync();
        }

        var now = DateTime.UtcNow;
        var sentCount = 0;

        foreach (var p in participants)
        {
            var emailLog = new TrainingVideoEmailLog
            {
                TrainingVideoParticipantId = p.Id,
                EmailTemplateId = template?.Id,
                SentAt = now,
                SentByUserId = sentByUserId,
                EmailTypeId = emailTypeId,
                IsSuccess = false
            };

            try
            {
                // Email gönder
                if (!string.IsNullOrEmpty(p.CustomerPersonnel?.Email) && template != null)
                {
                    var subject = ReplaceTrainingVideoPlaceholders(template.Subject, assignment, p);
                    var body = ReplaceTrainingVideoPlaceholders(template.Body, assignment, p);

                    var result = await _emailService.SendEmailAsync(p.CustomerPersonnel.Email, subject, body, true);
                    emailLog.IsSuccess = result.Success;
                    if (!result.Success)
                        emailLog.ErrorMessage = result.ErrorMessage;
                    else
                        sentCount++;
                }
                else
                {
                    emailLog.ErrorMessage = string.IsNullOrEmpty(p.CustomerPersonnel?.Email)
                        ? "Email adresi bulunamadı"
                        : "Email şablonu bulunamadı";
                }
            }
            catch (Exception ex)
            {
                emailLog.IsSuccess = false;
                emailLog.ErrorMessage = ex.Message;
            }

            _context.TrainingVideoEmailLogs.Add(emailLog);

            // Participant özet alanlarını güncelle
            if (p.FirstEmailSentAt == null)
                p.FirstEmailSentAt = now;
            p.LastEmailSentAt = now;
            p.EmailSentCount++;
            p.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();
        return sentCount;
    }

    /// <summary>
    /// Training video email şablonundaki placeholder'ları değiştirir
    /// </summary>
    private string ReplaceTrainingVideoPlaceholders(string content, TrainingVideoAssignment assignment, TrainingVideoParticipant participant)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var video = assignment.TrainingVideo;
        var personnel = participant.CustomerPersonnel;

        // Video süresi formatı
        var duration = video.DurationSeconds;
        var durationStr = duration >= 60
            ? $"{duration / 60} dk {duration % 60} sn"
            : $"{duration} sn";

        // Eğitimler sayfası linki - HTML anchor olarak oluştur (mobil cihazların yanlış parse etmemesi için)
        var trainingsUrl = $"{GetBaseUrl()}/CustomerPortal/MyTrainings";
        var videoLinkHtml = $"<a href=\"{trainingsUrl}\" style=\"color: #007bff; text-decoration: underline;\">Eğitimlerim Sayfası</a>";

        // Placeholder değiştirmeleri
        content = content.Replace(EmailPlaceholders.TrainingVideoTitle, video.Title);
        content = content.Replace(EmailPlaceholders.TrainingVideoDescription, video.Description ?? "");
        content = content.Replace(EmailPlaceholders.TrainingVideoDuration, durationStr);
        content = content.Replace(EmailPlaceholders.TrainingVideoLink, videoLinkHtml);
        content = content.Replace(EmailPlaceholders.TrainingVideoUrl, trainingsUrl);
        content = content.Replace(EmailPlaceholders.TrainingAssignmentTitle, assignment.Title);
        content = content.Replace(EmailPlaceholders.TrainingStartDate, assignment.StartDate.ToString("dd.MM.yyyy"));
        content = content.Replace(EmailPlaceholders.TrainingDueDate, assignment.DueDate.ToString("dd.MM.yyyy"));

        // Kişi bilgileri
        if (personnel != null)
        {
            var fullName = $"{personnel.FirstName} {personnel.LastName}".Trim();
            content = content.Replace(EmailPlaceholders.RecipientName, fullName);
            content = content.Replace(EmailPlaceholders.RecipientFirstName, personnel.FirstName ?? "");
            content = content.Replace(EmailPlaceholders.RecipientLastName, personnel.LastName ?? "");
            content = content.Replace(EmailPlaceholders.RecipientEmail, personnel.Email ?? "");
        }

        // Sistem bilgileri
        content = content.Replace(EmailPlaceholders.CurrentDate, DateTime.Now.ToString("dd.MM.yyyy"));
        content = content.Replace(EmailPlaceholders.CurrentYear, DateTime.Now.Year.ToString());
        content = content.Replace(EmailPlaceholders.SystemName, "Secret Customer");

        return content;
    }

    /// <summary>
    /// Dış katılımcı email şablonundaki placeholder'ları değiştirir
    /// Hem yeni {Placeholder} hem de eski {{Placeholder}} formatını destekler
    /// </summary>
    private string ReplaceExternalTrainingPlaceholders(string content, TrainingVideoAssignment assignment, TrainingVideoExternalParticipant participant, string watchUrl)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var video = assignment.TrainingVideo;

        // Video süresi formatı
        var duration = video.DurationSeconds;
        var durationStr = duration >= 60
            ? $"{duration / 60} dk {duration % 60} sn"
            : $"{duration} sn";

        // HTML anchor link
        var videoLinkHtml = $"<a href=\"{watchUrl}\" style=\"color: #007bff; text-decoration: underline;\">Videoyu İzle</a>";

        // Yeni standart placeholder'lar {Placeholder}
        content = content.Replace(EmailPlaceholders.TrainingVideoTitle, video.Title);
        content = content.Replace(EmailPlaceholders.TrainingVideoDescription, video.Description ?? "");
        content = content.Replace(EmailPlaceholders.TrainingVideoDuration, durationStr);
        content = content.Replace(EmailPlaceholders.TrainingVideoLink, videoLinkHtml);
        content = content.Replace(EmailPlaceholders.TrainingVideoUrl, watchUrl);
        content = content.Replace(EmailPlaceholders.TrainingAssignmentTitle, assignment.Title);
        content = content.Replace(EmailPlaceholders.TrainingStartDate, assignment.StartDate.ToString("dd.MM.yyyy"));
        content = content.Replace(EmailPlaceholders.TrainingDueDate, assignment.DueDate.ToString("dd.MM.yyyy"));

        // Alıcı bilgileri
        var fullName = participant.FullName ?? participant.Email;
        content = content.Replace(EmailPlaceholders.RecipientName, fullName);
        content = content.Replace(EmailPlaceholders.RecipientEmail, participant.Email ?? "");

        // Sistem bilgileri
        content = content.Replace(EmailPlaceholders.CurrentDate, DateTime.Now.ToString("dd.MM.yyyy"));
        content = content.Replace(EmailPlaceholders.CurrentYear, DateTime.Now.Year.ToString());
        content = content.Replace(EmailPlaceholders.SystemName, "Secret Customer");

        // Eski format desteği (geriye uyumluluk) {{Placeholder}}
        content = content.Replace("{{VideoTitle}}", video.Title);
        content = content.Replace("{{AssignmentTitle}}", assignment.Title);
        content = content.Replace("{{DueDate}}", assignment.DueDate.ToString("dd.MM.yyyy"));
        content = content.Replace("{{UserName}}", fullName);
        content = content.Replace("{{WatchUrl}}", watchUrl);
        content = content.Replace("{{VideoLink}}", videoLinkHtml);

        return content;
    }

    public async Task<bool> SendRemindersAsync(int assignmentId)
    {
        // Tamamlamamış katılımcılara email gönder
        var participants = await _context.TrainingVideoParticipants
            .Where(p => p.TrainingVideoAssignmentId == assignmentId && !p.IsDeleted && !p.IsCompleted)
            .Select(p => p.Id)
            .ToListAsync();

        if (!participants.Any())
            return false;

        var result = await SendEmailsAsync(new SendTrainingEmailDto
        {
            AssignmentId = assignmentId,
            ParticipantIds = participants
        });

        return result > 0;
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
            DaysRemaining = Math.Max(0, (p.Assignment.DueDate - now).Days),
            // İzleme kontrol ayarları
            MinWatchPercentage = p.Assignment.TrainingVideo.MinWatchPercentage,
            AllowSkipping = p.Assignment.TrainingVideo.AllowSkipping,
            MaxPlaybackSpeed = p.Assignment.TrainingVideo.MaxPlaybackSpeed
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
            DaysRemaining = Math.Max(0, (participant.Assignment.DueDate - now).Days),
            // İzleme kontrol ayarları
            MinWatchPercentage = participant.Assignment.TrainingVideo.MinWatchPercentage,
            AllowSkipping = participant.Assignment.TrainingVideo.AllowSkipping,
            MaxPlaybackSpeed = participant.Assignment.TrainingVideo.MaxPlaybackSpeed
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
            // İzleme kontrol ayarları
            MinWatchPercentage = video.MinWatchPercentage,
            AllowSkipping = video.AllowSkipping,
            MaxPlaybackSpeed = video.MaxPlaybackSpeed,
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
            MinWatchCount = assignment.MinWatchCount,
            MaxWatchCount = assignment.MaxWatchCount,
            AllowSpeedChange = assignment.AllowSpeedChange,
            AllowSeeking = assignment.AllowSeeking,
            EmailTemplateId = assignment.EmailTemplateId,
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

    #region Scope-based Data

    /// <summary>
    /// Video scope'undaki checklist'lerde değerlendirmesi olan müşterileri getirir
    /// </summary>
    public async Task<IEnumerable<object>> GetScopeCustomersAsync(int videoId)
    {
        var video = await _context.TrainingVideos
            .Include(v => v.Scopes)
            .FirstOrDefaultAsync(v => v.Id == videoId && !v.IsDeleted);

        if (video == null)
            return new List<object>();

        // Video scope'undaki checklist'leri al
        var checklistIds = video.Scopes
            .Where(s => !s.IsDeleted && s.ChecklistId.HasValue)
            .Select(s => s.ChecklistId!.Value)
            .Distinct()
            .ToList();

        // QuestionGroup veya Question scope'ları için, ilişkili checklist'leri bul
        var questionGroupNames = video.Scopes
            .Where(s => !s.IsDeleted && !string.IsNullOrEmpty(s.QuestionGroupName))
            .Select(s => s.QuestionGroupName!)
            .Distinct()
            .ToList();

        var questionIds = video.Scopes
            .Where(s => !s.IsDeleted && s.QuestionId.HasValue)
            .Select(s => s.QuestionId!.Value)
            .Distinct()
            .ToList();

        if (questionGroupNames.Any())
        {
            var groupChecklistIds = await _context.Questions
                .Where(q => !q.IsDeleted && q.GroupName != null && questionGroupNames.Contains(q.GroupName))
                .Select(q => q.ChecklistId)
                .Distinct()
                .ToListAsync();
            checklistIds = checklistIds.Union(groupChecklistIds).ToList();
        }

        if (questionIds.Any())
        {
            var questionChecklistIds = await _context.Questions
                .Where(q => !q.IsDeleted && questionIds.Contains(q.Id))
                .Select(q => q.ChecklistId)
                .Distinct()
                .ToListAsync();
            checklistIds = checklistIds.Union(questionChecklistIds).ToList();
        }

        if (!checklistIds.Any())
        {
            // Scope yoksa tüm aktif müşterileri getir
            return await _context.Customers
                .Where(c => !c.IsDeleted && c.IsActive)
                .Select(c => new { c.Id, c.CompanyName })
                .OrderBy(c => c.CompanyName)
                .ToListAsync();
        }

        // Bu checklist'lerde değerlendirmesi olan müşterileri bul
        var customerIds = await _context.Evaluations
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed)
            .Where(e => checklistIds.Contains(e.Project.ChecklistId))
            .Where(e => e.EvaluatedCustomerPersonnelId.HasValue && e.EvaluatedCustomerPersonnel != null)
            .Select(e => e.EvaluatedCustomerPersonnel!.CustomerId)
            .Distinct()
            .ToListAsync();

        // Müşteri listesini getir
        return await _context.Customers
            .Where(c => !c.IsDeleted && customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.CompanyName })
            .OrderBy(c => c.CompanyName)
            .ToListAsync();
    }

    /// <summary>
    /// Video scope'una göre personel arar (edit modal için)
    /// Videonun Checklist/GroupName/Question kapsamında dinlemesi olan personeller
    /// </summary>
    public async Task<IEnumerable<ScopePersonnelSearchResultDto>> SearchScopePersonnelAsync(int videoId, string? searchTerm, int maxResults = 20)
    {
        var video = await _context.TrainingVideos
            .Include(v => v.Scopes)
            .FirstOrDefaultAsync(v => v.Id == videoId && !v.IsDeleted);

        if (video == null)
            return new List<ScopePersonnelSearchResultDto>();

        // Video scope'undaki verileri çıkar
        var videoScopes = video.Scopes.Where(s => !s.IsDeleted).ToList();

        var checklistIds = videoScopes
            .Where(s => s.ScopeTypeId == TrainingVideoScopeTypes.Ids.Checklist && s.ChecklistId.HasValue)
            .Select(s => s.ChecklistId!.Value)
            .ToList();

        var questionGroupNames = videoScopes
            .Where(s => s.ScopeTypeId == TrainingVideoScopeTypes.Ids.QuestionGroup && !string.IsNullOrEmpty(s.QuestionGroupName))
            .Select(s => s.QuestionGroupName!)
            .ToList();

        var questionIds = videoScopes
            .Where(s => s.ScopeTypeId == TrainingVideoScopeTypes.Ids.Question && s.QuestionId.HasValue)
            .Select(s => s.QuestionId!.Value)
            .ToList();

        // Checklist kapsamındaki soruların checklist'lerini de ekle
        if (checklistIds.Any())
        {
            // Checklist zaten var, soruları da dahil et
        }

        // Soru grubu kapsamındaki checklist'leri bul
        if (questionGroupNames.Any())
        {
            var groupChecklistIds = await _context.Questions
                .Where(q => !q.IsDeleted && q.GroupName != null && questionGroupNames.Contains(q.GroupName))
                .Select(q => q.ChecklistId)
                .Distinct()
                .ToListAsync();
            checklistIds = checklistIds.Union(groupChecklistIds).ToList();
        }

        // Soru kapsamındaki checklist'leri bul
        if (questionIds.Any())
        {
            var questionChecklistIds = await _context.Questions
                .Where(q => !q.IsDeleted && questionIds.Contains(q.Id))
                .Select(q => q.ChecklistId)
                .Distinct()
                .ToListAsync();
            checklistIds = checklistIds.Union(questionChecklistIds).ToList();
        }

        // Scope'a göre değerlendirmesi olan personelleri bul
        var query = _context.Evaluations
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(cp => cp!.Customer)
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed)
            .Where(e => e.EvaluatedCustomerPersonnelId.HasValue && e.EvaluatedCustomerPersonnel != null);

        // Checklist filtresi (scope varsa)
        if (checklistIds.Any())
        {
            query = query.Where(e => checklistIds.Contains(e.Project.ChecklistId));
        }

        // Arama filtresi
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(e =>
                (e.EvaluatedCustomerPersonnel!.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName).ToLower().Contains(term) ||
                (e.EvaluatedCustomerPersonnel.Email != null && e.EvaluatedCustomerPersonnel.Email.ToLower().Contains(term)));
        }

        // Distinct personel listesi
        var personnelList = await query
            .Select(e => new
            {
                e.EvaluatedCustomerPersonnel!.Id,
                e.EvaluatedCustomerPersonnel.FirstName,
                e.EvaluatedCustomerPersonnel.LastName,
                e.EvaluatedCustomerPersonnel.Email,
                CustomerName = e.EvaluatedCustomerPersonnel.Customer != null ? e.EvaluatedCustomerPersonnel.Customer.CompanyName : null
            })
            .Distinct()
            .Take(maxResults)
            .ToListAsync();

        return personnelList.Select(p => new ScopePersonnelSearchResultDto
        {
            Id = p.Id,
            FullName = $"{p.FirstName} {p.LastName}".Trim(),
            Email = p.Email,
            CustomerName = p.CustomerName
        });
    }

    #endregion

    #region Dış Katılımcı Yönetimi

    /// <summary>
    /// Bir atamanın dış katılımcılarını getirir
    /// </summary>
    public async Task<IEnumerable<TrainingVideoExternalParticipantDto>> GetExternalParticipantsAsync(int assignmentId)
    {
        var participants = await _context.TrainingVideoExternalParticipants
            .Where(p => !p.IsDeleted && p.TrainingVideoAssignmentId == assignmentId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return participants.Select(p => new TrainingVideoExternalParticipantDto
        {
            Id = p.Id,
            TrainingVideoAssignmentId = p.TrainingVideoAssignmentId,
            Email = p.Email,
            FirstName = p.FirstName,
            LastName = p.LastName,
            FullName = p.FullName,
            Token = p.Token,
            StatusId = p.StatusId,
            StatusName = GetParticipantStatusName(p.StatusId),
            IsOpened = p.IsOpened,
            OpenedAt = p.OpenedAt,
            StartedAt = p.StartedAt,
            CompletedAt = p.CompletedAt,
            WatchedSeconds = p.WatchedSeconds,
            IsCompleted = p.IsCompleted,
            WatchCount = p.WatchCount,
            FirstEmailSentAt = p.FirstEmailSentAt,
            LastEmailSentAt = p.LastEmailSentAt,
            EmailSentCount = p.EmailSentCount,
            CreatedAt = p.CreatedAt,
            WatchUrl = $"{GetBaseUrl()}/Training/External/{p.Token}"
        });
    }

    /// <summary>
    /// Toplu dış katılımcı ekler
    /// </summary>
    public async Task<AddExternalParticipantsResultDto> AddExternalParticipantsAsync(AddExternalParticipantsDto dto, int? sentByUserId = null)
    {
        var result = new AddExternalParticipantsResultDto();

        var assignment = await _context.TrainingVideoAssignments
            .Include(a => a.TrainingVideo)
            .Include(a => a.ExternalParticipants.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId && !a.IsDeleted);

        if (assignment == null)
        {
            result.Message = "Atama bulunamadı";
            return result;
        }

        var existingEmails = assignment.ExternalParticipants
            .Select(p => p.Email.ToLower())
            .ToHashSet();

        var addedParticipants = new List<TrainingVideoExternalParticipant>();

        foreach (var item in dto.Participants)
        {
            var email = item.Email?.Trim().ToLower();
            if (string.IsNullOrEmpty(email))
            {
                result.SkippedCount++;
                continue;
            }

            // Email format kontrolü
            if (!IsValidEmail(email))
            {
                result.SkippedCount++;
                result.SkippedEmails.Add($"{email} (geçersiz format)");
                continue;
            }

            // Zaten ekli mi?
            if (existingEmails.Contains(email))
            {
                result.SkippedCount++;
                result.SkippedEmails.Add($"{email} (zaten ekli)");
                continue;
            }

            var participant = new TrainingVideoExternalParticipant
            {
                TrainingVideoAssignmentId = dto.AssignmentId,
                Email = email,
                FirstName = item.FirstName?.Trim(),
                LastName = item.LastName?.Trim(),
                Token = Guid.NewGuid().ToString("N"),
                StatusId = 1, // Pending
                CreatedAt = DateTime.UtcNow
            };

            _context.TrainingVideoExternalParticipants.Add(participant);
            addedParticipants.Add(participant);
            existingEmails.Add(email);
            result.AddedCount++;
        }

        await _context.SaveChangesAsync();

        // Email gönderimi
        if (dto.SendEmail && addedParticipants.Any())
        {
            var templateId = dto.EmailTemplateId ?? assignment.EmailTemplateId;
            foreach (var participant in addedParticipants)
            {
                try
                {
                    await SendExternalParticipantEmailAsync(participant, assignment, templateId, sentByUserId, 1);
                    result.EmailSentCount++;
                }
                catch (Exception ex)
                {
                    // Email hatası eklemeyi engellemez
                    Console.WriteLine($"Email gönderilemedi: {participant.Email} - {ex.Message}");
                }
            }
        }

        result.Message = $"{result.AddedCount} katılımcı eklendi" +
            (result.SkippedCount > 0 ? $", {result.SkippedCount} atlandı" : "") +
            (result.EmailSentCount > 0 ? $", {result.EmailSentCount} email gönderildi" : "");

        return result;
    }

    /// <summary>
    /// Dış katılımcı siler
    /// </summary>
    public async Task<bool> DeleteExternalParticipantAsync(int participantId)
    {
        var participant = await _context.TrainingVideoExternalParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId && !p.IsDeleted);

        if (participant == null)
            return false;

        participant.IsDeleted = true;
        participant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Dış katılımcılara email gönderir
    /// </summary>
    public async Task<int> SendExternalEmailsAsync(SendExternalEmailsDto dto, int? sentByUserId = null)
    {
        var assignment = await _context.TrainingVideoAssignments
            .Include(a => a.TrainingVideo)
            .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId && !a.IsDeleted);

        if (assignment == null)
            return 0;

        var participants = await _context.TrainingVideoExternalParticipants
            .Where(p => !p.IsDeleted && dto.ParticipantIds.Contains(p.Id))
            .ToListAsync();

        var templateId = dto.EmailTemplateId ?? assignment.EmailTemplateId;
        var sentCount = 0;

        foreach (var participant in participants)
        {
            try
            {
                await SendExternalParticipantEmailAsync(participant, assignment, templateId, sentByUserId, dto.EmailTypeId);
                sentCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email gönderilemedi: {participant.Email} - {ex.Message}");
            }
        }

        return sentCount;
    }

    /// <summary>
    /// Token ile video bilgisini getirir (halka açık erişim)
    /// </summary>
    public async Task<ExternalVideoWatchDto?> GetExternalVideoByTokenAsync(string token)
    {
        var participant = await _context.TrainingVideoExternalParticipants
            .Include(p => p.Assignment)
                .ThenInclude(a => a.TrainingVideo)
            .FirstOrDefaultAsync(p => p.Token == token && !p.IsDeleted);

        if (participant == null)
            return null;

        var assignment = participant.Assignment;
        if (assignment == null || assignment.IsDeleted)
            return null;

        var video = assignment.TrainingVideo;
        if (video == null || video.IsDeleted)
            return null;

        // İlk açılış ise işaretle
        if (!participant.IsOpened)
        {
            participant.IsOpened = true;
            participant.OpenedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return new ExternalVideoWatchDto
        {
            ParticipantId = participant.Id,
            VideoTitle = video.Title,
            VideoDescription = video.Description,
            VideoDurationSeconds = video.DurationSeconds,
            VideoStreamUrl = $"/api/training-video-assignments/external/{token}/stream",
            AssignmentTitle = assignment.Title,
            DueDate = assignment.DueDate,
            MinWatchCount = assignment.MinWatchCount,
            MaxWatchCount = assignment.MaxWatchCount,
            AllowSpeedChange = assignment.AllowSpeedChange,
            AllowSeeking = assignment.AllowSeeking,
            CurrentWatchCount = participant.WatchCount,
            WatchedSeconds = participant.WatchedSeconds,
            IsCompleted = participant.IsCompleted,
            IsExpired = DateTime.UtcNow > assignment.DueDate,
            ParticipantName = participant.FullName
        };
    }

    /// <summary>
    /// Dış katılımcı izleme ilerlemesini günceller
    /// </summary>
    public async Task<bool> UpdateExternalWatchProgressAsync(string token, UpdateWatchProgressDto dto)
    {
        var participant = await _context.TrainingVideoExternalParticipants
            .Include(p => p.Assignment)
                .ThenInclude(a => a.TrainingVideo)
            .FirstOrDefaultAsync(p => p.Token == token && !p.IsDeleted);

        if (participant == null)
            return false;

        var assignment = participant.Assignment;
        var video = assignment.TrainingVideo;

        // İlk izleme başlangıcı
        if (participant.StatusId == 1 && dto.WatchedSeconds > 0)
        {
            participant.StatusId = 2; // InProgress
            participant.StartedAt = DateTime.UtcNow;
        }

        // İzleme süresi güncelle
        if (dto.WatchedSeconds > participant.WatchedSeconds)
        {
            participant.WatchedSeconds = dto.WatchedSeconds;
        }

        // Video sonuna ulaşıldı mı?
        if (dto.IsVideoEnded)
        {
            participant.WatchCount++;

            // Minimum izleme yüzdesi kontrolü
            var watchPercentage = (decimal)participant.WatchedSeconds / video.DurationSeconds * 100;

            // Tamamlandı mı?
            if (participant.WatchCount >= assignment.MinWatchCount && watchPercentage >= video.MinWatchPercentage)
            {
                participant.IsCompleted = true;
                participant.StatusId = 3; // Completed
                participant.CompletedAt = DateTime.UtcNow;
            }
        }

        participant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Dış katılımcıya email gönderir (internal helper)
    /// </summary>
    private async Task SendExternalParticipantEmailAsync(
        TrainingVideoExternalParticipant participant,
        TrainingVideoAssignment assignment,
        int? templateId,
        int? sentByUserId,
        int emailTypeId)
    {
        var watchUrl = $"{GetBaseUrl()}/Training/External/{participant.Token}";

        // Email şablonu
        var template = templateId.HasValue
            ? await _context.EmailTemplates.FirstOrDefaultAsync(t => t.Id == templateId && !t.IsDeleted)
            : null;

        string subject, body;

        if (template != null)
        {
            subject = ReplaceExternalTrainingPlaceholders(template.Subject, assignment, participant, watchUrl);
            body = ReplaceExternalTrainingPlaceholders(template.Body, assignment, participant, watchUrl);
        }
        else
        {
            subject = emailTypeId == 1
                ? $"Eğitim Videosu: {assignment.TrainingVideo.Title}"
                : $"Hatırlatma: {assignment.TrainingVideo.Title}";

            body = $@"
                <p>Merhaba {participant.FullName ?? ""},</p>
                <p>Size <strong>{assignment.TrainingVideo.Title}</strong> eğitim videosu atanmıştır.</p>
                <p>Son izleme tarihi: <strong>{assignment.DueDate:dd.MM.yyyy}</strong></p>
                <p><a href=""{watchUrl}"" style=""display:inline-block;padding:10px 20px;background-color:#0d6efd;color:white;text-decoration:none;border-radius:5px;"">Videoyu İzle</a></p>
                <p>İyi çalışmalar.</p>
            ";
        }

        await _emailService.SendEmailAsync(participant.Email, subject, body);

        // Email log kaydet
        var emailLog = new TrainingVideoExternalEmailLog
        {
            TrainingVideoExternalParticipantId = participant.Id,
            EmailTemplateId = templateId,
            SentAt = DateTime.UtcNow,
            SentByUserId = sentByUserId,
            EmailTypeId = emailTypeId,
            IsSuccess = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.TrainingVideoExternalEmailLogs.Add(emailLog);

        // Participant güncelle
        participant.EmailSentCount++;
        participant.LastEmailSentAt = DateTime.UtcNow;
        if (!participant.FirstEmailSentAt.HasValue)
        {
            participant.FirstEmailSentAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Email format kontrolü
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Katılımcı durumu adını getirir
    /// </summary>
    private static string GetParticipantStatusName(int statusId)
    {
        return statusId switch
        {
            1 => "Bekliyor",
            2 => "İzliyor",
            3 => "Tamamladı",
            _ => "Bilinmiyor"
        };
    }

    /// <summary>
    /// Token ile video stream'ini getirir (dış katılımcı için)
    /// </summary>
    public async Task<Stream?> GetExternalVideoStreamAsync(string token)
    {
        var participant = await _context.TrainingVideoExternalParticipants
            .Include(p => p.Assignment)
            .FirstOrDefaultAsync(p => p.Token == token && !p.IsDeleted);

        if (participant == null)
            return null;

        return await GetVideoStreamAsync(participant.Assignment.TrainingVideoId);
    }

    #endregion
}
