using FluentAssertions;
using SecretCustomer.Core.DTOs.Announcement;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Services.Services;
using SecretCustomer.Tests.Unit.Builders;
using SecretCustomer.Tests.Unit.Infrastructure;

namespace SecretCustomer.Tests.Unit.Services;

public class AnnouncementServiceTests
{
    private readonly AnnouncementService _sut;
    private readonly Data.ApplicationDbContext _context;

    public AnnouncementServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new AnnouncementService(_context);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsActiveAnnouncements()
    {
        // Arrange
        var active = new AnnouncementBuilder().WithTitle("Active").Build();
        var inactive = new AnnouncementBuilder().WithTitle("Inactive").Inactive().Build();
        _context.Announcements.AddRange(active, inactive);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync("Admin", includeExpired: true);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Active");
    }

    [Fact]
    public async Task GetAllAsync_ExcludesExpiredWhenNotIncluded()
    {
        // Arrange
        var valid = new AnnouncementBuilder().WithTitle("Valid").Build();
        var expired = new AnnouncementBuilder().WithTitle("Expired").Expired().Build();
        _context.Announcements.AddRange(valid, expired);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync("Admin", includeExpired: false);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Valid");
    }

    [Fact]
    public async Task GetAllAsync_IncludesExpiredWhenRequested()
    {
        // Arrange
        var valid = new AnnouncementBuilder().WithTitle("Valid").Build();
        var expired = new AnnouncementBuilder().WithTitle("Expired").Expired().Build();
        _context.Announcements.AddRange(valid, expired);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync("Admin", includeExpired: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByTargetRole()
    {
        // Arrange
        var adminOnly = new AnnouncementBuilder().WithTitle("Admin Only").WithTargetRoles("Admin").Build();
        var everyone = new AnnouncementBuilder().WithTitle("Everyone").Build();
        _context.Announcements.AddRange(adminOnly, everyone);
        await _context.SaveChangesAsync();

        // Act
        var adminResult = await _sut.GetAllAsync("Admin", includeExpired: true);
        var fieldWorkerResult = await _sut.GetAllAsync("FieldWorker", includeExpired: true);

        // Assert
        adminResult.Should().HaveCount(2);
        fieldWorkerResult.Should().HaveCount(1);
        fieldWorkerResult[0].Title.Should().Be("Everyone");
    }

    [Fact]
    public async Task GetAllAsync_OrdersByPinnedThenPriorityThenDate()
    {
        // Arrange
        var normal = new AnnouncementBuilder().WithTitle("Normal").WithPriority(1).Build();
        var highPriority = new AnnouncementBuilder().WithTitle("High").WithPriority(5).Build();
        var pinned = new AnnouncementBuilder().WithTitle("Pinned").WithPriority(1).Pinned().Build();
        _context.Announcements.AddRange(normal, highPriority, pinned);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync("Admin", includeExpired: true);

        // Assert
        result[0].Title.Should().Be("Pinned");
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ReturnsAnnouncementWhenExists()
    {
        // Arrange
        var announcement = new AnnouncementBuilder().WithTitle("Test").Build();
        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(announcement.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenNotExists()
    {
        // Act
        var result = await _sut.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_CreatesAnnouncementAndReturnsDto()
    {
        // Arrange
        var dto = new CreateAnnouncementDto
        {
            Title = "New Announcement",
            Content = "Content here",
            TypeId = AnnouncementTypes.Ids.Info,
            IsActive = true
        };

        // Act
        var (result, notifyUserIds) = await _sut.CreateAsync(dto, userId: 1);

        // Assert
        result.Title.Should().Be("New Announcement");
        result.Id.Should().BeGreaterThan(0);

        var saved = await _context.Announcements.FindAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.CreatedByUserId.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ExcludesCreatorFromNotifyList()
    {
        // Arrange
        var user = new UserBuilder().WithUsername("creator").AsAdmin().Build();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new CreateAnnouncementDto
        {
            Title = "Test",
            Content = "Content",
            TargetRoles = "Admin"
        };

        // Act
        var (_, notifyUserIds) = await _sut.CreateAsync(dto, userId: user.Id);

        // Assert
        notifyUserIds.Should().NotContain(user.Id);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_UpdatesExistingAnnouncement()
    {
        // Arrange
        var announcement = new AnnouncementBuilder().WithTitle("Old Title").Build();
        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();

        var dto = new CreateAnnouncementDto
        {
            Title = "New Title",
            Content = "Updated content",
            TypeId = AnnouncementTypes.Ids.Info
        };

        // Act
        var (success, message) = await _sut.UpdateAsync(announcement.Id, dto);

        // Assert
        success.Should().BeTrue();
        var updated = await _context.Announcements.FindAsync(announcement.Id);
        updated!.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalseWhenNotFound()
    {
        // Arrange
        var dto = new CreateAnnouncementDto { Title = "Test", Content = "Content" };

        // Act
        var (success, message) = await _sut.UpdateAsync(999, dto);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("NotFound");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_SoftDeletesAnnouncement()
    {
        // Arrange
        var announcement = new AnnouncementBuilder().WithTitle("To Delete").Build();
        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();

        // Act
        var (success, _) = await _sut.DeleteAsync(announcement.Id);

        // Assert
        success.Should().BeTrue();
        var deleted = await _context.Announcements.FindAsync(announcement.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenNotFound()
    {
        // Act
        var (success, message) = await _sut.DeleteAsync(999);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("NotFound");
    }

    #endregion

    #region GetForDashboardAsync

    [Fact]
    public async Task GetForDashboardAsync_ReturnsLimitedCount()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            _context.Announcements.Add(
                new AnnouncementBuilder()
                    .WithTitle($"Ann {i}")
                    .WithPublishDate(TurkeyTime.Now.AddDays(-1))
                    .Build());
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetForDashboardAsync("Admin", count: 3);

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region GetAllAdminAsync

    [Fact]
    public async Task GetAllAdminAsync_ReturnsAllIncludingInactive()
    {
        // Arrange
        var active = new AnnouncementBuilder().WithTitle("Active").Build();
        var inactive = new AnnouncementBuilder().WithTitle("Inactive").Inactive().Build();
        _context.Announcements.AddRange(active, inactive);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAdminAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion
}
