using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Tests.Unit.Builders;

public class UserBuilder
{
    private readonly User _user = new()
    {
        Username = "testuser",
        Email = "test@example.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234!"),
        FirstName = "Test",
        LastName = "User",
        RoleId = UserRoles.Ids.Admin,
        IsActive = true
    };

    public UserBuilder WithId(int id) { _user.Id = id; return this; }
    public UserBuilder WithUsername(string username) { _user.Username = username; return this; }
    public UserBuilder WithEmail(string email) { _user.Email = email; return this; }
    public UserBuilder WithFirstName(string name) { _user.FirstName = name; return this; }
    public UserBuilder WithLastName(string name) { _user.LastName = name; return this; }
    public UserBuilder WithRole(int roleId) { _user.RoleId = roleId; return this; }
    public UserBuilder AsAdmin() { _user.RoleId = UserRoles.Ids.Admin; return this; }
    public UserBuilder AsQualitySpecialist() { _user.RoleId = UserRoles.Ids.QualitySpecialist; return this; }
    public UserBuilder AsFieldWorker() { _user.RoleId = UserRoles.Ids.FieldWorker; return this; }
    public UserBuilder Inactive() { _user.IsActive = false; return this; }
    public UserBuilder Deleted() { _user.IsDeleted = true; return this; }

    public User Build() => _user;
}
