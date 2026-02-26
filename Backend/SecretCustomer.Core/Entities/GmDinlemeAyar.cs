namespace SecretCustomer.Core.Entities;

/// <summary>
/// Gölge Müşteri - Dinleme Ayar (dönem + müşteri → checklist eşleştirmesi)
/// </summary>
public class GmDinlemeAyar : BaseEntity
{
    public int GmDonemId { get; set; }
    public GmDonem? GmDonem { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int ChecklistId { get; set; }
    public Checklist? Checklist { get; set; }
}
