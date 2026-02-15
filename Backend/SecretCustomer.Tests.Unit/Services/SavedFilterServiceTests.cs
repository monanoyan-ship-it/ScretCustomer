using FluentAssertions;
using SecretCustomer.Core.DTOs.SavedFilter;
using SecretCustomer.Services.Services;
using SecretCustomer.Tests.Unit.Builders;
using SecretCustomer.Tests.Unit.Infrastructure;

namespace SecretCustomer.Tests.Unit.Services;

public class SavedFilterServiceTests
{
    private readonly SavedFilterService _sut;
    private readonly Data.ApplicationDbContext _context;

    public SavedFilterServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new SavedFilterService(_context);
    }

    #region GetByUserAndPageAsync

    [Fact]
    public async Task GetByUserAndPageAsync_ReturnsFiltersForPage()
    {
        // Arrange
        var filter1 = new SavedFilterBuilder().WithUserId(1).WithPageName("Evaluations").WithName("F1").Build();
        var filter2 = new SavedFilterBuilder().WithUserId(1).WithPageName("Evaluations").WithName("F2").Build();
        var otherPage = new SavedFilterBuilder().WithUserId(1).WithPageName("Reports").WithName("F3").Build();
        _context.SavedFilters.AddRange(filter1, filter2, otherPage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByUserAndPageAsync(1, "Evaluations");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByUserAndPageAsync_ExcludesDeletedFilters()
    {
        // Arrange
        var active = new SavedFilterBuilder().WithUserId(1).WithPageName("Test").WithName("Active").Build();
        var deleted = new SavedFilterBuilder().WithUserId(1).WithPageName("Test").WithName("Deleted").Deleted().Build();
        _context.SavedFilters.AddRange(active, deleted);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByUserAndPageAsync(1, "Test");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByUserAndPageAsync_DefaultFilterFirst()
    {
        // Arrange
        var normal = new SavedFilterBuilder().WithUserId(1).WithPageName("Test").WithName("Normal").Build();
        var defaultFilter = new SavedFilterBuilder().WithUserId(1).WithPageName("Test").WithName("Default").AsDefault().Build();
        _context.SavedFilters.AddRange(normal, defaultFilter);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetByUserAndPageAsync(1, "Test")).ToList();

        // Assert
        result[0].Name.Should().Be("Default");
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ReturnsFilterWhenExists()
    {
        // Arrange
        var filter = new SavedFilterBuilder().WithName("Test Filter").Build();
        _context.SavedFilters.Add(filter);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(filter.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Filter");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForDeletedFilter()
    {
        // Arrange
        var filter = new SavedFilterBuilder().WithName("Deleted").Deleted().Build();
        _context.SavedFilters.Add(filter);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(filter.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_CreatesFilterAndReturnsDto()
    {
        // Arrange
        var dto = new CreateSavedFilterDto
        {
            PageName = "Evaluations",
            Name = "New Filter",
            FilterData = "{\"status\":\"active\"}"
        };

        // Act
        var result = await _sut.CreateAsync(1, dto);

        // Assert
        result.Name.Should().Be("New Filter");
        result.PageName.Should().Be("Evaluations");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_ClearsOtherDefaultsWhenSetAsDefault()
    {
        // Arrange
        var existingDefault = new SavedFilterBuilder()
            .WithPageName("Evaluations")
            .WithName("Old Default")
            .AsDefault()
            .Build();
        _context.SavedFilters.Add(existingDefault);
        await _context.SaveChangesAsync();

        var dto = new CreateSavedFilterDto
        {
            PageName = "Evaluations",
            Name = "New Default",
            FilterData = "{}",
            IsDefault = true
        };

        // Act
        await _sut.CreateAsync(1, dto);

        // Assert
        var oldDefault = await _context.SavedFilters.FindAsync(existingDefault.Id);
        oldDefault!.IsDefault.Should().BeFalse();
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_SoftDeletesFilter()
    {
        // Arrange
        var filter = new SavedFilterBuilder().WithName("To Delete").Build();
        _context.SavedFilters.Add(filter);
        await _context.SaveChangesAsync();

        // Act
        await _sut.DeleteAsync(filter.Id, 1);

        // Assert
        var deleted = await _context.SavedFilters.FindAsync(filter.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.DeleteAsync(999, 1));
    }

    #endregion

    #region SetDefaultAsync

    [Fact]
    public async Task SetDefaultAsync_SetsFilterAsDefault()
    {
        // Arrange
        var filter = new SavedFilterBuilder()
            .WithPageName("Test")
            .WithName("Filter 1")
            .Build();
        _context.SavedFilters.Add(filter);
        await _context.SaveChangesAsync();

        // Act
        await _sut.SetDefaultAsync(1, "Test", filter.Id);

        // Assert
        var updated = await _context.SavedFilters.FindAsync(filter.Id);
        updated!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task SetDefaultAsync_ClearsOtherDefaults()
    {
        // Arrange
        var oldDefault = new SavedFilterBuilder()
            .WithPageName("Test")
            .WithName("Old Default")
            .AsDefault()
            .Build();
        var newDefault = new SavedFilterBuilder()
            .WithPageName("Test")
            .WithName("New Default")
            .Build();
        _context.SavedFilters.AddRange(oldDefault, newDefault);
        await _context.SaveChangesAsync();

        // Act
        await _sut.SetDefaultAsync(1, "Test", newDefault.Id);

        // Assert
        var old = await _context.SavedFilters.FindAsync(oldDefault.Id);
        old!.IsDefault.Should().BeFalse();
        var fresh = await _context.SavedFilters.FindAsync(newDefault.Id);
        fresh!.IsDefault.Should().BeTrue();
    }

    #endregion
}
