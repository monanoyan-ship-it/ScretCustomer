using FluentAssertions;
using SecretCustomer.Core.Entities;
using SecretCustomer.Services.Services;
using SecretCustomer.Tests.Unit.Infrastructure;

namespace SecretCustomer.Tests.Unit.Services;

public class SmtpProfileServiceTests
{
    private readonly SmtpProfileService _sut;
    private readonly Data.ApplicationDbContext _context;

    public SmtpProfileServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new SmtpProfileService(_context);
    }

    private SmtpProfile CreateProfile(string name = "Test SMTP", bool isDefault = false) => new()
    {
        Name = name,
        Host = "smtp.test.com",
        Port = 587,
        FromEmail = "test@test.com",
        UseSsl = true,
        Enabled = true,
        IsDefault = isDefault
    };

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsAllProfiles()
    {
        // Arrange
        _context.SmtpProfiles.AddRange(
            CreateProfile("SMTP 1"),
            CreateProfile("SMTP 2")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_CreatesProfile()
    {
        // Arrange
        var profile = CreateProfile("New SMTP");

        // Act
        var (success, id, message) = await _sut.CreateAsync(profile, isDefault: false);

        // Assert
        success.Should().BeTrue();
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_FirstProfileBecomesDefault()
    {
        // Arrange
        var profile = CreateProfile("First");

        // Act
        await _sut.CreateAsync(profile, isDefault: false);

        // Assert
        var saved = await _context.SmtpProfiles.FindAsync(profile.Id);
        saved!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_SetsDefaultAndClearsOthers()
    {
        // Arrange
        var existing = CreateProfile("Existing", isDefault: true);
        _context.SmtpProfiles.Add(existing);
        await _context.SaveChangesAsync();

        var newProfile = CreateProfile("New");

        // Act
        await _sut.CreateAsync(newProfile, isDefault: true);

        // Assert
        var old = await _context.SmtpProfiles.FindAsync(existing.Id);
        old!.IsDefault.Should().BeFalse();
        var fresh = await _context.SmtpProfiles.FindAsync(newProfile.Id);
        fresh!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_FailsWithoutName()
    {
        // Arrange
        var profile = CreateProfile();
        profile.Name = "";

        // Act
        var (success, _, message) = await _sut.CreateAsync(profile, false);

        // Assert
        success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_FailsWithoutHost()
    {
        // Arrange
        var profile = CreateProfile();
        profile.Host = "";

        // Act
        var (success, _, _) = await _sut.CreateAsync(profile, false);

        // Assert
        success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_FailsWithoutFromEmail()
    {
        // Arrange
        var profile = CreateProfile();
        profile.FromEmail = "";

        // Act
        var (success, _, _) = await _sut.CreateAsync(profile, false);

        // Assert
        success.Should().BeFalse();
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_UpdatesProfile()
    {
        // Arrange
        var profile = CreateProfile("Old");
        _context.SmtpProfiles.Add(profile);
        await _context.SaveChangesAsync();

        var updated = CreateProfile("Updated");

        // Act
        var (success, _) = await _sut.UpdateAsync(profile.Id, updated, false);

        // Assert
        success.Should().BeTrue();
        var saved = await _context.SmtpProfiles.FindAsync(profile.Id);
        saved!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalseWhenNotFound()
    {
        // Act
        var (success, _) = await _sut.UpdateAsync(999, CreateProfile(), false);

        // Assert
        success.Should().BeFalse();
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_SoftDeletesProfile()
    {
        // Arrange
        var profile = CreateProfile();
        _context.SmtpProfiles.Add(profile);
        await _context.SaveChangesAsync();

        // Act
        var (success, _) = await _sut.DeleteAsync(profile.Id);

        // Assert
        success.Should().BeTrue();
        var deleted = await _context.SmtpProfiles.FindAsync(profile.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_PreventsDefaultProfileDeletion()
    {
        // Arrange
        var profile = CreateProfile("Default", isDefault: true);
        _context.SmtpProfiles.Add(profile);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _sut.DeleteAsync(profile.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Contain("Varsayılan");
    }

    #endregion

    #region SetDefaultAsync

    [Fact]
    public async Task SetDefaultAsync_SetsNewDefault()
    {
        // Arrange
        var old = CreateProfile("Old", isDefault: true);
        var target = CreateProfile("Target");
        _context.SmtpProfiles.AddRange(old, target);
        await _context.SaveChangesAsync();

        // Act
        var (success, _) = await _sut.SetDefaultAsync(target.Id);

        // Assert
        success.Should().BeTrue();
        var oldProfile = await _context.SmtpProfiles.FindAsync(old.Id);
        oldProfile!.IsDefault.Should().BeFalse();
        var newDefault = await _context.SmtpProfiles.FindAsync(target.Id);
        newDefault!.IsDefault.Should().BeTrue();
    }

    #endregion
}
