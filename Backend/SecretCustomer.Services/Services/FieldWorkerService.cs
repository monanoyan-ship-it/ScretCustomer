using SecretCustomer.Core.DTOs.FieldWorker;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class FieldWorkerService : IFieldWorkerService
{
    private readonly IFieldWorkerRepository _fieldWorkerRepository;

    public FieldWorkerService(IFieldWorkerRepository fieldWorkerRepository)
    {
        _fieldWorkerRepository = fieldWorkerRepository;
    }

    public async Task<FieldWorkerDto?> GetByIdAsync(Guid id)
    {
        var fieldWorker = await _fieldWorkerRepository.GetByIdAsync(id);
        return fieldWorker == null ? null : MapToDto(fieldWorker);
    }

    public async Task<IEnumerable<FieldWorkerDto>> GetAllAsync(bool includeInactive = false)
    {
        var fieldWorkers = await _fieldWorkerRepository.GetAllAsync(includeInactive);
        return fieldWorkers.Select(MapToDto);
    }

    public async Task<IEnumerable<FieldWorkerDto>> GetActiveAsync()
    {
        var fieldWorkers = await _fieldWorkerRepository.GetActiveAsync();
        return fieldWorkers.Select(MapToDto);
    }

    public async Task<FieldWorkerDto> CreateAsync(CreateFieldWorkerDto createDto)
    {
        // Check if phone number already exists
        if (await _fieldWorkerRepository.ExistsByPhoneNumberAsync(createDto.PhoneNumber))
        {
            throw new InvalidOperationException("Bu telefon numarası zaten kullanılıyor.");
        }

        var fieldWorker = new FieldWorker
        {
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            PhoneNumber = createDto.PhoneNumber,
            Address = createDto.Address,
            Email = createDto.Email,
            IsActive = createDto.IsActive,
            Notes = createDto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var createdFieldWorker = await _fieldWorkerRepository.CreateAsync(fieldWorker);
        return MapToDto(createdFieldWorker);
    }

    public async Task<FieldWorkerDto> UpdateAsync(Guid id, UpdateFieldWorkerDto updateDto)
    {
        var fieldWorker = await _fieldWorkerRepository.GetByIdAsync(id);
        if (fieldWorker == null)
        {
            throw new KeyNotFoundException($"ID {id} ile saha çalışanı bulunamadı.");
        }

        // Check if phone number is being changed and if it's already in use
        if (fieldWorker.PhoneNumber != updateDto.PhoneNumber &&
            await _fieldWorkerRepository.ExistsByPhoneNumberAsync(updateDto.PhoneNumber, id))
        {
            throw new InvalidOperationException("Bu telefon numarası zaten kullanılıyor.");
        }

        fieldWorker.FirstName = updateDto.FirstName;
        fieldWorker.LastName = updateDto.LastName;
        fieldWorker.PhoneNumber = updateDto.PhoneNumber;
        fieldWorker.Address = updateDto.Address;
        fieldWorker.Email = updateDto.Email;
        fieldWorker.IsActive = updateDto.IsActive;
        fieldWorker.Notes = updateDto.Notes;

        var updatedFieldWorker = await _fieldWorkerRepository.UpdateAsync(fieldWorker);
        return MapToDto(updatedFieldWorker);
    }

    public async Task DeleteAsync(Guid id)
    {
        var fieldWorker = await _fieldWorkerRepository.GetByIdAsync(id);
        if (fieldWorker == null)
        {
            throw new KeyNotFoundException($"ID {id} ile saha çalışanı bulunamadı.");
        }

        await _fieldWorkerRepository.DeleteAsync(id);
    }

    private static FieldWorkerDto MapToDto(FieldWorker fieldWorker)
    {
        return new FieldWorkerDto
        {
            Id = fieldWorker.Id,
            FirstName = fieldWorker.FirstName,
            LastName = fieldWorker.LastName,
            PhoneNumber = fieldWorker.PhoneNumber,
            Address = fieldWorker.Address,
            Email = fieldWorker.Email,
            IsActive = fieldWorker.IsActive,
            Notes = fieldWorker.Notes,
            CreatedAt = fieldWorker.CreatedAt,
            UpdatedAt = fieldWorker.UpdatedAt
        };
    }
}
