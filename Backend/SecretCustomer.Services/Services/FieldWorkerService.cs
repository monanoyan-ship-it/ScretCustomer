using SecretCustomer.Core.DTOs.FieldWorker;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Services.Services;

public class FieldWorkerService : IFieldWorkerService
{
    private readonly IFieldWorkerRepository _fieldWorkerRepository;
    private readonly IUserRepository _userRepository;

    public FieldWorkerService(
        IFieldWorkerRepository fieldWorkerRepository,
        IUserRepository userRepository)
    {
        _fieldWorkerRepository = fieldWorkerRepository;
        _userRepository = userRepository;
    }

    public async Task<FieldWorkerDto?> GetByIdAsync(Guid id)
    {
        var fieldWorker = await _fieldWorkerRepository.GetByIdAsync(id);
        return fieldWorker == null ? null : MapToDto(fieldWorker);
    }

    public async Task<FieldWorkerDto?> GetByUserIdAsync(Guid userId)
    {
        var fieldWorker = await _fieldWorkerRepository.GetByUserIdAsync(userId);
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

        // Check if username already exists
        if (await _userRepository.ExistsByUsernameAsync(createDto.Username))
        {
            throw new InvalidOperationException("Bu kullanıcı adı zaten kullanılıyor.");
        }

        // Check if email already exists
        if (!string.IsNullOrEmpty(createDto.Email) && await _userRepository.ExistsByEmailAsync(createDto.Email))
        {
            throw new InvalidOperationException("Bu e-posta adresi zaten kullanılıyor.");
        }

        // Create User first
        var user = new User
        {
            Username = createDto.Username,
            Email = createDto.Email ?? $"{createDto.Username}@fieldworker.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(createDto.Password),
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Role = UserRole.FieldWorker,
            IsActive = createDto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(user);

        // Create FieldWorker linked to User
        var fieldWorker = new FieldWorker
        {
            UserId = createdUser.Id,
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

        // Update FieldWorker
        fieldWorker.FirstName = updateDto.FirstName;
        fieldWorker.LastName = updateDto.LastName;
        fieldWorker.PhoneNumber = updateDto.PhoneNumber;
        fieldWorker.Address = updateDto.Address;
        fieldWorker.Email = updateDto.Email;
        fieldWorker.IsActive = updateDto.IsActive;
        fieldWorker.Notes = updateDto.Notes;

        // Update related User if exists
        if (fieldWorker.UserId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(fieldWorker.UserId.Value);
            if (user != null)
            {
                // Check if username is being changed and if it's already in use
                if (user.Username != updateDto.Username &&
                    await _userRepository.ExistsByUsernameAsync(updateDto.Username))
                {
                    throw new InvalidOperationException("Bu kullanıcı adı zaten kullanılıyor.");
                }

                // Check if email is being changed and if it's already in use
                if (!string.IsNullOrEmpty(updateDto.Email) && 
                    user.Email != updateDto.Email &&
                    await _userRepository.ExistsByEmailAsync(updateDto.Email))
                {
                    throw new InvalidOperationException("Bu e-posta adresi zaten kullanılıyor.");
                }

                user.Username = updateDto.Username;
                user.Email = updateDto.Email ?? user.Email;
                user.FirstName = updateDto.FirstName;
                user.LastName = updateDto.LastName;
                user.IsActive = updateDto.IsActive;
                await _userRepository.UpdateAsync(user);
            }
        }

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
            UserId = fieldWorker.UserId,
            Username = fieldWorker.User?.Username,
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
