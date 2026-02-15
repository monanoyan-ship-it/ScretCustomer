using SecretCustomer.Core.Entities;

namespace SecretCustomer.Tests.Unit.Builders;

public class SavedFilterBuilder
{
    private readonly SavedFilter _filter = new()
    {
        PageName = "TestPage",
        Name = "Test Filter",
        FilterData = "{}",
        IsDefault = false
    };

    public SavedFilterBuilder WithId(int id) { _filter.Id = id; return this; }
    public SavedFilterBuilder WithUserId(int userId) { _filter.UserId = userId; return this; }
    public SavedFilterBuilder WithCustomerId(int customerId) { _filter.CustomerId = customerId; return this; }
    public SavedFilterBuilder WithPageName(string pageName) { _filter.PageName = pageName; return this; }
    public SavedFilterBuilder WithName(string name) { _filter.Name = name; return this; }
    public SavedFilterBuilder WithDescription(string desc) { _filter.Description = desc; return this; }
    public SavedFilterBuilder WithFilterData(string data) { _filter.FilterData = data; return this; }
    public SavedFilterBuilder AsDefault() { _filter.IsDefault = true; return this; }
    public SavedFilterBuilder Deleted() { _filter.IsDeleted = true; return this; }

    public SavedFilter Build() => _filter;
}
