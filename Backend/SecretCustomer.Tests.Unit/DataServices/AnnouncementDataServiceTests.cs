using FluentAssertions;
using SecretCustomer.Core.Entities;
using SecretCustomer.Services.Services.DataServices;
using SecretCustomer.Tests.Unit.Builders;
using SecretCustomer.Tests.Unit.Infrastructure;

namespace SecretCustomer.Tests.Unit.DataServices;

/// <summary>
/// DataService test pattern: CRUD operations on entities.
/// Bu pattern diğer DataService testleri için referans alınmalıdır.
/// </summary>
public class AnnouncementDataServiceTests
{
    private readonly AnnouncementDataService _sut;
    private readonly Data.ApplicationDbContext _context;

    public AnnouncementDataServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new AnnouncementDataService(_context);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity()
    {
        // Arrange
        var entity = new AnnouncementBuilder().WithTitle("Test").Build();
        _context.Announcements.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenNotFound()
    {
        // Act
        var result = await _sut.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNonDeletedEntities()
    {
        // Arrange
        var active = new AnnouncementBuilder().WithTitle("Active").Build();
        var deleted = new AnnouncementBuilder().WithTitle("Deleted").Deleted().Build();
        _context.Announcements.AddRange(active, deleted);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddAsync_AddsEntity()
    {
        // Arrange
        var entity = new AnnouncementBuilder().WithTitle("New").Build();

        // Act
        await _sut.AddAsync(entity);

        // Assert
        var saved = await _context.Announcements.FindAsync(entity.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("New");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        // Arrange
        var entity = new AnnouncementBuilder().WithTitle("Old").Build();
        _context.Announcements.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        entity.Title = "Updated";
        await _sut.UpdateAsync(entity);

        // Assert
        var updated = await _context.Announcements.FindAsync(entity.Id);
        updated!.Title.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesEntity()
    {
        // Arrange
        var entity = new AnnouncementBuilder().WithTitle("To Delete").Build();
        _context.Announcements.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        await _sut.DeleteAsync(entity.Id);

        // Assert
        var deleted = await _context.Announcements.FindAsync(entity.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothingWhenNotFound()
    {
        // Act - should not throw
        await _sut.DeleteAsync(999);

        // Assert - no exception thrown
    }
}
