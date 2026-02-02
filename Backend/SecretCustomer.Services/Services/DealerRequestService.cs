using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Dealer;
using SecretCustomer.Core.DTOs.DealerRequest;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class DealerRequestService : IDealerRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IDealerService _dealerService;

    public DealerRequestService(ApplicationDbContext context, IDealerService dealerService)
    {
        _context = context;
        _dealerService = dealerService;
    }

    public async Task<PagedDealerRequestResult> GetAllAsync(DealerRequestFilterDto filter)
    {
        var query = _context.CustomerDealerRequests
            .Include(r => r.Customer)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ProcessedByUser)
            .Include(r => r.CustomerDealer)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(r =>
                (r.Customer != null && r.Customer.CompanyName.ToLower().Contains(term)) ||
                (r.RequestedByUser != null && (r.RequestedByUser.FirstName + " " + r.RequestedByUser.LastName).ToLower().Contains(term)) ||
                (r.CustomerDealer != null && r.CustomerDealer.Name.ToLower().Contains(term)));
        }

        if (filter.CustomerId.HasValue)
            query = query.Where(r => r.CustomerId == filter.CustomerId.Value);

        if (filter.RequestedByUserId.HasValue)
            query = query.Where(r => r.RequestedByUserId == filter.RequestedByUserId.Value);

        if (filter.RequestTypeId.HasValue)
            query = query.Where(r => r.RequestTypeId == filter.RequestTypeId.Value);

        if (filter.StatusId.HasValue)
            query = query.Where(r => r.StatusId == filter.StatusId.Value);

        // Get total count
        var totalCount = await query.CountAsync();

        // Get status counts
        var pendingCount = await query.CountAsync(r => r.StatusId == CustomerDealerRequestStatuses.Ids.Pending);
        var approvedCount = await query.CountAsync(r => r.StatusId == CustomerDealerRequestStatuses.Ids.Approved);
        var rejectedCount = await query.CountAsync(r => r.StatusId == CustomerDealerRequestStatuses.Ids.Rejected);

        // Apply pagination
        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedDealerRequestResult
        {
            Items = requests.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            PendingCount = pendingCount,
            ApprovedCount = approvedCount,
            RejectedCount = rejectedCount
        };
    }

    public async Task<DealerRequestDto?> GetByIdAsync(int id)
    {
        var request = await _context.CustomerDealerRequests
            .Include(r => r.Customer)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ProcessedByUser)
            .Include(r => r.CustomerDealer)
            .FirstOrDefaultAsync(r => r.Id == id);

        return request == null ? null : MapToDto(request);
    }

    public async Task<List<DealerRequestDto>> GetMyRequestsAsync(int userId)
    {
        var requests = await _context.CustomerDealerRequests
            .Include(r => r.Customer)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ProcessedByUser)
            .Include(r => r.CustomerDealer)
            .Where(r => r.RequestedByUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(MapToDto).ToList();
    }

    public async Task<DealerRequestDto> CreateAsync(CreateDealerRequestDto dto, int requestedByUserId)
    {
        var request = new Core.Entities.CustomerDealerRequest
        {
            CustomerId = dto.CustomerId,
            RequestedByUserId = requestedByUserId,
            RequestTypeId = dto.RequestTypeId,
            StatusId = CustomerDealerRequestStatuses.Ids.Pending,
            CustomerDealerId = dto.CustomerDealerId,
            RequestDataJson = dto.RequestDataJson
        };

        _context.CustomerDealerRequests.Add(request);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(request.Id))!;
    }

    public async Task<DealerRequestDto?> ProcessAsync(int id, ProcessDealerRequestDto dto, int processedByUserId)
    {
        var request = await _context.CustomerDealerRequests
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            return null;

        // Check if already processed
        if (request.StatusId != CustomerDealerRequestStatuses.Ids.Pending)
            throw new InvalidOperationException("Bu talep zaten işlenmiş.");

        request.StatusId = dto.Approve ? CustomerDealerRequestStatuses.Ids.Approved : CustomerDealerRequestStatuses.Ids.Rejected;
        request.AdminResponse = dto.AdminResponse;
        request.ProcessedByUserId = processedByUserId;
        request.ProcessedAt = DateTime.UtcNow;

        // If approved and it's a new dealer request, create the dealer
        if (dto.Approve && request.RequestTypeId == CustomerDealerRequestTypes.Ids.NewDealer)
        {
            try
            {
                var requestData = JsonSerializer.Deserialize<DealerRequestData>(request.RequestDataJson);
                if (requestData != null)
                {
                    var createDealerDto = new CreateDealerDto
                    {
                        Name = requestData.Name ?? "Yeni Bayi",
                        Address = requestData.Address,
                        City = requestData.City,
                        District = requestData.District,
                        Latitude = requestData.Latitude,
                        Longitude = requestData.Longitude,
                        Phone = requestData.Phone,
                        Email = requestData.Email,
                        ContactPerson = requestData.ContactPerson,
                        WorkingHoursJson = requestData.WorkingHoursJson,
                        DealerTypeId = requestData.DealerTypeId,
                        Notes = requestData.Notes,
                        IsActive = true,
                        CustomerId = request.CustomerId
                    };

                    var dealer = await _dealerService.CreateAsync(createDealerDto);
                    request.CustomerDealerId = dealer.Id;
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Talep verileri geçersiz: " + ex.Message);
            }
        }
        // If approved and it's an update request, update the dealer
        else if (dto.Approve && request.RequestTypeId == CustomerDealerRequestTypes.Ids.UpdateDealer && request.CustomerDealerId.HasValue)
        {
            try
            {
                var requestData = JsonSerializer.Deserialize<DealerRequestData>(request.RequestDataJson);
                if (requestData != null)
                {
                    var updateDealerDto = new UpdateDealerDto
                    {
                        Name = requestData.Name ?? "Bayi",
                        Address = requestData.Address,
                        City = requestData.City,
                        District = requestData.District,
                        Latitude = requestData.Latitude,
                        Longitude = requestData.Longitude,
                        Phone = requestData.Phone,
                        Email = requestData.Email,
                        ContactPerson = requestData.ContactPerson,
                        WorkingHoursJson = requestData.WorkingHoursJson,
                        DealerTypeId = requestData.DealerTypeId,
                        Notes = requestData.Notes,
                        IsActive = true
                    };

                    await _dealerService.UpdateAsync(request.CustomerDealerId.Value, updateDealerDto);
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Talep verileri geçersiz: " + ex.Message);
            }
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var request = await _context.CustomerDealerRequests.FindAsync(id);
        if (request == null)
            return false;

        request.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetPendingCountAsync()
    {
        return await _context.CustomerDealerRequests
            .CountAsync(r => r.StatusId == CustomerDealerRequestStatuses.Ids.Pending);
    }

    private static DealerRequestDto MapToDto(Core.Entities.CustomerDealerRequest request)
    {
        return new DealerRequestDto
        {
            Id = request.Id,
            CustomerId = request.CustomerId,
            CustomerName = request.Customer?.CompanyName,
            RequestedByUserId = request.RequestedByUserId,
            RequestedByUserName = request.RequestedByUser != null
                ? $"{request.RequestedByUser.FirstName} {request.RequestedByUser.LastName}"
                : null,
            RequestTypeId = request.RequestTypeId,
            StatusId = request.StatusId,
            CustomerDealerId = request.CustomerDealerId,
            CustomerDealerName = request.CustomerDealer?.Name,
            RequestDataJson = request.RequestDataJson,
            AdminResponse = request.AdminResponse,
            ProcessedByUserId = request.ProcessedByUserId,
            ProcessedByUserName = request.ProcessedByUser != null
                ? $"{request.ProcessedByUser.FirstName} {request.ProcessedByUser.LastName}"
                : null,
            ProcessedAt = request.ProcessedAt,
            CreatedAt = request.CreatedAt
        };
    }
}
