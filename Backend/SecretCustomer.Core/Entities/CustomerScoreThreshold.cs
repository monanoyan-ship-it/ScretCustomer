namespace SecretCustomer.Core.Entities;

public class CustomerScoreThreshold : BaseEntity
{
    public int CustomerId { get; set; }
    public int ProjectTypeId { get; set; }
    public decimal SuccessThreshold { get; set; } = 80;
    public decimal WarningThreshold { get; set; } = 60;
    public bool IsActive { get; set; } = true;

    // Navigation
    public Customer Customer { get; set; } = null!;
}
