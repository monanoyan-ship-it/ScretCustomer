using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Tests.Unit.Builders;

public class AnnouncementBuilder
{
    private readonly Announcement _announcement = new()
    {
        Title = "Test Announcement",
        Content = "Test content",
        TypeId = AnnouncementTypes.Ids.Info,
        Priority = 3,
        PublishDate = TurkeyTime.Now.AddDays(-1),
        IsActive = true,
        IsPinned = false
    };

    public AnnouncementBuilder WithId(int id) { _announcement.Id = id; return this; }
    public AnnouncementBuilder WithTitle(string title) { _announcement.Title = title; return this; }
    public AnnouncementBuilder WithContent(string content) { _announcement.Content = content; return this; }
    public AnnouncementBuilder WithSummary(string summary) { _announcement.Summary = summary; return this; }
    public AnnouncementBuilder WithType(int typeId) { _announcement.TypeId = typeId; return this; }
    public AnnouncementBuilder WithPriority(int priority) { _announcement.Priority = priority; return this; }
    public AnnouncementBuilder WithPublishDate(DateTime date) { _announcement.PublishDate = date; return this; }
    public AnnouncementBuilder WithExpiryDate(DateTime date) { _announcement.ExpiryDate = date; return this; }
    public AnnouncementBuilder Expired() { _announcement.ExpiryDate = TurkeyTime.Now.AddDays(-1); return this; }
    public AnnouncementBuilder Pinned() { _announcement.IsPinned = true; return this; }
    public AnnouncementBuilder Inactive() { _announcement.IsActive = false; return this; }
    public AnnouncementBuilder Deleted() { _announcement.IsDeleted = true; return this; }
    public AnnouncementBuilder WithTargetRoles(string roles) { _announcement.TargetRoles = roles; return this; }
    public AnnouncementBuilder CreatedBy(int userId) { _announcement.CreatedByUserId = userId; return this; }

    public Announcement Build() => _announcement;
}
