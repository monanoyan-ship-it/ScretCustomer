using FluentAssertions;
using SecretCustomer.Core.DTOs.SystemSetting;
using SecretCustomer.Core.Entities;
using SecretCustomer.Services.Services;
using SecretCustomer.Tests.Unit.Infrastructure;

namespace SecretCustomer.Tests.Unit.Services;

public class SystemSettingServiceTests
{
    private readonly SystemSettingService _sut;
    private readonly Data.ApplicationDbContext _context;

    public SystemSettingServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new SystemSettingService(_context);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsNonDeletedSettings()
    {
        // Arrange
        _context.SystemSettings.AddRange(
            new SystemSetting { Key = "Key1", Value = "Value1" },
            new SystemSetting { Key = "Key2", Value = "Value2", IsDeleted = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Key.Should().Be("Key1");
    }

    #endregion

    #region GetByKeyAsync

    [Fact]
    public async Task GetByKeyAsync_ReturnsSetting()
    {
        // Arrange
        _context.SystemSettings.Add(new SystemSetting { Key = "TestKey", Value = "TestValue" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByKeyAsync("TestKey");

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("TestValue");
    }

    [Fact]
    public async Task GetByKeyAsync_ReturnsNullForDeletedSetting()
    {
        // Arrange
        _context.SystemSettings.Add(new SystemSetting { Key = "Deleted", Value = "V", IsDeleted = true });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByKeyAsync("Deleted");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetValueAsync / Typed Getters

    [Fact]
    public async Task GetValueAsync_ReturnsValue()
    {
        // Arrange
        _context.SystemSettings.Add(new SystemSetting { Key = "Key1", Value = "Hello" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetValueAsync("Key1");

        // Assert
        result.Should().Be("Hello");
    }

    [Fact]
    public async Task GetIntValueAsync_ParsesCorrectly()
    {
        // Arrange
        _context.SystemSettings.Add(new SystemSetting { Key = "IntKey", Value = "42" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetIntValueAsync("IntKey");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task GetIntValueAsync_ReturnsDefaultWhenNotFound()
    {
        // Act
        var result = await _sut.GetIntValueAsync("NonExistent", defaultValue: 10);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public async Task GetBoolValueAsync_ParsesCorrectly()
    {
        // Arrange
        _context.SystemSettings.Add(new SystemSetting { Key = "BoolKey", Value = "true" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetBoolValueAsync("BoolKey");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetDecimalValueAsync_ParsesCorrectly()
    {
        // Arrange
        _context.SystemSettings.Add(new SystemSetting { Key = "DecKey", Value = "3.14" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetDecimalValueAsync("DecKey");

        // Assert
        result.Should().Be(3.14m);
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_CreatesNewSetting()
    {
        // Arrange
        var dto = new CreateSystemSettingDto
        {
            Key = "NewKey",
            Value = "NewValue",
            Category = "General"
        };

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Key.Should().Be("NewKey");
        result.Value.Should().Be("NewValue");
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenKeyAlreadyExists()
    {
        // Arrange
        _context.SystemSettings.Add(new SystemSetting { Key = "Existing", Value = "V" });
        await _context.SaveChangesAsync();

        var dto = new CreateSystemSettingDto { Key = "Existing", Value = "V2" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(dto));
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_UpdatesExistingSetting()
    {
        // Arrange
        _context.SystemSettings.Add(new SystemSetting { Key = "UpdKey", Value = "Old" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.UpdateAsync("UpdKey", new UpdateSystemSettingDto { Value = "New" });

        // Assert
        result.Value.Should().Be("New");
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenKeyNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.UpdateAsync("NoKey", new UpdateSystemSettingDto { Value = "V" }));
    }

    #endregion

    #region SetValueAsync

    [Fact]
    public async Task SetValueAsync_CreatesWhenNotExists()
    {
        // Act
        var result = await _sut.SetValueAsync("AutoKey", "AutoValue");

        // Assert
        result.Should().BeTrue();
        var setting = await _sut.GetByKeyAsync("AutoKey");
        setting.Should().NotBeNull();
        setting!.Value.Should().Be("AutoValue");
    }

    [Fact]
    public async Task SetValueAsync_UpdatesWhenExists()
    {
        // Arrange
        _context.SystemSettings.Add(new SystemSetting { Key = "ExKey", Value = "Old" });
        await _context.SaveChangesAsync();

        // Act
        await _sut.SetValueAsync("ExKey", "New");

        // Assert
        var setting = await _sut.GetByKeyAsync("ExKey");
        setting!.Value.Should().Be("New");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_SoftDeletesSetting()
    {
        // Arrange
        _context.SystemSettings.Add(new SystemSetting { Key = "DelKey", Value = "V" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync("DelKey");

        // Assert
        result.Should().BeTrue();
        var setting = await _sut.GetByKeyAsync("DelKey");
        setting.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenNotFound()
    {
        // Act
        var result = await _sut.DeleteAsync("NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
