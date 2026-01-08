using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.DTOs.PersonnelRequest;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class PersonnelRequestService : IPersonnelRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PersonnelRequestService> _logger;

    public PersonnelRequestService(
        ApplicationDbContext context,
        ILogger<PersonnelRequestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(List<PersonnelRequestDto> Items, int TotalCount)> GetAllAsync(PersonnelRequestFilterDto filter)
    {
        var query = _context.PersonnelRequests
            .Include(pr => pr.Customer)
            .Include(pr => pr.CustomerOrganization)
            .Include(pr => pr.RequestedByUser)
            .Include(pr => pr.ReviewedByUser)
            .Include(pr => pr.CreatedPersonnel)
            .AsQueryable();

        // Filtreler
        if (filter.Status.HasValue)
        {
            query = query.Where(pr => pr.Status == filter.Status.Value);
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(pr =>
                pr.FirstName.ToLower().Contains(term) ||
                pr.LastName.ToLower().Contains(term) ||
                pr.Customer.CompanyName.ToLower().Contains(term) ||
                pr.CustomerOrganization.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(pr => pr.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(pr => MapToDto(pr))
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<PersonnelRequestDto?> GetByIdAsync(int id)
    {
        var request = await _context.PersonnelRequests
            .Include(pr => pr.Customer)
            .Include(pr => pr.CustomerOrganization)
            .Include(pr => pr.RequestedByUser)
            .Include(pr => pr.ReviewedByUser)
            .Include(pr => pr.CreatedPersonnel)
            .FirstOrDefaultAsync(pr => pr.Id == id);

        return request == null ? null : MapToDto(request);
    }

    public async Task<PersonnelRequest?> GetByEvaluationIdAsync(int evaluationId)
    {
        return await _context.PersonnelRequests
            .FirstOrDefaultAsync(pr => pr.EvaluationId == evaluationId);
    }

    public async Task<PersonnelRequestDto> CreateAsync(CreatePersonnelRequestDto dto, int requestedByUserId)
    {
        var request = new PersonnelRequest
        {
            EvaluationId = dto.EvaluationId,
            CustomerId = dto.CustomerId,
            CustomerOrganizationId = dto.CustomerOrganizationId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Title = dto.Title,
            Notes = dto.Notes,
            RequestedByUserId = requestedByUserId,
            Status = ApprovalStatuses.Ids.Pending
        };

        _context.PersonnelRequests.Add(request);
        await _context.SaveChangesAsync();

        // Admin'lere bildirim gönder
        var admins = await _context.Users
            .Where(u => u.Role == UserRole.Admin && u.IsActive && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var adminId in admins)
        {
            await CreateNotificationAsync(
                adminId,
                "PersonnelRequest.New",
                $"Yeni personel talebi: {request.FullName}",
                $"/UserRequests?tab=personnel&id={request.Id}");
        }

        _logger.LogInformation("Personnel request created: {Id} - {FullName}", request.Id, request.FullName);

        return await GetByIdAsync(request.Id) ?? throw new Exception("Request not found after creation");
    }

    public async Task<PersonnelRequestDto> ApproveAsync(ApprovePersonnelRequestDto dto, int reviewedByUserId)
    {
        var request = await _context.PersonnelRequests
            .Include(pr => pr.Evaluation)
            .FirstOrDefaultAsync(pr => pr.Id == dto.Id);

        if (request == null)
            throw new KeyNotFoundException($"Personnel request not found: {dto.Id}");

        if (request.Status != ApprovalStatuses.Ids.Pending)
            throw new InvalidOperationException("Only pending requests can be approved");

        // Email varsayılanı: username@temp.com
        var email = string.IsNullOrEmpty(dto.Email) ? $"{dto.Username}@temp.com" : dto.Email;

        // 1. CustomerPersonnel oluştur
        var personnel = new CustomerPersonnel
        {
            CustomerId = request.CustomerId,
            OrganizationId = request.CustomerOrganizationId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Title = request.Title,
            Username = dto.Username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("user@123"),
            MustChangePassword = true,
            SupervisorId = dto.SupervisorId,
            Role = CustomerPersonnelRole.CustomerOperator,
            IsActive = true
        };

        _context.CustomerPersonnel.Add(personnel);
        await _context.SaveChangesAsync();

        // 2. Request güncelle
        request.Status = ApprovalStatuses.Ids.Approved;
        request.ReviewedByUserId = reviewedByUserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.CreatedPersonnelId = personnel.Id;

        // 3. Evaluation güncelle
        if (request.Evaluation != null)
        {
            request.Evaluation.EvaluatedCustomerPersonnelId = personnel.Id;
        }

        await _context.SaveChangesAsync();

        // 4. Talep eden kullanıcıya bildirim
        await CreateNotificationAsync(
            request.RequestedByUserId,
            "PersonnelRequest.Approved",
            $"Personel talebiniz onaylandı: {request.FullName}",
            $"/Evaluations?id={request.EvaluationId}");

        _logger.LogInformation("Personnel request approved: {Id} - {FullName}, Created Personnel: {PersonnelId}",
            request.Id, request.FullName, personnel.Id);

        return await GetByIdAsync(request.Id) ?? throw new Exception("Request not found after approval");
    }

    public async Task<PersonnelRequestDto> RejectAsync(RejectPersonnelRequestDto dto, int reviewedByUserId)
    {
        var request = await _context.PersonnelRequests
            .Include(pr => pr.Evaluation)
            .FirstOrDefaultAsync(pr => pr.Id == dto.Id);

        if (request == null)
            throw new KeyNotFoundException($"Personnel request not found: {dto.Id}");

        if (request.Status != ApprovalStatuses.Ids.Pending)
            throw new InvalidOperationException("Only pending requests can be rejected");

        // 1. Request güncelle
        request.Status = ApprovalStatuses.Ids.Rejected;
        request.ReviewedByUserId = reviewedByUserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.RejectReason = dto.RejectReason;

        // 2. Tamamlanmış değerlendirmeyi taslağa al
        if (request.Evaluation != null && request.Evaluation.Status == EvaluationStatus.Completed)
        {
            request.Evaluation.Status = EvaluationStatus.Draft;
            _logger.LogInformation("Evaluation {Id} reverted to draft due to rejected personnel request", request.EvaluationId);
        }

        await _context.SaveChangesAsync();

        // 3. Talep eden kullanıcıya bildirim
        await CreateNotificationAsync(
            request.RequestedByUserId,
            "PersonnelRequest.Rejected",
            $"Personel talebiniz reddedildi: {request.FullName}. Sebep: {dto.RejectReason}. İlgili değerlendirme taslağa alındı.",
            $"/Evaluations?id={request.EvaluationId}");

        _logger.LogInformation("Personnel request rejected: {Id} - {FullName}, Reason: {Reason}",
            request.Id, request.FullName, dto.RejectReason);

        return await GetByIdAsync(request.Id) ?? throw new Exception("Request not found after rejection");
    }

    public async Task<List<(int Id, string FullName)>> GetSupervisorsForOrganizationAsync(int customerOrganizationId)
    {
        return await _context.CustomerPersonnel
            .Where(cp => cp.OrganizationId == customerOrganizationId
                      && cp.Role == CustomerPersonnelRole.CustomerSupervisor
                      && cp.IsActive
                      && !cp.IsDeleted)
            .Select(cp => new ValueTuple<int, string>(cp.Id, cp.FirstName + " " + cp.LastName))
            .ToListAsync();
    }

    public async Task<(int Pending, int Approved, int Rejected)> GetStatusCountsAsync()
    {
        var counts = await _context.PersonnelRequests
            .GroupBy(pr => pr.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return (
            counts.FirstOrDefault(c => c.Status == ApprovalStatuses.Ids.Pending)?.Count ?? 0,
            counts.FirstOrDefault(c => c.Status == ApprovalStatuses.Ids.Approved)?.Count ?? 0,
            counts.FirstOrDefault(c => c.Status == ApprovalStatuses.Ids.Rejected)?.Count ?? 0
        );
    }

    private async Task CreateNotificationAsync(int userId, string title, string message, string? url = null)
    {
        var notification = new Notification
        {
            RecipientUserId = userId,
            NotificationType = NotificationType.Info,
            Channel = NotificationChannel.InApp,
            Priority = NotificationPriority.Normal,
            Title = title,
            Message = message,
            ActionUrl = url,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    private static PersonnelRequestDto MapToDto(PersonnelRequest pr)
    {
        return new PersonnelRequestDto
        {
            Id = pr.Id,
            EvaluationId = pr.EvaluationId,
            CustomerId = pr.CustomerId,
            CustomerName = pr.Customer?.CompanyName ?? "",
            CustomerOrganizationId = pr.CustomerOrganizationId,
            CustomerOrganizationName = pr.CustomerOrganization?.Name ?? "",
            FirstName = pr.FirstName,
            LastName = pr.LastName,
            Title = pr.Title,
            Notes = pr.Notes,
            RequestedByUserId = pr.RequestedByUserId,
            RequestedByUserName = pr.RequestedByUser != null
                ? $"{pr.RequestedByUser.FirstName} {pr.RequestedByUser.LastName}"
                : "",
            RequestedAt = pr.CreatedAt,
            Status = pr.Status,
            StatusName = ApprovalStatuses.GetById(pr.Status)?.NameResourceKey ?? "Unknown",
            ReviewedByUserId = pr.ReviewedByUserId,
            ReviewedByUserName = pr.ReviewedByUser != null
                ? $"{pr.ReviewedByUser.FirstName} {pr.ReviewedByUser.LastName}"
                : null,
            ReviewedAt = pr.ReviewedAt,
            RejectReason = pr.RejectReason,
            CreatedPersonnelId = pr.CreatedPersonnelId,
            CreatedPersonnelName = pr.CreatedPersonnel != null
                ? $"{pr.CreatedPersonnel.FirstName} {pr.CreatedPersonnel.LastName}"
                : null
        };
    }
}
