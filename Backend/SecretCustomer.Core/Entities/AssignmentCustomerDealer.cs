namespace SecretCustomer.Core.Entities;

/// <summary>
/// Atama-Bayi İlişkisi - Bir atamaya hangi bayilerin dahil olduğunu belirtir
/// </summary>
public class AssignmentCustomerDealer : BaseEntity
{
    /// <summary>
    /// Atama ID
    /// </summary>
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    /// <summary>
    /// Bayi ID
    /// </summary>
    public int CustomerDealerId { get; set; }
    public CustomerDealer CustomerDealer { get; set; } = null!;

    /// <summary>
    /// Ziyaret yapıldıysa bağlı değerlendirme
    /// </summary>
    public int? EvaluationId { get; set; }
    public Evaluation? Evaluation { get; set; }

    /// <summary>
    /// Sıralama (opsiyonel - ziyaret önceliği için)
    /// </summary>
    public int SortOrder { get; set; } = 0;
}
