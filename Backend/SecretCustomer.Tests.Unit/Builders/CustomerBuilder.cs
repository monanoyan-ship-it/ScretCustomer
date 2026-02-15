using SecretCustomer.Core.Entities;

namespace SecretCustomer.Tests.Unit.Builders;

public class CustomerBuilder
{
    private readonly Customer _customer = new()
    {
        CompanyName = "Test Company",
        TaxNumber = "1234567890",
        IsActive = true
    };

    public CustomerBuilder WithId(int id) { _customer.Id = id; return this; }
    public CustomerBuilder WithCompanyName(string name) { _customer.CompanyName = name; return this; }
    public CustomerBuilder WithCode(string code) { _customer.Code = code; return this; }
    public CustomerBuilder WithTaxNumber(string tax) { _customer.TaxNumber = tax; return this; }
    public CustomerBuilder WithEmail(string email) { _customer.Email = email; return this; }
    public CustomerBuilder WithPhone(string phone) { _customer.Phone = phone; return this; }
    public CustomerBuilder Inactive() { _customer.IsActive = false; return this; }
    public CustomerBuilder Deleted() { _customer.IsDeleted = true; return this; }

    public Customer Build() => _customer;
}
