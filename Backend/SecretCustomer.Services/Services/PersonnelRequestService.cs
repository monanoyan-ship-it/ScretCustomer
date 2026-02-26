using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.PersonnelRequest;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services;

public class PersonnelRequestService : IPersonnelRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationCreatorService _notificationCreator;
    private readonly ILocalizationService _localizationService;

    public PersonnelRequestService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        INotificationCreatorService notificationCreator,
        ILocalizationService localizationService)
    {
        _context = context;
        _auditLogService = auditLogService;
        _notificationCreator = notificationCreator;
        _localizationService = localizationService;
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

        // Admin'lere bildirim gönder (SignalR push + email)
        var adminIds = await _context.Users
            .Where(u => u.RoleId == UserRoles.Ids.Admin && u.IsActive && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync();

        if (adminIds.Any())
        {
            await _notificationCreator.CreateBulkAsync(
                adminIds,
                NotificationTypes.Ids.Info,
                await _localizationService.GetResourceAsync("PersonnelRequest.New"),
                $"{await _localizationService.GetResourceAsync("PersonnelRequest.New")}: {request.FullName}",
                actionUrl: $"/UserRequests?tab=personnel&id={request.Id}",
                relatedEntityId: request.Id,
                relatedEntityType: "PersonnelRequest",
                senderUserId: requestedByUserId);
        }

        await _auditLogService.LogInfoAsync($"Personnel request created: {request.Id} - {request.FullName}", "PersonnelRequestService");

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

        // Orijinal adı sakla (toplu eşleşme için)
        var originalFirstName = request.FirstName;
        var originalLastName = request.LastName;

        // Admin ad/soyad düzelttiyse güncelle
        if (!string.IsNullOrWhiteSpace(dto.FirstName))
            request.FirstName = dto.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.LastName))
            request.LastName = dto.LastName.Trim();

        // Email varsayılanı: username@temp.com
        var email = string.IsNullOrEmpty(dto.Email) ? $"{dto.Username}@temp.com" : dto.Email;

        // 1. CustomerPersonnel oluştur (OrganizationId ve SupervisorId artık junction table'da)
        var personnel = new CustomerPersonnel
        {
            CustomerId = request.CustomerId,
            // OrganizationId ve SupervisorId artık junction table'da - doğrudan set etme
            FirstName = request.FirstName,
            LastName = request.LastName,
            Title = request.Title,
            Username = dto.Username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("user@123"),
            MustChangePassword = true,
            RoleId = CustomerPersonnelRoles.Ids.Operator,
            IsActive = true
        };

        _context.CustomerPersonnel.Add(personnel);
        await _context.SaveChangesAsync();

        // Organizasyon: DTO'dan gelirse override et, yoksa request'tekini kullan
        var organizationId = dto.CustomerOrganizationId ?? request.CustomerOrganizationId;

        // Junction table'a organizasyon ataması ekle
        var orgAssignment = new CustomerPersonnelOrganization
        {
            CustomerPersonnelId = personnel.Id,
            CustomerOrganizationId = organizationId,
            SupervisorId = dto.SupervisorId,
            AssignedAt = TurkeyTime.Now,
            Notes = $"PersonnelRequest #{request.Id} onayıyla oluşturuldu",
            CreatedAt = TurkeyTime.Now
        };
        _context.CustomerPersonnelOrganizations.Add(orgAssignment);
        await _context.SaveChangesAsync();

        // 2. Request güncelle
        request.Status = ApprovalStatuses.Ids.Approved;
        request.ReviewedByUserId = reviewedByUserId;
        request.ReviewedAt = TurkeyTime.Now;
        request.CreatedPersonnelId = personnel.Id;

        // 3. İLGİLİ DEĞERLENDİRMEYE personeli ata (en önemli adım)
        if (request.Evaluation != null)
        {
            request.Evaluation.EvaluatedCustomerPersonnelId = personnel.Id;
            request.Evaluation.EvaluatedUnknownPersonnel = null; // Artık tanımlı personel
            await _auditLogService.LogInfoAsync($"Evaluation {request.EvaluationId} assigned to new personnel {personnel.Id}", "PersonnelRequestService");
        }

        // 4. Aynı müşteride aynı ad-soyad ile "Listede Yok" olarak kaydedilmiş DİĞER evaluation'ları da güncelle
        // Orijinal ad/soyadla eşleştir (düzeltilmiş değil - çünkü diğer kayıtlar da eski adla kaydedilmiş)
        var originalFullName = $"{originalFirstName} {originalLastName}";
        var evaluationsToUpdate = await _context.Evaluations
            .Include(e => e.Project)
            .Where(e => e.Id != request.EvaluationId &&
                       e.Project.CustomerId == request.CustomerId &&
                       e.EvaluatedUnknownPersonnel != null &&
                       (e.EvaluatedUnknownPersonnel.ToLower() == originalFullName.ToLower() ||
                        e.EvaluatedUnknownPersonnel.ToLower() == $"{originalFirstName.ToLower()} {originalLastName.ToLower()}"))
            .ToListAsync();

        foreach (var evaluation in evaluationsToUpdate)
        {
            evaluation.EvaluatedCustomerPersonnelId = personnel.Id;
            evaluation.EvaluatedUnknownPersonnel = null;
        }

        await _auditLogService.LogInfoAsync($"Updated {evaluationsToUpdate.Count} additional evaluations with new personnel ID {personnel.Id}", "PersonnelRequestService");

        // 5. Aynı ad-soyad + aynı firma altındaki DİĞER bekleyen personel taleplerini de onayla
        // Orijinal ad/soyadla eşleştir (diğer talepler de eski adla kaydedilmiş)
        var otherPendingRequests = await _context.PersonnelRequests
            .Include(pr => pr.Evaluation)
            .Where(pr => pr.Id != request.Id &&
                        pr.CustomerId == request.CustomerId &&
                        pr.Status == ApprovalStatuses.Ids.Pending &&
                        !pr.IsDeleted &&
                        pr.FirstName.ToLower() == originalFirstName.ToLower() &&
                        pr.LastName.ToLower() == originalLastName.ToLower())
            .ToListAsync();

        foreach (var otherRequest in otherPendingRequests)
        {
            otherRequest.Status = ApprovalStatuses.Ids.Approved;
            otherRequest.ReviewedByUserId = reviewedByUserId;
            otherRequest.ReviewedAt = TurkeyTime.Now;
            otherRequest.CreatedPersonnelId = personnel.Id;

            // Diğer taleplerin ad/soyadını da düzeltilmiş hale güncelle
            otherRequest.FirstName = request.FirstName;
            otherRequest.LastName = request.LastName;

            // Bu talebin değerlendirmesine de personeli ata
            if (otherRequest.Evaluation != null)
            {
                otherRequest.Evaluation.EvaluatedCustomerPersonnelId = personnel.Id;
                otherRequest.Evaluation.EvaluatedUnknownPersonnel = null;
            }

            // Farklı organizasyondaysa, o organizasyon için de atama ekle (zaten yoksa)
            if (otherRequest.CustomerOrganizationId != organizationId)
            {
                var existingAssignment = await _context.CustomerPersonnelOrganizations
                    .AnyAsync(cpo => cpo.CustomerPersonnelId == personnel.Id &&
                                    cpo.CustomerOrganizationId == otherRequest.CustomerOrganizationId);
                if (!existingAssignment)
                {
                    _context.CustomerPersonnelOrganizations.Add(new CustomerPersonnelOrganization
                    {
                        CustomerPersonnelId = personnel.Id,
                        CustomerOrganizationId = otherRequest.CustomerOrganizationId,
                        AssignedAt = TurkeyTime.Now,
                        Notes = $"PersonnelRequest #{otherRequest.Id} toplu onayıyla oluşturuldu",
                        CreatedAt = TurkeyTime.Now
                    });
                }
            }
        }

        if (otherPendingRequests.Any())
        {
            await _auditLogService.LogInfoAsync($"Auto-approved {otherPendingRequests.Count} other pending requests for {originalFullName} (Customer: {request.CustomerId})", "PersonnelRequestService");
        }

        await _context.SaveChangesAsync();

        // 6. Talep eden kullanıcıya bildirim (SignalR push + email)
        await _notificationCreator.CreateAsync(
            request.RequestedByUserId,
            NotificationTypes.Ids.Success,
            await _localizationService.GetResourceAsync("PersonnelRequest.Approved"),
            $"Personel talebiniz onaylandı: {request.FullName}",
            actionUrl: $"/Evaluations?id={request.EvaluationId}",
            relatedEntityId: request.Id,
            relatedEntityType: "PersonnelRequest",
            senderUserId: reviewedByUserId);

        // Toplu onaylanan taleplerin sahiplerine de bildirim gönder
        var notifiedUserIds = new HashSet<int> { request.RequestedByUserId };
        foreach (var otherRequest in otherPendingRequests)
        {
            if (notifiedUserIds.Add(otherRequest.RequestedByUserId))
            {
                await _notificationCreator.CreateAsync(
                    otherRequest.RequestedByUserId,
                    NotificationTypes.Ids.Success,
                    await _localizationService.GetResourceAsync("PersonnelRequest.Approved"),
                    $"Personel talebiniz onaylandı: {otherRequest.FullName}",
                    actionUrl: $"/Evaluations?id={otherRequest.EvaluationId}",
                    relatedEntityId: otherRequest.Id,
                    relatedEntityType: "PersonnelRequest",
                    senderUserId: reviewedByUserId);
            }
        }

        await _auditLogService.LogInfoAsync($"Personnel request approved: {request.Id} - {request.FullName}, Created Personnel: {personnel.Id}", "PersonnelRequestService");

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
        request.ReviewedAt = TurkeyTime.Now;
        request.RejectReason = dto.RejectReason;

        string notificationMessage;

        // 2. Doğru personel seçildiyse değerlendirmeyi ona ata, seçilmediyse taslağa al
        if (request.Evaluation != null)
        {
            if (dto.CorrectPersonnelId.HasValue)
            {
                // Personel var mı kontrol et
                var correctPersonnel = await _context.CustomerPersonnel
                    .FirstOrDefaultAsync(cp => cp.Id == dto.CorrectPersonnelId.Value && !cp.IsDeleted);

                if (correctPersonnel != null)
                {
                    request.Evaluation.EvaluatedCustomerPersonnelId = dto.CorrectPersonnelId.Value;
                    // Değerlendirme tamamlanmış kalabilir, sadece personel değişir
                    await _auditLogService.LogInfoAsync($"Evaluation {request.EvaluationId} reassigned to personnel {dto.CorrectPersonnelId.Value} due to rejected personnel request", "PersonnelRequestService");

                    notificationMessage = $"Personel talebiniz reddedildi: {request.FullName}. Sebep: {dto.RejectReason}. Değerlendirme {correctPersonnel.FullName} adlı personele atandı.";
                }
                else
                {
                    // Personel bulunamazsa taslağa al
                    request.Evaluation.StatusId = EvaluationStatuses.Ids.Draft;
                    await _auditLogService.LogWarningAsync($"Correct personnel {dto.CorrectPersonnelId.Value} not found, evaluation {request.EvaluationId} reverted to draft", "PersonnelRequestService");

                    notificationMessage = $"Personel talebiniz reddedildi: {request.FullName}. Sebep: {dto.RejectReason}. İlgili değerlendirme taslağa alındı.";
                }
            }
            else
            {
                // Personel seçilmedi, taslağa al
                if (request.Evaluation.StatusId == EvaluationStatuses.Ids.Completed)
                {
                    request.Evaluation.StatusId = EvaluationStatuses.Ids.Draft;
                    await _auditLogService.LogInfoAsync($"Evaluation {request.EvaluationId} reverted to draft due to rejected personnel request", "PersonnelRequestService");
                }

                notificationMessage = $"Personel talebiniz reddedildi: {request.FullName}. Sebep: {dto.RejectReason}. İlgili değerlendirme taslağa alındı.";
            }
        }
        else
        {
            notificationMessage = $"Personel talebiniz reddedildi: {request.FullName}. Sebep: {dto.RejectReason}.";
        }

        await _context.SaveChangesAsync();

        // 3. Talep eden kullanıcıya bildirim (SignalR push + email)
        await _notificationCreator.CreateAsync(
            request.RequestedByUserId,
            NotificationTypes.Ids.Warning,
            await _localizationService.GetResourceAsync("PersonnelRequest.Rejected"),
            notificationMessage,
            actionUrl: $"/Evaluations?id={request.EvaluationId}",
            relatedEntityId: request.Id,
            relatedEntityType: "PersonnelRequest",
            senderUserId: reviewedByUserId);

        await _auditLogService.LogInfoAsync($"Personnel request rejected: {request.Id} - {request.FullName}, Reason: {dto.RejectReason}, CorrectPersonnelId: {dto.CorrectPersonnelId}", "PersonnelRequestService");

        return await GetByIdAsync(request.Id) ?? throw new Exception("Request not found after rejection");
    }

    public async Task<List<(int Id, string FullName)>> GetSupervisorsForOrganizationAsync(int customerOrganizationId)
    {
        // Sadece junction table'dan cek
        return await _context.CustomerPersonnel
            .Where(cp => cp.OrganizationAssignments.Any(oa => oa.CustomerOrganizationId == customerOrganizationId)
                      && cp.RoleId == CustomerPersonnelRoles.Ids.Supervisor
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
