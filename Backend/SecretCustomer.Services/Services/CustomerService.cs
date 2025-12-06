using SecretCustomer.Core.DTOs.Customer;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id, includeDetails: true);
        return customer == null ? null : MapToDto(customer);
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync(bool includeInactive = false)
    {
        var customers = await _customerRepository.GetAllAsync(includeInactive);
        return customers.Select(MapToDto);
    }

    public async Task<IEnumerable<CustomerDto>> GetActiveAsync()
    {
        var customers = await _customerRepository.GetActiveAsync();
        return customers.Select(MapToDto);
    }

    public async Task<CustomerDto?> GetByTaxNumberAsync(string taxNumber)
    {
        var customer = await _customerRepository.GetByTaxNumberAsync(taxNumber);
        return customer == null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto createCustomerDto)
    {
        // Check if tax number already exists
        if (await _customerRepository.ExistsByTaxNumberAsync(createCustomerDto.TaxNumber))
        {
            throw new InvalidOperationException("Bu vergi numarası ile kayıtlı bir müşteri zaten mevcut");
        }

        // Check if email already exists (if provided)
        if (!string.IsNullOrEmpty(createCustomerDto.Email) &&
            await _customerRepository.ExistsByEmailAsync(createCustomerDto.Email))
        {
            throw new InvalidOperationException("Bu e-posta adresi ile kayıtlı bir müşteri zaten mevcut");
        }

        var customer = new Customer
        {
            CompanyName = createCustomerDto.CompanyName,
            TaxNumber = createCustomerDto.TaxNumber,
            Phone = createCustomerDto.Phone,
            Email = createCustomerDto.Email,
            Address = createCustomerDto.Address,
            City = createCustomerDto.City,
            IsActive = createCustomerDto.IsActive,
            ContractStartDate = createCustomerDto.ContractStartDate,
            ContractEndDate = createCustomerDto.ContractEndDate,
            Notes = createCustomerDto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var createdCustomer = await _customerRepository.CreateAsync(customer);
        return MapToDto(createdCustomer);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerDto updateCustomerDto)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Müşteri bulunamadı (ID: {id})");
        }

        // Check if tax number is being changed and if it's already in use
        if (customer.TaxNumber != updateCustomerDto.TaxNumber &&
            await _customerRepository.ExistsByTaxNumberAsync(updateCustomerDto.TaxNumber, id))
        {
            throw new InvalidOperationException("Bu vergi numarası ile kayıtlı bir müşteri zaten mevcut");
        }

        // Check if email is being changed and if it's already in use
        if (!string.IsNullOrEmpty(updateCustomerDto.Email) &&
            customer.Email != updateCustomerDto.Email &&
            await _customerRepository.ExistsByEmailAsync(updateCustomerDto.Email, id))
        {
            throw new InvalidOperationException("Bu e-posta adresi ile kayıtlı bir müşteri zaten mevcut");
        }

        customer.CompanyName = updateCustomerDto.CompanyName;
        customer.TaxNumber = updateCustomerDto.TaxNumber;
        customer.Phone = updateCustomerDto.Phone;
        customer.Email = updateCustomerDto.Email;
        customer.Address = updateCustomerDto.Address;
        customer.City = updateCustomerDto.City;
        customer.IsActive = updateCustomerDto.IsActive;
        customer.ContractStartDate = updateCustomerDto.ContractStartDate;
        customer.ContractEndDate = updateCustomerDto.ContractEndDate;
        customer.Notes = updateCustomerDto.Notes;

        var updatedCustomer = await _customerRepository.UpdateAsync(customer);
        return MapToDto(updatedCustomer);
    }

    public async Task DeleteAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Müşteri bulunamadı (ID: {id})");
        }

        await _customerRepository.DeleteAsync(id);
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            CompanyName = customer.CompanyName,
            TaxNumber = customer.TaxNumber,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address,
            City = customer.City,
            IsActive = customer.IsActive,
            ContractStartDate = customer.ContractStartDate,
            ContractEndDate = customer.ContractEndDate,
            Notes = customer.Notes,
            PersonnelCount = customer.Personnel?.Count ?? 0,
            BranchCount = customer.Branches?.Count ?? 0,
            ProjectCount = customer.Projects?.Count ?? 0,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }
}
