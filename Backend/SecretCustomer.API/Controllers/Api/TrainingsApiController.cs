using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Training;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// Eğitim yönetimi API controller
/// </summary>
[Route("api/trainings")]
[ApiController]
[Authorize]
public class TrainingsApiController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILocalizationService _localizationService;

    public TrainingsApiController(
        ApplicationDbContext context,
        IWebHostEnvironment env,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _context = context;
        _env = env;
        _localizationService = localizationService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Eğitimleri listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTrainings([FromQuery] TrainingFilterDto filter)
    {
        var query = _context.Trainings
            .Include(t => t.Trainer)
            .Include(t => t.Project)
            .Include(t => t.Customer)
            .Include(t => t.Participants)
            .AsQueryable();

        if (filter.ProjectId.HasValue)
            query = query.Where(t => t.ProjectId == filter.ProjectId.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(t => t.CustomerId == filter.CustomerId.Value);

        if (filter.TrainerId.HasValue)
            query = query.Where(t => t.TrainerId == filter.TrainerId.Value);

        if (!string.IsNullOrEmpty(filter.TrainingType) && Enum.TryParse<TrainingType>(filter.TrainingType, out var trainingType))
            query = query.Where(t => t.TrainingType == trainingType);

        if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<TrainingStatus>(filter.Status, out var status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrEmpty(filter.Category) && Enum.TryParse<TrainingCategory>(filter.Category, out var category))
            query = query.Where(t => t.Category == category);

        if (filter.StartDate.HasValue)
            query = query.Where(t => t.StartDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(t => t.StartDate <= filter.EndDate.Value);

        if (filter.IsMandatory.HasValue)
            query = query.Where(t => t.IsMandatory == filter.IsMandatory.Value);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(searchTerm) ||
                                     t.Code.ToLower().Contains(searchTerm) ||
                                     (t.Description != null && t.Description.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync();

        var trainings = await query
            .OrderByDescending(t => t.StartDate ?? t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new TrainingListDto
            {
                Id = t.Id,
                Code = t.Code,
                Title = t.Title,
                TrainingType = t.TrainingType.ToString(),
                Status = t.Status.ToString(),
                Category = t.Category.ToString(),
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                DurationMinutes = t.DurationMinutes,
                Location = t.Location,
                MaxParticipants = t.MaxParticipants,
                CurrentParticipants = t.Participants.Count(p => !p.IsDeleted),
                IsMandatory = t.IsMandatory,
                TrainerName = t.Trainer != null ? t.Trainer.FirstName + " " + t.Trainer.LastName : t.ExternalTrainerName,
                ProjectName = t.Project != null ? t.Project.Name : null
            })
            .ToListAsync();

        return Ok(new { items = trainings, totalCount });
    }

    /// <summary>
    /// Eğitim detayını getir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTraining(int id)
    {
        var training = await _context.Trainings
            .Include(t => t.Trainer)
            .Include(t => t.Project)
            .Include(t => t.Customer)
            .Include(t => t.Participants).ThenInclude(p => p.User)
            .Include(t => t.Materials).ThenInclude(m => m.UploadedByUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (training == null)
            return NotFound();

        var dto = new TrainingDto
        {
            Id = training.Id,
            Code = training.Code,
            Title = training.Title,
            Description = training.Description,
            TrainingType = training.TrainingType.ToString(),
            Status = training.Status.ToString(),
            Category = training.Category.ToString(),
            StartDate = training.StartDate,
            EndDate = training.EndDate,
            DurationMinutes = training.DurationMinutes,
            Location = training.Location,
            OnlineLink = training.OnlineLink,
            MaxParticipants = training.MaxParticipants,
            CurrentParticipants = training.Participants.Count(p => !p.IsDeleted),
            IsMandatory = training.IsMandatory,
            HasCertificate = training.HasCertificate,
            PassingScore = training.PassingScore,
            Curriculum = training.Curriculum,
            Prerequisites = training.Prerequisites,
            Objectives = training.Objectives,
            TrainerId = training.TrainerId,
            TrainerName = training.Trainer != null ? $"{training.Trainer.FirstName} {training.Trainer.LastName}" : null,
            ExternalTrainerName = training.ExternalTrainerName,
            ExternalTrainerCompany = training.ExternalTrainerCompany,
            ProjectId = training.ProjectId,
            ProjectName = training.Project?.Name,
            CustomerId = training.CustomerId,
            CustomerName = training.Customer?.CompanyName,
            Cost = training.Cost,
            Currency = training.Currency,
            Tags = training.Tags,
            CreatedAt = training.CreatedAt,
            Participants = training.Participants.Where(p => !p.IsDeleted).Select(p => new TrainingParticipantDto
            {
                Id = p.Id,
                TrainingId = p.TrainingId,
                UserId = p.UserId,
                UserName = p.User != null ? $"{p.User.FirstName} {p.User.LastName}" : null,
                ExternalName = p.ExternalName,
                ExternalEmail = p.ExternalEmail,
                ExternalPhone = p.ExternalPhone,
                Status = p.Status.ToString(),
                AttendanceDate = p.AttendanceDate,
                AttendanceMinutes = p.AttendanceMinutes,
                Score = p.Score,
                CertificateIssued = p.CertificateIssued,
                CertificateDate = p.CertificateDate,
                Feedback = p.Feedback,
                Rating = p.Rating,
                Notes = p.Notes
            }).ToList(),
            Materials = training.Materials.Where(m => !m.IsDeleted).OrderBy(m => m.SortOrder).Select(m => new TrainingMaterialDto
            {
                Id = m.Id,
                TrainingId = m.TrainingId,
                Name = m.Name,
                Description = m.Description,
                FileName = m.FileName,
                FileSize = m.FileSize,
                ContentType = m.ContentType,
                ExternalUrl = m.ExternalUrl,
                SortOrder = m.SortOrder,
                IsRequired = m.IsRequired,
                UploadedByName = m.UploadedByUser != null ? $"{m.UploadedByUser.FirstName} {m.UploadedByUser.LastName}" : null,
                CreatedAt = m.CreatedAt
            }).ToList()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Yeni eğitim oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTraining([FromBody] CreateTrainingDto dto)
    {
        var training = new Training
        {
            Code = await GenerateTrainingCode(),
            Title = dto.Title,
            Description = dto.Description,
            TrainingType = Enum.TryParse<TrainingType>(dto.TrainingType, out var trainingType) ? trainingType : TrainingType.InPerson,
            Status = TrainingStatus.Draft,
            Category = Enum.TryParse<TrainingCategory>(dto.Category, out var category) ? category : TrainingCategory.General,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            DurationMinutes = dto.DurationMinutes,
            Location = dto.Location,
            OnlineLink = dto.OnlineLink,
            MaxParticipants = dto.MaxParticipants,
            IsMandatory = dto.IsMandatory,
            HasCertificate = dto.HasCertificate,
            PassingScore = dto.PassingScore,
            Curriculum = dto.Curriculum,
            Prerequisites = dto.Prerequisites,
            Objectives = dto.Objectives,
            TrainerId = dto.TrainerId,
            ExternalTrainerName = dto.ExternalTrainerName,
            ExternalTrainerCompany = dto.ExternalTrainerCompany,
            ProjectId = dto.ProjectId,
            CustomerId = dto.CustomerId,
            Cost = dto.Cost,
            Currency = dto.Currency,
            Tags = dto.Tags,
            CreatedByUserId = GetCurrentUserId()
        };

        _context.Trainings.Add(training);
        await _context.SaveChangesAsync();

        return Ok(new { id = training.Id, code = training.Code });
    }

    /// <summary>
    /// Eğitimi güncelle
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTraining(int id, [FromBody] UpdateTrainingDto dto)
    {
        var training = await _context.Trainings.FindAsync(id);
        if (training == null)
            return NotFound();

        training.Title = dto.Title;
        training.Description = dto.Description;
        training.TrainingType = Enum.TryParse<TrainingType>(dto.TrainingType, out var trainingType) ? trainingType : training.TrainingType;
        training.Status = Enum.TryParse<TrainingStatus>(dto.Status, out var status) ? status : training.Status;
        training.Category = Enum.TryParse<TrainingCategory>(dto.Category, out var category) ? category : training.Category;
        training.StartDate = dto.StartDate;
        training.EndDate = dto.EndDate;
        training.DurationMinutes = dto.DurationMinutes;
        training.Location = dto.Location;
        training.OnlineLink = dto.OnlineLink;
        training.MaxParticipants = dto.MaxParticipants;
        training.IsMandatory = dto.IsMandatory;
        training.HasCertificate = dto.HasCertificate;
        training.PassingScore = dto.PassingScore;
        training.Curriculum = dto.Curriculum;
        training.Prerequisites = dto.Prerequisites;
        training.Objectives = dto.Objectives;
        training.TrainerId = dto.TrainerId;
        training.ExternalTrainerName = dto.ExternalTrainerName;
        training.ExternalTrainerCompany = dto.ExternalTrainerCompany;
        training.ProjectId = dto.ProjectId;
        training.CustomerId = dto.CustomerId;
        training.Cost = dto.Cost;
        training.Currency = dto.Currency;
        training.Tags = dto.Tags;

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Eğitimi sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTraining(int id)
    {
        var training = await _context.Trainings.FindAsync(id);
        if (training == null)
            return NotFound();

        training.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Eğitimi başlat
    /// </summary>
    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartTraining(int id)
    {
        var training = await _context.Trainings.FindAsync(id);
        if (training == null)
            return NotFound();

        training.Status = TrainingStatus.InProgress;
        if (!training.StartDate.HasValue)
            training.StartDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Eğitimi tamamla
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteTraining(int id, [FromBody] CompleteTrainingDto dto)
    {
        var training = await _context.Trainings.FindAsync(id);
        if (training == null)
            return NotFound();

        training.Status = TrainingStatus.Completed;
        if (!training.EndDate.HasValue)
            training.EndDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Eğitimi iptal et
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelTraining(int id)
    {
        var training = await _context.Trainings.FindAsync(id);
        if (training == null)
            return NotFound();

        training.Status = TrainingStatus.Cancelled;
        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Eğitimi ertele
    /// </summary>
    [HttpPost("{id}/postpone")]
    public async Task<IActionResult> PostponeTraining(int id, [FromBody] DateTime? newDate)
    {
        var training = await _context.Trainings.FindAsync(id);
        if (training == null)
            return NotFound();

        training.Status = TrainingStatus.Postponed;
        if (newDate.HasValue)
            training.StartDate = newDate.Value;

        await _context.SaveChangesAsync();
        return Ok();
    }

    #region Participants

    /// <summary>
    /// Katılımcı ekle
    /// </summary>
    [HttpPost("{id}/participants")]
    public async Task<IActionResult> AddParticipant(int id, [FromBody] AddTrainingParticipantDto dto)
    {
        var training = await _context.Trainings.Include(t => t.Participants).FirstOrDefaultAsync(t => t.Id == id);
        if (training == null)
            return NotFound();

        if (training.MaxParticipants.HasValue && training.Participants.Count(p => !p.IsDeleted) >= training.MaxParticipants.Value)
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Training.MaxParticipantsReached")));

        if (dto.UserId.HasValue)
        {
            var exists = await _context.TrainingParticipants.AnyAsync(p => p.TrainingId == id && p.UserId == dto.UserId && !p.IsDeleted);
            if (exists)
                return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Training.ParticipantAlreadyExists")));
        }

        var participant = new TrainingParticipant
        {
            TrainingId = id,
            UserId = dto.UserId,
            ExternalName = dto.ExternalName,
            ExternalEmail = dto.ExternalEmail,
            ExternalPhone = dto.ExternalPhone,
            Status = TrainingParticipantStatus.Invited
        };

        _context.TrainingParticipants.Add(participant);
        await _context.SaveChangesAsync();

        return Ok(new { id = participant.Id });
    }

    /// <summary>
    /// Katılımcı durumunu güncelle
    /// </summary>
    [HttpPut("{id}/participants/{participantId}")]
    public async Task<IActionResult> UpdateParticipantStatus(int id, int participantId, [FromBody] UpdateParticipantStatusDto dto)
    {
        var participant = await _context.TrainingParticipants.FirstOrDefaultAsync(p => p.Id == participantId && p.TrainingId == id);
        if (participant == null)
            return NotFound();

        if (Enum.TryParse<TrainingParticipantStatus>(dto.Status, out var status))
        {
            participant.Status = status;
            if (status == TrainingParticipantStatus.Attended || status == TrainingParticipantStatus.Completed)
                participant.AttendanceDate = DateTime.UtcNow;
        }

        participant.Score = dto.Score;
        participant.Feedback = dto.Feedback;
        participant.Rating = dto.Rating;
        participant.Notes = dto.Notes;

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Sertifika ver
    /// </summary>
    [HttpPost("{id}/participants/{participantId}/certificate")]
    public async Task<IActionResult> IssueCertificate(int id, int participantId)
    {
        var participant = await _context.TrainingParticipants.FirstOrDefaultAsync(p => p.Id == participantId && p.TrainingId == id);
        if (participant == null)
            return NotFound();

        participant.CertificateIssued = true;
        participant.CertificateDate = DateTime.UtcNow;
        participant.Status = TrainingParticipantStatus.Completed;

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Katılımcı sil
    /// </summary>
    [HttpDelete("{id}/participants/{participantId}")]
    public async Task<IActionResult> RemoveParticipant(int id, int participantId)
    {
        var participant = await _context.TrainingParticipants.FirstOrDefaultAsync(p => p.Id == participantId && p.TrainingId == id);
        if (participant == null)
            return NotFound();

        participant.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok();
    }

    #endregion

    #region Materials

    /// <summary>
    /// Materyal ekle (dosya ile)
    /// </summary>
    [HttpPost("{id}/materials")]
    public async Task<IActionResult> AddMaterial(int id, [FromForm] IFormFile? file, [FromForm] string name, [FromForm] string? description, [FromForm] string? externalUrl, [FromForm] int sortOrder = 0, [FromForm] bool isRequired = false)
    {
        var training = await _context.Trainings.FindAsync(id);
        if (training == null)
            return NotFound();

        var material = new TrainingMaterial
        {
            TrainingId = id,
            Name = name,
            Description = description,
            ExternalUrl = externalUrl,
            SortOrder = sortOrder,
            IsRequired = isRequired,
            UploadedByUserId = GetCurrentUserId()
        };

        if (file != null && file.Length > 0)
        {
            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "trainings", id.ToString());
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            material.FileName = file.FileName;
            material.FilePath = filePath;
            material.FileSize = file.Length;
            material.ContentType = file.ContentType;
        }

        _context.TrainingMaterials.Add(material);
        await _context.SaveChangesAsync();

        return Ok(new { id = material.Id });
    }

    /// <summary>
    /// Materyal indir
    /// </summary>
    [HttpGet("{id}/materials/{materialId}/download")]
    public async Task<IActionResult> DownloadMaterial(int id, int materialId)
    {
        var material = await _context.TrainingMaterials.FirstOrDefaultAsync(m => m.Id == materialId && m.TrainingId == id);
        if (material == null || string.IsNullOrEmpty(material.FilePath))
            return NotFound();

        if (!System.IO.File.Exists(material.FilePath))
            return NotFound();

        var bytes = await System.IO.File.ReadAllBytesAsync(material.FilePath);
        return File(bytes, material.ContentType ?? "application/octet-stream", material.FileName);
    }

    /// <summary>
    /// Materyal sil
    /// </summary>
    [HttpDelete("{id}/materials/{materialId}")]
    public async Task<IActionResult> DeleteMaterial(int id, int materialId)
    {
        var material = await _context.TrainingMaterials.FirstOrDefaultAsync(m => m.Id == materialId && m.TrainingId == id);
        if (material == null)
            return NotFound();

        material.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok();
    }

    #endregion

    /// <summary>
    /// Eğitim özeti
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var now = DateTime.UtcNow;

        var summary = new TrainingSummaryDto
        {
            TotalTrainings = await _context.Trainings.CountAsync(),
            PlannedTrainings = await _context.Trainings.CountAsync(t => t.Status == TrainingStatus.Planned || t.Status == TrainingStatus.Approved),
            InProgressTrainings = await _context.Trainings.CountAsync(t => t.Status == TrainingStatus.InProgress),
            CompletedTrainings = await _context.Trainings.CountAsync(t => t.Status == TrainingStatus.Completed),
            TotalParticipants = await _context.TrainingParticipants.CountAsync(p => p.Status == TrainingParticipantStatus.Attended || p.Status == TrainingParticipantStatus.Completed),
            CertificatesIssued = await _context.TrainingParticipants.CountAsync(p => p.CertificateIssued),
            UpcomingTrainings = await _context.Trainings.CountAsync(t => t.StartDate > now && (t.Status == TrainingStatus.Planned || t.Status == TrainingStatus.Approved)),
            RecentTrainings = await _context.Trainings
                .Include(t => t.Trainer)
                .Include(t => t.Participants)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new TrainingListDto
                {
                    Id = t.Id,
                    Code = t.Code,
                    Title = t.Title,
                    TrainingType = t.TrainingType.ToString(),
                    Status = t.Status.ToString(),
                    Category = t.Category.ToString(),
                    StartDate = t.StartDate,
                    TrainerName = t.Trainer != null ? t.Trainer.FirstName + " " + t.Trainer.LastName : t.ExternalTrainerName,
                    CurrentParticipants = t.Participants.Count(p => !p.IsDeleted)
                })
                .ToListAsync()
        };

        var ratings = await _context.TrainingParticipants
            .Where(p => p.Rating.HasValue)
            .Select(p => p.Rating!.Value)
            .ToListAsync();

        summary.AverageRating = ratings.Any() ? ratings.Average() : 0;

        return Ok(summary);
    }

    private async Task<string> GenerateTrainingCode()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Trainings.CountAsync(t => t.CreatedAt.Year == year) + 1;
        return $"TRN-{year}-{count:D4}";
    }
}
