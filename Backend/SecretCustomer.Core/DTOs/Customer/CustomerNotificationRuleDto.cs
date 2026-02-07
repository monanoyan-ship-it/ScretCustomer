namespace SecretCustomer.Core.DTOs.Customer;

public class CustomerNotificationRuleDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int FrequencyId { get; set; }
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public string? Emails { get; set; }
    public bool SendToPersonnel { get; set; }
    public int? EmailTemplateId { get; set; }
    public string? EmailTemplateName { get; set; }
    public int TokenExpirationDays { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSentAt { get; set; }
}

public class CreateCustomerNotificationRuleDto
{
    public int FrequencyId { get; set; }
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public string? Emails { get; set; }
    public bool SendToPersonnel { get; set; }
    public int? EmailTemplateId { get; set; }
    public int TokenExpirationDays { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}

public class UpdateCustomerNotificationRuleDto
{
    public int Id { get; set; }
    public int FrequencyId { get; set; }
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public string? Emails { get; set; }
    public bool SendToPersonnel { get; set; }
    public int? EmailTemplateId { get; set; }
    public int TokenExpirationDays { get; set; } = 30;
    public bool IsActive { get; set; }
}
